using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// The run's end, in place of the old "play again?" question: Persona style, in the biome's colours. A band sweeps
// in with the title, then the run in numbers (time, stations, biome, kills, money), the damage each weapon dealt as
// bars (RunStats), the record (a stamp when it is a new one) and the tickets earned for the workshop. Two buttons:
// again, or the main menu. Endless runs keep their record; a story chapter just counts.
public class Results : MonoBehaviour
{
    Canvas canvas;
    float openedAt;
    int choice;
    bool held;
    System.Action again, leave;
    Slanted[] buttons = new Slanted[2];
    readonly List<(RectTransform rect, CanvasGroup fade, Vector2 rest, Vector2 from, float at, float length)> moves = new();
    readonly List<(Text text, float value, string format, float at)> counters = new();
    readonly List<(RectTransform fill, float width, float at)> bars = new();

    public void Show(TrainSim train, System.Action again, System.Action leave)
    {
        this.again = again;
        this.leave = leave;
        openedAt = Time.unscaledTime;
        Time.timeScale = 0f;
        UiKit.Tones tone = UiKit.Biome(train.BiomeId);
        float time = Time.time - RunStats.Started;
        int stations = train.Stations, kills = train.Kills, tickets = RunStats.Tickets(stations, kills, Story.Active ? 0 : train.Biome);
        bool record = !Story.Active && SaveGame.Record(stations, kills, time);
        SaveGame.AddTickets(tickets);

        canvas = UiKit.NewCanvas("Results", 40);
        var veil = UiKit.Panel(canvas.transform, Vector2.zero, new Vector2(4000f, 3000f), Color.white, Color.white, 0f);
        veil.sprite = null;
        veil.color = new Color(tone.deep.r, tone.deep.g, tone.deep.b, 0.88f);
        // the band across, tilted, with dots
        RectTransform band = UiKit.Group(canvas.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, 330f));
        band.localRotation = Quaternion.Euler(0f, 0f, -5f);
        Slanted fill = UiKit.Panel(band, Vector2.zero, new Vector2(3400f, 210f), tone.main, tone.main, 0f);
        fill.sprite = null;
        Slanted dots = UiKit.Panel(band, Vector2.zero, new Vector2(3400f, 210f), Color.white, Color.white, 0f);
        dots.sprite = UiKit.Halftone();
        dots.type = Image.Type.Tiled;
        dots.color = new Color(tone.deep.r, tone.deep.g, tone.deep.b, 0.22f);
        string heading = L10n.T(Story.Active ? "results.story" : "results.title").ToUpperInvariant();
        Text shade = UiKit.Label(band, UiKit.Display, 140, tone.deep, TextAnchor.MiddleCenter, new Vector2(12f, -10f), new Vector2(1800f, 180f), true);
        Text title = UiKit.Label(band, UiKit.Display, 140, Color.white, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(1800f, 180f), true);
        shade.text = title.text = heading;
        Move(band, 0f, new Vector2(-3200f, 0f), 0.3f);

        // left: the run in numbers, each on its own tilted plate
        string Clock(float s) => Mathf.FloorToInt(s / 60f) + ":" + Mathf.FloorToInt(s % 60f).ToString("00");
        var stats = new (string key, float value, string format)[]
        {
            ("results.time", time, "time"), ("results.stations", stations, "0"), ("results.kills", kills, "0"), ("results.money", train.money, "0"),
        };
        for (int i = 0; i < stats.Length; i++)
        {
            Slanted plate = UiKit.Plate(canvas.transform, new Vector2(-520f, 120f - i * 96f), new Vector2(560f, 78f), 0.15f, 4f, 8f);
            UiKit.Root(plate).localRotation = Quaternion.Euler(0f, 0f, 2f);
            UiKit.Label(plate.transform, UiKit.Display, 30, new Color(1f, 1f, 1f, 0.8f), TextAnchor.MiddleLeft, new Vector2(-90f, 2f), new Vector2(340f, 60f), true).text = L10n.T(stats[i].key).ToUpperInvariant();
            Text number = UiKit.Label(plate.transform, UiKit.Display, 48, tone.hot, TextAnchor.MiddleRight, new Vector2(140f, 2f), new Vector2(240f, 70f), true);
            counters.Add((number, stats[i].value, stats[i].format, 0.45f + i * 0.08f));
            Move(UiKit.Root(plate), 0.3f + i * 0.08f, new Vector2(-900f, 0f), 0.28f);
        }
        // the biome reached, under them
        Slanted where = UiKit.Plate(canvas.transform, new Vector2(-520f, 120f - stats.Length * 96f), new Vector2(560f, 64f), 0.15f, 4f, 8f);
        UiKit.Label(where.transform, UiKit.Display, 30, Color.white, TextAnchor.MiddleCenter, new Vector2(0f, 2f), new Vector2(520f, 56f), true).text =
            L10n.T("results.biome", train.BiomeLabel).ToUpperInvariant();
        Move(UiKit.Root(where), 0.3f + stats.Length * 0.08f, new Vector2(-900f, 0f), 0.28f);

