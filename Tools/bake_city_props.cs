// Editor script (run with `unity command eval_file`): turns the models of Tools/make_city_props.py into city scenery,
// the way Tools/bake_scenery.cs did the packs' (which are gone: do not run that one again, it rebuilds from them).
// Each model gets RealToon materials (a material named Glow... comes out bright), its base on the ground, centred,
// and is saved as Resources/Scenery/city/<NN>_<name>.prefab after the ones already there. Also drops the litter.
const string PR = "Assets/Scenery/Props/city__";
string[] names = { "tower", "tower2", "flats", "shop_red", "shop_blue", "kiosk", "car_red", "car_blue", "car_mint", "hydrant", "barrier", "planter" };
const string folder = "Assets/Resources/Scenery/city";
UnityEditor.AssetDatabase.DeleteAsset(folder + "/16_Props_Garbage_03.prefab"); // bin bags scattered everywhere: looked like rubbish, not a city
var toon = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/Resources/Toon.mat");
var made = new System.Collections.Generic.Dictionary<string, UnityEngine.Material>();
int index = 19;
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
    float scale = UnityEngine.Mathf.Min(1f, 14f / bounds.size.y) * (name.StartsWith("car_") ? 0.62f : 1f); // no taller than this; the cars the size of the pack's
    body.transform.SetParent(root.transform, false);
    body.transform.localScale *= scale;
    body.transform.position += new UnityEngine.Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z) * scale;
    string to = folder + "/" + index.ToString("00") + "_city_" + name + ".prefab";
    UnityEditor.PrefabUtility.SaveAsPrefabAsset(root, to);
    UnityEngine.Object.DestroyImmediate(root);
    index++;
}
UnityEditor.AssetDatabase.SaveAssets();
UnityEditor.AssetDatabase.Refresh();
return "BAKED " + (index - 19) + " mats=" + made.Count + " " + log;
