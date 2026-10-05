using UnityEngine;

namespace Tidebreak
{
    public class EliteTactics : MonoBehaviour
    {
        public static readonly string[] Names={"号令猎首","鼓胀刺球","蜕皮追猎者","折翼舞者","双钳卫士","共生灯母","退潮堡垒","墨影术士","追风剑客","育潮祭司","棘冠反击者","血迹猎鲨"};
        public static readonly string[] Counters={
            "蓄势时命中弱点打断号令；击败援军也会让猎首失衡。","鼓胀时身体减伤，射击嘴部使它泄压。","半血蜕皮脱逃并留下空壳；射破空壳才能停止它的再生。","左右翼交替划出水刃；看清当前亮起的翼，向另一边绕行。","正面钳甲抵挡子弹；绕后或命中眼部打开放血窗口。","击碎共生灯可解除护盾；正面强攻只造成少量伤害。","退壳蓄震时身体坚硬；跳过震荡环后有完整伸头窗口。","墨影会假扮本体，只有真正的眼睛能受伤；留意回旋刃返程。","锁定旧站位后直线连刺；第一刺后继续横向换位。","打碎育潮卵，阻止持续治疗及孵化援军。","蓄力时射身体会弹出反刺；瞄准弱点可打断。","半血后连续两次追扑，第二扑重新锁定；保留一次冲刺。"};
        Enemy owner;GameDirector game;float next,openUntil,chargingUntil,interruptReady,reflectReady,breachReady,followAt,weakPressure,nextSupportVisual;int cycle,previousMinions,wingSign;bool shed,secondPounce,needsAnchor;EncounterTarget anchor;Vector3 attackPoint,wingAxis;LineRenderer wingTell;MeshFilter wingMesh;
        public int HighlightedWing {get{return Time.time<chargingUntil?wingSign:0;}}
        public string Cue {get;private set;}
        public int Moves {get;private set;}
        public EncounterTarget Anchor {get{return anchor;}}
        public float Windup {get{return Time.time<chargingUntil?Mathf.Clamp01(1-(chargingUntil-Time.time)/1.2f):0;}}
        public float DamageFactor(bool weak)
        {
            if(anchor)return weak?.38f:.16f;
            if(weak)return Time.time<openUntil?1.16f:1;
            if(Time.time<openUntil)return 1.3f;
            if(owner.Spec.body==BodyFamily.Crab){Vector3 to=game.Player.transform.position-owner.transform.position;to.y=0;return Vector3.Dot(owner.transform.forward,to.normalized)>.35f?.2f:1.25f;}
            if(owner.Spec.body==BodyFamily.Turtle)return chargingUntil>0?.15f:.75f;
            if(owner.Spec.body==BodyFamily.Puffer)return chargingUntil>0?.25f:.85f;
            return 1;
        }
        public void Init(Enemy e)
        {
            owner=e;game=e.game;next=Time.time+.65f;Cue=Names[(int)e.Spec.body]+" · "+Counters[(int)e.Spec.body];
            needsAnchor=e.Spec.body==BodyFamily.Jelly||e.Spec.body==BodyFamily.Seahorse;
            if(e.Spec.body==BodyFamily.Ray){var obj=new GameObject("Committed blade wing");obj.transform.SetParent(transform,false);wingTell=obj.AddComponent<LineRenderer>();wingTell.material=Shape.Mat(new Color(1,.76f,.3f),true);wingTell.positionCount=3;wingTell.loop=true;wingTell.useWorldSpace=true;wingTell.startWidth=wingTell.endWidth=.1f;wingTell.enabled=false;}
        }
        public void OnHit(bool weak)
        {
            if(!weak&&owner.Spec.body==BodyFamily.Urchin&&Time.time<chargingUntil&&Time.time>=reflectReady){reflectReady=Time.time+.75f;owner.EmitBolt(game.Player.transform.position,12,7+owner.CombatIsland,1.2f);Cue="棘冠反刺 · 停止射身体，改瞄弱点";game.Notice(Cue,2);}
            if(weak){weakPressure+=owner.LastDamageApplied;bool interruptible=owner.Spec.body==BodyFamily.Perch||owner.Spec.body==BodyFamily.Puffer||owner.Spec.body==BodyFamily.Urchin;
                if(owner.Spec.body==BodyFamily.Crab&&Time.time<chargingUntil&&Time.time>=breachReady&&weakPressure>=owner.maxHealth*.12f){breachReady=Time.time+5;openUntil=Time.time+2.2f;weakPressure=0;Cue="眼甲裂开 · 躲开钳击后追打弱点";game.Audio.Cue("weak");}
                if(interruptible&&Time.time<chargingUntil&&Time.time>=interruptReady&&weakPressure>=owner.maxHealth*.16f){weakPressure=0;interruptReady=Time.time+6.5f;chargingUntil=0;next=Time.time+1.6f;openUntil=next;Cue="弱点打断 · 破招窗口";game.Audio.Cue("weak");}}
        }
        void Update()
        {
            if(!owner||owner.dead||game.Paused||game.State!=VoyageState.Combat)return;
            if(owner.Airborne){next=Mathf.Max(next,Time.time+.45f);return;}
            if(needsAnchor){
                needsAnchor=false;var p=owner.transform.position+Vector3.right*2;
                if(game.World.GroundAt(p)<0)p=game.Player.transform.position+game.Player.transform.right*2;
                p.y=Mathf.Max(-.3f,game.World.GroundAt(p))+1.8f;
                anchor=EncounterTarget.Create(game,p,owner.Spec.body==BodyFamily.Jelly?"共生灯":"育潮卵",45+owner.CombatIsland*9,Color.cyan,t=>{anchor=null;openUntil=Time.time+8;Cue="共生已切断 · 本体暴露";});
            }
            if(owner.Stunned){next+=Time.deltaTime;if(chargingUntil>0)chargingUntil+=Time.deltaTime;if(secondPounce)followAt+=Time.deltaTime;return;}
            if(owner.Spec.body==BodyFamily.Eel&&!shed&&owner.health<owner.maxHealth*.5f){shed=true;Vector3 p=owner.transform.position;p.y=game.World.GroundAt(p)+1.3f;anchor=EncounterTarget.Create(game,p,"脱落鳗皮 · 切断再生",38+owner.CombatIsland*5,Color.green,t=>{anchor=null;openUntil=Time.time+5;Cue="蜕皮已碎 · 再生停止";});owner.Lunge(game.Player.transform.position+game.Player.transform.right*6,.6f);EnemySkillFX.For(game).Action(owner,AttackStyle.Burrow,owner.transform.position);game.Notice("鳗鱼蜕皮 · 留在原处的空壳正为它再生",3);}
            if(anchor&&owner.Spec.body==BodyFamily.Eel)owner.health=Mathf.Min(owner.maxHealth*.6f,owner.health+owner.maxHealth*.022f*Time.deltaTime);
            if(owner.Spec.body==BodyFamily.Perch){int count=0;foreach(var e in game.Enemies)if(e&&e.Summoned)count++;if(previousMinions>0&&count==0){chargingUntil=0;next=Time.time+3;openUntil=next;Cue="卫队败退 · 号令猎首失衡";game.Notice(Cue,2);}previousMinions=count;}
            if(secondPounce&&Time.time>=followAt){secondPounce=false;Vector3 nextPoint=game.Player.transform.position;EnemySkillFX.Warn(owner,nextPoint,1.8f,.85f,16+owner.CombatIsland);owner.Lunge(nextPoint,.55f);Cue="第二次追扑 · 横向冲刺";}
            if(anchor&&Time.time>=nextSupportVisual&&(owner.Spec.body==BodyFamily.Eel||owner.Spec.body==BodyFamily.Seahorse)){nextSupportVisual=Time.time+.8f;EnemySkillFX.For(game).Action(owner,AttackStyle.Heal,owner.transform.position);}
            if(anchor&&owner.Spec.body==BodyFamily.Seahorse)owner.health=Mathf.Min(owner.maxHealth,owner.health+owner.maxHealth*.018f*Time.deltaTime);
            if(chargingUntil>0&&Time.time>=chargingUntil){chargingUntil=0;Execute();next=Time.time+(owner.health<owner.maxHealth*.45f?2.1f:2.65f);openUntil=Mathf.Max(openUntil,Time.time+1.35f);}
            else if(chargingUntil==0&&Time.time>=next){chargingUntil=Time.time+1.2f;weakPressure=0;game.Audio.ThreatTell(owner.transform.position,true);attackPoint=game.Player.transform.position;Cue=Names[(int)owner.Spec.body]+" · "+Counters[(int)owner.Spec.body];if(owner.Spec.body==BodyFamily.Swordfish||owner.Spec.body==BodyFamily.Shark)ThreatField.Line(game,owner.transform.position,attackPoint,.5f,1.2f,0,Color.yellow,source:owner);
                if(wingTell){wingSign=(cycle+1)%2==0?1:-1;wingAxis=owner.transform.right;foreach(var mesh in owner.GetComponentsInChildren<MeshFilter>())if(mesh.name=="Fin membrane"&&Mathf.Sign(mesh.sharedMesh.bounds.center.x)==wingSign){wingMesh=mesh;break;}}
            }
        }
        void LateUpdate(){if(!wingTell)return;wingTell.enabled=HighlightedWing!=0&&wingMesh&&owner&&!owner.dead&&game.State==VoyageState.Combat;if(!wingTell.enabled)return;var vertices=wingMesh.sharedMesh.vertices;int[] edge={0,1,vertices.Length>=42?vertices.Length-4:2};for(int i=0;i<3;i++)wingTell.SetPosition(i,wingMesh.transform.TransformPoint(vertices[edge[i]])+Vector3.up*.035f);}
        void Execute()
        {
            cycle++;Moves++;owner.NotifyAttackExecuted();Vector3 p=attackPoint;float damage=15+owner.CombatIsland*1.6f;Vector3 side=game.Player.transform.right;
            switch(owner.Spec.body){
                case BodyFamily.Perch:if(cycle%2==1){game.SpawnMinion(owner);EnemySkillFX.For(game).Action(owner,AttackStyle.Split,owner.transform.position);}owner.EmitBolt(p,11,damage);break;
                case BodyFamily.Puffer:ThreatField.Ring(game,owner.transform.position,damage,Color.yellow,source:owner);break;
                case BodyFamily.Eel:for(int i=0;i<3;i++)ThreatField.Pool(game,Vector3.Lerp(owner.transform.position,p,i/2f),1.2f,.7f,damage*.4f,Color.green,SeaTrait.Venom,source:owner,theme:SkillTheme.VenomRoot);owner.Lunge(p,.7f);break;
                case BodyFamily.Ray:for(int i=-2;i<=2;i++)owner.EmitBolt(p+wingAxis*(i*1.7f+wingSign*2),9,damage*.7f);break;
                case BodyFamily.Crab:ThreatField.Line(game,p-side*7,p+side*7,.85f,.8f,damage,Color.yellow,source:owner);break;
                case BodyFamily.Jelly:ThreatField.Ring(game,owner.transform.position,damage,Color.cyan,source:owner);owner.EmitBolt(p,7,damage*.5f);break;
                case BodyFamily.Turtle:ThreatField.Ring(game,owner.transform.position,damage,Color.yellow,source:owner);openUntil=Time.time+2.8f;break;
                case BodyFamily.Squid:ThreatField.Pool(game,p,1.7f,.85f,damage*.4f,Color.magenta,source:owner,theme:SkillTheme.Ink);owner.EmitBolt(p,9,damage,1.4f);CreateInkDecoys();break;
                case BodyFamily.Swordfish:ThreatField.Line(game,owner.transform.position,p+game.Player.transform.forward*4,.8f,.65f,damage,Color.yellow,source:owner);owner.Lunge(p,.65f);EnemySkillFX.Warn(owner,p+side*2,1.5f,1.6f,damage);break;
                case BodyFamily.Seahorse:game.SpawnMinion(owner);EnemySkillFX.For(game).Action(owner,AttackStyle.Split,owner.transform.position);owner.EmitBolt(p,8,damage*.6f);break;
                case BodyFamily.Urchin:for(int i=-1;i<=1;i++)owner.EmitBolt(p+side*i*2,12,damage*.65f,1.6f);break;
                case BodyFamily.Shark:EnemySkillFX.Warn(owner,p,1.6f,.65f,damage);owner.Lunge(p,.6f);if(owner.health<owner.maxHealth*.5f){secondPounce=true;followAt=Time.time+1.15f;}break;
            }
            if(owner.Regional)owner.Regional.AfterAttack(p);
        }
        void CreateInkDecoys()
        {
            for(int i=-1;i<=1;i+=2){var obj=new GameObject("Ink silhouette · no luminous eye");obj.transform.SetParent(game.Hazards);obj.transform.position=owner.transform.position+owner.transform.right*(i*3);obj.transform.rotation=owner.transform.rotation;
                Color dark=new Color(.13f,.055f,.19f),rim=new Color(.32f,.13f,.4f);
                CreatureSculpt.Sections(obj.transform,"Ink silhouette mantle",new[]{new Vector3(0,.15f,-.45f),new Vector3(0,.35f,-.27f),new Vector3(0,.4f,.08f),new Vector3(0,.3f,.43f),new Vector3(0,.24f,.61f)},new[]{new Vector2(.02f,.02f),new Vector2(.36f,.51f),new Vector2(.42f,.48f),new Vector2(.21f,.25f),new Vector2(.01f,.01f)},dark,rim,16);
                for(int arm=0;arm<6;arm++){float a=arm*Mathf.PI*2/6;Vector3 side=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));CreatureSculpt.Curve(obj.transform,"Curling false ink arm",new[]{side*.23f+Vector3.up*.18f,side*.53f+Vector3.down*.1f,side*.71f+Vector3.down*.38f,side*.91f+Vector3.down*.22f},new[]{.115f,.085f,.04f,.006f},dark,rim,7,3);}
                EnemySkillFX.Burst(game,SkillTheme.Ink,obj.transform.position,.7f,owner.Spec.id);Destroy(obj,3.5f);}

        }
        void OnDestroy(){if(anchor)Destroy(anchor.gameObject);}
    }
}
