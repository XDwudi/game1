using System.Collections.Generic;
using UnityEngine;

namespace Tidebreak
{
    // Authored sections, rather than stretched primitives, define the animals' anatomy.
    // Cosmetic only: callers own hit hulls. Meshes are created once at spawn and disposed with the rig.
    public static class CreatureSculpt
    {
        static Material surface;
        static Color Ink(Color dorsal,Color ventral,float elevation,float along,float around,int pattern)
        {
            float top=Mathf.SmoothStep(0,1,Mathf.Clamp01(elevation*.8f+.47f));
            Color c=Color.Lerp(ventral,dorsal,top);
            float mark=pattern==1?Mathf.Pow(Mathf.Max(0,Mathf.Sin(along*31+around*2)),12):pattern==2?Mathf.Pow(Mathf.Max(0,Mathf.Sin(around*6+along*4)),20):0;
            c=Color.Lerp(c,Color.Lerp(dorsal,new Color(.045f,.10f,.12f),.48f),mark*top*.4f);
            c.a=1;return c;
        }
        static Transform Make(string name,Transform parent,List<Vector3> vertices,List<int> triangles,List<Color> colors)
        {
            var mesh=new Mesh{name="Sculpt / "+name};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.SetColors(colors);mesh.RecalculateNormals();mesh.RecalculateBounds();
            var g=new GameObject(name);g.transform.SetParent(parent,false);g.AddComponent<MeshFilter>().sharedMesh=mesh;g.AddComponent<RuntimeMeshOwner>().Owned=mesh;
            if(!surface)surface=new Material(Resources.Load<Shader>("Coastal"));g.AddComponent<MeshRenderer>().sharedMaterial=surface;return g.transform;
        }
        static void Quad(List<int> t,int a,int b,int c,int d){t.Add(a);t.Add(b);t.Add(c);t.Add(a);t.Add(c);t.Add(d);}
        public static Transform Sections(Transform parent,string name,Vector3[] centres,Vector2[] radii,Color back,Color belly,int sides=24,int pattern=0)
        {
            var v=new List<Vector3>();var t=new List<int>();var c=new List<Color>();
            for(int k=0;k<centres.Length;k++)for(int j=0;j<=sides;j++){
                float angle=j*Mathf.PI*2/sides;float y=Mathf.Sin(angle),x=Mathf.Cos(angle);
                v.Add(centres[k]+new Vector3(x*radii[k].x,y*radii[k].y,0));c.Add(Ink(back,belly,y,k/(float)(centres.Length-1),angle,pattern));
                if(k<centres.Length-1&&j<sides){int a=k*(sides+1)+j,b=a+sides+1;Quad(t,a,a+1,b+1,b);}
            }
            return Make(name,parent,v,t,c);
        }
        public static Transform Curve(Transform parent,string name,Vector3[] controls,float[] widths,Color back,Color belly,int sides=10,int steps=3,float flatten=1)
        {
            var points=new List<Vector3>();var radii=new List<float>();
            for(int k=0;k<controls.Length-1;k++)for(int j=0;j<steps;j++){
                float u=j/(float)steps;Vector3 a=controls[Mathf.Max(0,k-1)],b=controls[k],c=controls[k+1],d=controls[Mathf.Min(controls.Length-1,k+2)];
                points.Add(.5f*((2*b)+(-a+c)*u+(2*a-5*b+4*c-d)*u*u+(-a+3*b-3*c+d)*u*u*u));radii.Add(Mathf.Lerp(widths[k],widths[k+1],u));
            }
            points.Add(controls[controls.Length-1]);radii.Add(widths[widths.Length-1]);
            var verts=new List<Vector3>();var triangles=new List<int>();var colors=new List<Color>();
            Vector3 previousRight=Vector3.right;
            for(int k=0;k<points.Count;k++){
                var direction=(points[Mathf.Min(k+1,points.Count-1)]-points[Mathf.Max(0,k-1)]).normalized;
                Vector3 right=k==0?Vector3.Cross(Mathf.Abs(direction.y)>.92f?Vector3.forward:Vector3.up,direction):Vector3.ProjectOnPlane(previousRight,direction);
                if(right.sqrMagnitude<.001f)right=Vector3.Cross(Mathf.Abs(direction.y)>.92f?Vector3.forward:Vector3.up,direction);
                right.Normalize();previousRight=right;var up=Vector3.Cross(direction,right);
                for(int j=0;j<=sides;j++){
                    float angle=j*Mathf.PI*2/sides;verts.Add(points[k]+(right*Mathf.Cos(angle)+up*Mathf.Sin(angle)*flatten)*radii[k]);
                    colors.Add(Ink(back,belly,Mathf.Sin(angle),k/(float)(points.Count-1),angle,0));
                    if(k<points.Count-1&&j<sides){int a=k*(sides+1)+j,b=a+sides+1;Quad(triangles,a,a+1,b+1,b);}
                }
            }
            return Make(name,parent,verts,triangles,colors);
        }
        public static Transform Fin(Transform parent,string name,Vector3 root,Vector3[] rim,Color upper,Color lower,float camber=.08f,float thickness=.025f,bool rays=true)
        {
            var verts=new List<Vector3>();var colors=new List<Color>();var triangles=new List<int>();const int span=5;
            Vector3 normal=Vector3.zero;float largest=0;
            for(int k=1;k<rim.Length;k++){var n=Vector3.Cross(rim[k-1]-root,rim[k]-root);if(n.sqrMagnitude>largest){largest=n.sqrMagnitude;normal=n;}}
            if(largest<.000001f)normal=Vector3.up;normal.Normalize();
            float dominant=Mathf.Abs(normal.y)>=Mathf.Abs(normal.x)&&Mathf.Abs(normal.y)>=Mathf.Abs(normal.z)?normal.y:Mathf.Abs(normal.x)>=Mathf.Abs(normal.z)?normal.x:normal.z;
            if(dominant<0)normal=-normal;
            // Two curved skins make the fins visible edge-on. Trailing points provide a scalloped silhouette.
            for(int skin=0;skin<2;skin++)for(int k=0;k<rim.Length;k++)for(int j=0;j<=span;j++){
                float u=j/(float)span;var p=Vector3.Lerp(root,rim[k],u);p+=normal*(Mathf.Sin(u*Mathf.PI)*camber+(skin==0?1:-1)*thickness*Mathf.Sin(u*Mathf.PI*.94f));
                verts.Add(p-root);colors.Add(Color.Lerp(skin==0?upper:lower,lower,u*.58f));
            }
            for(int skin=0;skin<2;skin++)for(int k=0;k<rim.Length-1;k++)for(int j=0;j<span;j++){
                int a=skin*rim.Length*(span+1)+k*(span+1)+j,b=a+span+1;bool forward=Vector3.Dot(Vector3.Cross(verts[b]-verts[a],verts[b+1]-verts[a]),normal)>0;
                if(forward==(skin==0))Quad(triangles,a,b,b+1,a+1);else Quad(triangles,a,a+1,b+1,b);
            }
            var fin=Make(name,parent,verts,triangles,colors);fin.localPosition=root;
            if(rays)for(int k=0;k<rim.Length;k+=2){var end=rim[k]-root;Curve(fin,"Fin bone",new[]{Vector3.zero,end*.55f+normal*(camber+thickness),end},new[]{.018f,.012f,.002f},Color.Lerp(upper,lower,.35f),lower,5,2);}
            var localRim=new Vector3[rim.Length];for(int k=0;k<rim.Length;k++)localRim[k]=rim[k]-root;
            Curve(fin,"Fin trailing edge",localRim,EqualWidths(rim.Length,.011f),upper,lower,5,2);return fin;
        }
        static float[] EqualWidths(int count,float radius){var r=new float[count];for(int i=0;i<count;i++)r[i]=radius;return r;}
        public static Transform Bell(Transform parent,Color skin,Color belly)
        {
            var v=new List<Vector3>();var c=new List<Color>();var t=new List<int>();const int rows=16,segments=48;
            // An actual open bell: outer dome rolls under a scalloped rim into its concave inner surface.
            for(int r=0;r<=rows;r++)for(int s=0;s<=segments;s++){
                float u=r/(float)rows,a=s*Mathf.PI*2/segments;
                float radius=u<.7f?Mathf.Sin(u/.7f*Mathf.PI*.5f)*.74f:Mathf.Lerp(.74f,.025f,(u-.7f)/.3f);
                float y=u<.7f?.83f-Mathf.Pow(u/.7f,1.7f)*.52f:.31f+Mathf.Sin((u-.7f)/.3f*Mathf.PI*.5f)*.24f;
                float frill=Mathf.Pow(Mathf.Sin(Mathf.Min(u/.7f,1)*Mathf.PI*.5f),5);radius*=1+Mathf.Cos(a*12)*.025f*frill;y+=Mathf.Cos(a*12)*.025f*frill;
                v.Add(new Vector3(Mathf.Cos(a)*radius,y,Mathf.Sin(a)*radius*.92f));c.Add(Color.Lerp(belly,skin,u<.7f?.35f+u*.55f:.4f));
                if(r<rows&&s<segments){int k=r*(segments+1)+s,b=k+segments+1;Quad(t,k,k+1,b+1,b);}
            }
            return Make("Sculpted open jelly bell",parent,v,t,c);
        }
        public static void Eye(Transform parent,Vector3 at,float size,Color lid,Color iris,int side=1)
        {
            var socket=CreatureSurfaceArt.Form(parent,"Almond eye socket",at,new Vector3(size*1.65f,size*1.12f,size*.74f),Color.Lerp(lid,Color.black,.64f));
            socket.localRotation=Quaternion.Euler(0,side*18,side*-9);
            CreatureSurfaceArt.Form(parent,"Inset amber iris",at+Vector3.forward*size*.31f,new Vector3(size,size*.92f,size*.37f),iris);
            CreatureSurfaceArt.Form(parent,"Vertical dark pupil",at+Vector3.forward*size*.49f,new Vector3(size*.36f,size*.70f,size*.15f),new Color(.018f,.045f,.049f));
            CreatureSurfaceArt.Form(parent,"Eye glint",at+new Vector3(-size*.16f,size*.19f,size*.55f),Vector3.one*size*.14f,new Color(.93f,.95f,.83f));
            Curve(parent,"Heavy expressive eyelid",new[]{at+new Vector3(-size*.69f,0,size*.17f),at+new Vector3(0,size*.56f,size*.21f),at+new Vector3(size*.65f,size*.08f,size*.10f)},new[]{size*.11f,size*.17f,size*.045f},lid,Color.Lerp(lid,iris,.25f),7,3);
        }
        public static void WeakOrgan(GameObject hitObject)
        {
            // Keep the original spherical hit target and its transform exactly intact.
            // Only the visible surface becomes an inset organ with a cut iris and bone bezel.
            var sphere=hitObject.GetComponent<MeshRenderer>();if(sphere)sphere.enabled=false;
            // The former sphere hid the gap to narrow snouts. Join the flatter organ
            // back into the forehead so it cannot read as a floating target from the side.
            Curve(hitObject.transform,"Weak organ cartilage",new[]{new Vector3(0,-.24f,-1.4f),new Vector3(0,-.08f,-.55f),new Vector3(0,0,.015f)},new[]{.35f,.39f,.29f},new Color(.28f,.40f,.35f),new Color(.51f,.55f,.36f),12,4,.78f);
            var mesh=new CoastalMesh();const int sides=12;
            Color bronze=new Color(.54f,.39f,.18f),edge=new Color(.86f,.72f,.42f),teal=new Color(.19f,.53f,.49f),light=new Color(.48f,.81f,.67f),pupil=new Color(.025f,.095f,.11f);
            for(int i=0;i<sides;i++){
                float a=i*Mathf.PI*2/sides,b=(i+1)*Mathf.PI*2/sides;
                Vector3 outerA=new Vector3(Mathf.Cos(a)*.48f,Mathf.Sin(a)*.35f,.015f),outerB=new Vector3(Mathf.Cos(b)*.48f,Mathf.Sin(b)*.35f,.015f);
                Vector3 lipA=new Vector3(Mathf.Cos(a)*.40f,Mathf.Sin(a)*.285f,.17f),lipB=new Vector3(Mathf.Cos(b)*.40f,Mathf.Sin(b)*.285f,.17f);
                Vector3 innerA=new Vector3(Mathf.Cos(a)*.32f,Mathf.Sin(a)*.224f,.145f),innerB=new Vector3(Mathf.Cos(b)*.32f,Mathf.Sin(b)*.224f,.145f);
                mesh.Quad(outerA,outerB,lipB,lipA,Color.Lerp(bronze,edge,i%3==0?.64f:.24f));mesh.Quad(lipA,lipB,innerB,innerA,edge);
                mesh.Tri(innerA,innerB,new Vector3(0,0,.24f),Color.Lerp(teal,light,i%3==0?.64f:.19f));
            }
            mesh.Build("Faceted weak organ",hitObject.transform);
            var cut=new CoastalMesh();cut.Quad(new Vector3(0,.184f,.251f),new Vector3(-.052f,.021f,.265f),new Vector3(0,-.184f,.251f),new Vector3(.041f,-.015f,.265f),pupil);cut.Build("Recessed iris slit",hitObject.transform);
            Curve(hitObject.transform,"Upper organic eyelid",new[]{new Vector3(-.46f,.01f,.045f),new Vector3(-.25f,.31f,.11f),new Vector3(.13f,.36f,.08f),new Vector3(.46f,.045f,.045f)},new[]{.022f,.043f,.032f,.005f},bronze,edge,7,3);
        }
    }
}
