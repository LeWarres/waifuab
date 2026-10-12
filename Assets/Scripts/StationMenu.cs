using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// Reports pointer clicks and hovers; no Selectable, so the UI module's own navigation stays out of it.
public class Clicks : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler
{
    public int index;
    public System.Action<int> click, hover;
    public void OnPointerClick(PointerEventData e) => click?.Invoke(index);
    public void OnPointerEnter(PointerEventData e) => hover?.Invoke(index);
}

// The shared look of the canvas UI: colours, fonts and the few building blocks every screen is made of.
// Everything is built in code; the only assets are the fonts in Resources/Fonts and the icons in Resources/Icons.
public static class UiKit
{
    public static readonly Color Navy = new Color(0.15f, 0.2f, 0.33f), Sky = new Color(0.25f, 0.64f, 1f), Gold = new Color(1f, 0.8f, 0.1f),
                                 Ice = new Color(0.86f, 0.94f, 1f), Slate = new Color(0.36f, 0.43f, 0.56f),
                                 Mint = new Color(0.3f, 0.85f, 0.5f), Rose = new Color(1f, 0.4f, 0.45f), White = new Color(1f, 1f, 1f, 0.97f);

    static Font body, heading;
    static Sprite rounded;

    // "ui" is the text font, "ui_title" the heavier one for headings; a system font stands in if they are missing.
    public static Font Body => body ? body : body = Resources.Load<Font>("Fonts/ui")
        ?? Font.CreateDynamicFontFromOSFont(new[] { "M PLUS Rounded 1c", "Yu Gothic UI", "Segoe UI", "Arial" }, 32);
    public static Font Heading => heading ? heading : heading = Resources.Load<Font>("Fonts/ui_title") ?? Body;

    public static Canvas NewCanvas(string name, int order)
    {
        if (!Object.FindAnyObjectByType<EventSystem>()) new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        var canvas = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)).GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = order;
        var scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 1f;
        return canvas;
    }

    public static RectTransform Group(Transform parent, Vector2 anchor, Vector2 position)
    {
        var rect = new GameObject("Group", typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = Vector2.zero;
        return rect;
    }

    public static Slanted Panel(Transform parent, Vector2 position, Vector2 size, Color top, Color bottom, float slant = 0.12f)
    {
        var image = new GameObject("Panel", typeof(RectTransform), typeof(Slanted)).GetComponent<Slanted>();
        image.transform.SetParent(parent, false);
        image.sprite = rounded ? rounded : rounded = Rounded();
        image.type = Image.Type.Sliced;
        image.color = top;
        image.bottom = bottom;
        image.fade = top != bottom;
        image.slant = slant;
        image.raycastTarget = false;
        image.rectTransform.sizeDelta = size;
        image.rectTransform.anchoredPosition = position;
        return image;
    }

    // fit: shrink the text to its box instead of cutting or wrapping it.
    public static Text Label(Transform parent, Font font, int size, Color color, TextAnchor anchor, Vector2 position, Vector2 box, bool fit = false)
    {
        var text = new GameObject("Text", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
        text.transform.SetParent(parent, false);
        text.font = font;
        text.fontSize = size;
        text.fontStyle = FontStyle.Italic; // the fonts are already bold; Unity adds the lean
        text.color = color;
        text.alignment = anchor;
        text.raycastTarget = false;
        if (fit)
        {
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Mathf.Max(12, size / 2);
            text.resizeTextMaxSize = size;
        }
        text.rectTransform.sizeDelta = box;
        text.rectTransform.anchoredPosition = position;
        return text;
    }

    public static RawImage Picture(Transform parent, string icon, Vector2 position, float size)
    {
        var picture = new GameObject("Icon", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
        picture.transform.SetParent(parent, false);
        picture.raycastTarget = false;
        picture.rectTransform.sizeDelta = new Vector2(size, size);
        picture.rectTransform.anchoredPosition = position;
        SetIcon(picture, icon);
        return picture;
    }

    public static void SetIcon(RawImage picture, string icon)
    {
        picture.texture = Icon(icon);
        picture.enabled = picture.texture;
    }

    static readonly Dictionary<string, Texture2D> icons = new();

    // Icons live in Assets/Resources/Icons (made by Tools/gen_icons.py and Tools/make_icons.py), asked for by file name.
    public static Texture2D Icon(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        if (!icons.TryGetValue(name, out Texture2D icon) || !icon) icons[name] = icon = Resources.Load<Texture2D>("Icons/" + name);
        return icon;
    }

    // White rounded square with soft edges, sliced so it stretches to any panel size.
    public static Sprite Rounded()
    {
        var saved = Resources.Load<Sprite>("Menu/rounded");
        if (saved) return saved;
        const int size = 64, radius = 20;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Max(radius - x - 0.5f, x + 0.5f - (size - radius), 0f), dy = Mathf.Max(radius - y - 0.5f, y + 0.5f - (size - radius), 0f);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(radius - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f)));
            }
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, Vector4.one * (radius + 2));
    }
}

