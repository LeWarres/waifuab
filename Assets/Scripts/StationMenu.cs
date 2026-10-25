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

// A line of text with the buttons drawn in: [W] [Shift] [Space] keys, [Mouse], [△] [○] [□] [✕] PlayStation,
// [xA] [xB] [xX] [xY] Xbox, [L1] [R2] shoulders and triggers, [LS] [RS] sticks. Rebuilt only when the line changes.
public class Prompt : MonoBehaviour
{
    public int size = 24;
    public Color ink = Color.white;
    public Font font;
    string shown;

    public static Prompt Make(Transform parent, Vector2 position, int size, Color ink, Font font = null)
    {
        var made = UiKit.Group(parent, new Vector2(0.5f, 0.5f), position).gameObject.AddComponent<Prompt>();
        made.size = size;
        made.ink = ink;
        made.font = font;
        return made;
    }

    // In capitals, but for the buttons between brackets ([xA] must stay Xbox's A).
    public static string Upper(string line)
    {
        var upper = new System.Text.StringBuilder(line.Length);
        bool inside = false;
        foreach (char c in line)
        {
            if (c == '[') inside = true;
            else if (c == ']') inside = false;
            upper.Append(inside ? c : char.ToUpperInvariant(c));
        }
        return upper.ToString();
    }

    public void Set(string line)
    {
        if (line == shown) return;
        shown = line;
        foreach (Transform old in transform) Destroy(old.gameObject);
        var parts = new List<RectTransform>();
        float x = 0f;
        for (int i = 0; i < line.Length;)
        {
            int open = line.IndexOf('[', i), close = open < 0 ? -1 : line.IndexOf(']', open);
            if (close < 0) open = -1;
            string words = open < 0 ? line.Substring(i) : line.Substring(i, open - i);
            if (words.Length > 0)
            {
                Text text = UiKit.Label(transform, font ? font : UiKit.Body, size, ink, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(4000f, size * 1.5f));
                text.horizontalOverflow = HorizontalWrapMode.Overflow;
                text.text = words;
                text.rectTransform.sizeDelta = new Vector2(text.preferredWidth + 4f, size * 1.5f);
                parts.Add(text.rectTransform);
            }
            if (open < 0) break;
            parts.Add(UiKit.Glyph(transform, line.Substring(open + 1, close - open - 1), size * 1.35f));
            i = close + 1;
        }
        foreach (RectTransform part in parts) x += part.sizeDelta.x + 4f;
        float at = -x * 0.5f;
        foreach (RectTransform part in parts)
        {
            part.anchoredPosition = new Vector2(at + part.sizeDelta.x * 0.5f, 0f);
            at += part.sizeDelta.x + 4f;
        }
    }
}

// The shadow and outline behind a panel made Persona (UiKit.Persona): each keeps to the panel's place, size and
// turn, and shows only while the panel does. They do it themselves, every frame, so they also notice when the
// panel is switched off (its own scripts stop then).
public class Backed : MonoBehaviour
{
    public RectTransform shadow, rim;
    public float outline, offset;

    void Awake() => Sync(); // panels saved with a scene get their followers too

    public void Sync()
    {
        Follow(shadow, new Vector2(offset, -offset));
        Follow(rim, Vector2.zero);
    }

    void Follow(RectTransform part, Vector2 by)
    {
        if (!part) return;
        var follower = part.GetComponent<BackedPart>() ?? part.gameObject.AddComponent<BackedPart>();
        follower.owner = (RectTransform)transform;
        follower.by = by;
        follower.outline = outline;
        follower.Copy();
    }
}

public class BackedPart : MonoBehaviour
{
    public RectTransform owner;
    public Vector2 by;
    public float outline;
    Graphic look;

    void LateUpdate()
    {
        if (!owner) { Destroy(gameObject); return; }
        Copy();
    }

    public void Copy()
    {
        if (!look) look = GetComponent<Graphic>();
        look.enabled = owner.gameObject.activeInHierarchy;
        var to = (RectTransform)transform;
        var me = owner;
        to.anchorMin = me.anchorMin;
        to.anchorMax = me.anchorMax;
        to.pivot = me.pivot;
        to.localRotation = me.localRotation;
        to.localScale = me.localScale;
        to.anchoredPosition = me.anchoredPosition + (Vector2)(me.localRotation * by);
        to.sizeDelta = me.sizeDelta + Vector2.one * outline * 2f;
    }
}

// Keeps a graphic in the biome's colours (UiKit.Paint), repainting it when the biome changes.
public class Themed : MonoBehaviour
{
    public UiKit.Ink ink;
    public float alpha = 1f;
    int seen = -1;

    void LateUpdate()
    {
        if (seen != UiKit.ToneVersion) Apply();
    }

    public void Apply()
    {
        seen = UiKit.ToneVersion;
        UiKit.Tones tone = UiKit.Now;
        Color top = ink switch { UiKit.Ink.Main => tone.main, UiKit.Ink.Hot => tone.hot, UiKit.Ink.Deep => tone.deep, _ => tone.deep * 1.7f + new Color(0.03f, 0.03f, 0.05f) };
        Color bottom = ink == UiKit.Ink.Plate ? tone.deep : top;
        top.a = bottom.a = alpha;
        var graphic = GetComponent<Graphic>();
        graphic.color = top;
        if (graphic is Slanted slanted)
        {
            slanted.bottom = bottom;
            slanted.fade = top != bottom;
            slanted.SetVerticesDirty();
        }
    }
}

// The shared look of the canvas UI: colours, fonts and the few building blocks every screen is made of.
// Everything is built in code; the only assets are the fonts in Resources/Fonts and the icons in Resources/Icons.
public static class UiKit
{
    public static readonly Color Navy = new Color(0.15f, 0.2f, 0.33f), Sky = new Color(0.25f, 0.64f, 1f), Gold = new Color(1f, 0.8f, 0.1f),
                                 Ice = new Color(0.86f, 0.94f, 1f), Slate = new Color(0.36f, 0.43f, 0.56f),
                                 Mint = new Color(0.3f, 0.85f, 0.5f), Rose = new Color(1f, 0.4f, 0.45f), White = new Color(1f, 1f, 1f, 0.97f);

    static Font body, heading, display;
    static Sprite rounded;

    // "ui" is the text font, "ui_title" the heavier one for headings; a system font stands in if they are missing.
    public static Font Body => body ? body : body = Resources.Load<Font>("Fonts/ui")
        ?? Font.CreateDynamicFontFromOSFont(new[] { "M PLUS Rounded 1c", "Yu Gothic UI", "Segoe UI", "Arial" }, 32);
    public static Font Heading => heading ? heading : heading = Resources.Load<Font>("Fonts/ui_title") ?? Body;
    // "display" (Anton) for the big words and numbers; ui_title stands in for the letters it lacks (Japanese).
    public static Font Display => display ? display : display = Resources.Load<Font>("Fonts/display") ?? Heading;

