using System.Collections.Generic;
using UnityEngine;

namespace Tidebreak
{
    public static class Shape
    {
        static Dictionary<string,Material> materials = new Dictionary<string,Material>();
        public static Material Wood(Color c)
        {string key="Wood"+c;Material m;if(materials.TryGetValue(key,out m)&&m)return m;m=new Material(Resources.Load<Shader>("WeatheredWood"));m.color=c;materials[key]=m;return m;}
        public static Material Metal(Color c)
        {string key="Metal"+c;Material m;if(materials.TryGetValue(key,out m)&&m)return m;m=new Material(Shader.Find("Standard"));m.color=c;m.SetFloat("_Metallic",.35f);m.SetFloat("_Glossiness",.45f);materials[key]=m;return m;}
        public static Material Mat(Color c, bool glow=false)
        {
            string key=c.ToString()+glow;
            Material m;
            if(materials.TryGetValue(key,out m) && m) return m;
            m=new Material(Shader.Find("Standard")); m.color=c;
            m.SetFloat("_Glossiness",.22f);
            if(glow) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", c*1.8f); }
            materials[key]=m; return m;
        }
        static readonly MaterialPropertyBlock lineTint=new MaterialPropertyBlock();
        public static void TintLine(LineRenderer line,Color color)
        {
            // Standard does not consume LineRenderer vertex colors. Apply the
            // tint to this renderer without changing the cached shared material.
            line.startColor=line.endColor=color;
            line.GetPropertyBlock(lineTint);lineTint.SetColor("_Color",color);
            lineTint.SetColor("_EmissionColor",color*1.8f);line.SetPropertyBlock(lineTint);
        }
        public static GameObject Part(string name, PrimitiveType type, Transform parent, Vector3 pos, Vector3 scale, Color color, bool collider=false, bool glow=false)
        {
            var g=GameObject.CreatePrimitive(type); g.name=name; g.transform.SetParent(parent,false);
            g.transform.localPosition=pos; g.transform.localScale=scale;
            g.GetComponent<Renderer>().sharedMaterial=Mat(color,glow);
            if(!collider) {g.GetComponent<Collider>().enabled=false;Object.Destroy(g.GetComponent<Collider>());}
            return g;
        }
        public static GameObject MeshObject(string name, Transform parent, Vector3[] vertices, int[] triangles, Color color)
        {
            var g=new GameObject(name); g.transform.SetParent(parent,false);
            var mesh=new Mesh { name=name };
            // Split faces to preserve the deliberately faceted art style.
            var v=new Vector3[triangles.Length]; var t=new int[v.Length];
            for(int i=0;i<v.Length;i++) { v[i]=vertices[triangles[i]]; t[i]=i; }
            mesh.vertices=v; mesh.triangles=t; mesh.RecalculateNormals(); mesh.RecalculateBounds();
            g.AddComponent<MeshFilter>().sharedMesh=mesh;
            g.AddComponent<RuntimeMeshOwner>().Owned=mesh;
            g.AddComponent<MeshRenderer>().sharedMaterial=Mat(color);
            return g;
        }
        public static GameObject Rock(Transform parent, Vector3 pos, Vector3 scale, Color color, int sides=7)
        {
            var vs=new Vector3[sides*2+2]; vs[vs.Length-2]=Vector3.up; vs[vs.Length-1]=Vector3.down*.45f;
            for(int i=0;i<sides;i++) {
                float a=i*Mathf.PI*2/sides;
                vs[i]=new Vector3(Mathf.Cos(a),.1f,Mathf.Sin(a));
                vs[i+sides]=new Vector3(Mathf.Cos(a+.12f)*.6f,.75f+Mathf.Sin(i*7)*.17f,Mathf.Sin(a+.12f)*.6f);
            }
            var tris=new List<int>();
            for(int i=0;i<sides;i++) { int j=(i+1)%sides;
                tris.AddRange(new[]{i,i+sides,j+sides,i,j+sides,j,i+sides,vs.Length-2,j+sides,j,vs.Length-1,i});
            }
            var g=MeshObject("Basalt",parent,vs,tris.ToArray(),color); g.transform.localPosition=pos; g.transform.localScale=scale; return g;
        }
        public static GameObject Beam(Transform parent, Vector3 a, Vector3 b, float radius, Color color, bool glow=false)
        {
            var g=Part("Beam",PrimitiveType.Cylinder,parent,(a+b)*.5f,new Vector3(radius,(b-a).magnitude*.5f,radius),color,false,glow);
            g.transform.localRotation=Quaternion.FromToRotation(Vector3.up,b-a); return g;
        }
    }

}
