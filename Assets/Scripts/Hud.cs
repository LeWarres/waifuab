using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// The always-on HUD, kept small: a dark glass panel for the train (where it is, health, trip) with money and
// medkits under it, one for the hero (weapon, health) and a thin health bar over every wagon and the hero.
// Also the flashing warning strip, the short result messages and the wagon numbers shown while placing things.
// It only reads from TrainSim and Player.
public class Hud : MonoBehaviour
{
    // A slanted bar that fills from the left.
    class Meter
    {
        public RectTransform root, fill;
        public Slanted colour;
        public float width;

        public Meter(Transform parent, Vector2 position, Vector2 size)
        {
            width = size.x;
            var back = new Color(0.02f, 0.03f, 0.08f, 0.6f);
            root = UiKit.Panel(parent, position, size, back, back, 0.25f).rectTransform;
            colour = UiKit.Panel(root, Vector2.zero, size, UiKit.Mint, UiKit.Mint, 0.25f);
            fill = colour.rectTransform;
            fill.pivot = new Vector2(0f, 0.5f);
            fill.anchoredPosition = new Vector2(-size.x * 0.5f, 0f);
        }

        public void Set(float fraction, bool health = true)
        {
            fraction = Mathf.Clamp01(fraction);
            fill.sizeDelta = new Vector2(Mathf.Max(width * fraction, fraction > 0f ? fill.sizeDelta.y * 0.6f : 0f), fill.sizeDelta.y);
            colour.enabled = fraction > 0f;
            if (health) colour.color = fraction > 0.5f ? UiKit.Mint : fraction > 0.25f ? UiKit.Gold : UiKit.Rose; // clean steps, not a muddy blend
        }
    }

    TrainSim train;
    Canvas canvas;
    Text where, level, trainText, money, leg, kits, heroText, hint, warnTitle, warnDetail, toastText;
    Slanted warnBand, toastPanel, hintPanel;
    float toastUntil;
    readonly List<Text> numbers = new();

    // Floating damage numbers: one text per hit, red over an enemy and yellow over a wagon, rising from the blow.
    class Popup
    {
        public Text text;
        public Vector3 at;
        public float birth, amount;
        public bool onTrain, critical, delivery;
    }

    readonly List<Popup> popupPool = new();
    readonly List<Popup> popups = new();
    const float popupLife = 0.9f;
    // Many hits at once (a minigun, a blast over a crowd) must not mean hundreds of texts: hits landing close
    // together in a short time add up into one number, and there is a top to how many are out.
    const int MaxPopups = 48;
    const float MergeTime = 0.18f, MergeDistance = 1.6f;
    Canvas popupCanvas; // their own canvas: they move every frame, and the rest of the HUD need not be rebuilt for it
    Meter trainBar, tripBar, heroBar;
    RawImage ammoIcon;
    // Under the train's card, two rows of boxes: the weapons carried (the hero's first) and the hero's upgrades,
    // each with its level.
    readonly List<(RawImage icon, Text level)> weapons = new(), passives = new();
    Text ammoText;
    readonly List<Meter> worldBars = new();
    RectTransform world;
    Slanted bossPanel;
    Text bossName;
    Meter bossBar;
    float bossUntil;

