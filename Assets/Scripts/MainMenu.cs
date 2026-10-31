using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// The title screen, Persona style: the ocean railway full screen behind (drifting slowly), a dark slab slanting
// across the left with the name huge and tilted on it, the options in big letters down a slant (the one in focus on
// a red bar, pushed out, with a line saying what it does), BBA on a tilted card on the right, streaks of light
// running across. Everything slides in when it opens.
public class MainMenu : MonoBehaviour
{
    public string gameScene = "sandScenario";

    class Row
    {
        public string label;
        public System.Action run;
        public System.Action<int> adjust; // a setting: left / right change it
        public bool back;
        public string desc;   // what it does, under the list while it is in focus
        public bool dim;      // shown faded (an achievement not won yet, an upgrade out of reach)
    }

    const int MaxRows = 18;
    readonly List<Row> rows = new();
    [SerializeField] Text[] lines = new Text[MaxRows];
    [SerializeField] Canvas canvas;
    [SerializeField] RectTransform bar, wordmark;
    [SerializeField] Text pageTitle;
    [SerializeField] Text operationsLabel;
    [SerializeField] Text hint;
    [SerializeField] Text description;
    [SerializeField] RawImage art;
    [SerializeField] RectTransform slab, card, streaks;
    [SerializeField] RawImage heroArt, heroShade;
    [SerializeField] Text heroName;
    Prompt hintLine;
    float openedAt;
    Vector2 slabRest, cardRest, wordRest;

    InputAction move, confirm;
    // The page on show: 0 the main list, then the settings, the workshop and the achievements.
    int page;
    bool settings => page != 0;
    bool held, pad;
    int focus;

    void Awake()
    {
        Time.timeScale = 1f;
        Display.Apply();
        UiKit.SetBiome("menu"); // the menus' colours
        if(!canvas) BuildUI();
        if(Application.isPlaying) Shuffle();
        FrameHero();
        var highlight=bar.GetComponent<Slanted>();
        highlight.sprite=null; // a sharp bar, Persona style
        for(int i=0;i<MaxRows;i++) {
            var clicks=lines[i].GetComponent<Clicks>() ?? lines[i].gameObject.AddComponent<Clicks>();
            clicks.index=i;
            clicks.click=index=>{focus=index;Build();Activate(index);};
            clicks.hover=index=>{if(focus!=index) Sound.Play("click");focus=index;};
        }
        openedAt = Time.unscaledTime;
        slabRest = slab.anchoredPosition;
        cardRest = card.anchoredPosition;
        wordRest = wordmark.anchoredPosition;
        InputSystem.actions.Enable();
        move = InputSystem.actions.FindAction("Player/Move", true);
        confirm = InputSystem.actions.FindAction("Player/Jump", true);
    }

    // Each time it opens: one of the heroes on the card (the menu in their colours) and the train in one of the
    // biomes behind, at random (Tools/make_hero_art.py and Tools/render_menu_backdrops.cs made the pictures).
    void Shuffle()
    {
        var heroes=new List<(string id,Texture2D art)>();
        for(int i=0;i<Locomotives.Models.Length;i++) {
            string id=i==11 ? "crochtrain" : Locomotives.CampaignHeroId(i);
            var art=Resources.Load<Texture2D>("Menu/hero_"+id+"_cut");
            if(art && !heroes.Exists(h=>h.id==id)) heroes.Add((id,art));
        }
        if(heroes.Count>0) {
            var (id,art)=heroes[Random.Range(0,heroes.Count)];
            UiKit.SetHeroTones(id);
            heroArt.texture=heroShade.texture=art;
            heroShade.color=UiKit.Now.main;
            heroName.text=Localized(id=="crochtrain" ? "loco.crochet" : "hero.character."+id).ToUpperInvariant();
            FrameHero();
        }
        var backdrops=new List<Texture2D>();
        foreach(string biome in new[] { "desert","snow","jungle","ocean","volcano","city","space","alien","fortress" }) {
            var view=Resources.Load<Texture2D>("Menu/backdrop_"+biome);
            if(view) backdrops.Add(view);
        }
        if(backdrops.Count>0) art.texture=backdrops[Random.Range(0,backdrops.Count)];
    }

    void Add(string label, System.Action run, System.Action<int> adjust = null, bool back = false, string desc = null, bool dim = false) =>
        rows.Add(new Row { label = label, run = run, adjust = adjust, back = back, desc = desc, dim = dim });

