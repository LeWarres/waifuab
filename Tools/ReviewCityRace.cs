using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
public static class ReviewCityRace
{
    public static string Before()=>Open("before");
    public static string After()=>Open("after");
    public static string Regression()=>Open("regression");
    static string Open(string id)
    {
        if(!Application.isPlaying)throw new Exception("Play Mode required");
        var p=new GameObject("CityFinishReview").AddComponent<CityFinishReview>();p.id=id;UnityEngine.Object.DontDestroyOnLoad(p.gameObject);p.StartCoroutine(p.Guarded());return "Started city finish review: "+id;
    }
}
public class CityFinishReview:MonoBehaviour
{
    public string id;
    const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    const string Dir="Tools/city_finish_review/";
    bool oldEvents;int errors;Gamepad pad;
    object Get(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
    void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
    void Error(string m,string s,LogType t){if(t==LogType.Error||t==LogType.Exception){errors++;File.AppendAllText(Dir+id+".txt","ERROR "+m+"\n");}}
    public IEnumerator Guarded()
    {
        Directory.CreateDirectory(Dir);File.WriteAllText(Dir+id+".txt","RUNNING\n");Application.logMessageReceived+=Error;
        var stack=new System.Collections.Generic.Stack<IEnumerator>();stack.Push(Run());
        while(stack.Count>0){object value=null;bool more=false;Exception fault=null;try{more=stack.Peek().MoveNext();if(more)value=stack.Peek().Current;}catch(Exception e){fault=e;}
            if(fault!=null){File.AppendAllText(Dir+id+".txt","FAIL "+fault);Destroy(gameObject);yield break;}if(!more){stack.Pop();continue;}if(value is IEnumerator nested){stack.Push(nested);continue;}yield return value;}
        File.AppendAllText(Dir+id+".txt","PASS errors="+errors+"\n");Destroy(gameObject);
    }
    IEnumerator Run()
    {
        oldEvents=TrainSim.NoEvents;TrainSim.NoEvents=true;Application.runInBackground=true;
        if(id=="regression"){yield return CheckModes();yield break;}
        Locomotives.Select(0);Locomotives.StoryMode=true;SaveGame.NewStory(0);Story.Play(1);SceneManager.LoadScene("sandScenario");yield return new WaitForSecondsRealtime(1.5f);
        var train=UnityEngine.Object.FindAnyObjectByType<TrainSim>();var race=train.GetComponent<RaceEvent>();Set(train,"junctionAsked",true);
        UnityEngine.Random.InitState(73146);race.Begin(_=>{});yield return new WaitForSecondsRealtime(1f);
        var view=race.View;var course=(Transform)Get(race,"course");
        File.AppendAllText(Dir+id+".txt","Renderers="+course.GetComponentsInChildren<Renderer>().Length+"\n");
        if(id=="after")ValidateFinish(course);
        // Match the before/after viewpoints on exactly the same seeded course.
        var board=(Canvas)Get(race,"board");board.enabled=false;
        yield return Shot(view,new Vector3(7984,7,-12),new Vector3(8000,3,80),"grid");
        yield return Shot(view,new Vector3(7984,2,25),new Vector3(8005,2,110),"barrier");
        board.enabled=true;
        Set(race,"startsAt",Time.unscaledTime-.1f);
        var briefing=(Slanted)Get(race,"briefing");briefing.gameObject.SetActive(false);
        pad=InputSystem.AddDevice<Gamepad>("CityFinishReviewPad");pad.MakeCurrent();object me=Get(race,"me");Vector3 start=(Vector3)Get(me,"pos");
        for(int f=0;f<120;f++){InputSystem.QueueStateEvent(pad,new GamepadState{rightTrigger=1f});yield return null;}
        InputSystem.QueueStateEvent(pad,new GamepadState());yield return null;
        float moved=Vector3.Distance(start,(Vector3)Get(me,"pos"));if(moved<1)throw new Exception("Race did not move");
        File.AppendAllText(Dir+id+".txt","R2 distance="+moved+" frames="+Time.frameCount+"\n");
        ScreenCapture.CaptureScreenshot(Dir+id+"-driving.png");yield return new WaitForEndOfFrame();yield return null;
        if(errors!=0)throw new Exception("Console errors");
    }
    void ValidateFinish(Transform course)
    {
        int concrete=0,metal=0,facade=0;var shared=new System.Collections.Generic.HashSet<Material>();
        foreach(var r in course.GetComponentsInChildren<Renderer>())foreach(var m in r.sharedMaterials)
        {
            if(!m||!m.mainTexture)continue;string n=m.mainTexture.name;
            if(n=="concrete")concrete++;if(n=="steel")metal++;if(n=="facade")facade++;
            if(n=="concrete"||n=="steel")shared.Add(m);
        }
        if(concrete<2||metal<20||facade<20)throw new Exception("Missing city material coverage");
        foreach(string name in new[]{"BarrierL","BarrierR","Road"})
        {
            var go=course.Find(name);if(!go)throw new Exception("Missing "+name);var mesh=go.GetComponent<MeshFilter>().sharedMesh;
            var uv=mesh.uv;if(uv.Length!=mesh.vertexCount)throw new Exception("UV count mismatch: "+name);
            Vector2 lo=uv[0],hi=uv[0];foreach(var v in uv){lo=Vector2.Min(lo,v);hi=Vector2.Max(hi,v);}
            if((hi-lo).sqrMagnitude<.01f)throw new Exception("Collapsed UVs: "+name);
        }
        if(course.Find("Road").GetComponent<Renderer>().sharedMaterial.mainTexture.name!="asphalt")throw new Exception("Road texture missing");
        File.AppendAllText(Dir+id+".txt",$"PASS textures: concrete={concrete}, steel={metal}, facade={facade}, shared surface materials={shared.Count}; road/barrier UVs valid\n");
    }
    IEnumerator CheckModes()
    {
        foreach(bool crates in new[]{true,false})
        {
            Locomotives.StoryMode=false;SceneManager.LoadScene("sandScenario");yield return new WaitForSecondsRealtime(1.2f);
            var race=UnityEngine.Object.FindAnyObjectByType<TrainSim>().GetComponent<RaceEvent>();
            if(crates)race.BeginCrates(_=>{});else race.BeginTrench(_=>{});yield return new WaitForSecondsRealtime(.5f);
            var course=(Transform)Get(race,"course");var road=course.Find("Road").GetComponent<Renderer>().sharedMaterial;
            if(road.mainTexture!=Resources.Load<Texture2D>(crates?"RaceSurfaces/hellstone":"RaceSurfaces/trenchdeck"))throw new Exception("Wrong mode's road");
            foreach(var renderer in course.GetComponentsInChildren<Renderer>())foreach(var m in renderer.sharedMaterials)
                if(m&&(m.mainTexture==Resources.Load<Texture2D>("RaceSurfaces/asphalt")||m.mainTexture==Resources.Load<Texture2D>("RaceSurfaces/facade")||m.name.Contains("city painted")))throw new Exception("City finish leaked into another mode");
            int frame=Time.frameCount;yield return null;yield return null;if(Time.frameCount<=frame)throw new Exception("Game frozen");
            File.AppendAllText(Dir+id+".txt","PASS "+(crates?"hell crates":"alien trench")+": separate painted road retained, real frames advancing\n");
        }
        if(errors!=0)throw new Exception("Other mode console errors");
    }
    IEnumerator Shot(Camera view,Vector3 pos,Vector3 at,string name)
    {
        // Pause race camera updates only while taking the comparison frame.
        var race=UnityEngine.Object.FindAnyObjectByType<RaceEvent>();race.enabled=false;view.transform.position=pos;view.transform.LookAt(at);
        for(int f=0;f<24;f++)yield return null;
        ScreenCapture.CaptureScreenshot(Dir+id+"-"+name+".png");yield return new WaitForEndOfFrame();yield return null;race.enabled=true;
    }
    void OnDestroy(){Application.logMessageReceived-=Error;TrainSim.NoEvents=oldEvents;if(pad!=null&&pad.added)InputSystem.RemoveDevice(pad);}
}