    void Awake()
    {
        train = GetComponent<TrainSim>();
        canvas = UiKit.NewCanvas("Hud", 0);

        // Small dark glass panels and thin bars: readable at a glance, out of the way of the action.
        var glass = new Color(0.06f, 0.09f, 0.19f, 0.62f);
        var dim = new Color(0.8f, 0.87f, 1f, 0.72f);

        // ---- train, top left: where we are, health, the trip; money and medkits in two pills under it ----
        RectTransform left = UiKit.Group(canvas.transform, new Vector2(0f, 1f), new Vector2(26f, -22f));
        left.localScale = Vector3.one * 1.15f;
        Slanted card = UiKit.Panel(left, new Vector2(215f, -52f), new Vector2(430f, 104f), glass, glass, 0.06f);
        UiKit.Panel(card.transform, new Vector2(-207f, 0f), new Vector2(5f, 104f), UiKit.Sky, UiKit.Sky, 0.06f);
        where = UiKit.Label(card.transform, UiKit.Heading, 24, Color.white, TextAnchor.MiddleLeft, new Vector2(-35f, 30f), new Vector2(300f, 30f), true);
        level = UiKit.Label(card.transform, UiKit.Body, 16, dim, TextAnchor.MiddleRight, new Vector2(150f, 30f), new Vector2(100f, 24f), true);
        UiKit.Picture(card.transform, "wagon", new Vector2(-180f, -2f), 28f);
        trainBar = new Meter(card.transform, new Vector2(-15f, -2f), new Vector2(290f, 12f));
        trainText = UiKit.Label(card.transform, UiKit.Body, 16, Color.white, TextAnchor.MiddleRight, new Vector2(170f, -2f), new Vector2(70f, 22f), true);
        tripBar = new Meter(card.transform, new Vector2(-15f, -22f), new Vector2(290f, 5f));
        tripBar.colour.color = UiKit.Sky;
        leg = UiKit.Label(card.transform, UiKit.Body, 14, dim, TextAnchor.MiddleLeft, new Vector2(-15f, -38f), new Vector2(290f, 20f), true);
        Slanted purse = UiKit.Panel(left, new Vector2(72f, -130f), new Vector2(140f, 40f), glass, glass, 0.12f);
        UiKit.Picture(purse.transform, "money", new Vector2(-44f, 0f), 30f);
        money = UiKit.Label(purse.transform, UiKit.Heading, 24, UiKit.Gold, TextAnchor.MiddleLeft, new Vector2(18f, 0f), new Vector2(86f, 30f), true);
        Slanted bag = UiKit.Panel(left, new Vector2(222f, -130f), new Vector2(140f, 40f), glass, glass, 0.12f);
        UiKit.Picture(bag.transform, "medkit", new Vector2(-44f, 0f), 28f);
        kits = UiKit.Label(bag.transform, UiKit.Body, 18, Color.white, TextAnchor.MiddleLeft, new Vector2(18f, 0f), new Vector2(86f, 28f), true);
        // Square see-through boxes side by side, Vampire Survivors style: five drawn even when empty, one more for
        // every thing past five.
        void Boxes(List<(RawImage, Text)> row, int count, float y, Color edge)
        {
            var fill = new Color(0f, 0f, 0f, 0.22f);
            for (int i = 0; i < count; i++)
            {
                Slanted box = UiKit.Panel(left, new Vector2(20f + i * 40f, y), new Vector2(40f, 40f), fill, fill, 0f);
                foreach (var (at, size) in new[] { (new Vector2(0f, 19f), new Vector2(40f, 2f)), (new Vector2(0f, -19f), new Vector2(40f, 2f)),
                                                   (new Vector2(-19f, 0f), new Vector2(2f, 40f)), (new Vector2(19f, 0f), new Vector2(2f, 40f)) })
                    UiKit.Panel(box.transform, at, size, edge, edge, 0f);
                RawImage icon = UiKit.Picture(box.transform, null, new Vector2(0f, 1f), 32f);
                Text lv = UiKit.Label(box.transform, UiKit.Heading, 13, UiKit.Gold, TextAnchor.LowerRight, new Vector2(1f, 0f), new Vector2(36f, 38f), true);
                row.Add((icon, lv));
            }
        }
        var line = new Color(1f, 1f, 1f, 0.35f);
        Boxes(weapons, 14, -178f, line);
        Boxes(passives, TrainSim.Passives.Length, -218f, line);

        // ---- hero, top right: weapon and health; the stats only at a station, the controls only at the start ----
        RectTransform right = UiKit.Group(canvas.transform, new Vector2(1f, 1f), new Vector2(-26f, -22f));
        right.localScale = Vector3.one * 1.15f;
        Slanted hero = UiKit.Panel(right, new Vector2(-175f, -38f), new Vector2(350f, 76f), glass, glass, 0.06f);
        UiKit.Panel(hero.transform, new Vector2(167f, 0f), new Vector2(5f, 76f), UiKit.Gold, UiKit.Gold, 0.06f);
        ammoIcon = UiKit.Picture(hero.transform, null, new Vector2(-138f, 0f), 46f);
        ammoText = UiKit.Label(hero.transform, UiKit.Heading, 20, Color.white, TextAnchor.MiddleLeft, new Vector2(20f, 15f), new Vector2(250f, 26f), true);
        heroBar = new Meter(hero.transform, new Vector2(-13f, -14f), new Vector2(185f, 10f));
        heroText = UiKit.Label(hero.transform, UiKit.Body, 16, Color.white, TextAnchor.MiddleRight, new Vector2(122f, -14f), new Vector2(76f, 22f), true);
        hintPanel = UiKit.Panel(right, new Vector2(-260f, -100f), new Vector2(520f, 32f), glass, glass, 0.12f);
        hint = UiKit.Label(hintPanel.transform, UiKit.Body, 15, dim, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(500f, 26f), true);

        world = UiKit.Group(canvas.transform, Vector2.zero, Vector2.zero);
        world.SetAsFirstSibling(); // world bars under the cards

        // Warning strip across the screen, and the short message that reports how an event went.
        warnBand = UiKit.Panel(canvas.transform, new Vector2(0f, -330f), new Vector2(2400f, 150f), UiKit.Gold, UiKit.Gold, 0f);
        warnBand.rectTransform.anchorMin = warnBand.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        UiKit.Panel(warnBand.transform, new Vector2(0f, 71f), new Vector2(2400f, 8f), UiKit.Navy, UiKit.Navy, 0f);
        UiKit.Panel(warnBand.transform, new Vector2(0f, -71f), new Vector2(2400f, 8f), UiKit.Navy, UiKit.Navy, 0f);
        warnTitle = UiKit.Label(warnBand.transform, UiKit.Heading, 68, UiKit.Navy, TextAnchor.MiddleCenter, new Vector2(0f, 22f), new Vector2(1700f, 84f), true);
        warnDetail = UiKit.Label(warnBand.transform, UiKit.Body, 30, UiKit.Navy, TextAnchor.MiddleCenter, new Vector2(0f, -40f), new Vector2(1700f, 42f), true);
        warnBand.gameObject.SetActive(false);

        var dark = new Color(UiKit.Navy.r, UiKit.Navy.g, UiKit.Navy.b, 0.9f);
        toastPanel = UiKit.Panel(canvas.transform, new Vector2(0f, 250f), new Vector2(1100f, 78f), dark, dark, 0.25f);
        toastPanel.rectTransform.anchorMin = toastPanel.rectTransform.anchorMax = new Vector2(0.5f, 0f);
        toastText = UiKit.Label(toastPanel.transform, UiKit.Heading, 38, Color.white, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(1040f, 70f), true);
        toastPanel.gameObject.SetActive(false);
    }

