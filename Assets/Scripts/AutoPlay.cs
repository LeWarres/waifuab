using System.Text;
using UnityEngine;

// A bot that plays on its own, sped up, to measure how hard the game is: it flies the hero (closes in on the
// nearest enemy, keeps at about two thirds of its weapon's reach, circles, falls back to the train when hurt),
// takes every station's first upgrade, repairs when the train is hurt, buys wagons when it can, and skips the
// cargo. It cannot play the button games, so while it plays none come (TrainSim.NoEvents). Every minute of game
// time it notes how things stand in Log. Not part of the game: added from the editor while playing.
public class AutoPlay : MonoBehaviour
{
    public float speed = 5f;     // time scale while riding
    public float every = 30f;    // seconds of game time between two notes
    public static readonly StringBuilder Log = new(); // static: kept when a won chapter changes scene
    public bool Over { get; private set; }

    TrainSim train;
    float nextNote, nextChoice, orbit = 1f;
    int picks;
    bool fighting;
    float lastHealth;

    void Start()
    {
        train = GetComponent<TrainSim>();
        TrainSim.NoEvents = true;
        Player.Bot = Steer;
        Note("start");
    }

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
        Combat.Enemy foe = Combat.Instance.Nearest(p, 60f, null, true);
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
        if (train.Talking) { train.SkipTalk(); return; }
        if (train.DeliveryOpen) { train.CloseDelivery(); return; }
        int kind = train.MenuKind;
        if (kind < 0 || Time.unscaledTime < nextChoice) return;
        nextChoice = Time.unscaledTime + 0.15f;
        switch (kind)
        {
            case 0: Note("TRAIN DESTROYED"); Over = true; Time.timeScale = 1f; return;
            case 2: // the wagon offer: the first card, or the next one if that weapon is already at its top
                Pick(picks++ % Mathf.Max(1, train.OptionCount - 1));
                break;
            case 3: if (train.OptionEnabled(0)) { train.Choose(0); picks = 0; } else train.Choose(1); break;
            case 5: train.Choose(train.OptionCount - 1); break; // no cargo
            case 7: // the shop
                if (train.Health < train.MaxHealth * 0.75f && train.OptionEnabled(0)) train.Choose(0);
                else if (train.Player.Health < train.Player.maxHealth * 0.5f && train.OptionEnabled(1)) train.Choose(1);
                else if (Find("wagon") >= 0 && train.OptionEnabled(Find("wagon"))) train.Choose(Find("wagon"));
                else train.Choose(train.OptionCount - 1);
                break;
            case 8: Note("CHAPTER WON"); train.Choose(0); Over = true; Time.timeScale = 1f; return;
            default: Pick(0); break; // hero upgrade, route, placing cargo: the first
        }
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
                   $"cars={train.CarCount} enemies={Combat.Instance.EnemyCount} duel={train.Dueling} boss={train.BossFight && !train.Dueling} station={train.AtStation}\n");
    }
}
