using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class SetupHeroSocialAnimation
{
    public static string Run()
    {
        if (EditorApplication.isPlaying) throw new System.Exception("Stop Play mode before creating the preview controller.");
        var source = AssetDatabase.LoadAllAssetsAtPath("Assets/Social Animations/Animations/Waving/Waving3.fbx")
            .OfType<AnimationClip>().First(c => !c.name.StartsWith("__"));
        Directory.CreateDirectory("Assets/Resources/Preview");
        AssetDatabase.Refresh();
        const string clipPath = "Assets/Resources/Preview/BbaGreeting.anim";
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        if (!clip)
        {
            clip = Object.Instantiate(source);
            clip.name = "BbaGreeting";
            AssetDatabase.CreateAsset(clip, clipPath);
        }
        else EditorUtility.CopySerialized(source, clip);
        clip.name = "BbaGreeting";
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        settings.loopBlend = true;
        settings.loopBlendOrientation = true;
        settings.loopBlendPositionY = true;
        settings.loopBlendPositionXZ = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        clip.wrapMode = WrapMode.Loop;
        EditorUtility.SetDirty(clip);
        const string controllerPath = "Assets/Resources/Preview/BbaSocial.controller";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath)
            ?? AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        var machine = controller.layers[0].stateMachine;
        var layers = controller.layers;
        layers[0].iKPass = true;
        controller.layers = layers;
        var state = machine.states.Select(s => s.state).FirstOrDefault(s => s.name == "SocialGreeting")
            ?? machine.AddState("SocialGreeting");
        state.motion = clip;
        machine.defaultState = state;
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        return "Waving3 greeting prepared with face tracking for BBA's preview.";
    }
}
