using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// One slot per locomotive, plus a random choice drawn only from the ones that are not locked.
// A locked one can be looked at, preview and all, but not taken out.
public class SelectLocomotive : MonoBehaviour
{
    const int Slots=12, RandomSlot=12, Start=13, Back=14, Columns=4;
    public string gameScene="sandScenario";
    public Transform[] previews;
    public Transform lockedPreview;
    public float maximumSpeed=20, initialHealth=500;
    [SerializeField] Canvas canvas;
    [SerializeField] Canvas backgroundCanvas;
    [SerializeField] Slanted[] cards;
    [SerializeField] UnityEngine.UI.RawImage[] portraits;
    [SerializeField] UnityEngine.UI.Text[] slotNames, badges;
    [SerializeField] UnityEngine.UI.Text title, eyebrow, description, counter, startText, backText, hint, rosterTitle, dataTitle, speed, health, status, question;
    [SerializeField] Slanted startPanel, backPanel;
    InputAction move, confirm;
    int selected, focus;
    bool held, pad, leaving;
    bool Exists => selected<Locomotives.Models.Length;
    bool Available => selected==RandomSlot || (Exists && !Locomotives.Locked(selected));

    void Awake()
    {
        Time.timeScale=1;Display.Apply();
        if(!canvas || cards.Length!=Slots+1) BuildUI();
        if(!backgroundCanvas) BuildBackdrop();
        // The scene holds a preview of the first ones; the rest are built here, standing as those stand.
        Transform stage=previews[0].parent;
        System.Array.Resize(ref previews,Locomotives.Models.Length);
        for(int i=0;i<previews.Length;i++) if(!previews[i]) {
            previews[i]=Locomotives.Create(stage,i);previews[i].localRotation=previews[0].localRotation;
        }
        selected=focus=Locomotives.Selected;
        for(int i=0;i<cards.Length;i++) Bind(cards[i],i);
        Bind(startPanel,Start);Bind(backPanel,Back);
        InputSystem.actions.Enable();
        move=InputSystem.actions.FindAction("Player/Move",true);
        confirm=InputSystem.actions.FindAction("Player/Jump",true);
        Refresh();
    }

    void Bind(Slanted panel,int index)
    {
        panel.raycastTarget=true;
        var clicks=panel.GetComponent<Clicks>() ?? panel.gameObject.AddComponent<Clicks>();
        clicks.index=index;clicks.click=Activate;
        clicks.hover=i=>{focus=i;if(i<=RandomSlot) selected=i;Refresh();};
    }

    void Activate(int index)
    {
        if(leaving) return;
        focus=index;
        if(index<=RandomSlot) {selected=index;Refresh();return;}
        if(index==Start && !Available) return;
        leaving=true;
        if(index==Start) {
            int choice=selected;
            if(selected==RandomSlot) do choice=Random.Range(0,Locomotives.Models.Length); while(Locomotives.Locked(choice));
            Locomotives.Select(choice);
            SceneManager.LoadScene(gameScene);
        } else SceneManager.LoadScene("MainMenu");
    }

    void Update()
    {
        if(leaving) return;
        Vector2 stick=move.ReadValue<Vector2>();
        bool vertical=Mathf.Abs(stick.y)>.5f && Mathf.Abs(stick.y)>=Mathf.Abs(stick.x);
        bool horizontal=Mathf.Abs(stick.x)>.5f && !vertical;
        if(!held && (vertical||horizontal)) {
            if(focus<=RandomSlot) {
                if(vertical && stick.y<0 && focus+Columns>RandomSlot) focus=Start;
                else {
                    int step=vertical ? (stick.y>0 ? -Columns:Columns) : (stick.x>0 ? 1:-1);
                    focus=(focus+step+Slots+1)%(Slots+1);selected=focus;
                }
            } else if(vertical && stick.y>0) focus=selected;
            else focus=focus==Start ? Back:Start;
        }
        held=vertical||horizontal;
        if(held && move.activeControl!=null) pad=move.activeControl.device is Gamepad;
        var keyboard=Keyboard.current;
        if(confirm.WasPressedThisFrame() || (keyboard!=null && keyboard.enterKey.wasPressedThisFrame)) {
            if(focus<=RandomSlot) {if(Available) focus=Start;}
            else Activate(focus);
        }
        if((keyboard!=null && keyboard.escapeKey.wasPressedThisFrame) || (Gamepad.current!=null && Gamepad.current.buttonEast.wasPressedThisFrame)) Activate(Back);
        Refresh();
    }

