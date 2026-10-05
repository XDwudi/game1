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
            bool detail=name.Contains("eye")||name.Contains("Eye")||name.Contains("iris")||name.Contains("pupil")||name.Contains("glint")||name.Contains("sucker")||name.Contains("Blowhole");
            int rings=detail?8:18,sides=detail?16:28;var vertices=new Vector3[(rings+1)*(sides+1)];var colors=new Color[vertices.Length];var triangles=new List<int>();
            for(int r=0;r<=rings;r++)for(int s=0;s<=sides;s++){
                float u=r/(float)rings,angle=s/(float)sides*Mathf.PI*2,z=Mathf.Cos(u*Mathf.PI)*.5f,radial=Mathf.Sin(u*Mathf.PI)*.5f;
                float x=Mathf.Cos(angle)*radial,y=Mathf.Sin(angle)*radial;
                if(shell){y=y<0?y*.35f:y*(.9f+.11f*Mathf.Cos(angle*6));x*=1+.035f*Mathf.Cos(u*24);}
                if(bell){y=y<0?y*.24f:y;float rim=Mathf.Sin(u*Mathf.PI);x*=1+.035f*Mathf.Cos(angle*12)*rim;z*=1+.035f*Mathf.Cos(angle*12)*rim;}
                if(ray){x*=1+.18f*Mathf.Sin(u*Mathf.PI);y+=.055f*Mathf.Sin(u*Mathf.PI);z+=.05f;}
                if(squid){float taper=Mathf.Lerp(.45f,1.15f,u);x*=taper;y*=taper;z-=.05f;}
                if(head){x*=.93f+.1f*u;y+=.025f*Mathf.Sin(u*Mathf.PI);}
                if(name.Contains("socket")){y*=.79f+.21f*Mathf.Sin(u*Mathf.PI);x*=1+.08f*Mathf.Sin(angle*2);}
                int index=r*(sides+1)+s;vertices[index]=new Vector3(x,y,z);
                Color belly=Color.Lerp(ink,new Color(.95f,.91f,.73f),.59f),back=Color.Lerp(ink,new Color(.045f,.12f,.16f),.21f);
                float dorsal=Mathf.SmoothStep(.08f,.87f,Mathf.Clamp01(y*2+.48f));Color c=Color.Lerp(belly,back,dorsal);
                float stripe=Mathf.Pow(Mathf.Max(0,Mathf.Sin(u*Mathf.PI*8+.5f)),12)*Mathf.Clamp01(y*5+.2f);
                c=Color.Lerp(c,Color.Lerp(ink,Color.black,.35f),stripe*(shell?.32f:.18f));
                c=Color.Lerp(c,new Color(.93f,.87f,.65f),Mathf.Pow(Mathf.Max(0,Mathf.Cos(angle)*Mathf.Sin(u*34)),24)*.16f);
                if(detail)c=ink*(.92f+.08f*Mathf.Sin(u*Mathf.PI));c.a=1;colors[index]=c;
                if(r<rings&&s<sides){int a=index,b=a+sides+1;triangles.Add(a);triangles.Add(b);triangles.Add(a+1);triangles.Add(a+1);triangles.Add(b);triangles.Add(b+1);}
            }
            var mesh=new Mesh{name="Sculpted "+name};mesh.vertices=vertices;mesh.colors=colors;mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            g.AddComponent<MeshFilter>().sharedMesh=mesh;g.AddComponent<RuntimeMeshOwner>().Owned=mesh;if(!material)material=new Material(Resources.Load<Shader>("Coastal"));g.AddComponent<MeshRenderer>().sharedMaterial=material;return g.transform;
        }
        public static void Membrane(Transform parent,Vector3 a,Vector3 b,Vector3 c,Color upper,Color lower)
        {
            CreatureSculpt.Fin(parent,"Fin membrane",a,new[]{a,Vector3.Lerp(a,b,.57f),b,Vector3.Lerp(b,c,.54f),c},upper,lower,.04f,.018f);
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
        // Cosmetic attachment points follow the dorsal cross sections of each anatomy.
        // Lateral is a fraction of that section's width; the small inset buries the root seam.
        public static Vector3 CrestAnchor(BodyFamily family,float lateral,float row)
        {
            float z=Mathf.Lerp(-.3f,.1f,row),width=Mathf.Lerp(.436f,.45f,row),height=Mathf.Lerp(.528f,.545f,row),lift=0,centre=0;
            switch(family){
                case BodyFamily.Puffer:width=Mathf.Lerp(.692f,.728f,row);height=Mathf.Lerp(.64f,.668f,row);break;
                case BodyFamily.Shark:width=Mathf.Lerp(.43f,.462f,row);height=Mathf.Lerp(.324f,.331f,row);break;
                case BodyFamily.Eel:
                    z=Mathf.Lerp(-.34f,.28f,row);float u=(z+2)/3.8f;
                    centre=Mathf.Sin(u*5)*.16f;lift=Mathf.Sin(u*3)*.08f;
                    width=Mathf.Sin(u*Mathf.PI*.86f)*.245f+.017f;height=width*1.03f;break;
                case BodyFamily.Ray:z=Mathf.Lerp(-.28f,.12f,row);width=Mathf.Lerp(.49f,.54f,row);height=Mathf.Lerp(.14f,.178f,row);break;
                case BodyFamily.Crab:z=Mathf.Lerp(-.26f,.1f,row);width=Mathf.Lerp(.61f,.68f,row);height=Mathf.Lerp(.172f,.18f,row);lift=.089f;break;
                case BodyFamily.Squid:z=Mathf.Lerp(-.8f,-.26f,row);width=Mathf.Lerp(.39f,.43f,row);height=Mathf.Lerp(.47f,.46f,row);break;
                case BodyFamily.Seahorse:z=Mathf.Lerp(.08f,.3f,row);width=Mathf.Lerp(.15f,.242f,row);height=Mathf.Lerp(.174f,.228f,row);lift=Mathf.Lerp(.85f,.843f,row);break;
                case BodyFamily.Jelly:
                    z=Mathf.Lerp(-.2f,.18f,row);float x=lateral*.66f;
                    float radius=Mathf.Clamp01(Mathf.Sqrt(x*x/(.74f*.74f)+z*z/(.681f*.681f)));
                    float dome=Mathf.Asin(radius)/(Mathf.PI*.5f);
                    return new Vector3(x,.83f-.52f*Mathf.Pow(dome,1.7f)-.024f,z);
                case BodyFamily.Turtle:case BodyFamily.Urchin:
                    float shellRadius=family==BodyFamily.Turtle?1:.55f;
                    float section=Mathf.Sqrt(1-z*z/(shellRadius*shellRadius));
                    width=(family==BodyFamily.Turtle?.9f:.55f)*section;
                    height=(family==BodyFamily.Turtle?.475f:.55f)*section*(.9f+.11f*Mathf.Cos(Mathf.Acos(lateral)*6));break;
            }
            return new Vector3(centre+lateral*width,lift+height*Mathf.Sqrt(1-lateral*lateral)-.025f,z);
        }
        public static float CrestScale(BodyFamily family)
        {
            switch(family){
                case BodyFamily.Seahorse:return .55f;
                case BodyFamily.Eel:case BodyFamily.Ray:return .68f;
                case BodyFamily.Crab:case BodyFamily.Jelly:return .75f;
                case BodyFamily.Squid:case BodyFamily.Shark:return .85f;
                default:return 1;
            }
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
                float scale=CrestScale(spec.body);
                for(int side=-1;side<=1;side+=2)for(int i=0;i<3;i++){
                    Vector3 p=CrestAnchor(spec.body,side*(.35f+i*.1f),i*.5f);
                    CoastalMesh.Tube("Elite coral crown",root,new[]{p,p+new Vector3(side*.1f,.24f,-.08f)*scale,p+new Vector3(side*.21f,.38f-i*.05f,-.13f)*scale},new[]{.07f*scale,.046f*scale,.005f},new Color(.88f,.55f,.22f),new Color(.38f,.17f,.08f),7);
                }
            }
        }
    }
}
