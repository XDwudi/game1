using UnityEngine;

namespace Tidebreak
{
    public class EliteTactics : MonoBehaviour
    {
        public static readonly string[] Names={"号令猎首","鼓胀刺球","蜕皮追猎者","折翼舞者","双钳卫士","共生灯母","退潮堡垒","墨影术士","追风剑客","育潮祭司","棘冠反击者","血迹猎鲨"};
        public static readonly string[] Counters={
            "蓄势时命中弱点打断号令；击败援军也会让猎首失衡。","鼓胀时身体减伤，射击嘴部使它泄压。","半血蜕皮脱逃并留下空壳；射破空壳才能停止它的再生。","左右翼交替划出水刃；看清当前亮起的翼，向另一边绕行。","正面钳甲抵挡子弹；绕后或命中眼部打开放血窗口。","击碎共生灯可解除护盾；正面强攻只造成少量伤害。","退壳蓄震时身体坚硬；跳过震荡环后有完整伸头窗口。","墨影会假扮本体，只有真正的眼睛能受伤；留意回旋刃返程。","锁定旧站位后直线连刺；第一刺后继续横向换位。","打碎育潮卵，阻止持续治疗及孵化援军。","蓄力时射身体会弹出反刺；瞄准弱点可打断。","半血后连续两次追扑，第二扑重新锁定；保留一次冲刺。"};
        Enemy owner;GameDirector game;float next,openUntil,chargingUntil,interruptReady,reflectReady,followAt;int cycle,previousMinions,wingSign;bool shed,secondPounce;EncounterTarget anchor;Vector3 attackPoint,wingAxis;LineRenderer wingTell;MeshFilter wingMesh;
        public int HighlightedWing {get{return Time.time<chargingUntil?wingSign:0;}}
        public string Cue {get;private set;}
        public int Moves {get;private set;}
        public EncounterTarget Anchor {get{return anchor;}}
        public float Windup {get{return Time.time<chargingUntil?Mathf.Clamp01(1-(chargingUntil-Time.time)/1.2f):0;}}
        public float DamageFactor(bool weak)
        {
            if(anchor)return weak?.38f:.16f;
            if(weak)return 1;
            if(Time.time<openUntil)return 1.3f;
            if(owner.Spec.body==BodyFamily.Crab){Vector3 to=game.Player.transform.position-owner.transform.position;to.y=0;return Vector3.Dot(owner.transform.forward,to.normalized)>.35f?.2f:1.25f;}
            if(owner.Spec.body==BodyFamily.Turtle)return chargingUntil>0?.15f:.75f;
            if(owner.Spec.body==BodyFamily.Puffer)return chargingUntil>0?.25f:.85f;
            return 1;
        }
        public void Init(Enemy e)
        {
            owner=e;game=e.game;next=Time.time+.65f;Cue=Names[(int)e.Spec.body]+" · "+Counters[(int)e.Spec.body];
            if(e.Spec.body==BodyFamily.Jelly||e.Spec.body==BodyFamily.Seahorse){var p=e.transform.position+Vector3.right*2;p.y=game.World.GroundAt(p)+1.8f;anchor=EncounterTarget.Create(game,p,e.Spec.body==BodyFamily.Jelly?"共生灯":"育潮卵",45+e.Spec.island*9,Color.cyan,t=>{anchor=null;openUntil=Time.time+8;Cue="共生已切断 · 本体暴露";});}
            if(e.Spec.body==BodyFamily.Ray){var obj=new GameObject("Committed blade wing");obj.transform.SetParent(transform,false);wingTell=obj.AddComponent<LineRenderer>();wingTell.material=Shape.Mat(new Color(1,.76f,.3f),true);wingTell.positionCount=3;wingTell.loop=true;wingTell.useWorldSpace=true;wingTell.startWidth=wingTell.endWidth=.1f;wingTell.enabled=false;}
        }
        public void OnHit(bool weak)
        {
            if(!weak&&owner.Spec.body==BodyFamily.Urchin&&Time.time<chargingUntil&&Time.time>=reflectReady){reflectReady=Time.time+.75f;owner.EmitBolt(game.Player.transform.position,12,7+owner.Spec.island,1.2f);Cue="棘冠反刺 · 停止射身体，改瞄弱点";game.Notice(Cue,2);}
            if(weak){openUntil=Time.time+2.2f;bool interruptible=owner.Spec.body==BodyFamily.Perch||owner.Spec.body==BodyFamily.Puffer||owner.Spec.body==BodyFamily.Urchin;
                if(interruptible&&Time.time<chargingUntil&&Time.time>=interruptReady){interruptReady=Time.time+5;chargingUntil=0;next=Time.time+1.4f;Cue="弱点打断 · 破招窗口";game.Audio.Cue("weak");}}
        }
        void Update()
        {
            if(!owner||owner.dead||game.Paused||game.State!=VoyageState.Combat)return;
            if(owner.Spec.body==BodyFamily.Eel&&!shed&&owner.health<owner.maxHealth*.5f){shed=true;Vector3 p=owner.transform.position;p.y=game.World.GroundAt(p)+1.3f;anchor=EncounterTarget.Create(game,p,"脱落鳗皮 · 切断再生",38+owner.Spec.island*5,Color.green,t=>{anchor=null;openUntil=Time.time+5;Cue="蜕皮已碎 · 再生停止";});owner.Lunge(game.Player.transform.position+game.Player.transform.right*6,.6f);game.Notice("鳗鱼蜕皮 · 留在原处的空壳正为它再生",3);}
            if(anchor&&owner.Spec.body==BodyFamily.Eel)owner.health=Mathf.Min(owner.maxHealth*.6f,owner.health+owner.maxHealth*.022f*Time.deltaTime);
            if(owner.Spec.body==BodyFamily.Perch){int count=0;foreach(var e in game.Enemies)if(e&&e.Summoned)count++;if(previousMinions>0&&count==0){chargingUntil=0;next=Time.time+3;openUntil=next;Cue="卫队败退 · 号令猎首失衡";game.Notice(Cue,2);}previousMinions=count;}
            if(secondPounce&&Time.time>=followAt){secondPounce=false;Vector3 nextPoint=game.Player.transform.position;game.Warn(nextPoint,1.8f,.85f,16+owner.Spec.island);owner.Lunge(nextPoint,.55f);Cue="第二次追扑 · 横向冲刺";}
            if(anchor&&owner.Spec.body==BodyFamily.Seahorse)owner.health=Mathf.Min(owner.maxHealth,owner.health+owner.maxHealth*.018f*Time.deltaTime);
            if(chargingUntil>0&&Time.time>=chargingUntil){chargingUntil=0;Execute();next=Time.time+3.2f;openUntil=Mathf.Max(openUntil,Time.time+1.6f);}
            else if(chargingUntil==0&&Time.time>=next){chargingUntil=Time.time+1.2f;attackPoint=game.Player.transform.position;Cue=Names[(int)owner.Spec.body]+" · "+Counters[(int)owner.Spec.body];if(owner.Spec.body==BodyFamily.Swordfish||owner.Spec.body==BodyFamily.Shark)ThreatField.Line(game,owner.transform.position,attackPoint,.5f,1.2f,0,Color.yellow);
                if(wingTell){wingSign=(cycle+1)%2==0?1:-1;wingAxis=owner.transform.right;foreach(var mesh in owner.GetComponentsInChildren<MeshFilter>())if(mesh.name=="Fin membrane"&&Mathf.Sign(mesh.sharedMesh.bounds.center.x)==wingSign){wingMesh=mesh;break;}}
            }
        }
        void LateUpdate(){if(!wingTell)return;wingTell.enabled=HighlightedWing!=0&&wingMesh&&owner&&!owner.dead&&game.State==VoyageState.Combat;if(!wingTell.enabled)return;var vertices=wingMesh.sharedMesh.vertices;for(int i=0;i<3;i++)wingTell.SetPosition(i,wingMesh.transform.TransformPoint(vertices[i])+Vector3.up*.035f);}
        void Execute()
        {
            cycle++;Moves++;owner.NotifyAttackExecuted();Vector3 p=attackPoint;float damage=10+owner.Spec.island*1.4f;Vector3 side=game.Player.transform.right;
            switch(owner.Spec.body){
                case BodyFamily.Perch:if(cycle%2==1)game.SpawnMinion(owner);owner.EmitBolt(p,11,damage);break;
                case BodyFamily.Puffer:ThreatField.Ring(game,owner.transform.position,damage,Color.yellow);break;
                case BodyFamily.Eel:for(int i=0;i<3;i++)ThreatField.Pool(game,Vector3.Lerp(owner.transform.position,p,i/2f),1.2f,.7f,damage*.4f,Color.green,SeaTrait.Venom);owner.Lunge(p,.7f);break;
                case BodyFamily.Ray:for(int i=-2;i<=2;i++)owner.EmitBolt(p+wingAxis*(i*1.7f+wingSign*2),9,damage*.7f);break;
                case BodyFamily.Crab:ThreatField.Line(game,p-side*7,p+side*7,.85f,.8f,damage,Color.yellow);break;
                case BodyFamily.Jelly:ThreatField.Ring(game,owner.transform.position,damage,Color.cyan);owner.EmitBolt(p,7,damage*.5f);break;
                case BodyFamily.Turtle:ThreatField.Ring(game,owner.transform.position,damage,Color.yellow);openUntil=Time.time+2.8f;break;
                case BodyFamily.Squid:ThreatField.Pool(game,p,1.7f,.85f,damage*.4f,Color.magenta);owner.EmitBolt(p,9,damage,1.4f);CreateInkDecoys();break;
                case BodyFamily.Swordfish:ThreatField.Line(game,owner.transform.position,p+game.Player.transform.forward*4,.8f,.65f,damage,Color.yellow);owner.Lunge(p,.65f);game.Warn(p+side*2,1.5f,1.6f,damage);break;
                case BodyFamily.Seahorse:game.SpawnMinion(owner);owner.EmitBolt(p,8,damage*.6f);break;
                case BodyFamily.Urchin:for(int i=-1;i<=1;i++)owner.EmitBolt(p+side*i*2,12,damage*.65f,1.6f);break;
                case BodyFamily.Shark:game.Warn(p,1.6f,.65f,damage);owner.Lunge(p,.6f);if(owner.health<owner.maxHealth*.5f){secondPounce=true;followAt=Time.time+1.15f;}break;
            }
        }
        void CreateInkDecoys()
        {
            for(int i=-1;i<=1;i+=2){var obj=new GameObject("Ink silhouette · no luminous eye");obj.transform.SetParent(game.Hazards);obj.transform.position=owner.transform.position+owner.transform.right*(i*3);obj.transform.rotation=owner.transform.rotation;
                var form=Shape.Part("False mantle",PrimitiveType.Capsule,obj.transform,Vector3.up*.4f,new Vector3(.8f,1.1f,.8f),new Color(.2f,.13f,.32f),false);
                for(int arm=0;arm<5;arm++)Shape.Part("Ink trail",PrimitiveType.Capsule,obj.transform,new Vector3((arm-2)*.2f,-.45f,0),new Vector3(.1f,.7f,.1f),new Color(.3f,.17f,.4f),false);Destroy(obj,3.5f);}
        }
        void OnDestroy(){if(anchor)Destroy(anchor.gameObject);}
    }
}
