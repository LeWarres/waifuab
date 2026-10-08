using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.DualShock;

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
    public int faceButtonsAt = 3;      // difficulty from which the face buttons (or WASD) join the arrows
    public int shoulderButtonsAt = 7;  // difficulty from which shoulders and triggers (or Q E Z C) join too

    public bool Active { get; private set; }

    // Twelve buttons per device, paired by position in the list: d-pad or arrows, face buttons or WASD,
    // shoulders and triggers or Q E Z C. A sequence is a list of positions, so it reads on either device.
    // ponytail: reads the devices directly; give these their own input actions when rebinding matters.
    static readonly string[] PlayStation = { "↓", "→", "←", "↑", "✕", "○", "□", "△", "L1", "R1", "L2", "R2" },
                             Xbox = { "↓", "→", "←", "↑", "A", "B", "X", "Y", "LB", "RB", "LT", "RT" },
                             Keys = { "↓", "→", "←", "↑", "S", "D", "A", "W", "Q", "E", "Z", "C" };

    static ButtonControl[] PadButtons(Gamepad p) => new ButtonControl[]
    {
        p.dpad.down, p.dpad.right, p.dpad.left, p.dpad.up, p.buttonSouth, p.buttonEast, p.buttonWest, p.buttonNorth,
        p.leftShoulder, p.rightShoulder, p.leftTrigger, p.rightTrigger,
    };

    static ButtonControl[] KeyButtons(Keyboard k) => new ButtonControl[]
    {
        k.downArrowKey, k.rightArrowKey, k.leftArrowKey, k.upArrowKey, k.sKey, k.dKey, k.aKey, k.wKey,
        k.qKey, k.eKey, k.zKey, k.cKey,
    };

    Gamepad pad;            // devices the button lists below were built for
    Keyboard keyboard;
    ButtonControl[] padButtons, keyButtons;
    bool usingPad;          // which symbols to show: follows the last device that pressed a button

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
        // The lists go arrows, face buttons, shoulders: a harder game draws from more of them.
        int pool = train.Difficulty >= shoulderButtonsAt ? 12 : train.Difficulty >= faceButtonsAt ? 8 : 4;
        for (int i = 0; i < sequence.Length; i++) sequence[i] = Random.Range(0, pool);
        step = hits = 0;
        timer = stepTime;
        usingPad = train.Player.UsingGamepad;
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

    // Position in the list of the button that went down this frame, or -1.
    int Pressed()
    {
        if (Gamepad.current != pad)
        {
            pad = Gamepad.current;
            padButtons = pad != null ? PadButtons(pad) : null;
        }
        if (Keyboard.current != keyboard)
        {
            keyboard = Keyboard.current;
            keyButtons = keyboard != null ? KeyButtons(keyboard) : null;
        }
        for (int i = 0; padButtons != null && i < padButtons.Length; i++)
            if (padButtons[i].wasPressedThisFrame) { usingPad = true; return i; }
        for (int i = 0; keyButtons != null && i < keyButtons.Length; i++)
            if (keyButtons[i].wasPressedThisFrame) { usingPad = false; return i; }
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
        string[] symbols = !usingPad ? Keys : Gamepad.current is DualShockGamepad ? PlayStation : Xbox;
        const float box = 46f, gap = 6f;
        float width = sequence.Length * (box + gap) - gap + 20f, x = (Ui.Width - width) * 0.5f, y = 400f; // low on the screen, clear of the train
        Ui.Panel(Ui.R(x, y, width, 104f));
        GUI.Label(Ui.R(x, y + 4f, width, 26f), L10n.T("qte.title"), Ui.Title);
        for (int i = 0; i < sequence.Length; i++)
        {
            Rect rect = Ui.R(x + 10f + i * (box + gap), y + 36f, box, box);
            GUI.color = outcome[i] == 1 ? Color.green : outcome[i] == 2 ? Color.red : Color.white;
            GUI.Label(rect, "", i == step ? Ui.Focused : Ui.Normal);
            GUI.contentColor = i == step ? Color.black : Color.white;
            GUI.Label(rect, symbols[sequence[i]], Ui.Title); // big: it has to be read in a blink
            GUI.contentColor = GUI.color = Color.white;
            if (i == step) Ui.Progress(Ui.R(rect.x / Ui.Scale, y + 88f, box, 6f), timer / stepTime);
        }
    }
}
