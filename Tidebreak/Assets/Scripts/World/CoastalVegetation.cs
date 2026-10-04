using System;
using UnityEngine;

namespace Tidebreak
{
    // One wood mesh, one foliage mesh, and an optional snow mesh per tree.
    // Position-seeded variation does not consume the encounter/world random stream.
    public static class CoastalVegetation
    {
        static float Range(System.Random rng,float min,float max){return Mathf.Lerp(min,max,(float)rng.NextDouble());}
        static System.Random Seed(Vector3 p,float h)
        {unchecked{return new System.Random(Mathf.RoundToInt(p.x*97)*73856093^Mathf.RoundToInt(p.z*97)*19349663^Mathf.RoundToInt(h*29)*83492791);}}
        static Vector3 Trunk(float t,float h,Vector3 lean)
        {return Vector3.up*(t*h)+lean*(t*t)+new Vector3(Mathf.Sin(t*5.1f)*h*.013f,0,Mathf.Sin(t*3.8f)*h*.009f);}

        public static GameObject Pine(Transform parent,Vector3 localPos,float height,bool snow,bool collide)
        {
            height=Mathf.Max(1,height);var rng=Seed(localPos,height);
            var root=new GameObject(snow?"Wind-shaped snow cedar":"Wind-shaped coastal pine");root.transform.SetParent(parent,false);root.transform.localPosition=localPos;
            float direction=Range(rng,0,Mathf.PI*2);Vector3 wind=new Vector3(Mathf.Cos(direction),0,Mathf.Sin(direction));
            Vector3 lean=wind*(height*Range(rng,.045f,.09f));var wood=new CoastalMesh();var leaves=new CoastalMesh();var caps=snow?new CoastalMesh():null;
            Color bark=new Color(.28f,.19f,.12f),barkLit=new Color(.44f,.31f,.2f);
            for(int i=0;i<9;i++){
                float a=i/9f,b=(i+1)/9f;
                Segment(wood,Trunk(a,height,lean),Trunk(b,height,lean),height*Mathf.Lerp(.034f,.005f,a),height*Mathf.Lerp(.034f,.005f,b),bark,barkLit,7);
            }
            for(int i=0;i<5;i++){
                float angle=direction+i*Mathf.PI*.4f;Vector3 foot=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
                Segment(wood,Vector3.up*height*.09f,foot*height*.075f+Vector3.down*.025f,height*.026f,height*.008f,bark,barkLit,5);
            }
            int count=25+rng.Next(5);
            Color shade=snow?new Color(.105f,.23f,.205f):new Color(.085f,.21f,.145f),sun=snow?new Color(.31f,.46f,.38f):new Color(.31f,.43f,.225f);
            for(int i=0;i<count;i++){
                float t=.28f+.65f*i/(count-1f),angle=direction+i*2.399963f+Range(rng,-.18f,.18f);
                Vector3 radial=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
                float leeward=1+Vector3.Dot(radial,wind)*.18f;
                float length=height*(.245f*(1-t)+.026f)*Range(rng,.82f,1.19f)*leeward;
                Vector3 start=Trunk(t,height,lean),elbow=start+radial*length*.47f+Vector3.down*height*.018f;
                Vector3 tip=start+radial*length+wind*height*.035f+Vector3.up*(height*.033f);
                Segment(wood,start,elbow,height*.011f*(1-t+.25f),height*.005f,bark,barkLit,5);
                Segment(wood,elbow,tip,height*.005f,height*.0015f,bark,barkLit,5);
                for(int j=0;j<3;j++){
                    float along=.29f+j*.27f;Vector3 center=Vector3.Lerp(elbow,tip,along);
                    Vector3 outward=Quaternion.Euler(0,(j%2==0?-1:1)*Range(rng,13,30),0)*radial;
                    float sprayLength=length*Range(rng,.57f,.78f),width=sprayLength*Range(rng,.43f,.6f);
                    Color pigment=Color.Lerp(shade,sun,t*.58f+Range(rng,0,.2f));
                    Spray(leaves,caps,center,outward,sprayLength,width,height*.043f,pigment,snow&&rng.NextDouble()>.14,rng);
                }
            }
            // The broken, slightly bent leader closes the crown without a conical cap.
            Vector3 leader=Trunk(.91f,height,lean);
            for(int i=0;i<4;i++){float a=direction+i*1.55f;Spray(leaves,caps,leader+Vector3.up*height*(i*.018f),new Vector3(Mathf.Cos(a),.48f,Mathf.Sin(a)).normalized,height*.115f,height*.04f,height*.034f,Color.Lerp(shade,sun,.8f),snow,rng);}
            MakeTree(root,wood,leaves,caps,height,lean,collide);return root;
        }