    public void BuildUI()
    {
        if(canvas) {
            if(Application.isPlaying) Destroy(canvas.gameObject);
            else DestroyImmediate(canvas.gameObject);
        }
        canvas=UiKit.NewCanvas("LocomotiveMenu",0);
        canvas.GetComponent<UnityEngine.UI.CanvasScaler>().matchWidthOrHeight=.5f;
        RectTransform left=UiKit.Group(canvas.transform,new Vector2(0,.5f),new Vector2(80,0));
        eyebrow=Label(left,26,new Vector2(450,450),new Vector2(900,40));
        title=Label(left,64,new Vector2(495,380),new Vector2(990,100));title.font=UiKit.Heading;
        UiKit.Panel(left,new Vector2(495,320),new Vector2(990,4),UiKit.Sky,UiKit.Sky,0);
        question=Label(left,210,new Vector2(495,20),new Vector2(650,280));question.alignment=TextAnchor.MiddleCenter;
        var data=UiKit.Panel(left,new Vector2(495,-345),new Vector2(990,192),new Color(.035f,.07f,.14f,.93f),new Color(.035f,.07f,.14f,.93f),.08f);
        dataTitle=Label(data.transform,19,new Vector2(0,61),new Vector2(900,30));
        description=Label(data.transform,25,new Vector2(0,19),new Vector2(900,36));
        speed=Label(data.transform,21,new Vector2(-300,-40),new Vector2(285,70));
        health=Label(data.transform,21,new Vector2(0,-40),new Vector2(285,70));
        status=Label(data.transform,21,new Vector2(300,-40),new Vector2(285,70));
        RectTransform right=UiKit.Group(canvas.transform,new Vector2(1,.5f),new Vector2(-445,0));
        rosterTitle=Label(right,26,new Vector2(0,385),new Vector2(650,50));rosterTitle.alignment=TextAnchor.MiddleCenter;
        counter=Label(right,20,new Vector2(0,337),new Vector2(650,32));counter.alignment=TextAnchor.MiddleCenter;
        cards=new Slanted[Slots+1];portraits=new UnityEngine.UI.RawImage[cards.Length];
        slotNames=new UnityEngine.UI.Text[cards.Length];badges=new UnityEngine.UI.Text[cards.Length];
        for(int i=0;i<cards.Length;i++) {
            int col=i%Columns,row=i/Columns;
            var position=new Vector2((col-(Columns-1)*.5f)*162+(row%2)*16-8,238-row*130);
            if(i==RandomSlot) position.x=0; // alone on its row: in the middle of it
            cards[i]=UiKit.Panel(right,position,new Vector2(154,112),UiKit.Navy,UiKit.Navy,.14f);
            cards[i].gameObject.name=i==RandomSlot ? "RandomLocomotive" : "LocomotiveSlot"+(i+1);
            var image=new GameObject("Portrait",typeof(RectTransform),typeof(UnityEngine.UI.RawImage)).GetComponent<UnityEngine.UI.RawImage>();
            image.transform.SetParent(cards[i].transform,false);image.raycastTarget=false;
            image.rectTransform.sizeDelta=new Vector2(142,89);image.rectTransform.anchoredPosition=new Vector2(0,9);
            image.texture=Resources.Load<Texture2D>(Locomotives.Portraits[i<Locomotives.Portraits.Length ? i : 0]);
            portraits[i]=image;
            slotNames[i]=Label(cards[i].transform,13,new Vector2(0,-43),new Vector2(146,22));slotNames[i].alignment=TextAnchor.MiddleCenter;
            badges[i]=Label(cards[i].transform,i==RandomSlot ? 56:14,new Vector2(0,9),new Vector2(142,80));
            badges[i].alignment=TextAnchor.MiddleCenter;
        }
        startPanel=UiKit.Panel(right,new Vector2(0,-335),new Vector2(560,65),UiKit.Sky,UiKit.Sky,.12f);
        startPanel.gameObject.name="StartJourney";
        startText=Label(startPanel.transform,28,Vector2.zero,new Vector2(520,55));startText.alignment=TextAnchor.MiddleCenter;
        backPanel=UiKit.Panel(right,new Vector2(0,-416),new Vector2(320,48),UiKit.Navy,UiKit.Navy,.12f);
        backPanel.gameObject.name="BackToMainMenu";
        backText=Label(backPanel.transform,22,Vector2.zero,new Vector2(280,45));backText.alignment=TextAnchor.MiddleCenter;
        RectTransform footer=UiKit.Group(canvas.transform,new Vector2(.5f,0),new Vector2(0,35));
        hint=Label(footer,19,Vector2.zero,new Vector2(1780,38));hint.alignment=TextAnchor.MiddleCenter;
        selected=focus=Locomotives.Selected;Refresh();BuildBackdrop();
    }

