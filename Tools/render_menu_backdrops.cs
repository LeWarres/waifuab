// Editor script, run in Play mode in an endless run (sandScenario), once per biome with a frame or two between
// (unity command eval --code "$(cat Tools/render_menu_backdrops.cs)"): the first call ever stops the fight (no enemies,
// no shots, nothing drawn of it); each call renders the biome it is in, then moves the run on to the next. The train
// seen low from ahead, from the camera's side of the track (the hills are on the far one), under the biome's sky:
// Assets/Resources/Menu/backdrop_<biome>.png (1920 x 1080). The main menu shows one at random each time it opens.
var train = UnityEngine.Object.FindFirstObjectByType<TrainSim>();
var B = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var depart = typeof(TrainSim).GetMethod("Depart", B);
var enemies = (System.Collections.IList)typeof(Combat).GetField("enemies", B).GetValue(Combat.Instance);
var bullets = (System.Collections.IList)typeof(Combat).GetField("bullets", B).GetValue(Combat.Instance);
if (Combat.Instance.enabled) { enemies.Clear(); bullets.Clear(); Combat.Instance.enabled = false; UnityEngine.Time.timeScale = 0f; return "STOPPED"; }
var camGo = new UnityEngine.GameObject("BackdropCamera");
var cam = camGo.AddComponent<UnityEngine.Camera>();
camGo.AddComponent<UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData>();
cam.fieldOfView = 38f;
cam.nearClipPlane = 0.3f;
cam.farClipPlane = 4000f;
var target = new UnityEngine.RenderTexture(1920, 1080, 24, UnityEngine.RenderTextureFormat.ARGB32);
cam.targetTexture = target;
// the sky: the biome's painting on a big shell round the camera (as the race does)
var sky = UnityEngine.GameObject.CreatePrimitive(UnityEngine.PrimitiveType.Sphere);
UnityEngine.Object.DestroyImmediate(sky.GetComponent<UnityEngine.Collider>());
sky.transform.localScale = UnityEngine.Vector3.one * 3000f;
var round = UnityEngine.Object.Instantiate(sky.GetComponent<UnityEngine.MeshFilter>().sharedMesh);
round.normals = System.Array.ConvertAll(round.normals, _ => UnityEngine.Vector3.up);
sky.GetComponent<UnityEngine.MeshFilter>().sharedMesh = round;
var paint = TrainSim.Mat(new UnityEngine.Color(1.05f, 1.05f, 1.05f), false);
paint.SetInt("_Culling", 0);
paint.mainTextureScale = new UnityEngine.Vector2(2f, 2f);
paint.mainTextureOffset = new UnityEngine.Vector2(0f, -1f);
sky.GetComponent<UnityEngine.Renderer>().sharedMaterial = paint;
string log = "";
string dir = UnityEngine.Application.dataPath + "/Resources/Menu/";
{
    string id = train.BiomeId;
    paint.mainTexture = UnityEngine.Resources.Load<UnityEngine.Texture2D>("Sky/" + id);
    // ahead of the engine, off to one side, low; looking back along the train
    float nose = train.NoseS;
    var eye = train.TrackPoint(nose + 16f, -train.FarSide * 9f, out _) + UnityEngine.Vector3.up * 2.6f;
    var look = train.TrackPoint(nose - 14f, 1f, out _) + UnityEngine.Vector3.up * 1.6f;
    camGo.transform.position = eye;
    camGo.transform.LookAt(look);
    camGo.transform.Rotate(0f, -12f * train.FarSide, 0f, UnityEngine.Space.World); // the train left of centre: the menu's card covers the right
    sky.transform.position = eye;
    cam.Render();
    var was = UnityEngine.RenderTexture.active;
    UnityEngine.RenderTexture.active = target;
    var shot = new UnityEngine.Texture2D(1920, 1080, UnityEngine.TextureFormat.RGB24, false);
    shot.ReadPixels(new UnityEngine.Rect(0, 0, 1920, 1080), 0, 0);
    shot.Apply();
    UnityEngine.RenderTexture.active = was;
    System.IO.File.WriteAllBytes(dir + "backdrop_" + id + ".png", shot.EncodeToPNG());
    log += id + " ";
    if (id != "fortress") depart.Invoke(train, new object[] { true }); // on to the next biome, for the next call
}
UnityEngine.Object.DestroyImmediate(sky);
UnityEngine.Object.DestroyImmediate(camGo);
return "BACKDROPS " + log;
