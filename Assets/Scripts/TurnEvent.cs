using UnityEngine;
using UnityEngine.InputSystem;

// The button sequence played while the train goes through a curve (TrainSim bends the track):
// 4 to 8 buttons, in order and in time. Mostly right pays health or money per hit; mostly wrong
// damages every wagon.
public class TurnEvent : MonoBehaviour
{
    public float stepTime = 0.8f;      // seconds to press each button
    public int moneyPerHit = 10;
    public float healthPerHit = 2f;    // to every wagon and the hero
    public float damagePerMiss = 3f;   // to every wagon
    public float resultTime = 2.5f;    // how long the outcome stays on screen

    public bool Active { get; private set; }

    // Same layout on both devices: bottom, right, left, top.
    // ponytail: reads the devices directly; give these their own input actions when rebinding matters.
    static readonly string[] PadSymbols = { "✕", "○", "□", "△" }, KeySymbols = { "↓", "→", "←", "↑" };

    TrainSim train;
    int[] sequence;
    int[] outcome;      // per step: 0 pending, 1 hit, 2 miss
    int step, hits;
    float timer;
    string resultKey;
    float resultValue, resultUntil;

    void Awake() => train = GetComponent<TrainSim>();

    // passage: game seconds the train needs to clear the curve. Time slows so the sequence lasts exactly that long.
    public void Begin(float passage)
    {
        sequence = new int[Random.Range(4, 9)];
        outcome = new int[sequence.Length];
        for (int i = 0; i < sequence.Length; i++) sequence[i] = Random.Range(0, 4);
        step = hits = 0;
        timer = stepTime;
        Active = true;
        Time.timeScale = Mathf.Clamp(passage / (sequence.Length * stepTime), 0.05f, 1f);
    }

    void Update()
    {
        if (!Active) return;
        if (train.Health <= 0f) // train lost mid-curve: leave the game-over freeze alone
        {
            Active = false;
            return;
        }

        timer -= Time.unscaledDeltaTime;
        int pressed = Pressed();
        if (pressed >= 0 || timer <= 0f)
        {
            bool hit = pressed == sequence[step];
            outcome[step] = hit ? 1 : 2;
            if (hit) hits++;
            step++;
            timer = stepTime;
        }

        if (step == sequence.Length) Finish();
    }

    void Finish()
    {
        Active = false;
        Time.timeScale = 1f;

        int misses = sequence.Length - hits;
        if (hits > misses)
        {
            // Health only when something is hurt, otherwise it would be wasted.
            if (train.Damaged && Random.value < 0.5f)
            {
                resultKey = "qte.health";
                resultValue = hits * healthPerHit;
                train.Repair(resultValue);
                train.Player.Heal(resultValue);
            }
            else
            {
                resultKey = "qte.money";
                resultValue = hits * moneyPerHit;
                train.money += hits * moneyPerHit;
            }
        }
        else if (misses > hits)
        {
            resultKey = "qte.fail";
            resultValue = misses * damagePerMiss;
            train.DamageAll(resultValue);
        }
        else resultKey = "qte.tie";
        resultUntil = Time.unscaledTime + resultTime;
    }

    // Which of the four buttons went down this frame, or -1.
    static int Pressed()
    {
        Gamepad pad = Gamepad.current;
        if (pad != null)
        {
            if (pad.buttonSouth.wasPressedThisFrame) return 0;
            if (pad.buttonEast.wasPressedThisFrame) return 1;
            if (pad.buttonWest.wasPressedThisFrame) return 2;
            if (pad.buttonNorth.wasPressedThisFrame) return 3;
        }
        Keyboard keys = Keyboard.current;
        if (keys != null)
        {
            if (keys.downArrowKey.wasPressedThisFrame) return 0;
            if (keys.rightArrowKey.wasPressedThisFrame) return 1;
            if (keys.leftArrowKey.wasPressedThisFrame) return 2;
            if (keys.upArrowKey.wasPressedThisFrame) return 3;
        }
        return -1;
    }

    void OnGUI()
    {
        Ui.Begin();
        if (!Active)
        {
            if (resultKey == null || Time.unscaledTime > resultUntil) return;
            Rect banner = Ui.R(Ui.Width * 0.5f - 230f, 420f, 460f, 36f);
            Ui.Panel(banner);
            GUI.Label(banner, L10n.T(resultKey, resultValue), Ui.Title);
            return;
        }

        // One box per button: green hit, red miss, yellow the one to press now, with its time running out below.
        string[] symbols = train.Player.UsingGamepad ? PadSymbols : KeySymbols;
        const float box = 46f, gap = 6f;
        float width = sequence.Length * (box + gap) - gap + 20f, x = (Ui.Width - width) * 0.5f, y = 400f; // low on the screen, clear of the train
        Ui.Panel(Ui.R(x, y, width, 104f));
        GUI.Label(Ui.R(x, y + 4f, width, 26f), L10n.T("qte.title"), Ui.Title);
        for (int i = 0; i < sequence.Length; i++)
        {
            Rect rect = Ui.R(x + 10f + i * (box + gap), y + 36f, box, box);
            GUI.color = outcome[i] == 1 ? Color.green : outcome[i] == 2 ? Color.red : Color.white;
            GUI.Label(rect, symbols[sequence[i]], i == step ? Ui.Focused : Ui.Normal);
            GUI.color = Color.white;
            if (i == step) Ui.Progress(Ui.R(rect.x / Ui.Scale, y + 88f, box, 6f), timer / stepTime);
        }
    }
}