    void FrameHero()
    {
        if (!heroArt || !heroArt.texture) return;
        float aspect = heroArt.texture.width / (float)heroArt.texture.height;
        Vector2 size = aspect > 600f / 877f ? new Vector2(600f, 600f / aspect) : new Vector2(877f * aspect, 877f);
        heroArt.rectTransform.sizeDelta = heroShade.rectTransform.sizeDelta = size;
    }
    void Open(int to) { page = to; focus = 0; }
    void Back() { focus = page == 1 ? 5 : page == 2 ? 3 : 4; page = 0; } // back to where the page was opened from

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
        if (page == 0)
        {
            // New game (the story from the start), load game (the story saved, at the start of its biome), endless,
            // the workshop, the achievements, options, exit.
            Add(Localized("main.new"), () => SelectJourney(true), desc: Localized("main.new.desc"));
            if (Story.CanContinue) Add(Localized("main.load", SaveGame.Chapter + 1), Continue, desc: Localized("main.load.desc"));
            else Add(Localized("main.load.empty"), () => Sound.Play("menuback"), desc: Localized("main.load.empty.desc"), dim: true); // nothing saved yet
            Add(Localized("main.endless"), () => SelectJourney(false), desc: Localized("main.endless.desc"));
            Add(Localized("main.workshop"), () => Open(2), desc: Localized("main.workshop.desc", SaveGame.Tickets));
            Add(Localized("main.achievements"), () => Open(3), desc: Localized("main.achievements.desc", Achievements.Count, Achievements.All.Length));
            Add(Localized("main.options"), () => Open(1), desc: Localized("main.options.desc"));
            Add(Localized("main.exit"), Quit, desc: Localized("main.exit.desc"));
            return;
        }
        if (page == 2)
        {
            // The workshop: each upgrade, its level, what the next costs; bought with a press.
            for (int i = 0; i < Workshop.Upgrades.Length; i++)
            {
                int at = i;
                var u = Workshop.Upgrades[i];
                int level = Workshop.Level(i);
                string price = level >= Workshop.MaxLevel ? Localized("workshop.max") : Localized("workshop.cost", Workshop.Cost(i));
                Add(Localized("workshop." + u.id) + "  " + Localized("workshop.level", level, Workshop.MaxLevel) + "  ·  " + price,
                    () => { if (!Workshop.Buy(at)) Sound.Play("menuback"); }, desc: Localized("workshop." + u.id + ".desc"),
                    dim: level >= Workshop.MaxLevel || SaveGame.Tickets < Workshop.Cost(i));
            }
            Add(Localized("menu.back"), Back, back: true);
            return;
        }
        if (page == 3)
        {
            foreach (var a in Achievements.All)
            {
                bool won = SaveGame.HasAchievement(a.id);
                Add((won ? "✓  " : "") + Localized("ach." + a.id), () => { }, desc: Localized("ach." + a.id + ".desc") + "  ·  " + Localized("ach.reward", a.reward), dim: !won);
            }
            Add(Localized("menu.back"), Back, back: true);
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
        if (Application.isPlaying) foreach (var (label, run, adjust) in Comfort.Rows()) Add(label, run, adjust);
        Add(Localized("language"), L10n.NextLocale, _ => L10n.NextLocale());
        Add(Localized("menu.back"), Back, back: true);
    }

    // Story mode is BBA's for now: her locomotive, the chapter picked, its opening told first.
    // The story saved: its chapter from the start, told first, with what the chapters before it handed on.
    void Continue()
    {
        Locomotives.StoryMode = true;
        Locomotives.Select(SaveGame.Locomotive);
        Story.Play(SaveGame.Chapter);
        Wipe.Load("historiaDialogos");
    }


