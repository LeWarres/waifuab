using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// The title screen combines the game's ocean railway with the selection screen's Canvas artwork.
public class MainMenu : MonoBehaviour
{
    public string gameScene = "sandScenario";

    class Row
    {
        public string label;
        public System.Action run;
        public System.Action<int> adjust; // a setting: left / right change it
    }

    const int MaxRows = 10;
    readonly List<Row> rows = new();
    [SerializeField] Text[] lines = new Text[MaxRows];
    [SerializeField] Canvas canvas;
    [SerializeField] RectTransform bar, wordmark;
    [SerializeField] Text pageTitle;
    [SerializeField] Text operationsLabel;
    [SerializeField] Text hint;
    InputAction move, confirm;
    bool settings, held, pad;
    int focus;

    void Awake()
    {
        Time.timeScale = 1f;
        Display.Apply();
        if(!canvas) BuildUI();
        var highlight=bar.GetComponent<Slanted>();
        highlight.sprite=UiKit.Rounded();
        highlight.type=UnityEngine.UI.Image.Type.Sliced;
        for(int i=0;i<MaxRows;i++) {
            var clicks=lines[i].GetComponent<Clicks>() ?? lines[i].gameObject.AddComponent<Clicks>();
            clicks.index=i;
            clicks.click=index=>{focus=index;Build();rows[index].run();};
            clicks.hover=index=>focus=index;
        }
        InputSystem.actions.Enable();
        move = InputSystem.actions.FindAction("Player/Move", true);
        confirm = InputSystem.actions.FindAction("Player/Jump", true);
    }

    void Add(string label, System.Action run, System.Action<int> adjust = null) => rows.Add(new Row { label = label, run = run, adjust = adjust });

    void Volume(string key, float value, int which) =>
        Add(Localized(key, Mathf.RoundToInt(value * 100f)), () => Sound.Step(which, 1), steps => Sound.Step(which, steps));

    // Rebuilt every frame, like the game's menus: the labels show the settings as they are now.
    void Build()
    {
        rows.Clear();
        if (!settings)
        {
            Add(Localized("main.story"), () => SelectJourney(true));
            Add(Localized("main.endless"), () => SelectJourney(false));
            Add(Localized("menu.settings"), () => { settings = true; focus = 0; });
            Add(Localized("main.exit"), Quit);
            return;
        }
        Add(Localized("settings.mode", Localized(Display.ModeKeys[Display.ModeIndex])), () => Display.StepMode(1), Display.StepMode);
        Add(Localized("settings.resolution", Display.Width, Display.Height), () => Display.StepResolution(1), Display.StepResolution);
        Add(Localized("settings.fps", Display.FpsLabel), () => Display.StepFps(1), Display.StepFps);
        Add(Localized("settings.vsync", Localized(Display.VSync ? "menu.yes" : "menu.no")), Display.ToggleVSync, _ => Display.ToggleVSync());
        Volume("sound.master", Sound.Master, 0);
        Volume("sound.music", Sound.Music, 1);
        Volume("sound.sfx", Sound.Effects, 2);
        Add(Localized("sound.mute", Localized(Sound.Muted ? "menu.yes" : "menu.no")), Sound.ToggleMute, _ => Sound.ToggleMute());
        Add(Localized("language"), L10n.NextLocale, _ => L10n.NextLocale());
        Add(Localized("menu.back"), () => { settings = false; focus = 2; });
    }

    void SelectJourney(bool story)
    {
        Locomotives.StoryMode = story;
        SceneManager.LoadScene("selectHero");
    }

    void Update()
    {
        Build();
        focus = Mathf.Clamp(focus, 0, rows.Count - 1);

        // Up / down choose, left / right change a setting, confirm runs the line. One step per push of the stick.
        Vector2 stick = move.ReadValue<Vector2>();
        bool vertical = Mathf.Abs(stick.y) > 0.5f && Mathf.Abs(stick.y) >= Mathf.Abs(stick.x), sideways = Mathf.Abs(stick.x) > 0.5f && !vertical;
        if (!held && vertical) focus = (focus + (stick.y > 0f ? -1 : 1) + rows.Count) % rows.Count;
        else if (!held && sideways && rows[focus].adjust != null) rows[focus].adjust(stick.x > 0f ? 1 : -1);
        held = vertical || sideways;
        if (move.activeControl != null && held) pad = move.activeControl.device is Gamepad;
        if (confirm.WasPressedThisFrame()) rows[focus].run();
        if (settings && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) { settings = false; focus = 2; }

        Build(); // what was just changed shows at once
        RefreshUI();
    }

    public void RefreshUI()
    {
        Build();
        for(int i=0;i<MaxRows;i++) {
            bool used=i<rows.Count;
            lines[i].gameObject.SetActive(used);
            if(used) {
                lines[i].text=rows[i].label.Replace("\n"," ");
                lines[i].rectTransform.anchoredPosition=new Vector2(335, (settings ? 210:30)-i*62);
                lines[i].color=i==focus ? Color.white:UiKit.Navy;
            }
        }
        wordmark.gameObject.SetActive(!settings);
        pageTitle.gameObject.SetActive(settings);
        pageTitle.text=Localized("menu.settings");
        if(operationsLabel) operationsLabel.text="01  /  "+Localized("main.operations");
        bar.anchoredPosition=new Vector2(330,(settings ? 210:30)-focus*62);
        var highlight=bar.GetComponent<Slanted>();
        highlight.color=highlight.bottom=new Color(.08f,.48f,.88f,1f);
        highlight.fade=false;
        highlight.SetVerticesDirty();
        hint.text=Localized(settings ? (pad ? "sound.help.gamepad":"sound.help.keyboard") : (pad ? "pause.help.gamepad":"pause.help.keyboard"));
    }

