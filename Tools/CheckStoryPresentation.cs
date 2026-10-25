using System;
using System.Reflection;
using UnityEditor.Localization;
using UnityEngine;

public static class CheckStoryPresentation
{
    static Rect Bounds(RectTransform transform)
    {
        var corners = new Vector3[4];
        transform.GetWorldCorners(corners);
        Vector2 min = corners[0], max = corners[0];
        foreach (Vector3 corner in corners) { min = Vector2.Min(min, corner); max = Vector2.Max(max, corner); }
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    public static string Run()
    {
        if (!Application.isPlaying) throw new Exception("Run in Play Mode");
        var story = UnityEngine.Object.FindAnyObjectByType<StoryIntro>();
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var art = (UnityEngine.UI.RawImage)typeof(StoryIntro).GetField("art", flags).GetValue(story);
        var text = (UnityEngine.UI.Text)typeof(StoryIntro).GetField("narration", flags).GetValue(story);
        var speaker = (UnityEngine.UI.Text)typeof(StoryIntro).GetField("storyName", flags).GetValue(story);
        var canvas = (Canvas)typeof(StoryIntro).GetField("canvas", flags).GetValue(story);
        var pages = story.pages;
        int page = (int)typeof(StoryIntro).GetField("page", flags).GetValue(story);
        int illustrations = 0, translations = 0;
        float longest = 0f;
        try
        {
            if (canvas.GetComponentsInChildren<UnityEngine.UI.Button>().Length != 2) throw new Exception("Unexpected story control, expected Skip and Continue only");
            foreach (Texture2D texture in Resources.LoadAll<Texture2D>("Story"))
            {
                if (texture.name == "prologue") continue; // The atlas is checked through its real page UVs below.
                story.pages = new[] { new StoryIntro.Page { artwork = "Story/" + texture.name, textKey = "story.page.01", image = 0 } };
                story.ShowPage(0, true);
                CheckArt(art, speaker, canvas);
                illustrations++;
            }
            story.pages = pages;
            for (int i = 0; i < pages.Length; i++) { story.ShowPage(i, true); CheckArt(art, speaker, canvas); }
            foreach (var table in LocalizationEditorSettings.GetStringTableCollection("Game").StringTables)
                foreach (var entry in table.Values)
                {
                    string key = entry.Key;
                    if (!key.StartsWith("story.page.") && !(key.StartsWith("story.") && (key.Contains(".intro.") || key.Contains(".outro.")))) continue;
                    string line = entry.LocalizedValue;
                    var settings = text.GetGenerationSettings(new Vector2(text.rectTransform.rect.width, 0f));
                    settings.resizeTextForBestFit = false;
                    settings.fontSize = text.resizeTextMinSize;
                    float height = new TextGenerator().GetPreferredHeight(line, settings) / text.pixelsPerUnit;
                    longest = Mathf.Max(longest, height);
                    if (height > text.rectTransform.rect.height + 1f) throw new Exception("Text clips: " + table.LocaleIdentifier.Code + " " + key + " height=" + height);
                    translations++;
                }
            return "PASS: " + illustrations + " full illustrations, " + pages.Length + " prologue pages, " + translations + " localized texts. No artwork overlaps the name or dialogue. Longest minimum-size text height=" + longest;
        }
        finally { story.pages = pages; story.ShowPage(page, true); }
    }

    static void CheckArt(UnityEngine.UI.RawImage art, UnityEngine.UI.Text speaker, Canvas canvas)
    {
        Canvas.ForceUpdateCanvases();
        Rect image = Bounds(art.rectTransform), tag = Bounds((RectTransform)speaker.transform.parent), screen = Bounds((RectTransform)canvas.transform);
        if (Overlaps(art.rectTransform, (RectTransform)speaker.transform.parent)) throw new Exception("Speaker tag covers illustration " + art.texture.name);
        if (image.xMin < screen.xMin - 0.5f || image.xMax > screen.xMax + 0.5f || image.yMax > screen.yMax + 0.5f) throw new Exception("Illustration exceeds screen " + art.texture.name);
        float expected = art.texture.width * art.uvRect.width / (art.texture.height * art.uvRect.height);
        if (Mathf.Abs(art.rectTransform.rect.width / art.rectTransform.rect.height - expected) > 0.01f) throw new Exception("Illustration aspect changed " + art.texture.name);
    }

    // Rotated frames may share an axis-aligned bounding box without actually covering each other.
    static bool Overlaps(RectTransform a, RectTransform b)
    {
        var ac = new Vector3[4]; var bc = new Vector3[4];
        a.GetWorldCorners(ac); b.GetWorldCorners(bc);
        foreach (var corners in new[] { ac, bc })
            for (int i = 0; i < 2; i++)
            {
                Vector3 edge = corners[i + 1] - corners[i];
                Vector3 axis = new Vector3(-edge.y, edge.x, 0).normalized;
                float amin = float.PositiveInfinity, amax = float.NegativeInfinity;
                float bmin = float.PositiveInfinity, bmax = float.NegativeInfinity;
                foreach (var p in ac) { float d = Vector3.Dot(p, axis); amin = Mathf.Min(amin, d); amax = Mathf.Max(amax, d); }
                foreach (var p in bc) { float d = Vector3.Dot(p, axis); bmin = Mathf.Min(bmin, d); bmax = Mathf.Max(bmax, d); }
                if (amax <= bmin + .5f || bmax <= amin + .5f) return false;
            }
        return true;
    }
}
