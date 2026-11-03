using System.Text;
using UnityEditor;
using UnityEngine;
public static class InspectRicMar
{
    public static string Run()
    {
        var go=Resources.Load<GameObject>("Models/Locomotive");var b=new StringBuilder();
        foreach(var f in go.GetComponentsInChildren<MeshFilter>())b.AppendLine(f.name+" vertices="+f.sharedMesh.vertexCount+" uv="+f.sharedMesh.uv.Length+" bounds="+f.sharedMesh.bounds);
        foreach(var r in go.GetComponentsInChildren<Renderer>())foreach(var m in r.sharedMaterials)b.AppendLine(m.name+" path="+AssetDatabase.GetAssetPath(m)+" shader="+m.shader.name+" color="+m.GetColor("_MainColor")+" texture="+m.mainTexture);
        return b.ToString();
    }
}
