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
    [SerializeField] Canvas canvas;
    [SerializeField] Canvas backgroundCanvas;
    [SerializeField] Slanted[] cards;
    [SerializeField] UnityEngine.UI.RawImage[] portraits;
    [SerializeField] UnityEngine.UI.Text[] slotNames, badges;
    [SerializeField] UnityEngine.UI.Text title, eyebrow, description, counter, startText, backText, hint, rosterTitle, dataTitle, weaponName, weaponInfo, question;
    [SerializeField] UnityEngine.UI.RawImage weaponIcon;
    [SerializeField] Slanted startPanel, backPanel;
    InputAction confirm;
    int selected, focus;
    bool held, pad, leaving;
    [SerializeField] Transform heroPreview;
    [SerializeField] UnityEngine.UI.Text heroName, heroPlaceholder;
    [SerializeField] Slanted heroPlaceholderPanel;
    [SerializeField] RectTransform heroAnchor;
    [SerializeField] UnityEngine.UI.Text pilotData;
    Vector3 heroFootOffset;
    readonly System.Collections.Generic.Dictionary<string, Transform> heroes = new(); // every hero shown so far, by its id
    Slanted heroFrame;                    // a hero with a picture (Resources/Menu/hero_<id>) is shown framed, not as a model
    UnityEngine.UI.RawImage heroPicture;
    // The other way to show a hero, being tried out: a pilot's licence, with its photo (Resources/Menu/license_<id>).
    public bool license;
    public bool pictures; // and the first way tried: the picture alone, framed. Both off: the hero's model stands there.
    Slanted heroCard;
    UnityEngine.UI.RawImage cardPhoto;
    UnityEngine.UI.Text cardTitle, cardName, cardEngine, cardNumber;
    bool heroFramed;
    bool Exists => selected<Locomotives.Models.Length;
    // One that can be taken out: opened by the save and, for a story, with a pilot who has one (the rest: coming soon).
    static bool Open(int i) => i<Locomotives.Models.Length && !Locomotives.Locked(i) && !Soon(i);
    static bool Soon(int i) => Locomotives.StoryMode && Story.Route(Locomotives.CampaignHeroId(i))==null;
    bool Available => selected==RandomSlot || Open(selected);
    float lockIn=-1f;         // when the choice was taken: the card jumps, then off
    float nextStep;           // a direction held steps again after a moment, then quicker

    void Awake()
    {
        Time.timeScale=1;Display.Apply();
        UiKit.SetBiome("menu"); // the menus' colours
        if(!canvas || cards.Length!=Slots+1 || !weaponIcon) BuildUI();
        if(!backgroundCanvas) BuildBackdrop();
        BuildHeroPreview();
        // The scene holds a preview of the first ones; the rest are built here, standing as those stand.
        Transform stage=previews[0].parent;
        System.Array.Resize(ref previews,Locomotives.Models.Length);
        for(int i=0;i<previews.Length;i++) if(!previews[i]) {
            previews[i]=Locomotives.Create(stage,i);previews[i].localRotation=previews[0].localRotation;
        }
        ComposePreview();
        selected=focus=Locomotives.Selected;
        for(int i=0;i<cards.Length;i++) Bind(cards[i],i);
        // Refresh colours these itself; their theme painter would paint over it each frame the colours blend.
        foreach(var panel in new[] {startPanel,backPanel})
            if(panel.TryGetComponent(out Themed painter)) Destroy(painter);
        Bind(startPanel,Start);Bind(backPanel,Back);
        InputSystem.actions.Enable();
        confirm=InputSystem.actions.FindAction("Player/Jump",true);
        Refresh();
    }

    void Bind(Slanted panel,int index)
    {
        panel.raycastTarget=true;
        var clicks=panel.GetComponent<Clicks>() ?? panel.gameObject.AddComponent<Clicks>();
        clicks.index=index;clicks.click=Activate;
        clicks.hover=i=>{if(leaving || lockIn>=0 || Mouse.current==null || Mouse.current.delta.ReadValue().sqrMagnitude<1f) return; // only a mouse really moved onto it, not one left resting where the cards change
        if(focus!=i) Sound.Play("click");focus=i;if(i<=RandomSlot) selected=i;Refresh();};
    }

    // A click on a card picks it; Start (or confirm on a card) takes it, Back goes back.
    void Activate(int index)
    {
        if(leaving || lockIn>=0) return;
        focus=index;
        if(index<=RandomSlot) {Sound.Play("confirm");selected=index;Refresh();return;}
        if(index==Back) {leaving=true;Sound.Play("menuback");Wipe.Load("MainMenu");return;}
        if(Available) LockIn(); else Sound.Play("menuback");
    }

    void LockIn()
    {
        Sound.Play("confirm");
        lockIn=Time.unscaledTime;focus=Start;
    }

    void Go()
    {
        // A new story wipes the one saved: ask first.
        if(Locomotives.StoryMode && SaveGame.HasStory && !asking) {asking=true;lockIn=-1f;ShowWarning(true);return;}
        leaving=true;
        int choice=selected;
        if(selected==RandomSlot) do choice=Random.Range(0,Locomotives.Models.Length); while(!Open(choice));
        Locomotives.Select(choice);
        if(Locomotives.StoryMode) { SaveGame.NewStory(choice); Story.Play(0); }
        Wipe.Load(Locomotives.StoryMode ? "historiaDialogos" : gameScene);
    }

    // The warning that a new story replaces the saved one: confirm goes on, back stays.
    bool asking;
    Canvas warning;
    void ShowWarning(bool show)
    {
        if(!warning) {
            warning=UiKit.NewCanvas("NewStoryWarning",30);
            UiKit.Paint(UiKit.Panel(warning.transform,Vector2.zero,new Vector2(2600f,1500f),Color.white,Color.white,0f),UiKit.Ink.Deep,0.85f);
            UiKit.Plate(warning.transform,Vector2.zero,new Vector2(1100f,340f),0.06f,6f,14f);
            UiKit.Label(warning.transform,UiKit.Display,52,Color.white,TextAnchor.MiddleCenter,new Vector2(0f,80f),new Vector2(1000f,70f),true).text=L10n.T("save.overwrite.title").ToUpperInvariant();
            UiKit.Label(warning.transform,UiKit.Body,26,Color.white,TextAnchor.MiddleCenter,new Vector2(0f,5f),new Vector2(1000f,80f),true).text=L10n.T("save.overwrite",SaveGame.Chapter+1);
            UiKit.Paint(UiKit.Label(warning.transform,UiKit.Body,24,Color.white,TextAnchor.MiddleCenter,new Vector2(0f,-95f),new Vector2(1000f,40f),true),UiKit.Ink.Hot).text=L10n.T("save.overwrite.help");
        }
        warning.gameObject.SetActive(show);
    }

    public void BuildHeroPreview()
    {
        if (!heroName)
        {
            // Labels left from an earlier build would stay on show, their placeholder over the hero: out they go.
            foreach (Transform old in canvas.transform)
                if (old.name == "HeroPreviewLabels") { if (Application.isPlaying) Destroy(old.gameObject); else DestroyImmediate(old.gameObject); break; }
            var previewArea = UiKit.Group(canvas.transform, new Vector2(0f, 0.5f), Vector2.zero);
            previewArea.name = "HeroPreviewLabels";
            heroAnchor = UiKit.Group(previewArea, new Vector2(0.5f, 0.5f), new Vector2(880f, -187f));
            heroAnchor.name = "HeroFeetAnchor";
            var nameBand = UiKit.Persona(UiKit.Panel(previewArea, new Vector2(895f, 375f), new Vector2(310f, 64f), UiKit.Sky, UiKit.Sky, 0.12f), 4f, 8f);
            UiKit.Paint(nameBand, UiKit.Ink.Main);
            nameBand.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 3f);
            heroName = Label(nameBand.transform, 34, new Vector2(0f, 2f), new Vector2(285f, 54f));
            heroName.font = UiKit.Display;
            heroName.alignment = TextAnchor.MiddleCenter;
            heroName.color = Color.white;
            heroPlaceholderPanel = UiKit.Panel(previewArea, new Vector2(880f, 20f), new Vector2(260f, 380f), UiKit.Navy, UiKit.Navy, 0.06f);
            heroPlaceholder = Label(heroPlaceholderPanel.transform, 90, Vector2.zero, new Vector2(210f, 300f));
            heroPlaceholder.alignment = TextAnchor.MiddleCenter;
        }
        if (!heroPreview)
        {
            heroPreview = Instantiate(Resources.Load<GameObject>("Characters/bba/bba"), transform).transform;
            heroPreview.name = "BbaHeroPreview";
            var rig = heroPreview.GetComponent<Animator>();
            if (rig) rig.enabled = false;
            Material skin = Resources.Load<Material>("Characters/bba/bba_toon");
            foreach (Renderer part in heroPreview.GetComponentsInChildren<Renderer>()) part.sharedMaterial = skin;
            if (rig && rig.isHuman)
            {
                Vector3 across = rig.GetBoneTransform(HumanBodyBones.RightUpperArm).position - rig.GetBoneTransform(HumanBodyBones.LeftUpperArm).position;
                Vector3 front = Vector3.Cross(across, Vector3.up);
                Vector3 facing = -Camera.main.transform.forward;
                facing.y = front.y = 0f;
                heroPreview.rotation = Quaternion.FromToRotation(front.normalized, facing.normalized) * heroPreview.rotation;
            }
        }
        Material previewSkin = Resources.Load<Material>("Preview/BbaMatte") ?? Resources.Load<Material>("Characters/bba/bba_toon");
        foreach (Renderer part in heroPreview.GetComponentsInChildren<Renderer>(true)) part.sharedMaterial = previewSkin;
        var animator = heroPreview.GetComponent<Animator>();
        var social = Resources.Load<RuntimeAnimatorController>("Preview/BbaSocial");
        if (animator && social)
        {
            animator.enabled = false;
            animator.runtimeAnimatorController = social;
            animator.applyRootMotion = false;
            animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            if (!heroPreview.GetComponent<PreviewHeroLook>()) heroPreview.gameObject.AddComponent<PreviewHeroLook>();
            Vector3 facing = -Camera.main.transform.forward;
            facing.y = 0f;
            heroPreview.rotation = Quaternion.LookRotation(facing);
            foreach (var skin in heroPreview.GetComponentsInChildren<SkinnedMeshRenderer>()) skin.updateWhenOffscreen = true;
            animator.enabled = true;
            animator.Rebind();
            animator.Update(0f);
        }
        Canvas.ForceUpdateCanvases();
        heroFramed = false;
        PositionHeroPreview();
        if (animator && social) animator.enabled = true;
    }

    // The hero that goes with a locomotive, built the first time it is asked for: its model in its toon looks,
    // facing the viewer, with the greeting bba has (they are all humanoids, so her gesture fits them).
    Transform HeroFor(string id)
    {
        if (heroes.Count == 0 && heroPreview) heroes["bba"] = heroPreview;
        if (heroes.TryGetValue(id, out Transform hero)) return hero;
        if (id == "giger" || id == "gabriel" || id == "nath") return null;
        string who = "Characters/" + id + "/" + id;
        hero = Instantiate(Resources.Load<GameObject>(who), transform).transform;
        hero.name = id + "HeroPreview";
        Player.Skin(hero, who);
        Vector3 facing = -Camera.main.transform.forward;
        facing.y = 0f;
        hero.rotation = Quaternion.LookRotation(facing);
        var animator = hero.GetComponent<Animator>();
        var social = Resources.Load<RuntimeAnimatorController>("Preview/BbaSocial");
        if (animator && social)
        {
            animator.runtimeAnimatorController = social;
            animator.applyRootMotion = false;
            animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            hero.gameObject.AddComponent<PreviewHeroLook>();
            animator.Rebind();
            animator.Update(0f);
        }
        foreach (var skin in hero.GetComponentsInChildren<SkinnedMeshRenderer>()) skin.updateWhenOffscreen = true;
        return heroes[id] = hero;
    }

    // The licence: a pale card with a dark heading, the photo in a blue border, the hero's name, the locomotive it
    // is licensed for, a number and a bar code.
    void BuildLicense()
    {
        Color paper = new Color(0.95f, 0.97f, 1f);
        heroCard = UiKit.Panel(heroPlaceholderPanel.transform.parent, new Vector2(880f, 62f), new Vector2(304f, 437f), paper, paper, 0f);
        heroCard.gameObject.name = "HeroLicense";
        Transform card = heroCard.transform;
        UnityEngine.UI.Text Line(int size, float y, Color colour)
        {
            var line = Label(card, size, new Vector2(0f, y), new Vector2(288f, size + 12f));
            line.alignment = TextAnchor.MiddleCenter;
            line.color = colour;
            return line;
        }
        UiKit.Panel(card, new Vector2(0f, 191f), new Vector2(304f, 55f), UiKit.Navy, UiKit.Navy, 0f);
        UiKit.Panel(card, new Vector2(0f, 160f), new Vector2(304f, 6f), UiKit.Sky, UiKit.Sky, 0f);
        cardTitle = Line(19, 191f, Color.white);
        UiKit.Panel(card, new Vector2(0f, 36f), new Vector2(184f, 228f), UiKit.Sky, UiKit.Sky, 0f);
        cardPhoto = new GameObject("Photo", typeof(RectTransform), typeof(UnityEngine.UI.RawImage)).GetComponent<UnityEngine.UI.RawImage>();
        cardPhoto.transform.SetParent(card, false);
        cardPhoto.raycastTarget = false;
        cardPhoto.rectTransform.sizeDelta = new Vector2(174f, 218f);
        cardPhoto.rectTransform.anchoredPosition = new Vector2(0f, 36f);
        cardName = Line(27, -102f, UiKit.Navy);
        cardEngine = Line(15, -133f, UiKit.Navy);
        cardNumber = Line(14, -154f, new Color(0.35f, 0.42f, 0.55f));
        for (int bar = 0; bar < 30; bar++)
            UiKit.Panel(card, new Vector2(-123f + bar * 8.5f, -192f), new Vector2(2f + bar * 7 % 3 * 2f, 28f), UiKit.Navy, UiKit.Navy, 0f);
    }

    public void ComposePreview()
    {
        if (previews == null || previews.Length == 0 || !previews[0] || !Camera.main) return;
        Transform stage = previews[0].parent;
        stage.position = Camera.main.transform.right * -1.7f - Camera.main.transform.up * 0.25f;
        foreach (Transform preview in previews) if (preview) preview.localScale = Vector3.one * 1.0f;
        if (lockedPreview) lockedPreview.localScale = Vector3.one;
        if (!backgroundCanvas.transform.Find("PreviewPlatform"))
        {
            var platform = UiKit.Group(backgroundCanvas.transform, new Vector2(0f, 0.5f), Vector2.zero);
            platform.name = "PreviewPlatform";
            UiKit.Panel(platform,new Vector2(565f,-216f),new Vector2(970f,85f),new Color(0.10f,0.55f,0.84f,0.5f),new Color(0.10f,0.55f,0.84f,0.5f),0.15f);
            UiKit.Panel(platform,new Vector2(550f,-197f),new Vector2(920f,65f),new Color(0.88f,0.97f,1f,0.8f),new Color(0.88f,0.97f,1f,0.8f),0.15f);
            UiKit.Panel(platform,new Vector2(430f,-189f),new Vector2(510f,25f),new Color(0.08f,0.20f,0.30f,0.22f),new Color(0.08f,0.20f,0.30f,0.22f),0.2f);
            UiKit.Panel(platform,new Vector2(880f,-213f),new Vector2(185f,25f),new Color(0.08f,0.20f,0.30f,0.28f),new Color(0.08f,0.20f,0.30f,0.28f),0.2f);
        }
    }

    void PositionHeroPreview()
    {
        if (!heroPreview || !Camera.main) return;
        Camera cam = Camera.main;
        if (!heroFramed)
        {
            var parts = heroPreview.GetComponentsInChildren<Renderer>(true);
            Bounds bounds = parts[0].bounds;
            foreach (Renderer part in parts) bounds.Encapsulate(part.bounds);
            heroPreview.localScale *= 4.6f / Mathf.Max(0.01f, bounds.size.y);
            bounds = parts[0].bounds;
            foreach (Renderer part in parts) bounds.Encapsulate(part.bounds);
            Vector3 anchor = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            var animator = heroPreview.GetComponent<Animator>();
            if (animator && animator.isHuman && animator.runtimeAnimatorController)
            {
                Vector3 hips = animator.GetBoneTransform(HumanBodyBones.Hips).position;
                float feet = Mathf.Min(animator.GetBoneTransform(HumanBodyBones.LeftFoot).position.y, animator.GetBoneTransform(HumanBodyBones.RightFoot).position.y);
                anchor = new Vector3(hips.x, feet - 0.08f, hips.z);
            }
            heroFootOffset = heroPreview.InverseTransformVector(anchor - heroPreview.position);
            heroFramed = true; // Animated bounds must not resize and recenter her on every gesture.
        }
        // Position by the Canvas label so the hero remains beside the train at different aspect ratios.
        Vector3 screen = RectTransformUtility.WorldToScreenPoint(canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera, heroAnchor.position);
        Vector3 foot = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, 18f));
        heroPreview.position = foot - heroPreview.TransformVector(heroFootOffset);
    }

    void Update()
    {
        Animate();
        if(leaving) return;
        if(asking) {
            var keys=Keyboard.current;var padNow=Gamepad.current;
            if((keys!=null && (keys.spaceKey.wasPressedThisFrame || keys.enterKey.wasPressedThisFrame)) || (padNow!=null && padNow.buttonSouth.wasPressedThisFrame)) {ShowWarning(false);LockIn();}
            else if((keys!=null && keys.escapeKey.wasPressedThisFrame) || (padNow!=null && padNow.buttonEast.wasPressedThisFrame)) {
                asking=false;ShowWarning(false);Sound.Play("menuback");
            }
            if(lockIn<0) return;
        }
        if(lockIn>=0) {if(Time.unscaledTime-lockIn>.8f) Go();Refresh();return;}
        // Read off the devices themselves: the Move action, shared by a keyboard and a pad that reports all the time
        // (a DualSense), flickers between them, and every flicker counted as a new press.
        Vector2 stick=Vector2.zero;
        var board=Keyboard.current;
        if(board!=null) stick=new Vector2((board.dKey.isPressed||board.rightArrowKey.isPressed ? 1:0)-(board.aKey.isPressed||board.leftArrowKey.isPressed ? 1:0),
                                         (board.wKey.isPressed||board.upArrowKey.isPressed ? 1:0)-(board.sKey.isPressed||board.downArrowKey.isPressed ? 1:0));
        if(stick!=Vector2.zero) pad=false;
        else if(Gamepad.current!=null) {
            Vector2 cross=Gamepad.current.dpad.ReadValue();
            stick=cross!=Vector2.zero ? cross : Gamepad.current.leftStick.ReadValue();
            if(stick.sqrMagnitude>.25f) pad=true;
        }
        bool vertical=Mathf.Abs(stick.y)>.5f && Mathf.Abs(stick.y)>=Mathf.Abs(stick.x);
        bool horizontal=Mathf.Abs(stick.x)>.5f && !vertical;
        if((vertical||horizontal) && (!held || Time.unscaledTime>=nextStep)) {
            nextStep=Time.unscaledTime+(held ? .14f:.5f);
            int was=focus;
            if(focus<=RandomSlot) {
                if(vertical && stick.y<0 && focus+Columns>RandomSlot) focus=Start;
                else {
                    int step=vertical ? (stick.y>0 ? -Columns:Columns) : (stick.x>0 ? 1:-1);
                    focus=(focus+step+Slots+1)%(Slots+1);selected=focus;
                }
            } else if(vertical) focus=stick.y>0 ? (focus==Back ? Start:selected) : Back;
            if(focus!=was) Sound.Play("click");
        }
        held=vertical||horizontal;
        var keyboard=Keyboard.current;
        if(confirm.WasPressedThisFrame() || (keyboard!=null && keyboard.enterKey.wasPressedThisFrame)) {
            if(focus<=RandomSlot) {if(Available) LockIn(); else Sound.Play("menuback");}
            else Activate(focus);
        }
        if((keyboard!=null && keyboard.escapeKey.wasPressedThisFrame) || (Gamepad.current!=null && Gamepad.current.buttonEast.wasPressedThisFrame)) Activate(Back);
        Refresh();
    }

    // The choice taken: its card and Start jump.
    void Animate()
    {
        if(!canvas) return;
        float punch=lockIn<0 ? 0f : Mathf.Sin(Mathf.Clamp01((Time.unscaledTime-lockIn)/.3f)*Mathf.PI)*.15f;
        for(int i=0;i<cards.Length;i++) cards[i].transform.localScale=Vector3.one*(i==selected ? 1f+punch : 1f);
        startPanel.transform.localScale=Vector3.one*(1f+punch);
    }

    public void BuildUI()
    {
        // These belong to the old Canvas, whose destruction is deferred in Play mode.
        heroName = null;
        heroPlaceholder = null;
        heroPlaceholderPanel = null;
        heroAnchor = null;
        if(canvas) {
            if(Application.isPlaying) Destroy(canvas.gameObject);
            else DestroyImmediate(canvas.gameObject);
        }
        canvas=UiKit.NewCanvas("LocomotiveMenu",0);
        canvas.GetComponent<UnityEngine.UI.CanvasScaler>().matchWidthOrHeight=.5f;
        RectTransform left=UiKit.Group(canvas.transform,new Vector2(0,.5f),new Vector2(80,0));
        eyebrow=Label(left,30,new Vector2(450,450),new Vector2(900,40));eyebrow.font=UiKit.Display;
        title=Label(left,96,new Vector2(300,384),new Vector2(600,120));title.font=UiKit.Display;title.fontStyle=FontStyle.Italic;
        UiKit.Paint(UiKit.Panel(left,new Vector2(495,316),new Vector2(990,8),Color.white,Color.white,0),UiKit.Ink.Main);
        description=Label(left,23,new Vector2(495,288),new Vector2(990,36));
        question=Label(left,210,new Vector2(495,20),new Vector2(650,280));question.alignment=TextAnchor.MiddleCenter;
        var data=UiKit.Persona(UiKit.Panel(left,new Vector2(495,-345),new Vector2(990,192),new Color(.035f,.07f,.14f,.93f),new Color(.035f,.07f,.14f,.93f),.08f),6f,14f);
        dataTitle=Label(data.transform,19,new Vector2(0,61),new Vector2(900,30));
        // The pilot's weapon, the one thing that sets one locomotive apart: its icon on a disc, its name, what it does.
        var disc=UiKit.Panel(data.transform,new Vector2(-400,-14),new Vector2(118,118),Color.white,Color.white,0);
        disc.sprite=UiKit.Disc();UiKit.Paint(disc,UiKit.Ink.Main);
        weaponIcon=new GameObject("WeaponIcon",typeof(RectTransform),typeof(UnityEngine.UI.RawImage)).GetComponent<UnityEngine.UI.RawImage>();
        weaponIcon.transform.SetParent(disc.transform,false);weaponIcon.raycastTarget=false;weaponIcon.rectTransform.sizeDelta=new Vector2(86,86);
        weaponName=Label(data.transform,44,new Vector2(-95,12),new Vector2(450,54));weaponName.font=UiKit.Display;
        weaponInfo=Label(data.transform,21,new Vector2(-95,-44),new Vector2(450,58));
        UiKit.Paint(UiKit.Panel(data.transform,new Vector2(245,0),new Vector2(4,140),Color.white,Color.white,0),UiKit.Ink.Main);
        pilotData=Label(data.transform,22,new Vector2(365,-8),new Vector2(215,116));
        RectTransform right=UiKit.Group(canvas.transform,new Vector2(1,.5f),new Vector2(-445,0));
        rosterTitle=Label(right,40,new Vector2(0,385),new Vector2(650,56));rosterTitle.alignment=TextAnchor.MiddleCenter;rosterTitle.font=UiKit.Display;
        counter=Label(right,20,new Vector2(0,337),new Vector2(650,32));counter.alignment=TextAnchor.MiddleCenter;
        cards=new Slanted[Slots+1];portraits=new UnityEngine.UI.RawImage[cards.Length];
        slotNames=new UnityEngine.UI.Text[cards.Length];badges=new UnityEngine.UI.Text[cards.Length];
        for(int i=0;i<cards.Length;i++) {
            int col=i%Columns,row=i/Columns;
            var position=new Vector2((col-(Columns-1)*.5f)*162+(row%2)*16-8,238-row*130);
            if(i==RandomSlot) position.x=0; // alone on its row: in the middle of it
            cards[i]=UiKit.Panel(right,position,new Vector2(154,112),UiKit.Navy,UiKit.Navy,.14f); // plain: only the one picked stands out
            cards[i].gameObject.name=i==RandomSlot ? "RandomLocomotive" : "LocomotiveSlot"+(i+1);
            var image=new GameObject("Portrait",typeof(RectTransform),typeof(UnityEngine.UI.RawImage)).GetComponent<UnityEngine.UI.RawImage>();
            image.transform.SetParent(cards[i].transform,false);image.raycastTarget=false;
            image.rectTransform.sizeDelta=new Vector2(142,89);image.rectTransform.anchoredPosition=new Vector2(0,9);
            image.texture=Resources.Load<Texture2D>(Locomotives.Portraits[i<Locomotives.Portraits.Length ? i : 0]);
            portraits[i]=image;
            slotNames[i]=Label(cards[i].transform,15,new Vector2(0,-43),new Vector2(146,22));slotNames[i].alignment=TextAnchor.MiddleCenter;slotNames[i].font=UiKit.Display;
            badges[i]=Label(cards[i].transform,i==RandomSlot ? 56:14,new Vector2(0,9),new Vector2(142,80));
            badges[i].alignment=TextAnchor.MiddleCenter;
        }
        startPanel=UiKit.Panel(right,new Vector2(0,-335),new Vector2(560,65),UiKit.Sky,UiKit.Sky,.12f);
        startPanel.gameObject.name="StartJourney";
        UiKit.Persona(startPanel,5f,10f);
        startText=Label(startPanel.transform,38,Vector2.zero,new Vector2(520,60));startText.alignment=TextAnchor.MiddleCenter;startText.font=UiKit.Display;
        backPanel=UiKit.Panel(right,new Vector2(0,-416),new Vector2(320,48),UiKit.Navy,UiKit.Navy,.12f);
        backPanel.gameObject.name="BackToMainMenu";
        UiKit.Persona(backPanel,4f,7f);
        backText=Label(backPanel.transform,28,Vector2.zero,new Vector2(280,48));backText.alignment=TextAnchor.MiddleCenter;backText.font=UiKit.Display;
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
        title.color=eyebrow.color=rosterTitle.color=counter.color=Color.white; // on the dark backdrop
    }

    Prompt hintLine;

    UnityEngine.UI.Text Label(Transform parent,int size,Vector2 position,Vector2 box)
    {
        var label=UiKit.Label(parent,UiKit.Body,size,Color.white,TextAnchor.MiddleLeft,position,box,true);
        label.fontStyle=FontStyle.Normal;return label;
    }

    void Refresh()
    {
        bool rolling=selected==RandomSlot;
        // The hero's colours, blended into over a moment.
        UiKit.EaseTones(rolling || !Exists ? UiKit.Biome("menu") : UiKit.Hero(Locomotives.CampaignHeroId(selected)),Application.isPlaying ? Time.unscaledDeltaTime*8f : 1f);
        if(!canvas) return;
        bool random=selected==RandomSlot;
        bool locked=!Available;
        if (heroName)
        {
            // The selected locomotive's own hero stands there; the one that stood there before steps out.
            string id = Exists && !random ? Locomotives.CampaignHeroId(selected) : null;
            Texture2D picture = (pictures || id == "giger" || id == "gabriel" || id == "nath") && id != null && Application.isPlaying ? Resources.Load<Texture2D>(id == "giger" ? "Menu/loco_beast" : "Menu/hero_" + id) : null;
            if (picture && !heroFrame)
            {
                heroFrame = UiKit.Panel(heroPlaceholderPanel.transform.parent, new Vector2(880f, 62f), new Vector2(304f, 437f), UiKit.Now.main, UiKit.Now.main, 0f);
                heroFrame.gameObject.name = "HeroFrame";
                heroPicture = new GameObject("HeroPicture", typeof(RectTransform), typeof(UnityEngine.UI.RawImage)).GetComponent<UnityEngine.UI.RawImage>();
                heroPicture.transform.SetParent(heroFrame.transform, false);
                heroPicture.raycastTarget = false;
                heroPicture.rectTransform.sizeDelta = new Vector2(288f, 421f);
            }
            Texture2D photo = license && id != null && Application.isPlaying ? Resources.Load<Texture2D>("Menu/license_" + id) : null;
            if (photo && !heroCard) BuildLicense();
            if (heroCard) heroCard.gameObject.SetActive(photo);
            if (photo)
            {
                cardPhoto.texture = photo;
                cardTitle.text = Localized("license.title");
                cardName.text = Localized(Locomotives.HeroNameKey(selected));
                cardEngine.text = Localized("license.engine", Localized(Locomotives.NameKeys[selected]));
                cardNumber.text = Localized("license.number", selected + 1);
            }
            if (heroFrame) heroFrame.gameObject.SetActive(picture && !photo);
            if (picture) heroPicture.texture = picture;
            Transform hero = id == null || picture || photo ? null : Application.isPlaying ? HeroFor(id) : id == "bba" ? heroPreview : null;
            bool appearing = hero && (hero != heroPreview || !hero.gameObject.activeSelf);
            if (hero && hero != heroPreview)
            {
                heroPreview.gameObject.SetActive(false);
                heroPreview = hero;
                heroFramed = false; // sized and stood on the mark again, as itself
            }
            heroPreview.gameObject.SetActive(hero);
            if (appearing && Application.isPlaying)
            {
                var animator = heroPreview.GetComponent<Animator>();
                if (animator && animator.runtimeAnimatorController) animator.Play("SocialGreeting", 0, 0f);
            }
            heroPlaceholderPanel.gameObject.SetActive(!hero && !picture && !photo);
            heroPlaceholder.text = "?";
            heroName.text = Localized("loco.hero", random ? "?" : Localized(Exists ? Locomotives.HeroNameKey(selected) : "hero.character.test"));
            PositionHeroPreview();
        }
        eyebrow.text="1P  /  "+Localized(Locomotives.StoryMode ? "loco.mode.story":"loco.mode.endless");
        title.text=random ? Localized("loco.random") : !Exists ? Localized("loco.slot",selected+1) : Localized(Locomotives.NameKeys[selected]);
        int open=0;for(int i=0;i<Locomotives.Models.Length;i++) if(Open(i)) open++;
        rosterTitle.text=Localized("loco.select");counter.text=Localized("loco.unlocked",open,Slots);
        for(int i=0;i<cards.Length;i++) {
            bool exists=i<Locomotives.Models.Length, available=Open(i), active=i==selected, roll=i==RandomSlot;
            cards[i].color=cards[i].bottom=active ? UiKit.Now.main : available ? new Color(.2f,.12f,.16f) : new Color(.1f,.08f,.1f);
            // A locked one keeps its picture, dimmed, under the word.
            portraits[i].enabled=!roll;portraits[i].color=available ? Color.white : exists ? new Color(.42f,.45f,.52f) : Color.black;
            slotNames[i].text=roll ? Localized("loco.random") : exists ? Localized(Locomotives.NameKeys[i]) : Localized("loco.slot",i+1);
            slotNames[i].color=available||roll||active ? Color.white : new Color(.48f,.52f,.60f);
            badges[i].text=roll ? "?" : available ? "" : Localized(exists && Soon(i) && !Locomotives.Locked(i) ? "loco.soon":"loco.locked");
            badges[i].color=roll && !active ? new Color(.58f,.63f,.72f) : Color.white;
        }
        for(int i=0;i<previews.Length;i++) if(previews[i]) previews[i].gameObject.SetActive(i==selected && !random);
        if(lockedPreview) lockedPreview.gameObject.SetActive(random||!Exists);
        question.gameObject.SetActive(random);
        question.text="?";
        description.text=random || !Exists ? "" : Localized(Locomotives.DescriptionKeys[selected]);
        dataTitle.text=Localized("loco.weapon");
        Weapon gun=random || !Exists ? null : Weapon.ForHero(Locomotives.CampaignHeroId(selected));
        weaponIcon.texture=gun!=null ? UiKit.Icon(gun.id) : null;weaponIcon.enabled=gun!=null;
        weaponName.text=gun!=null ? Localized("weapon."+gun.id+".name").ToUpperInvariant() : "?";
        weaponInfo.text=gun!=null ? Localized("weapon."+gun.id+".info") : Localized(random ? "loco.random.description":"loco.locked.description");
        bool soon=Exists && !random && Soon(selected);
        if (pilotData) pilotData.text = Localized("loco.pilot.data", random ? "?" : Localized(Exists ? Locomotives.HeroNameKey(selected) : "hero.character.test"),
            random || !Exists ? "" : Localized(Story.Route(Locomotives.CampaignHeroId(selected)) != null ? "loco.story.yes" : "loco.soon"));
        startText.text=Localized(lockIn>=0 ? "loco.go" : soon && !Locomotives.Locked(selected) ? "loco.soon" : locked ? "loco.locked":"loco.start");backText.text=Localized("menu.back");
        startPanel.color=startPanel.bottom=locked ? new Color(.12f,.15f,.20f) : focus==Start && (lockIn<0 || Time.unscaledTime*12f%2f<1f) ? UiKit.Now.hot : UiKit.Now.main;
        startText.color=focus==Start && !locked ? UiKit.Now.deep : Color.white;
        backPanel.color=backPanel.bottom=focus==Back ? UiKit.Now.main : new Color(.16f,.08f,.12f);
        hint.text="";
        if(!hintLine) hintLine=hint.GetComponentInChildren<Prompt>(); // one saved with the scene
        if(!hintLine) hintLine=Prompt.Make(hint.transform,Vector2.zero,19,Color.white); // the buttons drawn
        hintLine.Set(Localized(pad ? "loco.help.gamepad":"loco.help.keyboard",UiKit.ConfirmGlyph(pad),UnityEngine.InputSystem.Gamepad.current is UnityEngine.InputSystem.DualShock.DualShockGamepad ? "○" : "xB"));
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
