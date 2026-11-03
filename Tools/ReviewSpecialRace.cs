using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
public static class ReviewSpecialRace
{
    public static string Before()=>Open(false);
    public static string After()=>Open(true);
    static string Open(bool after)
    {
        if(!Application.isPlaying)throw new Exception("Play Mode required");
        var p=new GameObject("SpecialSurfaceReview").AddComponent<SpecialSurfaceReview>();p.after=after;UnityEngine.Object.DontDestroyOnLoad(p);p.StartCoroutine(p.Guard());return "Started special race review";
    }
}
public class SpecialSurfaceReview:MonoBehaviour
{
    public bool after;
    const string Dir="Tools/special_finish_review/";
    const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    bool oldEvents;int errors;Gamepad pad;
    object Get(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
    void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
    void Check(bool yes,string message){if(!yes)throw new Exception(message);}
    string LogPath=>Dir+(after?"after":"before")+".txt";
    void Log(string m)=>File.AppendAllText(LogPath,m+"\n");
    void Error(string m,string s,LogType t){if(t==LogType.Error||t==LogType.Exception){errors++;Log("ERROR "+m);}}
    public IEnumerator Guard()
    {
        Directory.CreateDirectory(Dir);File.WriteAllText(LogPath,"RUNNING\n");oldEvents=TrainSim.NoEvents;TrainSim.NoEvents=true;Application.runInBackground=true;Application.logMessageReceived+=Error;
        var stack=new Stack<IEnumerator>();stack.Push(Run());
        while(stack.Count>0){object next=null;bool more=false;Exception fault=null;try{more=stack.Peek().MoveNext();if(more)next=stack.Peek().Current;}catch(Exception e){fault=e;}
            if(fault!=null){Log("FAIL "+fault);Destroy(gameObject);yield break;}if(!more){stack.Pop();continue;}if(next is IEnumerator nested){stack.Push(nested);continue;}yield return next;}
        Log("PASS errors="+errors);Destroy(gameObject);
    }
    IEnumerator Run()
    {
        pad=InputSystem.AddDevice<Gamepad>("SpecialSurfacePad");pad.MakeCurrent();
        foreach(bool hell in new[]{true,false})
        {
            Locomotives.StoryMode=false;Locomotives.Select(hell?9:0);SceneManager.LoadScene("sandScenario");yield return new WaitForSecondsRealtime(1.3f);
            var race=UnityEngine.Object.FindAnyObjectByType<TrainSim>().GetComponent<RaceEvent>();UnityEngine.Random.InitState(62148);
            if(hell)race.BeginCrates(_=>{});else race.BeginTrench(_=>{});yield return new WaitForSecondsRealtime(.6f);
            var course=(Transform)Get(race,"course");var board=(Canvas)Get(race,"board");board.enabled=false;
            string mode=hell?"hell":"trench";
            if(after)
            {
                var counts=new Dictionary<string,int>();foreach(var r in course.GetComponentsInChildren<Renderer>())foreach(var m in r.sharedMaterials)if(m&&m.mainTexture){string n=m.mainTexture.name;if(!counts.ContainsKey(n))counts[n]=0;counts[n]++;}
                foreach(string n in hell?new[]{"hellstone","hellwood"}:new[]{"trencharmor","trenchdeck"})Check(counts.ContainsKey(n),"Missing "+mode+" texture "+n);
                foreach(string n in hell?new[]{"trencharmor","trenchdeck","asphalt","facade"}:new[]{"hellstone","hellwood","asphalt","facade"})Check(!counts.ContainsKey(n),"Surface leaked: "+n);
                Log("PASS "+mode+" textured surfaces: "+string.Join(", ",counts));
                foreach(string name in hell?new[]{"Road","WallL","WallR"}:new[]{"Road","BarrierL","BarrierR"})
                {
                    var mesh=course.Find(name).GetComponent<MeshFilter>().sharedMesh;var uv=mesh.uv;Check(uv.Length==mesh.vertexCount,"Missing UV "+name);
                    Vector2 lo=uv[0],hi=uv[0];foreach(var v in uv){lo=Vector2.Min(lo,v);hi=Vector2.Max(hi,v);}Check((hi-lo).sqrMagnitude>.01f,"Collapsed UV "+name);
                }
            }
            yield return Shot(race,new Vector3(7984,7,-12),new Vector3(8000,4,110),mode+"-overview");
            if(hell)yield return Shot(race,new Vector3(7985,6,193),new Vector3(8000,2,226),mode+"-crate");
            else yield return Shot(race,new Vector3(7986,6,25),new Vector3(8020,9,75),mode+"-wall");
            board.enabled=true;((Slanted)Get(race,"briefing")).gameObject.SetActive(false);Set(race,"startsAt",Time.unscaledTime-.1f);
            object me=Get(race,"me");Vector3 start=(Vector3)Get(me,"pos");int frame=Time.frameCount;
            for(int f=0;f<150;f++){pad.MakeCurrent();InputSystem.QueueStateEvent(pad,new GamepadState{rightTrigger=1f});yield return null;}
            InputSystem.QueueStateEvent(pad,new GamepadState());yield return null;float moved=Vector3.Distance(start,(Vector3)Get(me,"pos"));Check(moved>2f&&Time.frameCount>frame+100,"Not advancing under R2");
            ScreenCapture.CaptureScreenshot(Dir+(after?"after-":"before-")+mode+"-driving.png");yield return new WaitForEndOfFrame();yield return null;Log("PASS "+mode+" R2 moved "+moved+"m / "+(Time.frameCount-frame)+" frames");
            if(hell)
            {
                var crates=(IList)Get(race,"crates");Check(crates.Count==40,"Changed crate count");object box=crates[0];Vector3 at=(Vector3)box.GetType().GetField("Item2").GetValue(box);
                Set(me,"pos",at);Set(me,"speed",0f);Set(race,"cratePrevious",at-Vector3.forward);yield return null;yield return null;
                Check(race.CratesBroken==1,"Crate no longer breaks");Log("PASS actual crate collision; 40 crates / goal 35 retained");
            }
            else
            {
                int life=(int)Get(race,"life");race.GetType().GetMethod("TrenchHurt",F).Invoke(race,new object[]{15});Check((int)Get(race,"life")==life-15,"Trench damage changed");
                var lasers=(IList)Get(race,"lasers");Check(lasers.Count>0,"Missing laser gates");Log("PASS damage and laser gates retained");
                if(after){Set(race,"aiming",true);board.enabled=false;Vector3 hatch=(Vector3)Get(race,"hatch");var dirs=(List<Vector3>)Get(race,"dirs");yield return Shot(race,hatch-dirs[dirs.Count-1]*26f+Vector3.up*2f,hatch,"trench-vent");}
            }
            Check(errors==0,"Console errors");
        }
    }
    IEnumerator Shot(RaceEvent race,Vector3 pos,Vector3 at,string suffix)
    {
        race.enabled=false;race.View.transform.position=pos;race.View.transform.LookAt(at);for(int f=0;f<20;f++)yield return null;
        ScreenCapture.CaptureScreenshot(Dir+(after?"after-":"before-")+suffix+".png");yield return new WaitForEndOfFrame();yield return null;race.enabled=true;
    }
    void OnDestroy(){TrainSim.NoEvents=oldEvents;Application.logMessageReceived-=Error;if(pad!=null&&pad.added)InputSystem.RemoveDevice(pad);}
}
