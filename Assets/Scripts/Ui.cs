using UnityEngine;

// Shared look of the HUD and menus. Everything is laid out on a virtual screen 540 units
// tall and scaled to the real resolution, so text and buttons stay readable at any size.
// ponytail: still IMGUI; move to a UI canvas when the game needs animation or art.
public static class Ui
{
    public static GUIStyle Title, Text, Small, Money, Normal, Focused, Disabled;
    static Texture2D panel, normal, hover, focus, disabled, white;
    static int builtFor;

    public static float Scale => Screen.height / 540f;
    public static float Width => Screen.width / Scale;

    // Call first in every OnGUI: (re)builds the styles when the resolution changes.
    public static void Begin()
    {
        if (builtFor == Screen.height && panel) return;
        builtFor = Screen.height;
        panel = Tex(new Color(0.05f, 0.06f, 0.1f, 0.88f));
        normal = Tex(new Color(0.22f, 0.25f, 0.32f));
        hover = Tex(new Color(0.32f, 0.36f, 0.45f));
        focus = Tex(new Color(1f, 0.82f, 0.1f));
        disabled = Tex(new Color(0.15f, 0.15f, 0.17f));
        white = Texture2D.whiteTexture;

        Title = Style(18, Color.white, FontStyle.Bold, TextAnchor.MiddleCenter);
        Text = Style(13, Color.white, FontStyle.Bold, TextAnchor.MiddleLeft);
        Small = Style(11, new Color(0.8f, 0.85f, 0.9f), FontStyle.Normal, TextAnchor.MiddleCenter);
        Money = Style(15, new Color(1f, 0.85f, 0.2f), FontStyle.Bold, TextAnchor.MiddleRight);
        Normal = Style(12, Color.white, FontStyle.Bold, TextAnchor.MiddleCenter, normal, hover);
        Focused = Style(12, Color.black, FontStyle.Bold, TextAnchor.MiddleCenter, focus, focus);
        Disabled = Style(12, new Color(0.45f, 0.45f, 0.45f), FontStyle.Normal, TextAnchor.MiddleCenter, disabled, disabled);
    }

    static Texture2D Tex(Color color)
    {
        var t = new Texture2D(1, 1);
        t.SetPixel(0, 0, color);
        t.Apply();
        return t;
    }

    static GUIStyle Style(int size, Color color, FontStyle font, TextAnchor anchor, Texture2D background = null, Texture2D over = null)
    {
        var s = new GUIStyle
        {
            fontSize = Mathf.RoundToInt(size * Scale), fontStyle = font, alignment = anchor, wordWrap = true,
            padding = new RectOffset(4, 4, 2, 2)
        };
        s.normal.textColor = s.hover.textColor = s.active.textColor = color;
        s.normal.background = background;
        s.hover.background = s.active.background = over;
        return s;
    }

    // Virtual units to real pixels.
    public static Rect R(float x, float y, float w, float h) => new Rect(x * Scale, y * Scale, w * Scale, h * Scale);

    // World position to virtual screen units.
    public static Vector2 Point(Vector3 world)
    {
        Vector3 s = Camera.main.WorldToScreenPoint(world);
        return new Vector2(s.x / Scale, (Screen.height - s.y) / Scale);
    }

    public static void Panel(Rect rect) => GUI.DrawTexture(rect, panel);

    // The focused button is yellow with a white frame, so it stands out with or without a mouse.
    public static bool Button(Rect rect, string label, bool focused, bool enabled = true)
    {
        if (focused)
        {
            float b = 3f * Scale;
            GUI.DrawTexture(new Rect(rect.x - b, rect.y - b, rect.width + b * 2f, rect.height + b * 2f), white);
        }
        return GUI.Button(rect, label, !enabled ? Disabled : focused ? Focused : Normal) && enabled;
    }

    public static void Progress(Rect rect, float fraction)
    {
        GUI.color = Color.black;
        GUI.DrawTexture(rect, white);
        rect.width *= Mathf.Clamp01(fraction);
        GUI.color = new Color(0.4f, 0.75f, 1f);
        GUI.DrawTexture(rect, white);
        GUI.color = Color.white;
    }

    public static void Bar(Rect rect, float fraction)
    {
        fraction = Mathf.Clamp01(fraction);
        GUI.color = Color.black;
        GUI.DrawTexture(rect, white);
        rect.width *= fraction;
        GUI.color = Color.Lerp(Color.red, Color.green, fraction);
        GUI.DrawTexture(rect, white);
        GUI.color = Color.white;
    }
}
