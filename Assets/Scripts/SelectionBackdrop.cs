using UnityEngine;

// A resolution-independent sky/blueprint composition, drawn as Canvas geometry behind the 3D train.
[ExecuteAlways, RequireComponent(typeof(CanvasRenderer))]
public class SelectionBackdrop : UnityEngine.UI.MaskableGraphic
{
    protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper mesh)
    {
        mesh.Clear();
        if(rectTransform.rect.width<=0 || rectTransform.rect.height<=0) return;
        Color deep=UiKit.Now.deep;
        Quad(mesh,0,0,1,1,deep,Color.Lerp(deep,UiKit.Now.main,.12f));
        Glow(mesh,new Vector2(.26f,.52f),new Vector2(.36f,.64f),Accent(.35f));
        Polygon(mesh,new[]{new Vector2(-.1f,.15f),new Vector2(.16f,-.1f),new Vector2(.70f,1.1f),new Vector2(.39f,1.1f)},Accent(.85f));
        Polygon(mesh,new[]{new Vector2(.58f,0),new Vector2(.94f,0),new Vector2(1.12f,1),new Vector2(.76f,1)},new Color(1,1,1,.12f));
        Polygon(mesh,new[]{new Vector2(.51f,0),new Vector2(.535f,0),new Vector2(.715f,1),new Vector2(.69f,1)},new Color(1,1,1,.85f));
        for(int x=0;x<29;x++) for(int y=0;y<19;y++) {
            float px=.02f+x*.035f,py=.12f+y*.048f;
            Quad(mesh,px,py,px+.0012f,py+.0021f,new Color(1,1,1,.10f),new Color(1,1,1,.10f));
        }
        Ring(mesh,new Vector2(.29f,.52f),new Vector2(.275f,.489f),new Color(1,1,1,.22f),false);
        Ring(mesh,new Vector2(.29f,.52f),new Vector2(.254f,.452f),Accent(.4f),true);
        for(int i=0;i<7;i++) {
            float y=.17f+i*.065f;
            Line(mesh,new Vector2(.005f,y),new Vector2(.13f,y+.12f),new Color(1,1,1,.32f),1.4f);
            Line(mesh,new Vector2(.91f,y+.25f),new Vector2(1.05f,y+.40f),Accent(.35f),1.5f);
        }
        for(int i=0;i<15;i++) {
            float x=.43f+i*.042f, height=.06f+(i*7%5)*.021f;
            Quad(mesh,x,.11f,x+.024f,.11f+height,new Color(1,1,1,.05f),new Color(1,1,1,.01f));
        }
        Glow(mesh,new Vector2(.05f,.93f),new Vector2(.20f,.16f),Accent(.3f));
        Star(mesh,.57f,.70f,15);Star(mesh,.08f,.32f,12);Star(mesh,.94f,.90f,19);
        Quad(mesh,0,0,1,.16f,new Color(deep.r,deep.g,deep.b,.96f),new Color(deep.r,deep.g,deep.b,0));
    }

    static Color Accent(float alpha) { Color c=UiKit.Now.main; c.a=alpha; return c; }

    int seen=-1;
    void Update() { if(seen!=UiKit.ToneVersion) { seen=UiKit.ToneVersion; SetVerticesDirty(); } } // redrawn in the new colours

    void Vertex(UnityEngine.UI.VertexHelper mesh,Vector2 point,Color shade)
    {
        Rect rect=rectTransform.rect;
        mesh.AddVert(new Vector3(rect.xMin+point.x*rect.width,rect.yMin+point.y*rect.height),shade*color,Vector2.zero);
    }
    void Quad(UnityEngine.UI.VertexHelper mesh,float x0,float y0,float x1,float y1,Color bottom,Color top)
    {
        int n=mesh.currentVertCount;
        Vertex(mesh,new Vector2(x0,y0),bottom);Vertex(mesh,new Vector2(x0,y1),top);
        Vertex(mesh,new Vector2(x1,y1),top);Vertex(mesh,new Vector2(x1,y0),bottom);
        mesh.AddTriangle(n,n+1,n+2);mesh.AddTriangle(n,n+2,n+3);
    }
    void Polygon(UnityEngine.UI.VertexHelper mesh,Vector2[] points,Color shade)
    {
        int n=mesh.currentVertCount;foreach(var point in points) Vertex(mesh,point,shade);
        for(int i=1;i<points.Length-1;i++) mesh.AddTriangle(n,n+i,n+i+1);
    }
    void Glow(UnityEngine.UI.VertexHelper mesh,Vector2 center,Vector2 radius,Color shade)
    {
        const int rings=10,segments=64;
        for(int r=0;r<rings;r++) for(int i=0;i<segments;i++) {
            float inner=r/(float)rings,outer=(r+1)/(float)rings;
            Color a=shade,b=shade;a.a*=Mathf.Pow(1-inner,2);b.a*=Mathf.Pow(1-outer,2);
            float start=i*Mathf.PI*2/segments,end=(i+1)*Mathf.PI*2/segments;
            Vector2 u=new Vector2(Mathf.Cos(start)*radius.x,Mathf.Sin(start)*radius.y);
            Vector2 v=new Vector2(Mathf.Cos(end)*radius.x,Mathf.Sin(end)*radius.y);
            int n=mesh.currentVertCount;
            Vertex(mesh,center+u*inner,a);Vertex(mesh,center+u*outer,b);
            Vertex(mesh,center+v*outer,b);Vertex(mesh,center+v*inner,a);
            mesh.AddTriangle(n,n+1,n+2);mesh.AddTriangle(n,n+2,n+3);
        }
    }
    void Ring(UnityEngine.UI.VertexHelper mesh,Vector2 center,Vector2 radius,Color shade,bool dashed)
    {
        const int segments=100;
        for(int i=0;i<segments;i++) {
            if(dashed && i%5==0) continue;
            float a=i*Mathf.PI*2/segments,b=(i+1)*Mathf.PI*2/segments;
            Line(mesh,center+new Vector2(Mathf.Cos(a)*radius.x,Mathf.Sin(a)*radius.y),center+new Vector2(Mathf.Cos(b)*radius.x,Mathf.Sin(b)*radius.y),shade,dashed ? 1:2);
        }
    }
    void Line(UnityEngine.UI.VertexHelper mesh,Vector2 a,Vector2 b,Color shade,float width)
    {
        Vector2 size=rectTransform.rect.size;
        Vector2 direction=Vector2.Scale(b-a,size).normalized;
        Vector2 normal=new Vector2(-direction.y/size.x,direction.x/size.y)*width*.5f;
        Polygon(mesh,new[]{a-normal,a+normal,b+normal,b-normal},shade);
    }
    void Star(UnityEngine.UI.VertexHelper mesh,float x,float y,float size)
    {
        Vector2 radius=new Vector2(size/rectTransform.rect.width,size/rectTransform.rect.height);
        Polygon(mesh,new[]{new Vector2(x-radius.x,y),new Vector2(x,y+radius.y),new Vector2(x+radius.x,y),new Vector2(x,y-radius.y)},new Color(1,1,1,.85f));
    }
}