        // right: the damage each weapon dealt, as bars
        Slanted board = UiKit.Plate(canvas.transform, new Vector2(390f, -20f), new Vector2(760f, 470f), 0.06f, 5f, 12f);
        UiKit.Root(board).localRotation = Quaternion.Euler(0f, 0f, -1.5f);
        Slanted tag = UiKit.Panel(board.transform, new Vector2(-180f, 238f), new Vector2(380f, 56f), tone.main, tone.main, 0.25f);
        tag.sprite = null;
        UiKit.Label(tag.transform, UiKit.Display, 34, Color.white, TextAnchor.MiddleCenter, new Vector2(0f, 2f), new Vector2(370f, 52f), true).text = L10n.T("results.damage").ToUpperInvariant();
        var top = RunStats.Top(6);
        float best = top.Count > 0 ? top[0].dealt : 1f, total = Mathf.Max(1f, RunStats.Total);
        for (int i = 0; i < top.Count; i++)
        {
            var (weapon, dealt) = top[i];
            float y = 170f - i * 66f;
            UiKit.Picture(board.transform, weapon.id, new Vector2(-330f, y), 50f);
            UiKit.Label(board.transform, UiKit.Display, 24, Color.white, TextAnchor.MiddleLeft, new Vector2(-150f, y + 14f), new Vector2(300f, 30f), true).text = weapon.Name.ToUpperInvariant();
            var track = UiKit.Panel(board.transform, new Vector2(30f, y - 12f), new Vector2(560f, 18f), new Color(0f, 0f, 0f, 0.5f), new Color(0f, 0f, 0f, 0.5f), 0f);
            track.sprite = null;
            var barFill = UiKit.Panel(board.transform, new Vector2(-250f, y - 12f), new Vector2(0f, 18f), i == 0 ? tone.hot : tone.main, i == 0 ? tone.hot : tone.main, 0f);
            barFill.sprite = null;
            barFill.rectTransform.pivot = new Vector2(0f, 0.5f);
            bars.Add((barFill.rectTransform, 560f * dealt / best, 0.7f + i * 0.08f));
            UiKit.Label(board.transform, UiKit.Display, 24, new Color(1f, 1f, 1f, 0.85f), TextAnchor.MiddleRight, new Vector2(230f, y + 14f), new Vector2(200f, 30f), true).text =
                Mathf.RoundToInt(dealt).ToString("N0") + "  ·  " + Mathf.RoundToInt(dealt / total * 100f) + "%";
        }
        if (top.Count == 0) UiKit.Label(board.transform, UiKit.Body, 26, Color.white, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(600f, 60f), true).text = L10n.T("results.nodamage");
        Move(UiKit.Root(board), 0.4f, new Vector2(1200f, 0f), 0.3f);

        // the record and the tickets, under the board
        string line = Story.Active ? "" : L10n.T("results.best", SaveGame.BestStations, Clock(SaveGame.BestTime)) + "     ";
        Text footer = UiKit.Label(canvas.transform, UiKit.Display, 30, Color.white, TextAnchor.MiddleCenter, new Vector2(390f, -300f), new Vector2(900f, 44f), true);
        footer.text = line + L10n.T("results.tickets", tickets, SaveGame.Tickets);
        Move(footer.rectTransform, 1.1f, new Vector2(0f, -120f), 0.25f);
        if (record && stations > 0) // a run that never reached a station is no record to stamp
        {
            RectTransform stamp = UiKit.Group(canvas.transform, new Vector2(0.5f, 0.5f), new Vector2(-560f, -350f));
            stamp.localRotation = Quaternion.Euler(0f, 0f, 12f);
            var ink = new Color(tone.hot.r, tone.hot.g, tone.hot.b, 0.95f);
            UiKit.Frame(stamp, new Vector2(420f, 110f), 8f, ink);
            UiKit.Label(stamp, UiKit.Display, 60, ink, TextAnchor.MiddleCenter, new Vector2(0f, 2f), new Vector2(400f, 96f), true).text = L10n.T("results.record");
            Move(stamp, 1.4f, Vector2.zero, 0.16f, 2.4f);
        }

