using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// A chest being opened (TrainSim.OpenChest), Vampire Survivors style. The game stops; the chest shakes and glows
// with rays in its colour (blue: one prize, gold: three, every colour: five), bursts open in a flash, and its prizes
// come out one by one. Confirm (Space, Enter, A, a click) hurries it along, then closes it.
public class Chest : MonoBehaviour
{
    public struct Prize { public string icon, label; public bool evolution; }

    const float Shaking = 1.6f, Between = 0.5f;
    Canvas canvas;
    RectTransform chest, rays;
    readonly List<Slanted> beams = new();
    Slanted flash;
    Text title, hint;
    readonly List<(RectTransform rect, RawImage icon, Text label, Slanted glow)> slots = new();
    readonly Queue<List<Prize>> queue = new();
    List<Prize> showing;
    float since, timeBefore = 1f;

    public bool Open => showing != null;

    public void Show(List<Prize> prizes)
    {
        queue.Enqueue(prizes);
        if (showing == null) Next();
    }

    void Next()
    {
        if (!canvas) Build();
        showing = queue.Dequeue();
        since = 0f;
        if (Time.timeScale > 0f) timeBefore = Time.timeScale;
        Time.timeScale = 0f;
        canvas.gameObject.SetActive(true);
        chest.gameObject.SetActive(true);
        title.text = L10n.T("chest.opening");
        hint.text = "";
        for (int i = 0; i < slots.Count; i++) slots[i].rect.gameObject.SetActive(false);
        Sound.Play("Pause");
    }

    public void Close()
    {
        if (showing == null) return;
        showing = null;
        if (queue.Count > 0) { Next(); return; }
        canvas.gameObject.SetActive(false);
        Time.timeScale = timeBefore;
    }

    void Build()
    {
        canvas = UiKit.NewCanvas("Chest", 25);
        Transform root = canvas.transform;
        var shade = new Color(0.02f, 0.03f, 0.08f, 0.82f);
        UiKit.Panel(root, Vector2.zero, new Vector2(2600f, 1500f), shade, shade, 0f);
        rays = UiKit.Group(root, new Vector2(0.5f, 0.5f), new Vector2(0f, 40f));
        for (int i = 0; i < 16; i++)
        {
            Slanted beam = UiKit.Panel(rays, Vector2.zero, new Vector2(46f, 1500f), Color.white, new Color(1f, 1f, 1f, 0f), 0f);
            beam.rectTransform.pivot = new Vector2(0.5f, 0f);
            beam.rectTransform.localRotation = Quaternion.Euler(0f, 0f, i * 22.5f);
            beams.Add(beam);
        }
        chest = UiKit.Picture(root, "chest", new Vector2(0f, 40f), 300f).rectTransform;
        title = UiKit.Label(root, UiKit.Heading, 56, Color.white, TextAnchor.MiddleCenter, new Vector2(0f, 380f), new Vector2(1600f, 80f), true);
        hint = UiKit.Label(root, UiKit.Body, 24, new Color(0.85f, 0.9f, 1f, 0.85f), TextAnchor.MiddleCenter, new Vector2(0f, -430f), new Vector2(1600f, 40f), true);
        for (int i = 0; i < 5; i++)
        {
            RectTransform slot = UiKit.Group(root, new Vector2(0.5f, 0.5f), Vector2.zero);
            Slanted glow = UiKit.Panel(slot, Vector2.zero, new Vector2(190f, 190f), new Color(1f, 1f, 1f, 0.25f), new Color(1f, 1f, 1f, 0.05f), 0.08f);
            RawImage icon = UiKit.Picture(slot, null, Vector2.zero, 150f);
            Text label = UiKit.Label(slot, UiKit.Heading, 24, Color.white, TextAnchor.UpperCenter, new Vector2(0f, -150f), new Vector2(260f, 80f), true);
            slots.Add((slot, icon, label, glow));
            slot.gameObject.SetActive(false);
        }
        flash = UiKit.Panel(root, Vector2.zero, new Vector2(2600f, 1500f), Color.white, Color.white, 0f);
        flash.color = new Color(1f, 1f, 1f, 0f);
        canvas.gameObject.SetActive(false);
    }

