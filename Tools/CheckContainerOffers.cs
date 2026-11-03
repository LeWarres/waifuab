using System;
using System.Reflection;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
public static class CheckContainerOffers
{
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    public static string Inspect()
    {
        var train=UnityEngine.Object.FindAnyObjectByType<TrainSim>();
        if(!train)return "Playing="+Application.isPlaying+"; no active TrainSim";
        object Read(string name)=>typeof(TrainSim).GetField(name,Flags)?.GetValue(train)??typeof(TrainSim).GetProperty(name,Flags)?.GetValue(train);
        return "Playing="+Application.isPlaying+"; Eternals="+Read("Eternals")+"; stage="+Read("stage")+"; offers="+string.Join(",",(int[])Read("offers"))+"; RideClock="+train.RideClock;
    }
    public static string Reproduce()=>Start(true);
    public static string Verify()=>Start(false);
    static string Start(bool reproduce)
    {
        if(!Application.isPlaying)throw new Exception("Play Mode required");
        var probe=new GameObject("ContainerOfferProbe").AddComponent<ContainerOfferProbe>();
        UnityEngine.Object.DontDestroyOnLoad(probe.gameObject);probe.StartCoroutine(probe.Run(reproduce));
        return "Started container offer "+(reproduce?"reproduction":"regression test");
    }
}
public class ContainerOfferProbe:MonoBehaviour
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    const string Report="Tools/container_offer_regression.txt";
    int errors;
    void OnLog(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception){errors++;Log("ERROR "+message);}}
    void OnDestroy(){Application.logMessageReceived-=OnLog;TrainSim.NoEvents=false;}
    object Get(object o,string n)=>o.GetType().GetField(n,F)?.GetValue(o)??o.GetType().GetProperty(n,F)?.GetValue(o);
    void Set(object o,string n,object v){var f=o.GetType().GetField(n,F);if(f!=null)f.SetValue(o,v);else o.GetType().GetProperty(n,F).SetValue(o,v);}
    object Call(object o,string n)=>o.GetType().GetMethod(n,F).Invoke(o,null);
    void Check(bool value,string reason){if(!value)throw new Exception(reason);}
    void Log(string value)=>File.AppendAllText(Report,value+"\n");
    public IEnumerator Run(bool reproduce)
    {
        File.WriteAllText(Report,"RUNNING "+(reproduce?"reproduction":"regression")+"\n");
        Application.logMessageReceived+=OnLog;
        Application.runInBackground=true;TrainSim.NoEvents=true;
        Locomotives.Select(0);Locomotives.StoryMode=true;SaveGame.NewStory(0);Story.Play(0);
        SceneManager.LoadScene("sandScenario");yield return new WaitForSecondsRealtime(1.2f);
        try{Test(reproduce);}catch(Exception e){Log("FAIL "+e);Destroy(gameObject);yield break;}
        if(!reproduce)
        {
            int before=Time.frameCount;
            for(int i=0;i<120;i++)yield return null;
            Log("PASS station Update advanced "+(Time.frameCount-before)+" frames with capped container offer");
        }
        Log((errors==0?"PASS":"FAIL")+" complete; console errors="+errors);Destroy(gameObject);
    }
    void Test(bool reproduce)
    {
        var train=UnityEngine.Object.FindAnyObjectByType<TrainSim>();Check(train,"Missing TrainSim");
        train.GetComponent<Combat>().enabled=false;Time.timeScale=0f;
        Set(train,"<AtStation>k__BackingField",true);Set(train,"deliveryPending",false);Set(train,"stage",0);
        var cars=(IList)Get(train,"cars");
        while(cars.Count<3)typeof(TrainSim).GetMethod("AddCar",F).Invoke(train,new object[]{cars.Count,-1,false});
        void Permanent(int count){for(int i=0;i<cars.Count;i++)Set(cars[i],"permanent",i<count);}
        void Offer(){var cards=(int[])Get(train,"offers");cards[0]=300;cards[1]=Weapon.Wagon.Length;cards[2]=Weapon.Wagon.Length+1;}
        Permanent(3);Offer();
        if(reproduce)
        {
            try{Call(train,"BuildMenu");throw new Exception("Expected original failure was absent");}
            catch(TargetInvocationException e) when(e.InnerException is IndexOutOfRangeException){Log("PASS reproduced original IndexOutOfRangeException: capped Eternal=300 fell through to Weapon.Wagon[100]");}
            train.enabled=false;return;
        }
        train.eternalOfferChance=1f;
        foreach(int count in new[]{0,2,3})
        {
            Permanent(count);train.maxCars=cars.Count+4;
            for(int seed=0;seed<100;seed++)
            {
                UnityEngine.Random.InitState(seed);Call(train,"RollOffers");var cards=(int[])Get(train,"offers");
                Check(Array.IndexOf(cards,300)>=0==count<TrainSim.MaxEternal,"Wrong rare offer eligibility at "+count);
                Call(train,"BuildMenu");Check(train.OptionCount==4,"Other station cards lost");
            }
            Offer();Call(train,"BuildMenu");Check(train.OptionIcon(0)=="container","Rare offer decoded as a different type");
            Check(train.OptionEnabled(0)==(count<TrainSim.MaxEternal),"Wrong rare card availability");
            if(count==3){train.Choose(0);Check((int)Get(train,"Eternals")==3,"Disabled card granted a fourth container");}
            Log("PASS 100 seeded rolls and rare card rendering at "+count+" permanent containers");
        }
        Permanent(2);train.maxCars=cars.Count;Call(train,"RollOffers");Check(Array.IndexOf((int[])Get(train,"offers"),300)<0,"Full train rolled a container");
        Offer();Call(train,"BuildMenu");Check(!train.OptionEnabled(0),"Full train rare card enabled");Log("PASS full train rejects new and stale container offers");
        Permanent(3);train.maxCars=cars.Count+4;Offer();Call(train,"BuildMenu");
    }
}
