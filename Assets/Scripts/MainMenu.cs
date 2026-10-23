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
        public bool back;
    }

    const int MaxRows = 10;
    readonly List<Row> rows = new();
    [SerializeField] Text[] lines = new Text[MaxRows];
    [SerializeField] Canvas canvas;
    [SerializeField] RectTransform bar, wordmark;
    [SerializeField] Text pageTitle;
    [SerializeField] Text operationsLabel;
    [SerializeField] Text hint;
    Prompt hintLine;
    InputAction move, confirm;
    bool settings, held, pad;
    int focus;

    void Awake()
    {
        Time.timeScale = 1f;
        Display.Apply();
        UiKit.SetBiome("menu"); // the menus' colours
        if(!canvas) BuildUI();
        var highlight=bar.GetComponent<Slanted>();
        highlight.sprite=null; // a sharp bar, Persona style
        for(int i=0;i<MaxRows;i++) {
            var clicks=lines[i].GetComponent<Clicks>() ?? lines[i].gameObject.AddComponent<Clicks>();
            clicks.index=i;
            clicks.click=index=>{focus=index;Build();Activate(index);};
            clicks.hover=index=>{if(focus!=index) Sound.Play("click");focus=index;};
        }
        InputSystem.actions.Enable();
        move = InputSystem.actions.FindAction("Player/Move", true);
        confirm = InputSystem.actions.FindAction("Player/Jump", true);
    }

    void Add(string label, System.Action run, System.Action<int> adjust = null, bool back = false) => rows.Add(new Row { label = label, run = run, adjust = adjust, back = back });

    void Activate(int index)
    {
        if(index<0 || index>=rows.Count) return;
        Sound.Play(rows[index].back ? "menuback" : "confirm");
        rows[index].run();
    }

    void Volume(string key, float value, int which) =>
        Add(Localized(key, Mathf.RoundToInt(value * 100f)), () => Sound.Step(which, 1), steps => Sound.Step(which, steps));

    // Rebuilt every frame, like the game's menus: the labels show the settings as they are now.
    void Build()
    {
        rows.Clear();
        if (!settings)
        {
            // New game (the story from the start), load game (the story saved, at the start of its biome), endless,
            // options, exit.
            Add(Localized("main.new"), () => SelectJourney(true));
            if (Story.CanContinue) Add(Localized("main.load", SaveGame.Chapter + 1), Continue);
            else Add(Localized("main.load.empty"), () => Sound.Play("menuback")); // nothing saved yet
            Add(Localized("main.endless"), () => SelectJourney(false));
            Add(Localized("main.options"), () => { settings = true; focus = 0; });
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
        Add(Localized("menu.back"), () => { settings = false; focus = 3; }, back: true);
    }

    // Story mode is BBA's for now: her locomotive, the chapter picked, its opening told first.
    // The story saved: its chapter from the start, told first, with what the chapters before it handed on.
    void Continue()
    {
        Locomotives.StoryMode = true;
        Locomotives.Select(SaveGame.Locomotive);
        Story.Play(SaveGame.Chapter);
        SceneManager.LoadScene("historiaDialogos");
    }


    void SelectJourney(bool story)
    {
        Locomotives.StoryMode = story;
        if (story) Story.Play(0); // a new story: the save is replaced once the locomotive is picked (SelectLocomotive)
        SceneManager.LoadScene("selectHero");
    }

    void Update()
    {
        Build();
        focus = Mathf.Clamp(focus, 0, rows.Count - 1);

        // Up / down choose, left / right change a setting, confirm runs the line. One step per push of the stick.
        Vector2 stick = move.ReadValue<Vector2>();
        bool vertical = Mathf.Abs(stick.y) > 0.5f && Mathf.Abs(stick.y) >= Mathf.Abs(stick.x), sideways = Mathf.Abs(stick.x) > 0.5f && !vertical;
        if (!held && vertical) { focus = (focus + (stick.y > 0f ? -1 : 1) + rows.Count) % rows.Count; Sound.Play("click"); }
        else if (!held && sideways && rows[focus].adjust != null) { rows[focus].adjust(stick.x > 0f ? 1 : -1); Sound.Play("confirm"); }
        held = vertical || sideways;
        if (move.activeControl != null && held) pad = move.activeControl.device is Gamepad;
        if (confirm.WasPressedThisFrame()) Activate(focus);
        if (settings && ((Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) ||
            (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame))) { Sound.Play("menuback"); focus = 3; settings = false; }

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
                lines[i].text=rows[i].label.Replace("\n"," ").ToUpperInvariant();
                lines[i].rectTransform.anchoredPosition=new Vector2(i==focus ? 355:335, (settings ? 210:30)-i*62); // the one in focus pushed out
                lines[i].color=i==focus ? Color.white:new Color(1,1,1,.55f);
            }
        }
        wordmark.gameObject.SetActive(!settings);
        pageTitle.gameObject.SetActive(settings);
        pageTitle.text=Localized("menu.settings");
        if(operationsLabel) operationsLabel.text="01  /  "+Localized("main.operations");
        bar.anchoredPosition=new Vector2(330,(settings ? 210:30)-focus*62);
        var highlight=bar.GetComponent<Slanted>();
        highlight.color=highlight.bottom=UiKit.Now.main;
        highlight.fade=false;
        highlight.SetVerticesDirty();
        hint.text="";
        if(!hintLine) hintLine=hint.GetComponentInChildren<Prompt>(); // one saved with the scene
        if(!hintLine) hintLine=Prompt.Make(hint.transform,Vector2.zero,20,Color.white); // the buttons drawn
        hintLine.Set(Localized(settings ? (pad ? "sound.help.gamepad":"sound.help.keyboard") : (pad ? "pause.help.gamepad":"pause.help.keyboard"),UiKit.ConfirmGlyph(pad)));
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
        // Persona style: a slanted near-black column on a red one, the name huge and tilted on a red shadow, the
        // options in the display font, the one in focus on a sharp red bar.
        UiKit.Paint(UiKit.Panel(left,new Vector2(395,0),new Vector2(830,1250),Color.white,Color.white,.14f),UiKit.Ink.Main);
        UiKit.Panel(left,new Vector2(371,0),new Vector2(830,1250),Color.white,Color.white,.14f);
        UiKit.Paint(UiKit.Panel(left,new Vector2(357,0),new Vector2(830,1250),Color.white,Color.white,.14f),UiKit.Ink.Plate,.97f);
        wordmark=UiKit.Group(left,new Vector2(.5f,.5f),Vector2.zero);
        wordmark.localRotation=Quaternion.Euler(0,0,4);
        UiKit.Paint(Label(wordmark,"WAIFU",130,Color.white,new Vector2(368,320),new Vector2(600,150),UiKit.Display),UiKit.Ink.Main);
        Label(wordmark,"WAIFU",130,Color.white,new Vector2(358,330),new Vector2(600,150),UiKit.Display);
        UiKit.Paint(Label(wordmark,"ON RAILS",104,Color.white,new Vector2(368,212),new Vector2(600,130),UiKit.Display),UiKit.Ink.Main);
        Label(wordmark,"ON RAILS",104,Color.white,new Vector2(358,222),new Vector2(600,130),UiKit.Display);
        UiKit.Paint(UiKit.Panel(wordmark,new Vector2(305,144),new Vector2(445,8),Color.white,Color.white,0),UiKit.Ink.Hot);
        operationsLabel=Label(wordmark,"01  /  "+Localized("main.operations"),20,new Color(1,1,1,.6f),new Vector2(358,104),new Vector2(550,35),UiKit.Display);
        pageTitle=Label(left,"",64,Color.white,new Vector2(375,327),new Vector2(590,90),UiKit.Display);
        var blue=UiKit.Panel(left,new Vector2(340,30),new Vector2(560,60),Color.white,Color.white,.25f);bar=blue.rectTransform;
        for(int i=0;i<MaxRows;i++) {
            lines[i]=Label(left,"",38,Color.white,new Vector2(335,30-i*62),new Vector2(490,56),UiKit.Display);
            lines[i].raycastTarget=true;
        }
        Label(left,"WAIFU ON RAILS  /  "+Application.version,16,new Color(1,1,1,.45f),new Vector2(360,-449),new Vector2(550,34),UiKit.Body);
        var footer=UiKit.Group(canvas.transform,new Vector2(.5f,0),Vector2.zero);footer.name="Footer";
        UiKit.Paint(UiKit.Panel(footer,new Vector2(0,33),new Vector2(2400,66),Color.white,Color.white,0),UiKit.Ink.Deep,.92f);
        hint=Label(canvas.transform,"",20,Color.white,new Vector2(0,33),new Vector2(1600,42),UiKit.Body);
        hint.rectTransform.anchorMin=hint.rectTransform.anchorMax=new Vector2(.5f,0);hint.alignment=TextAnchor.MiddleCenter;
        var right=UiKit.Group(canvas.transform,new Vector2(1,.5f),new Vector2(-400,0));
        Label(right,"WAIFU ON RAILS",24,Color.white,new Vector2(0,414),new Vector2(590,40),UiKit.Display).alignment=TextAnchor.MiddleRight;
        UiKit.Paint(UiKit.Panel(right,new Vector2(235,380),new Vector2(120,5),Color.white,Color.white,0),UiKit.Ink.Main);
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

