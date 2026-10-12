using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using System.Reflection;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class CaptureLocomotiveMenu
{
    public static string Run()
    {
        Scene previousScene=SceneManager.GetActiveScene();
        Scene scene=SceneManager.GetSceneByPath("Assets/Scenes/selectHero.unity");
        bool opened=!scene.isLoaded;
        if(opened) scene=EditorSceneManager.OpenScene("Assets/Scenes/selectHero.unity",OpenSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        var otherCameras=Object.FindObjectsByType<Camera>().Where(c=>c.enabled && c.gameObject.scene!=scene).ToArray();
        foreach(var other in otherCameras) other.enabled=false;
        var camera=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Camera>()).First();
        var menu=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<SelectLocomotive>()).First();
        var selection=typeof(SelectLocomotive).GetField("selected",BindingFlags.Instance|BindingFlags.NonPublic);
        int previousSelection=(int)selection.GetValue(menu);
        selection.SetValue(menu,0);
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
            menu.SendMessage("Refresh",SendMessageOptions.RequireReceiver);
            var hero=menu.transform.Find("BbaHeroPreview");
            var animator=hero ? hero.GetComponent<Animator>() : null;
            if(animator && animator.runtimeAnimatorController) {
                animator.Rebind();animator.Update(0f);
                animator.Play("SocialGreeting",0,0f);animator.Update(1.2f);
            }
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
            foreach(var other in otherCameras) other.enabled=true;
            if(previousScene.IsValid() && previousScene.isLoaded) SceneManager.SetActiveScene(previousScene);
            if(opened) EditorSceneManager.CloseScene(scene,true);
        }
    }
}
