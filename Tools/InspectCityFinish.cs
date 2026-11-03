using System.Text;
using UnityEngine;
using UnityEditor;
public static class InspectCityFinish
{
    public static string RestoreUnusedReadback()
    {
        foreach(string id in new[]{"hydrant","planter"})
        {
            var importer=(ModelImporter)AssetImporter.GetAtPath("Assets/Scenery/Props/city__"+id+".fbx");
            importer.isReadable=false;importer.SaveAndReimport();
        }
        return "Unused small props restored to their original import settings.";
    }
    public static string Run()
    {
        var log=new StringBuilder();
        foreach(var go in Resources.LoadAll<GameObject>("Scenery/city"))
        {
            log.Append(go.name+": ");
            foreach(var r in go.GetComponentsInChildren<Renderer>())foreach(var m in r.sharedMaterials)if(m)log.Append(m.name+" ");log.AppendLine();
        }
        return log.ToString();
    }
}