    // Call every frame; null hides it. Flashes gold and orange.
    public void Warning(string title, string detail)
    {
        bool on = title != null;
        if (warnBand.gameObject.activeSelf != on) warnBand.gameObject.SetActive(on);
        if (!on) return;
        warnTitle.text = title;
        warnDetail.text = detail;
        warnBand.color = warnBand.bottom = Mathf.PingPong(Time.unscaledTime * 6f, 1f) > 0.5f ? UiKit.Gold : new Color(1f, 0.55f, 0.1f);
    }

    // Per effect: its particle system, its root under the camera and the camera size it was laid out for.
    readonly Dictionary<string, (ParticleSystem effect, Transform root, float zoom, float[] until)> screens = new();

    // A one-second effect over the whole picture, from Resources/Screen (blood, healing, wind).
    // Asking again while it is still playing does nothing, so callers can ask every frame.
    public void Flash(string effect)
    {
        if (!screens.TryGetValue(effect, out var screen))
        {
            // The pack's script has just sized it for the camera as it is now; from here on it only rides the
            // camera, and LateUpdate scales it with the zoom (the script would only resize particles not born yet).
            Camera cam = Camera.main;
            var made = Instantiate(Resources.Load<Hovl.HS_ScreenEffect>("Screen/" + effect), cam.transform);
            made.enabled = false;
            made.transform.SetLocalPositionAndRotation(Vector3.forward * 1.5f, Quaternion.identity); // past the near plane
            foreach (ParticleSystem part in made.GetComponentsInChildren<ParticleSystem>())
            {
                var main = part.main;
                main.scalingMode = ParticleSystemScalingMode.Hierarchy;
                main.useUnscaledTime = true; // or it freezes on screen whenever the game stops: station, pause, slow motion
                main.loop = false;
                if (effect == "wind") main.simulationSpace = ParticleSystemSimulationSpace.Local;
            }
            if (effect == "wind")
                foreach (ParticleSystemRenderer renderer in made.GetComponentsInChildren<ParticleSystemRenderer>())
                    renderer.alignment = ParticleSystemRenderSpace.Local;
            screens[effect] = screen = (made.GetComponentInChildren<ParticleSystem>(), made.transform, cam.orthographicSize, new float[1]);
        }
        screen.root.gameObject.SetActive(true);
        if (!screen.effect.isPlaying) screen.effect.Play(true);
        screen.until[0] = Time.unscaledTime + 1.5f;
    }

