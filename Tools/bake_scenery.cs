// Editor script (run with `unity command eval_file`): builds ALL the game's scenery from scratch.
// Sources: prefabs of the ToonScapes and Low Poly Mega City packs (import both first) and the models made by
// Tools/make_props.py. Each pick keeps its top LOD only, loses scripts and colliders, gets RealToon materials
// (tinted if asked; a material named Glow... comes out bright), is scaled to its height (0 = as modelled) with
// its base on the ground, and is saved under Resources/Scenery/<biome>/. Then the meshes and textures in use move
// to Assets/Scenery/Source and both packs are deleted.
const string TS = "Assets/ToonScapes/Spring Isles/Prefabs/", MC = "Assets/JC_LP_MegaCity/Prefabs/", PR = "Assets/Scenery/Props/";
UnityEditor.AssetDatabase.DeleteAsset("Assets/Resources/Scenery");
var picks = new (string biome, string path, float height, string tint)[]
{
    // desert
    ("desert", PR + "desert__cactus_tall", 0f, "FFFFFF"),
    ("desert", PR + "desert__cactus_round", 0f, "FFFFFF"),
    ("desert", PR + "desert__dry_bush", 0f, "FFFFFF"),
    ("desert", PR + "desert__ribs", 0f, "FFFFFF"),
    ("desert", TS + "Rocks/TSI_Rock_Medium_01A", 1.8f, "D9B27A"),
    ("desert", TS + "Rocks/TSI_Rock_Large_01A", 3.5f, "D9B27A"),
    ("desert", TS + "Rocks/TSI_Rock_Small_02A", 0.7f, "D9B27A"),
    ("desert", TS + "Vegetation/Trees/TSI_Amberleaf_Shrub_01A", 1.5f, "FFFFFF"),
    ("desert", TS + "Rocks/TSI_Cliff_01A", 6f, "D9B27A"),
    ("desert", TS + "Rocks/TSI_Rock_Small_05A", 0.5f, "D9B27A"),
    ("desert", TS + "Props/Wood Props/TSI_Wood_Cart_01A", 1.6f, "FFFFFF"),
    ("desert", TS + "Props/Wood Props/TSI_Sign_Post_01A", 2.0f, "FFFFFF"),
    // snow
    ("snow", PR + "snow__pine", 0f, "FFFFFF"),
    ("snow", PR + "snow__pine_small", 0f, "FFFFFF"),
    ("snow", PR + "snow__snowman", 0f, "FFFFFF"),
    ("snow", PR + "snow__ice", 0f, "FFFFFF"),
    ("snow", PR + "snow__drift", 0f, "FFFFFF"),
    ("snow", TS + "Rocks/TSI_Rock_Medium_01A", 1.8f, "CFE0F0"),
    ("snow", TS + "Rocks/TSI_Rock_Large_02A", 3.2f, "CFE0F0"),
    ("snow", TS + "Building Props/Torii Gate/TSI_Torii_Gate_01A", 4.5f, "FFFFFF"),
    ("snow", TS + "Building Props/Stone Kit/TSI_Stone_Lantern_01A", 2f, "E0ECFF"),
    ("snow", TS + "Vegetation/Trees/TSI_Blossom_Tree_01A", 4.5f, "FFFFFF"),
    ("snow", TS + "Props/Wood Props/TSI_Wood_Fence_01A", 1.2f, "FFFFFF"),
    ("snow", TS + "Rocks/TSI_Cliff_02A", 6f, "CFE0F0"),
    // jungle
    ("jungle", TS + "Vegetation/Trees/TSI_Broadleaf_Tree_01A", 5.5f, "FFFFFF"),
    ("jungle", TS + "Vegetation/Trees/TSI_Broadleaf_Tree_03A", 4.5f, "FFFFFF"),
    ("jungle", TS + "Vegetation/Trees/TSI_Springleaf_Tree_01A", 5f, "FFFFFF"),
    ("jungle", TS + "Vegetation/Bamboo/TSI_Bamboo_03A", 4f, "FFFFFF"),
    ("jungle", TS + "Vegetation/Bamboo/TSI_Bamboo_05A", 3.2f, "FFFFFF"),
    ("jungle", TS + "Vegetation/Plants & Flowers/TSI_Bush_01A", 1.2f, "FFFFFF"),
    ("jungle", TS + "Vegetation/Plants & Flowers/TSI_Bush_02A", 1.0f, "FFFFFF"),
    ("jungle", TS + "Vegetation/Plants & Flowers/TSI_Flower_Bush_01A", 1.0f, "FFFFFF"),
    ("jungle", TS + "Vegetation/Plants & Flowers/TSI_Flower_Patch_01A", 0.5f, "FFFFFF"),
    ("jungle", TS + "Vegetation/Plants & Flowers/TSI_Grass_Patch_01A", 0.6f, "FFFFFF"),
    ("jungle", TS + "Vegetation/Plants & Flowers/TSI_Grass_Patch_03A", 0.6f, "FFFFFF"),
    ("jungle", TS + "Vegetation/Plants & Flowers/TSI_Plant_09A", 1.5f, "FFFFFF"),
    ("jungle", TS + "Vegetation/Plants & Flowers/TSI_Plant_04A", 1.2f, "FFFFFF"),
    ("jungle", TS + "Vegetation/Plants & Flowers/TSI_Leaf_Patch_01A", 0.3f, "FFFFFF"),
    ("jungle", PR + "jungle__mushrooms", 0f, "FFFFFF"),
    ("jungle", PR + "jungle__log", 0f, "FFFFFF"),
    ("jungle", TS + "Rocks/TSI_Rock_Medium_01A", 1.5f, "9FB08A"),
    ("jungle", TS + "Vegetation/Plants & Flowers/TSI_Stone_Block_19A_Ivy", 1.6f, "FFFFFF"),
    ("jungle", TS + "Rocks/TSI_Cliff_03A", 6f, "9FB08A"),
    // ocean
    ("ocean", PR + "ocean__island_palms", 0f, "FFFFFF"),
    ("ocean", PR + "ocean__islet", 0f, "FFFFFF"),
    ("ocean", PR + "ocean__rocks", 0f, "FFFFFF"),
    ("ocean", PR + "ocean__buoy", 0f, "FFFFFF"),
    ("ocean", PR + "ocean__boat", 0f, "FFFFFF"),
    ("ocean", PR + "ocean__lighthouse", 0f, "FFFFFF"),
    ("ocean", PR + "ocean__piles", 0f, "FFFFFF"),
    ("ocean", PR + "ocean__coral", 0f, "FFFFFF"),
    // volcano
    ("volcano", PR + "volcano__spire", 0f, "FFFFFF"),
    ("volcano", PR + "volcano__lava_pool", 0f, "FFFFFF"),
    ("volcano", PR + "volcano__burnt_tree", 0f, "FFFFFF"),
    ("volcano", PR + "volcano__ember_crystal", 0f, "FFFFFF"),
    ("volcano", TS + "Rocks/TSI_Rock_Large_02A", 4f, "5A3A34"),
    ("volcano", TS + "Rocks/TSI_Rock_Medium_01B", 2f, "5A3A34"),
    ("volcano", TS + "Rocks/TSI_Rock_Small_03A", 0.7f, "5A3A34"),
    ("volcano", TS + "Building Props/Stone Kit/TSI_Stone_Arch_01A", 4f, "6A4A44"),
    ("volcano", TS + "Rocks/TSI_Cliff_04A", 7f, "4A2A26"),
    // city
    ("city", MC + "Buildings/SM_Buildings_Commercial_03", 9f, "FFFFFF"),
    ("city", MC + "Buildings/SM_Buildings_Hotel_01", 8f, "FFFFFF"),
    ("city", MC + "Buildings/SM_Buildings_Commercial_08", 10f, "FFFFFF"),
    ("city", MC + "Buildings/SM_Buildings_House_02_V1", 5f, "FFFFFF"),
    ("city", MC + "Buildings/SM_Buildings_Factory_02", 7f, "FFFFFF"),
    ("city", MC + "Nature/SM_Nature_Tree_03", 3.5f, "FFFFFF"),
    ("city", MC + "Nature/SM_Nature_Tree_07", 3f, "FFFFFF"),
    ("city", MC + "FloorProps/SM_FloorProps_Light_01", 3.5f, "FFFFFF"),
    ("city", MC + "FloorProps/SM_FloorProps_TrafficLight_01", 3f, "FFFFFF"),
    ("city", MC + "FloorProps/SM_FloorProps_RoadSign_05", 2.2f, "FFFFFF"),
    ("city", MC + "FloorProps/SM_FloorProps_BusStation_01", 2.5f, "FFFFFF"),
    ("city", MC + "FloorProps/SM_FloorProps_Fence_01", 1.0f, "FFFFFF"),
    ("city", MC + "Vehicles/SM_Vehicles_Bus_V1", 1.8f, "FFFFFF"),
    ("city", MC + "Vehicles/SM_Vehicles_Taxi_01", 1.1f, "FFFFFF"),
    ("city", MC + "Vehicles/SM_Vehicles_Police_01", 1.1f, "FFFFFF"),
    ("city", MC + "Props/SM_Props_Bench_01", 0.8f, "FFFFFF"),
    ("city", MC + "Props/SM_Props_Garbage_03", 0.9f, "FFFFFF"),
    ("city", MC + "Props/SM_Props_Fountain_01", 1.8f, "FFFFFF"),
    ("city", MC + "Props/SM_Props_Ads_02", 3f, "FFFFFF"),
    // space
    ("space", PR + "space__crater", 0f, "FFFFFF"),
    ("space", PR + "space__antenna", 0f, "FFFFFF"),
    ("space", PR + "space__dome", 0f, "FFFFFF"),
    ("space", PR + "space__crystal", 0f, "FFFFFF"),
    ("space", TS + "Rocks/TSI_Rock_Large_03A", 3f, "8088B0"),
    ("space", TS + "Rocks/TSI_Rock_Small_03A", 1.2f, "8088B0"),
    ("space", TS + "Rocks/TSI_Rock_Medium_01A", 1.6f, "8088B0"),
    ("space", TS + "Building Props/Stone Kit/TSI_Stone_Block_05A", 1.5f, "8890B8"),
    // alien
    ("alien", PR + "alien__glowshroom", 0f, "FFFFFF"),
    ("alien", PR + "alien__stalk", 0f, "FFFFFF"),
    ("alien", PR + "alien__pod", 0f, "FFFFFF"),
    ("alien", TS + "Vegetation/Trees/TSI_Blossom_Tree_02A", 5f, "C080FF"),
    ("alien", TS + "Vegetation/Trees/TSI_Broadleaf_Tree_02A", 4.5f, "60FFE0"),
    ("alien", TS + "Vegetation/Plants & Flowers/TSI_Plant_15A", 1.6f, "80FFD0"),
    ("alien", TS + "Vegetation/Plants & Flowers/TSI_Blossom_Bush_01A", 1.3f, "FF80E0"),
    ("alien", TS + "Rocks/TSI_Rock_Medium_01C", 1.8f, "B070D0"),
    ("alien", TS + "Vegetation/Plants & Flowers/TSI_Grass_Patch_02A", 0.6f, "A060FF"),
    ("alien", TS + "Vegetation/Plants & Flowers/TSI_Flower_Patch_02A", 0.5f, "FF80E0"),
    // fortress
    ("fortress", PR + "fortress__pylon", 0f, "FFFFFF"),
    ("fortress", PR + "fortress__wall", 0f, "FFFFFF"),
    ("fortress", PR + "fortress__gun_post", 0f, "FFFFFF"),
    ("fortress", PR + "fortress__barrier", 0f, "FFFFFF"),
    ("fortress", TS + "Building Props/Stone Kit/TSI_Stone_Arch_01B", 5f, "708078"),
    ("fortress", TS + "Building Props/Stone Kit/TSI_Stone_Block_09A", 2.5f, "708078"),
    ("fortress", TS + "Building Props/Torii Gate/TSI_Torii_Gate_01A", 5f, "405048"),
    ("fortress", TS + "Building Props/Stone Kit/TSI_Stone_Lantern_01A", 2.2f, "80A090"),
    ("fortress", TS + "Rocks/TSI_Rock_Small_05A", 1f, "607068"),
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
bool gone = UnityEditor.AssetDatabase.DeleteAsset("Assets/ToonScapes") & UnityEditor.AssetDatabase.DeleteAsset("Assets/JC_LP_MegaCity");
return "BAKED packsDeleted=" + gone + " " + string.Join(",", counts) + " mats=" + made.Count + " moved=" + moved + " " + log;
