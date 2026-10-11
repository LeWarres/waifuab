using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using System.Reflection;

public static class CaptureLocomotiveMenu
{
    public static string Run()
    {
        var camera=Camera.main;
        var menu=Object.FindAnyObjectByType<SelectLocomotive>();
        var selection=typeof(SelectLocomotive).GetField("selected",BindingFlags.Instance|BindingFlags.NonPublic);
        int previousSelection=(int)selection.GetValue(menu);
        selection.SetValue(menu,1);
        menu.SendMessage("Refresh",SendMessageOptions.RequireReceiver);
        var canvas=GameObject.Find("LocomotiveMenu").GetComponent<Canvas>();
        var previousMode=canvas.renderMode;
        var previousCamera=canvas.worldCamera;
        float previousDistance=canvas.planeDistance;
        float previousAspect=camera.aspect;
        var previousTarget=camera.targetTexture;
        var previousActive=RenderTexture.active;
        var target=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32);
        var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);
        try {
            target.Create();
            camera.aspect=1920f/1080f;
            camera.targetTexture=target;
            canvas.renderMode=RenderMode.ScreenSpaceCamera;
            canvas.worldCamera=camera;canvas.planeDistance=1;
            Canvas.ForceUpdateCanvases();
            RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest {destination=target});
            RenderTexture.active=target;
            image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();
            string path=Path.Combine(Path.GetDirectoryName(Application.dataPath),"Tools/select_locomotive.png");
            File.WriteAllBytes(path,image.EncodeToPNG());
            return path;
        } finally {
            canvas.renderMode=previousMode;canvas.worldCamera=previousCamera;canvas.planeDistance=previousDistance;
            camera.aspect=previousAspect;camera.targetTexture=previousTarget;
            RenderTexture.active=previousActive;target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(image);
            Canvas.ForceUpdateCanvases();
            selection.SetValue(menu,previousSelection);
            menu.SendMessage("Refresh",SendMessageOptions.RequireReceiver);
        }
    }
}
