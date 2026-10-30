using System;
using System.IO;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization.Tables;
public static class SetupGolemStory
{
    static void Folder(string path){if(AssetDatabase.IsValidFolder(path))return;Folder(Path.GetDirectoryName(path).Replace('\\','/'));AssetDatabase.CreateFolder(Path.GetDirectoryName(path).Replace('\\','/'),Path.GetFileName(path));}
    static Material Mat(string name,Color color)
    {
        string p="Assets/Art/RobotHeaven/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(p);if(!m){m=TrainSim.Mat(color);AssetDatabase.CreateAsset(m,p);}return m;
    }
    static Transform Shape(Transform parent,PrimitiveType type,Vector3 at,Vector3 size,Material mat)
    {
        var o=GameObject.CreatePrimitive(type);UnityEngine.Object.DestroyImmediate(o.GetComponent<Collider>());o.transform.SetParent(parent,false);o.transform.localPosition=at;o.transform.localScale=size;o.GetComponent<Renderer>().sharedMaterial=mat;return o.transform;
    }
    static void Ring(Transform parent,Vector3 at,float radius,Material mat,bool upright=false)
    {
        string path="Assets/Art/RobotHeaven/Ring_"+radius.ToString("F1",System.Globalization.CultureInfo.InvariantCulture)+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(!mesh)
        {
            const int n=40,sides=8;var v=new Vector3[n*sides];var tris=new int[n*sides*6];
            for(int i=0;i<n;i++)for(int j=0;j<sides;j++)
            {
                float a=i*Mathf.PI*2/n,b=j*Mathf.PI*2/sides;int k=i*sides+j;float r=radius+.1f*Mathf.Cos(b);
                v[k]=new Vector3(Mathf.Cos(a)*r,Mathf.Sin(b)*.1f,Mathf.Sin(a)*r);int next=((i+1)%n)*sides+j,q=i*sides+(j+1)%sides,z=((i+1)%n)*sides+(j+1)%sides,t=k*6;
                tris[t]=k;tris[t+1]=q;tris[t+2]=next;tris[t+3]=q;tris[t+4]=z;tris[t+5]=next;
            }
            mesh=new Mesh{name="CeramicHalo",vertices=v,triangles=tris};mesh.RecalculateNormals();AssetDatabase.CreateAsset(mesh,path);
        }
        var ring=new GameObject("CircuitHalo",typeof(MeshFilter),typeof(MeshRenderer));ring.transform.SetParent(parent,false);ring.transform.localPosition=at;if(upright)ring.transform.localRotation=Quaternion.Euler(90,0,0);
        ring.GetComponent<MeshFilter>().sharedMesh=mesh;ring.GetComponent<MeshRenderer>().sharedMaterial=mat;
    }
    static void Texture(string folder)
    {
        string path="Assets/Resources/"+folder+"/robotheaven.png";
        if(!File.Exists(path))
        {
            const int size=256;var tex=new Texture2D(size,size,TextureFormat.RGB24,false);var pixels=new Color[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                Color c;
                if(folder=="Sky")c=Color.Lerp(new Color(.63f,.76f,.9f),new Color(.9f,.95f,.99f),y/(float)size);
                else
                {
                    bool seam=x%64<2||y%64<2,circuit=(x%128==32&&y%128<96)||(y%128==96&&x%128>=32);
                    c=seam?new Color(.7f,.8f,.86f):circuit?new Color(.94f,.76f,.36f):new Color(.9f,.94f,.97f);
                }
                pixels[y*size+x]=c;
            }
            tex.SetPixels(pixels);tex.Apply();File.WriteAllBytes(path,tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);
        }
        AssetDatabase.ImportAsset(path);var t=(TextureImporter)AssetImporter.GetAtPath(path);t.textureType=TextureImporterType.Default;t.sRGBTexture=true;t.mipmapEnabled=folder=="Ground";t.wrapMode=folder=="Ground"?TextureWrapMode.Repeat:TextureWrapMode.Clamp;t.SaveAndReimport();
    }
    public static string Run()
    {
        if(Application.isPlaying)throw new Exception("Edit Mode required");
        Folder("Assets/Art/RobotHeaven");Folder("Assets/Resources/Scenery/robotheaven");
        var ivory=Mat("Ceramic",new Color(.91f,.94f,.97f));var shadow=Mat("BlueCeramic",new Color(.47f,.65f,.78f));var gold=Mat("GoldCircuit",new Color(.98f,.69f,.24f));var glow=Mat("MemoryLight",new Color(.35f,.87f,.98f));
        for(int kind=0;kind<4;kind++)
        {
            string path="Assets/Resources/Scenery/robotheaven/"+kind+".prefab";if(File.Exists(path))continue;
            var root=new GameObject("MemorySanctuary_"+kind);var p=root.transform;
            try
            {
                Shape(p,PrimitiveType.Cylinder,new Vector3(0,.13f,0),new Vector3(kind<2?1.6f:7,.13f,kind<2?1.6f:7),shadow);
                if(kind==0)
                {
                    Shape(p,PrimitiveType.Cylinder,new Vector3(0,.32f,0),new Vector3(.9f,.2f,.9f),ivory);Shape(p,PrimitiveType.Sphere,new Vector3(0,.72f,0),Vector3.one*.45f,glow);Ring(p,new Vector3(0,.65f,0),.6f,gold);
                }
                else if(kind==1)
                {
                    for(int n=0;n<3;n++)Shape(p,PrimitiveType.Sphere,new Vector3((n-1)*.5f,.6f,n%2*.3f),new Vector3(.9f,.65f,.8f),ivory);Ring(p,new Vector3(0,1.15f,0),.6f,gold);
                }
                else if(kind==2)
                {
                    for(int side=-1;side<=1;side+=2)
                    {
                        Shape(p,PrimitiveType.Cylinder,new Vector3(side*2.5f,3.2f,0),new Vector3(.9f,3f,.9f),ivory);
                        Shape(p,PrimitiveType.Cylinder,new Vector3(side*2.5f,.5f,0),new Vector3(1.7f,.3f,1.7f),gold);
                        Shape(p,PrimitiveType.Cube,new Vector3(side*2.5f,3.4f,-.48f),new Vector3(.16f,4.4f,.08f),glow);
                        Shape(p,PrimitiveType.Sphere,new Vector3(side*2.5f,6.5f,0),Vector3.one*.65f,gold);
                    }
                    Ring(p,new Vector3(0,6.5f,0),2.5f,gold,true);Shape(p,PrimitiveType.Cube,new Vector3(0,.55f,0),new Vector3(5,.4f,3),ivory);
                }
                else
                {
                    Shape(p,PrimitiveType.Cylinder,new Vector3(0,.55f,0),new Vector3(4,.4f,4),ivory);
                    var model=Resources.Load<GameObject>("Characters/sentinel/sentinel");if(!model)throw new Exception("Missing Sentinel hero model");
                    var statue=UnityEngine.Object.Instantiate(model,p).transform;var bounds=new Bounds();bool any=false;
                    foreach(var r in statue.GetComponentsInChildren<Renderer>()){if(any)bounds.Encapsulate(r.bounds);else bounds=r.bounds;any=true;r.sharedMaterials=Array.ConvertAll(r.sharedMaterials,_=>ivory);}
                    float scale=4.4f/Mathf.Max(.01f,bounds.size.y);statue.localScale*=scale;statue.localPosition=new Vector3(0,1f-bounds.min.y*scale,0);statue.localRotation=Quaternion.Euler(0,180,0);
                    Ring(p,new Vector3(0,6f,0),1.4f,gold);Shape(p,PrimitiveType.Sphere,new Vector3(0,6f,0),Vector3.one*.25f,glow);
                }
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        string stationPath="Assets/Resources/Models/Station_robotheaven.prefab";
        if(!File.Exists(stationPath))
        {
            var root=new GameObject("Station_robotheaven");try
            {
                var model=Resources.Load<GameObject>("Models/Station_heaven");if(!model)throw new Exception("Missing heaven station");UnityEngine.Object.Instantiate(model,root.transform);
                var arch=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Scenery/robotheaven/2.prefab"),root.transform);arch.transform.localPosition=new Vector3(0,0,5);PrefabUtility.SaveAsPrefabAsset(root,stationPath);
            }finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        Texture("Ground");Texture("Sky");int pictures=0,entries=0;
        foreach(var file in Directory.GetFiles("Assets/Resources/Story","golem_*.png"))
        {
            string p=file.Replace('\\','/');AssetDatabase.ImportAsset(p,ImportAssetOptions.ForceUpdate);var t=(TextureImporter)AssetImporter.GetAtPath(p);t.textureType=TextureImporterType.Default;t.sRGBTexture=true;t.mipmapEnabled=false;t.npotScale=TextureImporterNPOTScale.None;t.wrapMode=TextureWrapMode.Clamp;t.filterMode=FilterMode.Bilinear;t.maxTextureSize=2048;t.textureCompression=TextureImporterCompression.CompressedHQ;t.SaveAndReimport();pictures++;
        }
        var lines=File.ReadAllLines("Assets/Localization/strings.tsv");var header=lines[0].Split('\t');var collection=LocalizationEditorSettings.GetStringTableCollection("Game");
        for(int c=1;c<header.Length;c++)
        {
            var table=(StringTable)collection.GetTable(header[c]);foreach(var line in lines)if(line.StartsWith("story.sentinel.")||line.StartsWith("golem.")||line.StartsWith("biome.robotheaven\t")||line.StartsWith("boss.name.robotheaven\t")){var cells=line.Split('\t');table.AddEntry(cells[0],cells[c]);entries++;}EditorUtility.SetDirty(table);
        }
        EditorUtility.SetDirty(collection.SharedData);AssetDatabase.SaveAssets();return $"Robotic Heaven: four scenery prefabs, station, ground/sky. Imported {pictures} pictures and {entries} translations.";
    }
}
