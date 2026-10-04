using System.Collections.Generic;
using UnityEngine;
namespace Tidebreak
{
    public class WeaponMotion : MonoBehaviour
    {
        class Joint{public Transform t;public Vector3 p;public Quaternion q;public string name;}
        readonly List<Joint> joints=new List<Joint>();AnglerController player;float shotAge=10;
        public void Init(AnglerController p)
        {player=p;foreach(var t in GetComponentsInChildren<Transform>()){string n=t.name;if(n=="Left hand"||n=="Detachable magazine"||n=="Fluted cylinder"||n=="Pump foregrip"||n=="Harpoon shaft"||n=="Harpoon head"||n=="Hammer")joints.Add(new Joint{t=t,p=t.localPosition,q=t.localRotation,name=n});}}
        public void Shot(){shotAge=0;}
        void LateUpdate()
        {
            if(!player||player.game.Paused)return;shotAge+=Time.deltaTime;float reload=player.ReloadProgress;
            float reach=player.Reloading?Mathf.Sin(reload*Mathf.PI):0;float pump=Mathf.Sin(Mathf.Clamp01((shotAge-.08f)/.38f)*Mathf.PI);
            foreach(var j in joints){Vector3 off=Vector3.zero;Quaternion rot=Quaternion.identity;
                if(j.name=="Left hand"){off=new Vector3(-.1f*reach,-.18f*reach,-.2f*reach);rot=Quaternion.Euler(reach*30,0,-reach*35);if(player.weapon==WeaponKind.Scattergun)off.z-=pump*.095f;}
                else if(j.name=="Detachable magazine")off=Vector3.down*(reload>.15f&&reload<.73f?Mathf.Sin(Mathf.InverseLerp(.15f,.73f,reload)*Mathf.PI)*.29f:0);
                else if(j.name=="Fluted cylinder"){off.x=-reach*.085f;rot=Quaternion.Euler(0,0,reload*360);}
                else if(j.name=="Pump foregrip")off.z=-pump*.095f;
                else if(j.name=="Harpoon shaft"||j.name=="Harpoon head")off.z=player.Reloading?-reach*.32f:-Mathf.Exp(-shotAge*14)*.12f;
                else if(j.name=="Hammer")rot=Quaternion.Euler(-Mathf.Exp(-shotAge*22)*28,0,0);
                j.t.localPosition=j.p+off;j.t.localRotation=j.q*rot;
            }
        }
    }
}
