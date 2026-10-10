using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;

// The two jump games, played when the train reaches a gap in the track (TrainSim scrolls the gap in
// and makes the wagons hop):
//  1. Ramp: one press per wagon, just as that wagon reaches the ramp. A miss wrecks that wagon.
//  2. Needle: one press for the whole train while a needle sweeps a half dial.
//     Green heals a little, yellow does nothing, red damages every wagon.
public class JumpEvent : MonoBehaviour
{
    [Header("Ramp: one press per wagon")]
    // Each pair is (at the start, at the hardest): the games tighten as the difficulty climbs, up to a limit.
    public Vector2 secondsPerWagon = new Vector2(1.2f, 0.7f); // real seconds between one wagon and the next
    public Vector2 earliestPress = new Vector2(0.45f, 0.7f);  // how far along its approach a wagon must be for the press to count
    public float latest = 1.25f;                        // past this the wagon has jumped without a press
    [Range(0f, 1f)] public float missLoss = 1f;         // share of its health a wagon loses on a miss (1 = wrecked)

    [Header("Needle: one press for the train")]
    public float needleSeconds = 3.5f;                  // real seconds to make the press
    public float needleLead = 14f;                      // starts when the gap is this far ahead of the locomotive
    public Vector2 needleSweep = new Vector2(2.2f, 4.2f); // how fast the needle swings
    public Vector2 greenWidth = new Vector2(20f, 10f);    // half width of the green zone, out of the dial's 90
    public float yellowAngle = 50f;
    public float needleHeal = 8f, needleDamage = 10f;


    public bool Active { get; private set; }
    public bool Pending => mode != 0 && !Active;  // a gap is coming, its game has not started
    public int Mode => mode;
    // Distance ahead of the train's middle at which the game starts.
    public float StartS => train.CarS(0) + (mode == 1 ? train.CarSpacing : needleLead);

    // What to call the jump button on the device in use.
    public static string ButtonName(bool gamepad) =>
        !gamepad ? L10n.T("key.space") : Gamepad.current is DualShockGamepad ? "R2" : "RT";

    TrainSim train;
    InputAction press, pressAlt;
    int mode;           // armed by TrainSim: 1 ramp, 2 needle, 0 nothing pending
    int car;            // ramp: wagon whose turn it is
    int[] outcome;      // ramp: per wagon, 0 pending, 1 jumped, 2 wrecked
    int landed, lost;
    float needleStart;

    float SecondsPerWagon => Mathf.Lerp(secondsPerWagon.x, secondsPerWagon.y, train.Tempo);
    float earliest => Mathf.Lerp(earliestPress.x, earliestPress.y, train.Tempo);
    float needleSpeed => Mathf.Lerp(needleSweep.x, needleSweep.y, train.Tempo);
    float greenAngle => Mathf.Lerp(greenWidth.x, greenWidth.y, train.Tempo);

    void Awake()
    {
        train = GetComponent<TrainSim>();
        press = InputSystem.actions.FindAction("Player/Attack", true); // R2, left click, Enter
        pressAlt = InputSystem.actions.FindAction("Player/Jump", true); // cross / A, Space
    }

    // A gap is on its way; the game starts by itself when it gets close.
    public void Arm(int newMode) => mode = newMode;

    void Update()
    {
        if (mode == 0 || train.Paused) return;
        if (train.Health <= 0f || train.AtStation) // train lost or the trip ended: drop it, leave the time scale alone
        {
            mode = 0;
            Active = false;
            return;
        }

        float spacing = train.CarSpacing, speed = Mathf.Max(train.Speed, 1f);
        if (!Active)
        {
            float lead = mode == 1 ? spacing : needleLead;
            if (train.GapS > StartS) return;
            Active = true;
            car = landed = lost = 0;
            outcome = new int[train.CarCount];
            needleStart = Time.unscaledTime;
            // Slow motion sized so the game lasts as long as the train takes to get there.
            Time.timeScale = Mathf.Clamp(mode == 1 ? spacing / speed / SecondsPerWagon : lead / speed / needleSeconds, 0.05f, 1f);
        }

        bool pressed = press.WasPressedThisFrame() || pressAlt.WasPressedThisFrame();
        if (mode == 1)
        {
            if (outcome.Length != train.CarCount) System.Array.Resize(ref outcome, train.CarCount); // a wagon was won mid-jump
            while (car < outcome.Length && !train.CarAlive(car)) car++; // wrecks have nothing to lose
            if (car < outcome.Length)
            {
                float progress = Progress(car);
                if (pressed || progress > latest)
                {
                    bool good = pressed && progress >= earliest;
                    outcome[car] = good ? 1 : 2;
                    if (good) landed++;
                    else
                    {
                        lost++;
                        train.HurtCar(car, missLoss);
                    }
                    car++;
                }
            }
            if (car >= outcome.Length) Finish("jump.ramp.result", landed, lost);
        }
        else if (pressed || train.GapS <= train.CarS(0))
        {
            float angle = pressed ? Mathf.Abs(NeedleAngle()) : 90f; // no press in time lands in the red
            if (angle <= greenAngle)
            {
                train.Repair(needleHeal);
                train.Player.Heal(needleHeal);
                Finish("jump.needle.green", needleHeal, 0f);
            }
            else if (angle <= yellowAngle) Finish("jump.needle.yellow", 0f, 0f);
            else
            {
                Finish("jump.needle.red", needleDamage, 0f); // before the damage: it may end the run
                train.DamageAll(needleDamage);
            }
        }
    }

    // How far wagon i is on its way to the ramp: 0 when the wagon ahead of it is there, 1 when it is.
    float Progress(int i) => 1f - (train.GapS - train.CarS(i)) / train.CarSpacing;

    float NeedleAngle() => Mathf.Sin((Time.unscaledTime - needleStart) * needleSpeed) * 90f;

    void Finish(string key, float a, float b)
    {
        Active = false;
        mode = 0;
        Time.timeScale = 1f;
        train.Toast(L10n.T(key, a, b));
    }

    // On screen from the warning on, so the rhythm can be read before the press counts.
    void LateUpdate()
    {
        if (mode == 0 || (!Active && !train.Warning)) return;
        string button = ButtonName(train.Player.UsingGamepad);
        EventPanel panel = train.Panel;
        if (mode == 1)
        {
            // One box per wagon; the current one's bar runs into the green stretch as it reaches the ramp.
            int count = train.CarCount;
            panel.Show(L10n.T("jump.ramp.title", button), count);
            for (int i = 0; i < count; i++)
            {
                int state = Active && i < outcome.Length ? outcome[i] : 0;
                if (!train.CarAlive(i)) state = 2;
                else if (state == 0 && Active && i == car) state = 3;
                panel.Box(i, (i + 1).ToString(), state);
            }
            if (Active && car < count) panel.Bar(Progress(car) / latest, earliest / latest, 1f);
            panel.Cue(button, false);
            return;
        }
        if (!Active) return;   // the needle: no preview, the warning strip alone announces it
        panel.Show(button, 0); // nothing to read: the button, right over the needle
        panel.Dial(NeedleAngle(), greenAngle, yellowAngle);
    }
}
