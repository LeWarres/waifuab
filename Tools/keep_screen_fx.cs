// One-off editor script: keeps three of Hovl Studio's fullscreen effects (as Resources/Screen/<name>) with
// whatever they depend on, and deletes the rest of the pack.
var picks = new (string from, string to)[] { ("Screen blood", "blood"), ("Screen healing", "healing"), ("Screen wind straight", "wind") };
System.IO.Directory.CreateDirectory("Assets/Resources/Screen");
UnityEditor.AssetDatabase.Refresh();
string log = "";
var keep = new System.Collections.Generic.HashSet<string>();
foreach (var pick in picks)
{
    string to = "Assets/Resources/Screen/" + pick.to + ".prefab";
    string error = UnityEditor.AssetDatabase.MoveAsset("Assets/Hovl Studio/Fullscreen effects/Prefabs/" + pick.from + ".prefab", to);
    if (error != "") log += error + "; ";
    foreach (string dep in UnityEditor.AssetDatabase.GetDependencies(to, true)) keep.Add(dep);
}
var gone = new System.Collections.Generic.List<string>();
foreach (string guid in UnityEditor.AssetDatabase.FindAssets("", new[] { "Assets/Hovl Studio" }))
{
    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
    if (!UnityEditor.AssetDatabase.IsValidFolder(path) && !keep.Contains(path)) gone.Add(path);
}
var failed = new System.Collections.Generic.List<string>();
UnityEditor.AssetDatabase.DeleteAssets(gone.ToArray(), failed);
// Folders left empty.
for (int pass = 0; pass < 6; pass++)
    foreach (string guid in UnityEditor.AssetDatabase.FindAssets("", new[] { "Assets/Hovl Studio" }))
    {
        string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
        if (UnityEditor.AssetDatabase.IsValidFolder(path) && UnityEditor.AssetDatabase.FindAssets("", new[] { path }).Length == 0) UnityEditor.AssetDatabase.DeleteAsset(path);
    }
return "KEPT " + keep.Count + " deleted=" + gone.Count + " failed=" + failed.Count + " " + log;
