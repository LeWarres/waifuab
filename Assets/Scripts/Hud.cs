using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// The always-on HUD, in the canvas look: a card for the train (where it is, health, money, trip, medkits),
// a card for the hero (health, carried weapons, controls) and a small health bar over every wagon and the hero.
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
            root = UiKit.Panel(parent, position, size, new Color(0.1f, 0.14f, 0.25f, 0.55f), new Color(0.1f, 0.14f, 0.25f, 0.55f), 0.25f).rectTransform;
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
    Text status, trainText, money, leg, kits, heroName, heroText, controls, warnTitle, warnDetail, toastText;
    Slanted warnBand, toastPanel;
    float toastUntil;
    readonly List<Text> numbers = new();
    Meter trainBar, tripBar, heroBar;
    RawImage ammoIcon;
    Text ammoText, heroStats;
    readonly List<Meter> worldBars = new();
    RectTransform world;

    void Awake()
    {
        train = GetComponent<TrainSim>();
        canvas = UiKit.NewCanvas("Hud", 0);

        // ---- train card, top left ----
        RectTransform left = UiKit.Group(canvas.transform, new Vector2(0f, 1f), new Vector2(36f, -30f));
        Slanted card = UiKit.Panel(left, new Vector2(320f, -105f), new Vector2(620f, 210f), UiKit.White, UiKit.Ice, 0.08f);
        UiKit.Panel(card.transform, new Vector2(-292f, 0f), new Vector2(18f, 210f), UiKit.Sky, UiKit.Sky, 0.08f);
        status = UiKit.Label(card.transform, UiKit.Heading, 34, UiKit.Navy, TextAnchor.MiddleLeft, new Vector2(18f, 68f), new Vector2(560f, 48f), true);
        trainBar = new Meter(card.transform, new Vector2(-80f, 18f), new Vector2(370f, 36f));
        trainText = UiKit.Label(card.transform, UiKit.Body, 22, Color.white, TextAnchor.MiddleCenter, new Vector2(-80f, 18f), new Vector2(360f, 34f), true);
        UiKit.Picture(card.transform, "money", new Vector2(150f, 18f), 54f);
        money = UiKit.Label(card.transform, UiKit.Heading, 38, new Color(0.85f, 0.55f, 0f), TextAnchor.MiddleLeft, new Vector2(245f, 18f), new Vector2(120f, 48f), true);
        leg = UiKit.Label(card.transform, UiKit.Body, 22, UiKit.Slate, TextAnchor.MiddleLeft, new Vector2(-130f, -28f), new Vector2(280f, 32f), true);
        tripBar = new Meter(card.transform, new Vector2(150f, -28f), new Vector2(260f, 14f));
        tripBar.colour.color = UiKit.Sky;
        UiKit.Picture(card.transform, "medkit", new Vector2(-252f, -70f), 40f);
        kits = UiKit.Label(card.transform, UiKit.Body, 22, UiKit.Slate, TextAnchor.MiddleLeft, new Vector2(30f, -70f), new Vector2(520f, 32f), true);

        // ---- hero card, top right ----
        RectTransform right = UiKit.Group(canvas.transform, new Vector2(1f, 1f), new Vector2(-36f, -30f));
        Slanted hero = UiKit.Panel(right, new Vector2(-340f, -90f), new Vector2(660f, 180f), UiKit.White, UiKit.Ice, 0.08f);
        UiKit.Panel(hero.transform, new Vector2(312f, 0f), new Vector2(18f, 180f), UiKit.Gold, UiKit.Gold, 0.08f);
        heroName = UiKit.Label(hero.transform, UiKit.Heading, 34, UiKit.Navy, TextAnchor.MiddleLeft, new Vector2(-190f, 52f), new Vector2(230f, 46f), true);
        heroBar = new Meter(hero.transform, new Vector2(110f, 52f), new Vector2(330f, 34f));
        heroText = UiKit.Label(hero.transform, UiKit.Body, 22, Color.white, TextAnchor.MiddleCenter, new Vector2(110f, 52f), new Vector2(320f, 32f), true);
        Slanted chip = UiKit.Panel(hero.transform, new Vector2(-194f, 2f), new Vector2(214f, 46f), UiKit.Gold, UiKit.Gold, 0.25f);
        ammoIcon = UiKit.Picture(chip.transform, null, new Vector2(-80f, 0f), 40f);
        ammoText = UiKit.Label(chip.transform, UiKit.Body, 20, UiKit.Navy, TextAnchor.MiddleLeft, new Vector2(22f, 0f), new Vector2(154f, 40f), true);
        heroStats = UiKit.Label(hero.transform, UiKit.Body, 18, UiKit.Slate, TextAnchor.MiddleLeft, new Vector2(104f, 2f), new Vector2(372f, 40f), true);
        controls = UiKit.Label(hero.transform, UiKit.Body, 19, UiKit.Slate, TextAnchor.MiddleCenter, new Vector2(-6f, -52f), new Vector2(610f, 34f), true);

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

    public void Toast(string text, float seconds = 2.5f)
    {
        toastText.text = text;
        toastUntil = Time.unscaledTime + seconds;
    }

    void LateUpdate()
    {
        bool toast = Time.unscaledTime < toastUntil;
        if (toastPanel.gameObject.activeSelf != toast) toastPanel.gameObject.SetActive(toast);

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

        Player hero = train.Player;
        float health = train.Health, max = train.MaxHealth;
        status.text = L10n.T("hud.status", train.Stations, train.BiomeLabel, train.Difficulty);
        trainBar.Set(health / max);
        trainText.text = L10n.T("hud.train", Mathf.Round(health), Mathf.Round(max));
        money.text = train.money.ToString();
        leg.text = L10n.T("hud.leg", train.LegLabel);
        tripBar.Set(train.LegProgress, false);
        kits.text = L10n.T("hud.kits", train.Kits, train.KitButton);

        heroName.text = L10n.T("player.name");
        heroBar.Set(hero.Health / hero.maxHealth);
        heroText.text = hero.Alive ? $"{hero.Health:0} / {hero.maxHealth:0}" : L10n.T("player.down");
        UiKit.SetIcon(ammoIcon, hero.ammo.id);
        ammoText.text = L10n.T("player.slot", hero.ammo.Name, hero.level);
        heroStats.text = L10n.T("hud.hero.stats", Mathf.RoundToInt(hero.damageBonus * 100f), hero.speed.ToString("0.#"),
            Mathf.RoundToInt(hero.armor * 100f), Mathf.RoundToInt(hero.moneyBonus * 100f));
        controls.text = L10n.T(hero.UsingGamepad ? "player.help.gamepad" : "player.help.keyboard");

        // One small bar over every wagon, one over the hero; hidden while a menu covers the train.
        int wanted = train.MenuShown ? 0 : train.CarCount + (hero.Alive ? 1 : 0);
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
