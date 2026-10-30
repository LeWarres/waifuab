using System;
using System.IO;
using UnityEditor;
using UnityEngine;
public static class SetupCuluStory
{
    static void Folder(string p){if(AssetDatabase.IsValidFolder(p))return;Folder(Path.GetDirectoryName(p).Replace('\\','/'));AssetDatabase.CreateFolder(Path.GetDirectoryName(p).Replace('\\','/'),Path.GetFileName(p));}
    static Material Mat(string name,Color color)
    {
        string path="Assets/Art/ThuluWorld/"+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!material){material=TrainSim.Mat(color);AssetDatabase.CreateAsset(material,path);}return material;
    }
    static void Shape(Transform parent,PrimitiveType type,Vector3 position,Vector3 scale,Material material)
    {
        var o=GameObject.CreatePrimitive(type);UnityEngine.Object.DestroyImmediate(o.GetComponent<Collider>());
        o.transform.SetParent(parent,false);o.transform.localPosition=position;o.transform.localScale=scale;o.GetComponent<Renderer>().sharedMaterial=material;
    }
    static void Tube(Transform parent,string name,Func<float,Vector3> curve,float radius,Material material)
    {
        const int length=24,sides=8;var vertices=new Vector3[(length+1)*sides];var triangles=new int[length*sides*6];
        for(int n=0;n<=length;n++)
        {
            float t=n/(float)length;Vector3 at=curve(t);
            Vector3 tangent=(curve(Mathf.Min(1,t+.01f))-curve(Mathf.Max(0,t-.01f))).normalized;
            Vector3 across=Vector3.Cross(tangent,Vector3.forward).normalized,up=Vector3.Cross(tangent,across).normalized;
            float r=radius*Mathf.Lerp(1f,.06f,t);
            for(int k=0;k<sides;k++)
            {
                float a=k*Mathf.PI*2/sides;int x=n*sides+k;vertices[x]=at+r*(Mathf.Cos(a)*across+Mathf.Sin(a)*up);
                if(n==length)continue;int y=n*sides+(k+1)%sides,z=x+sides,w=y+sides,q=x*6;
                triangles[q]=x;triangles[q+1]=z;triangles[q+2]=y;triangles[q+3]=y;triangles[q+4]=z;triangles[q+5]=w;
            }
        }
        string path="Assets/Art/ThuluWorld/"+name+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(!mesh){mesh=new Mesh{name=name,vertices=vertices,triangles=triangles};mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);}
        var o=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));o.transform.SetParent(parent,false);
        o.GetComponent<MeshFilter>().sharedMesh=mesh;o.GetComponent<MeshRenderer>().sharedMaterial=material;
    }
    public static string Run()
    {
        if(Application.isPlaying)throw new Exception("Edit Mode required");
        Folder("Assets/Art/ThuluWorld");Folder("Assets/Resources/Scenery/thulu");
        var moss=Mat("Moss",new Color(.25f,.49f,.33f));var jade=Mat("Jade",new Color(.45f,.68f,.45f));
        var dark=Mat("DeepViolet",new Color(.13f,.07f,.2f));var ivory=Mat("AncientBone",new Color(.65f,.76f,.59f));
        var red=Mat("WatchingEye",new Color(.85f,.16f,.15f));
        for(int kind=0;kind<4;kind++)
        {
            var root=new GameObject("Thulu_"+kind);var p=root.transform;
            try
            {
                if(kind==0)
                {
                    Shape(p,PrimitiveType.Sphere,new Vector3(0,.4f,0),new Vector3(1.7f,.65f,.8f),moss);
                    Shape(p,PrimitiveType.Sphere,new Vector3(0,.45f,-.4f),new Vector3(.5f,.48f,.18f),red);
                    Shape(p,PrimitiveType.Sphere,new Vector3(0,.45f,-.5f),new Vector3(.1f,.35f,.06f),dark);
                }
                else if(kind==1)
                {
                    Tube(p,"YoungTendril",t=>new Vector3(Mathf.Sin(t*5)*.45f,t*2.7f,Mathf.Cos(t*4)*.3f),.42f,jade);
                    Shape(p,PrimitiveType.Sphere,new Vector3(0,.12f,0),new Vector3(1.6f,.3f,1.5f),dark);
                }
                else if(kind==2)
                {
                    for(int rib=0;rib<3;rib++)
                    {
                        int r=rib;Tube(p,"AncientRib_"+rib,t=>new Vector3(Mathf.Cos(t*Mathf.PI)*6,Mathf.Sin(t*Mathf.PI)*9,r*2-2),.8f,rib==1?ivory:moss);
                    }
                    Shape(p,PrimitiveType.Sphere,new Vector3(-4,.3f,0),new Vector3(4,.8f,5),dark);
                    Shape(p,PrimitiveType.Sphere,new Vector3(4,.3f,0),new Vector3(4,.8f,5),dark);
                }
                else
                {
                    for(int n=0;n<4;n++)
                    {
                        int q=n;Tube(p,"OldTendril_"+n,t=>new Vector3((q-1.5f)*1.1f+Mathf.Sin(t*5+q)*t,t*(6+q*.6f),Mathf.Cos(t*5+q)*t),.65f,n%2==0?moss:jade);
                    }
                }
                PrefabUtility.SaveAsPrefabAsset(root,"Assets/Resources/Scenery/thulu/"+kind+".prefab");
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        foreach(string folder in new[]{"Ground","Sky"})
        {
            string path="Assets/Resources/"+folder+"/thulu.png";
            if(!File.Exists(path))AssetDatabase.CopyAsset("Assets/Resources/"+folder+"/alien.png",path);
        }
        var station=new GameObject("Station_thulu");
        try
        {
            UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Models/Station_alien"),station.transform);
            var arch=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Scenery/thulu/2.prefab"),station.transform);
            arch.transform.localPosition=new Vector3(0,0,4);arch.transform.localScale=Vector3.one*.7f;
            PrefabUtility.SaveAsPrefabAsset(station,"Assets/Resources/Models/Station_thulu.prefab");
        }
        finally{UnityEngine.Object.DestroyImmediate(station);}
        foreach(string folder in new[]{"Ground","Sky"})
        {
            string path="Assets/Resources/"+folder+"/thulu.png";AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
            var t=(TextureImporter)AssetImporter.GetAtPath(path);t.textureType=TextureImporterType.Default;t.sRGBTexture=true;
            t.npotScale=TextureImporterNPOTScale.None;t.mipmapEnabled=folder=="Ground";t.wrapMode=folder=="Ground"?TextureWrapMode.Repeat:TextureWrapMode.Clamp;
            t.filterMode=FilterMode.Bilinear;t.maxTextureSize=2048;t.textureCompression=TextureImporterCompression.CompressedHQ;t.SaveAndReimport();
        }
        int count=0;
        foreach(string path in Directory.GetFiles("Assets/Resources/Story","culu_*.png"))
        {
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);var t=(TextureImporter)AssetImporter.GetAtPath(path);
            t.textureType=TextureImporterType.Default;t.sRGBTexture=true;t.mipmapEnabled=false;t.npotScale=TextureImporterNPOTScale.None;
            t.wrapMode=TextureWrapMode.Clamp;t.filterMode=FilterMode.Bilinear;t.maxTextureSize=2048;t.textureCompression=TextureImporterCompression.CompressedHQ;t.SaveAndReimport();count++;
        }
        AssetDatabase.SaveAssets();return LocalizationImport.Run()+" Thulu World: four scenery prefabs, station and "+count+" Culu illustrations imported.";
    }
}
