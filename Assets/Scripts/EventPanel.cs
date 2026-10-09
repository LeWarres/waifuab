using UnityEngine;
using UnityEngine.UI;

// The card the button games are played on (curve sequence, ramp jump, needle jump, zigzag): a title,
// a row of boxes, a timing bar with an optional green zone, a line of text, or a half dial with a needle.
// Whoever is using it calls Show() every frame and then fills in what it needs; it hides by itself
// as soon as nobody does.
public class EventPanel : MonoBehaviour
{
    class Cell
    {
        public Slanted back;
        public Text text;
    }

    const int MaxBoxes = 12;
    const float BoxSize = 84f, BoxGap = 12f, BarWidth = 900f, DialRadius = 190f;
    static readonly Vector2 DialCentre = new Vector2(0f, -120f);

    Canvas canvas;
    Text title, cue, tagText;
    readonly Cell[] boxes = new Cell[MaxBoxes];
    RectTransform bar, barFill, barZone, barMarker, dial, needle, tag;
    readonly Image[] segments = new Image[31];
    Slanted tagBack;
    int shownFrame = -10;

    void Awake()
    {
        canvas = UiKit.NewCanvas("EventPanel", 5);
        RectTransform root = UiKit.Group(canvas.transform, new Vector2(0.5f, 0f), new Vector2(0f, 230f));
        var shade = new Color(0.05f, 0.1f, 0.25f, 0.35f);
        UiKit.Panel(root, new Vector2(10f, -14f), new Vector2(1240f, 340f), shade, shade, 0.08f);
        Slanted card = UiKit.Panel(root, Vector2.zero, new Vector2(1240f, 340f), UiKit.White, UiKit.Ice, 0.08f);
        UiKit.Panel(card.transform, new Vector2(-602f, 0f), new Vector2(18f, 340f), UiKit.Sky, UiKit.Sky, 0.08f);
        title = UiKit.Label(card.transform, UiKit.Heading, 42, UiKit.Navy, TextAnchor.MiddleCenter, new Vector2(8f, 120f), new Vector2(1120f, 64f), true);

        for (int i = 0; i < MaxBoxes; i++)
        {
            var box = new Cell { back = UiKit.Panel(card.transform, Vector2.zero, new Vector2(BoxSize, BoxSize), UiKit.Ice, UiKit.Ice, 0.1f) };
            box.text = UiKit.Label(box.back.transform, UiKit.Heading, 46, UiKit.Navy, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(BoxSize - 6f, BoxSize - 6f), true);
            box.text.fontStyle = FontStyle.Normal; // symbols read better upright
            boxes[i] = box;
        }

        var dark = new Color(0.1f, 0.14f, 0.25f, 0.55f);
        bar = UiKit.Panel(card.transform, new Vector2(0f, -62f), new Vector2(BarWidth, 26f), dark, dark, 0.25f).rectTransform;
        barFill = Left(UiKit.Panel(bar, Vector2.zero, new Vector2(BarWidth, 26f), UiKit.Sky, UiKit.Sky, 0.25f).rectTransform);
        barZone = Left(UiKit.Panel(bar, Vector2.zero, new Vector2(100f, 40f), new Color(UiKit.Mint.r, UiKit.Mint.g, UiKit.Mint.b, 0.85f), UiKit.Mint, 0.25f).rectTransform);
        barMarker = UiKit.Panel(bar, Vector2.zero, new Vector2(10f, 54f), UiKit.Navy, UiKit.Navy, 0.25f).rectTransform;
        cue = UiKit.Label(card.transform, UiKit.Heading, 46, UiKit.Navy, TextAnchor.MiddleCenter, new Vector2(0f, -124f), new Vector2(1120f, 62f), true);

        // Half dial: coloured blocks along an arc and a needle turning over them.
        dial = UiKit.Group(card.transform, new Vector2(0.5f, 0.5f), Vector2.zero);
        for (int i = 0; i < segments.Length; i++)
        {
            float angle = -90f + i * 6f, radians = angle * Mathf.Deg2Rad;
            Slanted block = UiKit.Panel(dial, DialCentre + new Vector2(Mathf.Sin(radians), Mathf.Cos(radians)) * DialRadius, new Vector2(24f, 56f), Color.white, Color.white, 0f);
            block.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -angle);
            segments[i] = block;
        }
        needle = UiKit.Panel(dial, DialCentre, new Vector2(12f, DialRadius + 30f), UiKit.Navy, UiKit.Navy, 0f).rectTransform;
        needle.pivot = new Vector2(0.5f, 0f);
        UiKit.Panel(dial, DialCentre, new Vector2(40f, 40f), UiKit.Navy, UiKit.Navy, 0f);

