using System.Collections.Generic;
using UnityEngine;

namespace Tidebreak
{
    // Local-space deformation preserves the silhouette while fins and claws articulate independently.
    public class CreatureMotion : MonoBehaviour
    {
        class Flexible { public Mesh mesh;public Vector3[] rest,work;public string name; }
        class Joint {public Transform t;public Quaternion rest;public Vector3 position,pivot;public float side;public int kind;}
        Enemy enemy;Transform rig;Vector3 scale;float attackPulse,hitPulse,nextDeform,seed;
        readonly List<Flexible> flexible=new List<Flexible>();readonly List<Joint> joints=new List<Joint>();
        readonly Transform[] krakenArms=new Transform[8];
        public void Init(Enemy e,Transform model)
        {
            enemy=e;rig=model;scale=rig.localScale;seed=e.Spec.id*.43f;
            if(e.Spec.id==117)for(int i=0;i<8;i++)krakenArms[i]=rig.Find("Tentacle "+i);
            foreach(var f in rig.GetComponentsInChildren<MeshFilter>()){
                string n=f.name.ToLowerInvariant();
                if(n.Contains("tentacle")||n.Contains("curling arm")||n.Contains("faceted scales")||n.Contains("flexible")||n.Contains("streamlined")||n.Contains("whip tail")||n.Contains("fin membrane")){
                    var mesh=f.mesh;mesh.MarkDynamic();var v=mesh.vertices;flexible.Add(new Flexible{mesh=mesh,rest=v,work=new Vector3[v.Length],name=n});
                }
            }
            foreach(var t in rig.GetComponentsInChildren<Transform>()){
                if(t==rig)continue;string n=t.name.ToLowerInvariant();int k=n.Contains("claw")||n.Contains("pincer")?1:n.Contains("tail fin")?2:n.Contains("fin membrane")?3:n.Contains("leg")?4:n.Contains("whale tail fluke")?5:n.Contains("whale pectoral paddle")?6:0;
                if(k>0){var pivot=Vector3.zero;float side=t.localPosition.x;var mf=t.GetComponent<MeshFilter>();
                    // Procedural limbs bake their positions into vertices. Preserve their
                    // attachment point instead of rotating the whole limb around the torso.
                    if((k==1||k==4)&&t.localPosition.sqrMagnitude<.01f&&mf&&mf.sharedMesh){
                        side=mf.sharedMesh.bounds.center.x;float nearest=float.PositiveInfinity;
                        foreach(var v in mf.sharedMesh.vertices)if(v.sqrMagnitude<nearest){nearest=v.sqrMagnitude;pivot=v;}
                        pivot=Vector3.Scale(pivot,t.localScale);
                    }
                    joints.Add(new Joint{t=t,rest=t.localRotation,position=t.localPosition,pivot=pivot,side=side<0?-1:1,kind=k});}
            }
        }
        public void Attack(){attackPulse=1;}
        public void Hit(){hitPulse=1;}
        void LateUpdate()
        {
            if(!enemy||!rig||enemy.game.Paused)return;
            float t=Time.time,wind=enemy.Encounter?enemy.Encounter.Windup:enemy.Elite?enemy.Elite.Windup:enemy.AttackWindup;
            attackPulse=Mathf.MoveTowards(attackPulse,0,Time.deltaTime*2.8f);hitPulse=Mathf.MoveTowards(hitPulse,0,Time.deltaTime*7);
            float breath=Mathf.Sin(t*2.4f+seed)*.025f;
            rig.localScale=Vector3.Scale(scale,new Vector3(1+breath+wind*.06f,1+breath*.6f-wind*.08f+attackPulse*.08f,1-breath*.5f+wind*.05f));
            rig.localRotation=Quaternion.Euler(-wind*9+attackPulse*7-hitPulse*4+(enemy.Encounter?enemy.Encounter.PosePitch:0),Mathf.Sin(t*1.3f+seed)*2,Mathf.Sin(t*2+seed)*(enemy.IsBoss?1:3));
            if(enemy.Spec.id==117&&enemy.Encounter)for(int i=0;i<8;i++)if(krakenArms[i]){
                bool severed=(enemy.Encounter.SeveredArmMask&(1<<i))!=0;
                krakenArms[i].localScale=Vector3.Lerp(krakenArms[i].localScale,severed?new Vector3(.3f,.68f,.45f):Vector3.one,Time.deltaTime*5);
            }
            foreach(var j in joints){float side=j.side;float a=j.kind==1?side*(wind*28-attackPulse*42):j.kind==2?Mathf.Sin(t*7+seed)*20:j.kind==3?Mathf.Sin(t*3.5f+side)*12:j.kind==5?Mathf.Sin(t*2.1f)*9:j.kind==6?side*Mathf.Sin(t*1.7f)*8:Mathf.Sin(t*7+j.position.z*3+side)*11;j.t.localRotation=j.rest*Quaternion.Euler(j.kind==4||j.kind==5?a:0,j.kind==2?a:0,j.kind==1||j.kind==3||j.kind==6?a:0);j.t.localPosition=j.position+j.rest*j.pivot-j.t.localRotation*j.pivot;}
            if(t<nextDeform)return;nextDeform=t+1/30f;
            foreach(var f in flexible){for(int i=0;i<f.rest.Length;i++){var p=f.rest[i];bool arm=f.name.Contains("tentacle")||f.name.Contains("curling arm");float along=arm?p.magnitude:p.z;float weight=f.name.Contains("faceted scales")?Mathf.Clamp01(-p.z*.8f):Mathf.Clamp01(Mathf.Abs(along)*.6f);float wave=Mathf.Sin(t*3.6f-along*2.1f+seed)*(arm?.19f:.11f)*weight;if(f.name.Contains("fin")){p.y+=wave*.9f;}else {p.x+=wave;p.y+=Mathf.Cos(t*2.7f-along*1.8f+seed)*.07f*weight;p.z-=wind*weight*.1f;}f.work[i]=p;}f.mesh.vertices=f.work;f.mesh.RecalculateNormals();f.mesh.RecalculateBounds();}
        }
        void OnDestroy(){foreach(var f in flexible)if(f.mesh)Destroy(f.mesh);}
    }
}
