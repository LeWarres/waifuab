using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

public static class CaptureBbaReference
{
    public static string Run()
    {
        var hidden=new GameObject("BbaReferenceCapture");
        var body=Object.Instantiate(Resources.Load<GameObject>("Characters/bba/bba"),hidden.transform);
        foreach(var transform in body.GetComponentsInChildren<Transform>()) transform.gameObject.layer=31;
        var rig=body.GetComponent<Animator>();if(rig) rig.enabled=false;
        var renderers=body.GetComponentsInChildren<Renderer>();
        foreach(var renderer in renderers) renderer.sharedMaterial=Resources.Load<Material>("Characters/bba/bba");
        Bounds bounds=renderers[0].bounds;foreach(var renderer in renderers) bounds.Encapsulate(renderer.bounds);
        Vector3 front=Vector3.back;
        if(rig && rig.isHuman) {
            var left=rig.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            var right=rig.GetBoneTransform(HumanBodyBones.RightUpperArm);
            front=Vector3.Cross((right.position-left.position).normalized,Vector3.up).normalized;
        }
        var camera=new GameObject("ReferenceCamera").AddComponent<Camera>();camera.transform.SetParent(hidden.transform);
        camera.orthographic=true;camera.orthographicSize=bounds.size.y*.62f;camera.aspect=1;
        camera.cullingMask=1<<31;camera.nearClipPlane=.01f;camera.farClipPlane=1000;
        camera.transform.position=bounds.center+front*bounds.size.magnitude*2+Vector3.up*bounds.size.y*.08f;
        camera.transform.LookAt(bounds.center);
        var hd=camera.gameObject.AddComponent<HDAdditionalCameraData>();hd.clearColorMode=HDAdditionalCameraData.ClearColorMode.Color;
        hd.backgroundColorHDR=new Color(.65f,.82f,.92f);hd.volumeLayerMask=~0;
        var light=new GameObject("ReferenceLight").AddComponent<Light>();light.transform.SetParent(hidden.transform);
        light.type=LightType.Directional;light.intensity=3;light.transform.rotation=Quaternion.Euler(40,-30,0);
        var volume=hidden.AddComponent<Volume>();volume.isGlobal=true;volume.priority=100;
        volume.sharedProfile=UnityEditor.AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/Materials/SelectionLook.asset");
        var canvases=Object.FindObjectsByType<Canvas>().Where(c=>c.enabled).ToArray();foreach(var canvas in canvases) canvas.enabled=false;
        var target=new RenderTexture(1200,1200,24,RenderTextureFormat.ARGB32);
        var texture=new Texture2D(1200,1200,TextureFormat.RGB24,false);var previous=RenderTexture.active;
        try {
            target.Create();camera.targetTexture=target;
            RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});
            RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,1200,1200),0,0);texture.Apply();
            string path=Path.Combine(Path.GetDirectoryName(Application.dataPath),"Tools/bba_story_reference.png");
            File.WriteAllBytes(path,texture.EncodeToPNG());return path;
        } finally {
            RenderTexture.active=previous;foreach(var canvas in canvases) canvas.enabled=true;
            target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(texture);Object.DestroyImmediate(hidden);
        }
    }
}
