using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.DualShock;

// The button sequence played while the train goes through a curve (TrainSim bends the track):
// 4 to 8 buttons, in order and in time. Mostly right pays health or money per hit; mostly wrong
// damages every wagon.
public class TurnEvent : MonoBehaviour
{
    public float easyStepTime = 1.2f, hardStepTime = 0.6f; // seconds to press each button, at the start and at the hardest
    public int moneyPerHit = 10;
    public float healthPerHit = 2f * Balance.Scale;    // to every wagon and the hero
    public float damagePerMiss = 3f * Balance.Scale;   // to every wagon
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
    bool preview;       // sequence chosen and on screen, not started yet: time to read it
    string resultKey;
    float resultValue;

    void Awake() => train = GetComponent<TrainSim>();

    float StepTime => Mathf.Lerp(easyStepTime, hardStepTime, train.Tempo);

    // The curve is in sight: choose the sequence and show it, so the player can get ready.
    public void Prepare()
    {
        sequence = new int[Random.Range(4, 9)];
        outcome = new int[sequence.Length];
        // The lists go arrows, face buttons, shoulders: a harder game draws from more of them.
        int pool = train.Difficulty >= shoulderButtonsAt ? 12 : train.Difficulty >= faceButtonsAt ? 8 : 4;
        for (int i = 0; i < sequence.Length; i++) sequence[i] = Random.Range(0, pool);
        step = hits = 0;
        usingPad = train.Player.UsingGamepad;
        preview = true;
    }

    // passage: game seconds the train needs to clear the curve. Time slows so the sequence lasts exactly that long.
    public void Begin(float passage)
    {
        if (!preview) Prepare();
        preview = false;
        timer = StepTime;
        Active = true;
        Time.timeScale = Mathf.Clamp(passage / (sequence.Length * StepTime), 0.05f, 1f);
    }

    void Update()
    {
        if (!Active || train.Paused) return;
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
            timer = StepTime;
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
        train.Toast(L10n.T(resultKey, resultValue));
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

    // On screen from the warning on: the whole sequence, then the button to press now and its time running out.
    void LateUpdate()
    {
        if (preview && !train.Bending) preview = false; // the curve went away without being played
        if (preview) usingPad = train.Player.UsingGamepad;
        if (!Active && !(preview && train.Warning)) return;

        string[] symbols = !usingPad ? Keys : Gamepad.current is DualShockGamepad ? PlayStation : Xbox;
        EventPanel panel = train.Panel;
        panel.Show(L10n.T(Active ? "qte.title" : "warn.curve"), sequence.Length);
        for (int i = 0; i < sequence.Length; i++)
            panel.Box(i, symbols[sequence[i]], outcome[i] != 0 ? outcome[i] : Active && i == step ? 3 : 0);
        if (Active) panel.Bar(timer / StepTime);
    }
}
