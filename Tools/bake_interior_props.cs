// Editor script (run with `unity command eval_file`): turns the models of Tools/make_interior_props.py into the props of the
// wagon a fight happens in and of Faceless's cave. Same treatment as
// Tools/bake_city_props.cs (RealToon materials, base on the ground, centred).
const string PR = "Assets/Scenery/Props/kit__";
string[] names = { "crate", "barrel", "sacks", "lamp", "stack", "boulder_a", "boulder_b", "boulder_c", "stalactite", "stalagmite", "crystals", "wall" };
const string folder = "Assets/Resources/Interior";
if (!UnityEditor.AssetDatabase.IsValidFolder(folder)) UnityEditor.AssetDatabase.CreateFolder("Assets/Resources", "Interior");
foreach (string old in UnityEditor.AssetDatabase.FindAssets("t:Prefab", new[] { folder })) UnityEditor.AssetDatabase.DeleteAsset(UnityEditor.AssetDatabase.GUIDToAssetPath(old)); // the last bake goes
var toon = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/Resources/Toon.mat");
var made = new System.Collections.Generic.Dictionary<string, UnityEngine.Material>();
int index = 0;
string log = "";
foreach (string name in names)
{
    var model = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(PR + name + ".fbx");
    if (!model) { log += "MISSING " + name + "; "; continue; }
    var body = (UnityEngine.GameObject)UnityEngine.Object.Instantiate(model);
    var bounds = new UnityEngine.Bounds();
    bool any = false;
    foreach (var r in body.GetComponentsInChildren<UnityEngine.MeshRenderer>())
    {
        if (!any) bounds = r.bounds; else bounds.Encapsulate(r.bounds);
        any = true;
        var mats = r.sharedMaterials;
        for (int k = 0; k < mats.Length; k++)
        {
            var src = mats[k];
            if (!src) continue;
            if (!made.TryGetValue(src.name, out UnityEngine.Material twin))
            {
                UnityEngine.Color col = src.HasProperty("_BaseColor") ? src.GetColor("_BaseColor") : src.HasProperty("_Color") ? src.color : UnityEngine.Color.white;
                float glow = src.name.StartsWith("Glow") ? 2.5f : 1f;
                twin = new UnityEngine.Material(toon) { name = src.name, enableInstancing = true };
                twin.SetColor("_MainColor", new UnityEngine.Color(col.r * glow, col.g * glow, col.b * glow, 1f).linear);
                string path = "Assets/Scenery/Materials/" + src.name + ".mat";
                UnityEditor.AssetDatabase.DeleteAsset(path);
                UnityEditor.AssetDatabase.CreateAsset(twin, path);
                made[src.name] = twin;
            }
            mats[k] = twin;
        }
        r.sharedMaterials = mats;
    }
    var root = new UnityEngine.GameObject(name);
    float scale = 1f;
    body.transform.SetParent(root.transform, false);
    body.transform.localScale *= scale;
    body.transform.position += new UnityEngine.Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z) * scale;
    string to = folder + "/" + name + ".prefab";
    UnityEditor.PrefabUtility.SaveAsPrefabAsset(root, to);
    UnityEngine.Object.DestroyImmediate(root);
    index++;
}
UnityEditor.AssetDatabase.SaveAssets();
UnityEditor.AssetDatabase.Refresh();
return "BAKED " + index + " mats=" + made.Count + " " + log;
