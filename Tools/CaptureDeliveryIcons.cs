using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

public static class CaptureDeliveryIcons
{
    public static string Run()
    {
        var root = new GameObject("DeliveryThumbnailCapture");
        root.transform.position = Vector3.up * 10000f;
        var camera = new GameObject("ThumbnailCamera").AddComponent<Camera>();
        camera.transform.SetParent(root.transform, false);
        camera.orthographic = true;
        camera.aspect = 2f;
        camera.cullingMask = 1 << 31;
        camera.nearClipPlane = 0.01f;
        camera.farClipPlane = 100f;
        var hd = camera.gameObject.AddComponent<HDAdditionalCameraData>();
        hd.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;
        hd.backgroundColorHDR = new Color(0.86f, 0.94f, 1f);
        var light = new GameObject("ThumbnailLight").AddComponent<Light>();
        light.transform.SetParent(root.transform, false);
        light.type = LightType.Directional;
        light.intensity = 3f;
        light.transform.rotation = Quaternion.Euler(40f, -30f, 0f);
        var volume = root.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 100f;
        volume.sharedProfile = UnityEditor.AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/Materials/SelectionLook.asset");
        var canvases = Object.FindObjectsByType<Canvas>().Where(c => c.enabled).ToArray();
        foreach (var canvas in canvases) canvas.enabled = false;
        var target = new RenderTexture(256, 128, 24, RenderTextureFormat.ARGB32);
        var texture = new Texture2D(256, 128, TextureFormat.RGB24, false);
        var previous = RenderTexture.active;
        try
        {
            target.Create();
            camera.targetTexture = target;
            foreach (string id in new[] { "wood", "rock", "arms", "fuel", "container", "wagon" })
            {
                var prefab = Resources.Load<GameObject>("Models/" + (id == "wagon" ? "Wagon" : "Cargo_" + id));
                if (!prefab) throw new System.Exception("Missing wagon model: " + id);
                var body = Object.Instantiate(prefab, root.transform);
                foreach (var part in body.GetComponentsInChildren<Transform>()) part.gameObject.layer = 31;
                var renderers = body.GetComponentsInChildren<Renderer>();
                Bounds bounds = renderers[0].bounds;
                foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                camera.transform.position = bounds.center + new Vector3(7f, 5f, -9f);
                camera.transform.LookAt(bounds.center);
                camera.orthographicSize = Mathf.Max(bounds.size.y * 0.75f, bounds.size.magnitude * 0.38f);
                RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, 256, 128), 0, 0);
                texture.Apply();
                string path = "Assets/Resources/Icons/delivery_" + id + ".png";
                File.WriteAllBytes(path, texture.EncodeToPNG());
                UnityEditor.AssetDatabase.ImportAsset(path);
                Object.DestroyImmediate(body);
            }
            return "Captured six wagon thumbnails.";
        }
        finally
        {
            RenderTexture.active = previous;
            foreach (var canvas in canvases) canvas.enabled = true;
            target.Release();
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(texture);
            Object.DestroyImmediate(root);
        }
    }
}
