using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
public static class InspectSpecialFinish
{
    public static string Run()
    {
        var report=new System.Text.StringBuilder();
        foreach(string folder in new[]{"Scenery/hell","Scenery/fortress","Models/Race_trenchwall"})
        {
            report.AppendLine(folder);
            foreach(var go in Resources.LoadAll<GameObject>(folder))
            {
                var names=new HashSet<string>();foreach(var r in go.GetComponentsInChildren<Renderer>())foreach(var m in r.sharedMaterials)if(m)names.Add(m.name);
                report.AppendLine(go.name+": "+string.Join(",",names));
            }
        }
        return report.ToString();
    }
}