// Canvas version of the station menus (and the game-over question): light, slanted cards with an icon,
// a title and a line of text. The pause menu uses the same options as a box in the middle with one row each. It only draws and reports clicks; TrainSim still owns the options, the
// focus and the gamepad/keyboard navigation.
public class StationMenu : MonoBehaviour
{
    class Card
    {
        public RectTransform root;
        public CanvasGroup group;
        public Slanted frame, accent;
        public Color theme;
        public float rest, top; // where it sits in the row, and how far above that its top edge is
        public bool enabled, row;
    }

    public bool Visible => canvas.gameObject.activeSelf;

    Canvas canvas;
    RectTransform row, ribbon, listRoot;
    GameObject bar, dim;
    Slanted listCard;
    Text title, sub, purse, hint, listTitle, listHint;
    GameObject steps, purseRoot;
    Slanted edge;
    const int Steps = 5;
    readonly Slanted[] stepPills = new Slanted[Steps];
    readonly Text[] stepTexts = new Text[Steps];
    readonly Slanted[] shelfTags = new Slanted[2];
    readonly Text[] shelfTexts = new Text[2];
    System.Action<int> tabPicked;
    Text arrow;        // the first-station guide: bobs over the focused card
    Slanted hintBar;
    bool lowered;
    string[] shelves;  // names of the menu's two tabs, when it has them (the market)
    // One colour per step of the station, so each menu looks like what it is about.
    static readonly Color[] Themes =
    {
        new Color(0.25f, 0.64f, 1f),   // train: blue
        new Color(0.62f, 0.45f, 0.95f), // player: violet
        new Color(1f, 0.6f, 0.2f),      // cargo: orange
        new Color(0.12f, 0.72f, 0.66f), // shop: teal
        new Color(0.36f, 0.43f, 0.56f), // route: slate
    };
    static readonly Color CargoTone = new Color(1f, 0.6f, 0.2f); // the market's top row: orange
    static readonly string[] StepKeys = { "step.train", "step.hero", "step.cargo", "step.shop", "step.route" };
    Color theme = UiKit.Sky;
    readonly List<Card> cards = new();
    string shown; // what the cards on screen were built from
    float age;    // seconds since they were built, for the entrance

