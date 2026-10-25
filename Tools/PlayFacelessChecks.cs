using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

public static class PlayFacelessChecks
{
    public static string Flight()
    {
        var probe = new GameObject("FacelessFlightProbe").AddComponent<FacelessFlightProbe>();
        UnityEngine.Object.DontDestroyOnLoad(probe.gameObject); probe.StartCoroutine(probe.Run());
        return "Descent checks started; result in Temp/faceless-flight-check.txt";
    }
    public static string Forest()
    {
        if (!Application.isPlaying) throw new Exception("Enter Play Mode first");
        var probe = new GameObject("FacelessForestProbe").AddComponent<FacelessForestProbe>();
        UnityEngine.Object.DontDestroyOnLoad(probe.gameObject);
        probe.StartCoroutine(probe.Run());
        return "Forest controls/recovery/loss/ending checks started; result in Temp/faceless-forest-check.txt";
    }
}

public class FacelessFlightProbe : MonoBehaviour
{
    const BindingFlags F = BindingFlags.Instance | BindingFlags.NonPublic;
    Gamepad pad;
    readonly System.Collections.Generic.List<InputDevice> disabled = new();
    void KeepCurrent() { if (pad != null && pad.added) pad.MakeCurrent(); }
    void Input(GamepadState state) { pad.MakeCurrent(); InputSystem.QueueStateEvent(pad, state); }
    void Set(object obj, string field, object value) => obj.GetType().GetField(field, F).SetValue(obj, value);
    object Get(object obj, string field) => obj.GetType().GetField(field, F).GetValue(obj);
    public IEnumerator Run()
    {
        System.IO.File.WriteAllText("Temp/faceless-flight-check.txt", "RUNNING");
        foreach (var d in InputSystem.devices) if (d is Gamepad && d.enabled) { disabled.Add(d); InputSystem.DisableDevice(d); }
        pad = InputSystem.AddDevice<Gamepad>("FlightProbePad"); InputSystem.onAfterUpdate += KeepCurrent;
        PlayerPrefs.SetInt("locomotive.selected", 10); Locomotives.StoryMode = true; Story.TestChapter = -1;
        Story.Play(3); Story.Pages(); SceneManager.LoadScene("ascenso");
        yield return new WaitForSecondsRealtime(1.2f);
        var flight = UnityEngine.Object.FindAnyObjectByType<Ascenso>();
        var sky = UnityEngine.Object.FindAnyObjectByType<AscentBackdrop>();
        if (!sky.descending || sky.flight < .99f) { Fail("Descent did not start in space"); yield break; }
        Set(flight, "spawnIn", 100f);
        Input(new GamepadState().WithButton(GamepadButton.South)); yield return null; yield return null;
        Input(new GamepadState()); yield return null;
        if (Get(flight, "phase").ToString() != "Flying") { Fail("X did not start descent"); yield break; }
        var engine = (Transform)Get(flight, "train"); var from = engine.position;
        Input(new GamepadState { leftStick = Vector2.right }); yield return new WaitForSecondsRealtime(.25f);
        Input(new GamepadState()); yield return null;
        if (engine.position.x <= from.x + 1f) { Fail("Flight stick did not move"); yield break; }
        for (int i = 0; i < 3; i++)
        {
            Set(flight, "immunity", 0f);
            typeof(Ascenso).GetMethod("Spawn", F).Invoke(flight, new object[] { engine.position.x, 20f, 0f, 0f, false });
            var debris = (IList)Get(flight, "debris"); var piece = debris[debris.Count - 1];
            var body = (Transform)piece.GetType().GetField("body").GetValue(piece);
            body.position = engine.position - Vector3.up * .5f;
            yield return null; yield return null;
            if ((int)Get(flight, "hits") != i + 1) { Fail("Descending debris collision " + i); yield break; }
        }
        if (Get(flight, "phase").ToString() != "Lost") { Fail("Three hits did not lose"); yield break; }
        yield return new WaitForSecondsRealtime(.4f);
        Input(new GamepadState().WithButton(GamepadButton.South)); yield return new WaitForSecondsRealtime(1.2f);
        Input(new GamepadState());
        var story = UnityEngine.Object.FindAnyObjectByType<StoryIntro>();
        if (!story || !Story.AscentPending || story.pages.Length != 2 || !story.pages[0].textKey.Contains(".2.outro.")) { Fail("Loss did not return to space ending"); yield break; }
        typeof(StoryIntro).GetMethod("Finish", F).Invoke(story, null); yield return new WaitForSecondsRealtime(1.2f);
        flight = UnityEngine.Object.FindAnyObjectByType<Ascenso>();
        if (!flight) { Fail("Retry did not reopen descent"); yield break; }
        flight.duration = 1f; Set(flight, "spawnIn", 100f);
        Input(new GamepadState().WithButton(GamepadButton.South)); yield return null; yield return null;
        Input(new GamepadState()); yield return new WaitForSecondsRealtime(1.3f);
        sky = UnityEngine.Object.FindAnyObjectByType<AscentBackdrop>();
        if (Get(flight, "phase").ToString() != "Arrived" || sky.flight > .01f) { Fail("Descent did not finish in atmosphere"); yield break; }
        yield return new WaitForSecondsRealtime(.4f);
        Input(new GamepadState().WithButton(GamepadButton.South)); yield return new WaitForSecondsRealtime(1.2f);
        Input(new GamepadState()); story = UnityEngine.Object.FindAnyObjectByType<StoryIntro>();
        if (!story || Story.AscentPending || story.pages.Length != 3 || !story.pages[0].textKey.Contains(".3.intro.")) { Fail("Missing Earth arrival after landing"); yield break; }
        typeof(StoryIntro).GetMethod("Finish", F).Invoke(story, null); yield return new WaitForSecondsRealtime(2f);
        var train = UnityEngine.Object.FindAnyObjectByType<TrainSim>();
        if (!train || train.BiomeId != "jungle" || Story.Current.rival != 8) { Fail("Did not enter jungle with R-20 rival"); yield break; }
        System.IO.File.WriteAllText("Temp/faceless-flight-check.txt", "PASS: starts in space; X starts flight; stick moves; three actual debris collisions lose; loss returns to space narration; retry reopens descent; successful flight ends in atmosphere; Earth dialogue then jungle/R-20.");
        Cleanup(); Destroy(gameObject);
    }
    void Fail(string why) { System.IO.File.WriteAllText("Temp/faceless-flight-check.txt", "FAIL: " + why); Cleanup(); Destroy(gameObject); }
    void Cleanup() { InputSystem.onAfterUpdate -= KeepCurrent; if (pad != null && pad.added) InputSystem.RemoveDevice(pad); foreach (var d in disabled) if (d.added) InputSystem.EnableDevice(d); }
    void OnDestroy() { Cleanup(); }
}

