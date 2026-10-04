using UnityEngine;

namespace Tidebreak
{
    public partial class SeaWorld
    {
        void BuildCoastFoam()
        {
            var foam=new CoastalMesh();Color color=new Color(.63f,.82f,.79f);
            for(int z=-73;z<25;z+=2)for(int x=-49;x<49;x+=2){
                var corners=new[]{new Vector3(x,Height(x,z),z),new Vector3(x+2,Height(x+2,z),z),new Vector3(x+2,Height(x+2,z+2),z+2),new Vector3(x,Height(x,z+2),z+2)};
                Vector3 first=Vector3.zero;int hits=0;
                for(int i=0;i<4;i++){Vector3 a=corners[i],b=corners[(i+1)%4];if((a.y<-.42f)==(b.y<-.42f))continue;Vector3 p=Vector3.Lerp(a,b,Mathf.InverseLerp(a.y,b.y,-.42f));p.y=-.38f;
                    if(hits++==0)first=p;else {Vector3 side=Vector3.Cross((p-first).normalized,Vector3.up)*.1f;foam.Quad(first-side,first+side,p+side,p-side,color);}}
            }
            foam.Build("Foam follows sculpted coastline",Scenery);
        }
        void BuildRouteArchitecture()
        {
            var root=new GameObject("Authored paths and crossings").transform;root.SetParent(Scenery);
            foreach(var route in Layout.Routes){
                Vector3 a=Layout.Points[route.a],b=Layout.Points[route.b],forward=(b-a).normalized;Vector3 side=Vector3.Cross(forward,Vector3.up).normalized;
                float length=Vector3.Distance(a,b);
                if(route.bridge){
                    var deck=new CoastalMesh();Color timber=Region>=6?Color.Lerp(iron,Definition.accent,.22f):Region==5?new Color(.43f,.61f,.65f):wood;
                    // One continuous sloped collider; decorative seams never create
                    // stair lips for a CharacterController to catch on.
                    float half=route.width*.5f;Vector3 rise=Vector3.up*.06f;
                    deck.Quad(a+side*half+rise,b+side*half+rise,b-side*half+rise,a-side*half+rise,timber);
                    deck.Quad(a-side*half-Vector3.up*.25f,b-side*half-Vector3.up*.25f,b+side*half-Vector3.up*.25f,a+side*half-Vector3.up*.25f,timber*.7f);
                    deck.Build("Walkable crossing "+route.a+" to "+route.b,root,true);
                    int planks=Mathf.CeilToInt(length/.8f);for(int i=1;i<planks;i++){
                        Vector3 p=Vector3.Lerp(a,b,i/(float)planks)+rise+Vector3.up*.014f;
                        Shape.Beam(root,p+side*(half-.08f),p-side*(half-.08f),.025f,ivory*.62f);
                    }
                    for(int i=0;i<=Mathf.CeilToInt(length/5);i++){
                        Vector3 p=Vector3.Lerp(a,b,Mathf.Clamp01(i*5/length));
                        foreach(int s in new[]{-1,1}){
                            Vector3 at=p+side*(half+.12f)*s;float bed=Height(at.x,at.z);
                            Shape.Beam(root,new Vector3(at.x,Mathf.Min(at.y-.4f,bed),at.z),at+Vector3.up*.82f,.2f,timber);
                            Shape.Part("Crossing reflector",PrimitiveType.Cube,root,at+Vector3.up*.87f,new Vector3(.13f,.12f,.13f),Definition.accent,false,true);
                        }
                    }
                    // Leave a wide centre and open intersections. The rail is an
                    // honest visible edge cue, never an invisible collision wall.
                    for(int s=-1;s<=1;s+=2){Shape.Beam(root,a+side*(half+.12f)*s+Vector3.up*.7f,b+side*(half+.12f)*s+Vector3.up*.7f,.045f,timber);}
                }
                else {
                    for(float d=3;d<length-3;d+=5.5f){
                        Vector3 p=Vector3.Lerp(a,b,d/length);p+=side*(route.width*.5f+.55f);p.y=Height(p.x,p.z);
                        var stone=Shape.Rock(root,p,new Vector3(.44f,.26f,.37f),Color.Lerp(Definition.ground,ivory,.35f),5);stone.name="Trail edge masonry";
                    }
                }
            }
            for(int i=4;i<Layout.Points.Length;i++){
                Vector3 p=Layout.Points[i]+new Vector3(2.9f,0,2.9f);p.y=Height(p.x,p.z);TrailLamp(root,p,Region>=6?2.8f:2.35f);
            }
            BuildCliffLayers(root);
            Vector3 board=Layout.Points[1]+new Vector3(-4.3f,0,1.7f);board.y=Height(board.x,board.z);
            Shape.Beam(root,board,board+Vector3.up*2.9f,.14f,wood);
            Sign(root,board+Vector3.up*2.2f,Layout.Name,"沿路灯探索 · 岔路通往隐藏日志",0,4.3f);
        }
        void TrailLamp(Transform root,Vector3 p,float height)
        {
            Shape.Beam(root,p,p+Vector3.up*height,.1f,iron);
            Shape.Beam(root,p+Vector3.up*height,p+new Vector3(.55f,height,0),.08f,iron);
            Box("Route lantern frame",root,p+new Vector3(.5f,height-.23f,0),new Vector3(.34f,.46f,.34f),iron);
            Box("Route lantern glass",root,p+new Vector3(.5f,height-.23f,.18f),new Vector3(.22f,.28f,.03f),Region>=6?Definition.accent:new Color(1,.77f,.4f));
            Shape.Part("Route lantern flame",PrimitiveType.Sphere,root,p+new Vector3(.5f,height-.23f,.2f),Vector3.one*.13f,Region>=6?Definition.accent:new Color(1,.74f,.3f),false,true);
        }
        void BuildCliffLayers(Transform root)
        {
            // Large coherent shelves replace a uniform scatter of little cones.
            // Only slope-facing outcrops are placed; all authored routes stay clear.
            for(int z=-62;z<-13;z+=7)for(int x=-37;x<=37;x+=8){
                if(Layout.IsTrail(x,z,3.3f)||z>2&&Mathf.Abs(x)<29)continue;float h=Height(x,z);if(h<.8f)continue;
                float east=Height(x+3,z),north=Height(x,z+3);if(Mathf.Max(Mathf.Abs(h-east),Mathf.Abs(h-north))<1.35f)continue;
                Vector3 pos=new Vector3(x,h-.6f,z);Color stone=Region==5?new Color(.52f,.69f,.74f):Color.Lerp(Definition.ground,iron,.52f);
                for(int layer=0;layer<3;layer++){
                    var rock=Shape.Rock(root,pos+Vector3.up*(layer*.58f),new Vector3(2.9f-layer*.25f,.64f,2.2f-layer*.19f),stone*(.9f+layer*.065f),7);rock.name="Layered coastal escarpment";
                    // Close the visible stone volume to player and bullet casts.
                    rock.AddComponent<MeshCollider>();rock.layer=8;
                }
            }
        }
        Transform BuildInvestigationProp(Transform root,int index)
        {
            Color metal=Color.Lerp(iron,Definition.accent,.18f),light=Definition.accent;
            if(index==2){
                Box("Captain's archive chest",root,new Vector3(0,.43f,0),new Vector3(1.45f,.85f,.85f),wood,true);
                for(int s=-1;s<=1;s+=2)Box("Archive chest iron band",root,new Vector3(s*.5f,.43f,0),new Vector3(.09f,.89f,.89f),iron);
                Box("Archive charts",root,new Vector3(0,.9f,.04f),new Vector3(.65f,.07f,.4f),ivory);
            }else switch(Region){
                case 0:
                    if(index==0){Box("Weatherproof battery locker",root,new Vector3(0,.63f,0),new Vector3(1.8f,1.2f,.95f),metal,true);for(int i=-1;i<=1;i++)Shape.Part("Copper battery",PrimitiveType.Cylinder,root,new Vector3(i*.45f,1.38f,0),new Vector3(.3f,.22f,.3f),orange);Box("Battery terminals",root,new Vector3(0,1.76f,0),new Vector3(1.45f,.09f,.16f),ivory);}
                    else{Box("Lens calibration bench",root,new Vector3(0,.55f,0),new Vector3(2.2f,1.1f,1.2f),wood,true);for(int i=0;i<4;i++)CoastalMesh.Ring(root,new Vector3(0,1.6f,i*.09f),.65f-i*.08f,.055f,light,Quaternion.identity);}
                    break;
                case 1:
                    if(index==0){for(int i=0;i<5;i++){float angle=i*72*Mathf.Deg2Rad;Vector3 p=new Vector3(Mathf.Cos(angle)*.85f,.4f,Mathf.Sin(angle)*.65f);var shell=Shape.Rock(root,p,new Vector3(.52f,.36f,.7f),Color.Lerp(ivory,light,i*.15f),9);shell.transform.localRotation=Quaternion.Euler(0,-i*72,0);}CoastalMesh.Ring(root,Vector3.up*.13f,1.5f,.12f,ivory,Quaternion.Euler(90,0,0));}
                    else{Shape.Beam(root,Vector3.zero,Vector3.up*1.9f,.23f,metal);for(int s=-1;s<=1;s+=2){Shape.Beam(root,new Vector3(s*.63f,1.4f,0),new Vector3(s*.63f,3.2f,0),.16f,light);Shape.Beam(root,new Vector3(s*.63f,1.4f,0),Vector3.up*1.4f,.16f,light);}CoastalMesh.Ring(root,Vector3.up*.15f,1.2f,.13f,ivory,Quaternion.Euler(90,0,0));}
                    break;
                case 2:
                    Box("Filtration pump housing",root,new Vector3(0,.6f,0),new Vector3(1.8f,1.1f,1.35f),metal,true);
                    for(int s=-1;s<=1;s+=2){Shape.Part("Filter pressure tank",PrimitiveType.Cylinder,root,new Vector3(s*.52f,1.65f,0),new Vector3(.52f,.7f,.52f),s<0?ivory:light);Shape.Beam(root,new Vector3(s*.52f,1.8f,0),new Vector3(s*1.35f,1.8f,0),.13f,iron);}
                    CoastalMesh.Ring(root,new Vector3(0,1.05f,.8f),.46f,.07f,orange,Quaternion.identity);break;
                case 3:
                    if(index==0){var slab=Shape.Rock(root,new Vector3(0,1.1f,0),new Vector3(1.25f,1.5f,.36f),metal,5);slab.name="Solar inscription slab";for(int n=0;n<4;n++)Box("Inlaid stone inscription",root,new Vector3(0,.7f+n*.4f,.37f),new Vector3(1.35f-n*.2f,.06f,.025f),light);}
                    else{Shape.Part("Astrolabe plinth",PrimitiveType.Cylinder,root,new Vector3(0,.35f,0),new Vector3(2.6f,.35f,2.6f),metal,true).layer=8;for(int n=0;n<3;n++)CoastalMesh.Ring(root,Vector3.up*1.3f,1-n*.15f,.065f,n==0?orange:ivory,Quaternion.Euler(n*55,0,n*30));Shape.Beam(root,new Vector3(0,.5f,0),new Vector3(.7f,2.1f,0),.08f,light);}
                    break;
                case 4:
                    if(index==0){Box("Black box shock cage",root,new Vector3(0,.58f,0),new Vector3(1.7f,1.15f,.95f),iron,true);Box("Northstar black box",root,new Vector3(0,.75f,.52f),new Vector3(1.25f,.68f,.16f),orange);for(int i=0;i<4;i++)Box("Black box recorder fins",root,new Vector3(-.5f+i*.33f,1.2f,0),new Vector3(.06f,.3f,.7f),ivory);}
                    else{for(int s=-1;s<=1;s+=2)Shape.Beam(root,new Vector3(s*.85f,0,0),new Vector3(s*.85f,2.8f,0),.17f,wood);Shape.Beam(root,new Vector3(-.85f,2.8f,0),new Vector3(.85f,2.8f,0),.2f,wood);CoastalMesh.Tube("Salvaged ship bell",root,new[]{Vector3.up*2.5f,Vector3.up*2.2f,Vector3.up*1.6f,Vector3.up*1.5f},new[]{.16f,.35f,.55f,.68f},orange,metal,14);Shape.Beam(root,Vector3.up*1.8f,Vector3.up*.7f,.04f,ivory);}
                    break;
                case 5:
                    if(index==0){Box("Heater cabinet",root,new Vector3(0,.72f,0),new Vector3(1.6f,1.4f,1.1f),metal,true);for(int i=0;i<5;i++)Box("Heat-core grille",root,new Vector3(0,.4f+i*.18f,.57f),new Vector3(1.1f,.06f,.04f),orange);Shape.Beam(root,new Vector3(.65f,.8f,0),new Vector3(.65f,3.3f,0),.12f,ivory);}
                    else{var capsule=Shape.Part("Frozen passenger capsule",PrimitiveType.Capsule,root,new Vector3(0,1.05f,-.4f),new Vector3(1.7f,1.65f,1.7f),ivory,true);capsule.layer=8;capsule.transform.localRotation=Quaternion.Euler(90,0,0);Box("Capsule viewing pane",root,new Vector3(0,1.55f,.45f),new Vector3(1.1f,.42f,.9f),new Color(.26f,.52f,.61f));for(int s=-1;s<=1;s+=2)Box("Rescue runners",root,new Vector3(s*.7f,.17f,-.5f),new Vector3(.18f,.25f,3.2f),iron);}
                    break;
                case 6:
                    Box("Relay maintenance console",root,new Vector3(0,.55f,0),new Vector3(1.6f,1.1f,1.2f),metal,true);Shape.Beam(root,new Vector3(0,1,0),new Vector3(0,3.7f,0),.2f,iron);for(int n=0;n<3;n++)CoastalMesh.Ring(root,Vector3.up*(1.7f+n*.55f),.55f+n*.15f,.07f,n%2==0?light:ivory,Quaternion.Euler(90,0,0));Box("Relay readout",root,new Vector3(0,.95f,.66f),new Vector3(1,.25f,.03f),light);break;
                case 7:
                    if(index==0){for(int n=0;n<5;n++){var p=new Vector3((n-2)*.45f,.4f,Mathf.Sin(n)*.35f);var rock=Shape.Rock(root,p,new Vector3(.5f,.65f+n*.17f,.5f),metal,5);rock.transform.localRotation=Quaternion.Euler(0,n*31,(n-2)*-13);Shape.Beam(root,p+Vector3.up*.15f,p+Vector3.up*(.7f+n*.14f),.065f,light,true);}}
                    else{Box("Ancient forge base",root,new Vector3(0,.5f,-.3f),new Vector3(2.5f,1,1.9f),metal,true);Shape.Part("Forge crucible",PrimitiveType.Cylinder,root,new Vector3(0,1.3f,-.3f),new Vector3(1.9f,.5f,1.9f),iron);Shape.Part("Molten key bed",PrimitiveType.Cylinder,root,new Vector3(0,1.81f,-.3f),new Vector3(1.55f,.025f,1.55f),light,false,true);Box("Forge exhaust",root,new Vector3(0,2,-1),new Vector3(1.3f,2.2f,.6f),metal);}
                    break;
                default:
                    Shape.Part("Memory dais",PrimitiveType.Cylinder,root,new Vector3(0,.25f,0),new Vector3(2.6f,.25f,2.6f),metal,true).layer=8;
                    if(index==0){for(int n=0;n<3;n++){float angle=n*120*Mathf.Deg2Rad;Vector3 p=new Vector3(Mathf.Cos(angle)*.7f,1.1f,Mathf.Sin(angle)*.7f);var shard=Shape.Rock(root,p,new Vector3(.28f,1.2f,.28f),light,4);shard.transform.localRotation=Quaternion.Euler(0,-n*120,18);}}
                    else{for(int n=0;n<3;n++)CoastalMesh.Ring(root,Vector3.up*(.8f+n*.45f),.85f,.06f,light,Quaternion.Euler(90,0,n*30));Shape.Beam(root,Vector3.up*.5f,Vector3.up*2.7f,.13f,ivory);}
                    break;
            }
            var marker=Shape.Part("Investigation signal",PrimitiveType.Sphere,root,new Vector3(0,2.8f,0),Vector3.one*.12f,light,false,true);
            return marker.transform;
        }
    }
}