    void Awake()
    {
        canvas = UiKit.NewCanvas("StationMenu", 10);

        // Title ribbon: a slanted white strip with a blue edge.
        // Where we are in the station: five steps in a row, the current one lit in its colour.
        RectTransform stepRow = UiKit.Group(canvas.transform, new Vector2(0.5f, 1f), new Vector2(0f, -205f));
        steps = stepRow.gameObject;
        for (int i = 0; i < Steps; i++)
        {
            stepPills[i] = UiKit.Panel(stepRow, new Vector2((i - (Steps - 1) * 0.5f) * 232f, 0f), new Vector2(220f, 50f), UiKit.Ice, UiKit.Ice, 0.25f);
            stepTexts[i] = UiKit.Label(stepPills[i].transform, UiKit.Heading, 26, UiKit.Navy, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(200f, 44f), true);
        }
        // Title ribbon: what this menu is, and one line saying what to do in it. The purse sits on its right
        // whenever money is spent here.
        ribbon = UiKit.Panel(canvas.transform, new Vector2(0f, -310f), new Vector2(1240f, 124f), UiKit.White, UiKit.Ice, 0.2f).rectTransform;
        ribbon.anchorMin = ribbon.anchorMax = new Vector2(0.5f, 1f);
        edge = UiKit.Panel(ribbon, new Vector2(-600f, 0f), new Vector2(26f, 124f), UiKit.Sky, UiKit.Sky, 0.2f);
        title = UiKit.Label(ribbon, UiKit.Heading, 48, UiKit.Navy, TextAnchor.MiddleCenter, new Vector2(0f, 24f), new Vector2(1100f, 62f), true);
        sub = UiKit.Label(ribbon, UiKit.Body, 27, UiKit.Slate, TextAnchor.MiddleCenter, new Vector2(0f, -30f), new Vector2(1100f, 40f), true);
        Slanted wallet = UiKit.Panel(canvas.transform, new Vector2(760f, -310f), new Vector2(230f, 84f), UiKit.Navy, UiKit.Navy, 0.2f);
        wallet.rectTransform.anchorMin = wallet.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        purseRoot = wallet.gameObject;
        UiKit.Picture(wallet.transform, "money", new Vector2(-68f, 0f), 62f);
        purse = UiKit.Label(wallet.transform, UiKit.Heading, 44, UiKit.Gold, TextAnchor.MiddleLeft, new Vector2(36f, 0f), new Vector2(140f, 60f), true);

        row = UiKit.Group(canvas.transform, new Vector2(0.5f, 0f), new Vector2(0f, 395f));

        Slanted strip = UiKit.Panel(canvas.transform, new Vector2(0f, 60f), new Vector2(1160f, 54f), new Color(UiKit.Navy.r, UiKit.Navy.g, UiKit.Navy.b, 0.88f), UiKit.Navy, 0.25f);
        strip.rectTransform.anchorMin = strip.rectTransform.anchorMax = new Vector2(0.5f, 0f);
        bar = strip.gameObject;
        hintBar = strip;
        for (int i = 0; i < 2; i++)
        {
            // The market's two tabs, side by side between the title and the cards; clicking one opens it.
            shelfTags[i] = UiKit.Panel(canvas.transform, new Vector2(i == 0 ? -270f : 270f, 676f), new Vector2(510f, 54f), UiKit.Navy, UiKit.Navy, 0.2f);
            shelfTags[i].rectTransform.anchorMin = shelfTags[i].rectTransform.anchorMax = new Vector2(0.5f, 0f);
            shelfTags[i].raycastTarget = true;
            shelfTexts[i] = UiKit.Label(shelfTags[i].transform, UiKit.Heading, 30, Color.white, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(480f, 50f), true);
            var pick = shelfTags[i].gameObject.AddComponent<Clicks>();
            pick.index = i;
            pick.click = index => tabPicked?.Invoke(index);
        }
        arrow = UiKit.Label(canvas.transform, UiKit.Heading, 90, UiKit.Gold, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(120f, 100f), true);
        arrow.rectTransform.anchorMin = arrow.rectTransform.anchorMax = new Vector2(0.5f, 0f);
        arrow.text = "▼";
        arrow.fontStyle = FontStyle.Normal;
        var rim = arrow.gameObject.AddComponent<Outline>();
        rim.effectColor = UiKit.Navy;
        rim.effectDistance = new Vector2(4f, -4f);
        hint = UiKit.Label(strip.transform, UiKit.Body, 25, Color.white, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(1120f, 50f), true);

        // List layout (pause): the game dimmed, a box in the middle, one row per option.
        var veil = new GameObject("Dim", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        veil.transform.SetParent(canvas.transform, false);
        veil.color = new Color(0.04f, 0.07f, 0.16f, 0.6f);
        veil.rectTransform.anchorMin = Vector2.zero;
        veil.rectTransform.anchorMax = Vector2.one;
        veil.rectTransform.sizeDelta = Vector2.zero;
        dim = veil.gameObject;
        listRoot = UiKit.Group(canvas.transform, new Vector2(0.5f, 0.5f), Vector2.zero);
        listCard = UiKit.Panel(listRoot, Vector2.zero, new Vector2(700f, 400f), UiKit.White, UiKit.Ice, 0.04f);
        listTitle = UiKit.Label(listRoot, UiKit.Heading, 50, UiKit.Navy, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(620f, 110f), true);
        listHint = UiKit.Label(listRoot, UiKit.Body, 22, UiKit.Slate, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(620f, 40f), true);

        canvas.gameObject.SetActive(false);
    }

    // Call every frame. options == null hides the menu. list: the pause layout instead of the cards.
    // step: which of the station's five steps this is (-1: none, no step row). note: the line under the title.
    // money: shown in a purse beside the title when it is not negative.
    public void Render(IList<TrainSim.Option> options, string heading, int focus, string help, System.Action<int> click, System.Action<int> hover, bool list = false,
        int step = -1, string note = null, int money = -1, string[] lines = null, bool guide = false,
        int tab = 0, System.Action<int> pickTab = null, bool compact = false)
    {
        // tab: which of the two tabs (lines) is open. compact: the cards drop to the foot of the screen and the
        // controls bar goes, so the train stays in view (placing a wagon).
        shelves = list ? null : lines;
        tabPicked = pickTab;
        row.anchoredPosition = new Vector2(0f, compact ? 215f : shelves != null ? 365f : 395f); // under the tabs, when there are any
        lowered = compact;
        if (options == null || options.Count == 0)
        {
            if (Visible) canvas.gameObject.SetActive(false);
            shown = null;
            return;
        }
        if (!Visible) canvas.gameObject.SetActive(true);
        title.text = listTitle.text = heading;
        hint.text = listHint.text = help;
        ribbon.gameObject.SetActive(!list);
        bar.SetActive(!list && !compact);
        steps.SetActive(!list && step >= 0);
        purseRoot.SetActive(!list && money >= 0);
        purse.text = money.ToString();
        theme = step >= 0 ? Themes[step] : UiKit.Sky;
        edge.color = edge.bottom = theme;
        bool noted = !string.IsNullOrEmpty(note);
        sub.text = noted ? note : "";
        title.rectTransform.anchoredPosition = new Vector2(0f, noted ? 24f : 2f);
        for (int i = 0; i < Steps && step >= 0; i++)
        {
            // Done steps are ticked and faded, the current one is lit, the ones to come are plain.
            stepPills[i].color = stepPills[i].bottom = i == step ? Themes[i] : i < step ? new Color(0.8f, 0.86f, 0.94f, 0.75f) : new Color(1f, 1f, 1f, 0.8f);
            stepPills[i].rectTransform.localScale = Vector3.one * (i == step ? 1.12f : 0.94f);
            stepTexts[i].text = (i < step ? "✓ " : (i + 1) + "  ") + L10n.T(StepKeys[i]);
            stepTexts[i].color = i == step ? Color.white : i < step ? UiKit.Slate : UiKit.Navy;
        }
        dim.SetActive(list);
        listRoot.gameObject.SetActive(list);

        string signature = list ? "list" : "";
        foreach (TrainSim.Option o in options) signature += "|" + o.icon + o.label + o.enabled + o.badge + o.tone + o.shelf;
        signature += "|" + step + (compact ? "c" : "");
        for (int i = 0; i < 2; i++)
        {
            shelfTags[i].gameObject.SetActive(shelves != null);
            if (shelves == null) continue;
            // The open tab is lit in its colour and bigger; the other is plain and says it can be opened.
            bool open = i == tab;
            Color paint = i == 0 ? CargoTone : Themes[3];
            shelfTexts[i].text = shelves[i];
            shelfTexts[i].color = open ? Color.white : UiKit.Navy;
            shelfTags[i].color = shelfTags[i].bottom = open ? paint : new Color(1f, 1f, 1f, 0.85f);
            shelfTags[i].rectTransform.localScale = Vector3.one * (open ? 1.06f : 0.94f);
            if (open) theme = paint; // the cards of that tab take its colour
        }
        if (signature != shown)
        {
            bool sameMenu = shown != null && shown.Split('|').Length == signature.Split('|').Length; // same cards, new texts: no entrance again
            shown = signature;
            if (list) BuildList(options, click, hover);
            else Build(options, click, hover);
            if (sameMenu) age = 10f;
        }

        // Entrance (cards rise in, one after another) and focus (the chosen card grows and turns gold).
        age += Time.unscaledDeltaTime;
        for (int i = 0; i < cards.Count; i++)
        {
            Card card = cards[i];
            float arrive = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((age - i * 0.05f) / 0.22f));
            bool focused = i == focus;
            if (card.row)
            {
                // A row: gold when focused, no rise.
                card.group.alpha = arrive;
                card.accent.color = card.accent.bottom = focused ? UiKit.Gold : UiKit.Ice;
                float grow = Mathf.Lerp(card.root.localScale.x, focused ? 1.05f : 1f, 1f - Mathf.Exp(-18f * Time.unscaledDeltaTime));
                card.root.localScale = new Vector3(grow, grow, 1f);
                continue;
            }
            float size = Mathf.Lerp(card.root.localScale.x, focused ? 1.08f : 1f, 1f - Mathf.Exp(-18f * Time.unscaledDeltaTime));
            card.root.localScale = new Vector3(size, size, 1f);
            card.root.anchoredPosition = new Vector2(card.root.anchoredPosition.x, card.rest + (1f - arrive) * -70f + (focused ? 10f : 0f));
            card.group.alpha = arrive * (card.enabled ? 1f : 0.45f);
            card.frame.color = focused ? UiKit.Gold : new Color(1f, 1f, 1f, 0f);
            card.accent.color = card.accent.bottom = focused ? UiKit.Gold : card.theme;
        }

        // First station ever: a big arrow bobs over the card in focus and the controls line glows.
        bool guiding = guide && !list && focus >= 0 && focus < cards.Count;
        if (arrow.gameObject.activeSelf != guiding) arrow.gameObject.SetActive(guiding);
        float beat = Mathf.PingPong(Time.unscaledTime * 2.4f, 1f);
        if (guiding)
        {
            Card on = cards[focus];
            arrow.rectTransform.anchoredPosition = new Vector2(on.root.anchoredPosition.x, row.anchoredPosition.y + on.rest + on.top + 96f + beat * 22f);
            arrow.transform.SetAsLastSibling();
        }
        Color calm = new Color(UiKit.Navy.r, UiKit.Navy.g, UiKit.Navy.b, 0.88f);
        hintBar.color = hintBar.bottom = guiding ? Color.Lerp(calm, new Color(0.85f, 0.55f, 0f, 0.95f), beat) : calm;
        hintBar.rectTransform.localScale = Vector3.one * (guiding ? 1.12f : 1f);
    }

