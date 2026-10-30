using System;
using System.IO;
using UnityEditor;
using UnityEngine;
public static class SetupOniniStory
{
    static void Folder(string p){if(AssetDatabase.IsValidFolder(p))return;var parent=Path.GetDirectoryName(p).Replace('\\','/');Folder(parent);AssetDatabase.CreateFolder(parent,Path.GetFileName(p));}
    static Material Mat(string id,Color c)
    {
        string p="Assets/Art/OniniWorlds/"+id+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(p);
        if(!m){m=TrainSim.Mat(c);AssetDatabase.CreateAsset(m,p);}return m;
    }
    static Transform Shape(Transform parent,PrimitiveType type,Vector3 at,Vector3 size,Material mat)
    {
        var o=GameObject.CreatePrimitive(type);UnityEngine.Object.DestroyImmediate(o.GetComponent<Collider>());
        o.transform.SetParent(parent,false);o.transform.localPosition=at;o.transform.localScale=size;o.GetComponent<Renderer>().sharedMaterial=mat;return o.transform;
    }
    static void Box(Transform p,Vector3 at,Vector3 size,Material m)=>Shape(p,PrimitiveType.Cube,at,size,m);
    static void Save(GameObject root,string path){try{PrefabUtility.SaveAsPrefabAsset(root,path);}finally{UnityEngine.Object.DestroyImmediate(root);}}
    static Mesh RingMesh()
    {
        const string path="Assets/Art/OniniWorlds/Ring.asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(old)return old;
        const int n=48,k=8;var v=new Vector3[n*k];var t=new int[n*k*6];
        for(int i=0;i<n;i++)for(int j=0;j<k;j++)
        {
            float a=i*Mathf.PI*2/n,b=j*Mathf.PI*2/k,r=1f+.1f*Mathf.Cos(b);int x=i*k+j,y=((i+1)%n)*k+j,z=i*k+(j+1)%k,w=((i+1)%n)*k+(j+1)%k,q=x*6;
            v[x]=new Vector3(Mathf.Cos(a)*r,Mathf.Sin(a)*r,Mathf.Sin(b)*.1f);
            t[q]=x;t[q+1]=y;t[q+2]=z;t[q+3]=z;t[q+4]=y;t[q+5]=w;
        }
        var mesh=new Mesh{name="SculptedRing",vertices=v,triangles=t};mesh.RecalculateNormals();AssetDatabase.CreateAsset(mesh,path);return mesh;
    }
    static void Ring(Transform p,Vector3 at,float scale,Material m,Vector3 rotation=default)
    {
        var o=new GameObject("Sculpted ring",typeof(MeshFilter),typeof(MeshRenderer));o.transform.SetParent(p,false);o.transform.localPosition=at;o.transform.localScale=Vector3.one*scale;o.transform.localRotation=Quaternion.Euler(rotation);o.GetComponent<MeshFilter>().sharedMesh=RingMesh();o.GetComponent<Renderer>().sharedMaterial=m;
    }
    static void Column(Transform p,float x,float height,Material stone,Material gold)
    {
        Shape(p,PrimitiveType.Cylinder,new Vector3(x,height/2,0),new Vector3(.7f,height/2,.7f),stone);
        foreach(float y in new[]{.12f,.35f,height-.35f,height-.12f})Box(p,new Vector3(x,y,0),new Vector3(1f,.18f,1f),gold);
        for(int n=0;n<8;n++){float a=n*Mathf.PI/4;Shape(p,PrimitiveType.Cylinder,new Vector3(x+Mathf.Cos(a)*.36f,height/2,Mathf.Sin(a)*.36f),new Vector3(.06f,height*.42f,.06f),gold);}
    }
    static void Cloud(Transform p,Vector3 at,Material m)
    {
        for(int i=0;i<5;i++)Shape(p,PrimitiveType.Sphere,at+new Vector3((i-2)*.7f,(i%2)*.18f,0),new Vector3(1.6f,.7f,1.4f),m);
    }
    public static string Run()
    {
        if(Application.isPlaying)throw new Exception("Edit Mode required");
        Folder("Assets/Art/OniniWorlds");
        var black=Mat("Basalt",new Color(.08f,.07f,.1f));var red=Mat("InfernalIron",new Color(.4f,.07f,.08f));
        var lava=Mat("Lava",new Color(1.3f,.22f,.06f));var gold=Mat("HeavenGold",new Color(.9f,.69f,.24f));
        var ivory=Mat("Marble",new Color(.92f,.94f,.9f));var cloud=Mat("Cloud",new Color(.76f,.86f,.94f));
        var purple=Mat("PsychoPurple",new Color(.5f,.08f,.7f));var teal=Mat("PsychoTeal",new Color(.08f,.9f,.7f));
        foreach(string id in new[]{"hell","heaven","psychoworlds"})
        {
            Folder("Assets/Resources/Scenery/"+id);
            for(int kind=0;kind<4;kind++)
            {
                var root=new GameObject(id+"_"+kind);var p=root.transform;
                if(id=="hell")
                {
                    if(kind==0){Shape(p,PrimitiveType.Sphere,new Vector3(0,.2f,0),new Vector3(1,.4f,.8f),black);Box(p,new Vector3(0,.25f,0),new Vector3(.75f,.12f,.12f),lava);}
                    else if(kind==1)
                    {
                        foreach(float x in new[]{-.6f,.6f})foreach(float z in new[]{-.6f,.6f})Shape(p,PrimitiveType.Cylinder,new Vector3(x,1.1f,z),new Vector3(.1f,1.1f,.1f),red);
                        foreach(float y in new[]{.15f,2.1f})Box(p,new Vector3(0,y,0),new Vector3(1.5f,.2f,1.5f),black);
                        Ring(p,new Vector3(0,2.5f,0),.35f,red);
                    }
                    else if(kind==2)
                    {
                        foreach(float x in new[]{-3.5f,3.5f})
                        {
                            Box(p,new Vector3(x,3.5f,0),new Vector3(1.6f,7f,1.8f),black);
                            foreach(float y in new[]{.4f,2f,5.8f,7f})Box(p,new Vector3(x,y,0),new Vector3(2f,.25f,2f),red);
                            Shape(p,PrimitiveType.Sphere,new Vector3(x,6.2f,-1),new Vector3(.9f,.9f,.6f),ivory);
                            Box(p,new Vector3(x,4,-.95f),new Vector3(.12f,2.4f,.12f),lava);
                        }
                        Box(p,new Vector3(0,7.5f,0),new Vector3(8.6f,1.1f,1.6f),black);
                        for(int i=-3;i<=3;i++)Box(p,new Vector3(i,3.2f,0),new Vector3(.14f,6.3f,.2f),red);
                        Ring(p,new Vector3(0,7.5f,-1),.6f,lava);
                    }
                    else
                    {
                        var source=Resources.Load<GameObject>("Scenery/volcano/00_spire");var o=UnityEngine.Object.Instantiate(source,p);o.transform.localPosition=Vector3.zero;
                    }
                }
                else if(id=="heaven")
                {
                    if(kind==0){Shape(p,PrimitiveType.Sphere,new Vector3(0,.25f,0),new Vector3(1.5f,.5f,1f),cloud);}
                    else if(kind==1){Column(p,0,2.2f,ivory,gold);Ring(p,new Vector3(0,2.5f,0),.25f,gold);}
                    else if(kind==2)
                    {
                        Column(p,-3.5f,7,ivory,gold);Column(p,3.5f,7,ivory,gold);
                        Ring(p,new Vector3(0,5,0),3.5f,gold);
                        Box(p,new Vector3(0,0,0),new Vector3(10,.25f,5),ivory);
                        for(int i=0;i<3;i++)Box(p,new Vector3(0,-.12f-i*.12f,-2.5f-i*.3f),new Vector3(10+i*.4f,.2f,.6f),gold);
                        Cloud(p,new Vector3(-4,0,1),cloud);Cloud(p,new Vector3(4,0,1),cloud);
                    }
                    else {Cloud(p,new Vector3(0,.5f,0),cloud);Ring(p,new Vector3(0,2,0),1.2f,gold);}
                }
                else
                {
                    if(kind==0)
                    {
                        Shape(p,PrimitiveType.Sphere,new Vector3(0,.4f,0),new Vector3(1.6f,.8f,.7f),ivory);
                        Shape(p,PrimitiveType.Sphere,new Vector3(0,.4f,-.36f),new Vector3(.55f,.55f,.15f),black);
                    }
                    else if(kind==1)
                    {
                        Box(p,new Vector3(0,1.3f,0),new Vector3(1.3f,2.6f,.25f),black);
                        foreach(float x in new[]{-.72f,.72f})Box(p,new Vector3(x,1.3f,0),new Vector3(.15f,2.8f,.4f),teal);
                        Box(p,new Vector3(0,2.65f,0),new Vector3(1.6f,.2f,.4f),purple);
                        Shape(p,PrimitiveType.Sphere,new Vector3(.45f,1.2f,-.2f),Vector3.one*.18f,gold);
                    }
                    else if(kind==2)
                    {
                        Ring(p,new Vector3(0,5,0),4.4f,purple,new Vector3(0,0,25));Ring(p,new Vector3(0,5,0),3.6f,teal,new Vector3(0,35,-15));
                        for(int i=0;i<5;i++)Box(p,new Vector3((i-2)*1.3f,1+i*.35f,0),new Vector3(1.3f,.25f,3),i%2==0?black:ivory);
                    }
                    else
                    {
                        Shape(p,PrimitiveType.Cylinder,new Vector3(0,2,0),new Vector3(.65f,2,.65f),teal);
                        Shape(p,PrimitiveType.Sphere,new Vector3(0,4,0),new Vector3(5,1.2f,5),purple);
                        for(int i=0;i<8;i++){float a=i*Mathf.PI/4;Shape(p,PrimitiveType.Sphere,new Vector3(Mathf.Cos(a)*1.5f,4.4f,Mathf.Sin(a)*1.5f),new Vector3(.5f,.15f,.5f),ivory);}
                    }
                }
                Save(root,"Assets/Resources/Scenery/"+id+"/"+kind+".prefab");
            }
            string sourceId=id=="hell"?"volcano":id=="heaven"?"snow":"alien";
            foreach(string folder in new[]{"Ground","Sky"})
            {
                string target="Assets/Resources/"+folder+"/"+id+".png";
                if(!File.Exists(target))AssetDatabase.CopyAsset("Assets/Resources/"+folder+"/"+sourceId+".png",target);
            }
            var station=new GameObject("Station_"+id);
            UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Models/Station_"+sourceId),station.transform);
            var arch=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Scenery/"+id+"/2.prefab"),station.transform);
            arch.transform.localPosition=new Vector3(0,0,4);arch.transform.localScale=Vector3.one*.7f;
            Save(station,"Assets/Resources/Models/Station_"+id+".prefab");
        }
        foreach(string path in Directory.GetFiles("Assets/Resources/Story","onini_*.png"))
        {
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
            var t=(TextureImporter)AssetImporter.GetAtPath(path);t.textureType=TextureImporterType.Default;t.sRGBTexture=true;t.mipmapEnabled=false;
            t.npotScale=TextureImporterNPOTScale.None;t.wrapMode=TextureWrapMode.Clamp;t.filterMode=FilterMode.Bilinear;t.maxTextureSize=2048;t.textureCompression=TextureImporterCompression.CompressedHQ;t.SaveAndReimport();
        }
        AssetDatabase.SaveAssets();return LocalizationImport.Run()+" Onini: hell, heaven and psychoworlds assets ready.";
    }
}
