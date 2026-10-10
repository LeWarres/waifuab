// Editor script (run with `unity command eval_file`): the game's toon look in one place. Applies the same
// RealToon settings to Resources/Toon.mat (every material made in code copies it) and to the baked scenery
// materials. Per-material choices (colour, texture, leaf cut-out, outline off) are left alone.
var mats = new System.Collections.Generic.List<UnityEngine.Material> { UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/Resources/Toon.mat") };
foreach (string guid in UnityEditor.AssetDatabase.FindAssets("t:Material", new[] { "Assets/Scenery/Materials" }))
    mats.Add(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid)));
foreach (var m in mats)
{
    // Shadows: light and blue-violet, never black.
    m.SetColor("_OverallShadowColor", new UnityEngine.Color(0.56f, 0.6f, 0.93f, 1f));
    // Outline: thin, dark navy rather than black.
    m.SetFloat("_OutlineWidth", 1.5f);
    m.SetColor("_OutlineColor", new UnityEngine.Color(0.12f, 0.1f, 0.26f, 1f));
    // No rim light: on flat boxes and on the ground it only washes the colour out.
    m.DisableKeyword("N_F_RL_ON"); m.SetFloat("_N_F_RL", 0f);
    UnityEditor.EditorUtility.SetDirty(m);
}
UnityEditor.AssetDatabase.SaveAssets();
return "LOOK applied to " + mats.Count;