    // Call every frame while a boss is on the field; it hides itself a moment after the last call.
    public void Boss(string name, float fraction)
    {
        if (bossPanel == null)
        {
            var glass = new Color(0.06f, 0.09f, 0.19f, 0.78f);
            bossPanel = UiKit.Panel(canvas.transform, new Vector2(0f, -74f), new Vector2(780f, 84f), glass, glass, 0.1f);
            bossPanel.rectTransform.anchorMin = bossPanel.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            UiKit.Panel(bossPanel.transform, new Vector2(0f, 41f), new Vector2(780f, 6f), UiKit.Rose, UiKit.Rose, 0.1f);
            bossName = UiKit.Label(bossPanel.transform, UiKit.Heading, 30, Color.white, TextAnchor.MiddleCenter, new Vector2(0f, 22f), new Vector2(740f, 34f), true);
            bossBar = new Meter(bossPanel.transform, new Vector2(0f, -16f), new Vector2(700f, 18f));
        }
        bossName.text = name;
        bossBar.Set(fraction, false);
        bossBar.colour.color = bossBar.colour.bottom = UiKit.Rose; // a boss does not go green as it is worn down
        bossUntil = Time.unscaledTime + 0.25f;
    }

    const int BaseBoxes = 5;

    // A box on screen (slot), with or without a symbol in it (shown).
    static void Show((RawImage icon, Text level) box, bool shown, bool slot)
    {
        GameObject frame = box.icon.transform.parent.gameObject;
        if (frame.activeSelf != slot) frame.SetActive(slot);
        if (box.icon.enabled != shown) box.icon.enabled = shown;
        if (!shown) box.level.text = "";
    }

    public void Toast(string text, float seconds = 2.5f)
    {
        toastText.text = text;
        toastUntil = Time.unscaledTime + seconds;
    }

    // A number for one blow: red when the enemy is hurt, yellow when a wagon is, at the point of the hit.
    public void DeliveryNumber(Vector3 at, int amount) => DamageNumber(at, Mathf.Abs(amount), false, false,
        L10n.T("delivery.amount", amount.ToString("+0;-0;0")), amount < 0 ? UiKit.Rose : UiKit.Mint);

    public void DamageNumber(Vector3 at, float amount, bool onTrain, bool critical = false, string caption = null, Color? tint = null)
    {
        Popup popup = null;
        float now = Time.unscaledTime;
        for (int i = popups.Count - 1; i >= 0 && now - popups[i].birth <= MergeTime; i--) // newest last
            if (caption == null && !popups[i].delivery && popups[i].onTrain == onTrain && popups[i].critical == critical && (popups[i].at - at).sqrMagnitude <= MergeDistance * MergeDistance)
            {
                popups[i].amount += amount;
                popups[i].text.text = Mathf.Max(1, Mathf.RoundToInt(popups[i].amount)).ToString() + (critical ? "!" : "");
                return;
            }
        if (popups.Count >= MaxPopups)
        {
            popup = popups[0]; // the oldest gives way
            popups.RemoveAt(0);
        }
        else if (popupPool.Count > 0)
        {
            popup = popupPool[popupPool.Count - 1];
            popupPool.RemoveAt(popupPool.Count - 1);
        }
        else
        {
            if (!popupCanvas)
            {
                popupCanvas = UiKit.NewCanvas("DamageNumbers", 1);
                Destroy(popupCanvas.GetComponent<GraphicRaycaster>()); // nothing here is clicked
            }
            Text text = UiKit.Label(popupCanvas.transform, UiKit.Heading, 46, Color.white, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(200f, 70f), true);
            text.rectTransform.anchorMin = text.rectTransform.anchorMax = Vector2.zero; // placed in screen points
            var outline = text.gameObject.AddComponent<Outline>(); // keeps it readable over any ground
            outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
            outline.effectDistance = new Vector2(2.5f, -2.5f);
            popup = new Popup { text = text };
        }
        popup.text.text = caption ?? Mathf.Max(1, Mathf.RoundToInt(amount)).ToString() + (critical ? "!" : "");
        popup.text.color = tint ?? (critical ? new Color(1f, 0.55f, 0.1f) : onTrain ? UiKit.Gold : new Color(1f, 0.25f, 0.2f)); // a critical: orange, and it ends in "!"
        popup.text.fontSize = critical ? 56 : 46;
        popup.at = at;
        popup.amount = amount;
        popup.onTrain = onTrain;
        popup.critical = critical;
        popup.delivery = caption != null;
        popup.birth = now;
        popup.text.gameObject.SetActive(true);
        popups.Add(popup);
    }

