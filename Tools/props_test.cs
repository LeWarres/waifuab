string[] paths = {
 "Assets/AnimeNaturalEnvironment/Prefabs/Trees/Tree_01.prefab",
 "Assets/AnimeNaturalEnvironment/Prefabs/Trees/Tree_05.prefab",
 "Assets/AnimeNaturalEnvironment/Prefabs/RockStyle1/Rock_01.prefab",
 "Assets/AnimeNaturalEnvironment/Prefabs/RockStyle2/Boulder_01_Mesh.prefab",
 "Assets/AnimeNaturalEnvironment/Prefabs/Plants/Bush.prefab",
 "Assets/ToonScapes/Spring Isles/Prefabs/Vegetation/Trees/TSI_Blossom_Tree_01A.prefab",
 "Assets/ToonScapes/Spring Isles/Prefabs/Vegetation/Trees/TSI_Broadleaf_Tree_01A.prefab",
 "Assets/ToonScapes/Spring Isles/Prefabs/Vegetation/Bamboo/TSI_Bamboo_01A.prefab",
 "Assets/ToonScapes/Spring Isles/Prefabs/Rocks/TSI_Rock_Large_01A.prefab",
 "Assets/ToonScapes/Spring Isles/Prefabs/Rocks/TSI_Rock_Medium_01A.prefab",
 "Assets/ToonScapes/Spring Isles/Prefabs/Building Props/Torii Gate/TSI_Torii_Gate_01A.prefab",
 "Assets/ToonScapes/Spring Isles/Prefabs/Building Props/Stone Kit/TSI_Stone_Lantern_01A.prefab",
};
string[] texNames = { "_MainTex", "_BaseMap", "_MainTexture", "_Albedo", "_SurfaceTexture", "_Texture0", "_TextureSample0", "_TextureSample" };
string[] colNames = { "_BaseColor", "_Color" };
var old = UnityEngine.GameObject.Find("PropTest"); if (old) UnityEngine.Object.DestroyImmediate(old);
var root = new UnityEngine.GameObject("PropTest");
string log = "";
for (int i = 0; i < paths.Length; i++)
{
    var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(paths[i]);
    if (!prefab) { log += "MISSING " + paths[i] + "; "; continue; }
    var go = UnityEngine.Object.Instantiate(prefab, root.transform);
    foreach (var c in go.GetComponentsInChildren<UnityEngine.MonoBehaviour>()) c.enabled = false;
    foreach (var l in go.GetComponentsInChildren<UnityEngine.LODGroup>()) l.enabled = false;
    var rs = go.GetComponentsInChildren<UnityEngine.Renderer>();
    var b = new UnityEngine.Bounds(go.transform.position, UnityEngine.Vector3.zero);
    foreach (var r in rs)
    {
        if (r.name.Contains("LOD1") || r.name.Contains("LOD2") || r.name.Contains("LOD3") || r is UnityEngine.ParticleSystemRenderer) { r.enabled = false; continue; }
        b.Encapsulate(r.bounds);
        var mats = r.sharedMaterials;
        for (int k = 0; k < mats.Length; k++)
        {
            var src = mats[k]; if (!src) continue;
            UnityEngine.Texture tex = null; string used = "-";
            UnityEngine.Color col = UnityEngine.Color.white;
            // Saved values straight from the asset: they are still there when the material's shader is gone.
            var saved = new UnityEditor.SerializedObject(src).FindProperty("m_SavedProperties");
            var texs = saved.FindPropertyRelative("m_TexEnvs"); var cols = saved.FindPropertyRelative("m_Colors");
            foreach (var n in texNames)
            {
                for (int e = 0; e < texs.arraySize && !tex; e++)
                {
                    var entry = texs.GetArrayElementAtIndex(e);
                    if (entry.FindPropertyRelative("first").stringValue != n) continue;
                    tex = entry.FindPropertyRelative("second.m_Texture").objectReferenceValue as UnityEngine.Texture;
                    if (tex) used = n;
                }
                if (tex) break;
            }
            bool gotCol = false;
            foreach (var n in colNames)
            {
                for (int e = 0; e < cols.arraySize && !gotCol; e++)
                {
                    var entry = cols.GetArrayElementAtIndex(e);
                    if (entry.FindPropertyRelative("first").stringValue != n) continue;
                    col = entry.FindPropertyRelative("second").colorValue; col.a = 1f; gotCol = true;
                }
                if (gotCol) break;
            }
            if (src.name.StartsWith("TSI_")) col = UnityEngine.Color.white;
            string lower = src.name.ToLower();
            bool leaf = lower.Contains("foliage") || lower.Contains("vegetation") || lower.Contains("tree") || lower.Contains("grass") || lower.Contains("flower") || lower.Contains("plant") || lower.Contains("leaf");
            var m = TrainSim.Mat(col, !leaf);
            m.name = src.name;
            if (tex) m.SetTexture("_MainTex", tex);
            if (leaf) { m.EnableKeyword("N_F_CO_ON"); m.SetFloat("_N_F_CO", 1f); m.SetFloat("_Cutout", 0.4f); m.SetInt("_Culling", 0); m.renderQueue = 2450; m.SetOverrideTag("RenderType", "TransparentCutout"); }
            mats[k] = m;
            log += src.name + "[" + used + (leaf ? ",leaf" : "") + " " + UnityEngine.ColorUtility.ToHtmlStringRGB(col) + "] ";
        }
        r.sharedMaterials = mats;
    }
    float h = UnityEngine.Mathf.Max(b.size.y, 0.01f);
    float want = paths[i].Contains("Tree") || paths[i].Contains("Torii") || paths[i].Contains("Bamboo") ? 5f : 2f;
    go.transform.localScale *= want / h;
    go.transform.position = new UnityEngine.Vector3(-13f + (i % 6) * 5.2f, 0f, i < 6 ? 6.5f : -6.5f);
    log += "| " + prefab.name + " h=" + h.ToString("0.0") + " ; ";
}
UnityEngine.Time.timeScale = 0f;
return "PROPS " + log;
