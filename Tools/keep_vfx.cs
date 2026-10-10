// One-off editor script: keeps a handful of effects from Gabriel Aguiar's VFX Graph Mega Packs (Vol 1 and 4)
// as Resources/Vfx/<name>, with whatever they depend on, and deletes the rest of both packs.
const string P = "Assets/GabrielAguiarProductions/Prefabs/";
var picks = new (string from, string to)[]
{
    ("StylizedSmoke/vfxgraph_StylizedSmoke_Impact_v4_Fire", "rocket"), ("StylizedSmoke/vfxgraph_StylizedSmoke_Impact_v1", "grenade"),
    ("StylizedSmoke/vfxgraph_StylizedSmoke_Impact_v6_Arcane", "pulse"), ("StylizedSmoke/vfxgraph_StylizedSmoke_Impact_v7", "nova"),
    ("ElectricOrbs/vfx_Electricity_Burst_v1", "storm"), ("ElectricOrbs/vfx_Electricity_Burst_v3", "spark"),
    ("StylizedSmoke/vfxgraph_StylizedSmoke_Impact_v5_Poison", "enemy"), ("LevelUpEffects/vfx_LevelUp_v1_orange", "levelup"),
    ("StylizedFire/vfx_Fire01_v1", "fire"),
};
System.IO.Directory.CreateDirectory("Assets/Resources/Vfx");
UnityEditor.AssetDatabase.Refresh();
string log = "";
var keep = new System.Collections.Generic.HashSet<string>();
foreach (var pick in picks)
{
    string to = "Assets/Resources/Vfx/" + pick.to + ".prefab";
    string error = UnityEditor.AssetDatabase.MoveAsset(P + pick.from + ".prefab", to);
    if (error != "") { log += error + "; "; continue; }
    foreach (string dep in UnityEditor.AssetDatabase.GetDependencies(to, true)) keep.Add(dep);
}
var gone = new System.Collections.Generic.List<string>();
foreach (string guid in UnityEditor.AssetDatabase.FindAssets("", new[] { "Assets/GabrielAguiarProductions" }))
{
    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
    if (!UnityEditor.AssetDatabase.IsValidFolder(path) && !keep.Contains(path)) gone.Add(path);
}
var failed = new System.Collections.Generic.List<string>();
UnityEditor.AssetDatabase.DeleteAssets(gone.ToArray(), failed);
for (int pass = 0; pass < 8; pass++)
    foreach (string guid in UnityEditor.AssetDatabase.FindAssets("", new[] { "Assets/GabrielAguiarProductions" }))
    {
        string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
        if (UnityEditor.AssetDatabase.IsValidFolder(path) && UnityEditor.AssetDatabase.FindAssets("", new[] { path }).Length == 0) UnityEditor.AssetDatabase.DeleteAsset(path);
    }
return "KEPT " + keep.Count + " deleted=" + gone.Count + " failed=" + failed.Count + " " + log;