    // The chest's colour: what it holds, told before it opens.
    Color Tone(float t) => showing.Count >= 5 ? Color.HSVToRGB(Mathf.Repeat(t * 0.6f, 1f), 0.75f, 1f)
                         : showing.Count >= 3 ? UiKit.Gold : UiKit.Sky;

    void Update()
    {
        if (showing == null) return;
        since += Time.unscaledDeltaTime;
        bool next = (Keyboard.current != null && (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame))
                    || (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame)
                    || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);
        float done = Shaking + Between * showing.Count;
        if (next) { if (since >= done) { Close(); return; } since = Mathf.Max(since, done); } // hurry along, then close

        // The rays: in the chest's colour, turning, stronger as it shakes, then a full blaze once open.
        Color tone = Tone(since);
        float strength = since < Shaking ? since / Shaking * 0.55f : 0.8f;
        rays.localRotation = Quaternion.Euler(0f, 0f, since * 25f);
        rays.localScale = Vector3.one * (since < Shaking ? 0.6f + 0.4f * since / Shaking : 1.1f + 0.05f * Mathf.Sin(since * 4f));
        foreach (Slanted beam in beams)
        {
            beam.color = new Color(tone.r, tone.g, tone.b, strength);
            beam.bottom = new Color(tone.r, tone.g, tone.b, 0f);
        }

        if (since < Shaking)
        {
            // Shaking harder and harder, swelling a little.
            float k = since / Shaking;
            chest.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(since * 45f) * 12f * k);
            chest.localScale = Vector3.one * (1f + 0.25f * k + 0.04f * Mathf.Sin(since * 30f));
            flash.color = new Color(1f, 1f, 1f, 0f);
            return;
        }
        if (chest.gameObject.activeSelf)
        {
            chest.gameObject.SetActive(false);
            Sound.Play("wagongood");
            title.text = L10n.T(showing.Count >= 5 ? "chest.legendary" : showing.Count >= 3 ? "chest.rare" : "chest.common");
        }
        title.color = tone;
        flash.color = new Color(1f, 1f, 1f, Mathf.Clamp01(1f - (since - Shaking) / 0.45f));

        // The prizes, one by one, in a row: each pops out a little too big, then settles.
        for (int i = 0; i < showing.Count; i++)
        {
            float t = since - Shaking - Between * i;
            var slot = slots[i];
            if (t < 0f) continue;
            if (!slot.rect.gameObject.activeSelf)
            {
                slot.rect.gameObject.SetActive(true);
                slot.rect.anchoredPosition = new Vector2((i - (showing.Count - 1) * 0.5f) * 280f, -90f);
                UiKit.SetIcon(slot.icon, showing[i].icon);
                slot.label.text = showing[i].label;
                slot.label.color = showing[i].evolution ? UiKit.Gold : Color.white;
                Sound.Play(showing[i].evolution ? "healthup" : "newcard");

            }
            float pop = t < 0.25f ? Mathf.Lerp(0.2f, 1.25f, t / 0.25f) : Mathf.Lerp(1.25f, 1f, Mathf.Clamp01((t - 0.25f) / 0.2f));
            slot.rect.localScale = Vector3.one * pop;
            Color glow = showing[i].evolution ? UiKit.Gold : tone;
            slot.glow.color = new Color(glow.r, glow.g, glow.b, 0.35f + 0.15f * Mathf.Sin(since * 5f + i));
            slot.glow.rectTransform.localRotation = Quaternion.Euler(0f, 0f, since * 40f + i * 20f);
        }
        if (since >= done) hint.text = L10n.T("chest.continue");
    }
}
