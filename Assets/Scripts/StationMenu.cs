using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// A UI image leaning to the right, optionally fading from one colour at the top to another at the bottom.
public class Slanted : Image
{
    public float slant = 0.12f; // sideways shift per unit of height
    public bool fade;
    public Color bottom = Color.white;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        base.OnPopulateMesh(vh);
        Rect r = rectTransform.rect;
        UIVertex v = default;
        for (int i = 0; i < vh.currentVertCount; i++)
        {
            vh.PopulateUIVertex(ref v, i);
            float t = Mathf.InverseLerp(r.yMin, r.yMax, v.position.y);
            v.position.x += (t - 0.5f) * r.height * slant;
            if (fade) v.color = Color.Lerp(bottom, color, t);
            vh.SetUIVertex(v, i);
        }
    }
}

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
    static Sprite Rounded()
    {
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
        public bool enabled, row;
    }

    public bool Visible => canvas.gameObject.activeSelf;

    Canvas canvas;
    RectTransform row, ribbon, listRoot;
    GameObject bar, dim;
    Slanted listCard;
    Text title, hint, listTitle, listHint;
    readonly List<Card> cards = new();
    string shown; // what the cards on screen were built from
    float age;    // seconds since they were built, for the entrance

    void Awake()
    {
        canvas = UiKit.NewCanvas("StationMenu", 10);

        // Title ribbon: a slanted white strip with a blue edge.
        ribbon = UiKit.Panel(canvas.transform, new Vector2(0f, -310f), new Vector2(1100f, 96f), UiKit.White, UiKit.Ice, 0.25f).rectTransform;
        ribbon.anchorMin = ribbon.anchorMax = new Vector2(0.5f, 1f);
        UiKit.Panel(ribbon, new Vector2(-530f, 0f), new Vector2(26f, 96f), UiKit.Sky, UiKit.Sky, 0.25f);
        title = UiKit.Label(ribbon, UiKit.Heading, 48, UiKit.Navy, TextAnchor.MiddleCenter, new Vector2(0f, 2f), new Vector2(1010f, 86f), true);

        row = UiKit.Group(canvas.transform, new Vector2(0.5f, 0f), new Vector2(0f, 395f));

        Slanted strip = UiKit.Panel(canvas.transform, new Vector2(0f, 60f), new Vector2(1160f, 54f), new Color(UiKit.Navy.r, UiKit.Navy.g, UiKit.Navy.b, 0.88f), UiKit.Navy, 0.25f);
        strip.rectTransform.anchorMin = strip.rectTransform.anchorMax = new Vector2(0.5f, 0f);
        bar = strip.gameObject;
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
    public void Render(IList<TrainSim.Option> options, string heading, int focus, string help, System.Action<int> click, System.Action<int> hover, bool list = false)
    {
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
        bar.SetActive(!list);
        dim.SetActive(list);
        listRoot.gameObject.SetActive(list);

        string signature = list ? "list" : "";
        foreach (TrainSim.Option o in options) signature += "|" + o.icon + o.label + o.enabled;
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
            card.root.anchoredPosition = new Vector2(card.root.anchoredPosition.x, (1f - arrive) * -70f + (focused ? 14f : 0f));
            card.group.alpha = arrive * (card.enabled ? 1f : 0.45f);
            card.frame.color = focused ? UiKit.Gold : new Color(1f, 1f, 1f, 0f);
            card.accent.color = focused ? UiKit.Gold : UiKit.Sky;
        }
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

        // Cards narrow down when there are many, so up to seven still fit across the screen.
        const float height = 450f, gap = 30f;
        float width = Mathf.Min(380f, (1800f - gap * (options.Count - 1)) / options.Count), total = options.Count * width + (options.Count - 1) * gap;
        float iconSize = Mathf.Min(200f, width - 50f);
        for (int i = 0; i < options.Count; i++)
        {
            TrainSim.Option option = options[i];
            var card = new Card { enabled = option.enabled };
            card.root = new GameObject("Card", typeof(RectTransform), typeof(CanvasGroup)).GetComponent<RectTransform>();
            card.root.SetParent(row, false);
            card.root.sizeDelta = new Vector2(width, height);
            card.root.anchoredPosition = new Vector2(-total * 0.5f + width * 0.5f + i * (width + gap), 0f);
            card.group = card.root.GetComponent<CanvasGroup>();

            var shade = new Color(0.05f, 0.1f, 0.25f, 0.35f);
            UiKit.Panel(card.root, new Vector2(10f, -14f), new Vector2(width, height), shade, shade);                                  // shadow
            card.frame = UiKit.Panel(card.root, Vector2.zero, new Vector2(width + 18f, height + 18f), UiKit.Gold, UiKit.Gold);       // focus rim
            Slanted face = UiKit.Panel(card.root, Vector2.zero, new Vector2(width, height), UiKit.White, UiKit.Ice);
            face.raycastTarget = true;
            card.accent = UiKit.Panel(face.transform, new Vector2(height * 0.5f * 0.12f - 7f, height * 0.5f - 12f), new Vector2(width - 40f, 12f), UiKit.Sky, UiKit.Sky);
            UiKit.Picture(face.transform, option.icon, new Vector2(14f, 95f), iconSize);

            // First line of the label is the name, the rest is the description.
            int split = option.label.IndexOf('\n');
            string name = split < 0 ? option.label : option.label.Substring(0, split), text = split < 0 ? "" : option.label.Substring(split + 1);
            UiKit.Label(face.transform, UiKit.Heading, 38, UiKit.Navy, TextAnchor.MiddleCenter, new Vector2(-6f, -42f), new Vector2(width - 36f, 56f), true).text = name;
            UiKit.Panel(face.transform, new Vector2(-10f, -78f), new Vector2(Mathf.Min(120f, width * 0.4f), 5f), UiKit.Sky, UiKit.Sky);
            UiKit.Label(face.transform, UiKit.Body, width < 300f ? 22 : 26, UiKit.Slate, TextAnchor.UpperCenter, new Vector2(-18f, -150f), new Vector2(width - 50f, 124f), true).text = text;

            var clicks = face.gameObject.AddComponent<Clicks>();
            clicks.index = i;
            clicks.click = option.enabled ? click : null;
            clicks.hover = option.enabled ? hover : null;
            cards.Add(card);
        }
    }
}
