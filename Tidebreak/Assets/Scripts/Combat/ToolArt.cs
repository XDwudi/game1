using UnityEngine;

namespace Tidebreak
{
    public static class ToolArt
    {
        static readonly Color Steel=new Color(.16f,.21f,.23f),Edge=new Color(.38f,.44f,.45f),Brass=new Color(.68f,.49f,.23f),Walnut=new Color(.32f,.17f,.09f),Skin=new Color(.67f,.44f,.28f),Sleeve=new Color(.25f,.38f,.35f);
        static Transform Part(Transform p,string n,Vector3 at,Vector3 size,Color c,PrimitiveType type=PrimitiveType.Cube)
        {var g=type==PrimitiveType.Cube&&(n=="Receiver"||n=="Walnut grip"||n=="Stock"||n=="Detachable magazine"||n=="Pump foregrip"||n=="Capacitor")?Beveled(p,n,at,size,c):Shape.Part(n,type,p,at,size,c);if(c==Steel||c==Edge||c==Brass)g.GetComponent<Renderer>().sharedMaterial=Shape.Metal(c);if(c==Walnut)g.GetComponent<Renderer>().sharedMaterial=Shape.Wood(c);return g.transform;}
        static Transform Cylinder(Transform p,string n,Vector3 at,float radius,float length,Color c)
        {var t=Part(p,n,at,new Vector3(radius*2,length*.5f,radius*2),c,PrimitiveType.Cylinder);t.localRotation=Quaternion.Euler(90,0,0);return t;}
        static GameObject Beveled(Transform parent,string name,Vector3 at,Vector3 scale,Color color)
        {
            var m=new CoastalMesh();float bevel=.14f;
            Vector2[] rim={new Vector2(-.5f,-.5f+bevel),new Vector2(-.5f+bevel,-.5f),new Vector2(.5f-bevel,-.5f),new Vector2(.5f,-.5f+bevel),new Vector2(.5f,.5f-bevel),new Vector2(.5f-bevel,.5f),new Vector2(-.5f+bevel,.5f),new Vector2(-.5f,.5f-bevel)};
            for(int i=0;i<8;i++){int j=(i+1)%8;Vector3 a=new Vector3(rim[i].x,rim[i].y,-.5f),b=new Vector3(rim[j].x,rim[j].y,-.5f);Color side=Color.Lerp(color,Edge,i%2==0?.16f:.48f);m.Quad(a,b,b+Vector3.forward,a+Vector3.forward,side);m.Tri(Vector3.back*.5f,b,a,color);m.Tri(Vector3.forward*.5f,a+Vector3.forward,b+Vector3.forward,color);}
            var g=m.Build(name,parent);g.transform.localPosition=at;g.transform.localScale=scale;return g;
        }
        static void Hand(Transform p,Vector3 at,Quaternion rotation,bool left=false)
        {
            var r=new GameObject(left?"Left hand":"Right hand").transform;r.SetParent(p,false);r.localPosition=at;r.localRotation=rotation;
            CoastalMesh.Tube("Jacket sleeve",r,new[]{new Vector3(0,-.46f,-.13f),new Vector3(0,-.16f,-.045f),Vector3.zero},new[]{.083f,.068f,.059f},Sleeve,Sleeve*.7f,8);
            Part(r,"Cuff",new Vector3(0,-.035f,0),new Vector3(.14f,.085f,.13f),Sleeve*.65f);
            var palm=Shape.MeshObject("Palm",r,new[]{new Vector3(-.047f,0,-.03f),new Vector3(.047f,0,-.03f),new Vector3(-.056f,.1f,-.024f),new Vector3(.056f,.1f,-.024f),new Vector3(-.047f,0,.038f),new Vector3(.047f,0,.038f),new Vector3(-.056f,.1f,.042f),new Vector3(.056f,.1f,.042f)},new[]{0,2,3,0,3,1,4,5,7,4,7,6,0,4,6,0,6,2,1,3,7,1,7,5,2,6,7,2,7,3,0,1,5,0,5,4},Skin);
            for(int i=0;i<4;i++){float x=(i-1.5f)*.027f;CoastalMesh.Tube("Curled finger",r,new[]{new Vector3(x,.095f,.005f),new Vector3(x,.132f,.04f),new Vector3(x,.09f,.078f),new Vector3(x,.047f,.06f)},new[]{.017f,.016f,.014f,.012f},Skin,Skin*.82f,6);}
            float side=left?-1:1;CoastalMesh.Tube("Thumb",r,new[]{new Vector3(side*.044f,.018f,.02f),new Vector3(side*.065f,.052f,.058f),new Vector3(side*.03f,.07f,.071f)},new[]{.023f,.02f,.014f},Skin,Skin*.82f,6);
        }
        public static GameObject Rod(Transform parent,out Transform tip,out Transform reel)
        {
            var root=new GameObject("Bayside carbon rod");var r=root.transform;r.SetParent(parent,false);r.localPosition=new Vector3(.26f,-.37f,.43f);r.localRotation=Quaternion.Euler(-28,-9,0);
            Shape.Beam(r,new Vector3(0,-.09f,-.12f),new Vector3(0,0,.42f),.044f,new Color(.49f,.34f,.19f));
            for(int i=0;i<13;i++)CoastalMesh.Ring(r,new Vector3(0,0,i*.027f),.024f,.0023f,Walnut,Quaternion.identity);
            var points=new Vector3[9];var radii=new float[9];
            for(int i=0;i<9;i++){float t=i/8f;points[i]=new Vector3(0,-t*t*.19f,.35f+t*1.8f);radii[i]=Mathf.Lerp(.014f,.0035f,t);}
            CoastalMesh.Tube("Tapered graphite blank",r,points,radii,new Color(.11f,.17f,.18f),Steel,8);
            for(int i=0;i<6;i++){float t=i/5f;CoastalMesh.Ring(r,new Vector3(0,.024f-t*t*.19f,.42f+t*1.68f),.032f-t*.019f,.0035f,Edge,Quaternion.identity);}
            Cylinder(r,"Reel spool",new Vector3(0,-.07f,.19f),.062f,.085f,Brass);
            Cylinder(r,"Line on spool",new Vector3(0,-.07f,.2f),.048f,.088f,new Color(.75f,.75f,.61f));
            reel=new GameObject("Reel crank").transform;reel.SetParent(r,false);reel.localPosition=new Vector3(-.065f,-.07f,.18f);
            Shape.Beam(reel,Vector3.zero,new Vector3(-.025f,-.055f,0),.009f,Steel);Shape.Beam(reel,new Vector3(-.025f,-.055f,0),new Vector3(-.07f,-.055f,0),.016f,Walnut);
            Hand(r,new Vector3(0,-.13f,.05f),Quaternion.Euler(0,0,0));Hand(r,new Vector3(-.12f,-.21f,.2f),Quaternion.Euler(10,0,-45),true);
            tip=new GameObject("Rod tip").transform;tip.SetParent(r,false);tip.localPosition=points[8];return root;
        }
        public static GameObject CarryHands(Transform parent)
        {var g=new GameObject("Carrying hands");g.transform.SetParent(parent,false);Hand(g.transform,new Vector3(.37f,-.46f,.88f),Quaternion.Euler(-20,15,30));Hand(g.transform,new Vector3(-.22f,-.42f,.85f),Quaternion.Euler(-20,-15,-30),true);return g;}
        public static GameObject Gun(Transform parent,WeaponKind kind,out Transform muzzle)
        {
            var root=new GameObject(Balance.Weapons[(int)kind].name);var r=root.transform;r.SetParent(parent,false);r.localPosition=new Vector3(.245f,-.245f,.44f);
            bool pistol=kind==WeaponKind.Revolver;float end=pistol?.43f:.77f;
            Part(r,"Receiver",new Vector3(0,0,.08f),new Vector3(.115f,.105f,.22f),Steel);
            Part(r,"Machined upper edge",new Vector3(0,.055f,.075f),new Vector3(.092f,.018f,.2f),Edge);
            var grip=Part(r,"Walnut grip",new Vector3(0,-.1f,-.02f),new Vector3(.09f,.18f,.085f),Walnut);grip.localRotation=Quaternion.Euler(-19,0,0);
            for(int i=0;i<5;i++)Part(grip,"Grip checkering",new Vector3(0,(i-2)*.16f,.515f),new Vector3(1.02f,.035f,.03f),Brass);
            CoastalMesh.Ring(r,new Vector3(0,-.073f,.071f),.046f,.007f,Steel,Quaternion.Euler(0,90,0));
            Shape.Beam(r,new Vector3(0,-.023f,.058f),new Vector3(0,-.067f,.08f),.009f,Edge);
            if(pistol) {
                var yoke=new GameObject("Revolver yoke").transform;yoke.SetParent(r,false);yoke.localPosition=new Vector3(0,0,.07f);
                Cylinder(yoke,"Fluted cylinder",Vector3.zero,.068f,.14f,Edge);
                for(int i=0;i<6;i++){float a=i*Mathf.PI/3;Cylinder(yoke,"Cylinder fluting",new Vector3(Mathf.Cos(a)*.057f,Mathf.Sin(a)*.057f,.01f),.014f,.133f,Steel);Cylinder(yoke,"Cartridge rim",new Vector3(Mathf.Cos(a)*.042f,Mathf.Sin(a)*.042f,-.074f),.012f,.008f,Brass);}
                Cylinder(r,"Octagonal barrel",new Vector3(0,.035f,.295f),.028f,.28f,Steel);
                Part(r,"Barrel rib",new Vector3(0,.064f,.295f),new Vector3(.023f,.016f,.28f),Edge);
                Part(r,"Hammer",new Vector3(0,.075f,-.042f),new Vector3(.021f,.044f,.031f),Steel).localRotation=Quaternion.Euler(-22,0,0);
            } else {
                Cylinder(r,"Long barrel",new Vector3(0,.037f,.46f),.031f,.62f,Steel);
                if(kind==WeaponKind.Scattergun)Cylinder(r,"Magazine tube",new Vector3(0,-.03f,.42f),.027f,.51f,Steel);
                if(kind==WeaponKind.Scattergun){var pump=Part(r,"Pump foregrip",new Vector3(0,-.036f,.4f),new Vector3(.098f,.09f,.24f),Walnut);for(int i=0;i<8;i++)Part(pump,"Pump grooves",new Vector3(0,.49f,-.375f+i*.1083f),new Vector3(1.02f,.11f,.029f),Brass*.5f);}
                else if(kind==WeaponKind.Harpoon){for(int s=-1;s<=1;s+=2)Shape.Beam(r,new Vector3(s*.09f,.08f,.13f),new Vector3(s*.09f,.08f,.66f),.012f,Brass);Cylinder(r,"Harpoon shaft",new Vector3(0,.077f,.55f),.009f,.7f,Edge);CoastalMesh.Tube("Harpoon head",r,new[]{new Vector3(0,.077f,.84f),new Vector3(0,.077f,.9f),new Vector3(0,.077f,1.03f)},new[]{.01f,.035f,0},Edge,Steel,5);end=1.02f;}
                if(kind==WeaponKind.Carbine||kind==WeaponKind.BurstRifle){Part(r,"Detachable magazine",new Vector3(0,-.17f,.12f),new Vector3(.09f,.27f,.12f),Steel).localRotation=Quaternion.Euler(-12,0,0);Part(r,"Stock",new Vector3(0,-.05f,-.24f),new Vector3(.09f,.17f,.3f),Walnut);for(int v=0;v<6;v++)Part(r,"Cooling vent",new Vector3(0,.074f,.23f+v*.055f),new Vector3(.072f,.02f,.02f),Edge);if(kind==WeaponKind.BurstRifle){CoastalMesh.Ring(r,new Vector3(0,.115f,.13f),.037f,.008f,Steel,Quaternion.identity);Part(r,"Optic foot",new Vector3(0,.072f,.13f),new Vector3(.045f,.028f,.075f),Steel);}}
                if(kind==WeaponKind.ArcCaster){for(int v=0;v<5;v++)CoastalMesh.Ring(r,new Vector3(0,.037f,.32f+v*.07f),.074f,.009f,new Color(.3f,.9f,.85f),Quaternion.identity);Part(r,"Capacitor",new Vector3(0,-.075f,.13f),new Vector3(.16f,.16f,.23f),Steel);}
                Hand(r,new Vector3(-.025f,-.16f,.38f),Quaternion.Euler(10,0,15),true);
            }
            Part(r,"Bolt carrier",new Vector3(.056f,.025f,.1f),new Vector3(.012f,.04f,.097f),Edge);
            Cylinder(r,"Charging handle",new Vector3(.075f,.025f,.08f),.013f,.035f,Brass);
            CoastalMesh.Ring(r,new Vector3(0,.035f,end),pistol?.029f:.033f,.006f,Edge,Quaternion.identity);
            Cylinder(r,"Dark bore",new Vector3(0,.035f,end+.003f),.019f,.01f,new Color(.025f,.032f,.033f));
            Part(r,"Front sight",new Vector3(0,.079f,end-.036f),new Vector3(.01f,.03f,.022f),Steel);Part(r,"Sight bead",new Vector3(0,.096f,end-.034f),new Vector3(.008f,.007f,.009f),new Color(.95f,.84f,.53f));
            Part(r,"Rear sight left",new Vector3(-.019f,.078f,-.025f),new Vector3(.013f,.022f,.02f),Steel);Part(r,"Rear sight right",new Vector3(.019f,.078f,-.025f),new Vector3(.013f,.022f,.02f),Steel);
            if(pistol)Hand(r,new Vector3(-.066f,-.163f,-.012f),Quaternion.Euler(5,0,-18),true);
            Hand(r,new Vector3(0,-.16f,-.043f),Quaternion.Euler(-8,0,0));
            muzzle=new GameObject("Muzzle").transform;muzzle.SetParent(r,false);muzzle.localPosition=new Vector3(0,.035f,end+.05f);return root;
        }
    }
}
