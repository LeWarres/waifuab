using UnityEditor;
using UnityEngine;

// This model has flat faces but no useful body-panel UVs. Keep its geometry and project in model space.
public class RicMarFinishImporter:AssetPostprocessor
{
    void OnPostprocessModel(GameObject model)
    {
        if(assetPath!="Assets/Resources/Models/Locomotive.fbx")return;
        foreach(var filter in model.GetComponentsInChildren<MeshFilter>())
        {
            var mesh=filter.sharedMesh;var renderer=filter.GetComponent<Renderer>();if(!mesh||!renderer)continue;
            var points=mesh.vertices;var normals=mesh.normals;var uv=new Vector2[points.Length];var mats=renderer.sharedMaterials;
            for(int sub=0;sub<mesh.subMeshCount;sub++)
            {
                float tile=sub<mats.Length&&mats[sub].name=="LocoGlass"?.8f:1.4f;
                foreach(int i in mesh.GetTriangles(sub))
                {
                    Vector3 p=points[i],n=normals[i];n=new Vector3(Mathf.Abs(n.x),Mathf.Abs(n.y),Mathf.Abs(n.z));
                    uv[i]=(n.y>n.x&&n.y>n.z?new Vector2(p.x,p.z):n.x>n.z?new Vector2(p.z,p.y):new Vector2(p.x,p.y))/tile;
                }
            }
            mesh.uv=uv;
        }
    }
}
