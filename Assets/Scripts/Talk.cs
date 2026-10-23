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
    Slanted edge, nameTag, boxShadow;
    Prompt hintLine;
    RectTransform more; // the arrow that says there is more, once the line is all out
    string[] names, lines;
    bool[] rival;
    int at;
    float since;
    System.Action done, finished; // finished: done, held back a frame so the last press is not taken twice

    public bool Open => canvas && canvas.gameObject.activeSelf;
    public bool Finishing => finished != null;

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
        // Persona style: a dark plate tilted a touch, outlined in white on a hard shadow of the biome's colour, the
        // speaker's name on a tag breaking out of its top left.
        RectTransform dialog = UiKit.Group(canvas.transform, new Vector2(0.5f, 0f), new Vector2(0f, 150f));
        dialog.localRotation = Quaternion.Euler(0f, 0f, 0.8f);
        Slanted box = UiKit.Plate(dialog, Vector2.zero, new Vector2(1780f, 230f), 0.035f, 6f, 14f);
        boxShadow = UiKit.Root(box).GetComponent<Slanted>();
        Destroy(boxShadow.GetComponent<Themed>()); // in the speaker's colour instead
        edge = UiKit.Panel(box.transform, new Vector2(0f, -100f), new Vector2(1740f, 6f), UiKit.Sky, UiKit.Sky, 0.035f);
        edge.sprite = null;
        nameTag = UiKit.Panel(dialog, new Vector2(-640f, 122f), new Vector2(420f, 64f), UiKit.Sky, UiKit.Sky, 0.25f);
        nameTag.sprite = null;
        nameTag.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 3f);
        who = UiKit.Label(nameTag.transform, UiKit.Display, 40, Color.white, TextAnchor.MiddleCenter, new Vector2(0f, 2f), new Vector2(400f, 60f), true);
        what = Label(dialog, 31, new Vector2(0f, -8f), new Vector2(1640f, 120f), Color.white);
        more = UiKit.Label(dialog, UiKit.Display, 44, Color.white, TextAnchor.MiddleCenter, new Vector2(840f, -80f), new Vector2(60f, 60f)).rectTransform;
        more.GetComponent<Text>().text = "▼";
        what.alignment = TextAnchor.UpperLeft;
        what.resizeTextForBestFit = false;
        hint = Label(canvas.transform, 18, new Vector2(0f, 23f), new Vector2(1600f, 36f), Color.white);
        hint.rectTransform.anchorMin = hint.rectTransform.anchorMax = new Vector2(0.5f, 0f);
        hint.alignment = TextAnchor.MiddleCenter;
        hint.enabled = false;
        hintLine = Prompt.Make(hint.transform, Vector2.zero, 18, Color.white); // the button drawn
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
        // Each hero's lines in their own colour; anyone else's on red (a rival) or the biome's.
        Color tone = UiKit.Speaker(names[at], rival[at]);
        who.text = names[at].ToUpper();
        nameTag.color = nameTag.bottom = edge.color = edge.bottom = boxShadow.color = boxShadow.bottom = tone;
        what.text = line.Substring(0, shown);
        hintLine.Set(L10n.T("talk.help", UiKit.ConfirmGlyph(Gamepad.current != null)));
        more.gameObject.SetActive(shown >= line.Length);
        more.anchoredPosition = new Vector2(840f, -80f + Mathf.Abs(Mathf.Sin(Time.unscaledTime * 6f)) * 10f);
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
