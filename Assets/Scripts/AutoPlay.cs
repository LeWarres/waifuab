using System.Text;
using UnityEngine;

// A bot that plays on its own, sped up, to measure how hard the game is: it flies the hero (closes in on the
// nearest enemy, keeps at about two thirds of its weapon's reach, circles, falls back to the train when hurt),
// builds on what it has at the stations (a level on a weapon it carries before anything new), repairs when the
// train is hurt, buys wagons when it can, and skips the cargo. It cannot play the button games, so while it plays none come (TrainSim.NoEvents). Every minute of game
// time it notes how things stand in Log. Not part of the game: added from the editor while playing.
public class AutoPlay : MonoBehaviour
{
    public float speed = 5f;     // time scale while riding
    public float every = 30f;    // seconds of game time between two notes
    public bool smart;           // picks the hero's cards like a player who knows the game: her weapon, then damage first
    public bool events;          // leaves the road's events on (it plays none of their buttons, so it fails them all)
    public static readonly StringBuilder Log = new(); // static: kept when a won chapter changes scene
    public bool Over { get; private set; }

    TrainSim train;
    float nextNote, nextChoice, orbit = 1f;
    bool fighting;
    float lastHealth;

    void Start()
    {
        train = GetComponent<TrainSim>();
        TrainSim.NoEvents = !events;
        Player.Bot = Steer;
        Note("start");
    }

    // A test bot never outlives the play session it was added in: it must not be there when someone plays.
    void OnApplicationQuit() => Destroy(this);

    void OnDestroy()
    {
        TrainSim.NoEvents = false;
        Player.Bot = null;
        if (Time.timeScale > 1f) Time.timeScale = 1f;
    }

    // The hero's stick, as the bot pushes it.
    Vector2 Steer()
    {
        Player hero = train.Player;
        Vector3 p = hero.Position, want;
        p.y = 0f;
        Combat.Enemy foe = Combat.Instance.Threat(p, 80f) ?? Combat.Instance.Nearest(p, 60f, null, true); // the bombs only she can stop first, as a player would
        float reach = hero.ammo.range;
        if (hero.Health < hero.maxHealth * 0.3f || foe == null)
            want = train.Focus + Vector3.Cross(Vector3.up, train.Forward(0)) * 6f - p; // back beside the train
        else
        {
            Vector3 to = foe.position - p;
            to.y = 0f;
            float d = to.magnitude;
            if (Random.value < 0.002f) orbit = -orbit;
            want = d > reach * 0.75f ? to : d < reach * 0.45f ? -to : Vector3.Cross(Vector3.up, to) * orbit;
        }
        if (want.sqrMagnitude < 0.5f) return Vector2.zero;
        Transform cam = Camera.main.transform;
        Vector3 forward = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
        want.Normalize();
        return new Vector2(Vector3.Dot(want, cam.right), Vector3.Dot(want, forward));
    }

    void Update()
    {
        if (Over) return;
        if (Mathf.Approximately(Time.timeScale, 1f)) Time.timeScale = speed; // the game sets 0 for its menus and talks
        if (Time.time >= nextNote) { nextNote = Time.time + every; Note("."); }
        if (train.BossFight != fighting) { fighting = train.BossFight; Note(fighting ? "BOSS IN" : "BOSS OUT"); }
        if (train.Health < lastHealth - train.MaxHealth * 0.15f) Note("HIT"); // a big loss at once: who did it
        lastHealth = train.Health;
        if (train.Talking) return; // Dialogue always waits for the player's confirmation, including bot sessions.
        if (train.DeliveryOpen) { train.CloseDelivery(); return; }
        if (train.ChestOpen) { train.CloseChest(); return; }
        int kind = train.MenuKind;
        if (kind < 0 || Time.unscaledTime < nextChoice) return;
        nextChoice = Time.unscaledTime + 0.15f;
        switch (kind)
        {
            case 0: Note("TRAIN DESTROYED"); Over = true; Time.timeScale = 1f; return;
            case 2: // the wagon offer: a level on a weapon already carried, else the first card
                Pick(Owned(false) >= 0 ? Owned(false) : 0);
                break;
            case 4: // the hero: a level on her weapon, else an upgrade; never another weapon
                Pick(Owned(true) >= 0 ? Owned(true) : smart ? Best() : NotAmmo());
                break;
            case 3: if (train.OptionEnabled(0)) train.Choose(0); else train.Choose(1); break;
            case 5: train.Choose(train.OptionCount - 1); break; // no cargo
            case 7: // the shop
                if (train.Health < train.MaxHealth * 0.75f && train.OptionEnabled(0)) train.Choose(0);
                else if (train.Player.Health < train.Player.maxHealth * 0.5f && train.OptionEnabled(1)) train.Choose(1);
                else if (Find("wagon") >= 0 && train.OptionEnabled(Find("wagon"))) train.Choose(Find("wagon"));
                else train.Choose(train.OptionCount - 1);
                break;
            case 11: // the junction: the truck only with the train in good shape, like a careful player
                Note(train.Health > train.MaxHealth * 0.75f ? "TRUCK" : "SKIP TRUCK");
                train.Choose(train.Health > train.MaxHealth * 0.75f ? 0 : 1);
                break;
            case 8: Note("CHAPTER WON"); train.Choose(0); Over = true; Time.timeScale = 1f; return;
            default: Pick(0); break; // hero upgrade, route, placing cargo: the first
        }
    }