    public void BuildUI()
    {
        canvas=UiKit.NewCanvas("MainMenuCanvas",0);
        canvas.GetComponent<CanvasScaler>().matchWidthOrHeight=.5f;
        var art=new GameObject("OceanRailway",typeof(RectTransform),typeof(RawImage),typeof(AspectRatioFitter)).GetComponent<RawImage>();
        art.transform.SetParent(canvas.transform,false);art.texture=Resources.Load<Texture2D>("Menu/title");art.raycastTarget=false;
        var fit=art.GetComponent<AspectRatioFitter>();fit.aspectMode=AspectRatioFitter.AspectMode.EnvelopeParent;fit.aspectRatio=16f/9;
        var graphics=new GameObject("SkyGraphics",typeof(RectTransform),typeof(CanvasRenderer),typeof(SelectionBackdrop)).GetComponent<SelectionBackdrop>();
        graphics.transform.SetParent(canvas.transform,false);graphics.raycastTarget=false;graphics.color=new Color(1,1,1,.12f);
        graphics.rectTransform.anchorMin=Vector2.zero;graphics.rectTransform.anchorMax=Vector2.one;
        graphics.rectTransform.offsetMin=graphics.rectTransform.offsetMax=Vector2.zero;
        var left=UiKit.Group(canvas.transform,new Vector2(0,.5f),Vector2.zero);
        UiKit.Panel(left,new Vector2(385,0),new Vector2(830,1250),new Color(.20f,.67f,1,.60f),new Color(.20f,.67f,1,.60f),.14f);
        UiKit.Panel(left,new Vector2(357,0),new Vector2(830,1250),new Color(.95f,.99f,1,.96f),new Color(.95f,.99f,1,.96f),.14f);
        wordmark=UiKit.Group(left,new Vector2(.5f,.5f),Vector2.zero);
        Label(wordmark,"W A I F U",80,UiKit.Navy,new Vector2(358,326),new Vector2(550,110),UiKit.Heading);
        Label(wordmark,"ON RAILS",88,UiKit.Navy,new Vector2(358,233),new Vector2(550,110),UiKit.Heading);
        UiKit.Panel(wordmark,new Vector2(305,154),new Vector2(445,5),UiKit.Sky,UiKit.Sky,0);
        operationsLabel=Label(wordmark,"01  /  "+Localized("main.operations"),18,UiKit.Slate,new Vector2(358,112),new Vector2(550,35),UiKit.Body);
        pageTitle=Label(left,"",46,UiKit.Navy,new Vector2(375,327),new Vector2(590,80),UiKit.Heading);
        var blue=UiKit.Panel(left,new Vector2(330,30),new Vector2(550,54),UiKit.Sky,UiKit.Sky,.14f);bar=blue.rectTransform;
        for(int i=0;i<MaxRows;i++) {
            lines[i]=Label(left,"",28,UiKit.Navy,new Vector2(335,30-i*62),new Vector2(490,52),UiKit.Body);
            lines[i].raycastTarget=true;
        }
        Label(left,"WAIFU ON RAILS  /  "+Application.version,16,UiKit.Slate,new Vector2(360,-449),new Vector2(550,34),UiKit.Body);
        var footer=UiKit.Group(canvas.transform,new Vector2(.5f,0),Vector2.zero);footer.name="Footer";
        UiKit.Panel(footer,new Vector2(0,33),new Vector2(2400,66),UiKit.Navy,UiKit.Navy,0);
        hint=Label(canvas.transform,"",20,Color.white,new Vector2(0,33),new Vector2(1600,42),UiKit.Body);
        hint.rectTransform.anchorMin=hint.rectTransform.anchorMax=new Vector2(.5f,0);hint.alignment=TextAnchor.MiddleCenter;
        var right=UiKit.Group(canvas.transform,new Vector2(1,.5f),new Vector2(-400,0));
        Label(right,"WAIFU ON RAILS",20,Color.white,new Vector2(0,414),new Vector2(590,40),UiKit.Heading).alignment=TextAnchor.MiddleRight;
        UiKit.Panel(right,new Vector2(235,380),new Vector2(120,3),UiKit.Ice,UiKit.Ice,0);
        RefreshUI();
    }

    static Text Label(Transform parent,string value,int size,Color color,Vector2 position,Vector2 box,Font font)
    {
        var text=UiKit.Label(parent,font,size,color,TextAnchor.MiddleLeft,position,box,true);
        text.text=value;text.fontStyle=FontStyle.Normal;return text;
    }

    static string Localized(string key,params object[] args)
    {
        string value;
#if UNITY_EDITOR
        if(!Application.isPlaying) {
            var locale=UnityEngine.Localization.Settings.LocalizationSettings.ProjectLocale;
            var collection=UnityEditor.Localization.LocalizationEditorSettings.GetStringTableCollection("Game");
            var table=collection.GetTable(locale ? locale.Identifier.Code:"es") as UnityEngine.Localization.Tables.StringTable;
            value=table.GetEntry(key)?.LocalizedValue ?? key;
        } else
#endif
        value=L10n.T(key);
        return args.Length==0 ? value:string.Format(value,args);
    }
    void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}