    public static Canvas NewCanvas(string name, int order)
    {
        if (!Object.FindAnyObjectByType<EventSystem>()) new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        var canvas = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)).GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = order;
        var scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f) / Comfort.Scale; // the interface's size (accessibility)
        scaler.matchWidthOrHeight = 1f;
        scalers.Add(scaler);
        return canvas;
    }

    // Every canvas made here, resized when the interface's size setting changes.
    static readonly List<CanvasScaler> scalers = new();
    public static void Rescale()
    {
        scalers.RemoveAll(s => !s);
        foreach (CanvasScaler s in scalers) s.referenceResolution = new Vector2(1920f, 1080f) / Comfort.Scale;
        foreach (CanvasScaler s in Object.FindObjectsByType<CanvasScaler>(FindObjectsSortMode.None)) // the ones saved with a scene too
            if (s.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize) s.referenceResolution = new Vector2(1920f, 1080f) / Comfort.Scale;
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
        if (font == display && !fit) text.verticalOverflow = VerticalWrapMode.Overflow; // its lines are taller than its letters: never cut them off (best fit shrinks them to the box instead)
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

    // The train look: a ticket (rounded, a notch either side where the stub tears off along a perforation, a
    // thin inner border), drawn white at its own size so it can be tinted; stub: the stub's height, 0 for none.
    static readonly Dictionary<Vector3Int, Sprite> tickets = new();
    public static Sprite Ticket(float width, float height, float stub)
    {
        var key = new Vector3Int(Mathf.RoundToInt(width), Mathf.RoundToInt(height), Mathf.RoundToInt(stub));
        if (tickets.TryGetValue(key, out Sprite made) && made) return made;
        int w = key.x, h = key.y, cut = key.z;
        const float corner = 18f, notch = 15f, inset = 11f;
        var texture = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color32[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float cx = x + 0.5f, cy = y + 0.5f;
                float dx = Mathf.Max(corner - cx, cx - (w - corner), 0f), dy = Mathf.Max(corner - cy, cy - (h - corner), 0f);
                float alpha = Mathf.Clamp01(corner - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f);
                byte grey = 255;
                if (cut > 0)
                {
                    float side = Mathf.Min(cx, w - cx), up = cy - cut;
                    alpha = Mathf.Min(alpha, Mathf.Clamp01(Mathf.Sqrt(side * side + up * up) - notch + 0.5f)); // the notches
                    if (Mathf.Abs(up) < 1.5f && cx > notch + 6f && cx < w - notch - 6f && (x / 9) % 2 == 0) grey = 175; // the perforation
                }
                bool border = Mathf.Abs(Mathf.Min(Mathf.Min(cx, w - cx), Mathf.Min(cy, h - cy)) - inset) < 1f && dx <= 0f && dy <= 0f;
                if (border && (cut == 0 || Mathf.Abs(cy - cut) > notch + 4f)) grey = 215;
                px[y * w + x] = new Color32(grey, grey, grey, (byte)(alpha * 255f));
            }
        texture.SetPixels32(px);
        texture.Apply();
        return tickets[key] = Sprite.Create(texture, new Rect(0f, 0f, w, h), new Vector2(0.5f, 0.5f), 100f);
    }

    // A white disc (the metro line's stops) and a dot for halftone, tiled.
    static Sprite disc, dot;
    public static Sprite Disc() => disc ? disc : disc = Round(64, 31f);
    public static Sprite Halftone() => dot ? dot : dot = Round(26, 6f);
    static Sprite Round(int size, float radius)
    {
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = new Vector2(x + 0.5f - size * 0.5f, y + 0.5f - size * 0.5f).magnitude;
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(radius - d + 0.5f)));
            }
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    // A ticket's barcode: black bars of random widths.
    static Texture2D barcode;
    public static Texture2D Barcode()
    {
        if (barcode) return barcode;
        const int w = 160, h = 8;
        barcode = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Point };
        var rng = new System.Random(3);
        var px = new Color32[w * h];
        for (int x = 0; x < w;)
        {
            int bar = rng.Next(1, 4), space = rng.Next(1, 3);
            for (int b = 0; b < bar + space && x < w; b++, x++)
                for (int y = 0; y < h; y++) px[y * w + x] = b < bar ? new Color32(30, 32, 44, 255) : new Color32(0, 0, 0, 0);
        }
        barcode.SetPixels32(px);
        barcode.Apply();
        return barcode;
    }

    // Each biome's colours for the big moments (the delivery report, the button games): main for the big shapes,
    // deep for the plates and the dark behind, hot to light what matters now.
    public struct Tones { public Color main, deep, hot; }
    public static Tones Biome(string id) => id switch
    {
        "snow" => new Tones { main = new(0.35f, 0.8f, 1f), deep = new(0.04f, 0.08f, 0.18f), hot = new(1f, 0.4f, 0.55f) },
        "jungle" => new Tones { main = new(0.55f, 0.9f, 0.15f), deep = new(0.03f, 0.11f, 0.05f), hot = new(1f, 0.85f, 0.2f) },
        "ocean" => new Tones { main = new(0.15f, 0.55f, 1f), deep = new(0.02f, 0.07f, 0.2f), hot = new(1f, 0.5f, 0.35f) },
        "volcano" => new Tones { main = new(1f, 0.22f, 0.1f), deep = new(0.13f, 0.03f, 0.02f), hot = new(1f, 0.75f, 0.1f) },
        "city" => new Tones { main = new(1f, 0.2f, 0.6f), deep = new(0.08f, 0.04f, 0.13f), hot = new(0.25f, 0.9f, 1f) },
        "space" => new Tones { main = new(0.58f, 0.38f, 1f), deep = new(0.04f, 0.02f, 0.11f), hot = new(1f, 0.85f, 0.25f) },
        "alien" => new Tones { main = new(0.1f, 0.92f, 0.75f), deep = new(0.1f, 0.03f, 0.14f), hot = new(1f, 0.3f, 0.8f) },
        "menu" => new Tones { main = new(0.9f, 0.12f, 0.2f), deep = new(0.06f, 0.03f, 0.06f), hot = new(1f, 0.82f, 0.2f) }, // outside a run: the menus, the story
        "fortress" => new Tones { main = new(0.45f, 1f, 0.2f), deep = new(0.03f, 0.06f, 0.05f), hot = new(1f, 0.28f, 0.2f) },
        _ => new Tones { main = new(1f, 0.55f, 0.15f), deep = new(0.17f, 0.07f, 0.03f), hot = new(1f, 0.88f, 0.25f) }, // the desert
    };

    // Each hero's colours, from their art: for the locomotive menu while they are picked, and their lines.
    public static Tones Hero(string id) => id switch
    {
        "cerebron" => new Tones { main = new(0.6f, 0.28f, 1f), deep = new(0.07f, 0.03f, 0.12f), hot = new(1f, 0.62f, 0.5f) },
        "culu" => new Tones { main = new(0.45f, 0.8f, 0.25f), deep = new(0.04f, 0.09f, 0.04f), hot = new(1f, 0.3f, 0.3f) },
        "faceless" => new Tones { main = new(0.3f, 0.75f, 1f), deep = new(0.03f, 0.06f, 0.12f), hot = new(0.95f, 0.98f, 1f) },
        "sentinel" => new Tones { main = new(1f, 0.5f, 0.08f), deep = new(0.1f, 0.05f, 0.02f), hot = new(1f, 0.86f, 0.3f) },
        "tzmil" => new Tones { main = new(0.95f, 0.2f, 0.8f), deep = new(0.09f, 0.03f, 0.1f), hot = new(0.72f, 0.5f, 1f) },
        "xiuhtecuhtli" => new Tones { main = new(0.1f, 0.92f, 0.78f), deep = new(0.02f, 0.09f, 0.08f), hot = new(1f, 0.45f, 0.15f) },
        "skullmonk" => new Tones { main = new(0.88f, 0.78f, 0.58f), deep = new(0.1f, 0.07f, 0.05f), hot = new(0.9f, 0.15f, 0.15f) },
        "onini" => new Tones { main = new(0.62f, 0.04f, 0.1f), deep = new(0.05f, 0.01f, 0.02f), hot = new(1f, 0.9f, 0.86f) },
        _ => Biome("menu"), // BBA: red and white
    };

    // The colours on screen now: a biome's while playing (TrainSim), a hero's or the menus' elsewhere. Whatever is
    // painted with Paint follows them when they change.
    static string toneKey = "";
    public static Tones Now = Biome("");
    public static int ToneVersion;
    public static void SetBiome(string id) => SetTones("biome:" + id, () => Biome(id));
    public static void SetHeroTones(string id) => SetTones("hero:" + id, () => Hero(id));
    static void SetTones(string key, System.Func<Tones> tones)
    {
        if (key == toneKey) return;
        toneKey = key;
        Now = tones();
        ToneVersion++;
    }

    // A step of the way from the colours now to others (k: how far, 1 the whole way): a blend instead of a cut.
    public static void EaseTones(Tones to, float k)
    {
        Tones from = Now;
        Now = new Tones { main = Color.Lerp(from.main, to.main, k), deep = Color.Lerp(from.deep, to.deep, k), hot = Color.Lerp(from.hot, to.hot, k) };
        if (Now.main == from.main && Now.deep == from.deep && Now.hot == from.hot) return;
        toneKey = "";
        ToneVersion++;
    }

    // Whose colours a line of dialogue is in: the hero whose name it is, else the rival's red or the biome's.
    public static Color Speaker(string name, bool rival)
    {
        foreach (string id in Locomotives.Heroes)
            if (id != null && L10n.T("hero.character." + id) == name) return Hero(id).main;
        if (L10n.T("hero.character.bba") == name) return Hero("bba").main;
        return rival ? new Color(0.9f, 0.15f, 0.2f) : Now.main;
    }

    public enum Ink { Main, Deep, Hot, Plate }
    public static T Paint<T>(T graphic, Ink ink, float alpha = 1f) where T : Graphic
    {
        var themed = graphic.gameObject.AddComponent<Themed>();
        themed.ink = ink;
        themed.alpha = alpha;
        themed.Apply();
        return graphic;
    }

    // The Persona plate: a hard shadow in the biome's main colour, a white outline, the dark plate itself (deep,
    // fading darker). Nested, so the shadow is its root: show, hide, move or anchor that (Root). Returns the plate.
    public static Slanted Plate(Transform parent, Vector2 at, Vector2 size, float slant = 0.12f, float outline = 5f, float shadow = 10f)
    {
        Slanted under = Panel(parent, at + new Vector2(shadow, -shadow), size + Vector2.one * outline * 2f, Color.white, Color.white, slant);
        Paint(under, Ink.Main);
        Slanted rim = Panel(under.transform, new Vector2(-shadow, shadow), size + Vector2.one * outline * 2f, Color.white, Color.white, slant);
        Slanted plate = Panel(rim.transform, Vector2.zero, size, Color.white, Color.white, slant);
        Paint(plate, Ink.Plate, 0.96f);
        under.pixelsPerUnitMultiplier = rim.pixelsPerUnitMultiplier = plate.pixelsPerUnitMultiplier = 3f;
        return plate;
    }
    // An existing panel made a Persona plate where it is: painted the plate's dark, with the biome's hard shadow
    // and a white outline put behind it as its siblings, following it (Backed). One line per panel.
    public static Slanted Persona(Slanted panel, float outline = 4f, float shadow = 8f)
    {
        Paint(panel, Ink.Plate, 0.94f);
        panel.pixelsPerUnitMultiplier = 3f;
        Transform parent = panel.transform.parent;
        int at = panel.transform.GetSiblingIndex();
        Slanted under = Panel(parent, Vector2.zero, Vector2.zero, Color.white, Color.white, panel.slant);
        Slanted rim = Panel(parent, Vector2.zero, Vector2.zero, Color.white, Color.white, panel.slant);
        Paint(under, Ink.Main);
        under.sprite = rim.sprite = panel.sprite;
        under.pixelsPerUnitMultiplier = rim.pixelsPerUnitMultiplier = 3f;
        under.transform.SetSiblingIndex(at);
        rim.transform.SetSiblingIndex(at + 1);
        var back = panel.gameObject.AddComponent<Backed>();
        back.shadow = under.rectTransform;
        back.rim = rim.rectTransform;
        back.outline = outline;
        back.offset = shadow;
        back.Sync();
        return panel;
    }

    public static RectTransform Root(Slanted plate) => (RectTransform)plate.transform.parent.parent;
    public static void Resize(Slanted plate, Vector2 size)
    {
        float outline = (((RectTransform)plate.transform.parent).sizeDelta.x - plate.rectTransform.sizeDelta.x) * 0.5f;
        plate.rectTransform.sizeDelta = size;
        ((RectTransform)plate.transform.parent).sizeDelta = Root(plate).sizeDelta = size + Vector2.one * outline * 2f;
    }

    // A hollow rectangle, thick bars of one colour (a stamp's border).
    public static void Frame(Transform parent, Vector2 size, float thick, Color colour)
    {
        foreach (var (at, bar) in new[] { (new Vector2(0f, (size.y - thick) * 0.5f), new Vector2(size.x, thick)), (new Vector2(0f, -(size.y - thick) * 0.5f), new Vector2(size.x, thick)),
                                          (new Vector2(-(size.x - thick) * 0.5f, 0f), new Vector2(thick, size.y)), (new Vector2((size.x - thick) * 0.5f, 0f), new Vector2(thick, size.y)) })
            Panel(parent, at, bar, colour, colour, 0f).sprite = null;
    }

    // Eases out past the mark and settles back on it (entrances with a bounce).
    public static float Back(float t)
    {
        t = Mathf.Clamp01(t) - 1f;
        return 1f + 2.70158f * t * t * t + 1.70158f * t * t;
    }

    // The button that confirms, as a glyph for the device in use.
    public static string ConfirmGlyph(bool gamepad) => !gamepad ? "Enter" : UnityEngine.InputSystem.Gamepad.current is UnityEngine.InputSystem.DualShock.DualShockGamepad ? "✕" : "xA";

    // A button drawn small, height tall: a key cap, the mouse, a pad's face button in its colour, a shoulder or a
    // trigger, a stick.
    public static RectTransform Glyph(Transform parent, string key, float height)
    {
        var root = Group(parent, new Vector2(0.5f, 0.5f), Vector2.zero);
        var ink = new Color(0.07f, 0.09f, 0.18f);
        var pad = new Color(0.1f, 0.12f, 0.2f);
        int face = "△○□✕".IndexOf(key);
        if (face >= 0 || (key.Length == 2 && key[0] == 'x'))
        {
            root.sizeDelta = new Vector2(height, height);
            Panel(root, new Vector2(0f, -3f), root.sizeDelta, new Color(0f, 0f, 0f, 0.5f), new Color(0f, 0f, 0f, 0.5f), 0f).sprite = Disc();
            Panel(root, Vector2.zero, root.sizeDelta, pad, pad, 0f).sprite = Disc();
            if (face >= 0)
            {
                var symbol = new GameObject("Symbol", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
                symbol.transform.SetParent(root, false);
                symbol.raycastTarget = false;
                symbol.texture = Symbol(face);
                symbol.color = new[] { new Color(0.3f, 0.9f, 0.7f), new Color(1f, 0.4f, 0.45f), new Color(0.95f, 0.5f, 0.85f), new Color(0.45f, 0.7f, 1f) }[face];
                symbol.rectTransform.sizeDelta = Vector2.one * height * 0.62f;
            }
            else
            {
                Color[] xbox = { new(0.4f, 0.85f, 0.3f), new(1f, 0.35f, 0.3f), new(0.3f, 0.6f, 1f), new(1f, 0.8f, 0.2f) };
                Text letter = Label(root, Display, Mathf.RoundToInt(height * 0.62f), xbox[Mathf.Max(0, "ABXY".IndexOf(key[1]))], TextAnchor.MiddleCenter, new Vector2(0f, 1f), root.sizeDelta);
                letter.fontStyle = FontStyle.Normal;
                letter.text = key.Substring(1);
            }
            return root;
        }
        if (key == "Mouse")
        {
            root.sizeDelta = new Vector2(height * 0.75f, height);
            var mouse = new GameObject("Mouse", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
            mouse.transform.SetParent(root, false);
            mouse.raycastTarget = false;
            mouse.texture = MouseTexture();
            mouse.rectTransform.sizeDelta = root.sizeDelta;
            return root;
        }
        if (key == "LS" || key == "RS")
        {
            root.sizeDelta = new Vector2(height, height);
            Panel(root, new Vector2(0f, -3f), root.sizeDelta, new Color(0f, 0f, 0f, 0.5f), new Color(0f, 0f, 0f, 0.5f), 0f).sprite = Disc();
            Panel(root, Vector2.zero, root.sizeDelta, new Color(0.8f, 0.84f, 0.92f), new Color(0.8f, 0.84f, 0.92f), 0f).sprite = Disc();
            Panel(root, Vector2.zero, root.sizeDelta * 0.74f, pad, pad, 0f).sprite = Disc();
            Text stick = Label(root, Display, Mathf.RoundToInt(height * 0.5f), Color.white, TextAnchor.MiddleCenter, new Vector2(0f, 1f), root.sizeDelta);
            stick.fontStyle = FontStyle.Normal;
            stick.text = key.Substring(0, 1);
            return root;
        }
        // Shoulders and triggers: dark, rounded; anything else is a key: a white cap on a dark edge.
        bool shoulder = key.Length == 2 && (key[0] == 'L' || key[0] == 'R') && "12BT".IndexOf(key[1]) >= 0;
        Text cap = Label(root, Display, Mathf.RoundToInt(height * 0.56f), shoulder ? Color.white : ink, TextAnchor.MiddleCenter, new Vector2(0f, 1f), new Vector2(400f, height));
        cap.fontStyle = FontStyle.Normal;
        cap.horizontalOverflow = HorizontalWrapMode.Overflow;
        cap.text = key.ToUpperInvariant();
        float wide = Mathf.Max(height * (shoulder ? 1.3f : 1f), cap.preferredWidth + height * 0.5f);
        root.sizeDelta = new Vector2(wide, height);
        cap.rectTransform.sizeDelta = root.sizeDelta;
        Color top = shoulder ? pad : new Color(0.97f, 0.97f, 1f), edge = shoulder ? new Color(0f, 0f, 0f, 0.5f) : new Color(0.55f, 0.6f, 0.72f);
        Slanted under = Panel(root, new Vector2(0f, -4f), root.sizeDelta, edge, edge, 0f);
        Slanted body = Panel(root, Vector2.zero, root.sizeDelta, top, shoulder ? top : new Color(0.86f, 0.89f, 0.95f), 0f);
        under.pixelsPerUnitMultiplier = body.pixelsPerUnitMultiplier = shoulder ? 2.5f : 4f; // a key's corners, not a pill's
        under.transform.SetAsFirstSibling();
        body.transform.SetSiblingIndex(1);
        return root;
    }

    // PlayStation's four symbols, white on clear, to be tinted: triangle, circle, square, cross.
    static readonly Texture2D[] symbols = new Texture2D[4];
    static Texture2D Symbol(int kind)
    {
        if (symbols[kind]) return symbols[kind];
        const int size = 64;
        const float r = 24f, line = 4.2f;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp };
        Vector2 a = new(0f, r), b = new(-r * 0.87f, -r * 0.5f), c = new(r * 0.87f, -r * 0.5f);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                var p = new Vector2(x + 0.5f - size * 0.5f, y + 0.5f - size * 0.5f + (kind == 0 ? 3f : 0f));
                float d = kind switch
                {
                    0 => Mathf.Min(Segment(p, a, b), Mathf.Min(Segment(p, b, c), Segment(p, c, a))),
                    1 => Mathf.Abs(p.magnitude - r * 0.85f),
                    2 => Mathf.Abs(Mathf.Max(Mathf.Abs(p.x), Mathf.Abs(p.y)) - r * 0.72f),
                    _ => Mathf.Min(Segment(p, new Vector2(-r, -r) * 0.72f, new Vector2(r, r) * 0.72f), Segment(p, new Vector2(-r, r) * 0.72f, new Vector2(r, -r) * 0.72f)),
                };
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(line - d + 0.5f)));
            }
        texture.Apply();
        return symbols[kind] = texture;
    }

    static float Segment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
        return (p - (a + ab * t)).magnitude;
    }

    // A mouse: white, a dark outline, the split between its buttons and its wheel.
    static Texture2D mouseTexture;
    static Texture2D MouseTexture()
    {
        if (mouseTexture) return mouseTexture;
        const int w = 48, h = 64;
        mouseTexture = new Texture2D(w, h, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp };
        var outline = new Color(0.07f, 0.09f, 0.18f);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                // a capsule: half widths 20 by 29, round ends
                float px = x + 0.5f - w * 0.5f, py = y + 0.5f - h * 0.5f;
                float qy = Mathf.Max(Mathf.Abs(py) - 9f, 0f), d = new Vector2(px, qy).magnitude - 20f;
                float inside = Mathf.Clamp01(-d + 0.5f), body = Mathf.Clamp01(-(d + 3.5f) + 0.5f);
                bool split = (Mathf.Abs(px) < 1.6f && py > 2f) || (Mathf.Abs(py - 4f) < 1.6f && Mathf.Abs(px) < 19f);
                bool wheel = Mathf.Abs(px) < 3.2f && py > 10f && py < 22f;
                Color c = Color.Lerp(outline, Color.white, body);
                if (split && body > 0.5f) c = outline;
                if (wheel) c = new Color(0.45f, 0.7f, 1f);
                c.a = inside;
                mouseTexture.SetPixel(x, y, c);
            }
        mouseTexture.Apply();
        return mouseTexture;
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
        public float rest, top, across; // where it sits in the row, how far above that its top edge is, and where across
        public bool enabled, row;
    }

    public bool Visible => canvas.gameObject.activeSelf;

    Canvas canvas;
    RectTransform row, ribbon, listRoot;
    GameObject bar, dim;
    Slanted listCard;
    Text title, sub, purse, hint, listTitle, listHint;
    GameObject steps, purseRoot;
    Slanted edge, lineBadge, halftone, signShadow;
    Text lineNumber, before, after;
    const int Steps = 5;
    readonly Slanted[] stepMarks = new Slanted[Steps];
    readonly Slanted[] shelfTags = new Slanted[2];
    readonly Text[] shelfTexts = new Text[2];
    System.Action<int> tabPicked;
    Text arrow;        // the first-station guide: bobs over the focused card
    Slanted hintBar, listTag;
    Prompt hintLine, listHintLine; // the controls, their buttons drawn
    RectTransform listTilt;
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

    // The train seen from the side, engine on the left, in a picture across the menu (TrainSim films it), with a
    // tag under every wagon saying what it carries: whichever way the real one runs on the screen and whatever
    // stands in front of it. Shown while something is being given a place on it.
    public struct Slot
    {
        public string icon, tag, label; // tag: the wagon's number
        public int state;               // 0 as it is, 1 the place being chosen, 2 out of the question
        public float x;                 // where the wagon is across the picture, 0 to 1
    }
    class Tile
    {
        public Slanted back;
        public RawImage icon;
        public Text label;
    }
    public const float PictureWide = 1500f, PictureTall = 290f;
    readonly List<Tile> tiles = new();
    RectTransform trainRow;
    RawImage trainPicture;
    int trainFrame = -1;

    // Call before Render, every frame it should show.
    public void Train(IList<Slot> slots, Texture picture)
    {
        trainFrame = Time.frameCount;
        trainPicture.texture = picture;
        for (int i = 0; i < Mathf.Max(slots.Count, tiles.Count); i++)
        {
            if (i == tiles.Count)
            {
                var made = new Tile { back = UiKit.Panel(trainRow, Vector2.zero, new Vector2(150f, 58f), UiKit.White, UiKit.White, 0.1f) };
                made.icon = UiKit.Picture(made.back.transform, null, new Vector2(-48f, 0f), 46f);
                made.label = UiKit.Label(made.back.transform, UiKit.Display, 24, UiKit.Navy, TextAnchor.MiddleCenter, new Vector2(24f, 0f), new Vector2(92f, 52f), true);
                tiles.Add(made);
            }
            Tile tile = tiles[i];
            tile.back.gameObject.SetActive(i < slots.Count);
            if (i >= slots.Count) continue;
            Slot slot = slots[i];
            bool chosen = slot.state == 1, out_ = slot.state == 2;
            tile.back.rectTransform.anchoredPosition = new Vector2((slot.x - 0.5f) * PictureWide, -155f - (chosen ? 8f : 0f));
            tile.back.rectTransform.localScale = Vector3.one * (chosen ? 1.25f + Mathf.Sin(Time.unscaledTime * 6f) * 0.04f : 0.95f);
            tile.back.color = tile.back.bottom = chosen ? UiKit.Rose : out_ ? new Color(UiKit.Navy.r, UiKit.Navy.g, UiKit.Navy.b, 0.75f) : UiKit.White;
            UiKit.SetIcon(tile.icon, slot.icon);
            tile.icon.color = out_ ? new Color(1f, 1f, 1f, 0.5f) : Color.white;
            tile.label.text = slot.tag + (string.IsNullOrEmpty(slot.label) ? "" : " · " + slot.label);
            tile.label.color = chosen || out_ ? Color.white : UiKit.Navy;
            if (chosen) tile.back.transform.SetAsLastSibling();
        }
    }

    readonly List<Card> cards = new();
    string shown; // what the cards on screen were built from
    float age;    // seconds since they were built, for the entrance

    void Awake()
    {
        canvas = UiKit.NewCanvas("StationMenu", 10);

        // Behind the title: a band of halftone dots across the screen, tilted, in the step's colour.
        halftone = UiKit.Panel(canvas.transform, new Vector2(0f, -300f), new Vector2(2800f, 230f), UiKit.Sky, UiKit.Sky, 0f);
        halftone.sprite = UiKit.Halftone();
        halftone.type = Image.Type.Tiled;
        halftone.rectTransform.anchorMin = halftone.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        halftone.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 4f);
        // Title: a station name sign, Japanese style. A white plate on a hard shadow, the step's number in a station
        // badge on its left, the title in big letters; along its foot a band in the step's colour with the step
        // before on the left, the next on the right and five marks for how far through the station we are.
        signShadow = UiKit.Panel(canvas.transform, new Vector2(14f, -332f), new Vector2(1240f, 200f), new Color(0.03f, 0.04f, 0.1f, 0.85f), new Color(0.03f, 0.04f, 0.1f, 0.85f), 0.12f);
        signShadow.rectTransform.anchorMin = signShadow.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        ribbon = UiKit.Panel(canvas.transform, new Vector2(0f, -318f), new Vector2(1240f, 200f), Color.white, new Color(0.9f, 0.93f, 0.98f), 0.12f).rectTransform;
        ribbon.anchorMin = ribbon.anchorMax = new Vector2(0.5f, 1f);
        edge = UiKit.Panel(ribbon, new Vector2(-4f, -74f), new Vector2(1228f, 52f), UiKit.Sky, UiKit.Sky, 0.12f);
        steps = edge.gameObject;
        for (int i = 0; i < Steps; i++)
            stepMarks[i] = UiKit.Panel(edge.transform, new Vector2((i - (Steps - 1) * 0.5f) * 34f, 0f), new Vector2(24f, 24f), Color.white, Color.white, 0.25f);
        before = UiKit.Label(edge.transform, UiKit.Display, 30, Color.white, TextAnchor.MiddleLeft, new Vector2(-370f, 0f), new Vector2(420f, 46f), true);
        after = UiKit.Label(edge.transform, UiKit.Display, 30, Color.white, TextAnchor.MiddleRight, new Vector2(370f, 0f), new Vector2(420f, 46f), true);
        lineBadge = UiKit.Panel(ribbon, new Vector2(-530f, 26f), new Vector2(108f, 108f), UiKit.Sky, UiKit.Sky, 0f);
        UiKit.Panel(lineBadge.transform, Vector2.zero, new Vector2(84f, 84f), Color.white, Color.white, 0f);
        lineNumber = UiKit.Label(lineBadge.transform, UiKit.Display, 64, UiKit.Sky, TextAnchor.MiddleCenter, new Vector2(0f, 2f), new Vector2(84f, 80f), true);
        lineNumber.fontStyle = FontStyle.Normal;
        title = UiKit.Label(ribbon, UiKit.Display, 68, UiKit.Navy, TextAnchor.MiddleCenter, new Vector2(30f, 46f), new Vector2(980f, 80f), true);
        sub = UiKit.Label(ribbon, UiKit.Body, 27, UiKit.Slate, TextAnchor.MiddleCenter, new Vector2(30f, -10f), new Vector2(980f, 36f), true);
        Slanted wallet = UiKit.Plate(canvas.transform, new Vector2(760f, -310f), new Vector2(230f, 84f), 0.2f);
        UiKit.Root(wallet).anchorMin = UiKit.Root(wallet).anchorMax = new Vector2(0.5f, 1f);
        purseRoot = UiKit.Root(wallet).gameObject;
        UiKit.Picture(wallet.transform, "money", new Vector2(-68f, 0f), 62f);
        purse = UiKit.Label(wallet.transform, UiKit.Display, 52, Color.white, TextAnchor.MiddleLeft, new Vector2(36f, 0f), new Vector2(140f, 60f), true);

        row = UiKit.Group(canvas.transform, new Vector2(0.5f, 0f), new Vector2(0f, 395f));
        trainRow = UiKit.Group(canvas.transform, new Vector2(0.5f, 1f), new Vector2(0f, -575f));
        UiKit.Panel(trainRow, new Vector2(0f, 30f), new Vector2(PictureWide + 40f, PictureTall + 30f), UiKit.White, UiKit.Ice, 0.03f);
        trainPicture = new GameObject("TrainFromTheSide", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
        trainPicture.transform.SetParent(trainRow, false);
        trainPicture.raycastTarget = false;
        trainPicture.rectTransform.sizeDelta = new Vector2(PictureWide, PictureTall);
        trainPicture.rectTransform.anchoredPosition = new Vector2(0f, 30f);

        Slanted strip = UiKit.Plate(canvas.transform, new Vector2(0f, 60f), new Vector2(1160f, 58f), 0.25f, 4f, 7f);
        UiKit.Root(strip).anchorMin = UiKit.Root(strip).anchorMax = new Vector2(0.5f, 0f);
        bar = UiKit.Root(strip).gameObject;
        hintBar = strip;
        for (int i = 0; i < 2; i++)
        {
            // The market's two tabs, side by side between the title and the cards; clicking one opens it.
            shelfTags[i] = UiKit.Panel(canvas.transform, new Vector2(i == 0 ? -270f : 270f, 642f), new Vector2(510f, 54f), UiKit.Navy, UiKit.Navy, 0.2f);
            shelfTags[i].rectTransform.anchorMin = shelfTags[i].rectTransform.anchorMax = new Vector2(0.5f, 0f);
            shelfTags[i].raycastTarget = true;
            shelfTexts[i] = UiKit.Label(shelfTags[i].transform, UiKit.Heading, 30, Color.white, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(480f, 50f), true);
            var pick = shelfTags[i].gameObject.AddComponent<Clicks>();
            pick.index = i;
            pick.click = index => tabPicked?.Invoke(index);
        }
        arrow = UiKit.Label(canvas.transform, UiKit.Heading, 90, Color.white, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(120f, 100f), true);
        UiKit.Paint(arrow, UiKit.Ink.Hot);
        arrow.rectTransform.anchorMin = arrow.rectTransform.anchorMax = new Vector2(0.5f, 0f);
        arrow.text = "▼";
        arrow.fontStyle = FontStyle.Normal;
        var rim = arrow.gameObject.AddComponent<Outline>();
        rim.effectColor = UiKit.Navy;
        rim.effectDistance = new Vector2(4f, -4f);
        hint = UiKit.Label(strip.transform, UiKit.Body, 25, Color.white, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(1120f, 50f), true);
        hint.enabled = false; // the line is drawn with its buttons
        hintLine = Prompt.Make(strip.transform, Vector2.zero, 23, Color.white);

        // List layout (pause): the game dimmed, a box in the middle, one row per option.
        var veil = new GameObject("Dim", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        veil.transform.SetParent(canvas.transform, false);
        veil.rectTransform.anchorMin = Vector2.zero;
        veil.rectTransform.anchorMax = Vector2.one;
        veil.rectTransform.sizeDelta = Vector2.zero;
        UiKit.Paint(veil, UiKit.Ink.Deep, 0.82f);
        dim = veil.gameObject;
        // The pause, Persona style: the biome's band across behind, a tilted dark plate, the title on a tag of the
        // main colour breaking out of its top.
        listRoot = UiKit.Group(canvas.transform, new Vector2(0.5f, 0.5f), Vector2.zero);
        RectTransform sweep = UiKit.Group(listRoot, new Vector2(0.5f, 0.5f), Vector2.zero);
        sweep.localRotation = Quaternion.Euler(0f, 0f, 12f);
        UiKit.Paint(UiKit.Panel(sweep, Vector2.zero, new Vector2(3400f, 300f), Color.white, Color.white, 0f), UiKit.Ink.Main, 0.9f).sprite = null;
        Slanted sweepDots = UiKit.Panel(sweep, Vector2.zero, new Vector2(3400f, 300f), Color.white, Color.white, 0f);
        sweepDots.sprite = UiKit.Halftone();
        sweepDots.type = Image.Type.Tiled;
        UiKit.Paint(sweepDots, UiKit.Ink.Deep, 0.25f);
        listTilt = UiKit.Group(listRoot, new Vector2(0.5f, 0.5f), Vector2.zero);
        listTilt.localRotation = Quaternion.Euler(0f, 0f, -2f);
        listCard = UiKit.Plate(listTilt, Vector2.zero, new Vector2(700f, 400f), 0.04f, 6f, 14f);
        listTag = UiKit.Panel(listTilt, Vector2.zero, new Vector2(520f, 90f), Color.white, Color.white, 0.25f);
        listTag.sprite = null;
        listTag.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 4f);
        UiKit.Paint(listTag, UiKit.Ink.Main);
        listTitle = UiKit.Label(listTag.transform, UiKit.Display, 60, Color.white, TextAnchor.MiddleCenter, new Vector2(0f, 2f), new Vector2(500f, 84f), true);
        listHint = UiKit.Label(listTilt, UiKit.Body, 22, Color.white, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(620f, 40f), true);
        listHint.enabled = false;
        listHintLine = Prompt.Make(listHint.transform, Vector2.zero, 20, Color.white);

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
        // compact: the two buttons, small, in the bottom right corner.
        row.anchoredPosition = new Vector2(compact ? 660f : 0f, compact ? 175f : shelves != null ? 350f : 375f); // under the tabs, when there are any
        row.localScale = Vector3.one * (compact ? 0.8f : 1f);
        lowered = compact;
        trainRow.gameObject.SetActive(!list && trainFrame == Time.frameCount);
        if (options == null || options.Count == 0)
        {
            if (Visible) canvas.gameObject.SetActive(false);
            shown = null;
            return;
        }
        if (!Visible) canvas.gameObject.SetActive(true);
        title.text = heading.ToUpperInvariant();
        listTitle.text = heading.ToUpperInvariant();
        hint.text = listHint.text = help;
        hintLine.Set(help);
        listHintLine.Set(help);
        ribbon.gameObject.SetActive(!list);
        bar.SetActive(!list && !compact);
        purseRoot.SetActive(!list && money >= 0);
        purse.text = money.ToString();
        theme = UiKit.Now.main; // the biome's colour, at every step
        edge.color = edge.bottom = lineBadge.color = lineBadge.bottom = theme;
        lineBadge.gameObject.SetActive(step >= 0);
        lineNumber.text = (step + 1).ToString("00");
        lineNumber.color = theme;
        signShadow.gameObject.SetActive(!list);
        signShadow.color = signShadow.bottom = new Color(UiKit.Now.deep.r, UiKit.Now.deep.g, UiKit.Now.deep.b, 0.9f);
        halftone.gameObject.SetActive(!list);
        halftone.color = halftone.bottom = new Color(theme.r, theme.g, theme.b, 0.55f);
        bool noted = !string.IsNullOrEmpty(note);
        sub.text = noted ? note : "";
        // The sign has its band with the steps in it; placing a wagon, it is shorter, so the train's picture fits.
        bool band = step >= 0 && !compact;
        float badge = step >= 0 ? 30f : 0f, high = band ? 200f : 130f, y = band ? -306f : -300f;
        title.rectTransform.anchoredPosition = new Vector2(badge, band ? (noted ? 46f : 30f) : noted ? 22f : 0f);
        sub.rectTransform.anchoredPosition = new Vector2(badge, band ? -10f : -36f);
        ribbon.sizeDelta = signShadow.rectTransform.sizeDelta = new Vector2(1240f, high);
        ribbon.anchoredPosition = new Vector2(0f, y);
        signShadow.rectTransform.anchoredPosition = new Vector2(14f, y - 14f);
        lineBadge.rectTransform.anchoredPosition = new Vector2(-530f, band ? 26f : 0f);
        steps.SetActive(!list && band);
        if (band)
        {
            // The steps on either side, as a station sign shows the stations either side; the marks: done and here
            // white, the one we are at bigger, the ones to come faint.
            before.text = step > 0 ? "◀  " + step.ToString("00") + "  " + L10n.T(StepKeys[step - 1]).ToUpperInvariant() : "";
            after.text = step < Steps - 1 ? L10n.T(StepKeys[step + 1]).ToUpperInvariant() + "  " + (step + 2).ToString("00") + "  ▶" : "";
            for (int i = 0; i < Steps; i++)
            {
                stepMarks[i].color = stepMarks[i].bottom = i <= step ? Color.white : new Color(1f, 1f, 1f, 0.3f);
                stepMarks[i].rectTransform.localScale = Vector3.one * (i == step ? 1.3f + Mathf.Sin(Time.unscaledTime * 5f) * 0.06f : 0.8f);
            }
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
                // A row: dark glass; the focused one in the main colour, bigger and pushed out to the right.
                card.group.alpha = arrive;
                card.accent.color = card.accent.bottom = focused ? UiKit.Now.main : new Color(1f, 1f, 1f, 0.08f);
                float grow = Mathf.Lerp(card.root.localScale.x, focused ? 1.08f : 1f, 1f - Mathf.Exp(-18f * Time.unscaledDeltaTime));
                card.root.localScale = new Vector3(grow, grow, 1f);
                card.accent.rectTransform.anchoredPosition = new Vector2(Mathf.Lerp(card.accent.rectTransform.anchoredPosition.x, focused ? 26f : 0f, 1f - Mathf.Exp(-18f * Time.unscaledDeltaTime)), 0f);
                continue;
            }
            float size = Mathf.Lerp(card.root.localScale.x, focused ? 1.08f : 1f, 1f - Mathf.Exp(-18f * Time.unscaledDeltaTime));
            card.root.localScale = new Vector3(size, size, 1f);
            float slide = Mathf.Clamp01((age - i * 0.06f) / 0.32f) - 1f, back = 1f + 2.70158f * slide * slide * slide + 1.70158f * slide * slide; // eases out past the mark and back
            card.root.anchoredPosition = new Vector2(card.across + (1f - back) * 420f, card.rest + (focused ? 14f : 0f));
            float tilt = Mathf.DeltaAngle(0f, card.root.localEulerAngles.z);
            card.root.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(tilt, focused ? 3f : 0f, 1f - Mathf.Exp(-14f * Time.unscaledDeltaTime)));
            card.group.alpha = arrive * (card.enabled ? 1f : 0.45f);
            card.frame.color = focused ? UiKit.Now.hot : new Color(1f, 1f, 1f, 0f);
            card.accent.color = card.accent.bottom = focused ? UiKit.Now.hot : card.theme;
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
        Color calm = new Color(0.02f, 0.02f, 0.04f, 0.92f);
        hintBar.color = hintBar.bottom = guiding ? Color.Lerp(calm, new Color(0.85f, 0.55f, 0f, 0.95f), beat) : calm;
        UiKit.Root(hintBar).localScale = Vector3.one * (guiding ? 1.12f : 1f);
    }

    void BuildList(IList<TrainSim.Option> options, System.Action<int> click, System.Action<int> hover)
    {
        foreach (Card old in cards) Destroy(old.root.gameObject);
        cards.Clear();
        age = 0f;

        const float rowHeight = 74f, gap = 14f, top = 130f, bottom = 70f;
        float height = top + options.Count * (rowHeight + gap) + bottom;
        UiKit.Resize(listCard, new Vector2(700f, height));
        listTag.rectTransform.anchoredPosition = new Vector2(-120f, height * 0.5f + 6f);
        listHint.rectTransform.anchoredPosition = new Vector2(0f, -height * 0.5f + 42f);
        for (int i = 0; i < options.Count; i++)
        {
            TrainSim.Option option = options[i];
            var card = new Card { enabled = true, row = true };
            card.root = new GameObject("Row", typeof(RectTransform), typeof(CanvasGroup)).GetComponent<RectTransform>();
            card.root.SetParent(listTilt, false);
            card.root.sizeDelta = new Vector2(560f, rowHeight);
            card.root.anchoredPosition = new Vector2(0f, height * 0.5f - top - rowHeight * 0.5f - i * (rowHeight + gap));
            card.group = card.root.GetComponent<CanvasGroup>();
            card.frame = card.accent = UiKit.Panel(card.root, Vector2.zero, new Vector2(560f, rowHeight), UiKit.Ice, UiKit.Ice, 0.25f);
            card.accent.sprite = null;
            card.accent.raycastTarget = true;
            UiKit.Picture(card.accent.transform, option.icon, new Vector2(-220f, 0f), 58f);
            UiKit.Label(card.accent.transform, UiKit.Display, 38, Color.white, TextAnchor.MiddleCenter, new Vector2(24f, 2f), new Vector2(400f, 62f), true).text = option.label.Replace('\n', ' ').ToUpperInvariant();
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
            card.across = x + w * 0.5f;
            card.root.anchoredPosition = new Vector2(card.across, card.rest);
            card.group = card.root.GetComponent<CanvasGroup>();
            RectTransform body = card.root;

            // A ticket: a hard shadow in its colour, the gold rim when chosen, the ticket itself with a band across
            // its top (its number on it) and, on the big ones, a stub with a barcode.
            float stub = minor ? 0f : 64f;
            Sprite ticket = UiKit.Ticket(w, h, stub);
            var shade = new Color(tone.r * 0.45f, tone.g * 0.45f, tone.b * 0.55f, 0.95f);
            UiKit.Panel(body, new Vector2(14f, -16f), new Vector2(w, h), shade, shade).sprite = ticket;                   // shadow
            card.frame = UiKit.Panel(body, Vector2.zero, new Vector2(w + 20f, h + 20f), UiKit.Now.hot, UiKit.Now.hot);       // focus rim
            card.frame.sprite = ticket;
            var paper = new Color(1f, 0.985f, 0.94f);
            Slanted face = UiKit.Panel(body, Vector2.zero, new Vector2(w, h), minor ? grey : paper, minor ? grey : new Color(0.97f, 0.94f, 0.87f));
            face.sprite = ticket;
            face.type = Image.Type.Simple;
            face.raycastTarget = true;
            card.accent = UiKit.Panel(face.transform, new Vector2((h * 0.5f - 36f) * 0.12f, h * 0.5f - 36f), new Vector2(w - 26f, 44f), tone, tone);
            var serial = UiKit.Label(card.accent.transform, UiKit.Display, 24, new Color(1f, 1f, 1f, 0.9f), TextAnchor.MiddleLeft, new Vector2(10f, 0f), new Vector2(w - 60f, 36f), true);
            serial.text = "Nº " + (i + 1).ToString("00");
            if (stub > 0f)
            {
                var bars = new GameObject("Barcode", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
                bars.transform.SetParent(face.transform, false);
                bars.raycastTarget = false;
                bars.texture = UiKit.Barcode();
                bars.rectTransform.sizeDelta = new Vector2(Mathf.Min(170f, w * 0.48f), 30f);
                bars.rectTransform.anchoredPosition = new Vector2(-w * 0.18f - 22f, -h * 0.5f + stub * 0.5f);
                UiKit.Label(face.transform, UiKit.Body, 18, UiKit.Slate, TextAnchor.MiddleRight, new Vector2(w * 0.2f - 24f, -h * 0.5f + stub * 0.5f), new Vector2(w * 0.42f, 30f), true).text =
                    "WOR-" + (1000 + Mathf.Abs((option.icon + option.label).GetHashCode()) % 9000);
            }

            // First line of the label is the name, the rest is the description.
            int split = option.label.IndexOf('\n');
            string name = split < 0 ? option.label : option.label.Substring(0, split), text = split < 0 ? "" : option.label.Substring(split + 1);
            if (minor)
            {
                UiKit.Picture(face.transform, option.icon, new Vector2(8f, 62f), 120f);
                UiKit.Label(face.transform, UiKit.Display, 38, UiKit.Navy, TextAnchor.MiddleCenter, new Vector2(-4f, -34f), new Vector2(w - 30f, 48f), true).text = name;
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
                UiKit.Picture(face.transform, option.icon, new Vector2(10f, 90f), Mathf.Min(iconSize, 170f));
                UiKit.Label(face.transform, UiKit.Display, 46, UiKit.Navy, TextAnchor.MiddleCenter, new Vector2(-4f, -16f), new Vector2(w - 36f, 58f), true).text = name;
                UiKit.Panel(face.transform, new Vector2(-8f, -48f), new Vector2(Mathf.Min(120f, w * 0.4f), 5f), tone, tone);
                UiKit.Label(face.transform, UiKit.Body, w < 300f ? 21 : 24, UiKit.Slate, TextAnchor.UpperCenter, new Vector2(-14f, -104f), new Vector2(w - 50f, 100f), true).text = text;
            }
            if (!string.IsNullOrEmpty(option.badge))
            {
                // The tag on the corner: free in green, a price in gold with its coin, "cannot" in red.
                bool price = option.tone >= 2 && int.TryParse(option.badge, out _);
                Color paint = option.tone == 1 ? UiKit.Mint : option.tone == 3 ? UiKit.Rose : UiKit.Gold;
                float tagWidth = Mathf.Min(w - 30f, price ? 150f : 190f), tagHeight = two ? 46f : 54f;
                Slanted tag = UiKit.Panel(face.transform, new Vector2(w * 0.5f - tagWidth * 0.5f + 22f, h * 0.5f - 6f), new Vector2(tagWidth, tagHeight), paint, paint, 0.2f);
                if (price) UiKit.Picture(tag.transform, "money", new Vector2(-tagWidth * 0.5f + 34f, 0f), tagHeight - 10f);
                UiKit.Label(tag.transform, UiKit.Display, two ? 30 : 36, option.tone == 2 ? UiKit.Navy : Color.white, TextAnchor.MiddleCenter,
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

