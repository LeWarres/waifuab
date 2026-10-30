using UnityEngine;

public partial class EyeBossEvent
{
    bool Celestial => train && train.BiomeId=="heaven";
    void BuildHeavenGuardian()
    {
        var gold=TrainSim.Mat(new Color(.95f,.73f,.25f));
        var ivory=TrainSim.Mat(new Color(.94f,.94f,.87f));
        for(int ring=0;ring<3;ring++)
        {
            var root=new GameObject("OphanimRing").transform;root.SetParent(boss,false);
            root.localRotation=Quaternion.Euler(ring==1?90:0,ring==2?90:0,ring*28);
            const int segments=64,sides=8;var vertices=new Vector3[segments*sides];var triangles=new int[segments*sides*6];
            for(int s=0;s<segments;s++)for(int k=0;k<sides;k++)
            {
                float a=s*Mathf.PI*2/segments,b=k*Mathf.PI*2/sides,r=6.35f+.24f*Mathf.Cos(b);
                int v=s*sides+k;vertices[v]=new Vector3(Mathf.Cos(a)*r,Mathf.Sin(a)*r,Mathf.Sin(b)*.24f);
                int n=((s+1)%segments)*sides+k,q=s*sides+(k+1)%sides,z=((s+1)%segments)*sides+(k+1)%sides,t=v*6;
                triangles[t]=v;triangles[t+1]=n;triangles[t+2]=q;triangles[t+3]=q;triangles[t+4]=n;triangles[t+5]=z;
            }
            var mesh=new Mesh{name="OphanimGoldRing",vertices=vertices,triangles=triangles};mesh.RecalculateNormals();
            root.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;root.gameObject.AddComponent<MeshRenderer>().sharedMaterial=gold;
        }
        foreach(int side in new[]{-1,1})for(int row=0;row<2;row++)for(int n=0;n<9;n++)
        {
            var feather=GameObject.CreatePrimitive(PrimitiveType.Capsule);Destroy(feather.GetComponent<Collider>());
            feather.name="IvoryFeather";feather.transform.SetParent(boss,false);
            feather.transform.localPosition=new Vector3(side*(5.3f+n*.7f),2f-n*.42f+row*.8f,row*.7f);
            feather.transform.localRotation=Quaternion.Euler(0,0,side*(38+n*4));
            feather.transform.localScale=new Vector3(.7f,3.4f-n*.15f,.3f);
            feather.GetComponent<Renderer>().sharedMaterial=ivory;
        }
    }
}
