using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// The station receipt, shown once after unloading and before choosing upgrades.
public class DeliveryReport : MonoBehaviour
{
    public struct Entry
    {
        public string icon, name;
        public int value;
        public bool destroyed;
    }

    Canvas canvas;
    System.Action completed;
    float openedAt;
    bool open;
    public bool Visible => open && canvas && canvas.gameObject.activeSelf;

    public void Show(IList<Entry> entries, int total, int balance, System.Action done)
    {
        if (canvas) Destroy(canvas.gameObject);
        canvas = UiKit.NewCanvas("DeliveryReport", 12);
        completed = done;
        openedAt = Time.unscaledTime;
        open = true;
        var veil = new GameObject("Dim", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        veil.transform.SetParent(canvas.transform, false);
        veil.color = new Color(0.04f, 0.07f, 0.16f, 0.7f);
        veil.rectTransform.anchorMin = Vector2.zero;
        veil.rectTransform.anchorMax = Vector2.one;
        veil.rectTransform.sizeDelta = Vector2.zero;
        float height = Mathf.Min(980f, 340f + Mathf.Max(1, entries.Count) * 78f);
        var page = UiKit.Panel(canvas.transform, Vector2.zero, new Vector2(1100f, height), UiKit.White, UiKit.Ice, 0.025f);
        UiKit.Panel(page.transform, new Vector2(0f, height * 0.5f - 35f), new Vector2(310f, 50f), UiKit.Sky, UiKit.Sky, 0.12f);
        UiKit.Label(page.transform, UiKit.Heading, 40, UiKit.Navy, TextAnchor.MiddleCenter, new Vector2(0f, height * 0.5f - 96f), new Vector2(1000f, 60f), true).text = L10n.T("delivery.title");
        UiKit.Label(page.transform, UiKit.Body, 23, UiKit.Slate, TextAnchor.MiddleLeft, new Vector2(-140f, height * 0.5f - 152f), new Vector2(660f, 35f), true).text = L10n.T("delivery.wagon");
        UiKit.Label(page.transform, UiKit.Body, 23, UiKit.Slate, TextAnchor.MiddleRight, new Vector2(370f, height * 0.5f - 152f), new Vector2(230f, 35f), true).text = L10n.T("delivery.value");
        float rowHeight = Mathf.Min(78f, (height - 340f) / Mathf.Max(1, entries.Count));
        if (entries.Count == 0)
            UiKit.Label(page.transform, UiKit.Body, 27, UiKit.Slate, TextAnchor.MiddleCenter, new Vector2(0f, height * 0.5f - 210f), new Vector2(950f, 60f), true).text = L10n.T("delivery.empty");
        for (int i = 0; i < entries.Count; i++)
        {
            Entry entry = entries[i];
            Color tone = entry.destroyed ? new Color(0.65f, 0.08f, 0.14f) : UiKit.Navy;
            Color background = entry.destroyed ? new Color(1f, 0.82f, 0.84f) : (i % 2 == 0 ? UiKit.Ice : Color.white);
            var row = UiKit.Panel(page.transform, new Vector2(0f, height * 0.5f - 188f - rowHeight * (i + 0.5f)), new Vector2(1000f, rowHeight - 5f), background, background, 0.02f);
            var thumbnail = UiKit.Picture(row.transform, entry.icon, new Vector2(-430f, 0f), rowHeight - 8f);
            thumbnail.rectTransform.sizeDelta = new Vector2(108f, rowHeight - 8f);
            UiKit.Label(row.transform, UiKit.Body, 27, tone, TextAnchor.MiddleLeft, new Vector2(-70f, 0f), new Vector2(570f, rowHeight - 8f), true).text = entry.name + (entry.destroyed ? "  ·  " + L10n.T("delivery.destroyed") : "");
            UiKit.Label(row.transform, UiKit.Heading, 30, tone, TextAnchor.MiddleRight, new Vector2(375f, 0f), new Vector2(210f, rowHeight - 8f), true).text = entry.value.ToString("+0;-0;0");
        }
        float footer = -height * 0.5f;
        var totalBar = UiKit.Panel(page.transform, new Vector2(0f, footer + 113f), new Vector2(1000f, 62f), UiKit.Navy, UiKit.Navy, 0.06f);
        UiKit.Label(totalBar.transform, UiKit.Heading, 29, Color.white, TextAnchor.MiddleLeft, new Vector2(-140f, 0f), new Vector2(660f, 52f), true).text = L10n.T("delivery.total");
        UiKit.Label(totalBar.transform, UiKit.Heading, 36, total < 0 ? UiKit.Rose : UiKit.Mint, TextAnchor.MiddleRight, new Vector2(370f, 0f), new Vector2(230f, 52f), true).text = total.ToString("+0;-0;0");
        UiKit.Label(page.transform, UiKit.Body, 23, UiKit.Slate, TextAnchor.MiddleLeft, new Vector2(-215f, footer + 48f), new Vector2(570f, 46f), true).text = L10n.T("delivery.balance", balance);
        var button = UiKit.Panel(page.transform, new Vector2(285f, footer + 48f), new Vector2(400f, 55f), UiKit.Sky, UiKit.Sky, 0.12f);
        button.raycastTarget = true;
        UiKit.Label(button.transform, UiKit.Heading, 25, Color.white, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(370f, 48f), true).text = L10n.T("delivery.continue");
        button.gameObject.AddComponent<Clicks>().click = _ => Finish();
    }

    public void SetVisible(bool show)
    {
        if (canvas) canvas.gameObject.SetActive(open && show);
    }

    void Update()
    {
        if (!Visible || Time.unscaledTime - openedAt < 0.3f) return;
        if (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame) { Finish(true); return; }
        if ((Keyboard.current != null && (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame)) ||
            (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame)) Finish();
    }

    public void Close() => Finish();

    void Finish(bool back = false)
    {
        if (!Visible || Time.unscaledTime - openedAt < 0.3f) return;
        Sound.Play(back ? "menuback" : "confirm");
        open = false;
        canvas.gameObject.SetActive(false);
        completed?.Invoke();
    }
}
