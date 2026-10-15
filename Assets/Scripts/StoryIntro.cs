using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class StoryIntro : MonoBehaviour
{
    [System.Serializable] public class Page { public int image, number; public string textKey, titleKey; } // number, titleKey: a story chapter's, over the picture
    public string gameScene="sandScenario";
    public Texture2D backgrounds;
    public Page[] pages;
    [SerializeField] Canvas canvas;
    [SerializeField] UnityEngine.UI.RawImage art;
    [SerializeField] UnityEngine.UI.Text narration, chapter, progress, automaticLabel, storyName, hint;
    [SerializeField] UnityEngine.UI.Button next, automatic, skip;
    string currentText;
    int page, visible;
    float elapsed, reading;
    bool auto=true, leaving;
    bool placeholder;

    void Awake()
    {
        Time.timeScale=1;Display.Apply();
        placeholder = Locomotives.HeroId(Locomotives.Selected) != "bba";
        if (placeholder) { BuildPlaceholder(); return; }
        if(!canvas) BuildUI();
        if(!backgrounds) backgrounds=Resources.Load<Texture2D>("Story/prologue");
        if(Story.Active) pages=Story.Pages(); // story mode: this chapter's opening, after the last one's ending
        next.onClick.AddListener(()=>{Sound.Play("confirm");Advance();});
        automatic.onClick.AddListener(()=>{Sound.Play("confirm");auto=!auto;reading=0;UpdateAutomaticLabel();});
        skip.onClick.AddListener(()=>{Sound.Play("confirm");Finish();});
        if(pages==null || pages.Length==0) {Finish();return;}
        ShowPage(0,false);
    }

    void Update()
    {
        if(leaving) return;
        if(Gamepad.current!=null && Gamepad.current.buttonEast.wasPressedThisFrame) {
            Sound.Play("menuback");leaving=true;SceneManager.LoadScene(Story.Active ? "MainMenu" : "selectHero");return;
        }
        if (placeholder)
        {
            elapsed += Time.unscaledDeltaTime;
            bool proceed = (Keyboard.current != null && (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame)) ||
                (Gamepad.current != null && (Gamepad.current.buttonSouth.wasPressedThisFrame || Gamepad.current.startButton.wasPressedThisFrame));
            if (elapsed >= 2f || (elapsed > 0.3f && proceed)) { if(proceed) Sound.Play("confirm"); Finish(); }
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
        if(visible==currentText.Length) {
            if(auto) reading+=Time.unscaledDeltaTime;
            if(auto && reading>=4f) Advance();
        }
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
            visible=currentText.Length;narration.text=currentText;elapsed=visible/34f;reading=0;
        } else if(page+1<pages.Length) ShowPage(page+1,false);
        else Finish();
    }

    void Finish()
    {
        if(leaving) return;
        leaving=true;SceneManager.LoadScene(Story.Active && Story.Over ? "MainMenu" : gameScene); // the story told to its end: back to the title
    }

    public void ShowPage(int index,bool complete)
    {
        page=Mathf.Clamp(index,0,pages.Length-1);elapsed=reading=0;
        currentText=Localized(pages[page].textKey);
        visible=complete ? currentText.Length:0;
        narration.text=complete ? currentText:"";
        int image=Mathf.Clamp(pages[page].image,0,5);
        art.texture=backgrounds;
        art.uvRect=new Rect((image%3)/3f, image<3 ? .5f:0, 1f/3,.5f);
        art.color=Color.white;
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
        UpdateAutomaticLabel();
    }
    void UpdateAutomaticLabel() => automaticLabel.text=Localized(auto ? "story.auto.on":"story.auto.off");

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
        UiKit.Panel(header,Vector2.zero,new Vector2(1780,62),new Color(.05f,.12f,.24f,.85f),new Color(.05f,.12f,.24f,.85f),0);
        chapter=Label(header,25,new Vector2(-340,0),new Vector2(980,45),UiKit.Ice);
        progress=Label(header,23,new Vector2(715,0),new Vector2(190,45),Color.white);progress.alignment=TextAnchor.MiddleRight;
        var dialog=UiKit.Group(canvas.transform,new Vector2(.5f,0),new Vector2(0,195));
        UiKit.Panel(dialog,new Vector2(0,0),new Vector2(1780,318),new Color(.035f,.09f,.19f,.96f),new Color(.035f,.09f,.19f,.96f),.035f);
        UiKit.Panel(dialog,new Vector2(0,157),new Vector2(1670,3),UiKit.Sky,UiKit.Sky,0);
        storyName=Label(dialog,21,new Vector2(-675,112),new Vector2(290,38),UiKit.Sky);
        narration=Label(dialog,31,new Vector2(0,16),new Vector2(1640,146),Color.white);
        narration.alignment=TextAnchor.UpperLeft;narration.resizeTextForBestFit=false;
        automatic=Button(dialog,new Vector2(-665,-111),new Vector2(290,52),"",out automaticLabel);
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
        var panel=UiKit.Panel(parent,position,size,Color.white,Color.white,0);panel.raycastTarget=true;
        var button=panel.gameObject.AddComponent<UnityEngine.UI.Button>();button.targetGraphic=panel;
        var colors=button.colors;colors.normalColor=UiKit.Navy;colors.highlightedColor=UiKit.Sky;colors.pressedColor=UiKit.Slate;colors.selectedColor=UiKit.Navy;button.colors=colors;
        button.navigation=new UnityEngine.UI.Navigation{mode=UnityEngine.UI.Navigation.Mode.None};
        label=Label(panel.transform,21,Vector2.zero,size-Vector2.one*12,Color.white);label.alignment=TextAnchor.MiddleCenter;label.text=text;
        return button;
    }
}