        // Broad broken fans along each branch give a cedar-like silhouette from
        // the water. Their serrated ends read as needle sprays at player distance.
        static void Spray(CoastalMesh leaves,CoastalMesh snow,Vector3 at,Vector3 direction,float length,float width,float rise,Color pigment,bool covered,System.Random rng)
        {
            Vector3 forward=direction.normalized,right=Vector3.Cross(Vector3.up,forward).normalized;
            const int sections=6;Vector3 oldL=at,oldR=at,oldRidge=at+Vector3.up*rise*.24f;
            for(int i=1;i<=sections;i++){
                float t=i/(float)sections;
                float tooth=i==sections?0:Mathf.Sin(t*Mathf.PI)*width*(i%2==0?1:.77f);
                Vector3 center=at+forward*(t*length)+Vector3.up*(Mathf.Sin(t*Mathf.PI)*rise*.38f-t*t*rise*.45f);
                Vector3 left=center-right*tooth,rightEdge=center+right*tooth*.86f,ridge=center+Vector3.up*(Mathf.Sin(t*Mathf.PI)*rise);
                Color light=Color.Lerp(pigment,new Color(.51f,.61f,.35f),.08f+i*.014f),dark=Color.Lerp(pigment,new Color(.025f,.09f,.085f),.27f);
                leaves.Quad(oldL,left,ridge,oldRidge,light);leaves.Quad(oldRidge,ridge,rightEdge,oldR,Color.Lerp(light,dark,.16f));
                leaves.Quad(oldR,rightEdge,left,oldL,dark);
                if(covered&&snow!=null&&i>1&&i<sections){
                    float cap=Range(rng,.63f,.88f);Vector3 lift=Vector3.up*rise*.09f;
                    Vector3 capL=Vector3.Lerp(ridge,left,cap)+lift,capR=Vector3.Lerp(ridge,rightEdge,cap*.91f)+lift;
                    Vector3 prevL=Vector3.Lerp(oldRidge,oldL,.72f)+lift,prevR=Vector3.Lerp(oldRidge,oldR,.69f)+lift;
                    snow.Quad(prevL,capL,ridge+lift,oldRidge+lift,new Color(.83f,.89f,.85f));
                    snow.Quad(oldRidge+lift,ridge+lift,capR,prevR,new Color(.7f,.81f,.8f));
                }
                oldL=left;oldR=rightEdge;oldRidge=ridge;
            }
        }

        public static GameObject Palm(Transform parent,Vector3 localPos,float height,bool collide)
        {
            height=Mathf.Max(1,height);var rng=Seed(localPos,height);var root=new GameObject("Curved reef palm");root.transform.SetParent(parent,false);root.transform.localPosition=localPos;
            float angle=Range(rng,0,Mathf.PI*2);Vector3 lean=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*height*.14f;
            var wood=new CoastalMesh();var leaves=new CoastalMesh();Color bark=new Color(.43f,.32f,.2f),light=new Color(.64f,.48f,.3f);
            for(int i=0;i<15;i++){float t=i/15f,u=(i+1)/15f;Segment(wood,Trunk(t,height,lean),Trunk(u,height,lean),height*Mathf.Lerp(.029f,.014f,t),height*Mathf.Lerp(.028f,.013f,u),i%2==0?bark:Color.Lerp(bark,light,.25f),light,8);}
            Vector3 crown=Trunk(1,height,lean);
            for(int i=0;i<11;i++){
                float a=angle+i*2.399963f;Vector3 direction=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a)),side=Vector3.Cross(Vector3.up,direction);
                float length=height*Range(rng,.28f,.41f);Vector3 old=crown;
                for(int j=1;j<=8;j++){
                    float t=j/8f;Vector3 p=crown+direction*(t*length)+Vector3.up*(Mathf.Sin(t*Mathf.PI)*length*.25f-t*t*length*.24f);
                    Segment(wood,old,p,height*.007f*(1-t+.1f),height*.005f*(1-t+.1f),new Color(.31f,.38f,.13f),light,4);
                    if(j<8)for(int s=-1;s<=1;s+=2){Vector3 tip=p+side*(s*length*.24f*Mathf.Sin(t*Mathf.PI))+direction*length*.12f+Vector3.down*length*.04f;Vector3 baseA=Vector3.Lerp(old,p,.12f),baseB=p+direction*length*.06f;leaves.Tri(baseA,tip,baseB,new Color(.2f+.06f*t,.4f+.08f*t,.17f));leaves.Tri(baseB,tip,baseA,new Color(.1f,.26f,.15f));}
                    old=p;
                }
            }
            MakeTree(root,wood,leaves,null,height,lean,collide);return root;
        }
        static void MakeTree(GameObject root,CoastalMesh wood,CoastalMesh foliage,CoastalMesh snow,float height,Vector3 lean,bool collide)
        {
            wood.Build("Bark and branch structure",root.transform);foliage.Build("Broken needle sprays",root.transform);if(snow!=null)snow.Build("Snow resting on upper boughs",root.transform);
            foreach(var child in root.GetComponentsInChildren<Transform>())child.gameObject.isStatic=true;
            if(collide){root.layer=8;var trunk=root.AddComponent<CapsuleCollider>();trunk.direction=1;trunk.height=height*.79f;trunk.radius=height*.047f;trunk.center=Vector3.up*height*.395f+lean*.22f;}
        }
        static void Segment(CoastalMesh mesh,Vector3 a,Vector3 b,float ra,float rb,Color dark,Color light,int sides)
        {
            Vector3 direction=(b-a).normalized,right=Vector3.Cross(direction,Mathf.Abs(direction.y)>.92f?Vector3.forward:Vector3.up).normalized,up=Vector3.Cross(right,direction).normalized;
            for(int i=0;i<sides;i++){float t=i*Mathf.PI*2/sides,u=(i+1)*Mathf.PI*2/sides;Vector3 x=right*Mathf.Cos(t)+up*Mathf.Sin(t),y=right*Mathf.Cos(u)+up*Mathf.Sin(u);Color c=Color.Lerp(dark,light,.18f+.38f*Mathf.Max(0,x.y));mesh.Quad(a+x*ra,b+x*rb,b+y*rb,a+y*ra,c);}
        }
    }
}
