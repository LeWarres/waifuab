// One-off editor script: takes the cargo wagons and the station building out of the Low Poly Mega City pack.
// Each output is one or more pack prefabs joined, with RealToon materials, turned so its long side runs along x,
// scaled to a set length, standing on y = 0 and centred. Then the used meshes and textures move to
// Assets/Scenery/Source and the rest of the pack is deleted.
const string P = "Assets/JC_LP_MegaCity/Prefabs/";
var outputs = new (string name, float length, (string path, float lift)[] parts)[]
{
    ("Cargo_wood", 5f, new[] { (P + "Vehicles/SM_Vehicles_TrainPart_01", 0f) }),
    ("Cargo_rock", 5f, new[] { (P + "Vehicles/SM_Vehicles_TrainPart_03", 0f) }),
    ("Cargo_arms", 5f, new[] { (P + "Vehicles/SM_Vehicles_TrainPart_04", 0f) }),
    ("Cargo_container", 5f, new[] { (P + "Vehicles/SM_Vehicles_TrainPart_02", 0f) }),
    ("Station", 11f, new[] { (P + "Buildings/SM_Buildings_Railway_01", 0f) }),
};
string[] texNames = { "_BaseMap", "_MainTex", "_BaseColorMap" };
string[] colNames = { "_BaseColor", "_Color" };
var toon = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/Resources/Toon.mat");
var made = new System.Collections.Generic.Dictionary<string, UnityEngine.Material>();
string log = "";
foreach (var output in outputs)
{
    var root = new UnityEngine.GameObject(output.name);
    var body = new UnityEngine.GameObject("Body");
    body.transform.SetParent(root.transform, false);
    foreach (var part in output.parts)
    {
        var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(part.path + ".prefab");
        if (!prefab) { log += "MISSING " + part.path + "; "; continue; }
        var piece = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab);
        UnityEditor.PrefabUtility.UnpackPrefabInstance(piece, UnityEditor.PrefabUnpackMode.Completely, UnityEditor.InteractionMode.AutomatedAction);
        piece.transform.SetParent(body.transform, false);
        piece.transform.localPosition = UnityEngine.Vector3.up * part.lift;
    }
    foreach (var c in body.GetComponentsInChildren<UnityEngine.Component>(true))
        if (c is UnityEngine.MonoBehaviour || c is UnityEngine.Collider || c is UnityEngine.LODGroup || c is UnityEngine.Light) UnityEngine.Object.DestroyImmediate(c);
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
                var saved = new UnityEditor.SerializedObject(src).FindProperty("m_SavedProperties");
                var texs = saved.FindPropertyRelative("m_TexEnvs");
                var cols = saved.FindPropertyRelative("m_Colors");
                UnityEngine.Texture tex = null;
                foreach (var n in texNames)
                {
                    for (int e = 0; e < texs.arraySize && !tex; e++)
                        if (texs.GetArrayElementAtIndex(e).FindPropertyRelative("first").stringValue == n)
                            tex = texs.GetArrayElementAtIndex(e).FindPropertyRelative("second.m_Texture").objectReferenceValue as UnityEngine.Texture;
                    if (tex) break;
                }
                UnityEngine.Color col = UnityEngine.Color.white;
                bool got = false;
                foreach (var n in colNames)
                    for (int e = 0; e < cols.arraySize && !got; e++)
                        if (cols.GetArrayElementAtIndex(e).FindPropertyRelative("first").stringValue == n)
                        {
                            col = cols.GetArrayElementAtIndex(e).FindPropertyRelative("second").colorValue;
                            got = true;
                        }
                col.a = 1f;
                twin = new UnityEngine.Material(toon) { name = src.name, enableInstancing = true };
                twin.SetColor("_MainColor", col.linear);
                if (tex) twin.SetTexture("_MainTex", tex);
                UnityEditor.AssetDatabase.CreateAsset(twin, "Assets/Scenery/Materials/" + src.name + ".mat");
                made[src.name] = twin;
            }
            mats[k] = twin;
        }
        r.sharedMaterials = mats;
    }
    if (!any) { log += "EMPTY " + output.name + "; "; UnityEngine.Object.DestroyImmediate(root); continue; }
    bool turned = bounds.size.z > bounds.size.x; // long side along x, like the wagons
    float scale = output.length / UnityEngine.Mathf.Max(bounds.size.x, bounds.size.z);
    body.transform.position = new UnityEngine.Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z);
    var pivot = new UnityEngine.GameObject("Pivot");
    pivot.transform.SetParent(root.transform, false);
    body.transform.SetParent(pivot.transform, true);
    pivot.transform.localRotation = UnityEngine.Quaternion.Euler(0f, turned ? 90f : 0f, 0f);
    pivot.transform.localScale = UnityEngine.Vector3.one * scale;
    UnityEditor.PrefabUtility.SaveAsPrefabAsset(root, "Assets/Resources/Models/" + output.name + ".prefab");
    log += output.name + " " + bounds.size + (turned ? " turned" : "") + " x" + scale.ToString("0.00") + "; ";
    UnityEngine.Object.DestroyImmediate(root);
}
UnityEditor.AssetDatabase.SaveAssets();
UnityEditor.AssetDatabase.Refresh();
var keep = new System.Collections.Generic.HashSet<string>();
foreach (var output in outputs)
    foreach (string dep in UnityEditor.AssetDatabase.GetDependencies("Assets/Resources/Models/" + output.name + ".prefab", false))
    {
        keep.Add(dep);
        if (dep.EndsWith(".mat")) foreach (string tex in UnityEditor.AssetDatabase.GetDependencies(dep, false)) keep.Add(tex);
    }
int moved = 0;
foreach (string path in keep)
{
    if (!path.StartsWith("Assets/JC_LP_MegaCity")) continue;
    string error = UnityEditor.AssetDatabase.MoveAsset(path, UnityEditor.AssetDatabase.GenerateUniqueAssetPath("Assets/Scenery/Source/" + System.IO.Path.GetFileName(path)));
    if (error != "") log += "MOVE " + error + "; "; else moved++;
}
bool deleted = UnityEditor.AssetDatabase.DeleteAsset("Assets/JC_LP_MegaCity");
return "BAKED mats=" + made.Count + " moved=" + moved + " deleted=" + deleted + " " + log;
