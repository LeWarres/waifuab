// Editor script, run in Play mode (in sandScenario) with `unity command eval --code "$(cat Tools/render_hero_poses.cs)"`:
// each hero's model in its idle pose, three quarters on, lit by the scene's sun, on plain white, rendered to
// Tools/hero_renders/<id>.png (832 x 1216). Tools/make_hero_art.py turns them into the menu's illustrations
// (ComfyUI: img2img, then the background taken off).
string dir = System.IO.Path.Combine(UnityEngine.Application.dataPath, "../Tools/hero_renders");
System.IO.Directory.CreateDirectory(dir);
var clips = UnityEngine.Resources.Load<BrawlClips>("Brawl/BrawlClips");
var stage = new UnityEngine.Vector3(6000f, 3000f, 0f);
var camGo = new UnityEngine.GameObject("PoseCamera");
var cam = camGo.AddComponent<UnityEngine.Camera>();
var hd = camGo.AddComponent<UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData>();
hd.clearColorMode = UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData.ClearColorMode.Color;
hd.backgroundColorHDR = UnityEngine.Color.white;
hd.customRenderingSettings = true; // no motion blur: the camera jumps from one hero to the next
hd.renderingPathCustomFrameSettingsOverrideMask.mask[(uint)UnityEngine.Rendering.HighDefinition.FrameSettingsField.MotionBlur] = true;
hd.renderingPathCustomFrameSettings.SetEnabled(UnityEngine.Rendering.HighDefinition.FrameSettingsField.MotionBlur, false);
cam.fieldOfView = 24f;
cam.nearClipPlane = 0.1f;
cam.farClipPlane = 50f;
var target = new UnityEngine.RenderTexture(832, 1216, 24, UnityEngine.RenderTextureFormat.ARGB32);
cam.targetTexture = target;
string log = "";
foreach (string id in Locomotives.Heroes)
{
    if (id == null) continue;
    var root = new UnityEngine.GameObject("Pose_" + id).transform;
    root.position = stage;
    root.rotation = UnityEngine.Quaternion.Euler(0f, 200f, 0f); // three quarters on to the camera
    string who = "Characters/" + id + "/" + id;
    var model = UnityEngine.Resources.Load<UnityEngine.GameObject>(who);
    if (!model) { log += id + ":nomodel "; UnityEngine.Object.DestroyImmediate(root.gameObject); continue; }
    var body = BrawlEvent.Body(model, root, 1.8f);
    Player.Skin(body.transform, who);
    var rig = new BrawlEvent.Rig(root, body);
    rig.Play(clips.idle, true, 1f, 0f);
    for (int k = 0; k < 8; k++) rig.Tick(0.05f);
    // framed on everything it draws (some are much bigger than the 1.8 m their bones say): its height fills 88%
    var box = new UnityEngine.Bounds(stage + UnityEngine.Vector3.up, UnityEngine.Vector3.zero);
    var baked = new UnityEngine.Mesh();
    foreach (var skin in root.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>())
    {
        skin.BakeMesh(baked, true); // the posed vertices themselves: a skinned renderer's own bounds are loose
        foreach (var v in baked.vertices) box.Encapsulate(skin.transform.TransformPoint(v));
    }
    foreach (var plain in root.GetComponentsInChildren<UnityEngine.MeshRenderer>()) box.Encapsulate(plain.bounds);
    float away = box.size.y / 0.88f * 0.5f / UnityEngine.Mathf.Tan(cam.fieldOfView * 0.5f * UnityEngine.Mathf.Deg2Rad);
    away = UnityEngine.Mathf.Max(away, box.size.x / 0.88f * 0.5f / UnityEngine.Mathf.Tan(cam.fieldOfView * 0.5f * UnityEngine.Mathf.Deg2Rad) * 1216f / 832f);
    camGo.transform.position = box.center + new UnityEngine.Vector3(0f, 0.1f, -away - box.extents.z);
    camGo.transform.LookAt(box.center);
    cam.Render();
    var was = UnityEngine.RenderTexture.active;
    UnityEngine.RenderTexture.active = target;
    var shot = new UnityEngine.Texture2D(832, 1216, UnityEngine.TextureFormat.RGB24, false);
    shot.ReadPixels(new UnityEngine.Rect(0, 0, 832, 1216), 0, 0);
    shot.Apply();
    UnityEngine.RenderTexture.active = was;
    System.IO.File.WriteAllBytes(System.IO.Path.Combine(dir, id + ".png"), shot.EncodeToPNG());
    rig.Free();
    UnityEngine.Object.DestroyImmediate(root.gameObject); // now: the next one is rendered in this same frame
    log += id + " ";
}
UnityEngine.Object.Destroy(camGo);
return "RENDERED " + log;
