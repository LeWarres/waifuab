using UnityEditor;
using UnityEngine;

public static class SetupBbaPreviewMaterial
{
    public static string Run()
    {
        const string path = "Assets/Resources/Preview/BbaMatte.mat";
        Material original = Resources.Load<Material>("Characters/bba/bba");
        var matte = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!matte)
        {
            matte = new Material(original);
            AssetDatabase.CreateAsset(matte, path);
        }
        matte.name = "BbaMatte";
        matte.SetColor("_MainColor", new Color(0.86f, 0.88f, 0.92f).linear);
        matte.SetColor("_HighlightColor", new Color(0.86f, 0.9f, 0.97f).linear);
        matte.SetColor("_OverallShadowColor", new Color(0.69f, 0.76f, 0.88f));
        matte.SetFloat("_Saturation", 0.72f);
        matte.SetFloat("_GlossIntensity", 0f);
        matte.SetFloat("_GlossColorPower", 0f);
        matte.SetFloat("_ReflectionIntensity", 0f);
        matte.SetFloat("_RimLigInt", 0f);
        matte.SetFloat("_SelfLitIntensity", 0f);
        matte.SetFloat("_HighlightColorPower", 0.75f);
        matte.SetFloat("_EnvironmentalLightingIntensity", 0.85f);
        EditorUtility.SetDirty(matte);
        AssetDatabase.SaveAssets();
        return "BBA preview uses a matte RealToon material with softer colors and cool shadows.";
    }
}
