using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
public static class ReviewRicMar
{
    public static string Before()=>Open(false);
    public static string After()=>Open(true);
    static string Open(bool after){var go=new GameObject("RicMarReview");var p=go.AddComponent<RicMarReview>();p.after=after;UnityEngine.Object.DontDestroyOnLoad(go);p.StartCoroutine(p.Guard());return "Started RicMar review";}
}
public class RicMarReview:MonoBehaviour
{
    public bool after;const string Dir="Tools/ricmar_finish_review/";int errors;bool oldEvents;
    string Path=>Dir+(after?"after":"before")+".txt";
    void Error(string m,string s,LogType t){if(t==LogType.Error||t==LogType.Exception){errors++;File.AppendAllText(Path,"ERROR "+m+"\n");}}
    public IEnumerator Guard()
    {
        Directory.CreateDirectory(Dir);File.WriteAllText(Path,"RUNNING\n");oldEvents=TrainSim.NoEvents;TrainSim.NoEvents=true;Application.runInBackground=true;Application.logMessageReceived+=Error;
        var stack=new Stack<IEnumerator>();stack.Push(Run());
        while(stack.Count>0){object next=null;bool more=false;Exception fault=null;try{more=stack.Peek().MoveNext();if(more)next=stack.Peek().Current;}catch(Exception e){fault=e;}
            if(fault!=null){File.AppendAllText(Path,"FAIL "+fault);Destroy(gameObject);yield break;}if(!more){stack.Pop();continue;}if(next is IEnumerator nested){stack.Push(nested);continue;}yield return next;}
        File.AppendAllText(Path,"PASS consoleErrors="+errors);Destroy(gameObject);
    }
    IEnumerator Run()
    {
        Locomotives.Select(0);Locomotives.StoryMode=false;SceneManager.LoadScene("selectHero");yield return new WaitForSecondsRealtime(2f);
        var selector=UnityEngine.Object.FindAnyObjectByType<SelectLocomotive>();if(!selector)throw new Exception("Missing selector");
        if(after)Validate(selector.previews[0]);
        yield return Shot("selector");
        // A matching close view under the selector's actual lighting, with its UI temporarily hidden.
        var canvases=UnityEngine.Object.FindObjectsByType<Canvas>();foreach(var c in canvases)c.enabled=false;
        selector.enabled=false;var cam=Camera.main;var rs=selector.previews[0].GetComponentsInChildren<Renderer>();Bounds b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
        var hero=(Transform)typeof(SelectLocomotive).GetField("heroPreview",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(selector);if(hero)hero.gameObject.SetActive(false);
        cam.transform.position=b.center+new Vector3(-5f,3f,-6f);cam.transform.LookAt(b.center);cam.orthographic=true;cam.orthographicSize=2.65f;
        yield return Shot("detail");
        cam.transform.position=b.center+new Vector3(5f,3f,-6f);cam.transform.LookAt(b.center);yield return Shot("front");
        SaveGame.NewStory(0);Locomotives.StoryMode=true;Story.Play(0);SceneManager.LoadScene("sandScenario");yield return new WaitForSecondsRealtime(2f);
        var train=UnityEngine.Object.FindAnyObjectByType<TrainSim>();var loco=(Transform)typeof(TrainSim).GetField("loco",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(train);if(after)Validate(loco);
        if(after)
        {
            var mesh=Resources.Load<GameObject>("Models/Locomotive").GetComponentInChildren<MeshFilter>().sharedMesh;
            if(mesh.vertexCount!=4160||!mesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.TexCoord0))throw new Exception("Geometry/UV layout changed");
            var other=Locomotives.Create(null,1);foreach(var r in other.GetComponentsInChildren<Renderer>())foreach(var m in r.sharedMaterials)
                if(UnityEditor.AssetDatabase.GetAssetPath(m).Contains("RicMarFinish"))throw new Exception("RicMar materials leaked onto ReyMar");
            Destroy(other.gameObject);File.AppendAllText(Path,"PASS original 4160 vertices retained; ReyMar materials unchanged\n");
        }
        var remaining=typeof(TrainSim).GetField("remaining",BindingFlags.Instance|BindingFlags.NonPublic);float start=(float)remaining.GetValue(train);int frame=Time.frameCount;yield return new WaitForSecondsRealtime(2f);
        float moved=start-(float)remaining.GetValue(train);
        if(Time.frameCount<=frame+10||moved<=.01f)throw new Exception("Journey not advancing: "+moved);
        File.AppendAllText(Path,"PASS advancing journey frames="+(Time.frameCount-frame)+" distance="+moved+"\n");yield return Shot("gameplay");
        if(errors>0)throw new Exception("Console errors");
    }
    void Validate(Transform t)
    {
        int paint=0,glass=0;foreach(var r in t.GetComponentsInChildren<Renderer>())foreach(var m in r.sharedMaterials){if(m.name=="LocoBlue"&&m.mainTexture&&m.mainTexture.name=="enamel")paint++;if(m.name=="LocoGlass"&&m.mainTexture&&m.mainTexture.name=="glass")glass++;}
        if(paint==0||glass==0)throw new Exception("Finish missing on "+t.name);File.AppendAllText(Path,"PASS painted body and glass: "+t.name+"\n");
    }
    IEnumerator Shot(string suffix){for(int f=0;f<20;f++)yield return null;ScreenCapture.CaptureScreenshot(Dir+(after?"after-":"before-")+suffix+".png");yield return new WaitForEndOfFrame();yield return null;}
    void OnDestroy(){Application.logMessageReceived-=Error;TrainSim.NoEvents=oldEvents;}
}
