using UnityEngine;
using UnityEngine.UI;

// The card the button games are played on (curve sequence, ramp jump, needle jump, zigzag): a title,
// a row of boxes, a timing bar with an optional green zone, a line of text, or a half dial with a needle.
// Whoever is using it calls Show() every frame and then fills in what it needs; it hides by itself
// as soon as nobody does. Mark() puts floating labels on spots of the world instead, with no card at all
// (the zigzag: its arrows sit on the track's own turns).
public class EventPanel : MonoBehaviour
{
    class Cell
    {
        public Slanted back;
        public Text text;
        public Prompt glyph; // a button, drawn
        public int state = -1;
        public float changed;
    }

    const int MaxBoxes = 12;
    const float BoxSize = 92f, BoxGap = 16f, BarWidth = 900f, DialRadius = 190f;
    static readonly Vector2 DialCentre = new Vector2(0f, -120f);

    Canvas canvas;
    Prompt title, cue;
    TrainSim train;
    UiKit.Tones tone;
    Slanted cardShadow, card, titleTag, barBack, barTrack;
    Image dots;
    RectTransform swing; // the card, tilted, that slams in
    float enteredAt;
    readonly Cell[] boxes = new Cell[MaxBoxes];
    RectTransform bar, barFill, barZone, barMarker, dial, needle;
    GameObject cardRoot;
    readonly System.Collections.Generic.List<(RectTransform rect, Slanted back, Text text)> marks = new();
    int cardFrame = -10, marksFrame = -10, marksUsed;
    readonly Image[] segments = new Image[31];
    int shownFrame = -10;

    void Awake()
    {
        train = GetComponent<TrainSim>();
        canvas = UiKit.NewCanvas("EventPanel", 5);
        RectTransform root = UiKit.Group(canvas.transform, new Vector2(0.5f, 0f), new Vector2(0f, 230f));
        cardRoot = root.gameObject;
        // Persona style: a dark plate in the biome's colours, outlined in white on a hard shadow of its main colour,
        // tilted; dots over it; the title on a tag of the main colour breaking out of its top left.
        swing = UiKit.Group(root, new Vector2(0.5f, 0.5f), Vector2.zero);
        cardShadow = UiKit.Panel(swing, new Vector2(16f, -16f), new Vector2(1240f, 340f), Color.white, Color.white, 0.08f);
        UiKit.Panel(swing, Vector2.zero, new Vector2(1262f, 362f), Color.white, Color.white, 0.08f).pixelsPerUnitMultiplier = 3f; // the white outline
        card = UiKit.Panel(swing, Vector2.zero, new Vector2(1240f, 340f), Color.white, Color.white, 0.08f);
        cardShadow.pixelsPerUnitMultiplier = card.pixelsPerUnitMultiplier = 3f;
        Slanted halftone = UiKit.Panel(card.transform, Vector2.zero, new Vector2(1220f, 320f), Color.white, Color.white, 0.08f);
        halftone.sprite = UiKit.Halftone();
        halftone.type = Image.Type.Tiled;
        dots = halftone;
        titleTag = UiKit.Panel(card.transform, new Vector2(-250f, 168f), new Vector2(760f, 84f), Color.white, Color.white, 0.25f);
        titleTag.sprite = null;
        titleTag.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 2.5f);
        title = Prompt.Make(titleTag.transform, new Vector2(0f, 2f), 46, Color.white, UiKit.Display);

        for (int i = 0; i < MaxBoxes; i++)
        {
            var box = new Cell { back = UiKit.Panel(card.transform, Vector2.zero, new Vector2(BoxSize, BoxSize), UiKit.Ice, UiKit.Ice, 0.1f) };
            box.back.pixelsPerUnitMultiplier = 3f;
            box.text = UiKit.Label(box.back.transform, UiKit.Display, 54, UiKit.Navy, TextAnchor.MiddleCenter, new Vector2(0f, 2f), new Vector2(BoxSize - 6f, BoxSize - 6f), true);
            box.text.fontStyle = FontStyle.Normal; // symbols read better upright
            box.glyph = Prompt.Make(box.back.transform, Vector2.zero, 46, Color.white, UiKit.Display);
            boxes[i] = box;
        }

