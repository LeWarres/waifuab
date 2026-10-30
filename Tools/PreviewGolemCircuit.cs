using System;
using UnityEngine;
public static class PreviewGolemCircuit
{
    public static string Run()
    {
        if(!Application.isPlaying)throw new Exception("Play Mode required");
        UiKit.SetBiome("thulu");var root=new GameObject("GolemCircuitVisualCheck");var game=root.AddComponent<GolemChallenge>();game.Begin(false,()=>UnityEngine.Object.Destroy(root));
        var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
        var stage=typeof(GolemChallenge).GetField("stage",flags);stage.SetValue(game,Enum.Parse(stage.FieldType,"Wires"));typeof(GolemChallenge).GetMethod("Refresh",flags).Invoke(game,null);
        Canvas.ForceUpdateCanvases();ScreenCapture.CaptureScreenshot("Tools/golem_test_session/circuits-final.png");return "Circuit visual check opened";
    }
}
