using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public enum QuestionnaireMode { Human, Alien }

public class Cuestionario : MonoBehaviour
{
    public QuestionnaireMode mode;
    bool Alien => mode == QuestionnaireMode.Alien;
    string ModeKey(string suffix) => (Alien ? "quiz.alien." : "quiz.") + suffix;
    enum Stage { Intro, Question, Feedback, Passed, Arrested }
    const int QuestionCount = 5, ErrorLimit = 3;
    // Bit masks: A=1, B=2, C=4, D=8. These are the fictional office's deliberately silly rules.
    static readonly int[] Correct = {
        4,9,4,2,1,8,4,1,2,8, 4,2,1,8,2,4,1,8,2,4,
        8,1,2,4,8,1,2,4,8,1, 4,2,8,1,4,2,8,1,4,2,
        8,1,4,2,8,1,4,2,8,4
    };
    // Alien questions rotate their deliberately alien answer through A, B, C, D.
    static int AlienAnswer(int id) => 1 << (id % 4);
    [SerializeField] Canvas canvas;
    [SerializeField] UnityEngine.UI.Text heading, progress, message, footer;
    [SerializeField] Slanted[] cards = new Slanted[4];
    [SerializeField] UnityEngine.UI.Text[] labels = new UnityEngine.UI.Text[4];
    readonly int[] deck = new int[Correct.Length];
    Stage stage;
    InputAction move;
    int question, errors, focus, picked, lastActionFrame = -1;
    bool held, answeredCorrectly;
    float feedbackUntil;

    void Awake()
    {
        Time.timeScale = 1f;
        Display.Apply();
        if (Story.Active && Story.InspectionPending) mode = Story.InspectionMode;
        var blink = GetComponent<OfficerBlink>();
        if (blink) blink.Configure(mode);
        if (!canvas) BuildUI();
        for (int i = 0; i < cards.Length; i++)
        {
            var clicks = cards[i].GetComponent<Clicks>() ?? cards[i].gameObject.AddComponent<Clicks>();
            clicks.index = i;
            clicks.click = Activate;
            clicks.hover = index => {
                if (stage == Stage.Feedback || focus == index) return;
                focus = index; Sound.Play("click"); Refresh();
            };
        }
        InputSystem.actions.Enable();
        move = InputSystem.actions.FindAction("Player/Move", true);
        Refresh();
    }

    void Begin()
    {
        for (int i = 0; i < deck.Length; i++) deck[i] = i;
        for (int i = deck.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (deck[i], deck[j]) = (deck[j], deck[i]);
        }
        question = errors = focus = 0;
        stage = Stage.Question;
    }

