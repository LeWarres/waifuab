using System;
using System.Collections;
using UnityEngine;

// The presentation uses real time while the fight waits on scaled time.
public class UltimateCutIn : MonoBehaviour
{
    public bool Active { get; private set; }
    Canvas canvas;
    UnityEngine.UI.Image shade;
    RectTransform banner;
    UnityEngine.UI.RawImage picture;
    UltimateStreaks streaks;
    float previousScale;
    float imageAspect;

    public void Play(Action completed)
    {
        if (Active) return;
        string hero = Locomotives.Hero;
        var texture = Resources.Load<Texture2D>("Ultimate/" + hero + "_cut_in");
        if (!texture) { completed?.Invoke(); return; }
        if (!canvas) Build(texture);
        picture.texture = texture;
        imageAspect = (float)texture.width / texture.height;
        Color power = hero switch
        {
            "cerebron" => new Color(0.65f, 0.25f, 1f),
            "faceless" => new Color(0.1f, 0.8f, 1f),
            "sentinel" => new Color(1f, 0.45f, 0.05f),
            "tzmil" => new Color(1f, 0.12f, 0.65f),
            "culu" => new Color(1f, 0.85f, 0.12f),
            "onini" => new Color(1f, 0.2f, 0.08f),
            _ => new Color(0.35f, 1f, 0.5f)
        };
        streaks.powerColor = power;
        streaks.accentColor = Color.Lerp(power, Color.white, 0.5f);
        previousScale = Time.timeScale;
        Active = true;
        Time.timeScale = 0f;
        canvas.gameObject.SetActive(true);
        Canvas.ForceUpdateCanvases();
        Pose(0f);
        StartCoroutine(Animate(completed));
    }

    void Build(Texture2D texture)
    {
        imageAspect = (float)texture.width / texture.height;
        canvas = UiKit.NewCanvas("UltimateCutIn", 1000);
        canvas.GetComponent<UnityEngine.UI.CanvasScaler>().matchWidthOrHeight = 0.5f;
        shade = new GameObject("Shade", typeof(RectTransform), typeof(UnityEngine.UI.Image)).GetComponent<UnityEngine.UI.Image>();
        shade.transform.SetParent(canvas.transform, false);
        shade.rectTransform.anchorMin = Vector2.zero;
        shade.rectTransform.anchorMax = Vector2.one;
        shade.rectTransform.offsetMin = shade.rectTransform.offsetMax = Vector2.zero;
        shade.raycastTarget = true;
        picture = new GameObject("HeroPortrait", typeof(RectTransform), typeof(UnityEngine.UI.RawImage)).GetComponent<UnityEngine.UI.RawImage>();
        picture.transform.SetParent(canvas.transform, false);
        picture.texture = texture;
        picture.raycastTarget = false;
        banner = picture.rectTransform;
        banner.anchorMin = banner.anchorMax = banner.pivot = new Vector2(0.5f, 0.5f);
        streaks = new GameObject("SpeedAndSparkles", typeof(RectTransform), typeof(CanvasRenderer), typeof(UltimateStreaks)).GetComponent<UltimateStreaks>();
        streaks.transform.SetParent(canvas.transform, false);
        streaks.rectTransform.anchorMin = Vector2.zero;
        streaks.rectTransform.anchorMax = Vector2.one;
        streaks.rectTransform.offsetMin = streaks.rectTransform.offsetMax = Vector2.zero;
        streaks.raycastTarget = false;
        canvas.gameObject.SetActive(false);
    }

    void Pose(float age)
    {
        var screen = (RectTransform)canvas.transform;
        float width = Mathf.Min(screen.rect.width + 80f, screen.rect.height * 0.7f * imageAspect);
        float height = width / imageAspect;
        banner.sizeDelta = new Vector2(width, height);
        float outside = (screen.rect.width + width) * 0.5f + 20f;
        float x;
        if (age < 0.15f) x = Mathf.Lerp(-outside, 0f, 1f - Mathf.Pow(1f - age / 0.15f, 3f));
        else if (age < 0.8f) x = Mathf.Lerp(0f, 24f, (age - 0.15f) / 0.65f);
        else x = Mathf.Lerp(24f, outside, Mathf.Pow((age - 0.8f) / 0.2f, 3f));
        banner.anchoredPosition = new Vector2(x, 0f);
        streaks.Pose(age, height, x, width);
        float fade = Mathf.Min(Mathf.Clamp01(age / 0.1f), Mathf.Clamp01((1f - age) / 0.15f));
        shade.color = new Color(0.025f, 0.035f, 0.08f, 0.5f * fade);
    }

    IEnumerator Animate(Action completed)
    {
        try
        {
            for (float age = 0f; age < 1f; age += Time.unscaledDeltaTime)
            {
                Pose(age);
                yield return null;
            }
        }
        finally { Restore(); }
        completed?.Invoke();
    }

    void Restore()
    {
        if (!Active) return;
        Active = false;
        Time.timeScale = previousScale;
        if (canvas) canvas.gameObject.SetActive(false);
    }

    void OnDisable() { StopAllCoroutines(); Restore(); }
    void OnDestroy() { if (canvas) Destroy(canvas.gameObject); }
}