        // The timing bar: a dark track outlined in white, filled in the main colour, the zone to hit in the hot one,
        // a thick white marker.
        bar = UiKit.Group(card.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, -62f));
        barBack = UiKit.Panel(bar, Vector2.zero, new Vector2(BarWidth + 10f, 36f), Color.white, Color.white, 0.25f);
        barTrack = UiKit.Panel(bar, Vector2.zero, new Vector2(BarWidth, 26f), Color.black, Color.black, 0.25f);
        barBack.sprite = barTrack.sprite = null;
        barFill = Left(UiKit.Panel(bar, Vector2.zero, new Vector2(BarWidth, 26f), UiKit.Sky, UiKit.Sky, 0.25f).rectTransform);
        barZone = Left(UiKit.Panel(bar, Vector2.zero, new Vector2(100f, 40f), UiKit.Gold, UiKit.Gold, 0.25f).rectTransform);
        barFill.GetComponent<Slanted>().sprite = barZone.GetComponent<Slanted>().sprite = null;
        barMarker = UiKit.Panel(bar, Vector2.zero, new Vector2(14f, 62f), Color.white, Color.white, 0.25f).rectTransform;
        barMarker.GetComponent<Slanted>().sprite = null;
        UiKit.Panel(barMarker, Vector2.zero, new Vector2(6f, 54f), UiKit.Navy, UiKit.Navy, 0.25f).sprite = null;
        cue = Prompt.Make(card.transform, new Vector2(0f, -124f), 46, Color.white, UiKit.Display);

        // Half dial: coloured blocks along an arc and a needle turning over them.
        dial = UiKit.Group(card.transform, new Vector2(0.5f, 0.5f), Vector2.zero);
        for (int i = 0; i < segments.Length; i++)
        {
            float angle = -90f + i * 6f, radians = angle * Mathf.Deg2Rad;
            Slanted block = UiKit.Panel(dial, DialCentre + new Vector2(Mathf.Sin(radians), Mathf.Cos(radians)) * DialRadius, new Vector2(24f, 56f), Color.white, Color.white, 0f);
            block.sprite = null;
            block.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -angle);
            segments[i] = block;
        }
        needle = UiKit.Panel(dial, DialCentre, new Vector2(12f, DialRadius + 30f), Color.white, Color.white, 0f).rectTransform;
        needle.pivot = new Vector2(0.5f, 0f);
        UiKit.Panel(dial, DialCentre, new Vector2(46f, 46f), Color.white, Color.white, 0f).sprite = UiKit.Disc();
        UiKit.Panel(dial, DialCentre, new Vector2(30f, 30f), UiKit.Navy, UiKit.Navy, 0f).sprite = UiKit.Disc();

        canvas.gameObject.SetActive(false);
    }

    // The biome's colours on the card, and its entrance: thrown in tilted and big, settling at a slight tilt.
    void Dress()
    {
        tone = UiKit.Biome(train ? train.BiomeId : "");
        cardShadow.color = cardShadow.bottom = tone.main;
        card.color = new Color(tone.deep.r * 1.6f + 0.03f, tone.deep.g * 1.6f + 0.03f, tone.deep.b * 1.6f + 0.05f, 0.96f);
        card.bottom = new Color(tone.deep.r, tone.deep.g, tone.deep.b, 0.96f);
        card.fade = true;
        dots.color = new Color(tone.main.r, tone.main.g, tone.main.b, 0.12f);
        titleTag.color = titleTag.bottom = tone.main;
        barTrack.color = barTrack.bottom = new Color(tone.deep.r, tone.deep.g, tone.deep.b, 1f);
        barFill.GetComponent<Slanted>().color = barFill.GetComponent<Slanted>().bottom = tone.main;
        barZone.GetComponent<Slanted>().color = barZone.GetComponent<Slanted>().bottom = tone.hot;
        float e = UiKit.Back((Time.unscaledTime - enteredAt) / 0.26f);
        swing.localScale = Vector3.one * Mathf.LerpUnclamped(1.3f, 1f, e);
        swing.localRotation = Quaternion.Euler(0f, 0f, Mathf.LerpUnclamped(-9f, -1.5f, e));
        swing.anchoredPosition = new Vector2(Mathf.LerpUnclamped(-500f, 0f, e), 0f);
    }

    // A box's symbol as a button glyph when it is one (PlayStation's, Xbox's, a shoulder, a letter key).
    static bool IsButton(string text) =>
        "✕○□△".Contains(text) && text.Length == 1 || text.Length == 2 && (text[0] == 'x' || ((text[0] == 'L' || text[0] == 'R') && "12BT".IndexOf(text[1]) >= 0)) ||
        text.Length == 1 && text[0] >= 'A' && text[0] <= 'Z';

    static RectTransform Left(RectTransform rect)
    {
        rect.pivot = new Vector2(0f, 0.5f);
        return rect;
    }

    // Call first, every frame the panel should be up. count: how many boxes the row has (0 for none).
    public void Show(string heading, int count)
    {
        shownFrame = cardFrame = Time.frameCount;
        if (!canvas.gameObject.activeSelf || !cardRoot.activeSelf) enteredAt = Time.unscaledTime;
        if (!canvas.gameObject.activeSelf) canvas.gameObject.SetActive(true);
        if (!cardRoot.activeSelf) cardRoot.SetActive(true);
        Dress();
        title.Set(Prompt.Upper(heading));
        count = Mathf.Min(count, MaxBoxes);
        float total = count * (BoxSize + BoxGap) - BoxGap;
        for (int i = 0; i < MaxBoxes; i++)
        {
            boxes[i].back.gameObject.SetActive(i < count);
            boxes[i].back.rectTransform.anchoredPosition = new Vector2(-total * 0.5f + BoxSize * 0.5f + i * (BoxSize + BoxGap), 34f);
        }
        bar.gameObject.SetActive(false);
        dial.gameObject.SetActive(false);
        cue.Set("");
    }

    // state: 0 to come, 1 done right, 2 failed, 3 the one to do now, 4 destroyed outside this attempt.
    public void Box(int i, string text, int state)
    {
        if (i >= MaxBoxes) return;
        Cell box = boxes[i];
        float now = Time.unscaledTime;
        if (box.state != state) { box.state = state; box.changed = now; }
        bool button = state != 4 && IsButton(text);
        box.text.text = state == 4 ? "X" : button ? "" : text;
        box.glyph.Set(button ? "[" + text + "]" : "");
        // To come: pale; now: the hot colour, big, beating; right: green with a pop (never the hero's colour, which
        // may be red); wrong: red, shaking.
        var wrong = new Color(1f, 0.25f, 0.28f);
        box.back.color = box.back.bottom = state == 4 ? UiKit.Slate : state == 1 ? UiKit.Mint :state == 2 ? wrong : state == 3 ? tone.hot : new Color(1f, 1f, 1f, 0.9f);
        box.text.color = state == 0 || state == 3 ? tone.deep : Color.white;
        float since = now - box.changed;
        float size = state == 3 ? 1.22f + Mathf.Sin(now * 9f) * 0.05f : state == 1 ? 1f + 0.35f * Mathf.Clamp01(1f - since / 0.18f) : state == 0 ? 0.92f : 1f;
        box.back.rectTransform.localScale = Vector3.one * size;
        box.back.rectTransform.localRotation = Quaternion.Euler(0f, 0f, state == 2 ? Mathf.Sin(now * 60f) * 8f * Mathf.Clamp01(1f - since / 0.35f) : 0f);
        if (state == 3) box.back.transform.SetAsLastSibling();
    }

    // A marker running left to right; the green stretch (zoneFrom..zoneTo, as fractions) is where the press counts.
    public void Bar(float fraction, float zoneFrom = 0f, float zoneTo = 0f)
    {
        fraction = Mathf.Clamp01(fraction);
        bar.gameObject.SetActive(true);
        barFill.anchoredPosition = new Vector2(-BarWidth * 0.5f, 0f);
        barFill.sizeDelta = new Vector2(Mathf.Max(BarWidth * fraction, 14f), 26f);
        barZone.gameObject.SetActive(zoneTo > zoneFrom);
        barZone.anchoredPosition = new Vector2(-BarWidth * 0.5f + BarWidth * zoneFrom, 0f);
        barZone.sizeDelta = new Vector2(BarWidth * (zoneTo - zoneFrom), 40f);
        barMarker.anchoredPosition = new Vector2(-BarWidth * 0.5f + BarWidth * fraction, 0f);
        barMarker.SetAsLastSibling();
    }

    // The line under the bar; buttons in it between brackets are drawn ([R2], [Espacio]).
    public void Cue(string text, bool hot)
    {
        cue.Set(text);
        cue.transform.localScale = Vector3.one * (hot ? 1.15f + Mathf.Sin(Time.unscaledTime * 12f) * 0.05f : 1f);
    }

    // angle: needle, -90 to 90. green / yellow: half widths of those zones; beyond yellow is red.
    public void Dial(float angle, float green, float yellow)
    {
        dial.gameObject.SetActive(true);
        for (int i = 0; i < segments.Length; i++)
        {
            float off = Mathf.Abs(-90f + i * 6f);
            segments[i].color = off <= green ? tone.main : off <= yellow ? tone.hot : new Color(0.3f, 0.3f, 0.36f);
        }
        needle.localRotation = Quaternion.Euler(0f, 0f, -angle);
    }

    // A label floating over a spot of the world; call for each one, every frame. With no Show() the card stays away.
    // state: 0 to come, 1 done right, 2 failed, 3 the one to do next, 4 the one to do NOW.
    public void Mark(Vector3 world, string text, int state)
    {
        if (marksFrame != Time.frameCount)
        {
            marksFrame = Time.frameCount;
            marksUsed = 0;
            if (cardFrame != Time.frameCount && cardRoot.activeSelf) cardRoot.SetActive(false);
        }
        shownFrame = Time.frameCount;
        if (!canvas.gameObject.activeSelf) canvas.gameObject.SetActive(true);
        if (marksUsed == marks.Count)
        {
            Slanted back = UiKit.Panel(canvas.transform, Vector2.zero, new Vector2(96f, 82f), UiKit.Navy, UiKit.Navy, 0.1f);
            back.rectTransform.anchorMin = back.rectTransform.anchorMax = Vector2.zero;
            Text label = UiKit.Label(back.transform, UiKit.Display, 62, UiKit.Gold, TextAnchor.MiddleCenter, new Vector2(0f, 2f), new Vector2(88f, 76f), true);
            label.fontStyle = FontStyle.Normal;
            marks.Add((back.rectTransform, back, label));
        }
        var mark = marks[marksUsed++];
        mark.rect.gameObject.SetActive(true);
        Vector3 screen = Camera.main.WorldToScreenPoint(world);
        mark.rect.anchoredPosition = new Vector2(screen.x, screen.y) / canvas.scaleFactor + Vector2.up * 40f;
        mark.text.text = text;
        bool current = state >= 3;
        mark.rect.localScale = Vector3.one * (state == 4 ? 1.5f + Mathf.PingPong(Time.unscaledTime * 4f, 0.25f) : state == 3 ? 1.25f : 0.8f);
        Color ink = state == 1 || state == 4 ? UiKit.Mint : state == 2 ? UiKit.Rose : state == 3 ? UiKit.Gold : Color.white;
        mark.text.color = ink;
        float see = current ? 0.92f : 0.55f; // the ones not up yet stay in the background
        mark.back.color = mark.back.bottom = new Color(UiKit.Navy.r, UiKit.Navy.g, UiKit.Navy.b, see);
        if (current) mark.rect.SetAsLastSibling();
        if (state == 5) // an alert: small, flashing red and yellow (the truck's bombs)
        {
            bool red = Mathf.Repeat(Time.unscaledTime * 5f, 1f) < 0.5f;
            mark.back.color = mark.back.bottom = red ? new Color(0.95f, 0.15f, 0.1f, 0.95f) : new Color(1f, 0.85f, 0.1f, 0.95f);
            mark.text.color = red ? new Color(1f, 0.9f, 0.2f) : new Color(0.85f, 0.1f, 0.05f);
            mark.rect.localScale = Vector3.one * 0.55f;
        }
    }

    void LateUpdate()
    {
        if (canvas.gameObject.activeSelf && Time.frameCount - shownFrame > 1) canvas.gameObject.SetActive(false);
        // Marks nobody asked for this frame (or the last, whoever's LateUpdate ran first) go away.
        int keep = Time.frameCount - marksFrame > 1 ? 0 : marksUsed;
        for (int i = keep; i < marks.Count; i++)
            if (marks[i].rect.gameObject.activeSelf) marks[i].rect.gameObject.SetActive(false);
    }
}
