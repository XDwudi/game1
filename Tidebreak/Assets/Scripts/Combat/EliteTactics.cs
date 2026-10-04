using UnityEngine;

namespace Tidebreak
{
    public class EliteTactics : MonoBehaviour
    {
        public static readonly string[] Names={"号令猎首","鼓胀刺球","蜕皮追猎者","折翼舞者","双钳卫士","共生灯母","退潮堡垒","墨影术士","追风剑客","育潮祭司","棘冠反击者","血迹猎鲨"};
        public static readonly string[] Counters={
            "蓄势时命中弱点可打断号令，避免召来援军。","鼓胀时身体减伤，射击嘴部使它泄压。","突进留下毒径；引开尾迹再回身射击。","交替从左右翼发射水刃，保持侧向移动。","正面钳甲抵挡子弹；命中眼部会打开放血窗口。","先击碎共生灯，再攻击失去护盾的本体。","退壳时只受少量伤害，震荡后头部伸出。","墨池掩护回旋刃；离开墨池后留意返程。","预测你的旧站位连刺两次，第一刺后继续换位。","打碎育潮卵，阻止持续治疗同伴。","棘冠蓄力时不要扫射身体，弱点攻击可压制反击。","半血后连续追扑；保留一次冲刺应对第二扑。"};
        Enemy owner;GameDirector game;float next,openUntil,chargingUntil,interruptReady;int cycle;EncounterTarget anchor;
        public string Cue {get;private set;}
        public int Moves {get;private set;}
        public EncounterTarget Anchor {get{return anchor;}}
        public float Windup {get{return Time.time<chargingUntil?Mathf.Clamp01(1-(chargingUntil-Time.time)/1.2f):0;}}
        public float DamageFactor(bool weak)
        {
            if(weak)return 1;
            if(anchor)return .22f;
            if(Time.time<openUntil)return 1.3f;
            return owner.Spec.body==BodyFamily.Crab||owner.Spec.body==BodyFamily.Turtle||owner.Spec.body==BodyFamily.Puffer?.5f:1;
        }
        public void Init(Enemy e)
        {
            owner=e;game=e.game;next=Time.time+.65f;Cue=Names[(int)e.Spec.body]+" · "+Counters[(int)e.Spec.body];
            if(e.Spec.body==BodyFamily.Jelly||e.Spec.body==BodyFamily.Seahorse){var p=e.transform.position+Vector3.right*2;p.y=game.World.GroundAt(p)+1.8f;anchor=EncounterTarget.Create(game,p,e.Spec.body==BodyFamily.Jelly?"共生灯":"育潮卵",45+e.Spec.island*9,Color.cyan,t=>{anchor=null;openUntil=Time.time+8;Cue="共生已切断 · 本体暴露";});}
        }
        public void OnHit(bool weak)
        {
            if(weak){openUntil=Time.time+2.2f;bool interruptible=owner.Spec.body==BodyFamily.Perch||owner.Spec.body==BodyFamily.Puffer||owner.Spec.body==BodyFamily.Urchin;
                if(interruptible&&Time.time<chargingUntil&&Time.time>=interruptReady){interruptReady=Time.time+5;chargingUntil=0;next=Time.time+1.4f;Cue="弱点打断 · 破招窗口";game.Audio.Cue("weak");}}
        }
        void Update()
        {
            if(!owner||owner.dead||game.Paused||game.State!=VoyageState.Combat)return;
            if(anchor&&owner.Spec.body==BodyFamily.Seahorse)owner.health=Mathf.Min(owner.maxHealth,owner.health+owner.maxHealth*.018f*Time.deltaTime);
            if(chargingUntil>0&&Time.time>=chargingUntil){chargingUntil=0;Execute();next=Time.time+3.2f;openUntil=Mathf.Max(openUntil,Time.time+1.6f);}
            else if(chargingUntil==0&&Time.time>=next){chargingUntil=Time.time+1.2f;Cue=Names[(int)owner.Spec.body]+" · "+Counters[(int)owner.Spec.body];game.Notice(Cue,2.5f);}
        }
        void Execute()
        {
            cycle++;Moves++;owner.NotifyAttackExecuted();Vector3 p=game.Player.transform.position;float damage=10+owner.Spec.island*1.4f;Vector3 side=game.Player.transform.right;
            switch(owner.Spec.body){
                case BodyFamily.Perch:if(cycle%2==1)game.SpawnMinion(owner);owner.EmitBolt(p,11,damage);break;
                case BodyFamily.Puffer:ThreatField.Ring(game,owner.transform.position,damage,Color.yellow);break;
                case BodyFamily.Eel:for(int i=0;i<3;i++)ThreatField.Pool(game,Vector3.Lerp(owner.transform.position,p,i/2f),1.2f,.7f,damage*.4f,Color.green);break;
                case BodyFamily.Ray:for(int i=-2;i<=2;i++)owner.EmitBolt(p+side*(i*1.7f+(cycle%2==0?2:-2)),9,damage*.7f);break;
                case BodyFamily.Crab:ThreatField.Line(game,p-side*7,p+side*7,.85f,.8f,damage,Color.yellow);break;
                case BodyFamily.Jelly:ThreatField.Ring(game,owner.transform.position,damage,Color.cyan);owner.EmitBolt(p,7,damage*.5f);break;
                case BodyFamily.Turtle:ThreatField.Ring(game,owner.transform.position,damage,Color.yellow);openUntil=Time.time+2.8f;break;
                case BodyFamily.Squid:ThreatField.Pool(game,p,1.7f,.85f,damage*.4f,Color.magenta);owner.EmitBolt(p,9,damage,1.4f);break;
                case BodyFamily.Swordfish:ThreatField.Line(game,owner.transform.position,p+game.Player.transform.forward*4,.8f,.65f,damage,Color.yellow);game.Warn(p+side*2,1.5f,1.6f,damage);break;
                case BodyFamily.Seahorse:game.SpawnMinion(owner);owner.EmitBolt(p,8,damage*.6f);break;
                case BodyFamily.Urchin:for(int i=-1;i<=1;i++)owner.EmitBolt(p+side*i*2,12,damage*.65f,1.6f);break;
                case BodyFamily.Shark:game.Warn(p,1.6f,.65f,damage);if(owner.health<owner.maxHealth*.5f)game.Warn(p+side*2.7f,1.9f,1.4f,damage*1.2f);break;
            }
        }
        void OnDestroy(){if(anchor)Destroy(anchor.gameObject);}
    }
}
