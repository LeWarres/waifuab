using System.IO;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
public static class InspectCuluLocomotives
{
    public static string Run()
    {
        var cameraObject=new GameObject("StoryModelInspectionCamera");
        var camera=cameraObject.AddComponent<Camera>();
        var hd=cameraObject.AddComponent<HDAdditionalCameraData>();
        hd.clearColorMode=HDAdditionalCameraData.ClearColorMode.Color;
        hd.backgroundColorHDR=new Color(.28f,.3f,.34f);
        hd.customRenderingSettings=true;
        hd.renderingPathCustomFrameSettingsOverrideMask.mask[(uint)FrameSettingsField.Postprocess]=true;
        hd.renderingPathCustomFrameSettings.SetEnabled(FrameSettingsField.Postprocess,false);
        camera.orthographic=true; camera.nearClipPlane=.01f; camera.farClipPlane=100;
        var target=new RenderTexture(1024,640,24);camera.targetTexture=target;
        Directory.CreateDirectory("Temp/culu-models");
        try
        {
            foreach(int index in new[]{0,2,6,8,9,11})
            {
                var model=Object.Instantiate(Resources.Load<GameObject>(Locomotives.Models[index]));
                var temporaryMaterials=new System.Collections.Generic.List<Material>();
                try
                {
                    model.transform.position=new Vector3(6000,3000,0);
                    var bounds=new Bounds(model.transform.position,Vector3.zero);
                    foreach(var renderer in model.GetComponentsInChildren<Renderer>())
                    {
                        bounds.Encapsulate(renderer.bounds);
                        var materials=renderer.sharedMaterials;
                        for(int m=0;m<materials.Length;m++)
                        {
                            var source=materials[m];var color=source.color;
                            var flat=new Material(Shader.Find("HDRP/Unlit"));
                            flat.SetColor("_UnlitColor",color*.55f);
                            flat.SetFloat("_IncludeIndirectLighting",0);
                            materials[m]=flat;temporaryMaterials.Add(flat);
                        }
                        renderer.sharedMaterials=materials;
                    }
                    camera.orthographicSize=Mathf.Max(bounds.size.y,bounds.size.x*.65f)*.8f;
                    camera.transform.position=bounds.center+new Vector3(-8,5,-8).normalized*20;
                    camera.transform.LookAt(bounds.center);
                    camera.Render();
                    var previous=RenderTexture.active;RenderTexture.active=target;
                    var shot=new Texture2D(1024,640,TextureFormat.RGB24,false);
                    shot.ReadPixels(new Rect(0,0,1024,640),0,0);shot.Apply();RenderTexture.active=previous;
                    File.WriteAllBytes("Temp/culu-models/model-"+index+".png",shot.EncodeToPNG());
                    Object.DestroyImmediate(shot);
                }
                finally{Object.DestroyImmediate(model);foreach(var mat in temporaryMaterials)Object.DestroyImmediate(mat);}
            }
            return "Rendered six actual assigned locomotive meshes; temporary flat materials expose original colors without scene lighting.";
        }
        finally{Object.DestroyImmediate(cameraObject);target.Release();Object.DestroyImmediate(target);}
    }
}
