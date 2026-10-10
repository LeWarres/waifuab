// Editor script: bakes ONE biome made only of our own models (Assets/Scenery/Props) into Resources/Scenery,
// without needing the asset packs. Same recipe as bake_scenery.cs. Here: the ocean.
const string TS = "Assets/ToonScapes/Spring Isles/Prefabs/", MC = "Assets/JC_LP_MegaCity/Prefabs/", PR = "Assets/Scenery/Props/";
UnityEditor.AssetDatabase.DeleteAsset("Assets/Resources/Scenery/ocean");
var picks = new (string biome, string path, float height, string tint)[]
{
    ("ocean", PR + "ocean__island_palms", 0f, "FFFFFF"),
    ("ocean", PR + "ocean__islet", 0f, "FFFFFF"),
    ("ocean", PR + "ocean__rocks", 0f, "FFFFFF"),
    ("ocean", PR + "ocean__buoy", 0f, "FFFFFF"),
    ("ocean", PR + "ocean__boat", 0f, "FFFFFF"),
    ("ocean", PR + "ocean__lighthouse", 0f, "FFFFFF"),
    ("ocean", PR + "ocean__piles", 0f, "FFFFFF"),
    ("ocean", PR + "ocean__coral", 0f, "FFFFFF"),
};
string[] texNames = { "_MainTex", "_BaseMap", "_BaseColorMap", "_MainTexture", "_Albedo", "_SurfaceTexture", "_Texture0", "_TextureSample0", "_TextureSample" };
string[] colNames = { "_BaseColor", "_Color" };
var toon = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/Resources/Toon.mat");
System.IO.Directory.CreateDirectory("Assets/Scenery/Materials");
System.IO.Directory.CreateDirectory("Assets/Scenery/Source");
UnityEditor.AssetDatabase.Refresh();
var made = new System.Collections.Generic.Dictionary<string, UnityEngine.Material>();
var counts = new System.Collections.Generic.Dictionary<string, int>();
string log = "";