    // Like a player who builds on what they have: the card of a weapon already carried (the hero's, or the wagons').
    int Owned(bool hero)
    {
        for (int i = 0; i < train.OptionCount; i++)
        {
            if (!train.OptionEnabled(i)) continue;
            string icon = train.OptionIcon(i);
            if (hero ? icon == train.Player.ammo.id : train.Arsenal().FindIndex(1, a => a.weapon.id == icon && a.level < Weapon.MaxLevel) >= 0) return i;
        }
        return -1;
    }

    // The first card that is not another weapon for the hero.
    int NotAmmo()
    {
        for (int i = 0; i < train.OptionCount; i++)
            if (train.OptionEnabled(i) && System.Array.FindIndex(Weapon.Hero, w => w.id == train.OptionIcon(i)) < 0) return i;
        return 0;
    }

    // The upgrade a player who knows the game takes (by its icon, TrainSim.PassiveIcons): damage, fire rate,
    // critical chance and damage, then armor and vitality; anything else only when none of those is offered.
    static readonly string[] Wanted = { "arms", "minigun", "sniper", "rail", "armor", "vitality" };
    // Before those: the upgrade that evolves a weapon she or a wagon carries (Weapon.evolvesWith), as a player who
    // knows the evolutions builds for them.
    int Best()
    {
        foreach (var a in train.Arsenal())
        {
            int stat = System.Array.IndexOf(TrainSim.Passives, a.weapon.evolvesWith);
            int at = stat < 0 ? -1 : Find(TrainSim.PassiveIcons[stat]);
            if (at >= 0 && train.OptionEnabled(at) && train.PassiveLevel(a.weapon.evolvesWith) == 0) return at;
        }
        foreach (string icon in Wanted) { int at = Find(icon); if (at >= 0 && train.OptionEnabled(at)) return at; }
        return NotAmmo();
    }

    int Find(string icon)
    {
        for (int i = 0; i < train.OptionCount; i++) if (train.OptionIcon(i) == icon) return i;
        return -1;
    }

    void Pick(int i)
    {
        for (int k = 0; k < train.OptionCount; k++)
        {
            int at = (i + k) % train.OptionCount;
            if (train.OptionEnabled(at)) { train.Choose(at); return; }
        }
    }

    void Note(string what)
    {
        Player hero = train.Player;
        Log.Append($"{what} t={Time.time / 60f:0.0}m clock={train.StoryClock / 60f:0.0}m dif={train.Difficulty} st={train.Stations} " +
                   $"train={train.Health:0}/{train.MaxHealth:0} hero={(hero ? hero.Health : 0f):0} lv={(hero ? hero.level : 0)} money={train.money} " +
                   $"cars={train.CarCount} enemies={Combat.Instance.EnemyCount} duel={train.Dueling} boss={train.BossFight && !train.Dueling} station={train.AtStation} " +
                   $"chests={train.Chests} mini={Combat.Instance.MiniBossesBeaten} arms={Arms()} upgrades={Upgrades()}\n");
    }

    // The weapons as id+level (the hero's first), and the hero's upgrades as id+level.
    string Arms()
    {
        var s = new StringBuilder();
        foreach (var a in train.Arsenal()) s.Append(a.weapon.id).Append(a.level).Append(',');
        return s.ToString();
    }

    string Upgrades()
    {
        var s = new StringBuilder();
        foreach (string id in train.passiveOrder) s.Append(id).Append(train.PassiveLevel(id)).Append(',');
        return s.ToString();
    }
}