    public void BuildBackdrop()
    {
        if(!backgroundCanvas) {
            backgroundCanvas=new GameObject("SelectionBackdrop",typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler)).GetComponent<Canvas>();
            backgroundCanvas.renderMode=RenderMode.ScreenSpaceCamera;
            backgroundCanvas.worldCamera=Camera.main;
            backgroundCanvas.planeDistance=60;
            backgroundCanvas.sortingOrder=-20;
            var scaler=backgroundCanvas.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
            var graphic=new GameObject("SkyBlueprint",typeof(RectTransform),typeof(CanvasRenderer),typeof(SelectionBackdrop)).GetComponent<SelectionBackdrop>();
            graphic.transform.SetParent(backgroundCanvas.transform,false);graphic.raycastTarget=false;
            graphic.rectTransform.anchorMin=Vector2.zero;graphic.rectTransform.anchorMax=Vector2.one;
            graphic.rectTransform.offsetMin=graphic.rectTransform.offsetMax=Vector2.zero;
        }
        var backdrop=backgroundCanvas.GetComponentInChildren<SelectionBackdrop>();
        if(!backdrop.GetComponent<CanvasRenderer>()) backdrop.gameObject.AddComponent<CanvasRenderer>();
        backdrop.SetAllDirty();
        title.color=eyebrow.color=rosterTitle.color=counter.color=UiKit.Navy;
    }

    UnityEngine.UI.Text Label(Transform parent,int size,Vector2 position,Vector2 box)
    {
        var label=UiKit.Label(parent,UiKit.Body,size,Color.white,TextAnchor.MiddleLeft,position,box,true);
        label.fontStyle=FontStyle.Normal;return label;
    }

    void Refresh()
    {
        if(!canvas) return;
        bool random=selected==RandomSlot;
        bool locked=!Available;
        eyebrow.text="1P  /  "+Localized("loco.collection");
        title.text=random ? Localized("loco.random") : !Exists ? Localized("loco.slot",selected+1) : Localized(Locomotives.NameKeys[selected]);
        int open=0;for(int i=0;i<Locomotives.Models.Length;i++) if(!Locomotives.Locked(i)) open++;
        rosterTitle.text=Localized("loco.select");counter.text=Localized("loco.unlocked",open,Slots);
        for(int i=0;i<cards.Length;i++) {
            bool exists=i<Locomotives.Models.Length, available=exists && !Locomotives.Locked(i), active=i==selected, roll=i==RandomSlot;
            cards[i].color=cards[i].bottom=active ? UiKit.Sky : available ? new Color(.22f,.30f,.46f) : new Color(.12f,.15f,.20f);
            // A locked one keeps its picture, dimmed, under the word.
            portraits[i].enabled=!roll;portraits[i].color=available ? Color.white : exists ? new Color(.42f,.45f,.52f) : Color.black;
            slotNames[i].text=roll ? Localized("loco.random") : exists ? Localized(Locomotives.NameKeys[i]) : Localized("loco.slot",i+1);
            slotNames[i].color=available||roll||active ? Color.white : new Color(.48f,.52f,.60f);
            badges[i].text=roll ? "?" : available ? "" : Localized("loco.locked");
            badges[i].color=roll && !active ? new Color(.58f,.63f,.72f) : Color.white;
        }
        for(int i=0;i<previews.Length;i++) if(previews[i]) previews[i].gameObject.SetActive(i==selected && !random);
        if(lockedPreview) lockedPreview.gameObject.SetActive(random||!Exists);
        question.gameObject.SetActive(random);
        question.text="?";
        description.text=random ? Localized("loco.random.description") : !Exists ? Localized("loco.locked.description") : Localized(Locomotives.DescriptionKeys[selected]);
        dataTitle.text=Localized("loco.data");
        speed.text=Localized("loco.speed",!Exists && !random ? "—":maximumSpeed.ToString("0.#"));
        health.text=Localized("loco.health",!Exists && !random ? "—":initialHealth.ToString("0"));
        status.text=Localized("loco.status",Localized(random ? "loco.random":locked ? "loco.locked":"loco.available"));
        startText.text=Localized(locked ? "loco.locked":"loco.start");backText.text=Localized("menu.back");
        startPanel.color=startPanel.bottom=locked ? new Color(.12f,.15f,.20f) : focus==Start ? UiKit.Mint : UiKit.Sky;
        backPanel.color=backPanel.bottom=focus==Back ? UiKit.Sky : UiKit.Navy;
        hint.text=Localized(pad ? "loco.help.gamepad":"loco.help.keyboard");
    }

    static string Localized(string key,params object[] args)
    {
        string value;
#if UNITY_EDITOR
        if(!Application.isPlaying) {
            var locale=UnityEngine.Localization.Settings.LocalizationSettings.ProjectLocale;
            var collection=UnityEditor.Localization.LocalizationEditorSettings.GetStringTableCollection("Game");
            var table=collection.GetTable(locale ? locale.Identifier.Code : "es") as UnityEngine.Localization.Tables.StringTable;
            value=table.GetEntry(key)?.LocalizedValue ?? key;
        } else
#endif
        value=L10n.T(key);
        return args.Length==0 ? value : string.Format(value,args);
    }
}
