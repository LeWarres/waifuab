using UnityEngine;

// Unscaled time keeps the small idle animation independent of menu or gameplay pauses.
public class OfficerBlink : MonoBehaviour
{
    public UnityEngine.UI.RawImage portrait;
    Texture half, closed;
    OfficerEyeOverlay eyes;
    float nextBlink, started = -1f;
    QuestionnaireMode mode;

    void OnEnable() => Configure(mode);

    public void Configure(QuestionnaireMode selected)
    {
        mode = selected;
        if (!portrait) { enabled = false; return; }
        bool alien = mode == QuestionnaireMode.Alien;
        string prefix = "Questionnaire/officer_" + (alien ? "alien_" : "");
        portrait.texture = Resources.Load<Texture2D>(prefix + (alien ? "matte" : "window"));
        half = Resources.Load<Texture2D>(prefix + "half");
        closed = Resources.Load<Texture2D>(prefix + "closed");
        if (!half || !closed) { enabled = false; return; }
        eyes = portrait.GetComponentInChildren<OfficerEyeOverlay>(true);
        if (!eyes)
        {
            eyes = new GameObject("BlinkEyes", typeof(RectTransform), typeof(CanvasRenderer), typeof(OfficerEyeOverlay)).GetComponent<OfficerEyeOverlay>();
            eyes.transform.SetParent(portrait.transform, false);
            eyes.rectTransform.anchorMin = Vector2.zero;
            eyes.rectTransform.anchorMax = Vector2.one;
            eyes.rectTransform.offsetMin = eyes.rectTransform.offsetMax = Vector2.zero;
            eyes.raycastTarget = false;
        }
        eyes.alien = alien;
        eyes.Show(null);
        eyes.enabled = false;
        started = -1f;
        nextBlink = Time.unscaledTime + Random.Range(2f, 4f);
    }

    void Update()
    {
        float now = Time.unscaledTime;
        if (started < 0f)
        {
            if (now < nextBlink) return;
            started = now;
        }
        float age = now - started;
        eyes.Show(age < 0.06f ? half : age < 0.15f ? closed : age < 0.21f ? half : null);
        if (age >= 0.21f)
        {
            started = -1f;
            nextBlink = now + Random.Range(3f, 6f);
        }
    }

    void OnDisable() { if (eyes) { eyes.Show(null); eyes.enabled = false; } }
    void OnDestroy() { if (eyes) Destroy(eyes.gameObject); }
}