foreach (var pick in picks)
{
    var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(pick.path + ".prefab");
    if (!prefab) prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(pick.path + ".fbx");
    if (!prefab) { log += "MISSING " + pick.path + "; "; continue; }
    UnityEngine.ColorUtility.TryParseHtmlString("#" + pick.tint, out UnityEngine.Color tint);
    var body = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab);
    UnityEditor.PrefabUtility.UnpackPrefabInstance(body, UnityEditor.PrefabUnpackMode.Completely, UnityEditor.InteractionMode.AutomatedAction);

    // Top LOD only; nothing but meshes.
    foreach (var group in body.GetComponentsInChildren<UnityEngine.LODGroup>())
    {
        var lods = group.GetLODs();
        for (int l = 1; l < lods.Length; l++)
            foreach (var r in lods[l].renderers)
                if (r && System.Array.IndexOf(lods[0].renderers, r) < 0) UnityEngine.Object.DestroyImmediate(r.gameObject);
        UnityEngine.Object.DestroyImmediate(group);
    }
    foreach (var c in body.GetComponentsInChildren<UnityEngine.Component>(true))
        if (!c || c is UnityEngine.MonoBehaviour || c is UnityEngine.Collider || c is UnityEngine.ParticleSystemRenderer || c is UnityEngine.ParticleSystem)
            if (c) UnityEngine.Object.DestroyImmediate(c);
    UnityEditor.GameObjectUtility.RemoveMonoBehavioursWithMissingScript(body);
    foreach (var t in body.GetComponentsInChildren<UnityEngine.Transform>(true)) if (t) UnityEditor.GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);

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
            string key = src.name + "_" + pick.tint;
            if (!made.TryGetValue(key, out UnityEngine.Material twin))
            {
                // Saved values straight from the asset: they are still there when the material's shader is gone.
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
                bool got = src.name.StartsWith("TSI_"); // ToonScapes colours live in its textures
                foreach (var n in colNames)
                    for (int e = 0; e < cols.arraySize && !got; e++)
                        if (cols.GetArrayElementAtIndex(e).FindPropertyRelative("first").stringValue == n)
                        {
                            col = cols.GetArrayElementAtIndex(e).FindPropertyRelative("second").colorValue;
                            got = true;
                        }
                // Glow... blooms; the ToonScapes rock and stone textures are dark greys and need lifting to take a tint.
                float glow = src.name.StartsWith("Glow") ? 2.5f : src.name.StartsWith("TSI_Rocks") || src.name.StartsWith("TSI_Stone") ? 1.8f : 1f;
                col = new UnityEngine.Color(col.r * tint.r * glow, col.g * tint.g * glow, col.b * tint.b * glow, 1f);
                string lower = src.name.ToLower();
                bool leaf = lower.Contains("foliage") || lower.Contains("vegetation") || lower.Contains("tree") || lower.Contains("grass")
                    || lower.Contains("flower") || lower.Contains("plant") || lower.Contains("leaf");
                twin = new UnityEngine.Material(toon) { name = key, enableInstancing = true };
                twin.SetColor("_MainColor", col.linear);
                if (tex) twin.SetTexture("_MainTex", tex);
                if (leaf)
                {
                    // Leaves are cards with holes: cut out, both faces, and no outline around every card.
                    twin.EnableKeyword("N_F_CO_ON"); twin.SetFloat("_N_F_CO", 1f); twin.SetFloat("_Cutout", 0.4f);
                    twin.SetInt("_Culling", 0); twin.renderQueue = 2450; twin.SetOverrideTag("RenderType", "TransparentCutout");
                    twin.DisableKeyword("N_F_O_ON"); twin.SetFloat("_N_F_O", 0f); twin.SetShaderPassEnabled("SRPDefaultUnlit", false);
                }
                UnityEditor.AssetDatabase.CreateAsset(twin, "Assets/Scenery/Materials/" + key + ".mat");
                made[key] = twin;
            }
            mats[k] = twin;
        }
        r.sharedMaterials = mats;
    }
    if (!any) { log += "EMPTY " + pick.path + "; "; UnityEngine.Object.DestroyImmediate(body); continue; }

    counts.TryGetValue(pick.biome, out int index);
    counts[pick.biome] = index + 1;
    string name = System.IO.Path.GetFileName(pick.path).Replace("TSI_", "").Replace("SM_", "").Replace(pick.biome + "__", "");
    var root = new UnityEngine.GameObject(name);
    float scale = pick.height > 0f ? pick.height / UnityEngine.Mathf.Max(bounds.size.y, 0.01f) : 1f;
    float wide = UnityEngine.Mathf.Max(bounds.size.x, bounds.size.z) * scale;
    if (wide > 12f) scale *= 12f / wide; // nothing so wide that it reaches the track from its row
    body.transform.SetParent(root.transform, false);
    body.transform.localScale *= scale;
    body.transform.position += new UnityEngine.Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z) * scale;
    string folder = "Assets/Resources/Scenery/" + pick.biome;
    System.IO.Directory.CreateDirectory(folder);
    UnityEditor.PrefabUtility.SaveAsPrefabAsset(root, folder + "/" + index.ToString("00") + "_" + name + ".prefab");
    UnityEngine.Object.DestroyImmediate(root);
}
UnityEditor.AssetDatabase.SaveAssets();
UnityEditor.AssetDatabase.Refresh();

// Keep what the baked prefabs point at (meshes, then the textures of the new materials); the packs can go.
var keep = new System.Collections.Generic.HashSet<string>();
foreach (string guid in UnityEditor.AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Resources/Scenery" }))
    foreach (string dep in UnityEditor.AssetDatabase.GetDependencies(UnityEditor.AssetDatabase.GUIDToAssetPath(guid), false))
    {
        keep.Add(dep);
        if (dep.EndsWith(".mat")) foreach (string tex in UnityEditor.AssetDatabase.GetDependencies(dep, false)) keep.Add(tex);
    }
int moved = 0;
foreach (string path in keep)
{
    if (!path.StartsWith("Assets/ToonScapes") && !path.StartsWith("Assets/JC_LP_MegaCity")) continue;
    string to = UnityEditor.AssetDatabase.GenerateUniqueAssetPath("Assets/Scenery/Source/" + System.IO.Path.GetFileName(path));
    string error = UnityEditor.AssetDatabase.MoveAsset(path, to);
    if (error != "") log += "MOVE " + error + "; "; else moved++;
}
bool gone = false;
return "BAKED packsDeleted=" + gone + " " + string.Join(",", counts) + " mats=" + made.Count + " moved=" + moved + " " + log;