    // ---- the pause menu's Evolutions: every weapon, the upgrade that evolves it, and what it becomes ----
    Canvas evolutions;
    readonly List<(Weapon weapon, RawImage gun, RawImage passive, Text name)> recipes = new();

    // Shown while the pause menu has it open; what is owned is lit, the rest faded, the evolved ones in gold.
    void Evolutions()
    {
        if (!train.EvolutionsOpen)
        {
            if (evolutions && evolutions.gameObject.activeSelf) evolutions.gameObject.SetActive(false);
            return;
        }
        if (!evolutions) BuildEvolutions();
        if (!evolutions.gameObject.activeSelf) evolutions.gameObject.SetActive(true);
        var faded = new Color(1f, 1f, 1f, 0.3f);
        var arsenal = train.Arsenal();
        foreach (var r in recipes)
        {
            int level = 0;
            foreach (var a in arsenal) if (a.weapon == r.weapon) level = a.level;
            bool partner = train.PassiveLevel(r.weapon.evolvesWith) > 0;
            r.gun.color = level > 0 ? Color.white : faded;
            r.passive.color = partner ? Color.white : faded;
            r.name.color = level >= Weapon.Evolved ? UiKit.Gold : level > 0 && partner ? Color.white : new Color(0.75f, 0.8f, 0.95f, 0.6f);
        }
    }

    void BuildEvolutions()
    {
        evolutions = UiKit.NewCanvas("Evolutions", 15); // over the pause menu
        Transform root = evolutions.transform;
        var shade = new Color(0.03f, 0.05f, 0.12f, 1f); // solid: the pause menu stays hidden under it
        var dim = new Color(0.8f, 0.87f, 1f, 0.72f);
        UiKit.Panel(root, Vector2.zero, new Vector2(2600f, 1500f), shade, shade, 0f);
        UiKit.Label(root, UiKit.Heading, 46, Color.white, TextAnchor.MiddleCenter, new Vector2(0f, 480f), new Vector2(1600f, 60f), true).text = L10n.T("evo.title");
        UiKit.Label(root, UiKit.Body, 22, dim, TextAnchor.MiddleCenter, new Vector2(0f, 436f), new Vector2(1700f, 32f), true).text = L10n.T("evo.rule");
        var all = new List<(Weapon, bool)>();
        foreach (Weapon w in Weapon.Hero) all.Add((w, true));
        foreach (Weapon w in Weapon.Wagon) all.Add((w, false));
        var row = new Color(1f, 1f, 1f, 0.05f);
        for (int i = 0; i < all.Count; i++)
        {
            var (weapon, hero) = all[i];
            int passive = System.Array.IndexOf(TrainSim.Passives, weapon.evolvesWith);
            RectTransform line = UiKit.Group(root, new Vector2(0.5f, 0.5f), new Vector2(i < 10 ? -470f : 470f, 370f - (i % 10) * 78f));
            UiKit.Panel(line, Vector2.zero, new Vector2(900f, 70f), row, row, 0f);
            RawImage gun = UiKit.Picture(line, weapon.id, new Vector2(-400f, 0f), 56f);
            UiKit.Label(line, UiKit.Heading, 30, dim, TextAnchor.MiddleCenter, new Vector2(-348f, 0f), new Vector2(30f, 40f), true).text = "+";
            RawImage partner = UiKit.Picture(line, passive >= 0 ? TrainSim.PassiveIcons[passive] : null, new Vector2(-296f, 0f), 56f);
            UiKit.Label(line, UiKit.Heading, 30, dim, TextAnchor.MiddleCenter, new Vector2(-240f, 0f), new Vector2(40f, 40f), true).text = "=";
            Text name = UiKit.Label(line, UiKit.Heading, 26, Color.white, TextAnchor.MiddleLeft, new Vector2(0f, 10f), new Vector2(420f, 34f), true);
            name.text = weapon.EvolvedName;
            UiKit.Label(line, UiKit.Body, 17, dim, TextAnchor.MiddleLeft, new Vector2(0f, -18f), new Vector2(420f, 24f), true).text =
                L10n.T(hero ? "evo.hero" : "evo.wagon", weapon.Name, passive >= 0 ? L10n.T("passive." + weapon.evolvesWith) : "");
            recipes.Add((weapon, gun, partner, name));
        }
        UiKit.Label(root, UiKit.Body, 20, dim, TextAnchor.MiddleCenter, new Vector2(0f, -440f), new Vector2(1600f, 30f), true).text = L10n.T("evo.help");
    }

