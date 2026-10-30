using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Golem's two chapter finales: assemble his portal closer, then judge his thirteen choices.
public class GolemChallenge : MonoBehaviour
{
    public bool Active { get; private set; }
    enum Stage { Intro, Parts, Wires, Armed, Questions, Result }
    Stage stage;
    bool morality, held;
    int focus, picked=-1, question, high, seed, lastFrame=-1;
    float openedAt;
    Action finished;
    Canvas canvas;
    Text title, subtitle, message, footer;
    readonly List<Slanted> cards=new();
    readonly List<Text> labels=new();
    readonly List<GameObject> wires=new();
    readonly bool[] installed=new bool[3], answers=new bool[13];
    readonly int[] socketParts={2,0,1};
    readonly int[] masks=new int[25];
    readonly Stack<(int tile,int mask)> undo=new();
    public static readonly Vector2Int[] Route={new(0,2),new(1,2),new(1,1),new(2,1),new(2,2),new(2,3),new(3,3),new(3,2),new(4,2)};
    public static int Rotate(int mask)=>((mask<<1)&15)|((mask>>3)&1); // north/east/south/west
    public static bool Connected(int[] board)
    {
        if(board==null||board.Length!=25||(board[10]&8)==0)return false;
        var seen=new HashSet<int>{10};var queue=new Queue<int>();queue.Enqueue(10);
        int[] dx={0,1,0,-1},dy={-1,0,1,0};
        while(queue.Count>0)
        {
            int at=queue.Dequeue();if(at==14&&(board[at]&2)!=0)return true;
            for(int d=0;d<4;d++)
            {
                int x=at%5+dx[d],y=at/5+dy[d];if(x<0||x>=5||y<0||y>=5)continue;int next=y*5+x;
                if((board[at]&(1<<d))!=0&&(board[next]&(1<<((d+2)%4)))!=0&&seen.Add(next))queue.Enqueue(next);
            }
        }
        return false;
    }
    public static int[] SolvedBoard()
    {
        var board=new int[25];
        for(int n=0;n<Route.Length;n++)
        {
            var at=Route[n];int mask=0;
            foreach(var toward in new[]{n==0?new Vector2Int(-1,2):Route[n-1],n==Route.Length-1?new Vector2Int(5,2):Route[n+1]})
            {var delta=toward-at;mask|=delta.y<0?1:delta.x>0?2:delta.y>0?4:8;}
            board[at.y*5+at.x]=mask;
        }
        return board;
    }
    public int HighOption(int n)=>(n+seed)%2;
    public void Begin(bool judge,Action done)
    {
        if(Active)return;Active=true;morality=judge;finished=done;stage=Stage.Intro;focus=question=high=0;picked=-1;held=false;
        seed=UnityEngine.Random.Range(0,2);openedAt=Time.unscaledTime;Array.Clear(installed,0,3);Array.Clear(answers,0,13);undo.Clear();
        var solved=SolvedBoard();for(int i=0;i<25;i++){masks[i]=solved[i]==0?(i%2==0?5:3):solved[i];for(int r=0;r<UnityEngine.Random.Range(0,4);r++)masks[i]=Rotate(masks[i]);}
        if(Connected(masks))masks[10]=Rotate(masks[10]);
        cards.Clear();labels.Clear();wires.Clear();Time.timeScale=0;Build();Refresh();
    }
    void Build()
    {
        canvas=UiKit.NewCanvas(morality?"SparkJudgment":"PortalCircuit",70);
        var back=UiKit.Panel(canvas.transform,Vector2.zero,new Vector2(1920,1080),new Color(.025f,.045f,.075f,.97f),new Color(.025f,.045f,.075f,.97f),0);back.sprite=null;back.raycastTarget=true;
        title=UiKit.Label(canvas.transform,UiKit.Display,64,Color.white,TextAnchor.MiddleCenter,new Vector2(0,440),new Vector2(1700,95),true);
        subtitle=UiKit.Label(canvas.transform,UiKit.Body,30,UiKit.Now.hot,TextAnchor.MiddleCenter,new Vector2(0,350),new Vector2(1650,65),true);
        message=UiKit.Label(canvas.transform,UiKit.Body,34,Color.white,TextAnchor.MiddleCenter,new Vector2(0,245),new Vector2(1540,145),true);
        footer=UiKit.Label(canvas.transform,UiKit.Body,24,Color.white,TextAnchor.MiddleCenter,new Vector2(0,-450),new Vector2(1680,80),true);
        for(int i=0;i<25;i++)
        {
            int index=i;var card=UiKit.Panel(canvas.transform,Vector2.zero,new Vector2(100,100),Color.gray,Color.gray,.06f);card.raycastTarget=true;
            var click=card.gameObject.AddComponent<Clicks>();click.index=i;click.click=Activate;click.hover=n=>{if(focus!=n){focus=n;Sound.Play("click");Refresh();}};
            cards.Add(card);labels.Add(UiKit.Label(card.transform,UiKit.Body,28,Color.white,TextAnchor.MiddleCenter,Vector2.zero,new Vector2(90,90),true));
        }
    }
    int Count=>stage==Stage.Parts?6:stage==Stage.Wires?25:stage==Stage.Questions?2:1;
    public void Activate(int index)
    {
        if(!Active||index<0||index>=Count||lastFrame==Time.frameCount||Time.unscaledTime-openedAt<.25f)return;
        lastFrame=Time.frameCount;focus=index;Sound.Play("confirm");
        if(stage==Stage.Intro){stage=morality?Stage.Questions:Stage.Parts;focus=0;}
        else if(stage==Stage.Parts)
        {
            if(index<3){if(!installed[index])picked=index;}
            else if(picked>=0&&picked==socketParts[index-3])
            {
                installed[picked]=true;picked=-1;Sound.Play("healthup");
                if(Array.TrueForAll(installed,b=>b)){stage=Stage.Wires;focus=10;}
            }
            else{message.text=L10n.T("golem.circuit.wrong");Sound.Play("menuback");return;}
        }
        else if(stage==Stage.Wires)
        {
            undo.Push((index,masks[index]));masks[index]=Rotate(masks[index]);
            if(Connected(masks)){stage=Stage.Armed;focus=0;Sound.Play("healthup");}
        }
        else if(stage==Stage.Questions)
        {
            answers[question]=index==HighOption(question);if(answers[question])high++;
            question++;focus=0;if(question==13)stage=Stage.Result;
        }
        else
        {
            if(morality)SaveGame.GolemJudged(high);
            Active=false;Destroy(canvas.gameObject);canvas=null;finished?.Invoke();return;
        }
        Refresh();
    }
    public void Back()
    {
        if(!Active)return;Sound.Play("menuback");
        if(stage==Stage.Questions&&question>0||stage==Stage.Result)
        {
            question--;if(answers[question])high--;answers[question]=false;stage=Stage.Questions;focus=0;
        }
        else if(stage==Stage.Parts)picked=-1;
        else if((stage==Stage.Wires||stage==Stage.Armed)&&undo.Count>0)
        {
            var previous=undo.Pop();masks[previous.tile]=previous.mask;focus=previous.tile;stage=Stage.Wires;
        }
        Refresh();
    }
    void Refresh()
    {
        if(!canvas)return;
        foreach(var wire in wires)Destroy(wire);wires.Clear();
        title.text=L10n.T(morality?"golem.judge.title":"golem.circuit.title");
        subtitle.text=morality?L10n.T("golem.judge.progress",Mathf.Min(question+1,13)):L10n.T(stage==Stage.Parts?"golem.circuit.parts":stage==Stage.Wires?"golem.circuit.wires":"golem.circuit.core");
        message.text=L10n.T(stage switch{Stage.Intro=>morality?"golem.judge.intro":"golem.circuit.intro",Stage.Parts=>"golem.circuit.instructions",Stage.Wires=>"golem.circuit.connect",Stage.Armed=>"golem.circuit.ready",Stage.Questions=>$"golem.moral.{question+1}.q",_=>"golem.judge.done"});
        footer.text=L10n.T(morality?"golem.judge.controls":"golem.circuit.controls");
        for(int i=0;i<cards.Count;i++)
        {
            var card=cards[i];card.gameObject.SetActive(i<Count);if(i>=Count)continue;
            Color ink=i==focus?UiKit.Now.main:new Color(.14f,.22f,.31f);if(stage==Stage.Parts&&i<3&&(installed[i]||picked==i))ink=installed[i]?new Color(.12f,.5f,.35f):UiKit.Now.hot;
            card.color=card.bottom=ink;labels[i].color=Color.white;
            if(stage==Stage.Parts)
            {
                int id=i<3?i:socketParts[i-3];card.rectTransform.anchoredPosition=new Vector2(i<3?-400:400,90-(i%3)*145);
                card.rectTransform.sizeDelta=new Vector2(560,115);labels[i].rectTransform.sizeDelta=new Vector2(510,100);
                labels[i].text=(i<3?"":L10n.T("golem.circuit.socket")+" ")+L10n.T("golem.circuit.part."+id)+(installed[id]?"  ✓":"");
            }
            else if(stage==Stage.Wires)
            {
                card.rectTransform.anchoredPosition=new Vector2((i%5-2)*112,70-i/5*100);card.rectTransform.sizeDelta=new Vector2(98,88);labels[i].text="";
                var center=UiKit.Panel(card.transform,Vector2.zero,new Vector2(18,18),UiKit.Now.hot,UiKit.Now.hot,0);center.sprite=null;wires.Add(center.gameObject);
                Vector2[] positions={new(0,23),new(25,0),new(0,-23),new(-25,0)};
                for(int d=0;d<4;d++)if((masks[i]&(1<<d))!=0)
                {
                    var segment=UiKit.Panel(card.transform,positions[d],d%2==0?new Vector2(12,46):new Vector2(50,12),UiKit.Now.hot,UiKit.Now.hot,0);segment.sprite=null;wires.Add(segment.gameObject);
                }
                // Labels distinguish the power input and core output from decorative tiles.
                if(i==10||i==14){labels[i].text=L10n.T(i==10?"golem.circuit.input":"golem.circuit.output");labels[i].rectTransform.anchoredPosition=new Vector2(i==10?-150:150,0);labels[i].rectTransform.sizeDelta=new Vector2(150,45);}
            }
            else
            {
                card.rectTransform.anchoredPosition=new Vector2(0,stage==Stage.Questions?-20-i*155:-100);card.rectTransform.sizeDelta=new Vector2(1450,stage==Stage.Questions?130:95);
                labels[i].rectTransform.anchoredPosition=Vector2.zero;labels[i].rectTransform.sizeDelta=new Vector2(1380,115);labels[i].fontSize=28;
                labels[i].text=L10n.T(stage==Stage.Intro?"golem.begin":stage==Stage.Armed?"golem.circuit.activate":stage==Stage.Questions?$"golem.moral.{question+1}.{(i==HighOption(question)?"high":"low")}":"quiz.ok");
            }
        }
    }
    void Update()
    {
        if(!Active)return;
        if(Story.TakeTestAdvance()){TestComplete();return;}
        var k=Keyboard.current;var p=Gamepad.current;
        Vector2 move=p!=null?p.leftStick.ReadValue()+p.dpad.ReadValue():Vector2.zero;
        if(k!=null)move+=new Vector2((k.rightArrowKey.isPressed||k.dKey.isPressed?1:0)-(k.leftArrowKey.isPressed||k.aKey.isPressed?1:0),(k.upArrowKey.isPressed||k.wKey.isPressed?1:0)-(k.downArrowKey.isPressed||k.sKey.isPressed?1:0));
        if(move.sqrMagnitude>.3f&&!held)
        {
            int delta=Mathf.Abs(move.x)>Mathf.Abs(move.y)?(move.x>0?1:-1):(move.y<0?1:-1);
            if(stage==Stage.Wires&&Mathf.Abs(move.y)>=Mathf.Abs(move.x))delta*=5;
            if(stage==Stage.Parts&&Mathf.Abs(move.x)>Mathf.Abs(move.y))delta*=3;
            focus=(focus+delta+Count)%Count;held=true;Sound.Play("click");Refresh();
        }
        else if(move.sqrMagnitude<.1f)held=false;
        if((k!=null&&(k.enterKey.wasPressedThisFrame||k.spaceKey.wasPressedThisFrame))||(p!=null&&p.buttonSouth.wasPressedThisFrame))Activate(focus);
        else if((k!=null&&k.escapeKey.wasPressedThisFrame)||(p!=null&&p.buttonEast.wasPressedThisFrame))Back();
    }
    public void TestComplete()
    {
        if(!Active)return;if(morality)SaveGame.GolemJudged(13);Active=false;if(canvas)Destroy(canvas.gameObject);canvas=null;finished?.Invoke();
    }
    void OnDestroy(){if(canvas)Destroy(canvas.gameObject);}
}
