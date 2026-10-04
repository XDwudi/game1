using System.Collections.Generic;
using UnityEngine;

namespace Tidebreak
{
    // Shared-vertex anatomical surfaces: rounded volumes, deliberate ridges and painted colour zones.
    // This layer is cosmetic. Hit hulls remain authored by SpeciesArt.
    public static class CreatureSurfaceArt
    {
        static Material material;
        public static Transform Form(Transform parent,string name,Vector3 at,Vector3 scale,Color ink)
        {
            var g=new GameObject(name);g.transform.SetParent(parent,false);g.transform.localPosition=at;g.transform.localScale=scale;
            bool shell=name.Contains("shell"),bell=name.Contains("bell"),ray=name.Contains("disc"),squid=name.Contains("mantle"),head=name.Contains("head");
            const int rings=18,sides=28;var vertices=new Vector3[(rings+1)*(sides+1)];var colors=new Color[vertices.Length];var triangles=new List<int>();
            for(int r=0;r<=rings;r++)for(int s=0;s<=sides;s++){
                float u=r/(float)rings,angle=s/(float)sides*Mathf.PI*2,z=Mathf.Cos(u*Mathf.PI)*.5f,radial=Mathf.Sin(u*Mathf.PI)*.5f;
                float x=Mathf.Cos(angle)*radial,y=Mathf.Sin(angle)*radial;
                if(shell){y=y<0?y*.35f:y*(.9f+.11f*Mathf.Cos(angle*6));x*=1+.035f*Mathf.Cos(u*24);}
                if(bell){y=y<0?y*.24f:y;float rim=Mathf.Sin(u*Mathf.PI);x*=1+.035f*Mathf.Cos(angle*12)*rim;z*=1+.035f*Mathf.Cos(angle*12)*rim;}
                if(ray){x*=1+.18f*Mathf.Sin(u*Mathf.PI);y+=.055f*Mathf.Sin(u*Mathf.PI);z+=.05f;}
                if(squid){float taper=Mathf.Lerp(.45f,1.15f,u);x*=taper;y*=taper;z-=.05f;}
                if(head){x*=.93f+.1f*u;y+=.025f*Mathf.Sin(u*Mathf.PI);}
                int index=r*(sides+1)+s;vertices[index]=new Vector3(x,y,z);
                Color belly=Color.Lerp(ink,new Color(.95f,.91f,.73f),.59f),back=Color.Lerp(ink,new Color(.045f,.12f,.16f),.21f);
                float dorsal=Mathf.SmoothStep(.08f,.87f,Mathf.Clamp01(y*2+.48f));Color c=Color.Lerp(belly,back,dorsal);
                float stripe=Mathf.Pow(Mathf.Max(0,Mathf.Sin(u*Mathf.PI*8+.5f)),12)*Mathf.Clamp01(y*5+.2f);
                c=Color.Lerp(c,Color.Lerp(ink,Color.black,.35f),stripe*(shell?.32f:.18f));
                c=Color.Lerp(c,new Color(.93f,.87f,.65f),Mathf.Pow(Mathf.Max(0,Mathf.Cos(angle)*Mathf.Sin(u*34)),24)*.16f);c.a=1;colors[index]=c;
                if(r<rings&&s<sides){int a=index,b=a+sides+1;triangles.Add(a);triangles.Add(b);triangles.Add(a+1);triangles.Add(a+1);triangles.Add(b);triangles.Add(b+1);}
            }
            var mesh=new Mesh{name="Sculpted "+name};mesh.vertices=vertices;mesh.colors=colors;mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            g.AddComponent<MeshFilter>().sharedMesh=mesh;g.AddComponent<RuntimeMeshOwner>().Owned=mesh;if(!material)material=new Material(Resources.Load<Shader>("Coastal"));g.AddComponent<MeshRenderer>().sharedMaterial=material;return g.transform;
        }
        public static void Membrane(Transform parent,Vector3 a,Vector3 b,Vector3 c,Color upper,Color lower)
        {
            var m=new CoastalMesh();const int count=7;
            for(int i=0;i<count;i++){
                float u=i/(float)count,v=(i+1)/(float)count;Vector3 p=Vector3.Lerp(b,c,u),q=Vector3.Lerp(b,c,v);
                p.y+=Mathf.Sin(u*Mathf.PI)*.055f;q.y+=Mathf.Sin(v*Mathf.PI)*.055f;
                Color color=Color.Lerp(upper,lower,.25f+u*.4f);m.Tri(a,p,q,color);m.Tri(q,p,a,Color.Lerp(color,lower,.2f));
                CoastalMesh.Tube("Fin ray",parent,new[]{a,Vector3.Lerp(a,p,.63f),p},new[]{.014f,.011f,.004f},lower,upper,5);
            }
            m.Build("Fin membrane",parent);
            CoastalMesh.Tube("Fin leading rim",parent,new[]{a,b,c},new[]{.028f,.017f,.006f},upper,lower,6);
        }
        public static Transform Facet(Transform parent,string name,Vector3 at,Vector3 scale,Color color)
        {
            var mesh=new CoastalMesh();bool shard=name.Contains("shard");const int sides=6;
            for(int i=0;i<sides;i++){
                float a=i*Mathf.PI*2/sides,b=(i+1)*Mathf.PI*2/sides;
                Vector3 x=new Vector3(Mathf.Cos(a)*.5f,shard?-.25f:0,Mathf.Sin(a)*.5f),y=new Vector3(Mathf.Cos(b)*.5f,shard?-.25f:0,Mathf.Sin(b)*.5f);
                mesh.Tri(Vector3.up*.5f,y,x,Color.Lerp(color,Color.white,i%2==0?.16f:.025f));
                mesh.Tri(Vector3.down*.5f,x,y,Color.Lerp(color,Color.black,.18f));
            }
            var g=mesh.Build(name,parent).transform;g.localPosition=at;g.localScale=scale;return g;
        }
        public static void Finish(Transform root,SpeciesDefinition spec,bool elite,Color skin,Color belly)
        {
            if(spec.body==BodyFamily.Turtle){
                for(int k=-1;k<=1;k++){
                    float z=k*.47f,y=.46f-Mathf.Abs(k)*.08f;
                    var points=new[]{new Vector3(-.36f,y,z-.2f),new Vector3(0,y+.06f,z-.3f),new Vector3(.36f,y,z-.2f),new Vector3(.36f,y,z+.17f),new Vector3(0,y+.06f,z+.29f),new Vector3(-.36f,y,z+.17f),new Vector3(-.36f,y,z-.2f)};
                    CoastalMesh.Tube("Shell scute seam",root,points,new[]{.014f,.014f,.014f,.014f,.014f,.014f,.014f},Color.Lerp(skin,Color.black,.5f),skin,5);
                }
            }
            if(spec.body==BodyFamily.Jelly){
                for(int i=0;i<12;i++){float a=i*Mathf.PI/6;CoastalMesh.Tube("Bell radial nerve",root,new[]{new Vector3(0,.84f,0),new Vector3(Mathf.Cos(a)*.42f,.68f,Mathf.Sin(a)*.39f),new Vector3(Mathf.Cos(a)*.7f,.36f,Mathf.Sin(a)*.63f)},new[]{.012f,.017f,.009f},belly,skin,5);}
                CoastalMesh.Ring(root,new Vector3(0,.31f,0),.67f,.032f,belly,Quaternion.Euler(90,0,0));
            }
            if(spec.body==BodyFamily.Squid)for(int side=-1;side<=1;side+=2)
                Membrane(root,new Vector3(side*.2f,0,-1.1f),new Vector3(side*.95f,.05f,-.65f),new Vector3(side*.35f,0,.03f),skin,belly);
            if(spec.body==BodyFamily.Eel)for(int i=0;i<9;i++){
                float z=-1.6f+i*.35f;Membrane(root,new Vector3(0,.16f,z),new Vector3(0,.42f,z+.2f),new Vector3(0,.19f,z+.39f),skin,belly);
            }
            if(elite){
                // The copper crown reads as a threat tier from behind as well as from the weak point.
                for(int side=-1;side<=1;side+=2)for(int i=0;i<3;i++){
                    Vector3 p=new Vector3(side*(.19f+i*.1f),.33f,-.3f+i*.2f);
                    CoastalMesh.Tube("Elite coral crown",root,new[]{p,p+new Vector3(side*.1f,.24f,-.08f),p+new Vector3(side*.21f,.38f-i*.05f,-.13f)},new[]{.07f,.046f,.005f},new Color(.88f,.55f,.22f),new Color(.38f,.17f,.08f),7);
                }
            }
        }
    }
}
