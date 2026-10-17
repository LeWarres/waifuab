using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

public static class CaptureUltimateReferences
{
    public static string Run() => Capture(new[] { "cerebron", "faceless", "sentinel", "tzmil", "culu" });
    public static string Culu() => Capture(new[] { "culu" });

    static string Capture(string[] heroes)
    {
        var root = new GameObject("UltimateReferences");
        root.transform.position = Vector3.up * 10000f;
        var camera = new GameObject("ReferenceCamera").AddComponent<Camera>();
        camera.transform.SetParent(root.transform, false);
        camera.orthographic = true; camera.aspect = 1f; camera.cullingMask = 1 << 31;
        camera.nearClipPlane = 0.01f; camera.farClipPlane = 1000f;
        var hd = camera.gameObject.AddComponent<HDAdditionalCameraData>();
        hd.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;
        hd.backgroundColorHDR = new Color(0.55f, 0.65f, 0.75f);
        var light = new GameObject("ReferenceLight").AddComponent<Light>();
        light.transform.SetParent(root.transform, false);
        light.type = LightType.Directional; light.intensity = 3f;
        light.transform.rotation = Quaternion.Euler(40f, -30f, 0f);
        var volume = root.AddComponent<Volume>(); volume.isGlobal = true; volume.priority = 100f;
        volume.sharedProfile = UnityEditor.AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/Materials/SelectionLook.asset");
        var canvases = Object.FindObjectsByType<Canvas>().Where(c => c.enabled).ToArray();
        foreach (var canvas in canvases) canvas.enabled = false;
        var target = new RenderTexture(1024, 1024, 24, RenderTextureFormat.ARGB32);
        var texture = new Texture2D(1024, 1024, TextureFormat.RGB24, false);
        var previous = RenderTexture.active;
        try
        {
            target.Create(); camera.targetTexture = target;
            foreach (string id in heroes)
            {
                string who = "Characters/" + id + "/" + id;
                var body = Object.Instantiate(Resources.Load<GameObject>(who), root.transform);
                foreach (var part in body.GetComponentsInChildren<Transform>()) part.gameObject.layer = 31;
                var rig = body.GetComponent<Animator>(); if (rig) rig.enabled = false;
                var renderers = body.GetComponentsInChildren<Renderer>();
                var material = new Material(Shader.Find("HDRP/Lit"));
                material.SetTexture("_BaseColorMap", Resources.Load<Texture2D>(who));
                material.SetColor("_BaseColor", Color.white);
                material.SetFloat("_Smoothness", 0.2f);
                foreach (var renderer in renderers) renderer.sharedMaterials = System.Array.ConvertAll(renderer.sharedMaterials, _ => material);
                Bounds bounds = renderers[0].bounds;
                foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                Vector3 front = Vector3.back;
                if (rig && rig.isHuman)
                {
                    var left = rig.GetBoneTransform(HumanBodyBones.LeftUpperArm);
                    var right = rig.GetBoneTransform(HumanBodyBones.RightUpperArm);
                    front = Vector3.Cross((right.position - left.position).normalized, Vector3.up).normalized;
                }
                Vector3 aim = bounds.center + Vector3.up * bounds.size.y * 0.25f;
                camera.orthographicSize = bounds.size.y * 0.28f;
                camera.transform.position = aim + front * bounds.size.magnitude * 2f;
                camera.transform.LookAt(aim);
                RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                RenderTexture.active = target; texture.ReadPixels(new Rect(0, 0, 1024, 1024), 0, 0); texture.Apply();
                File.WriteAllBytes(Path.Combine(Path.GetDirectoryName(Application.dataPath), "Tools/" + id + "_ultimate_reference.png"), texture.EncodeToPNG());
                Object.DestroyImmediate(body);
                Object.DestroyImmediate(material);
            }
            return "Exported hero references to Tools.";
        }
        finally
        {
            RenderTexture.active = previous;
            foreach (var canvas in canvases) if (canvas) canvas.enabled = true;
            camera.targetTexture = null; target.Release();
            Object.DestroyImmediate(target); Object.DestroyImmediate(texture); Object.DestroyImmediate(root);
        }
    }
}
