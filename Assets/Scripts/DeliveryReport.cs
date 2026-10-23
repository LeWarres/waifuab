using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// The station receipt, shown once after unloading and before choosing upgrades.
public class DeliveryReport : MonoBehaviour
{
    public struct Entry
    {
        public string icon, name;
        public int value;
        public bool destroyed;
    }

    Canvas canvas;
    System.Action completed;
    float openedAt;
    bool open;
    public bool Visible => open && canvas && canvas.gameObject.activeSelf;

    // The show, Persona style: a band of the biome's colour sweeps across, the title slams in tilted, the receipt
    // slides in and its rows after it, the total counts up huge beside it, then a stamp comes down on the receipt.
    // Everything that moves: where it rests, where it comes from, from what size, when and for how long.
    readonly List<(RectTransform rect, CanvasGroup fade, Vector2 rest, Vector2 from, float scale, float at, float length)> moves = new();
    RectTransform shaker, stamp;
    Text totalText;
    int total, shownTotal = int.MinValue;
    float countAt, stampAt, endsAt;
    bool stamped;

    public void Show(IList<Entry> entries, int total, int balance, System.Action done)
    {
        if (canvas) Destroy(canvas.gameObject);
        canvas = UiKit.NewCanvas("DeliveryReport", 12);
        completed = done;
        openedAt = Time.unscaledTime;
        open = true;
        moves.Clear();
        this.total = total;
        shownTotal = int.MinValue;
        stamped = false;
        var train = GetComponent<TrainSim>();
        UiKit.Tones tone = UiKit.Biome(train ? train.BiomeId : "");
        var paper = new Color(1f, 0.985f, 0.94f);
        var ink = new Color(0.07f, 0.09f, 0.18f);
        var red = new Color(0.86f, 0.1f, 0.16f);

        var veil = new GameObject("Dim", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        veil.transform.SetParent(canvas.transform, false);
        veil.color = new Color(tone.deep.r, tone.deep.g, tone.deep.b, 0.9f);
        veil.rectTransform.anchorMin = Vector2.zero;
        veil.rectTransform.anchorMax = Vector2.one;
        veil.rectTransform.sizeDelta = Vector2.zero;
        shaker = UiKit.Group(canvas.transform, new Vector2(0.5f, 0.5f), Vector2.zero);

        // The band across, tilted, with dots and two white stripes along it.
        RectTransform band = UiKit.Group(shaker, new Vector2(0.5f, 0.5f), new Vector2(0f, 20f));
        band.localRotation = Quaternion.Euler(0f, 0f, 10f);
        UiKit.Panel(band, Vector2.zero, new Vector2(3400f, 560f), tone.main, tone.main, 0f).sprite = null;
        Slanted dots = UiKit.Panel(band, Vector2.zero, new Vector2(3400f, 560f), new Color(tone.deep.r, tone.deep.g, tone.deep.b, 0.22f), new Color(tone.deep.r, tone.deep.g, tone.deep.b, 0.22f), 0f);
        dots.sprite = UiKit.Halftone();
        dots.type = Image.Type.Tiled;
        foreach (float y in new[] { 300f, -300f }) UiKit.Panel(band, new Vector2(0f, y), new Vector2(3400f, 16f), Color.white, Color.white, 0f).sprite = null;
        Move(band, 0f, new Vector2(-3400f, 0f), 1f, 0.28f);

        // The title, huge and tilted, on a hard shadow.
        RectTransform head = UiKit.Group(shaker, new Vector2(0.5f, 0.5f), new Vector2(-250f, 390f));
        head.localRotation = Quaternion.Euler(0f, 0f, 5f);
        string heading = L10n.T("delivery.title").ToUpperInvariant();
        UiKit.Label(head, UiKit.Display, 118, tone.deep, TextAnchor.MiddleLeft, new Vector2(10f, -10f), new Vector2(1300f, 150f), true).text = heading;
        UiKit.Label(head, UiKit.Display, 118, Color.white, TextAnchor.MiddleLeft, Vector2.zero, new Vector2(1300f, 150f), true).text = heading;
        Move(head, 0.12f, new Vector2(-260f, 0f), 1.5f, 0.24f);

        // The receipt: a ticket tilted the other way, outlined in white on a hard shadow, the cargo row by row,
        // the total's line on its stub.
        int n = Mathf.Max(1, entries.Count);
        float rowHeight = Mathf.Min(76f, 520f / n), stub = 92f, height = 150f + n * rowHeight + stub;
        RectTransform receipt = UiKit.Group(shaker, new Vector2(0.5f, 0.5f), new Vector2(-230f, -70f));
        receipt.localRotation = Quaternion.Euler(0f, 0f, -2.5f);
        Sprite ticket = UiKit.Ticket(900f, height, stub);
        Slanted shadow = UiKit.Panel(receipt, new Vector2(18f, -18f), new Vector2(900f, height), tone.deep, tone.deep, 0f);
        Slanted rim = UiKit.Panel(receipt, Vector2.zero, new Vector2(924f, height + 24f), Color.white, Color.white, 0f);
        Slanted face = UiKit.Panel(receipt, Vector2.zero, new Vector2(900f, height), paper, new Color(0.96f, 0.92f, 0.84f), 0f);
        shadow.sprite = rim.sprite = face.sprite = ticket;
        Transform sheet = face.transform;
        float top = height * 0.5f;
        UiKit.Panel(sheet, new Vector2(0f, top - 40f), new Vector2(874f, 54f), tone.main, tone.main, 0f).sprite = null;
        UiKit.Label(sheet, UiKit.Display, 26, ink, TextAnchor.MiddleLeft, new Vector2(-170f, top - 40f), new Vector2(500f, 40f), true).text = L10n.T("delivery.wagon").ToUpperInvariant();
        UiKit.Label(sheet, UiKit.Display, 26, ink, TextAnchor.MiddleRight, new Vector2(270f, top - 40f), new Vector2(300f, 40f), true).text = L10n.T("delivery.value").ToUpperInvariant();
        if (entries.Count == 0)
            UiKit.Label(sheet, UiKit.Body, 28, UiKit.Slate, TextAnchor.MiddleCenter, new Vector2(0f, top - 110f - rowHeight * 0.5f), new Vector2(800f, rowHeight), true).text = L10n.T("delivery.empty");
        for (int i = 0; i < entries.Count; i++)
        {
            Entry entry = entries[i];
            Color tint = entry.destroyed ? red : ink;
            RectTransform row = UiKit.Group(sheet, new Vector2(0.5f, 0.5f), new Vector2(0f, top - 92f - rowHeight * (i + 0.5f)));
            if (i % 2 == 1) UiKit.Panel(row, Vector2.zero, new Vector2(860f, rowHeight - 4f), new Color(0f, 0f, 0f, 0.04f), new Color(0f, 0f, 0f, 0.04f), 0f).sprite = null;
            RawImage thumbnail = UiKit.Picture(row, entry.icon, new Vector2(-370f, 0f), rowHeight - 10f);
            thumbnail.rectTransform.sizeDelta = new Vector2((rowHeight - 10f) * 1.6f, rowHeight - 10f);
            Text name = UiKit.Label(row, UiKit.Body, 28, tint, TextAnchor.MiddleLeft, new Vector2(-40f, 0f), new Vector2(520f, rowHeight - 8f), true);
            name.text = entry.name;
            UiKit.Label(row, UiKit.Display, 42, tint, TextAnchor.MiddleRight, new Vector2(300f, 0f), new Vector2(260f, rowHeight), true).text = entry.value.ToString("+0;-0;0");
            if (entry.destroyed)
            {
                // struck through, and a small red stamp on it
                float struck = Mathf.Min(520f, name.preferredWidth + 10f);
                UiKit.Panel(row, new Vector2(-305f + struck * 0.5f, 0f), new Vector2(struck, 4f), red, red, 0f).sprite = null;
                RectTransform mark = UiKit.Group(row, new Vector2(0.5f, 0.5f), new Vector2(110f, 2f));
                mark.localRotation = Quaternion.Euler(0f, 0f, 8f);
                UiKit.Frame(mark, new Vector2(170f, 44f), 4f, red);
                UiKit.Label(mark, UiKit.Display, 26, red, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(160f, 40f), true).text = L10n.T("delivery.destroyed");
            }
            Move(row, 0.42f + i * 0.08f, new Vector2(140f, 0f), 1f, 0.2f);
        }
        float foot = -height * 0.5f + stub * 0.5f;
        var bars = new GameObject("Barcode", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
        bars.transform.SetParent(sheet, false);
        bars.raycastTarget = false;
        bars.texture = UiKit.Barcode();
        bars.rectTransform.sizeDelta = new Vector2(220f, 40f);
        bars.rectTransform.anchoredPosition = new Vector2(-300f, foot);
        UiKit.Label(sheet, UiKit.Display, 34, ink, TextAnchor.MiddleRight, new Vector2(200f, foot), new Vector2(440f, 50f), true).text = L10n.T("delivery.total").ToUpperInvariant();
        Move(receipt, 0.22f, new Vector2(0f, -700f), 1f, 0.3f);

        // The total, huge beside the receipt, counting up; the balance under it.
        RectTransform sum = UiKit.Group(shaker, new Vector2(0.5f, 0.5f), new Vector2(560f, -30f));
        sum.localRotation = Quaternion.Euler(0f, 0f, 6f);
        UiKit.Label(sum, UiKit.Display, 44, Color.white, TextAnchor.MiddleCenter, new Vector2(0f, 120f), new Vector2(420f, 60f), true).text = L10n.T("delivery.total.short");
        Text totalShadow = UiKit.Label(sum, UiKit.Display, 190, tone.deep, TextAnchor.MiddleCenter, new Vector2(10f, -12f), new Vector2(560f, 220f), true);
        totalText = UiKit.Label(sum, UiKit.Display, 190, total < 0 ? new Color(1f, 0.35f, 0.35f) : tone.hot, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(560f, 220f), true);
        totalShadow.gameObject.AddComponent<Echo>().of = totalText; // the shadow shows what the number shows
        RectTransform purse = UiKit.Group(sum, new Vector2(0.5f, 0.5f), new Vector2(0f, -140f));
        UiKit.Panel(purse, Vector2.zero, new Vector2(340f, 60f), tone.deep, tone.deep, 0.2f);
        UiKit.Picture(purse, "money", new Vector2(-130f, 0f), 44f);
        UiKit.Label(purse, UiKit.Body, 26, Color.white, TextAnchor.MiddleLeft, new Vector2(30f, 0f), new Vector2(240f, 44f), true).text = L10n.T("delivery.balance", balance);
        Move(sum, 0.5f + n * 0.08f, new Vector2(300f, 0f), 1.4f, 0.24f);
        countAt = 0.62f + n * 0.08f;

        // The stamp, coming down on the receipt once the total is in.
        stamp = UiKit.Group(receipt, new Vector2(0.5f, 0.5f), new Vector2(330f, top + 10f)); // over the receipt's corner, half off it
        stamp.localRotation = Quaternion.Euler(0f, 0f, 14f);
        Color stampInk = total < 0 ? red : entries.Count == 0 ? new Color(0.35f, 0.4f, 0.5f) : new Color(0.1f, 0.55f, 0.3f);
        stampInk.a = 0.9f;
        UiKit.Frame(stamp, new Vector2(400f, 120f), 9f, stampInk);
        UiKit.Frame(stamp, new Vector2(372f, 92f), 3f, stampInk);
        UiKit.Label(stamp, UiKit.Display, 70, stampInk, TextAnchor.MiddleCenter, new Vector2(0f, 2f), new Vector2(360f, 90f), true).text =
            L10n.T(total < 0 ? "delivery.stamp.fine" : entries.Count == 0 ? "delivery.stamp.empty" : "delivery.stamp.ok");
        stampAt = countAt + 0.5f;
        Move(stamp, stampAt, Vector2.zero, 2.6f, 0.14f);

        // Continue: the button drawn, bottom right, clickable.
        bool pad = train && train.Player && train.Player.UsingGamepad;
        Slanted go = UiKit.Panel(shaker, new Vector2(700f, -470f), new Vector2(360f, 70f), Color.white, Color.white, 0.2f);
        go.pixelsPerUnitMultiplier = 3f;
        go.raycastTarget = true;
        go.gameObject.AddComponent<Clicks>().click = _ => Finish();
        Prompt.Make(go.transform, Vector2.zero, 34, ink, UiKit.Display).Set("[" + UiKit.ConfirmGlyph(pad) + "]  " + L10n.T("delivery.go"));
        endsAt = stampAt + 0.3f;
        Move(go, endsAt, new Vector2(0f, -160f), 1f, 0.2f);
    }

    // Animates c from offset `from` and size `scale` to where it was put, starting `at` seconds in.
    void Move(Component c, float at, Vector2 from, float scale, float length)
    {
        var rect = (RectTransform)c.transform;
        CanvasGroup fade = rect.GetComponent<CanvasGroup>();
        if (!fade) fade = rect.gameObject.AddComponent<CanvasGroup>();
        moves.Add((rect, fade, rect.anchoredPosition, from, scale, at, length));
        fade.alpha = 0f;
    }

    void LateUpdate()
    {
        if (!Visible) return;
        float age = Time.unscaledTime - openedAt;
        foreach (var m in moves)
        {
            if (!m.rect) continue;
            float t = (age - m.at) / m.length, e = UiKit.Back(t);
            m.fade.alpha = Mathf.Clamp01(t * 4f);
            m.rect.anchoredPosition = m.rest + m.from * (1f - e);
            float size = Mathf.LerpUnclamped(m.scale, 1f, e);
            m.rect.localScale = new Vector3(size, size, 1f);
        }
        // The total counts up; the stamp lands with a jolt.
        int shown = Mathf.RoundToInt(total * Mathf.Clamp01((age - countAt) / 0.45f));
        if (shown != shownTotal)
        {
            shownTotal = shown;
            totalText.text = shown.ToString("+0;-0;0");
        }
        if (!stamped && age >= stampAt + 0.1f)
        {
            stamped = true;
            Sound.Play("confirm");
        }
        float jolt = Mathf.Clamp01(1f - (age - stampAt - 0.1f) / 0.22f);
        shaker.anchoredPosition = stamped && jolt > 0f ? Random.insideUnitCircle * 14f * jolt : Vector2.zero;
    }

    public void SetVisible(bool show)
    {
        if (canvas) canvas.gameObject.SetActive(open && show);
    }

    void Update()
    {
        if (!Visible || Time.unscaledTime - openedAt < 0.3f) return;
        if (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame) { Finish(true); return; }
        if ((Keyboard.current != null && (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame)) ||
            (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame)) Finish();
    }

    public void Close() => Finish();

    void Finish(bool back = false)
    {
        if (!Visible || Time.unscaledTime - openedAt < 0.3f) return;
        if (Time.unscaledTime - openedAt < endsAt) { openedAt = Time.unscaledTime - endsAt - 0.3f; return; } // still playing: straight to the end
        Sound.Play(back ? "menuback" : "confirm");
        open = false;
        canvas.gameObject.SetActive(false);
        completed?.Invoke();
    }
}

// A text showing whatever another one shows (a shadow under a number that changes).
public class Echo : MonoBehaviour
{
    public Text of;
    Text self;
    void LateUpdate()
    {
        if (!self) self = GetComponent<Text>();
        if (of && self.text != of.text) self.text = of.text;
    }
}
