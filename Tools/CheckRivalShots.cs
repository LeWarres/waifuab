using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
public static class CheckRivalShots
{
    public static string Run(){var p=new GameObject("RivalShotProbe").AddComponent<RivalShotProbe>();UnityEngine.Object.DontDestroyOnLoad(p);p.StartCoroutine(p.Check());return "Started rival shot test";}
}
public class RivalShotProbe:MonoBehaviour
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    public IEnumerator Check()
    {
        File.WriteAllText("Tools/rival_shots_check.txt","RUNNING\n");Application.runInBackground=true;TrainSim.NoEvents=true;
        SceneManager.LoadScene("sandScenario");yield return new WaitForSecondsRealtime(1.2f);
        var train=UnityEngine.Object.FindAnyObjectByType<TrainSim>();var rival=train.GetComponent<RivalEvent>();rival.Begin(2);
        typeof(RivalEvent).GetField("startsAt",F).SetValue(rival,Time.unscaledTime-1f);
        var shots=(IList)typeof(RivalEvent).GetField("shots",F).GetValue(rival);
        float initial=train.Health;float until=Time.realtimeSinceStartup+5f;bool seen=false;
        while(Time.realtimeSinceStartup<until&&!seen){seen=shots.Count>0;yield return null;}
        if(!seen){File.AppendAllText("Tools/rival_shots_check.txt","FAIL no projectile\n");Destroy(gameObject);yield break;}
        yield return new WaitForSecondsRealtime(.8f);
        File.AppendAllText("Tools/rival_shots_check.txt",train.Health<initial?"PASS visible projectile followed by damage\n":"FAIL no impact damage\n");
        TrainSim.NoEvents=false;Destroy(gameObject);
    }
}