    void BuildList(IList<TrainSim.Option> options, System.Action<int> click, System.Action<int> hover)
    {
        foreach (Card old in cards) Destroy(old.root.gameObject);
        cards.Clear();
        age = 0f;

        const float rowHeight = 74f, gap = 14f, top = 130f, bottom = 70f;
        float height = top + options.Count * (rowHeight + gap) + bottom;
        listCard.rectTransform.sizeDelta = new Vector2(700f, height);
        listTitle.rectTransform.anchoredPosition = new Vector2(0f, height * 0.5f - 68f);
        listHint.rectTransform.anchoredPosition = new Vector2(0f, -height * 0.5f + 42f);
        for (int i = 0; i < options.Count; i++)
        {
            TrainSim.Option option = options[i];
            var card = new Card { enabled = true, row = true };
            card.root = new GameObject("Row", typeof(RectTransform), typeof(CanvasGroup)).GetComponent<RectTransform>();
            card.root.SetParent(listRoot, false);
            card.root.sizeDelta = new Vector2(560f, rowHeight);
            card.root.anchoredPosition = new Vector2(0f, height * 0.5f - top - rowHeight * 0.5f - i * (rowHeight + gap));
            card.group = card.root.GetComponent<CanvasGroup>();
            card.frame = card.accent = UiKit.Panel(card.root, Vector2.zero, new Vector2(560f, rowHeight), UiKit.Ice, UiKit.Ice, 0.25f);
            card.accent.raycastTarget = true;
            UiKit.Picture(card.accent.transform, option.icon, new Vector2(-220f, 0f), 58f);
            UiKit.Label(card.accent.transform, UiKit.Heading, 32, UiKit.Navy, TextAnchor.MiddleCenter, new Vector2(24f, 0f), new Vector2(400f, 62f), true).text = option.label.Replace('\n', ' ');
            var clicks = card.accent.gameObject.AddComponent<Clicks>();
            clicks.index = i;
            clicks.click = click;
            clicks.hover = hover;
            cards.Add(card);
        }
    }

