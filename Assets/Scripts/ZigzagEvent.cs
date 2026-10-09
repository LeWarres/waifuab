using UnityEngine;
using UnityEngine.InputSystem;

// The zigzag: the camera pulls back, the track ahead is a run of 4 to 6 sharp turns and the train
// speeds up instead of slowing down. At each turn the player flicks left or right, the way the track
// goes, right as the locomotive gets there. TrainSim lays the track; this is only the timing game.
public class ZigzagEvent : MonoBehaviour
{
    public float easyWindow = 0.34f, hardWindow = 0.18f; // seconds either side of the turn that still count
    public float lead = 0.9f;                            // seconds of run-up shown on the timing bar
    public int moneyPerHit = 8;
    public float damagePerMiss = 2f;                     // to every wagon, at once: a little per missed turn

    public bool Active { get; private set; }

    static readonly Color Pending = new Color(1f, 0.82f, 0.1f);
    readonly Combat.Batch pendingMarks = new Combat.Batch(Pending, false),
                          hitMarks = new Combat.Batch(Color.green, false), missMarks = new Combat.Batch(Color.red, false),
                          cueMarks = new Combat.Batch(Color.white, false);
    TrainSim train;
    InputAction steer;
    int[] outcome;  // per turn: 0 pending, 1 hit, 2 miss
    int next, hits, misses;
    bool held;      // stick or key still down from the last flick

    void Awake()
    {
        train = GetComponent<TrainSim>();
        steer = InputSystem.actions.FindAction("Player/Move", true); // left stick, d-pad keys, A/D, arrows
    }

    // The turns are laid and in sight: clean slate, so the marks and the panel show them as still to come.
    public void Prepare(int turns)
    {
        outcome = new int[turns];
        next = hits = misses = 0;
        Active = false;
    }

    public void Begin(int turns)
    {
        Prepare(turns);
        held = true; // a stick already pushed when it starts is not a flick
        Active = true;
    }

    float Window => Mathf.Lerp(easyWindow, hardWindow, train.Tempo);
    // Seconds until the locomotive is on turn i (negative once past it).
    float TimeTo(int i) => (train.KneeS(i) - train.FrontS) / Mathf.Max(train.Speed, 1f);

    void Update()
    {
        if (!Active || train.Paused) return;
        if (train.Health <= 0f || !train.Zigzag)
        {
            Active = false;
            return;
        }

        float x = steer.ReadValue<Vector2>().x;
        int flick = Mathf.Abs(x) > 0.5f ? (int)Mathf.Sign(x) : 0;
        bool pressed = flick != 0 && !held;
        held = flick != 0;

        float time = TimeTo(next), window = Window;
        // A flick well before the turn is ignored; near it, it has to be on time and the right way.
        if (pressed && time <= window * 2.5f) Resolve(Mathf.Abs(time) <= window && flick == (train.KneeTurn(next) > 0f ? 1 : -1));
        else if (time < -window) Resolve(false);
    }

    void Resolve(bool good)
    {
        outcome[next++] = good ? 1 : 2;
        if (good) hits++;
        else
        {
            misses++;
            train.DamageAll(damagePerMiss);
        }
        if (next < outcome.Length) return;
        Active = false;
        train.money += hits * moneyPerHit;
        train.Toast(L10n.T("zig.result", hits, misses, hits * moneyPerHit));
    }

    // A mark on every turn of the track: yellow to come, green taken, red missed.
    void LateUpdate()
    {
        if (outcome == null || !train.Zigzag) return;
        for (int i = 0; i < outcome.Length && i < train.KneeCount; i++)
        {
            Combat.Batch marks = outcome[i] == 1 ? hitMarks : outcome[i] == 2 ? missMarks : pendingMarks;
            float size = i == next && Active ? 4f + Mathf.PingPong(Time.unscaledTime * 6f, 1.5f) : 3.5f;
            marks.Add(train.KneePoint(i) + Vector3.up * 0.3f, new Vector3(size, 0.12f, size));
        }
        if (Active)
        {
            // Timing cue: a white frame closes in on the next turn and meets its mark exactly when to flick.
            float time = Mathf.Max(TimeTo(next), 0f), half = 2.2f + time * 13f;
            Vector3 c = train.KneePoint(next) + Vector3.up * 0.35f;
            bool now = Mathf.Abs(TimeTo(next)) <= Window;
            Combat.Batch frame = now ? hitMarks : cueMarks;
            frame.Add(c + Vector3.forward * half, new Vector3(half * 2f + 0.5f, 0.15f, 0.5f));
            frame.Add(c + Vector3.back * half, new Vector3(half * 2f + 0.5f, 0.15f, 0.5f));
            frame.Add(c + Vector3.right * half, new Vector3(0.5f, 0.15f, half * 2f + 0.5f));
            frame.Add(c + Vector3.left * half, new Vector3(0.5f, 0.15f, half * 2f + 0.5f));
        }
        pendingMarks.Flush();
        hitMarks.Flush();
        missMarks.Flush();
        cueMarks.Flush();

        // The panel: the turns in order, a marker running into the green stretch, and the arrow again right on the turn.
        if (!Active && (next != 0 || !train.Warning)) return;
        int count = outcome.Length;
        string control = L10n.T(train.Player.UsingGamepad ? "zig.stick" : "zig.keys");
        EventPanel panel = train.Panel;
        panel.Show(L10n.T("zig.title", control), count);
        for (int i = 0; i < count && i < train.KneeCount; i++)
            panel.Box(i, train.KneeTurn(i) > 0f ? "→" : "←", outcome[i] != 0 ? outcome[i] : Active && i == next ? 3 : 0);
        if (!Active) return;

        float left = TimeTo(next), window = Window, span = lead + window;
        bool hot = Mathf.Abs(left) <= window;
        string arrow = train.KneeTurn(next) > 0f ? "→" : "←";
        panel.Bar((lead - left) / span, 1f - 2f * window / span, 1f);
        panel.Cue(hot ? L10n.T("zig.now", arrow) : L10n.T("zig.wait", control, arrow), hot);
        panel.Tag(train.KneePoint(next) + Vector3.up * 3f, arrow, hot);
    }
}
