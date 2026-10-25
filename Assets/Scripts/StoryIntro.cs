using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class StoryIntro : MonoBehaviour
{
    [System.Serializable] public class Page { public int image, number; public string textKey, titleKey, artwork; } // artwork: an optional full-frame texture in Resources
    public string gameScene="sandScenario";
    public Texture2D backgrounds;
    public Page[] pages;
    [SerializeField] Canvas canvas;
    [SerializeField] UnityEngine.UI.RawImage art;
    [SerializeField] UnityEngine.UI.Text narration, chapter, progress, storyName, hint;
    [SerializeField] UnityEngine.UI.Button next, skip;
    [SerializeField] RectTransform illustrationArea;
    string currentText;
    int page, visible;
    float elapsed;
    bool leaving;
    bool placeholder;
    Vector2 layoutSize;
    UnityEngine.UI.RawImage artBackdrop; // the page's picture again, filling the screen behind, darkened
    RectTransform artFrame, artShadow;   // the picture's white border and hard shadow, kept to its size

    void Awake()
    {
        Time.timeScale=1;Display.Apply();
        UiKit.SetBiome("menu"); // the menus' colours
        placeholder = Story.Route(Locomotives.HeroId(Locomotives.Selected)) == null;
        if (placeholder) { BuildPlaceholder(); return; }
        if(!canvas) BuildUI();
        RefreshLayout();
        if(!backgrounds) backgrounds=Resources.Load<Texture2D>("Story/prologue");
        if(Story.Active) pages=Story.Pages(); // story mode: this chapter's opening, after the last one's ending
        next.onClick.AddListener(()=>{Sound.Play("confirm");Advance();});
        skip.onClick.AddListener(()=>{Sound.Play("confirm");Finish();});
        if(pages==null || pages.Length==0) {Finish();return;}
        ShowPage(0,false);
    }

    void Update()
    {
        if(leaving) return;
        if (Story.TakeTestAdvance())
        {
            if (pages != null && page + 1 < pages.Length) ShowPage(page + 1, true);
            else Finish();
            return;
        }
        if(Gamepad.current!=null && Gamepad.current.buttonEast.wasPressedThisFrame) {
            Sound.Play("menuback");leaving=true;Wipe.Load(Story.Active ? "MainMenu" : "selectHero");return;
        }
        if (placeholder)
        {
            elapsed += Time.unscaledDeltaTime;
            bool proceed = (Keyboard.current != null && (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame)) ||
                (Gamepad.current != null && (Gamepad.current.buttonSouth.wasPressedThisFrame || Gamepad.current.startButton.wasPressedThisFrame));
            if (elapsed > 0.3f && proceed) { Sound.Play("confirm"); Finish(); }
            return;
        }
        if(pages==null || pages.Length==0) return;
        if(Gamepad.current!=null && Gamepad.current.startButton.wasPressedThisFrame) {Sound.Play("confirm");Finish();return;}
        bool confirm=(Keyboard.current!=null && (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame))
            || (Gamepad.current!=null && Gamepad.current.buttonSouth.wasPressedThisFrame);
        if(confirm) {Sound.Play("confirm");Advance();return;}
        elapsed+=Time.unscaledDeltaTime;
        art.color=new Color(1,1,1,Mathf.Clamp01(elapsed/.5f));
        int count=Mathf.Min(currentText.Length,Mathf.FloorToInt(elapsed*34));
        if(count>visible) {visible=count;narration.text=currentText.Substring(0,visible);}
    }

    void BuildPlaceholder()
    {
        if (canvas) canvas.gameObject.SetActive(false);
        Canvas placeholderCanvas = UiKit.NewCanvas("TestStory", 20);
        var black = new GameObject("Black", typeof(RectTransform), typeof(UnityEngine.UI.Image)).GetComponent<UnityEngine.UI.Image>();
        black.transform.SetParent(placeholderCanvas.transform, false);
        black.color = Color.black;
        black.rectTransform.anchorMin = Vector2.zero;
        black.rectTransform.anchorMax = Vector2.one;
        black.rectTransform.sizeDelta = Vector2.zero;
        Label(placeholderCanvas.transform, 48, Vector2.zero, new Vector2(600f, 100f), Color.white).text = Localized("hero.character.test");
        var text = placeholderCanvas.GetComponentInChildren<UnityEngine.UI.Text>();
        text.alignment = TextAnchor.MiddleCenter;
    }

    void Advance()
    {
        if(leaving) return;
        if(visible<currentText.Length) {
            visible=currentText.Length;narration.text=currentText;elapsed=visible/34f;
        } else if(page+1<pages.Length) ShowPage(page+1,false);
        else Finish();
    }

    void Finish()
    {
        if(leaving) return;
        leaving=true;
        if(Story.Active && Story.Over) { StartCoroutine(Thanks()); return; } // the end of it all: a word of thanks first
        Wipe.Load(Story.Active && Story.Over ? "MainMenu" :
            Story.Active && Story.AscentPending ? "ascenso" :
            Story.Active && Story.InspectionPending ? "cuestionario" :
            Story.Active && Story.Current.drawings ? "bosque" : gameScene);
    }

    // After the last page: thanks and the closing note stay until the player confirms.
    System.Collections.IEnumerator Thanks()
    {
        Canvas card = UiKit.NewCanvas("Thanks", 50);
        var black = new GameObject("Black", typeof(RectTransform), typeof(UnityEngine.UI.Image)).GetComponent<UnityEngine.UI.Image>();
        black.transform.SetParent(card.transform, false);
        black.rectTransform.anchorMin = Vector2.zero;
        black.rectTransform.anchorMax = Vector2.one;
        black.rectTransform.sizeDelta = Vector2.zero;
        UnityEngine.UI.Text title = UiKit.Label(card.transform, UiKit.Display, 110, Color.white, TextAnchor.MiddleCenter, new Vector2(0f, 50f), new Vector2(1600f, 140f), true);
        title.text = Localized("thanks.title");
        UnityEngine.UI.Text note = UiKit.Label(card.transform, UiKit.Body, 32, new Color(1f, 1f, 1f, 0.75f), TextAnchor.MiddleCenter, new Vector2(0f, -60f), new Vector2(1600f, 60f), true);
        note.text = Localized("thanks.note");
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime)
        {
            black.color = new Color(0f, 0f, 0f, t);
            title.color = new Color(1f, 1f, 1f, t);
            note.color = new Color(1f, 1f, 1f, 0.75f * t);
            yield return null;
        }
        for (float t = 0f; ; t += Time.unscaledDeltaTime)
        {
            bool press = (Keyboard.current != null && (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame))
                || (Gamepad.current != null && (Gamepad.current.buttonSouth.wasPressedThisFrame || Gamepad.current.startButton.wasPressedThisFrame));
            if (press && t > 1.5f) break;
            yield return null;
        }
        Wipe.Load("MainMenu");
    }

    public void ShowPage(int index,bool complete)
    {
        page=Mathf.Clamp(index,0,pages.Length-1);elapsed=0;
        currentText=Localized(pages[page].textKey);
        visible=complete ? currentText.Length:0;
        narration.text=complete ? currentText:"";
        int image=Mathf.Clamp(pages[page].image,0,5);
        string artwork=pages[page].artwork;
        if(image==5 && string.IsNullOrEmpty(artwork)) artwork="Story/bba_alien_escape";
        var illustration=string.IsNullOrEmpty(artwork) ? null : Resources.Load<Texture2D>(artwork);
        art.texture=illustration ? illustration : backgrounds;
        art.uvRect=illustration ? new Rect(0f,0f,1f,1f) : new Rect((image%3)/3f, image<3 ? .5f:0, 1f/3,.5f);
        art.color=Color.white;
        RefreshLayout();
        chapter.text=string.IsNullOrEmpty(pages[page].titleKey) ? (image+1).ToString("00")+"  /  "+Localized("story.chapter."+image)
            : Localized("story.chapter.number").Replace("{0}",pages[page].number.ToString())+"  /  "+Localized(pages[page].titleKey);
        progress.text=(page+1).ToString("00")+" / "+pages.Length.ToString("00");
        RefreshLabels();
    }

    public void RefreshLabels()
    {
        storyName.text=Localized("story.name");
        hint.text=Localized("story.help");
        skip.GetComponentInChildren<UnityEngine.UI.Text>().text=Localized("story.skip");
        next.GetComponentInChildren<UnityEngine.UI.Text>().text=Localized("story.continue");
    }

    void LateUpdate()
    {
        if (!placeholder && canvas && ((RectTransform)canvas.transform).rect.size != layoutSize) RefreshLayout();
        if (artFrame)
        {
            Vector2 size = art.rectTransform.rect.size;
            artFrame.localPosition = art.rectTransform.localPosition;
            artFrame.sizeDelta = size + Vector2.one * 20f;
            artShadow.localPosition = art.rectTransform.localPosition + new Vector3(18f, -18f, 0f);
            artShadow.sizeDelta = artFrame.sizeDelta;
        }
    }

    // Reserve separate areas for the complete illustration and the dialogue, including its raised name tag.
    public void RefreshLayout()
    {
        if (!canvas || !art || !narration) return;
        layoutSize = ((RectTransform)canvas.transform).rect.size;
        float scale = Mathf.Min(1f, Mathf.Max(0.1f, (layoutSize.x - 80f) / 1780f));
        var dialog = (RectTransform)narration.transform.parent;
        dialog.anchoredPosition = new Vector2(0f, 146f * scale);
        dialog.localScale = Vector3.one * scale;
        var plate = dialog.GetComponentInChildren<Backed>();
        if (plate)
        {
            ((RectTransform)plate.transform).sizeDelta = new Vector2(1780f, 230f);
            plate.Sync();
        }
        foreach (Slanted panel in dialog.GetComponentsInChildren<Slanted>())
            if (panel.rectTransform.sizeDelta == new Vector2(1740f, 6f)) panel.rectTransform.anchoredPosition = new Vector2(0f, -112f);
        var nameTag = (RectTransform)storyName.transform.parent;
        nameTag.anchoredPosition = new Vector2(-640f, 122f);
        nameTag.sizeDelta = new Vector2(420f, 52f);
        storyName.rectTransform.sizeDelta = new Vector2(400f, 48f);
        narration.rectTransform.anchoredPosition = new Vector2(0f, 4f);
        narration.rectTransform.sizeDelta = new Vector2(1640f, 122f);
        narration.resizeTextForBestFit = true;
        narration.resizeTextMinSize = 25;
        narration.resizeTextMaxSize = 31;
        skip.GetComponent<RectTransform>().anchoredPosition = new Vector2(220f, -88f);
        next.GetComponent<RectTransform>().anchoredPosition = new Vector2(650f, -88f);
        foreach (var button in new[] { skip, next })
        {
            button.GetComponent<RectTransform>().sizeDelta = new Vector2(330f, 44f);
            button.GetComponentInChildren<UnityEngine.UI.Text>().rectTransform.sizeDelta = new Vector2(318f, 36f);
        }
        var header = (RectTransform)chapter.transform.parent;
        header.localScale = Vector3.one * scale;
        header.anchoredPosition = new Vector2(0f, -50f * scale);
        if (!illustrationArea)
        {
            illustrationArea = new GameObject("StoryArtArea", typeof(RectTransform)).GetComponent<RectTransform>();
            illustrationArea.SetParent(canvas.transform, false);
            illustrationArea.SetSiblingIndex(art.transform.GetSiblingIndex());
            art.transform.SetParent(illustrationArea, false);
        }
        illustrationArea.anchorMin = Vector2.zero;
        illustrationArea.anchorMax = Vector2.one;
        illustrationArea.offsetMin = new Vector2(24f, 310f * scale);
        illustrationArea.offsetMax = new Vector2(-24f, -108f * scale);
        var fit = art.GetComponent<UnityEngine.UI.AspectRatioFitter>();
        fit.aspectMode = UnityEngine.UI.AspectRatioFitter.AspectMode.FitInParent;
        fit.aspectRatio = art.texture ? art.texture.width * art.uvRect.width / (art.texture.height * art.uvRect.height) : 16f / 9f;
        // Persona's way with a picture: tilted in a white frame with a hard shadow, over a slash of colour, dots and the
        // same picture blown up and darkened behind, so no bare ground is left round it.
        illustrationArea.localRotation = Quaternion.Euler(0f, 0f, -1.5f);
        if (!artBackdrop && Application.isPlaying)
        {
            int under = illustrationArea.GetSiblingIndex();
            artBackdrop = new GameObject("StoryArtBackdrop", typeof(RectTransform), typeof(UnityEngine.UI.RawImage), typeof(UnityEngine.UI.AspectRatioFitter)).GetComponent<UnityEngine.UI.RawImage>();
            artBackdrop.transform.SetParent(canvas.transform, false);
            artBackdrop.raycastTarget = false;
            artBackdrop.color = new Color(0.3f, 0.3f, 0.38f);
            artBackdrop.GetComponent<UnityEngine.UI.AspectRatioFitter>().aspectMode = UnityEngine.UI.AspectRatioFitter.AspectMode.EnvelopeParent;
            var slash = UiKit.Paint(UiKit.Panel(canvas.transform, new Vector2(0f, 40f), new Vector2(3400f, 360f), Color.white, Color.white, 0f), UiKit.Ink.Main, 0.85f);
            slash.sprite = null;
            slash.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 14f);
            var dots = UiKit.Panel(canvas.transform, Vector2.zero, new Vector2(3400f, 2000f), Color.white, Color.white, 0f);
            dots.sprite = UiKit.Halftone();
            dots.type = UnityEngine.UI.Image.Type.Tiled;
            UiKit.Paint(dots, UiKit.Ink.Deep, 0.35f);
            artShadow = UiKit.Paint(UiKit.Panel(illustrationArea, Vector2.zero, Vector2.zero, Color.white, Color.white, 0f), UiKit.Ink.Main).rectTransform;
            artFrame = UiKit.Panel(illustrationArea, Vector2.zero, Vector2.zero, Color.white, Color.white, 0f).rectTransform;
            artShadow.GetComponent<UnityEngine.UI.Image>().sprite = artFrame.GetComponent<UnityEngine.UI.Image>().sprite = null;
            foreach (Transform layer in new[] { artBackdrop.transform, slash.transform, dots.transform }) layer.SetSiblingIndex(under++);
            artShadow.SetAsFirstSibling();
            artFrame.SetSiblingIndex(1);
        }
        if (artBackdrop)
        {
            artBackdrop.texture = art.texture;
            artBackdrop.uvRect = art.uvRect;
            artBackdrop.GetComponent<UnityEngine.UI.AspectRatioFitter>().aspectRatio = fit.aspectRatio;
        }
        hint.transform.SetParent(dialog, false);
        hint.rectTransform.anchorMin = hint.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        hint.rectTransform.anchoredPosition = new Vector2(-390f, -88f);
        hint.rectTransform.sizeDelta = new Vector2(850f, 40f);
        hint.resizeTextForBestFit = true;
        hint.resizeTextMinSize = 14;
        hint.resizeTextMaxSize = 18;
    }

    static string Localized(string key)
    {
#if UNITY_EDITOR
        if(!Application.isPlaying) {
            var locale=UnityEngine.Localization.Settings.LocalizationSettings.ProjectLocale;
            var collection=UnityEditor.Localization.LocalizationEditorSettings.GetStringTableCollection("Game");
            var table=collection.GetTable(locale ? locale.Identifier.Code:"es") as UnityEngine.Localization.Tables.StringTable;
            return table.GetEntry(key)?.LocalizedValue ?? key;
        }
#endif
        return L10n.T(key);
    }

    public void BuildUI()
    {
        canvas=UiKit.NewCanvas("StoryCanvas",0);
        canvas.GetComponent<UnityEngine.UI.CanvasScaler>().matchWidthOrHeight=.5f;
        var ground=UiKit.Panel(canvas.transform,Vector2.zero,new Vector2(2600,1800),UiKit.Navy,UiKit.Navy,0);
        art=new GameObject("StoryIllustration",typeof(RectTransform),typeof(UnityEngine.UI.RawImage),typeof(UnityEngine.UI.AspectRatioFitter)).GetComponent<UnityEngine.UI.RawImage>();
        art.transform.SetParent(canvas.transform,false);art.raycastTarget=false;
        var fit=art.GetComponent<UnityEngine.UI.AspectRatioFitter>();
        fit.aspectMode=UnityEngine.UI.AspectRatioFitter.AspectMode.EnvelopeParent;fit.aspectRatio=16f/9;
        var header=UiKit.Group(canvas.transform,new Vector2(.5f,1),new Vector2(0,-50));
        header.localRotation=Quaternion.Euler(0,0,-.8f);
        UiKit.Persona(UiKit.Panel(header,Vector2.zero,new Vector2(1780,66),new Color(.05f,.12f,.24f,.85f),new Color(.05f,.12f,.24f,.85f),.1f),4f,9f);
        chapter=Label(header,32,new Vector2(-340,1),new Vector2(980,50),Color.white);chapter.font=UiKit.Display;
        progress=Label(header,30,new Vector2(715,1),new Vector2(190,50),Color.white);progress.alignment=TextAnchor.MiddleRight;progress.font=UiKit.Display;
        var dialog=UiKit.Group(canvas.transform,new Vector2(.5f,0),new Vector2(0,195));
        UiKit.Persona(UiKit.Panel(dialog,new Vector2(0,0),new Vector2(1780,318),new Color(.035f,.09f,.19f,.96f),new Color(.035f,.09f,.19f,.96f),.035f),6f,14f);
        UiKit.Paint(UiKit.Panel(dialog,new Vector2(0,-150),new Vector2(1740,6),Color.white,Color.white,0),UiKit.Ink.Main);
        // the speaker on a red tag breaking out of the box's top left
        var nameTag=UiKit.Paint(UiKit.Panel(dialog,new Vector2(-640,160),new Vector2(420,64),Color.white,Color.white,.25f),UiKit.Ink.Main);
        nameTag.rectTransform.localRotation=Quaternion.Euler(0,0,3);
        storyName=Label(nameTag.transform,38,new Vector2(0,2),new Vector2(400,60),Color.white);storyName.font=UiKit.Display;storyName.alignment=TextAnchor.MiddleCenter;
        narration=Label(dialog,31,new Vector2(0,16),new Vector2(1640,146),Color.white);
        narration.alignment=TextAnchor.UpperLeft;narration.resizeTextForBestFit=false;
        skip=Button(dialog,new Vector2(220,-111),new Vector2(330,52),"",out var skipLabel);
        next=Button(dialog,new Vector2(650,-111),new Vector2(330,52),"",out var nextLabel);
        hint=Label(canvas.transform,18,new Vector2(0,23),new Vector2(1600,36),Color.white);
        hint.rectTransform.anchorMin=hint.rectTransform.anchorMax=new Vector2(.5f,0);hint.alignment=TextAnchor.MiddleCenter;

        ShowPage(0,true);
    }

    static UnityEngine.UI.Text Label(Transform parent,int size,Vector2 position,Vector2 box,Color color)
    {
        var text=UiKit.Label(parent,UiKit.Body,size,color,TextAnchor.MiddleLeft,position,box,true);
        text.fontStyle=FontStyle.Normal;return text;
    }
    static UnityEngine.UI.Button Button(Transform parent,Vector2 position,Vector2 size,string text,out UnityEngine.UI.Text label)
    {
        var panel=UiKit.Panel(parent,position,size,Color.white,Color.white,.25f);panel.raycastTarget=true;panel.sprite=null;
        var button=panel.gameObject.AddComponent<UnityEngine.UI.Button>();button.targetGraphic=panel;
        var tone=UiKit.Biome("menu"); // dark, red under the pointer, gold pressed
        var colors=button.colors;colors.normalColor=new Color(.16f,.08f,.12f);colors.highlightedColor=tone.main;colors.pressedColor=tone.hot;colors.selectedColor=new Color(.16f,.08f,.12f);button.colors=colors;
        button.navigation=new UnityEngine.UI.Navigation{mode=UnityEngine.UI.Navigation.Mode.None};
        label=Label(panel.transform,26,Vector2.zero,size-Vector2.one*12,Color.white);label.alignment=TextAnchor.MiddleCenter;label.text=text;label.font=UiKit.Display;
        return button;
    }
}