    void LateUpdate()
    {
        Evolutions();
        float zoom = Camera.main.orthographicSize;
        foreach (var entry in screens)
        {
            var screen = entry.Value;
            screen.root.localScale = Vector3.one * (zoom / screen.zoom); // keep covering the picture when the camera zooms
            if (entry.Key == "wind" && train.CarCount > 0)
            {
                Camera cam = Camera.main;
                Vector3 at = train.CarPosition(0);
                Vector3 direction = cam.WorldToScreenPoint(at + train.Forward(0)) - cam.WorldToScreenPoint(at);
                if (direction.sqrMagnitude > 0.001f)
                {
                    float angle = Mathf.Atan2(direction.y, direction.x);
                    screen.root.localRotation = Quaternion.Euler(0f, 0f, angle * Mathf.Rad2Deg);
                    // A rotated fullscreen rectangle needs extra coverage to keep its corners offscreen.
                    float c = Mathf.Abs(Mathf.Cos(angle)), s = Mathf.Abs(Mathf.Sin(angle));
                    screen.root.localScale *= Mathf.Max(c + s / cam.aspect, c + s * cam.aspect);
                }
            }
            // Gone for good a moment after the last time it was asked for, and never over a menu.
            bool show = Time.unscaledTime < screen.until[0] && !train.MenuShown;
            if (!show && screen.root.gameObject.activeSelf) screen.effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (screen.root.gameObject.activeSelf != show) screen.root.gameObject.SetActive(show);
        }
        bool toast = Time.unscaledTime < toastUntil;
        if (toastPanel.gameObject.activeSelf != toast) toastPanel.gameObject.SetActive(toast);

        bool boss = Time.unscaledTime < bossUntil && !train.MenuShown;
        if (bossPanel != null && bossPanel.gameObject.activeSelf != boss) bossPanel.gameObject.SetActive(boss);

        // Wagon numbers, to match the cards to the train while placing a weapon or a cargo wagon.
        int tags = train.ShowCarNumbers ? train.CarCount : 0;
        while (numbers.Count < tags)
        {
            Slanted pill = UiKit.Panel(world, Vector2.zero, new Vector2(64f, 56f), UiKit.Navy, UiKit.Navy, 0.1f);
            numbers.Add(UiKit.Label(pill.transform, UiKit.Heading, 38, Color.white, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(58f, 52f), true));
        }
        for (int i = 0; i < numbers.Count; i++)
        {
            Transform pill = numbers[i].transform.parent;
            pill.gameObject.SetActive(i < tags);
            if (i >= tags) continue;
            Vector3 at = Camera.main.WorldToScreenPoint(train.CarTop(i));
            ((RectTransform)pill).anchoredPosition = new Vector2(at.x, at.y) / canvas.scaleFactor + Vector2.up * 40f;
            numbers[i].text = (i + 1).ToString();
        }

        // Damage numbers: rise away from the blow and fade. Gone early when a menu opens. While the swarm is
        // fought the picture comes from the gunner's camera, so the numbers are placed with that one instead.
        Camera view = train.SwarmCamera ?? Camera.main;
        for (int i = popups.Count - 1; i >= 0; i--)
        {
            Popup popup = popups[i];
            float age = Time.unscaledTime - popup.birth;
            if (age >= popupLife || train.MenuShown)
            {
                popup.text.gameObject.SetActive(false);
                popupPool.Add(popup);
                popups.RemoveAt(i);
                continue;
            }
            float k = age / popupLife;
            Vector3 screen = view.WorldToScreenPoint(popup.at + Vector3.up * (k * 1.9f));
            popup.text.rectTransform.anchoredPosition = new Vector2(screen.x, screen.y) / canvas.scaleFactor;
            popup.text.rectTransform.localScale = Vector3.one * (1f + 0.6f * Mathf.Clamp01(1f - k * 5f)); // a little pop as it is born
            Color colour = popup.text.color;
            colour.a = 1f - Mathf.Clamp01((k - 0.55f) / 0.45f);
            popup.text.color = colour;
        }

        Player hero = train.Player;
        float health = train.Health, max = train.MaxHealth;
        where.text = Story.Active ? L10n.T("hud.chapter", Story.Chapter + 1, train.BiomeLabel, train.StoryTime) : L10n.T("hud.where", train.Stations, train.BiomeLabel);
        level.text = L10n.T("hud.level", train.Difficulty);
        trainBar.Set(health / max);
        trainText.text = $"{Mathf.Round(health)}/{Mathf.Round(max)}";
        money.text = train.money.ToString();
        leg.text = train.LegLabel;
        tripBar.Set(train.LegProgress, false);
        kits.text = $"{train.Kits}  ·  {train.KitButton}";

        heroBar.Set(hero.Health / hero.maxHealth);
        heroText.text = hero.Alive ? $"{hero.Health:0}/{hero.maxHealth:0}" : "0";
        UiKit.SetIcon(ammoIcon, hero.ammo.id);
        ammoText.text = !hero.Alive ? L10n.T("player.down") : hero.level >= Weapon.Evolved ? L10n.T("player.evo", hero.ammo.EvolvedName) : L10n.T("player.slot", hero.ammo.Name, hero.level);
        var arsenal = train.Arsenal();
        for (int i = 0; i < weapons.Count; i++)
        {
            bool shown = i < arsenal.Count;
            Show(weapons[i], shown, i < Mathf.Max(BaseBoxes, arsenal.Count));
            if (!shown) continue;
            UiKit.SetIcon(weapons[i].icon, arsenal[i].weapon.id);
            int lv = arsenal[i].level;
            weapons[i].level.text = lv >= Weapon.Evolved ? "EVO" : lv >= Weapon.MaxLevel ? "MAX" : lv.ToString();
        }
        for (int i = 0; i < passives.Count; i++)
        {
            bool shown = i < train.passiveOrder.Count;
            Show(passives[i], shown, i < Mathf.Max(BaseBoxes, train.passiveOrder.Count));
            if (!shown) continue;
            string id = train.passiveOrder[i];
            UiKit.SetIcon(passives[i].icon, TrainSim.PassiveIcons[System.Array.IndexOf(TrainSim.Passives, id)]);
            int lv = train.PassiveLevel(id);
            passives[i].level.text = lv >= TrainSim.MaxPassive ? "MAX" : lv.ToString();
        }
        // One quiet line under the hero: the stats while shopping, the controls for the first stretch of a run.
        hint.text = train.AtStation ? L10n.T("hud.hero.stats", Mathf.RoundToInt(hero.damageBonus * 100f), hero.speed.ToString("0.#"),
                Mathf.RoundToInt(hero.armor * 100f), Mathf.RoundToInt(hero.moneyBonus * 100f))
            : train.Stations == 0 && train.LegProgress < 0.3f ? L10n.T(hero.UsingGamepad ? "player.help.gamepad" : "player.help.keyboard") : "";
        if (hintPanel.gameObject.activeSelf != (hint.text != "")) hintPanel.gameObject.SetActive(hint.text != "");

        // One small bar over every wagon, one over the hero; hidden while a menu covers the train.
        int wanted = train.MenuShown || train.Swarming ? 0 : train.CarCount + (hero.Alive ? 1 : 0); // not from the gunner's seat
        while (worldBars.Count < wanted) worldBars.Add(new Meter(world, Vector2.zero, new Vector2(76f, 10f)));
        float scale = canvas.scaleFactor;
        for (int i = 0; i < worldBars.Count; i++)
        {
            bool on = i < wanted;
            worldBars[i].root.gameObject.SetActive(on);
            if (!on) continue;
            bool wagon = i < train.CarCount;
            Vector3 screen = Camera.main.WorldToScreenPoint(wagon ? train.CarTop(i) : hero.Position + Vector3.up * 1.6f);
            worldBars[i].root.anchoredPosition = new Vector2(screen.x, screen.y) / scale;
            worldBars[i].Set(wagon ? train.CarFill(i) : hero.Health / hero.maxHealth);
        }
    }
}
