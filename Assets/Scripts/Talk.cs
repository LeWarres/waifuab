using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// A few lines between the hero and a rival, said over the game, in the story scene's dialogue box (dark blue,
// a light line under the speaker's name, white text typed out), one line at a time. Confirm (Space, Enter, A, a click) shows the rest of the line or the
// next one. The caller stops the game while it lasts and gets the done callback after the last line.
public class Talk : MonoBehaviour
{
    Canvas canvas;
    Text who, what, hint;
    Slanted edge;
    string[] names, lines;
    bool[] rival;
    int at;
    float since;
    System.Action done, finished; // finished: done, held back a frame so the last press is not taken twice

    public bool Open => canvas && canvas.gameObject.activeSelf;

    // names[i] says lines[i]; rival[i]: that one is the other side (its name in the rival's colour).
    public void Show(string[] names, string[] lines, bool[] rival, System.Action done)
    {
        if (!canvas) Build();
        this.names = names;
        this.lines = lines;
        this.rival = rival;
        this.done = done;
        at = 0;
        since = 0f;
        canvas.gameObject.SetActive(true);
    }

    // The same box as StoryIntro.BuildUI's, without its buttons.
    void Build()
    {
        canvas = UiKit.NewCanvas("Talk", 30);
        canvas.GetComponent<CanvasScaler>().matchWidthOrHeight = 0.5f;
        RectTransform dialog = UiKit.Group(canvas.transform, new Vector2(0.5f, 0f), new Vector2(0f, 150f));
        Color deep = new Color(0.035f, 0.09f, 0.19f, 0.96f);
        UiKit.Panel(dialog, Vector2.zero, new Vector2(1780f, 230f), deep, deep, 0.035f);
        edge = UiKit.Panel(dialog, new Vector2(0f, 113f), new Vector2(1670f, 3f), UiKit.Sky, UiKit.Sky, 0f);
        who = Label(dialog, 21, new Vector2(-675f, 75f), new Vector2(290f, 38f), UiKit.Sky);
        what = Label(dialog, 31, new Vector2(0f, -8f), new Vector2(1640f, 120f), Color.white);
        what.alignment = TextAnchor.UpperLeft;
        what.resizeTextForBestFit = false;
        hint = Label(canvas.transform, 18, new Vector2(0f, 23f), new Vector2(1600f, 36f), Color.white);
        hint.rectTransform.anchorMin = hint.rectTransform.anchorMax = new Vector2(0.5f, 0f);
        hint.alignment = TextAnchor.MiddleCenter;
        canvas.gameObject.SetActive(false);
    }

    static Text Label(Transform parent, int size, Vector2 position, Vector2 box, Color color)
    {
        Text text = UiKit.Label(parent, UiKit.Body, size, color, TextAnchor.MiddleLeft, position, box, true);
        text.fontStyle = FontStyle.Normal;
        return text;
    }

    void Update()
    {
        if (finished != null) { System.Action call = finished; finished = null; call(); return; }
        if (!Open) return;
        since += Time.unscaledDeltaTime;
        string line = lines[at];
        int shown = Mathf.Min(line.Length, Mathf.FloorToInt(since * 40f));
        Color tone = rival[at] ? UiKit.Rose : UiKit.Sky;
        who.text = names[at].ToUpper();
        who.color = tone;
        edge.color = edge.bottom = tone;
        what.text = line.Substring(0, shown);
        hint.text = L10n.T("talk.help");
        bool next = (Keyboard.current != null && (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame))
            || (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame)
            || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);
        if (!next) return;
        Sound.Play("confirm");
        if (shown < line.Length) since = 999f; // the whole line at once
        else Advance();
    }

    // On to the next line, or done after the last.
    public void Advance()
    {
        if (!Open) return;
        if (++at < lines.Length) { since = 0f; return; }
        canvas.gameObject.SetActive(false);
        finished = done ?? (() => { });
    }
}
