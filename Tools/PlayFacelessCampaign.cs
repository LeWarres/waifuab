using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PlayFacelessCampaign
{
    public static string Run()
    {
        if (!Application.isPlaying) throw new Exception("Run in Play Mode");
        var p = new GameObject("FacelessCampaignProbe").AddComponent<FacelessCampaignProbe>();
        UnityEngine.Object.DontDestroyOnLoad(p.gameObject); p.StartCoroutine(p.Run());
        return "Campaign checks started; result in Temp/faceless-campaign-check.txt";
    }
}
public class FacelessCampaignProbe : MonoBehaviour
{
    const BindingFlags F = BindingFlags.Instance | BindingFlags.NonPublic;
    object Get(object o, string n) => o.GetType().GetField(n, F).GetValue(o);
    void Call(object o, string n, params object[] args) => o.GetType().GetMethod(n, F).Invoke(o, args);
    void Fail(string why) { System.IO.File.WriteAllText("Temp/faceless-campaign-check.txt", "FAIL: " + why); Destroy(gameObject); }
    public IEnumerator Run()
    {
        System.IO.File.WriteAllText("Temp/faceless-campaign-check.txt", "RUNNING");
        PlayerPrefs.SetInt("locomotive.selected", 10); Locomotives.StoryMode = true; Story.TestChapter = -1;
        Story.Play(1); Story.Pages(); SceneManager.LoadScene("cuestionario"); yield return new WaitForSecondsRealtime(1f);
        var quiz = UnityEngine.Object.FindAnyObjectByType<Cuestionario>();
        if (quiz.mode != QuestionnaireMode.Alien) { Fail("Human officer instead of alien"); yield break; }
        Call(quiz, "Activate", 0); yield return null;
        for (int i = 0; i < 3; i++)
        {
            int id = ((int[])Get(quiz, "deck"))[i]; Call(quiz, "Activate", (id % 4 + 1) % 4);
            yield return new WaitForSecondsRealtime(.85f);
        }
        if (Get(quiz, "stage").ToString() != "Arrested" || (int)Get(quiz, "errors") != 3) { Fail("Three mistakes did not arrest"); yield break; }
        Call(quiz, "Activate", 0); yield return new WaitForSecondsRealtime(1.2f);
        var intro = UnityEngine.Object.FindAnyObjectByType<StoryIntro>();
        if (!intro || intro.pages.Length != 5 || !Story.InspectionPending) { Fail("Arrest did not return to BBA victory narration"); yield break; }
        Call(intro, "Finish"); yield return new WaitForSecondsRealtime(1.2f);
        quiz = UnityEngine.Object.FindAnyObjectByType<Cuestionario>(); Call(quiz, "Activate", 0); yield return null;
        for (int i = 0; i < 5; i++)
        {
            int id = ((int[])Get(quiz, "deck"))[i]; Call(quiz, "Activate", id % 4);
            yield return new WaitForSecondsRealtime(.85f);
        }
        if (Get(quiz, "stage").ToString() != "Passed") { Fail("Five alien answers did not pass"); yield break; }
        Call(quiz, "Activate", 0); yield return new WaitForSecondsRealtime(1.2f);
        intro = UnityEngine.Object.FindAnyObjectByType<StoryIntro>();
        if (!intro || intro.pages.Length != 3 || !intro.pages[0].textKey.EndsWith(".intro.3")) { Fail("Missing boss refusal after questionnaire"); yield break; }
        Call(intro, "Finish"); yield return new WaitForSecondsRealtime(2f);
        var train = UnityEngine.Object.FindAnyObjectByType<TrainSim>();
        if (!train || train.BiomeId != "fortress") { Fail("No fortress gameplay"); yield break; }
        var rival = train.GetComponent<RivalEvent>(); var cave = train.GetComponent<CaveEvent>();
        rival.Begin(7); cave.Begin(7, won => { }); yield return new WaitForSecondsRealtime(1.4f);
        if (!rival.Active || !cave.Active || !((Transform)Get(cave, "faceless")).GetChild(0).name.ToLowerInvariant().Contains("cerebron")) { Fail("Cerebron cave battle model"); yield break; }
        cave.TestWin(); rival.TestWin();
        yield return null;
        Story.Play(0); SceneManager.LoadScene("sandScenario"); yield return new WaitForSecondsRealtime(2f);
        train = UnityEngine.Object.FindAnyObjectByType<TrainSim>(); rival = train.GetComponent<RivalEvent>(); cave = train.GetComponent<CaveEvent>();
        if (train.BiomeId != "alien") { Fail("Faceless starts outside alien biome"); yield break; }
        rival.Begin(0); cave.Begin(0, won => { }); yield return new WaitForSecondsRealtime(1.4f);
        if (!cave.Active || !((Transform)Get(cave, "faceless")).GetChild(0).name.ToLowerInvariant().Contains("bba")) { Fail("BBA enemy cave model"); yield break; }
        cave.TestWin(); rival.TestWin(); yield return null;
        Story.Play(3); Story.PassAscent(); SceneManager.LoadScene("sandScenario"); yield return new WaitForSecondsRealtime(2f);
        train = UnityEngine.Object.FindAnyObjectByType<TrainSim>(); rival = train.GetComponent<RivalEvent>(); rival.Begin(8);
        yield return new WaitForSecondsRealtime(1.4f);
        if (train.BiomeId != "jungle" || !rival.Active || (int)Get(rival, "rivalIndex") != 8) { Fail("R-20 duel in jungle"); yield break; }
        rival.TestWin(); yield return null;
        Story.Play(2); SceneManager.LoadScene("sandScenario"); yield return new WaitForSecondsRealtime(2f);
        train = UnityEngine.Object.FindAnyObjectByType<TrainSim>(); var eye = train.GetComponent<EyeBossEvent>();
        eye.Begin(won => { }); yield return new WaitForSecondsRealtime(1.4f);
        if (train.BiomeId != "space" || !eye.Active) { Fail("Eye boss in space"); yield break; }
        eye.TestWin(); eye.Close();
        Story.Play(0); SceneManager.LoadScene("historiaDialogos"); yield return new WaitForSecondsRealtime(1.2f);
        System.IO.File.WriteAllText("Temp/faceless-campaign-check.txt", "PASS: alien officer; three wrong answers/arrest/retry; five correct alien answers; rejected resignation then fortress; Cerebron cave model; BBA cave model on alien planet; R-20 duel in jungle; active eye boss in space. Battle victories accelerated with existing test hooks, not full combat balance runs.");
        Destroy(gameObject);
    }
}
