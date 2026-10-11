using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

public static class CaptureMainMenu
{
    public static string Run()
    {
        var camera=Camera.main;
        var canvas=GameObject.Find("MainMenuCanvas").GetComponent<Canvas>();
        Object.FindAnyObjectByType<MainMenu>().RefreshUI();
        var mode=canvas.renderMode;var worldCamera=canvas.worldCamera;float distance=canvas.planeDistance;
        float aspect=camera.aspect;var previousTarget=camera.targetTexture;var active=RenderTexture.active;
        var target=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32);
        var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);
        var look=new GameObject("CaptureLook").AddComponent<Volume>();
        look.isGlobal=true;look.priority=100;
        look.sharedProfile=UnityEditor.AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/Materials/SelectionLook.asset");
        try {
            target.Create();camera.aspect=16f/9;camera.targetTexture=target;
            canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
            foreach(var graphic in canvas.GetComponentsInChildren<UnityEngine.UI.Graphic>()) graphic.SetAllDirty();
            Canvas.ForceUpdateCanvases();
            RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});
            RenderTexture.active=target;image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();
            string path=Path.Combine(Path.GetDirectoryName(Application.dataPath),"Tools/main_menu_new.png");
            File.WriteAllBytes(path,image.EncodeToPNG());return path;
        } finally {
            canvas.renderMode=mode;canvas.worldCamera=worldCamera;canvas.planeDistance=distance;
            camera.aspect=aspect;camera.targetTexture=previousTarget;RenderTexture.active=active;
            target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(image);Object.DestroyImmediate(look.gameObject);Canvas.ForceUpdateCanvases();
        }
    }
}