    void Build(IList<TrainSim.Option> options, System.Action<int> click, System.Action<int> hover)
    {
        foreach (Card old in cards) Destroy(old.root.gameObject);
        cards.Clear();
        age = 0f;

        // The offers are the big cards. The ways out (skip, done, back) are small, grey and stand apart to the
        // right, so nobody takes them for one more thing to choose from. Cards narrow down when there are many.
        // A menu with two shelves (the market) stacks two rows of shorter cards, each under its own name.
        const bool two = false;
        float height = two ? 270f : 450f, gap = two ? 22f : 30f;
        const float minorWidth = 230f, minorHeight = 300f, apart = 46f;
        var count = new int[2];
        int minors = 0;
        foreach (TrainSim.Option o in options)
            if (o.minor) minors++; else count[two ? o.shelf : 0]++;
        int widest = Mathf.Max(count[0], count[1]);
        float room = 1800f - minors * (minorWidth + gap) - (minors > 0 && widest > 0 ? apart : 0f);
        float width = widest > 0 ? Mathf.Min(two ? 280f : 380f, (room - gap * (widest - 1)) / widest) : 0f;
        float span = widest * width + Mathf.Max(0, widest - 1) * gap;
        float total = span + minors * minorWidth + Mathf.Max(0, minors - 1) * gap + (minors > 0 && widest > 0 ? gap + apart : 0f);
        float iconSize = two ? 108f : Mathf.Min(200f, width - 50f), left = -total * 0.5f, minorX = left + span + (widest > 0 ? gap + apart : 0f);
        var placed = new int[2];
        var grey = new Color(0.9f, 0.92f, 0.96f, 0.97f);
        for (int i = 0; i < options.Count; i++)
        {
            TrainSim.Option option = options[i];
            bool minor = option.minor;
            int shelf = two && !minor ? option.shelf : 0;
            float w = minor ? minorWidth : width, h = minor ? minorHeight : height, x;
            if (minor) { x = minorX; minorX += minorWidth + gap; }
            else x = left + placed[shelf]++ * (width + gap);
            Color tone = minor ? UiKit.Slate : theme;
            var card = new Card { enabled = option.enabled, theme = tone };
            // Top row above the middle, bottom row below it; a way out sits between the two, or low beside one row.
            card.rest = minor && !lowered ? -(450f - minorHeight) * 0.5f : 0f;
            card.top = h * 0.5f;
            card.root = new GameObject("Card", typeof(RectTransform), typeof(CanvasGroup)).GetComponent<RectTransform>();
            card.root.SetParent(row, false);
            card.root.sizeDelta = new Vector2(w, h);
            card.root.anchoredPosition = new Vector2(x + w * 0.5f, card.rest);
            card.group = card.root.GetComponent<CanvasGroup>();
            RectTransform body = card.root;

            var shade = new Color(0.05f, 0.1f, 0.25f, 0.35f);
            UiKit.Panel(body, new Vector2(10f, -14f), new Vector2(w, h), shade, shade);                                  // shadow
            card.frame = UiKit.Panel(body, Vector2.zero, new Vector2(w + 18f, h + 18f), UiKit.Gold, UiKit.Gold);       // focus rim
            Slanted face = UiKit.Panel(body, Vector2.zero, new Vector2(w, h), minor ? grey : UiKit.White, minor ? grey : UiKit.Ice);
            face.raycastTarget = true;
            card.accent = UiKit.Panel(face.transform, new Vector2(h * 0.5f * 0.12f - 7f, h * 0.5f - 12f), new Vector2(w - 40f, 12f), tone, tone);

            // First line of the label is the name, the rest is the description.
            int split = option.label.IndexOf('\n');
            string name = split < 0 ? option.label : option.label.Substring(0, split), text = split < 0 ? "" : option.label.Substring(split + 1);
            if (minor)
            {
                UiKit.Picture(face.transform, option.icon, new Vector2(8f, 62f), 120f);
                UiKit.Label(face.transform, UiKit.Heading, 32, UiKit.Navy, TextAnchor.MiddleCenter, new Vector2(-4f, -34f), new Vector2(w - 30f, 46f), true).text = name;
                UiKit.Label(face.transform, UiKit.Body, 21, UiKit.Slate, TextAnchor.UpperCenter, new Vector2(-12f, -100f), new Vector2(w - 36f, 70f), true).text = text;
            }
            else if (two)
            {
                UiKit.Picture(face.transform, option.icon, new Vector2(10f, 56f), iconSize);
                UiKit.Label(face.transform, UiKit.Heading, 28, UiKit.Navy, TextAnchor.MiddleCenter, new Vector2(-4f, -24f), new Vector2(w - 26f, 38f), true).text = name;
                UiKit.Label(face.transform, UiKit.Body, 19, UiKit.Slate, TextAnchor.UpperCenter, new Vector2(-12f, -84f), new Vector2(w - 34f, 70f), true).text = text;
            }
            else
            {
                UiKit.Picture(face.transform, option.icon, new Vector2(14f, 95f), iconSize);
                UiKit.Label(face.transform, UiKit.Heading, 38, UiKit.Navy, TextAnchor.MiddleCenter, new Vector2(-6f, -42f), new Vector2(w - 36f, 56f), true).text = name;
                UiKit.Panel(face.transform, new Vector2(-10f, -78f), new Vector2(Mathf.Min(120f, w * 0.4f), 5f), tone, tone);
                UiKit.Label(face.transform, UiKit.Body, w < 300f ? 22 : 26, UiKit.Slate, TextAnchor.UpperCenter, new Vector2(-18f, -150f), new Vector2(w - 50f, 124f), true).text = text;
            }
            if (!string.IsNullOrEmpty(option.badge))
            {
                // The tag on the corner: free in green, a price in gold with its coin, "cannot" in red.
                bool price = option.tone >= 2 && int.TryParse(option.badge, out _);
                Color paint = option.tone == 1 ? UiKit.Mint : option.tone == 3 ? UiKit.Rose : UiKit.Gold;
                float tagWidth = Mathf.Min(w - 30f, price ? 150f : 190f), tagHeight = two ? 46f : 54f;
                Slanted tag = UiKit.Panel(face.transform, new Vector2(w * 0.5f - tagWidth * 0.5f + 22f, h * 0.5f - 6f), new Vector2(tagWidth, tagHeight), paint, paint, 0.2f);
                if (price) UiKit.Picture(tag.transform, "money", new Vector2(-tagWidth * 0.5f + 34f, 0f), tagHeight - 10f);
                UiKit.Label(tag.transform, UiKit.Heading, two ? 28 : 32, option.tone == 2 ? UiKit.Navy : Color.white, TextAnchor.MiddleCenter,
                    new Vector2(price ? 18f : 0f, 0f), new Vector2(tagWidth - (price ? 60f : 20f), tagHeight - 8f), true).text = option.badge;
            }

            var clicks = face.gameObject.AddComponent<Clicks>();
            clicks.index = i;
            clicks.click = option.enabled ? click : null;
            clicks.hover = option.enabled ? hover : null;
            cards.Add(card);
        }
    }
}

