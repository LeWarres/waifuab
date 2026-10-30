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

        public Meter(Transform parent, Vector2 position, Vector2 size, int notches = 0)
        {
            width = size.x;
            var back = new Color(0.01f, 0.02f, 0.06f, 0.8f);
            root = UiKit.Panel(parent, position, size, back, back, 0.25f).rectTransform;
            colour = UiKit.Panel(root, Vector2.zero, size, UiKit.Mint, UiKit.Mint, 0.25f);
            fill = colour.rectTransform;
            fill.pivot = new Vector2(0f, 0.5f);
            fill.anchoredPosition = new Vector2(-size.x * 0.5f, 0f);
            root.GetComponent<Slanted>().pixelsPerUnitMultiplier = colour.pixelsPerUnitMultiplier = 3f; // squarer ends
            // Notches across it, so how much is left reads at a glance; a light line along its top.
            var notch = new Color(0.01f, 0.02f, 0.06f, 0.55f);
            for (int i = 1; i < notches; i++) UiKit.Panel(root, new Vector2((i / (float)notches - 0.5f) * size.x, 0f), new Vector2(2f, size.y), notch, notch, 0.25f);
            if (notches > 0)
            {
                RectTransform shine = UiKit.Panel(fill, Vector2.zero, new Vector2(size.x, size.y * 0.18f), new Color(1f, 1f, 1f, 0.28f), new Color(1f, 1f, 1f, 0.28f), 0f).rectTransform;
                shine.anchorMin = shine.anchorMax = shine.pivot = new Vector2(0f, 0.5f);
                shine.anchoredPosition = new Vector2(0f, size.y * 0.28f);
            }
        }

        public void Set(float fraction, bool health = true)
        {
            fraction = Mathf.Clamp01(fraction);
            fill.sizeDelta = new Vector2(Mathf.Max(width * fraction, fraction > 0f ? fill.sizeDelta.y * 0.6f : 0f), fill.sizeDelta.y);
            if (fill.childCount > 0) ((RectTransform)fill.GetChild(0)).sizeDelta = new Vector2(fill.sizeDelta.x, fill.sizeDelta.y * 0.18f);
            colour.enabled = fraction > 0f;
            if (health) colour.color = fraction > 0.5f ? UiKit.Mint : fraction > 0.25f ? UiKit.Gold : UiKit.Rose; // clean steps, not a muddy blend
        }
    }

    TrainSim train;
    Canvas canvas;
    Text where, level, trainText, money, leg, kits, heroText, hint, warnTitle, warnDetail, toastText, stationNumber;
    Prompt controls, ultimateKey, ultimateNoticeLine;
    RectTransform tripMarker;
    Slanted warnBand, toastPanel, hintPanel;
    float toastUntil;
    readonly List<Text> numbers = new();

    // Floating damage numbers: one text per hit, red over an enemy and yellow over a wagon, rising from the blow.
    class Popup
    {
        public Text text;
        public Vector3 at;
        public float birth, amount, drift;
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
    Meter ultimateBar;
    Text killsText; // top middle: enemies shot down
    Text ultimateText;
    Slanted ultimatePanel, ultimateEdge;
    Slanted ultimateNotice;
    Text ultimateNoticeText;
    CanvasGroup ultimateNoticeFade;
    float ultimateNoticeUntil;
    const float UltimateNoticeSeconds = 2.5f;
    public bool Toasting => Time.unscaledTime < toastUntil || Time.unscaledTime < ultimateNoticeUntil;
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

        // Station signs, as at the stations: dark plates on hard shadows, a badge with a number or an icon, the big
        // words in the display font, thick bars with notches.
        var dim = new Color(0.8f, 0.87f, 1f, 0.72f);
        var paper = new Color(1f, 0.985f, 0.94f);

        // ---- train, top left: the station's badge, the biome, health, the trip as a stretch of track with the
        // train running along it to the next station; money and medkits on tickets under it ----
        RectTransform left = UiKit.Group(canvas.transform, new Vector2(0f, 1f), new Vector2(26f, -22f));
        left.localScale = Vector3.one * 1.1f;
        left.localRotation = Quaternion.Euler(0f, 0f, 1.2f); // a touch off level, Persona style
        Slanted card = Plate(left, new Vector2(222f, -64f), new Vector2(444f, 128f));
        (_, stationNumber) = Badge(card.transform, new Vector2(-172f, 6f), 84f, UiKit.Ink.Main);
        where = UiKit.Label(card.transform, UiKit.Display, 32, Color.white, TextAnchor.MiddleLeft, new Vector2(-6f, 38f), new Vector2(220f, 42f), true);
        Slanted tag = UiKit.Paint(Sharp(UiKit.Panel(card.transform, new Vector2(160f, 40f), new Vector2(96f, 30f), Color.white, Color.white, 0.25f), 3f), UiKit.Ink.Hot);
        level = UiKit.Label(tag.transform, UiKit.Display, 20, UiKit.Navy, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(88f, 28f), true);
        trainBar = new Meter(card.transform, new Vector2(25f, 4f), new Vector2(274f, 20f), 10);
        trainText = Numbers(card.transform, new Vector2(25f, 4f), new Vector2(262f, 24f), 20);
        // The track: sleepers, two rails, the stretch done lit in blue, a station at its end and the train on it.
        for (float x = -108f; x <= 158f; x += 11f) UiKit.Panel(card.transform, new Vector2(x, -26f), new Vector2(3f, 12f), new Color(0.45f, 0.36f, 0.3f), new Color(0.45f, 0.36f, 0.3f), 0f);
        foreach (float y in new[] { -22f, -30f }) UiKit.Panel(card.transform, new Vector2(25f, y), new Vector2(274f, 2f), new Color(0.65f, 0.7f, 0.8f), new Color(0.65f, 0.7f, 0.8f), 0f);
        tripBar = new Meter(card.transform, new Vector2(25f, -26f), new Vector2(274f, 4f));
        UiKit.Paint(tripBar.colour, UiKit.Ink.Main);
        tripBar.root.GetComponent<Slanted>().color = tripBar.root.GetComponent<Slanted>().bottom = new Color(0f, 0f, 0f, 0f);
        Slanted stop = UiKit.Paint(UiKit.Panel(card.transform, new Vector2(166f, -26f), new Vector2(20f, 20f), Color.white, Color.white, 0f), UiKit.Ink.Main);
        stop.sprite = UiKit.Disc();
        UiKit.Panel(stop.transform, Vector2.zero, new Vector2(10f, 10f), Color.white, Color.white, 0f).sprite = UiKit.Disc();
        tripMarker = UiKit.Picture(card.transform, "wagon", new Vector2(-110f, -20f), 30f).rectTransform;
        leg = UiKit.Label(card.transform, UiKit.Body, 14, dim, TextAnchor.MiddleLeft, new Vector2(25f, -48f), new Vector2(274f, 20f), true);
        Slanted purse = Ticket(left, new Vector2(74f, -158f), new Vector2(144f, 42f), paper);
        UiKit.Picture(purse.transform, "money", new Vector2(-44f, 0f), 32f);
        money = UiKit.Label(purse.transform, UiKit.Display, 28, UiKit.Navy, TextAnchor.MiddleLeft, new Vector2(18f, 0f), new Vector2(86f, 36f), true);
        Slanted bag = Ticket(left, new Vector2(228f, -158f), new Vector2(144f, 42f), paper);
        bag.transform.parent.gameObject.SetActive(TrainSim.KitsInUse); // the medkits, put away for now
        UiKit.Picture(bag.transform, "medkit", new Vector2(-44f, 0f), 28f);
        kits = UiKit.Label(bag.transform, UiKit.Display, 22, UiKit.Navy, TextAnchor.MiddleLeft, new Vector2(18f, 0f), new Vector2(86f, 30f), true);
        // Square boxes side by side, Vampire Survivors style: five drawn even when empty, one more for every thing
        // past five. The level on a small gold tag in the corner.
        void Boxes(List<(RawImage, Text)> row, int count, float y, Color edge)
        {
            var fill = new Color(0.04f, 0.06f, 0.14f, 0.72f);
            for (int i = 0; i < count; i++)
            {
                Slanted box = UiKit.Panel(left, new Vector2(21f + i * 42f, y), new Vector2(40f, 40f), fill, fill, 0f);
                box.sprite = null; // square, as its frame
                foreach (var (at, size) in new[] { (new Vector2(0f, 19f), new Vector2(40f, 2f)), (new Vector2(0f, -19f), new Vector2(40f, 2f)),
                                                   (new Vector2(-19f, 0f), new Vector2(2f, 40f)), (new Vector2(19f, 0f), new Vector2(2f, 40f)) })
                    UiKit.Panel(box.transform, at, size, edge, edge, 0f);
                RawImage icon = UiKit.Picture(box.transform, null, new Vector2(0f, 2f), 34f);
                Slanted badge = UiKit.Panel(box.transform, new Vector2(10f, -15f), new Vector2(30f, 16f), UiKit.Gold, UiKit.Gold, 0.25f);
                badge.sprite = null;
                Text lv = UiKit.Label(badge.transform, UiKit.Display, 14, UiKit.Navy, TextAnchor.MiddleCenter, new Vector2(0f, 0.5f), new Vector2(30f, 16f));
                lv.fontStyle = FontStyle.Normal;
                lv.verticalOverflow = VerticalWrapMode.Overflow; // the display font's line is taller than the tag
                badge.gameObject.SetActive(false);
                row.Add((icon, lv));
            }
        }
        var line = new Color(1f, 1f, 1f, 0.22f);
        Boxes(weapons, 14, -206f, line);
        Boxes(passives, TrainSim.Passives.Length, -250f, line);

        // ---- hero, top right: the weapon in its badge, its name and level, health; the ultimate under it; the
        // stats only at a station, the controls (their buttons drawn) only at the start ----
        RectTransform right = UiKit.Group(canvas.transform, new Vector2(1f, 1f), new Vector2(-26f, -22f));
        right.localScale = Vector3.one * 1.1f;
        right.localRotation = Quaternion.Euler(0f, 0f, -1.2f);
        Slanted hero = Plate(right, new Vector2(-184f, -46f), new Vector2(368f, 92f));
        var (weaponBadge, _) = Badge(hero.transform, new Vector2(140f, 0f), 76f, UiKit.Ink.Hot);
        ammoIcon = UiKit.Picture(weaponBadge.transform, null, Vector2.zero, 58f);
        ammoText = UiKit.Label(hero.transform, UiKit.Display, 26, Color.white, TextAnchor.MiddleLeft, new Vector2(-38f, 20f), new Vector2(250f, 34f), true);
        heroBar = new Meter(hero.transform, new Vector2(-38f, -16f), new Vector2(250f, 18f), 10);
        heroText = Numbers(hero.transform, new Vector2(-38f, -16f), new Vector2(238f, 22f), 18);
        // The ultimate: its own plate under the hero's, a wide bar that blazes in every colour once full, and then
        // the button to press.
        ultimatePanel = Plate(right, new Vector2(-184f, -120f), new Vector2(368f, 46f));
        ultimateEdge = Sharp(UiKit.Panel(ultimatePanel.transform, new Vector2(-150f, 0f), new Vector2(70f, 32f), UiKit.Sky, UiKit.Sky, 0.25f), 3f);
        Text ulti = UiKit.Label(ultimateEdge.transform, UiKit.Display, 22, Color.white, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(66f, 30f), true);
        ulti.text = "ULTI";
        var rim = ulti.gameObject.AddComponent<Outline>(); // white on any of the colours it runs through
        rim.effectColor = new Color(0f, 0f, 0.05f, 0.8f);
        rim.effectDistance = new Vector2(1.5f, -1.5f);
        ultimateBar = new Meter(ultimatePanel.transform, new Vector2(18f, 0f), new Vector2(206f, 18f), 10);
        ultimateText = UiKit.Label(ultimatePanel.transform, UiKit.Display, 22, Color.white, TextAnchor.MiddleRight, new Vector2(140f, 0f), new Vector2(64f, 30f), true);
        ultimateKey = Prompt.Make(ultimatePanel.transform, new Vector2(148f, 0f), 22, Color.white, UiKit.Display);
        Slanted hintPlate = Plate(right, new Vector2(-280f, -178f), new Vector2(560f, 46f));
        hintPanel = Whole(hintPlate);
        RectTransform top = UiKit.Group(canvas.transform, new Vector2(0.5f, 1f), new Vector2(0f, -24f));
        Slanted killsPanel = Plate(top, Vector2.zero, new Vector2(230f, 46f));
        killsText = UiKit.Label(killsPanel.transform, UiKit.Display, 22, dim, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(210f, 42f), true);
        hint = UiKit.Label(hintPlate.transform, UiKit.Body, 16, dim, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(540f, 30f), true);
        controls = Prompt.Make(hintPlate.transform, Vector2.zero, 19, Color.white);

        world = UiKit.Group(canvas.transform, Vector2.zero, Vector2.zero);
        world.SetAsFirstSibling(); // world bars under the cards

        // Warning strip across the screen, and the short message that reports how an event went.
        warnBand = UiKit.Panel(canvas.transform, new Vector2(0f, -330f), new Vector2(2400f, 150f), UiKit.Gold, UiKit.Gold, 0f);
        warnBand.rectTransform.anchorMin = warnBand.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        warnBand.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -2.5f);
        UiKit.Panel(warnBand.transform, new Vector2(0f, 71f), new Vector2(2400f, 8f), UiKit.Navy, UiKit.Navy, 0f);
        UiKit.Panel(warnBand.transform, new Vector2(0f, -71f), new Vector2(2400f, 8f), UiKit.Navy, UiKit.Navy, 0f);
        warnTitle = UiKit.Label(warnBand.transform, UiKit.Display, 76, UiKit.Navy, TextAnchor.MiddleCenter, new Vector2(0f, 22f), new Vector2(1700f, 84f), true);
        warnDetail = UiKit.Label(warnBand.transform, UiKit.Body, 30, UiKit.Navy, TextAnchor.MiddleCenter, new Vector2(0f, -40f), new Vector2(1700f, 42f), true);
        warnBand.gameObject.SetActive(false);

        Slanted toastPlate = UiKit.Plate(canvas.transform, new Vector2(0f, 250f), new Vector2(1100f, 78f), 0.25f, 5f, 10f);
        toastPanel = Whole(toastPlate);
        toastPanel.rectTransform.anchorMin = toastPanel.rectTransform.anchorMax = new Vector2(0.5f, 0f);
        toastPanel.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 1.5f);
        toastText = UiKit.Label(toastPlate.transform, UiKit.Display, 42, Color.white, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(1040f, 70f), true);
        toastPanel.gameObject.SetActive(false);

        Slanted noticePlate = UiKit.Plate(canvas.transform, Vector2.zero, new Vector2(760f, 82f), 0.12f, 5f, 10f);
        ultimateNotice = Whole(noticePlate);
        ultimateNotice.gameObject.name = "UltimateReadyNotice";
        ultimateNoticeFade = ultimateNotice.gameObject.AddComponent<CanvasGroup>();
        ultimateNoticeFade.blocksRaycasts = false;
        ultimateNoticeFade.interactable = false;
        ultimateNoticeText = UiKit.Label(noticePlate.transform, UiKit.Display, 34, Color.white, TextAnchor.MiddleCenter,
            Vector2.zero, new Vector2(600f, 65f), true);
        ultimateNoticeText.enabled = false; // the line is drawn with its button
        ultimateNoticeLine = Prompt.Make(noticePlate.transform, Vector2.zero, 32, Color.white, UiKit.Display);
        foreach (float x in new[] { -330f, 330f })
        {
            var triangle = new GameObject("Warning", typeof(RectTransform), typeof(CanvasRenderer), typeof(UltimateWarning)).GetComponent<UltimateWarning>();
            triangle.transform.SetParent(noticePlate.transform, false);
            triangle.rectTransform.anchoredPosition = new Vector2(x, 0f);
            triangle.rectTransform.sizeDelta = new Vector2(46f, 44f);
            UiKit.Paint(triangle, UiKit.Ink.Hot);
            triangle.raycastTarget = false;
            Text bang = UiKit.Label(triangle.transform, UiKit.Display, 30, UiKit.Navy, TextAnchor.MiddleCenter, new Vector2(0f, -6f), new Vector2(28f, 34f));
            bang.fontStyle = FontStyle.Normal;
            bang.text = "!";
        }
        ultimateNotice.gameObject.SetActive(false);
    }

    public void UltimateReady() => ultimateNoticeUntil = Time.unscaledTime + UltimateNoticeSeconds;

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
        if (!Comfort.Flashes) return; // full-screen effects turned off (accessibility)
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
        if (Time.unscaledTime > bossUntil + 4f) Entrance(name); // just came on
        if (bossPanel == null)
        {
            Slanted plate = UiKit.Plate(canvas.transform, new Vector2(0f, -104f), new Vector2(780f, 84f), 0.1f, 4f, 9f); // under the kill count
            bossPanel = Whole(plate);
            bossPanel.rectTransform.anchorMin = bossPanel.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            bossPanel.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -1f);
            UiKit.Paint(UiKit.Panel(plate.transform, new Vector2(-6f, 39f), new Vector2(768f, 6f), Color.white, Color.white, 0.1f), UiKit.Ink.Main).sprite = null;
            bossName = UiKit.Label(plate.transform, UiKit.Display, 34, Color.white, TextAnchor.MiddleCenter, new Vector2(0f, 18f), new Vector2(740f, 38f), true);
            bossBar = new Meter(plate.transform, new Vector2(0f, -18f), new Vector2(700f, 22f), 20);
        }
        bossName.text = name;
        bossBar.Set(fraction, false);
        bossBar.colour.color = bossBar.colour.bottom = UiKit.Rose; // a boss does not go green as it is worn down
        bossUntil = Time.unscaledTime + 0.25f;
    }

    // ---- a boss's entrance: a dark band in the biome's colours across the screen, tilted, the boss's name huge on it
    // under a "BOSS!" tag; it slams in, holds while the game runs slow, and sweeps out.
    RectTransform entrance;
    Text entranceName;
    float entranceAt = -10f;
    const float EntranceSeconds = 1.9f;

    void Entrance(string name)
    {
        if (!entrance)
        {
            entrance = UiKit.Group(canvas.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, 120f));
            entrance.localRotation = Quaternion.Euler(0f, 0f, -6f);
            UiKit.Paint(UiKit.Panel(entrance, new Vector2(0f, -14f), new Vector2(3200f, 230f), Color.white, Color.white, 0f), UiKit.Ink.Main).sprite = null;
            UiKit.Panel(entrance, Vector2.zero, new Vector2(3200f, 218f), Color.white, Color.white, 0f).sprite = null;
            UiKit.Paint(UiKit.Panel(entrance, Vector2.zero, new Vector2(3200f, 200f), Color.white, Color.white, 0f), UiKit.Ink.Deep).sprite = null;
            Slanted dots = UiKit.Panel(entrance, Vector2.zero, new Vector2(3200f, 200f), Color.white, Color.white, 0f);
            dots.sprite = UiKit.Halftone();
            dots.type = Image.Type.Tiled;
            UiKit.Paint(dots, UiKit.Ink.Main, 0.18f);
            Slanted tag = UiKit.Paint(UiKit.Panel(entrance, new Vector2(-420f, 112f), new Vector2(300f, 62f), Color.white, Color.white, 0.25f), UiKit.Ink.Hot);
            tag.sprite = null;
            tag.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 4f);
            UiKit.Paint(UiKit.Label(tag.transform, UiKit.Display, 44, Color.white, TextAnchor.MiddleCenter, new Vector2(0f, 2f), new Vector2(290f, 58f), true), UiKit.Ink.Deep).text = L10n.T("boss.title");
            Text shadow = UiKit.Paint(UiKit.Label(entrance, UiKit.Display, 130, Color.white, TextAnchor.MiddleCenter, new Vector2(10f, -12f), new Vector2(1700f, 170f), true), UiKit.Ink.Main);
            entranceName = UiKit.Label(entrance, UiKit.Display, 130, Color.white, TextAnchor.MiddleCenter, new Vector2(0f, -2f), new Vector2(1700f, 170f), true);
            shadow.gameObject.AddComponent<Echo>().of = entranceName;
        }
        entranceName.text = name.ToUpperInvariant();
        entranceAt = Time.unscaledTime;
        entrance.gameObject.SetActive(true);
        train.SlowMotion(EntranceSeconds * 0.8f, 0.3f);
        train.Shake(0.4f);
        Sound.Play("newcard");
    }

    void Entering()
    {
        if (!entrance || !entrance.gameObject.activeSelf) return;
        float t = Time.unscaledTime - entranceAt;
        if (t > EntranceSeconds || train.MenuShown) { entrance.gameObject.SetActive(false); return; }
        float x = t < 0.25f ? Mathf.LerpUnclamped(-2600f, 0f, UiKit.Back(t / 0.25f)) : t < EntranceSeconds - 0.3f ? (t - 0.25f) * 40f : Mathf.Lerp((EntranceSeconds - 0.55f) * 40f, 2800f, (t - EntranceSeconds + 0.3f) / 0.3f);
        entrance.anchoredPosition = new Vector2(x, 120f);
        entranceName.transform.localScale = Vector3.one * (t < 0.35f ? Mathf.Lerp(1.5f, 1f, t / 0.35f) : 1f);
    }

    // ---- an achievement won: a band slides in from the right, its icon, "ACHIEVEMENT!", its name and the tickets
    RectTransform award;
    RawImage awardIcon;
    Text awardName, awardReward;
    float awardAt = -10f;
    readonly Queue<Achievements.Def> awards = new();
    public void Achievement(Achievements.Def def) => awards.Enqueue(def);

    void Awarding()
    {
        float t = Time.unscaledTime - awardAt;
        if (t > 3.6f && awards.Count > 0)
        {
            if (!award)
            {
                award = UiKit.Group(canvas.transform, new Vector2(1f, 1f), Vector2.zero);
                award.localRotation = Quaternion.Euler(0f, 0f, 3f);
                Slanted plate = UiKit.Plate(award, Vector2.zero, new Vector2(560f, 110f), 0.15f, 5f, 10f);
                Slanted tag = UiKit.Paint(UiKit.Panel(award, new Vector2(-150f, 58f), new Vector2(260f, 46f), Color.white, Color.white, 0.25f), UiKit.Ink.Hot);
                tag.sprite = null;
                UiKit.Paint(UiKit.Label(tag.transform, UiKit.Display, 30, Color.white, TextAnchor.MiddleCenter, new Vector2(0f, 1f), new Vector2(250f, 44f), true), UiKit.Ink.Deep).text = L10n.T("ach.title");
                awardIcon = UiKit.Picture(plate.transform, null, new Vector2(-210f, -4f), 76f);
                awardName = UiKit.Label(plate.transform, UiKit.Display, 34, Color.white, TextAnchor.MiddleLeft, new Vector2(40f, 10f), new Vector2(400f, 46f), true);
                awardReward = UiKit.Label(plate.transform, UiKit.Body, 20, Color.white, TextAnchor.MiddleLeft, new Vector2(40f, -26f), new Vector2(400f, 30f), true);
            }
            Achievements.Def def = awards.Dequeue();
            UiKit.SetIcon(awardIcon, def.icon);
            awardName.text = L10n.T("ach." + def.id).ToUpperInvariant();
            awardReward.text = L10n.T("ach.reward", def.reward);
            awardAt = Time.unscaledTime;
            t = 0f;
            Sound.Play("newcard");
        }
        if (!award) return;
        bool on = t < 3.6f && !train.MenuShown;
        award.gameObject.SetActive(on);
        if (!on) return;
        float slide = t < 0.3f ? 1f - UiKit.Back(t / 0.3f) : t > 3.2f ? (t - 3.2f) / 0.4f : 0f;
        award.anchoredPosition = new Vector2(-320f + slide * 700f, -430f); // under the hero plate and the controls line
    }

    // ---- arrows at the edge of the screen for the big threats out of sight (Combat.Threats)
    readonly List<(Vector3 at, bool boss)> threats = new();
    readonly List<RectTransform> arrows = new();
    void Arrows()
    {
        if (train.MenuShown || train.Swarming || !Combat.Instance) threats.Clear();
        else Combat.Instance.Threats(threats);
        Camera view = Camera.main;
        int used = 0;
        Vector2 size = ((RectTransform)canvas.transform).rect.size, half = size * 0.5f;
        foreach (var (at, boss) in threats)
        {
            Vector3 seen = view.WorldToViewportPoint(at);
            if (seen.x > 0f && seen.x < 1f && seen.y > 0f && seen.y < 1f) continue;
            if (used == arrows.Count)
            {
                var arrow = UiKit.Group(canvas.transform, new Vector2(0.5f, 0.5f), Vector2.zero);
                UiKit.Paint(UiKit.Panel(arrow, Vector2.zero, new Vector2(54f, 54f), Color.white, Color.white, 0f), UiKit.Ink.Deep, 0.85f).sprite = UiKit.Disc();
                Text head = UiKit.Label(arrow, UiKit.Display, 44, Color.white, TextAnchor.MiddleCenter, new Vector2(3f, 2f), new Vector2(54f, 54f));
                head.fontStyle = FontStyle.Normal;
                head.text = "▶";
                arrows.Add(arrow);
            }
            RectTransform a = arrows[used++];
            a.gameObject.SetActive(true);
            Vector2 dir = new Vector2(seen.x - 0.5f, seen.y - 0.5f);
            if (seen.z < 0f) dir = -dir;
            dir = dir.sqrMagnitude < 0.0001f ? Vector2.right : dir.normalized;
            float reach = Mathf.Min((half.x - 60f) / Mathf.Max(Mathf.Abs(dir.x), 0.001f), (half.y - 60f) / Mathf.Max(Mathf.Abs(dir.y), 0.001f));
            a.anchoredPosition = dir * reach;
            a.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
            a.GetChild(1).GetComponent<Text>().color = boss ? new Color(1f, 0.3f, 0.3f) : UiKit.Now.hot;
            a.localScale = Vector3.one * (boss ? 1.25f : 0.9f) * (1f + 0.08f * Mathf.Sin(Time.unscaledTime * 8f));
        }
        for (int i = used; i < arrows.Count; i++) if (arrows[i].gameObject.activeSelf) arrows[i].gameObject.SetActive(false);
    }

    const int BaseBoxes = 5;

    // The pad's face button for the ultimate, as a glyph: Xbox's Y is told apart from the key Y.
    string KitGlyph => train.KitButton == "Y" ? "xY" : train.KitButton;

    // The Persona plate, a little lighter for the HUD: the biome's hard shadow, a white outline, the dark plate.
    static Slanted Plate(Transform parent, Vector2 at, Vector2 size) => UiKit.Plate(parent, at, size, 0.12f, 3f, 7f);
    // The root of a plate (its shadow): what is shown, hidden, scaled.
    static Slanted Whole(Slanted plate) => UiKit.Root(plate).GetComponent<Slanted>();

    // Tighter corners than the menus' soft ones.
    static Slanted Sharp(Slanted panel, float by = 2.5f)
    {
        panel.pixelsPerUnitMultiplier = by;
        return panel;
    }

    // A cream ticket on a hard shadow (the money, the medkits); its shadow holds it, to show or hide both.
    static Slanted Ticket(Transform parent, Vector2 at, Vector2 size, Color paper)
    {
        var shadow = new Color(0f, 0f, 0.03f, 0.55f);
        Slanted under = Sharp(UiKit.Panel(parent, at + new Vector2(5f, -5f), size, shadow, shadow, 0.2f));
        return Sharp(UiKit.Panel(under.transform, new Vector2(-5f, 5f), size, paper, new Color(0.95f, 0.91f, 0.83f), 0.2f));
    }

    // A station badge: a rounded square in its colour, white inside, a number in its colour (or an icon put in it).
    static (Slanted frame, Text number) Badge(Transform parent, Vector2 at, float size, UiKit.Ink ink)
    {
        Slanted frame = Sharp(UiKit.Panel(parent, at, new Vector2(size, size), Color.white, Color.white, 0f), 2f);
        UiKit.Paint(frame, ink);
        Sharp(UiKit.Panel(frame.transform, Vector2.zero, new Vector2(size - 16f, size - 16f), Color.white, new Color(0.88f, 0.92f, 0.98f), 0f), 3f);
        Text number = UiKit.Label(frame.transform, UiKit.Display, Mathf.RoundToInt(size * 0.58f), Color.white, TextAnchor.MiddleCenter, new Vector2(0f, 2f), new Vector2(size - 16f, size - 16f), true);
        UiKit.Paint(number, ink);
        number.fontStyle = FontStyle.Normal;
        return (frame, number);
    }

    // The numbers on a bar, white on a dark rim, at its right end.
    static Text Numbers(Transform parent, Vector2 at, Vector2 box, int size)
    {
        Text text = UiKit.Label(parent, UiKit.Display, size, Color.white, TextAnchor.MiddleRight, at, box, true);
        var rim = text.gameObject.AddComponent<Outline>();
        rim.effectColor = new Color(0f, 0f, 0.05f, 0.9f);
        rim.effectDistance = new Vector2(1.5f, -1.5f);
        return text;
    }

    // The level on a box's tag; no level, no tag.
    static void Level((RawImage icon, Text level) box, string text)
    {
        box.level.text = text;
        GameObject tag = box.level.transform.parent.gameObject;
        if (tag.activeSelf != (text != "")) tag.SetActive(text != "");
    }

    // A box on screen (slot), with or without a symbol in it (shown).
    static void Show((RawImage icon, Text level) box, bool shown, bool slot)
    {
        GameObject frame = box.icon.transform.parent.gameObject;
        if (frame.activeSelf != slot) frame.SetActive(slot);
        if (box.icon.enabled != shown) box.icon.enabled = shown;
        if (!shown) Level(box, "");
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
        if (!Comfort.Numbers && caption == null) return; // turned off (accessibility); the payouts still show
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
            Text text = UiKit.Label(popupCanvas.transform, UiKit.Display, 50, Color.white, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(200f, 70f), true);
            text.rectTransform.anchorMin = text.rectTransform.anchorMax = Vector2.zero; // placed in screen points
            var outline = text.gameObject.AddComponent<Outline>(); // keeps it readable over any ground
            outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
            outline.effectDistance = new Vector2(2.5f, -2.5f);
            popup = new Popup { text = text };
        }
        popup.text.text = caption ?? Mathf.Max(1, Mathf.RoundToInt(amount)).ToString() + (critical ? "!" : "");
        // An ordinary blow small and white, so the crowd stays in sight; a critical big and orange, ending in "!".
        popup.text.color = tint ?? (critical ? new Color(1f, 0.55f, 0.1f) : onTrain ? UiKit.Gold : new Color(1f, 0.97f, 0.9f));
        popup.text.fontSize = critical ? 62 : caption != null ? 50 : onTrain ? 40 : 30;
        popup.at = at;
        popup.amount = amount;
        popup.onTrain = onTrain;
        popup.critical = critical;
        popup.delivery = caption != null;
        popup.birth = now;
        popup.drift = Random.Range(-1f, 1f);
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
            foreach (var a in arsenal) if (a.weapon == r.weapon) level = Mathf.Max(level, a.level);
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
        var shade = UiKit.Now.deep; // solid: the pause menu stays hidden under it
        var dim = new Color(0.8f, 0.87f, 1f, 0.72f);
        UiKit.Panel(root, Vector2.zero, new Vector2(2600f, 1500f), shade, shade, 0f);
        RectTransform head = UiKit.Group(root, new Vector2(0.5f, 0.5f), new Vector2(0f, 482f));
        head.localRotation = Quaternion.Euler(0f, 0f, 1.5f);
        UiKit.Paint(UiKit.Panel(head, Vector2.zero, new Vector2(900f, 80f), Color.white, Color.white, 0.25f), UiKit.Ink.Main).sprite = null;
        UiKit.Label(head, UiKit.Display, 58, Color.white, TextAnchor.MiddleCenter, new Vector2(0f, 2f), new Vector2(880f, 76f), true).text = L10n.T("evo.title").ToUpperInvariant();
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
            Text name = UiKit.Label(line, UiKit.Display, 28, Color.white, TextAnchor.MiddleLeft, new Vector2(0f, 10f), new Vector2(420f, 34f), true);
            name.text = weapon.EvolvedName;
            UiKit.Label(line, UiKit.Body, 17, dim, TextAnchor.MiddleLeft, new Vector2(0f, -18f), new Vector2(420f, 24f), true).text =
                L10n.T(hero ? "evo.hero" : "evo.wagon", weapon.Name, passive >= 0 ? L10n.T("passive." + weapon.evolvesWith) : "");
            recipes.Add((weapon, gun, partner, name));
        }
        UiKit.Label(root, UiKit.Body, 20, dim, TextAnchor.MiddleCenter, new Vector2(0f, -440f), new Vector2(1600f, 30f), true).text = L10n.T("evo.help");
    }

    void LateUpdate()
    {
        canvas.enabled = !train.Racing; // the race has a board of its own
        if (train.UltimatePresenting) return;
        Evolutions();
        if (!Camera.main) return; // between scenes: no camera to place anything with
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

        if (!train.UltimateReady) ultimateNoticeUntil = 0f;
        bool notice = Time.unscaledTime < ultimateNoticeUntil && !train.Paused && !train.MenuShown && !train.Alerting;
        ultimateNotice.gameObject.SetActive(notice);
        if (notice)
        {
            float left = ultimateNoticeUntil - Time.unscaledTime;
            float age = UltimateNoticeSeconds - left;
            ultimateNoticeLine.Set(L10n.T("ultimate.ready", "[" + KitGlyph + "]"));
            ultimateNoticeFade.alpha = Mathf.Min(Mathf.Clamp01(age / 0.15f), Mathf.Clamp01(left / 0.25f));
            ultimateNotice.rectTransform.localScale = Vector3.one * (1f + 0.025f * Mathf.Sin(age * 8f));
        }

        Entering();
        Awarding();
        Arrows();
        bool boss = Time.unscaledTime < bossUntil && !train.MenuShown;
        if (bossPanel != null && bossPanel.gameObject.activeSelf != boss) bossPanel.gameObject.SetActive(boss);

        // Wagon numbers, to match the cards to the train while placing a weapon or a cargo wagon.
        int tags = train.ShowCarNumbers ? train.CarCount : 0;
        while (numbers.Count < tags)
        {
            Slanted pill = UiKit.Panel(world, Vector2.zero, new Vector2(64f, 56f), UiKit.Navy, UiKit.Navy, 0.1f);
            numbers.Add(UiKit.Label(pill.transform, UiKit.Display, 42, Color.white, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(58f, 52f), true));
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
            popup.text.rectTransform.anchoredPosition = new Vector2(screen.x, screen.y) / canvas.scaleFactor + new Vector2(popup.drift * 46f * k, 0f);
            // born big, it drops past its size and springs back (a bounce), then holds
            float pop = k < 0.1f ? Mathf.Lerp(2f, 0.85f, k / 0.1f) : k < 0.22f ? Mathf.Lerp(0.85f, 1.08f, (k - 0.1f) / 0.12f) : Mathf.Lerp(1.08f, 1f, Mathf.Clamp01((k - 0.22f) / 0.2f));
            popup.text.rectTransform.localScale = Vector3.one * pop * (popup.critical ? 1.2f : 1f);
            popup.text.rectTransform.localRotation = Quaternion.Euler(0f, 0f, popup.critical ? popup.drift * -12f : 0f);
            Color colour = popup.text.color;
            colour.a = 1f - Mathf.Clamp01((k - 0.55f) / 0.45f);
            popup.text.color = colour;
        }

        Player hero = train.Player;
        float health = train.Health, max = train.MaxHealth;
        // The badge: the station (the chapter, in the story); the sign: the biome; the tag: the difficulty.
        stationNumber.text = (Story.Active ? Story.Chapter + 1 : train.AtStation ? train.Stations : train.Stations + 1).ToString("00"); // on the way: the station ahead
        where.text = train.BiomeLabel.ToUpperInvariant();
        level.text = L10n.T("hud.level", train.Difficulty).ToUpperInvariant();
        trainBar.Set(health / max);
        trainText.text = $"{Mathf.Round(health)}/{Mathf.Round(max)}";
        money.text = train.money.ToString();
        leg.text = Story.Active ? train.StoryTime + "  ·  " + train.LegLabel : train.LegLabel;
        tripBar.Set(train.LegProgress, false);
        tripMarker.anchoredPosition = new Vector2(-110f + 274f * Mathf.Clamp01(train.LegProgress), -20f + (train.AtStation ? 0f : Mathf.Abs(Mathf.Sin(Time.time * 9f)) * 1.5f));
        kits.text = $"{train.Kits}  ·  {train.KitButton}";
        killsText.supportRichText = true;
        killsText.text = string.Format(L10n.T("hud.kills").ToUpperInvariant(), "<size=34><color=#ffffff>" + train.Kills + "</color></size>");
        // The ultimate: sky blue while it fills; once full, the bar runs through every colour, the panel throbs and
        // shows its button (dimmed while an alert holds it back).
        ultimateBar.Set(train.UltimateFill, false);
        bool ready = train.UltimateReady;
        float now = Time.unscaledTime;
        if (ready)
        {
            ultimateBar.colour.color = Color.HSVToRGB(Mathf.Repeat(now * 0.6f, 1f), 0.75f, 1f);
            ultimateBar.colour.bottom = Color.HSVToRGB(Mathf.Repeat(now * 0.6f + 0.35f, 1f), 0.75f, 1f);
            ultimateEdge.color = ultimateEdge.bottom = ultimateBar.colour.color;
            Whole(ultimatePanel).rectTransform.localScale = Vector3.one * (1f + 0.04f * Mathf.Sin(now * 8f));
            ultimateText.text = "";
            ultimateKey.gameObject.SetActive(!train.Alerting); // held back while an alert is up
            ultimateKey.Set("[" + KitGlyph + "]");
        }
        else
        {
            ultimateBar.colour.color = ultimateBar.colour.bottom = UiKit.Now.main;
            ultimateEdge.color = ultimateEdge.bottom = UiKit.Now.main;
            Whole(ultimatePanel).rectTransform.localScale = Vector3.one;
            ultimateText.text = Mathf.RoundToInt(train.UltimateFill * 100f) + "%";
            ultimateText.color = Color.white;
            ultimateKey.gameObject.SetActive(false);
        }

        heroBar.Set(hero.Health / hero.maxHealth);
        heroText.text = hero.Alive ? $"{hero.Health:0}/{hero.maxHealth:0}" : "0";
        UiKit.SetIcon(ammoIcon, hero.ammo.id);
        ammoText.text = (!hero.Alive ? L10n.T("player.down") : hero.level >= Weapon.Evolved ? L10n.T("player.evo", hero.ammo.EvolvedName) : L10n.T("player.slot", hero.ammo.Name, hero.level)).ToUpperInvariant();
        // The weapons on board, then those waiting for a wagon (faded).
        var arsenal = new List<(Weapon weapon, int level)>(train.Arsenal());
        int onBoard = arsenal.Count;
        arsenal.AddRange(train.Waiting);
        for (int i = 0; i < weapons.Count; i++)
        {
            bool shown = i < arsenal.Count;
            Show(weapons[i], shown, i < Mathf.Max(BaseBoxes, arsenal.Count));
            if (!shown) continue;
            UiKit.SetIcon(weapons[i].icon, arsenal[i].weapon.id);
            weapons[i].icon.color = new Color(1f, 1f, 1f, i < onBoard ? 1f : 0.4f);
            int lv = arsenal[i].level;
            Level(weapons[i], lv >= Weapon.Evolved ? "EVO" : lv >= Weapon.MaxLevel ? "MAX" : lv.ToString());
        }
        for (int i = 0; i < passives.Count; i++)
        {
            bool shown = i < train.passiveOrder.Count;
            Show(passives[i], shown, i < Mathf.Max(BaseBoxes, train.passiveOrder.Count));
            if (!shown) continue;
            string id = train.passiveOrder[i];
            UiKit.SetIcon(passives[i].icon, TrainSim.PassiveIcons[System.Array.IndexOf(TrainSim.Passives, id)]);
            int lv = train.PassiveLevel(id);
            Level(passives[i], lv >= TrainSim.PassiveCap(id) ? "MAX" : lv.ToString());
        }
        // One quiet line under the hero: the stats while shopping, the controls for the first stretch of a run.
        hint.text = train.AtStation ? L10n.T("hud.hero.stats", Mathf.RoundToInt(hero.damageBonus * 100f), hero.speed.ToString("0.#"),
                Mathf.RoundToInt(hero.armor * 100f), Mathf.RoundToInt(hero.moneyBonus * 100f)) : "";
        bool showControls = !train.AtStation && train.Stations == 0 && train.LegProgress < 0.3f;
        if (controls.gameObject.activeSelf != showControls) controls.gameObject.SetActive(showControls);
        if (showControls) controls.Set(L10n.T(hero.UsingGamepad ? "player.help.gamepad" : "player.help.keyboard"));
        bool hinted = hint.text != "" || showControls;
        if (hintPanel.gameObject.activeSelf != hinted) hintPanel.gameObject.SetActive(hinted);

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
