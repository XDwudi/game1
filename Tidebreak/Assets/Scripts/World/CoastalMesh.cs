using System.Collections.Generic;
using UnityEngine;

namespace Tidebreak
{
    // Shared, vertex-coloured meshes keep the island's hand-shaped silhouettes inexpensive.
    public sealed class CoastalMesh
    {
        readonly List<Vector3> v=new List<Vector3>();
        readonly List<Color> c=new List<Color>();
        readonly List<int> t=new List<int>();
        static Material material;
        public void Tri(Vector3 a,Vector3 b,Vector3 d,Color color)
        { int n=v.Count;v.Add(a);v.Add(b);v.Add(d);c.Add(color);c.Add(color);c.Add(color);t.Add(n);t.Add(n+1);t.Add(n+2); }
        public void Quad(Vector3 a,Vector3 b,Vector3 d,Vector3 e,Color color)
        {Tri(a,b,d,color);Tri(a,d,e,color);}
        public GameObject Build(string name,Transform parent,bool collider=false)
        {
            var g=new GameObject(name);g.transform.SetParent(parent,false);
            var mesh=new Mesh{name=name};if(v.Count>65000)mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(v);mesh.SetColors(c);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            g.AddComponent<MeshFilter>().sharedMesh=mesh;
            g.AddComponent<RuntimeMeshOwner>().Owned=mesh;
            if(!material)material=new Material(Resources.Load<Shader>("Coastal"));
            g.AddComponent<MeshRenderer>().sharedMaterial=material;
            if(collider){g.AddComponent<MeshCollider>().sharedMesh=mesh;g.layer=8;}
            return g;
        }
        public void Cone(Vector3 p,float radius,float height,Color color,int sides=7,float top=0)
        {
            for(int i=0;i<sides;i++) {
                float a=i*Mathf.PI*2/sides,b=(i+1)*Mathf.PI*2/sides;
                Vector3 x=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a)),y=new Vector3(Mathf.Cos(b),0,Mathf.Sin(b));
                Color shade=color*(.94f+.09f*Mathf.Sin(i*13));shade.a=1;
                Quad(p+x*radius,p+Vector3.up*height+x*top,p+Vector3.up*height+y*top,p+y*radius,shade);
                Tri(p,p+y*radius,p+x*radius,shade);
            }
        }
        public static Transform Tube(string name,Transform parent,Vector3[] points,float[] radii,Color upper,Color lower,int sides=10)
        {
            var m=new CoastalMesh();
            for(int k=0;k<points.Length-1;k++) {
                Vector3 dir=(points[k+1]-points[k]).normalized;
                Vector3 cross=Vector3.Cross(dir,Mathf.Abs(dir.y)>.95f?Vector3.forward:Vector3.up).normalized;
                Vector3 up=Vector3.Cross(cross,dir).normalized;
                for(int j=0;j<sides;j++) {
                    float a=j*Mathf.PI*2/sides,b=(j+1)*Mathf.PI*2/sides;
                    var va=cross*Mathf.Cos(a)+up*Mathf.Sin(a);var vb=cross*Mathf.Cos(b)+up*Mathf.Sin(b);
                    var color=Color.Lerp(lower,upper,Mathf.Clamp01((Mathf.Sin(a)+.35f)*.8f));
                    m.Quad(points[k]+va*radii[k],points[k+1]+va*radii[k+1],points[k+1]+vb*radii[k+1],points[k]+vb*radii[k],color);
                }
            }
            return m.Build(name,parent).transform;
        }
        public static Transform Ring(Transform parent,Vector3 pos,float radius,float thickness,Color color,Quaternion rotation)
        {
            var p=new Vector3[25];var r=new float[25];
            for(int i=0;i<25;i++){float a=i*Mathf.PI*2/24;p[i]=pos+rotation*new Vector3(Mathf.Cos(a)*radius,Mathf.Sin(a)*radius,0);r[i]=thickness;}
            return Tube("Forged ring",parent,p,r,color,color*.75f,6);
        }
    }
    public class RuntimeMeshOwner : MonoBehaviour
    {
        public Mesh Owned;
        void OnDestroy(){if(Owned)Destroy(Owned);}
    }
}
