using System.Collections.Generic;
using UnityEngine;

public partial class RaceEvent
{
    static readonly Dictionary<(Material, string), Material> citySurfaces = new();
    static readonly Dictionary<(Mesh, bool), Mesh> cityMappedMeshes = new();

    static string CitySurface(string name) => name switch
    {
        "RaceConcrete" or "CityConcrete" or "CityCream" or "CityBrick" => "concrete",
        "RaceSteel" or "RaceWhite" or "RacePodium" or "CarPaint" or "CitySteel" or "CityFrame" or "CityRoof" => "steel",
        _ => null
    };

    string RaceSurface(string name) => crateRun ? name switch
    {
        "Basalt" or "Basalt_FFFFFF" => "hellstone",
        "InfernalIron" => "steel",
        _ => null
    } : trench ? name switch
    {
        "TrenchDark" or "TrenchPlate" or "FortArmor" or "FortLight" or "RaceSteel" or "RaceWhite" => "trencharmor",
        "TrenchPipe" => "steel",
        _ => null
    } : CitySurface(name);

    static Material PaintedSurface(string texture, Color tint)
    {
        var paint=TrainSim.Mat(tint);paint.mainTexture=Resources.Load<Texture2D>("RaceSurfaces/"+texture);paint.enableInstancing=true;return paint;
    }

    // Shared painted clones stay local to the race; biome prefabs retain their original materials.
    void FinishRaceObject(GameObject go)
    {
        foreach(var renderer in go.GetComponentsInChildren<MeshRenderer>())
        {
            TrainSim.Toon(renderer);
            var mats=renderer.sharedMaterials;bool textured=false;
            var selected=new bool[mats.Length];
            for(int i=0;i<mats.Length;i++)
            {
                string surface=mats[i] ? RaceSurface(mats[i].name) : null;if(surface==null)continue;
                var key=(mats[i],surface);
                if(!citySurfaces.TryGetValue(key,out var paint))
                {
                    var texture=Resources.Load<Texture2D>("RaceSurfaces/"+surface);if(!texture)continue;
                    paint=new Material(mats[i]) { name=mats[i].name+" (race painted)",mainTexture=texture,enableInstancing=true };
                    paint.mainTextureScale=Vector2.one;paint.mainTextureOffset=Vector2.zero;
                    var tint=paint.GetColor("_MainColor");
                    if(trench||crateRun)tint=surface=="steel" ? (crateRun?new Color(.55f,.4f,.35f):new Color(.7f,.8f,1f)) : Color.white;
                    else tint=new Color(tint.r*1.8f,tint.g*1.8f,tint.b*1.8f,tint.a);
                    paint.SetColor("_MainColor",tint);citySurfaces[key]=paint;
                }
                selected[i]=true;textured=true;
                mats[i]=paint;
            }
            if(!textured)continue;
            renderer.sharedMaterials=mats;
            var filter=renderer.GetComponent<MeshFilter>();if(!filter||!filter.sharedMesh||!filter.sharedMesh.isReadable)continue;
            Mesh source=filter.sharedMesh;
            if(source.name=="Barrier")continue; // its UVs follow each unbent section, not the world-space curve
            var meshKey=(source,trench);
            if(!cityMappedMeshes.TryGetValue(meshKey,out var mapped))
            {
                mapped=Instantiate(source);mapped.name=source.name+" (race surface UV)";
                var vertices=source.vertices;var normals=source.normals;var uv=source.uv;
                if(uv.Length!=vertices.Length)uv=new Vector2[vertices.Length];
                // Only the painted submeshes: the tower windows retain their existing facade mapping.
                for(int sub=0;sub<source.subMeshCount&&sub<selected.Length;sub++)if(selected[sub])
                    foreach(int v in source.GetTriangles(sub))uv[v]=SurfaceUV(vertices[v],normals[v])*(trench?.5f:1f);
                mapped.uv=uv;cityMappedMeshes[meshKey]=mapped;
            }
            filter.sharedMesh=mapped;
        }
    }

    static Vector2 SurfaceUV(Vector3 p,Vector3 n)
    {
        var a=new Vector3(Mathf.Abs(n.x),Mathf.Abs(n.y),Mathf.Abs(n.z));
        return (a.y>a.x&&a.y>a.z ? new Vector2(p.x,p.z) : a.x>a.z ? new Vector2(p.z,p.y) : new Vector2(p.x,p.y))*.25f;
    }
}
