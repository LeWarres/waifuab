using System;
using UnityEditor;
using UnityEngine;
public static class FixRaceBarrierReadback
{
    public static string Run()
    {
        if(Application.isPlaying)throw new Exception("Edit Mode required");
        foreach(string name in new[]{"barrier","trenchwall"})
        {
            string path="Assets/Resources/Models/Race_"+name+".fbx";
            var importer=(ModelImporter)AssetImporter.GetAtPath(path);
            if(!importer.isReadable){importer.isReadable=true;importer.SaveAndReimport();}
            var model=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            foreach(var filter in model.GetComponentsInChildren<MeshFilter>(true))
                if(!filter.sharedMesh.isReadable)throw new Exception("Race mesh still unreadable: "+name);
        }
        return "Race barrier and trenchwall Read/Write enabled: RaceEvent.Barriers reads both meshes to build the course walls.";
    }
}