    void SelectJourney(bool story)
    {
        Locomotives.StoryMode = story;
        if (story) Story.Play(0); // a new story: the save is replaced once the locomotive is picked (SelectLocomotive)
        Wipe.Load("selectHero");
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
            (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame))) { Sound.Play("menuback"); Back(); }

        Build(); // what was just changed shows at once
        RefreshUI();
    }

    // Where option i sits: down a slant, each a little further right than the one above (as the slab leans).
    float Gap => settings ? Mathf.Min(60f, 590f / Mathf.Max(1, rows.Count)) : 68f;
    Vector2 Spot(int i) => new Vector2(150 + i * (settings ? 6 : 22), (settings ? 230 : 90) - i * Gap);

    public void RefreshUI()
    {
        Build();
        float age = Application.isPlaying ? Time.unscaledTime - openedAt : 10f, now = Application.isPlaying ? Time.unscaledTime : 0f;
        float In(float delay, float length) => UiKit.Back((age - delay) / length);
        for(int i=0;i<MaxRows;i++) {
            bool used=i<rows.Count;
            lines[i].gameObject.SetActive(used);
            if(!used) continue;
            bool on=i==focus;
            lines[i].text=rows[i].label.Replace("\n"," ").ToUpperInvariant();
            lines[i].fontSize=settings ? Mathf.RoundToInt(Mathf.Clamp(Gap*0.62f,20f,32f))+(on ? 6 : 0) : on ? 54 : 42;
            float slide=1f-In(0.35f+i*0.06f,0.3f);
            lines[i].rectTransform.anchoredPosition=Spot(i)+new Vector2((on ? 34 : 0)-slide*700,0);
            lines[i].color=on ? Color.white : new Color(1,1,1,rows[i].dim ? .4f : .72f);
            lines[i].rectTransform.localRotation=Quaternion.Euler(0,0,on ? 3 : 0);
        }
        // the focus bar: a red slab behind the option, a white sliver over its left end, beating a little
        Vector2 at=Spot(focus)+new Vector2(14,0);
        bar.anchoredPosition=Vector2.Lerp(bar.anchoredPosition,at+new Vector2(220,-2),Application.isPlaying ? 1f-Mathf.Exp(-22f*Time.unscaledDeltaTime) : 1f);
        bar.sizeDelta=new Vector2(settings ? 660 : 640,settings ? Gap*0.9f : 74);
        bar.localScale=Vector3.one*(1f+0.02f*Mathf.Sin(now*6f));
        var highlight=bar.GetComponent<Slanted>();
        highlight.color=highlight.bottom=UiKit.Now.main;
        highlight.fade=false;
        highlight.SetVerticesDirty();
        bar.gameObject.SetActive(age>0.5f);
        // what the option in focus does, under the list
        description.text=focus<rows.Count ? rows[focus].desc ?? "" : "";
        description.rectTransform.anchoredPosition=new Vector2(110,settings ? Mathf.Max(Spot(rows.Count).y-6,-405) : Spot(rows.Count).y-4);
        wordmark.gameObject.SetActive(!settings);
        pageTitle.gameObject.SetActive(settings);
        pageTitle.text=(page==2 ? Localized("main.workshop")+"  ·  "+Localized("workshop.tickets",SaveGame.Tickets)
                       : page==3 ? Localized("main.achievements")+"  "+Achievements.Count+"/"+Achievements.All.Length
                       : Localized("menu.settings")).ToUpperInvariant();
        if(operationsLabel) operationsLabel.text="01  /  "+Localized("main.operations");
        // the entrance: the slab from the left, the name slamming down, the card from the right; then the art drifts,
        // the card floats and the streaks run
        if(Application.isPlaying) {
            slab.anchoredPosition=slabRest+new Vector2(-1400*(1f-In(0f,0.35f)),0);
            float slam=Mathf.Clamp01((age-0.2f)/0.25f);
            wordmark.anchoredPosition=wordRest+new Vector2(0,(1f-UiKit.Back(slam))*260);
            wordmark.localScale=Vector3.one*Mathf.Lerp(1.6f,1f,slam);
            card.anchoredPosition=cardRest+new Vector2(1200*(1f-In(0.25f,0.4f)),Mathf.Sin(now*0.9f)*10);
            card.localRotation=Quaternion.Euler(0,0,-5+Mathf.Sin(now*0.7f)*0.8f);
            float drift=Mathf.PingPong(now*0.01f,0.06f);
            art.uvRect=new Rect(drift,drift*0.5f,1f-0.06f,1f-0.06f);
            for(int k=0;k<streaks.childCount;k++) {
                var s=(RectTransform)streaks.GetChild(k);
                s.anchoredPosition=new Vector2(Mathf.Repeat(now*(380+k*90)+k*700,3600)-1800,s.anchoredPosition.y);
            }
        }
        hint.text="";
        if(!hintLine) hintLine=hint.GetComponentInChildren<Prompt>(); // one saved with the scene
        if(!hintLine) hintLine=Prompt.Make(hint.transform,Vector2.zero,20,Color.white); // the buttons drawn
        hintLine.Set(Localized(settings ? (pad ? "sound.help.gamepad":"sound.help.keyboard") : (pad ? "pause.help.gamepad":"pause.help.keyboard"),UiKit.ConfirmGlyph(pad)));
    }

    public void BuildUI()
    {
        canvas=UiKit.NewCanvas("MainMenuCanvas",0);
        canvas.GetComponent<CanvasScaler>().matchWidthOrHeight=.5f;
        // the railway over the sea, full screen, a little dimmed on the left where the slab goes
        art=new GameObject("OceanRailway",typeof(RectTransform),typeof(RawImage),typeof(AspectRatioFitter)).GetComponent<RawImage>();
        art.transform.SetParent(canvas.transform,false);art.texture=Resources.Load<Texture2D>("Menu/title");art.raycastTarget=false;
        var fit=art.GetComponent<AspectRatioFitter>();fit.aspectMode=AspectRatioFitter.AspectMode.EnvelopeParent;fit.aspectRatio=16f/9;
        // streaks of light running across, tilted
        streaks=UiKit.Group(canvas.transform,new Vector2(.5f,.5f),Vector2.zero);streaks.name="Streaks";
        streaks.localRotation=Quaternion.Euler(0,0,14);
        for(int k=0;k<7;k++) {
            var s=UiKit.Panel(streaks,new Vector2(k*500-1500,-520+k*170),new Vector2(260+k%3*180,k%2==0 ? 5 : 3),new Color(1,1,1,.55f),new Color(1,1,1,.55f),0);
            s.sprite=null;
        }
        // the slab: a hard red shadow, a white edge, the near-black slab with dots over it
        slab=UiKit.Group(canvas.transform,new Vector2(0,.5f),Vector2.zero);slab.name="Slab";
        RectTransform tilt=UiKit.Group(slab,new Vector2(.5f,.5f),new Vector2(330,0));
        tilt.localRotation=Quaternion.Euler(0,0,-8);
        UiKit.Paint(UiKit.Panel(tilt,new Vector2(64,-20),new Vector2(980,1700),Color.white,Color.white,0),UiKit.Ink.Main).sprite=null;
        UiKit.Panel(tilt,new Vector2(30,0),new Vector2(980,1700),Color.white,Color.white,0).sprite=null;
        UiKit.Paint(UiKit.Panel(tilt,Vector2.zero,new Vector2(980,1700),Color.white,Color.white,0),UiKit.Ink.Deep,.95f).sprite=null;
        var dots=UiKit.Panel(tilt,Vector2.zero,new Vector2(980,1700),Color.white,Color.white,0);
        dots.sprite=UiKit.Halftone();dots.type=Image.Type.Tiled;
        UiKit.Paint(dots,UiKit.Ink.Main,.1f);
        // BBA breaking out of a tilted card, right: a red shadow, a white frame, inside it red with dots and her cut-out
        // (Tools: the menu art with its background taken off in ComfyUI) standing over it, out past its top
        card=UiKit.Group(canvas.transform,new Vector2(1,.5f),new Vector2(-420,0));card.name="Card";
        UiKit.Paint(UiKit.Panel(card,new Vector2(22,-22),new Vector2(520,700),Color.white,Color.white,.08f),UiKit.Ink.Deep).sprite=null;
        UiKit.Panel(card,Vector2.zero,new Vector2(520,700),Color.white,Color.white,.08f).sprite=null;
        UiKit.Paint(UiKit.Panel(card,Vector2.zero,new Vector2(490,670),Color.white,Color.white,.08f),UiKit.Ink.Deep).sprite=null; // dark, so her red stands out
        var inside=UiKit.Panel(card,Vector2.zero,new Vector2(490,670),Color.white,Color.white,.08f);
        inside.sprite=UiKit.Halftone();inside.type=Image.Type.Tiled;
        UiKit.Paint(inside,UiKit.Ink.Main,.45f);
        foreach(float y in new[] { 120f, -60f }) UiKit.Panel(card,new Vector2(0,y),new Vector2(490,14),new Color(1,1,1,.18f),new Color(1,1,1,.18f),.08f).sprite=null;
        var shade=new GameObject("HeroShadow",typeof(RectTransform),typeof(RawImage)).GetComponent<RawImage>();
        var hero=new GameObject("Hero",typeof(RectTransform),typeof(RawImage)).GetComponent<RawImage>();
        foreach(var (pic,at) in new[] { (shade,new Vector2(0,76)), (hero,new Vector2(-18,88)) }) {
            pic.transform.SetParent(card,false);pic.raycastTarget=false;
            pic.texture=Resources.Load<Texture2D>("Menu/hero_bba_cut");
            pic.rectTransform.sizeDelta=new Vector2(600,877);
            pic.rectTransform.anchoredPosition=at;
        }
        shade.color=new Color(0.9f,0.12f,0.2f,1f); // a hard shadow of them in the menu's colour, offset
        heroArt=hero;heroShade=shade;
        var tag=UiKit.Paint(UiKit.Panel(card,new Vector2(-110,-370),new Vector2(420,70),Color.white,Color.white,.25f),UiKit.Ink.Main);
        tag.sprite=null;tag.rectTransform.localRotation=Quaternion.Euler(0,0,4);
        heroName=Label(tag.transform,"BBA",54,Color.white,new Vector2(10,2),new Vector2(380,66),UiKit.Display);
        heroName.alignment=TextAnchor.MiddleCenter;
        // the name, top left, tilted, on a red shadow, a gold stroke under it
        var left=UiKit.Group(canvas.transform,new Vector2(0,.5f),Vector2.zero);
        wordmark=UiKit.Group(left,new Vector2(.5f,.5f),new Vector2(0,0));
        wordmark.localRotation=Quaternion.Euler(0,0,6);
        foreach(var (word,y,size) in new[] { ("WAIFU",400,170), ("ON RAILS",262,128) }) {
            UiKit.Paint(Label(wordmark,word,size,Color.white,new Vector2(504,y-12),new Vector2(800,190),UiKit.Display),UiKit.Ink.Main);
            Label(wordmark,word,size,Color.white,new Vector2(490,y),new Vector2(800,190),UiKit.Display);
        }
        UiKit.Paint(UiKit.Panel(wordmark,new Vector2(370,178),new Vector2(560,10),Color.white,Color.white,0),UiKit.Ink.Hot).sprite=null;
        operationsLabel=Label(wordmark,"01  /  "+Localized("main.operations"),22,new Color(1,1,1,.7f),new Vector2(440,146),new Vector2(700,36),UiKit.Display);
        pageTitle=Label(left,"",96,Color.white,new Vector2(490,330),new Vector2(800,120),UiKit.Display);
        pageTitle.rectTransform.localRotation=Quaternion.Euler(0,0,4);
        // the options and their bar
        var red=UiKit.Panel(left,Vector2.zero,new Vector2(640,82),Color.white,Color.white,.3f);red.sprite=null;bar=red.rectTransform;
        UiKit.Panel(red.transform,new Vector2(-300,0),new Vector2(14,82),Color.white,Color.white,.3f).sprite=null;
        for(int i=0;i<MaxRows;i++) {
            lines[i]=Label(left,"",44,Color.white,Vector2.zero,new Vector2(620,90),UiKit.Display);
            lines[i].rectTransform.pivot=new Vector2(0,.5f);
            lines[i].raycastTarget=true;
            var rim=lines[i].gameObject.AddComponent<Outline>();rim.effectColor=new Color(0,0,0,.85f);rim.effectDistance=new Vector2(3,-3);
        }
        description=Label(left,"",22,new Color(1,1,1,.85f),Vector2.zero,new Vector2(470,60),UiKit.Body);
        description.rectTransform.pivot=new Vector2(0,.5f);
        description.horizontalOverflow=HorizontalWrapMode.Wrap;
        Label(left,"WAIFU ON RAILS  /  "+Application.version,16,new Color(1,1,1,.5f),new Vector2(365,-440),new Vector2(550,34),UiKit.Body);
        // the controls, along the foot
        var footer=UiKit.Group(canvas.transform,new Vector2(.5f,0),Vector2.zero);footer.name="Footer";
        UiKit.Paint(UiKit.Panel(footer,new Vector2(0,33),new Vector2(2400,66),Color.white,Color.white,0),UiKit.Ink.Deep,.92f).sprite=null;
        UiKit.Paint(UiKit.Panel(footer,new Vector2(0,67),new Vector2(2400,4),Color.white,Color.white,0),UiKit.Ink.Main).sprite=null;
        hint=Label(canvas.transform,"",20,Color.white,new Vector2(0,33),new Vector2(1600,42),UiKit.Body);
        hint.rectTransform.anchorMin=hint.rectTransform.anchorMax=new Vector2(.5f,0);hint.alignment=TextAnchor.MiddleCenter;
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

