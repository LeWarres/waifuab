using System;
using UnityEngine;

// Lives behind Talk's text box only; StoryIntro keeps its own illustrations.
public class TalkPortraits : MonoBehaviour
{
    class Slot
    {
        public UnityEngine.UI.RawImage image;
        public float progress;
        public bool wanted, right;
    }
    readonly Slot[] slots = new Slot[2];
    Slot current;
    public bool Visible => Array.Exists(slots, s => s != null && s.progress > 0f);

    void Awake()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            var image = new GameObject("SpeakerPortrait", typeof(RectTransform), typeof(UnityEngine.UI.RawImage)).GetComponent<UnityEngine.UI.RawImage>();
            image.transform.SetParent(transform, false);
            image.raycastTarget = false;
            image.rectTransform.anchorMin = image.rectTransform.anchorMax = new Vector2(.5f, 0f);
            image.rectTransform.pivot = new Vector2(.5f, 0f);
            image.gameObject.SetActive(false);
            slots[i] = new Slot { image = image };
        }
    }

    public void Show(string speaker, bool right)
    {
        string hero = Resolve(speaker);
        var texture = hero == null ? null : Resources.Load<Texture2D>("Menu/hero_" + hero + "_cut");
        if (current != null && current.wanted && current.image.texture == texture && current.right == right) return;
        Hide();
        if (!texture) return; // radio voices and bosses without art do not borrow another hero's face
        Slot next = slots[0] == current ? slots[1] : slots[0];
        next.image.texture = texture;
        // Crop the lower body in UI only; menu art stays full length. Machines remain whole.
        float bottom = hero == "giger" || hero == "crochtrain" ? 0f : hero == "xiuhtecuhtli" ? .42f : .5f;
        next.image.uvRect = new Rect(0f, bottom, 1f, 1f - bottom);
        next.image.color = Same(speaker, L10n.T("story.onini.shadow")) ? Color.black : Color.white;
        next.right = right;
        next.progress = 0f;
        next.wanted = true;
        next.image.gameObject.SetActive(true);
        current = next;
        Pose(next);
    }

    public void Hide() { foreach (Slot slot in slots) if (slot != null) slot.wanted = false; }
    public void Clear()
    {
        Hide();
        foreach (Slot slot in slots) if (slot != null) { slot.progress = 0f; slot.image.gameObject.SetActive(false); }
        current = null;
    }

    static bool Same(string a, string b) => string.Equals(a?.Trim(), b?.Trim(), StringComparison.OrdinalIgnoreCase);
    static string Resolve(string speaker)
    {
        if (Same(speaker, L10n.T("story.crochtrain.thoughts"))) return "crochtrain";
        for (int i = 0; i < Locomotives.Models.Length; i++)
        {
            string hero = i == 11 ? "crochtrain" : Locomotives.CampaignHeroId(i);
            if (Same(speaker, L10n.T(Locomotives.HeroNameKey(i))) || Same(speaker, L10n.T(Locomotives.NameKeys[i]))) return hero;
        }
        if (Same(speaker, L10n.T("story.xiuhtecuhtli.clone"))) return "xiuhtecuhtli";
        if (Same(speaker, L10n.T("story.onini.shadow"))) return "onini";
        return null;
    }

    void Update()
    {
        foreach (Slot slot in slots)
        {
            slot.progress = Mathf.MoveTowards(slot.progress, slot.wanted ? 1f : 0f, Time.unscaledDeltaTime / .24f);
            slot.image.gameObject.SetActive(slot.wanted || slot.progress > 0f);
            if (slot.image.gameObject.activeSelf) Pose(slot);
        }
    }

    void Pose(Slot slot)
    {
        Rect screen = ((RectTransform)transform).rect;
        Rect crop = slot.image.uvRect;
        bool bust = crop.height < 1f;
        float aspect = slot.image.texture.width * crop.width / (slot.image.texture.height * crop.height);
        float height = Mathf.Min(bust ? 720f : 780f, screen.height * .74f);
        float width = height * aspect;
        float maxWidth = screen.width * (bust ? .42f : .34f);
        if (width > maxWidth) { width = maxWidth; height = width / aspect; }
        slot.image.rectTransform.sizeDelta = new Vector2(width, height);
        float outside = screen.width * .5f + width * .5f + 40f;
        float inside = screen.width * .5f - width * .5f - screen.width * .06f;
        float ease = 1f - Mathf.Pow(1f - slot.progress, 3f);
        slot.image.rectTransform.anchoredPosition = new Vector2((slot.right ? 1f : -1f) * Mathf.Lerp(outside, inside, ease), 130f);
    }
}
