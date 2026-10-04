using System.Collections.Generic;
using UnityEngine;
namespace Tidebreak
{
    public class WeaponMotion : MonoBehaviour
    {
        class Joint{public Transform t;public Vector3 p;public Quaternion q;public string name;}
        readonly List<Joint> joints=new List<Joint>();AnglerController player;float shotAge=10;int shots;
        public void Init(AnglerController p)
        {player=p;foreach(var t in GetComponentsInChildren<Transform>()){string n=t.name;if(n=="Left hand"||n=="Detachable magazine"||n=="Revolver yoke"||n=="Pump foregrip"||n=="Harpoon shaft"||n=="Harpoon head"||n=="Hammer"||n=="Bolt carrier"||n=="Charging handle")joints.Add(new Joint{t=t,p=t.localPosition,q=t.localRotation,name=n});}}
        public void Shot(){shotAge=0;shots++;}
        static float Step(float a,float b,float x){return Mathf.SmoothStep(0,1,Mathf.InverseLerp(a,b,x));}
        static float Window(float a,float b,float c,float d,float x){return Step(a,b,x)*(1-Step(c,d,x));}
        static Vector3 Reach(Vector3 rest,float t,Vector3 grip,Vector3 withdrawn,Vector3 latch)
        {
            if(t<.2f)return Vector3.Lerp(rest,grip,Step(.04f,.2f,t));
            if(t<.43f)return Vector3.Lerp(grip,withdrawn,Step(.24f,.43f,t));
            if(t<.7f)return Vector3.Lerp(withdrawn,grip,Step(.51f,.7f,t));
            if(t<.83f)return Vector3.Lerp(grip,latch,Step(.72f,.83f,t));
            if(t<.92f)return latch+Vector3.back*(Window(.83f,.86f,.89f,.92f,t)*.07f);
            return Vector3.Lerp(latch,rest,Step(.92f,1,t));
        }
        void LateUpdate()
        {
            if(!player||player.game.Paused)return;shotAge+=Time.deltaTime;bool reloading=player.Reloading;float r=reloading?player.ReloadProgress:0;
            float pump=Window(.09f,.23f,.3f,.47f,shotAge),bolt=Window(0,.024f,.046f,.115f,shotAge);
            float open=reloading?Window(.06f,.22f,.83f,.98f,r):0,magTravel=reloading?Window(.24f,.43f,.51f,.7f,r):0;
            foreach(var j in joints){Vector3 off=Vector3.zero;Quaternion rot=Quaternion.identity;
                if(j.name=="Left hand"){
                    if(reloading){
                        if(player.weapon==WeaponKind.Revolver){Vector3 grip=new Vector3(-.1f,-.065f,.055f),withdrawn=new Vector3(-.19f,-.22f,-.05f),latch=new Vector3(-.075f,-.025f,.045f);off=Reach(j.p,r,grip,withdrawn,latch)-j.p;rot=Quaternion.Euler(open*25,open*-20,open*-35);}
                        else if(player.weapon==WeaponKind.Scattergun){float feed=Mathf.Sin(Mathf.Clamp01((r-.12f)/.65f)*Mathf.PI);off=new Vector3(.05f*feed,-.1f*feed,-.29f*feed);rot=Quaternion.Euler(feed*35,0,feed*-20);}
                        else {off=Reach(j.p,r,new Vector3(-.025f,-.26f,.1f),new Vector3(-.075f,-.49f,.05f),new Vector3(.055f,-.022f,.08f))-j.p;rot=Quaternion.Euler(open*28,open*10,open*-24);}
                    }
                    if(player.weapon==WeaponKind.Scattergun)off.z-=pump*.095f;
                }
                else if(j.name=="Detachable magazine"){off=new Vector3(-.04f*magTravel,-.24f*magTravel,-.045f*magTravel);rot=Quaternion.Euler(magTravel*12,0,-magTravel*9);}
                else if(j.name=="Revolver yoke"){off=new Vector3(-open*.093f,-open*.012f,0);rot=Quaternion.Euler(0,0,reloading?Step(.28f,.61f,r)*120:shots==0?0:(shots-1+Step(0,.08f,shotAge))*60);}
                else if(j.name=="Pump foregrip")off.z=-pump*.095f;
                else if(j.name=="Bolt carrier"||j.name=="Charging handle")off.z=-bolt*.055f-(reloading?Window(.83f,.86f,.89f,.92f,r)*.07f:0);
                else if(j.name=="Harpoon shaft"||j.name=="Harpoon head")off.z=reloading?-open*.32f:-Mathf.Exp(-shotAge*14)*.12f;
                else if(j.name=="Hammer")rot=Quaternion.Euler(-Mathf.Exp(-shotAge*22)*28,0,0);
                j.t.localPosition=j.p+off;j.t.localRotation=j.q*rot;
            }
        }
    }
}
