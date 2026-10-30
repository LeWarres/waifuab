using System;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
public static class InspectR20Model
{
    public static string Run()
    {
        var root=new GameObject("R20Inspection").transform;
        var cameraObject=new GameObject("R20InspectionCamera");
        RenderTexture target=null;
        try
        {
            root.position=new Vector3(6000,3000,0);root.rotation=Quaternion.Euler(0,180,0);
            const string who="Characters/xiuhtecuhtli/xiuhtecuhtli";
            var body=BrawlEvent.Body(Resources.Load<GameObject>(who),root,1.8f);
            Player.Skin(body.transform,who);
            var head=body.GetBoneTransform(HumanBodyBones.Head);
            if(!head)throw new Exception("Missing head bone");
            var camera=cameraObject.AddComponent<Camera>();
            var hd=cameraObject.AddComponent<HDAdditionalCameraData>();
            hd.clearColorMode=HDAdditionalCameraData.ClearColorMode.Color;hd.backgroundColorHDR=new Color(.35f,.35f,.35f);
            hd.antialiasing=HDAdditionalCameraData.AntialiasingMode.None;
            hd.customRenderingSettings=true;
            hd.renderingPathCustomFrameSettingsOverrideMask.mask[(uint)FrameSettingsField.MotionBlur]=true;
            hd.renderingPathCustomFrameSettings.SetEnabled(FrameSettingsField.MotionBlur,false);
            camera.fieldOfView=32;camera.nearClipPlane=.01f;camera.farClipPlane=30;
            target=new RenderTexture(1024,1024,24,RenderTextureFormat.ARGB32);camera.targetTexture=target;
            Directory.CreateDirectory("Temp/r20-model");
            var aim=head.position+Vector3.up*.03f;
            foreach(float angle in new[]{0f,45f,90f})
            {
                camera.transform.position=aim+Quaternion.Euler(0,angle,0)*Vector3.back*.85f;
                camera.transform.LookAt(aim);
                camera.Render();
                var previous=RenderTexture.active;RenderTexture.active=target;
                var shot=new Texture2D(1024,1024,TextureFormat.RGB24,false);
                shot.ReadPixels(new Rect(0,0,1024,1024),0,0);shot.Apply();RenderTexture.active=previous;
                File.WriteAllBytes("Temp/r20-model/head-"+angle+".png",shot.EncodeToPNG());UnityEngine.Object.DestroyImmediate(shot);
            }
            return "Rendered actual R20 FBX head: front, 45 degrees and profile.";
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root.gameObject);UnityEngine.Object.DestroyImmediate(cameraObject);
            if(target){target.Release();UnityEngine.Object.DestroyImmediate(target);}
        }
    }
}