    void Activate(int index)
    {
        if (lastActionFrame == Time.frameCount || stage == Stage.Feedback) return;
        int count = stage == Stage.Question ? 4 : 1;
        if (index < 0 || index >= count) return;
        lastActionFrame = Time.frameCount;
        focus = index;
        Sound.Play("confirm");
        if ((stage == Stage.Passed || stage == Stage.Arrested) && Story.Active && Story.InspectionPending)
        {
            if (stage == Stage.Arrested)
            {
                Story.RetryInspection();
                SceneManager.LoadScene("historiaDialogos");
            }
            else
            {
                Story.PassInspection();
                SceneManager.LoadScene("sandScenario");
            }
            return;
        }
        if (stage != Stage.Question)
        {
            Begin();
        }
        else
        {
            picked = index;
            int answer = Alien ? AlienAnswer(deck[question]) : Correct[deck[question]];
            answeredCorrectly = (answer & (1 << index)) != 0;
            if (!answeredCorrectly) errors++;
            stage = errors >= ErrorLimit ? Stage.Arrested : Stage.Feedback;
            feedbackUntil = Time.unscaledTime + 0.75f;
            if (stage == Stage.Arrested) focus = 0;
        }
        Refresh();
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        var pad = Gamepad.current;
        if (stage == Stage.Feedback)
        {
            if (Time.unscaledTime >= feedbackUntil)
            {
                question++;
                stage = question >= QuestionCount ? Stage.Passed : Stage.Question;
                focus = 0;
                Refresh();
            }
            return;
        }
        Vector2 direction = move.ReadValue<Vector2>();
        bool vertical = Mathf.Abs(direction.y) > 0.5f && Mathf.Abs(direction.y) >= Mathf.Abs(direction.x);
        bool horizontal = Mathf.Abs(direction.x) > 0.5f && !vertical;
        if (stage == Stage.Question && !held && (vertical || horizontal))
        {
            focus = vertical ? (focus + 2) % 4 : focus ^ 1;
            Sound.Play("click"); Refresh();
        }
        held = vertical || horizontal;
        if ((keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame)) ||
            (pad != null && pad.buttonSouth.wasPressedThisFrame)) Activate(focus);
    }

    public void BuildUI()
    {
        if (canvas) { Refresh(); return; }
        canvas = UiKit.NewCanvas("QuestionnaireDialogue", 10);
        canvas.GetComponent<UnityEngine.UI.CanvasScaler>().matchWidthOrHeight = 0.5f;
        var root = UiKit.Group(canvas.transform, new Vector2(0.5f, 0f), new Vector2(0f, 230f));
        Color dark = new Color(0.10f, 0.12f, 0.22f, 0.96f);
        var panel = UiKit.Panel(root, Vector2.zero, new Vector2(1760f, 420f), dark, dark, 0.025f);
        UiKit.Panel(panel.transform, new Vector2(0f, 203f), new Vector2(1680f, 4f), UiKit.Sky, UiKit.Sky, 0f);
        heading = Label(panel.transform, 24, UiKit.Sky, TextAnchor.MiddleLeft, new Vector2(-490f, 170f), new Vector2(650f, 38f));
        progress = Label(panel.transform, 23, UiKit.Ice, TextAnchor.MiddleRight, new Vector2(420f, 170f), new Vector2(790f, 38f));
        message = Label(panel.transform, 30, Color.white, TextAnchor.MiddleLeft, new Vector2(0f, 100f), new Vector2(1620f, 108f));
        for (int i = 0; i < cards.Length; i++)
        {
            cards[i] = UiKit.Panel(panel.transform, Vector2.zero, new Vector2(800f, 72f), UiKit.Navy, UiKit.Navy, 0.08f);
            cards[i].gameObject.name = "Answer" + (char)('A' + i);
            cards[i].raycastTarget = true;
            labels[i] = Label(cards[i].transform, 25, Color.white, TextAnchor.MiddleLeft, Vector2.zero, new Vector2(744f, 62f));
        }
        footer = Label(panel.transform, 20, UiKit.Ice, TextAnchor.MiddleCenter, new Vector2(0f, -174f), new Vector2(1600f, 36f));
        Refresh();
    }

    void Refresh()
    {
        bool answering = stage == Stage.Question || stage == Stage.Feedback;
        heading.text = T(ModeKey("officer"));
        progress.text = stage == Stage.Intro ? "" : T("quiz.progress", Mathf.Min(question + 1, QuestionCount), errors);
        message.color = stage == Stage.Arrested ? new Color(1f, 0.18f, 0.24f) : stage == Stage.Passed ? UiKit.Mint : Color.white;
        message.fontSize = stage == Stage.Arrested ? 52 : 30;
        message.resizeTextMaxSize = message.fontSize;
        message.alignment = answering || stage == Stage.Intro ? TextAnchor.MiddleLeft : TextAnchor.MiddleCenter;
        message.rectTransform.anchoredPosition = new Vector2(0f, answering ? 100f : 42f);
        message.rectTransform.sizeDelta = new Vector2(1620f, answering ? 108f : 180f);
        string[] lines = answering ? T(ModeKey("q.") + (deck[question] + 1).ToString("00")).Split('\n') : null;
        message.text = answering ? lines[0] : T(stage == Stage.Intro ? ModeKey("intro") : stage == Stage.Arrested ? "quiz.arrested" : ModeKey("passed"));
        for (int i = 0; i < cards.Length; i++)
        {
            cards[i].gameObject.SetActive(answering || i == 0);
            cards[i].rectTransform.sizeDelta = answering ? new Vector2(800f, 72f) : new Vector2(500f, 62f);
            cards[i].rectTransform.anchoredPosition = answering ? new Vector2(i % 2 == 0 ? -415f : 415f, 4f - (i / 2) * 86f) : new Vector2(0f, -112f);
            labels[i].rectTransform.sizeDelta = new Vector2(answering ? 744f : 452f, 58f);
            labels[i].text = answering ? ((char)('A' + i)) + ") " + lines[i + 1] : T(stage == Stage.Intro ? "quiz.begin" : "quiz.ok");
            Color tint = focus == i ? new Color(0.12f, 0.48f, 0.82f) : UiKit.Navy;
            if (stage == Stage.Feedback && i == picked) tint = answeredCorrectly ? new Color(0.08f, 0.48f, 0.28f) : new Color(0.65f, 0.08f, 0.15f);
            cards[i].color = cards[i].bottom = tint;
        }
        footer.text = stage == Stage.Feedback ? T(answeredCorrectly ? "quiz.correct" : "quiz.wrong") :
            stage == Stage.Passed ? T(ModeKey("cleared")) : stage == Stage.Arrested ? T("quiz.arrest.detail") : T("quiz.help");
    }

    static UnityEngine.UI.Text Label(Transform parent, int size, Color color, TextAnchor alignment, Vector2 at, Vector2 box)
    {
        var text = UiKit.Label(parent, UiKit.Body, size, color, alignment, at, box, true);
        text.fontStyle = FontStyle.Normal;
        return text;
    }

    static string T(string key, params object[] args)
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            var collection = UnityEditor.Localization.LocalizationEditorSettings.GetStringTableCollection("Game");
            var locale = UnityEngine.Localization.Settings.LocalizationSettings.ProjectLocale;
            var table = collection.GetTable(locale ? locale.Identifier.Code : "es") as UnityEngine.Localization.Tables.StringTable;
            string value = table.GetEntry(key)?.LocalizedValue ?? key;
            return args.Length == 0 ? value : string.Format(value, args);
        }
#endif
        return L10n.T(key, args);
    }
}
