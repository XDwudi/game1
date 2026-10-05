using System.Collections.Generic;
using UnityEngine;

namespace Tidebreak
{
    // Cross-section modelling: continuous clothes, articulated sleeves and a carved face.
    // These are render-only meshes; merchant courts and interaction distances are unchanged.
    public static class SailorModel
    {
        static readonly Color Navy=new Color(.075f,.16f,.2f),Leather=new Color(.27f,.14f,.085f),Paper=new Color(.85f,.78f,.6f),Brass=new Color(.7f,.47f,.19f);
        static Material pigment;
        static Transform Joint(string name,Transform parent,Vector3 at)
        {var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=at;return t;}

        // Each section is y / half width / half depth / fore-aft offset.
        static Transform Loft(string name,Transform parent,Vector3 at,Color color,Vector4[] sections,int sides=16)
        {
            var vertices=new List<Vector3>();var colors=new List<Color>();var tris=new List<int>();
            for(int r=0;r<sections.Length;r++)for(int i=0;i<sides;i++){
                float a=i*Mathf.PI*2/sides;Vector4 s=sections[r];
                vertices.Add(new Vector3(Mathf.Sin(a)*s.y,s.x,Mathf.Cos(a)*s.z+s.w));
                Color tint=color*Mathf.Lerp(.82f,1.05f,Mathf.Clamp01(.55f+Mathf.Cos(a)*.22f+(float)r/sections.Length*.22f));tint.a=1;colors.Add(tint);
            }
            for(int r=0;r<sections.Length-1;r++)for(int i=0;i<sides;i++){
                int a=r*sides+i,b=(r+1)*sides+i,c=(r+1)*sides+(i+1)%sides,d=r*sides+(i+1)%sides;
                tris.Add(a);tris.Add(d);tris.Add(c);tris.Add(a);tris.Add(c);tris.Add(b);
            }
            for(int i=1;i<sides-1;i++){tris.Add(0);tris.Add(i+1);tris.Add(i);int end=(sections.Length-1)*sides;tris.Add(end);tris.Add(end+i);tris.Add(end+i+1);}
            var mesh=new Mesh{name=name};mesh.SetVertices(vertices);mesh.SetColors(colors);mesh.SetTriangles(tris,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            var part=Joint(name,parent,at);part.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
            part.gameObject.AddComponent<RuntimeMeshOwner>().Owned=mesh;
            if(!pigment)pigment=new Material(Resources.Load<Shader>("Coastal"));
            part.gameObject.AddComponent<MeshRenderer>().sharedMaterial=pigment;return part;
        }
        static Vector4 S(float y,float w,float d,float z=0){return new Vector4(y,w,d,z);}
        static Transform Seam(string name,Transform parent,Vector3[] p,float width,Color color)
        {var r=new float[p.Length];for(int i=0;i<r.Length;i++)r[i]=width;return CoastalMesh.Tube(name,parent,p,r,color,color*.82f,6);}
        static Transform Panel(string name,Transform parent,Color color,params Vector3[] points)
        {
            var mesh=new CoastalMesh();for(int i=1;i<points.Length-1;i++){mesh.Tri(points[0],points[i],points[i+1],color);mesh.Tri(points[0]+Vector3.back*.015f,points[i+1]+Vector3.back*.015f,points[i]+Vector3.back*.015f,color*.8f);}
            for(int i=0;i<points.Length;i++)mesh.Quad(points[i],points[(i+1)%points.Length],points[(i+1)%points.Length]+Vector3.back*.015f,points[i]+Vector3.back*.015f,color*.7f);
            return mesh.Build(name,parent).transform;
        }
        static void Stitch(Transform parent,Vector3 a,Vector3 b,int count,Color color)
        {for(int i=0;i<count;i++){var p=Vector3.Lerp(a,b,(i+.5f)/count);Seam("Tailoring stitch",parent,new[]{p+Vector3.left*.006f,p+Vector3.right*.007f},.0025f,color);}}
        static Transform Disk(string name,Transform parent,Vector3 position,float radius,Color color)
        {
            var d=Loft(name,parent,position,color,new[]{S(-.008f,radius,.92f*radius),S(.008f,radius,.92f*radius)},12);d.localRotation=Quaternion.Euler(90,0,0);return d;
        }
        public static void FirstPersonHand(Transform hand,Color sleeve,Color skin,bool left)
        {
            Loft("Contoured waterproof sleeve",hand,Vector3.zero,sleeve,new[]{S(-.46f,.083f,.082f,-.13f),S(-.34f,.084f,.077f,-.095f),S(-.2f,.073f,.069f,-.051f),S(-.075f,.062f,.059f,-.017f),S(-.01f,.06f,.052f)},14);
            Loft("Folded cuff binding",hand,Vector3.zero,Leather,new[]{S(-.045f,.063f,.06f),S(-.025f,.068f,.062f),S(.001f,.062f,.057f)},14);
            Loft("Sculpted fingerless glove palm",hand,Vector3.zero,Color.Lerp(sleeve,Navy,.7f),new[]{S(-.01f,.043f,.033f),S(.012f,.046f,.038f),S(.042f,.054f,.04f),S(.084f,.058f,.036f,.007f),S(.107f,.053f,.028f,.012f)},16);
            for(int f=0;f<4;f++){
                float x=(f-1.5f)*.027f;float length=f==0?.92f:f==3?.81f:1;
                var points=new[]{new Vector3(x,.094f,.01f),new Vector3(x,.129f*length,.035f),new Vector3(x,.121f*length,.066f),new Vector3(x,.082f,.082f),new Vector3(x,.054f,.062f)};
                CreatureSculpt.Curve(hand,"Curved finger phalanges",points,new[]{.016f,.016f,.015f,.013f,.008f},skin,skin*.83f,10,4);
                Seam("Knuckle glove stitching",hand,new[]{new Vector3(x-.007f,.082f,-.023f),new Vector3(x,.09f,-.027f),new Vector3(x+.007f,.082f,-.023f)},.0018f,Paper*.74f);
            }
            float side=left?-1:1;
            CreatureSculpt.Curve(hand,"Opposed thumb",new[]{new Vector3(side*.041f,.014f,.016f),new Vector3(side*.065f,.037f,.046f),new Vector3(side*.053f,.059f,.07f),new Vector3(side*.025f,.064f,.073f)},new[]{.023f,.023f,.017f,.009f},skin,skin*.8f,12,4);
            Seam("Palm reinforcing seam",hand,new[]{new Vector3(-.038f,.01f,-.033f),new Vector3(-.044f,.052f,-.037f),new Vector3(-.03f,.076f,-.031f)},.0022f,Paper*.62f);
        }
        static void CollectRigid(Transform current,Transform owner,HashSet<Transform> joints,List<MeshFilter> pieces)
        {
            if(current!=owner&&joints.Contains(current))return;
            var mesh=current.GetComponent<MeshFilter>();if(mesh&&mesh.sharedMesh)pieces.Add(mesh);
            for(int i=0;i<current.childCount;i++)CollectRigid(current.GetChild(i),owner,joints,pieces);
        }
        static void Consolidate(Transform root,IslandActorMotion motion)
        {
            // Seams share a vertex-colour shader. Merge only within rigid bones so
            // tailoring adds shape without hundreds of renderers or frozen facial motion.
            var joints=new HashSet<Transform>{root,motion.Chest,motion.Head,motion.LeftArm,motion.RightArm,motion.LeftForearm,motion.RightForearm,motion.Book,motion.Mouth};
            foreach(var eye in motion.Eyes)joints.Add(eye);
            foreach(var joint in joints){
                if(!joint)continue;var pieces=new List<MeshFilter>();CollectRigid(joint,joint,joints,pieces);if(pieces.Count<2)continue;
                var combine=new CombineInstance[pieces.Count];int count=0;
                for(int i=0;i<pieces.Count;i++){combine[i]=new CombineInstance{mesh=pieces[i].sharedMesh,transform=joint.worldToLocalMatrix*pieces[i].transform.localToWorldMatrix};count+=pieces[i].sharedMesh.vertexCount;}
                var mesh=new Mesh{name="Tailored / "+joint.name};if(count>65000)mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;mesh.CombineMeshes(combine,true,true);mesh.RecalculateBounds();
                var part=Joint("Sailor rigid mesh",joint,Vector3.zero);part.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;part.gameObject.AddComponent<MeshRenderer>().sharedMaterial=pigment;part.gameObject.AddComponent<RuntimeMeshOwner>().Owned=mesh;
                foreach(var piece in pieces){var renderer=piece.GetComponent<MeshRenderer>();renderer.enabled=false;Object.Destroy(renderer);Object.Destroy(piece.GetComponent<RuntimeMeshOwner>());Object.Destroy(piece);}
            }
        }
        public static IslandActorMotion Build(Transform root,int island,Color accent,int role)
        {
            // role 0 guide, 1 outfitter, 2 fishmonger. Nine guides vary dress and equipment.
            var motion=root.gameObject.AddComponent<IslandActorMotion>();
            Color coat=role==1?new Color(.18f,.28f,.3f):role==2?new Color(.36f,.39f,.27f):Color.Lerp(accent,Navy,.48f);
            Color skin=Color.Lerp(new Color(.68f,.43f,.28f),new Color(.86f,.63f,.43f),(island%3)*.23f);
            Color hair=role==2?new Color(.58f,.58f,.49f):island==5?new Color(.71f,.63f,.46f):new Color(.16f,.095f,.06f);
            for(int side=-1;side<=1;side+=2){
                var leg=Joint("Tailored trouser leg",root,new Vector3(side*.155f,0,0));
                Loft("Trouser knee and calf",leg,Vector3.zero,Navy,new[]{S(.19f,.08f,.08f),S(.31f,.088f,.086f),S(.48f,.096f,.105f,-.012f),S(.57f,.108f,.102f,.016f),S(.77f,.123f,.135f),S(.89f,.125f,.13f)},12);
                Loft("Sculpted sea boot",leg,Vector3.zero,Leather,new[]{S(.025f,.105f,.19f,.068f),S(.072f,.113f,.19f,.068f),S(.14f,.108f,.17f,.062f),S(.2f,.09f,.097f),S(.34f,.09f,.09f)},14);
                Loft("Welted boot sole",leg,Vector3.zero,Navy,new[]{S(.018f,.112f,.193f,.07f),S(.05f,.117f,.193f,.07f)},14);
                Loft("Boot folded cuff",leg,Vector3.zero,Leather*.8f,new[]{S(.31f,.098f,.097f),S(.35f,.1f,.097f)},12);
                Seam("Boot toe seam",leg,new[]{new Vector3(-.085f,.15f,.17f),new Vector3(0,.166f,.19f),new Vector3(.085f,.15f,.17f)},.006f,Brass*.58f);
            }
            var torso=Joint("Tailored coat breathing joint",root,new Vector3(0,1.1f,0));motion.Chest=torso;
            Loft("Cutaway oilskin coat",torso,Vector3.zero,coat,new[]{S(-.42f,.29f,.18f),S(-.32f,.3f,.19f),S(-.18f,.25f,.16f),S(.01f,.265f,.174f),S(.19f,.29f,.182f),S(.32f,.33f,.16f),S(.41f,.19f,.115f)},18);
            Panel("Cream ribbed undershirt",torso,Paper,new Vector3(-.11f,.38f,.15f),new Vector3(.11f,.38f,.15f),new Vector3(.12f,.04f,.179f),new Vector3(-.12f,.04f,.179f));
            for(int i=0;i<6;i++)Seam("Shirt woven rib",torso,new[]{new Vector3(-.1f,.08f+i*.041f,.185f),new Vector3(.1f,.08f+i*.041f,.185f)},.003f,Paper*.74f);
            for(int s=-1;s<=1;s+=2){
                Panel("Shaped coat lapel",torso,Color.Lerp(coat,Paper,.25f),new Vector3(s*.1f,.415f,.155f),new Vector3(s*.265f,.32f,.16f),new Vector3(s*.12f,.06f,.202f),new Vector3(s*.035f,.23f,.19f));
                Panel("Bellows coat pocket",torso,coat*.83f,new Vector3(s*.055f,-.22f,.185f),new Vector3(s*.22f,-.22f,.15f),new Vector3(s*.225f,-.055f,.177f),new Vector3(s*.065f,-.055f,.201f));
                Seam("Pocket folded flap",torso,new[]{new Vector3(s*.061f,-.052f,.205f),new Vector3(s*.14f,-.085f,.201f),new Vector3(s*.225f,-.052f,.181f)},.007f,Paper*.65f);
                Stitch(torso,new Vector3(s*.24f,-.34f,.155f),new Vector3(s*.27f,.17f,.17f),12,Paper*.48f);
            }
            Loft("Worn leather waist belt",torso,Vector3.zero,Leather,new[]{S(-.14f,.265f,.187f),S(-.085f,.267f,.188f)},18);
            Panel("Cast brass belt buckle",torso,Brass,new Vector3(-.04f,-.146f,.198f),new Vector3(.044f,-.146f,.198f),new Vector3(.044f,-.08f,.2f),new Vector3(-.04f,-.08f,.2f));
            for(int i=0;i<3;i++)Disk("Sewn brass button",torso,new Vector3(.037f,.015f+i*.072f,.193f),.014f,Brass);
            Seam("Cross-body bag strap",torso,new[]{new Vector3(-.25f,.33f,.125f),new Vector3(-.12f,.15f,.207f),new Vector3(.045f,-.04f,.214f),new Vector3(.245f,-.31f,.151f)},.029f,Leather);
            Loft("Gusseted map satchel",torso,new Vector3(.32f,-.33f,0),Leather,new[]{S(0,.12f,.06f),S(.04f,.145f,.075f),S(.22f,.135f,.072f),S(.25f,.11f,.065f)},12);
            if(role>0){Panel("Cut leather work apron",torso,role==1?new Color(.47f,.25f,.12f):new Color(.63f,.57f,.36f),new Vector3(-.16f,.29f,.205f),new Vector3(.16f,.29f,.205f),new Vector3(.245f,-.46f,.19f),new Vector3(0,-.51f,.207f),new Vector3(-.245f,-.46f,.19f));}
            Loft("Neck",root,new Vector3(0,1.49f,0),skin,new[]{S(0,.087f,.073f),S(.16f,.083f,.07f)},12);
            var head=Joint("Carved head joint",root,new Vector3(0,1.65f,0));motion.Head=head;
            Loft("Cheekbones jaw and brow",head,Vector3.zero,skin,new[]{S(-.055f,.08f,.06f,.025f),S(-.027f,.12f,.094f,.02f),S(.045f,.16f,.12f),S(.145f,.193f,.154f),S(.21f,.188f,.154f),S(.295f,.17f,.14f,-.014f),S(.35f,.113f,.093f,-.023f),S(.372f,.008f,.008f,-.03f)},20);
            Loft("Sculpted hair back",head,new Vector3(0,0,-.062f),hair,new[]{S(.04f,.135f,.105f,-.014f),S(.15f,.187f,.141f),S(.27f,.181f,.151f),S(.36f,.11f,.106f),S(.384f,.005f,.005f)},18);
            // The cheek ridge and angular nose are mesh surfaces, not stuck-on spheres.
            Panel("Carved nose bridge",head,skin*.95f,new Vector3(-.033f,.206f,.15f),new Vector3(.033f,.206f,.15f),new Vector3(.047f,.081f,.209f),new Vector3(0,.071f,.233f),new Vector3(-.047f,.081f,.209f));
            for(int s=-1;s<=1;s+=2){
                Loft("Shaped ear",head,new Vector3(s*.185f,.094f,-.016f),skin,new[]{S(0,.016f,.025f),S(.037f,.035f,.031f),S(.093f,.029f,.022f),S(.107f,.008f,.012f)},10);
                Seam("Raised eyelid",head,new[]{new Vector3(s*.038f,.171f,.149f),new Vector3(s*.084f,.189f,.155f),new Vector3(s*.13f,.177f,.122f)},.012f,skin*.78f);
                var eye=Disk("Almond eye",head,new Vector3(s*.083f,.17f,.155f),.025f,Paper);eye.localScale=new Vector3(1,1,.55f);
                var iris=Disk("Ink iris",head,new Vector3(s*.083f,.17f,.168f),.012f,Navy);iris.localScale=new Vector3(1,1,.78f);
                if(motion.Eyes==null)motion.Eyes=new Transform[4];motion.Eyes[(s+1)/2]=eye;motion.Eyes[2+(s+1)/2]=iris;
                Seam("Swept eyebrow",head,new[]{new Vector3(s*.038f,.225f,.15f),new Vector3(s*.085f,.229f,.157f),new Vector3(s*.135f,.208f,.122f)},.012f,hair);
                Seam("Cheek plane",head,new[]{new Vector3(s*.105f,.092f,.135f),new Vector3(s*.14f,.122f,.132f)},.011f,Color.Lerp(skin,new Color(.7f,.31f,.22f),.22f));
            }
            motion.Mouth=Seam("Quiet mouth",head,new[]{new Vector3(-.045f,.034f,.136f),new Vector3(0,.026f,.146f),new Vector3(.045f,.034f,.136f)},.007f,new Color(.3f,.105f,.07f));
            motion.Mouth.localPosition=Vector3.forward*.024f;
            if(role>0||island==0||island==4){
                Loft("Shaped salt beard",head,new Vector3(0,-.025f,.052f),hair,new[]{S(-.08f,.045f,.04f,.026f),S(-.045f,.097f,.088f),S(.04f,.136f,.098f),S(.09f,.127f,.085f,-.012f)},16);
                for(int s=-1;s<=1;s+=2)Seam("Swept moustache",head,new[]{new Vector3(0,.067f,.197f),new Vector3(s*.035f,.06f,.192f),new Vector3(s*.08f,.043f,.157f)},.014f,hair);
            }
            Color hat=role==2?new Color(.66f,.53f,.28f):island==5?Paper:coat;
            Loft("Seamed cap crown",head,Vector3.zero,hat,new[]{S(.28f,.187f,.162f,-.018f),S(.34f,.207f,.17f,-.018f),S(.42f,.177f,.146f,-.024f),S(.46f,.07f,.07f,-.022f)},18);
            Loft("Cap leather band",head,Vector3.zero,Leather,new[]{S(.285f,.19f,.164f,-.018f),S(.321f,.198f,.165f,-.018f)},18);
            // Swept crescent visor with a real thickness and tapered rim.
            var visor=new CoastalMesh();for(int i=0;i<16;i++){
                float a=-1.48f+i*2.96f/16,b=-1.48f+(i+1)*2.96f/16;
                var p0=new Vector3(Mathf.Sin(a)*.19f,.3f,Mathf.Cos(a)*.16f-.018f);var p1=new Vector3(Mathf.Sin(b)*.19f,.3f,Mathf.Cos(b)*.16f-.018f);
                var p2=new Vector3(Mathf.Sin(b)*.255f,.277f,Mathf.Cos(b)*.29f-.018f);var p3=new Vector3(Mathf.Sin(a)*.255f,.277f,Mathf.Cos(a)*.29f-.018f);
                visor.Quad(p0,p1,p2,p3,hat*.75f);visor.Quad(p3,p2,p2+Vector3.down*.012f,p3+Vector3.down*.012f,Brass*.67f);
            }visor.Build("Swept cap visor",head);
            Disk("Mariner cap badge",head,new Vector3(0,.36f,.16f),.036f,Brass);
            if(island==6||role==1)for(int s=-1;s<=1;s+=2){CoastalMesh.Ring(head,new Vector3(s*.09f,.365f,.174f),.054f,.011f,Brass,Quaternion.identity);Disk("Smoked work-goggle lens",head,new Vector3(s*.09f,.365f,.18f),.044f,new Color(.16f,.39f,.42f));}
            for(int side=-1;side<=1;side+=2){
                var shoulder=Joint(side<0?"Left shoulder":"Right shoulder",root,new Vector3(side*.3f,1.43f,0));
                Loft("Shoulder sleeve tailoring",shoulder,Vector3.zero,coat,new[]{S(-.34f,.086f,.087f),S(-.22f,.092f,.09f),S(-.06f,.119f,.118f),S(.03f,.102f,.098f)},14);
                var elbow=Joint("Elbow joint",shoulder,Vector3.down*.34f);
                Loft("Tapered sleeve and cuff",elbow,Vector3.zero,coat,new[]{S(-.27f,.069f,.064f),S(-.18f,.077f,.073f),S(-.04f,.089f,.087f),S(.016f,.086f,.086f)},14);
                Loft("Rolled sleeve seam",elbow,Vector3.zero,Paper*.73f,new[]{S(-.272f,.076f,.071f),S(-.236f,.079f,.074f)},12);
                var hand=Joint("Articulated mitten palm",elbow,new Vector3(0,-.267f,0));
                Loft("Palm knuckles",hand,Vector3.zero,skin,new[]{S(-.14f,.042f,.03f,.015f),S(-.105f,.064f,.037f,.006f),S(-.045f,.061f,.039f),S(0,.048f,.035f)},12);
                Seam("Thumb contour",hand,new[]{new Vector3(side*.045f,-.026f,.012f),new Vector3(side*.076f,-.063f,.029f),new Vector3(side*.067f,-.104f,.035f)},.025f,skin);
                for(int f=0;f<3;f++)Seam("Finger separation",hand,new[]{new Vector3(-.031f+f*.026f,-.09f,.04f),new Vector3(-.03f+f*.026f,-.133f,.031f)},.0025f,skin*.65f);
                if(side<0){motion.LeftArm=shoulder;motion.LeftForearm=elbow;}else{motion.RightArm=shoulder;motion.RightForearm=elbow;
                    if(role==0){var book=Joint("Survey notebook joint",elbow,new Vector3(0,-.36f,.06f));motion.Book=book;
                        Loft("Rounded notebook pages",book,Vector3.zero,Paper,new[]{S(0,.135f,.172f),S(.026f,.135f,.172f)},8);
                        Loft("Folded notebook cover",book,Vector3.zero,Leather,new[]{S(-.013f,.145f,.185f),S(-.001f,.145f,.185f)},8);
                        for(int l=0;l<5;l++)Seam("Notebook ruled ink",book,new[]{new Vector3(-.09f,.029f,-.12f+l*.047f),new Vector3(.09f,.029f,-.12f+l*.047f)},.002f,Navy);
                    }
                }
            }
            Consolidate(root,motion);motion.Initialize(island);return motion;
        }
    }
}
