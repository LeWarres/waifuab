// Editor script: brings the red-and-white locomotive's materials in line with the rest of the train. Its white
// was pure white and its red close to pure red, so it stood out as shiny beside the wagons' cream white and
// softer paints. Colours are given as they look (sRGB) and stored linear, like TrainSim.Mat does.
var look = new (string name, float r, float g, float b)[]
{
    ("RW_White", 0.88f, 0.88f, 0.85f),   // the wagons' own white
    ("RW_Red", 0.78f, 0.16f, 0.14f),
    ("RW_DeepRed", 0.5f, 0.09f, 0.09f),
    ("RW_Black", 0.05f, 0.05f, 0.06f),
    ("RW_Steel", 0.35f, 0.37f, 0.42f),   // the wagons' steel
    ("RW_Glass", 0.25f, 0.55f, 0.85f),   // the wagons' glass
    ("RW_Lamp", 1f, 0.95f, 0.75f),
};
var toon = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/Resources/Toon.mat");
string done = "";
foreach (var paint in look)
{
    var m = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/Materials/Locomotive_RedWhite/" + paint.name + ".mat");
    if (!m) { done += "MISSING " + paint.name + " "; continue; }
    // Everything but the colour comes from the game's one toon material: shadows, outline, no gloss, no rim.
    m.CopyPropertiesFromMaterial(toon);
    m.shaderKeywords = toon.shaderKeywords;
    m.renderQueue = toon.renderQueue;
    m.enableInstancing = true;
    m.SetColor("_MainColor", new UnityEngine.Color(paint.r, paint.g, paint.b, 1f).linear);
    UnityEditor.EditorUtility.SetDirty(m);
    done += paint.name + " ";
}
UnityEditor.AssetDatabase.SaveAssets();
return "MATCHED " + done;
