using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Scene changes, Persona style: tilted bands in the current colours (UiKit.Now) sweep in over the picture, a
// loading card shows while the next scene loads behind them, then they sweep on out. Load() in place of
// SceneManager.LoadScene; a scene loaded any other way still gets the sweep out. Lives across scenes.
public class Wipe : MonoBehaviour
{
    const float CoverSeconds = 0.32f, RevealSeconds = 0.4f, HoldAtLeast = 0.15f;
    enum Phase { Idle, Covering, Loading, Revealing }

    static Wipe instance;
    Canvas canvas;
    RectTransform sweep, card, runner;
    CanvasGroup cardFade;
    Text loading;
    Phase phase;
    float phaseAt;
    AsyncOperation pending;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Make()
    {
        if (instance) return;
        instance = new GameObject("Wipe").AddComponent<Wipe>();
        DontDestroyOnLoad(instance.gameObject);
    }

    public static void Load(string scene) => Begin(() => SceneManager.LoadSceneAsync(scene));
    public static void Load(int scene) => Begin(() => SceneManager.LoadSceneAsync(scene));

    static void Begin(System.Func<AsyncOperation> load)
    {
        Make();
        if (instance.phase == Phase.Covering || instance.phase == Phase.Loading) return; // one change at a time
        instance.Build();
        instance.pending = load();
        instance.pending.allowSceneActivation = false; // not until the picture is covered
        instance.Go(Phase.Covering);
    }

    void Awake() => SceneManager.sceneLoaded += Loaded;
    void OnDestroy() => SceneManager.sceneLoaded -= Loaded;

    void Loaded(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single) return;
        Build();
        if (phase != Phase.Loading) Place(0f); // loaded some other way: start covered
        Go(Phase.Revealing);
    }

    void Go(Phase next)
    {
        phase = next;
        phaseAt = Time.unscaledTime;
        canvas.gameObject.SetActive(next != Phase.Idle);
    }

    void Update()
    {
        if (phase == Phase.Idle || !canvas) return;
        float t = Time.unscaledTime - phaseAt;
        switch (phase)
        {
            case Phase.Covering:
                Place(-1f + UiKit.Back(t / CoverSeconds));
                cardFade.alpha = Mathf.Clamp01((t - CoverSeconds * 0.6f) / 0.15f);
                if (t >= CoverSeconds) Go(Phase.Loading);
                break;
            case Phase.Loading:
                Place(0f);
                cardFade.alpha = 1f;
                if (t >= HoldAtLeast && pending != null && pending.progress >= 0.89f) { pending.allowSceneActivation = true; pending = null; }
                break;
            case Phase.Revealing:
                float e = t / RevealSeconds;
                Place(Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(e)));
                cardFade.alpha = 1f - Mathf.Clamp01(e * 3f);
                if (e >= 1f) Go(Phase.Idle);
                break;
        }
        // the card: a little train running along its line, the dots after "loading" counting
        runner.anchoredPosition = new Vector2(-260f + Mathf.Repeat(Time.unscaledTime * 320f, 520f), -70f);
        loading.text = L10n.T("loading") + new string('.', 1 + (int)(Time.unscaledTime * 3f) % 3);
    }

    // where the sweep is: -1 out to the left, 0 covering, 1 out to the right
    void Place(float at) => sweep.anchoredPosition = new Vector2(at * 3300f, 0f);

    void Build()
    {
        if (canvas) return;
        canvas = UiKit.NewCanvas("Wipe", 2000);
        canvas.transform.SetParent(transform, false);
        Destroy(canvas.GetComponent<GraphicRaycaster>());
        sweep = UiKit.Group(canvas.transform, new Vector2(0.5f, 0.5f), Vector2.zero);
        sweep.localRotation = Quaternion.Euler(0f, 0f, 14f);
        // the bands: the deep colour over everything, the main colour and a white stripe leading in, dots on top
        UiKit.Paint(UiKit.Panel(sweep, Vector2.zero, new Vector2(3000f, 2600f), Color.white, Color.white, 0f), UiKit.Ink.Deep).sprite = null;
        UiKit.Paint(UiKit.Panel(sweep, new Vector2(1640f, 0f), new Vector2(260f, 2600f), Color.white, Color.white, 0f), UiKit.Ink.Main).sprite = null;
        UiKit.Panel(sweep, new Vector2(1800f, 0f), new Vector2(60f, 2600f), Color.white, Color.white, 0f).sprite = null;
        UiKit.Paint(UiKit.Panel(sweep, new Vector2(-1640f, 0f), new Vector2(260f, 2600f), Color.white, Color.white, 0f), UiKit.Ink.Main).sprite = null;
        UiKit.Panel(sweep, new Vector2(-1800f, 0f), new Vector2(60f, 2600f), Color.white, Color.white, 0f).sprite = null;
        Slanted dots = UiKit.Panel(sweep, Vector2.zero, new Vector2(3000f, 2600f), Color.white, Color.white, 0f);
        dots.sprite = UiKit.Halftone();
        dots.type = Image.Type.Tiled;
        UiKit.Paint(dots, UiKit.Ink.Main, 0.12f);
        // the loading card, level, in the middle of the screen
        card = UiKit.Group(canvas.transform, new Vector2(0.5f, 0.5f), Vector2.zero);
        cardFade = card.gameObject.AddComponent<CanvasGroup>();
        card.localRotation = Quaternion.Euler(0f, 0f, 4f);
        Text shadow = UiKit.Paint(UiKit.Label(card, UiKit.Display, 150, Color.white, TextAnchor.MiddleCenter, new Vector2(10f, 18f), new Vector2(1400f, 190f), true), UiKit.Ink.Main);
        loading = UiKit.Label(card, UiKit.Display, 150, Color.white, TextAnchor.MiddleCenter, new Vector2(0f, 28f), new Vector2(1400f, 190f), true);
        shadow.gameObject.AddComponent<Echo>().of = loading; // a hard shadow in the main colour
        UiKit.Paint(UiKit.Panel(card, new Vector2(0f, -78f), new Vector2(560f, 6f), Color.white, Color.white, 0f), UiKit.Ink.Main).sprite = null;
        runner = UiKit.Picture(card, "wagon", Vector2.zero, 56f).rectTransform;
        Place(-1f);
        canvas.gameObject.SetActive(false);
    }
}