public class FacelessForestProbe : MonoBehaviour
{
    const BindingFlags F = BindingFlags.Instance | BindingFlags.NonPublic;
    void Set(object obj, string field, object value) => obj.GetType().GetField(field, F).SetValue(obj, value);
    object Get(object obj, string field) => obj.GetType().GetField(field, F).GetValue(obj);
    Gamepad pad;
    void KeepCurrent() { if (pad != null && pad.added) pad.MakeCurrent(); }
    readonly System.Collections.Generic.List<InputDevice> disabled = new();
    void Input(GamepadState state) { pad.MakeCurrent(); InputSystem.QueueStateEvent(pad, state); }
    public IEnumerator Run()
    {
        System.IO.File.WriteAllText("Temp/faceless-forest-check.txt", "RUNNING");
        foreach (var d in InputSystem.devices) if (d is Gamepad && d.enabled) { disabled.Add(d); InputSystem.DisableDevice(d); }
        pad = InputSystem.AddDevice<Gamepad>("ForestProbePad");
        InputSystem.onAfterUpdate += KeepCurrent;
        PlayerPrefs.SetInt("locomotive.selected", 10); Locomotives.StoryMode = true; Story.TestChapter = -1; Story.Play(4);
        SceneManager.LoadScene("bosque");
        yield return new WaitForSecondsRealtime(1.2f);
        var forest = UnityEngine.Object.FindAnyObjectByType<ForestChase>();
        Input(new GamepadState().WithButton(GamepadButton.South)); yield return null; yield return null;
        Input(new GamepadState()); yield return new WaitForSecondsRealtime(1.2f);
        if (!forest.Playing) { Fail("X did not start forest"); yield break; }
        var before = forest.PlayerPosition;
        Input(new GamepadState { leftStick = Vector2.right }); yield return new WaitForSecondsRealtime(.3f);
        Input(new GamepadState()); yield return null;
        if (forest.PlayerPosition.x <= before.x + .5f) { Fail("Stick did not move hero"); yield break; }
        Set(forest, "remaining", .001f); yield return null; yield return null;
        if (Get(forest, "phase").ToString() != "Lost") { Fail("Timer did not lose"); yield break; }
        yield return new WaitForSecondsRealtime(.4f);
        Input(new GamepadState().WithButton(GamepadButton.South)); yield return new WaitForSecondsRealtime(1.2f);
        Input(new GamepadState());
        if (SceneManager.GetActiveScene().name != "historiaDialogos" || Story.Chapter != 4) { Fail("Loss did not return to forest narration"); yield break; }
        var intro = UnityEngine.Object.FindAnyObjectByType<StoryIntro>();
        typeof(StoryIntro).GetMethod("Finish", F).Invoke(intro, null);
        yield return new WaitForSecondsRealtime(1.2f);
        forest = UnityEngine.Object.FindAnyObjectByType<ForestChase>();
        if (!forest) { Fail("Forest narration did not open bosque"); yield break; }
        Input(new GamepadState().WithButton(GamepadButton.South)); yield return null; yield return null;
        Input(new GamepadState()); yield return new WaitForSecondsRealtime(1.2f);
        var thieves = (IList)Get(forest, "thieves"); var hero = (Transform)Get(forest, "player");
        for (int i = 0; i < thieves.Count; i++)
        {
            var t = thieves[i]; var root = (Transform)t.GetType().GetField("root").GetValue(t);
            root.position = new Vector3(5f, 0f, -5f);
            t.GetType().GetField("heading").SetValue(t, Vector3.zero);
            t.GetType().GetField("nextTurn").SetValue(t, Time.unscaledTime + 30f);
            hero.position = root.position + Vector3.right;
            Input(new GamepadState()); yield return null; yield return null;
            Input(new GamepadState().WithButton(GamepadButton.South)); yield return null; yield return null;
            Input(new GamepadState()); yield return null;
            if (forest.Recovered != i + 1) { Fail("X did not recover page " + i); yield break; }
        }
        if (Get(forest, "phase").ToString() != "Won") { Fail("Five drawings did not win"); yield break; }
        yield return new WaitForSecondsRealtime(.4f);
        Input(new GamepadState().WithButton(GamepadButton.South)); yield return new WaitForSecondsRealtime(1.2f);
        Input(new GamepadState());
        intro = UnityEngine.Object.FindAnyObjectByType<StoryIntro>();
        if (!intro || intro.pages.Length != 6 || !Story.Over) { Fail("Missing six-page epilogue"); yield break; }
        intro.ShowPage(5, true);
        System.IO.File.WriteAllText("Temp/faceless-forest-check.txt", "PASS: X starts; stick moves; timeout loses; loss returns to forest narration; narration returns to bosque; five real proximity/X interactions recover five drawings; win opens six-page epilogue.");
        Cleanup(); Destroy(gameObject);
    }
    void Fail(string why) { System.IO.File.WriteAllText("Temp/faceless-forest-check.txt", "FAIL: " + why); Cleanup(); Destroy(gameObject); }
    void Cleanup() { InputSystem.onAfterUpdate -= KeepCurrent; if (pad != null && pad.added) InputSystem.RemoveDevice(pad); foreach (var d in disabled) if (d.added) InputSystem.EnableDevice(d); }
    void OnDestroy() { Cleanup(); }
}