        // the two ways on
        string[] labels = { L10n.T("results.again"), L10n.T("results.menu") };
        for (int i = 0; i < 2; i++)
        {
            Slanted b = UiKit.Panel(canvas.transform, new Vector2(i == 0 ? -220f : 220f, -440f), new Vector2(400f, 76f), Color.white, Color.white, 0.25f);
            b.sprite = null;
            b.raycastTarget = true;
            int index = i;
            var clicks = b.gameObject.AddComponent<Clicks>();
            clicks.click = _ => Pick(index);
            clicks.hover = _ => choice = index;
            UiKit.Label(b.transform, UiKit.Display, 38, Color.white, TextAnchor.MiddleCenter, new Vector2(0f, 2f), new Vector2(380f, 70f), true).text = labels[i].ToUpperInvariant();
            buttons[i] = b;
            Move(b.rectTransform, 1.3f + i * 0.08f, new Vector2(0f, -200f), 0.22f);
        }
        Sound.Play("Pause");
    }

    void Move(RectTransform rect, float at, Vector2 from, float length, float scale = 1f)
    {
        CanvasGroup fade = rect.GetComponent<CanvasGroup>();
        if (!fade) fade = rect.gameObject.AddComponent<CanvasGroup>();
        fade.alpha = 0f;
        moves.Add((rect, fade, rect.anchoredPosition, from, at, length));
        if (scale != 1f) rect.localScale = Vector3.one * scale;
    }

    void Update()
    {
        if (!canvas) return;
        Time.timeScale = 0f;
        float age = Time.unscaledTime - openedAt;
        foreach (var m in moves)
        {
            float t = (age - m.at) / m.length, e = UiKit.Back(t);
            m.fade.alpha = Mathf.Clamp01(t * 4f);
            m.rect.anchoredPosition = m.rest + m.from * (1f - e);
            if (m.rect.localScale.x != 1f) m.rect.localScale = Vector3.one * Mathf.Lerp(m.rect.localScale.x, 1f, Mathf.Clamp01(t));
        }
        foreach (var (text, value, format, at) in counters)
        {
            float v = value * Mathf.Clamp01((age - at) / 0.6f);
            text.text = format == "time" ? Mathf.FloorToInt(v / 60f) + ":" + Mathf.FloorToInt(v % 60f).ToString("00") : Mathf.RoundToInt(v).ToString("N0");
        }
        foreach (var (fill, width, at) in bars) fill.sizeDelta = new Vector2(width * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((age - at) / 0.5f)), fill.sizeDelta.y);
        UiKit.Tones tone = UiKit.Now;
        for (int i = 0; i < 2; i++)
        {
            bool on = i == choice;
            buttons[i].color = buttons[i].bottom = on ? tone.main : new Color(tone.deep.r, tone.deep.g, tone.deep.b, 0.95f);
            buttons[i].rectTransform.localScale = Vector3.one * (on ? 1.08f + 0.02f * Mathf.Sin(age * 6f) : 1f);
        }
        if (age < 1.4f) return; // the show first
        var keys = Keyboard.current;
        var padNow = Gamepad.current;
        float x = 0f;
        if (keys != null) x += (keys.dKey.isPressed || keys.rightArrowKey.isPressed ? 1f : 0f) - (keys.aKey.isPressed || keys.leftArrowKey.isPressed ? 1f : 0f);
        if (padNow != null) x += padNow.leftStick.x.ReadValue() + padNow.dpad.x.ReadValue();
        if (Mathf.Abs(x) > 0.5f && !held) { choice = x > 0f ? 1 : 0; Sound.Play("click"); }
        held = Mathf.Abs(x) > 0.5f;
        bool go = (keys != null && (keys.spaceKey.wasPressedThisFrame || keys.enterKey.wasPressedThisFrame)) || (padNow != null && padNow.buttonSouth.wasPressedThisFrame);
        if (go) Pick(choice);
    }

    void Pick(int which)
    {
        if (Time.unscaledTime - openedAt < 1.4f) return;
        Sound.Play("confirm");
        Time.timeScale = 1f;
        Destroy(canvas.gameObject);
        canvas = null;
        (which == 0 ? again : leave)?.Invoke();
    }
}