        // Floating tag for a spot in the world (the zigzag's next turn).
        tagBack = UiKit.Panel(canvas.transform, Vector2.zero, new Vector2(110f, 92f), new Color(UiKit.Navy.r, UiKit.Navy.g, UiKit.Navy.b, 0.9f), UiKit.Navy, 0.1f);
        tag = tagBack.rectTransform;
        tag.anchorMin = tag.anchorMax = Vector2.zero;
        tagText = UiKit.Label(tag, UiKit.Heading, 64, UiKit.Gold, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(100f, 86f), true);
        tagText.fontStyle = FontStyle.Normal;

        canvas.gameObject.SetActive(false);
    }

    static RectTransform Left(RectTransform rect)
    {
        rect.pivot = new Vector2(0f, 0.5f);
        return rect;
    }

    // Call first, every frame the panel should be up. count: how many boxes the row has (0 for none).
    public void Show(string heading, int count)
    {
        shownFrame = Time.frameCount;
        if (!canvas.gameObject.activeSelf) canvas.gameObject.SetActive(true);
        title.text = heading;
        count = Mathf.Min(count, MaxBoxes);
        float total = count * (BoxSize + BoxGap) - BoxGap;
        for (int i = 0; i < MaxBoxes; i++)
        {
            boxes[i].back.gameObject.SetActive(i < count);
            boxes[i].back.rectTransform.anchoredPosition = new Vector2(-total * 0.5f + BoxSize * 0.5f + i * (BoxSize + BoxGap), 28f);
        }
        bar.gameObject.SetActive(false);
        dial.gameObject.SetActive(false);
        tag.gameObject.SetActive(false);
        cue.text = "";
    }

    // state: 0 to come, 1 done right, 2 failed, 3 the one to do now.
    public void Box(int i, string text, int state)
    {
        if (i >= MaxBoxes) return;
        Cell box = boxes[i];
        box.text.text = text;
        box.back.color = box.back.bottom = state == 1 ? UiKit.Mint : state == 2 ? UiKit.Rose : state == 3 ? UiKit.Gold : UiKit.Ice;
        box.text.color = state == 1 || state == 2 ? Color.white : UiKit.Navy;
        box.back.rectTransform.localScale = Vector3.one * (state == 3 ? 1.18f : 1f);
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
    }

    public void Cue(string text, bool hot)
    {
        cue.text = text;
        cue.color = hot ? new Color(0.1f, 0.65f, 0.3f) : UiKit.Navy;
    }

    // angle: needle, -90 to 90. green / yellow: half widths of those zones; beyond yellow is red.
    public void Dial(float angle, float green, float yellow)
    {
        dial.gameObject.SetActive(true);
        for (int i = 0; i < segments.Length; i++)
        {
            float off = Mathf.Abs(-90f + i * 6f);
            segments[i].color = off <= green ? UiKit.Mint : off <= yellow ? UiKit.Gold : UiKit.Rose;
        }
        needle.localRotation = Quaternion.Euler(0f, 0f, -angle);
    }

    public void Tag(Vector3 world, string text, bool hot)
    {
        tag.gameObject.SetActive(true);
        Vector3 screen = Camera.main.WorldToScreenPoint(world);
        tag.anchoredPosition = new Vector2(screen.x, screen.y) / canvas.scaleFactor + Vector2.up * 50f;
        tagText.text = text;
        tagText.color = hot ? UiKit.Mint : UiKit.Gold;
    }

    void LateUpdate()
    {
        if (canvas.gameObject.activeSelf && Time.frameCount - shownFrame > 1) canvas.gameObject.SetActive(false);
    }
}
