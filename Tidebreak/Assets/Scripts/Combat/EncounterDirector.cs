using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tidebreak
{
    // Combat phases share scheduling, while BossMechanism owns each encounter's
    // player actions and damage rules. Health is never scaled to player gear.
    public class EncounterDirector : MonoBehaviour
    {
        public Enemy Owner { get; private set; }
        public int Phase { get; private set; } = 1;
        public int Moves { get; private set; }
        public int GatesBroken { get; private set; }
        public BossMechanism Mechanism {get;private set;}
        public int SeveredArms { get {return bossIndex==9&&Mechanism?Mechanism.CompletedActions:0;} }
        public int SeveredArmMask { get {int mask=0;for(int i=0;i<SeveredArms;i++)mask|=1<<(i*2+1);return mask;} }
        public int KrakenSlamCount {get{return Mathf.Max(2,2+Phase-SeveredArms);}}
        string cue;
        public string Cue { get {return Mechanism&&Mechanism.Active?Mechanism.Cue:cue;} private set {cue=value;} }
        public float Windup { get { return pending == null ? 0 : Mathf.Clamp01(1 - (executeAt - Time.time) / tellDuration); } }
        public bool Recovering { get { return Mechanism&&Mechanism.Active?Mechanism.DamageMultiplier>=1:Time.time<recoveryUntil; } }
        public float Submerge {get{return Mechanism&&Mechanism.Active?Mechanism.Submerge:bossIndex==10&&pending!=null&&(turn-1)%3==0?Mathf.Sin(Windup*Mathf.PI)*2.4f:0;}}
        public Vector3 PoseOffset {get{return Mechanism?Mechanism.PoseOffset:Vector3.zero;}}
        public float PosePitch {get{return Mechanism&&Mechanism.Active?Mechanism.PosePitch:bossIndex==10?((turn-1)%3==2?-18:12)*Windup:bossIndex==9?-4*Windup:0;}}
        public float FacingOffset {get{return Mechanism&&Mechanism.Active?Mechanism.FacingOffset:bossIndex==10&&Recovering?Mathf.Sin(Time.time*.65f)*48:0;}}
        public readonly List<EncounterTarget> Targets = new List<EncounterTarget>();
        public float DamageFactor { get { return Mechanism&&Mechanism.Active?Mechanism.DamageMultiplier:Recovering?(Mechanism?Mechanism.ExposureMultiplier:1.35f):.65f; } }
        public static readonly string[] Mechanics = BossMechanism.Instructions;
        GameDirector game;
        Action pending;
        float nextMove, executeAt, tellDuration, recoveryUntil, nextPressure;
        Vector3 lockedPosition;
        readonly Queue<Vector3> recentPositions=new Queue<Vector3>();float recordAt;
        int turn, bossIndex;

        public void Init(Enemy enemy)
        {
            Owner = enemy; game = enemy.game; bossIndex = enemy.Spec.id - 108;
            nextMove = Time.time + 2.2f;
            Cue = Mechanics[bossIndex];
            Mechanism=gameObject.AddComponent<BossMechanism>();Mechanism.Init(enemy,MechanismSolved);Mechanism.BeginPhase(1);
        }
        void Update()
        {
            if (!Owner || Owner.dead || game.State != VoyageState.Combat || game.Paused) return;
            Targets.RemoveAll(t => !t);
            if(Time.time>=recordAt){recordAt=Time.time+.8f;recentPositions.Enqueue(game.Player.transform.position);while(recentPositions.Count>4)recentPositions.Dequeue();}
            int desired = Owner.health <= Owner.maxHealth * .32f ? 3 : Owner.health <= Owner.maxHealth * .67f ? 2 : 1;
            if (desired > Phase && (!Mechanism||!Mechanism.Active)) ChangePhase(desired);
            Owner.phase = Phase;
            if(Mechanism&&Mechanism.Active&&!Mechanism.ShouldRunAttackPattern){pending=null;return;}
            if (pending != null && Time.time >= executeAt)
            {
                var action = pending; pending = null; action(); Moves++; Owner.NotifyAttackExecuted();
                recoveryUntil = Mathf.Max(recoveryUntil,Time.time + (Phase == 3 ? 2.1f : 2.8f));
                nextMove = recoveryUntil + (Mechanism&&Mechanism.Active ? 3.5f : .7f);
            }
            if (pending == null && Time.time >= nextMove) ScheduleMove();
        }
        void ChangePhase(int phase)
        {
            Phase = phase; pending = null; turn = 0; recoveryUntil = 0;
            ClearTargets();
            Mechanism.BeginPhase(phase);
            Cue = "第 " + Phase + " 阶段 · " + Mechanics[bossIndex];
            game.Notice(BossNarrative.Get(Owner.Spec.id).speaker+"："+BossNarrative.PhaseLine(Owner.Spec.id,Phase), 5); game.Audio.Cue("boss"); nextMove = Time.time + 2;
        }
        public float ClampPhaseDamage(float amount)
        {
            // A huge critical hit cannot skip the next encounter phase.
            float floor = Phase == 1 ? Owner.maxHealth * .665f : Phase == 2 ? Owner.maxHealth * .315f : Mechanism&&Mechanism.Active?1:0;
            return Mathf.Min(amount, Mathf.Max(0, Owner.health - floor));
        }
        void MechanismSolved()
        {
            GatesBroken+=Mechanism.CompletedActions;pending=null;
            recoveryUntil=Time.time+Mechanism.ExposureDuration;
            nextMove=Time.time+Mathf.Max(2,Mechanism.ExposureDuration*.55f);
            Cue="机制破解 · "+Mechanism.TargetLabel+" · 核心可攻击";
            game.Notice(Cue,4);game.Audio.Cue("weak");
        }
        void Tell(string text, float seconds, Action action)
        {
            lockedPosition = game.Player.transform.position; Cue = text;
            tellDuration = seconds * (game.Run.easy ? 1.25f : 1); executeAt = Time.time + tellDuration; pending = action;
            game.Audio.Cue("sonar");
        }
        void ScheduleMove()
        {
            int move = turn++ % 3; float damage = 15 + bossIndex*1.4f + Phase*2;
            Vector3 p = game.Player.transform.position;
            Vector3 across = game.Player.transform.right;
            switch (bossIndex)
            {
                case 0:
                    if(move==0){Tell("裂钳交叉 · 离开两条橙色夹击线",1.15f,()=>{Line(lockedPosition,across,24,damage);Line(lockedPosition,Vector3.forward,24,damage);}); PreviewCross(p,across,1.15f);}
                    else if(move==1)Tell("重钳三连 · 不要提前耗尽冲刺",.7f,()=>{for(int i=0;i<Phase+1;i++)game.Warn(lockedPosition+across*i*2,2, .55f+i*.5f,damage);});
                    else Tell("巨蟹顿足 · 跳过低矮冲击环",.75f,()=>ThreatField.Ring(game,lockedPosition,damage,Color.yellow));
                    break;
                case 1:
                    if(move==0)Tell("逆音双环 · 两次起跳，间隔留出落地",.8f,()=>{ThreatField.Ring(game,Owner.transform.position,damage,Color.cyan);ThreatField.RingDelayed(game,Owner.transform.position,damage,Color.magenta,2.3f);});
                    else if(move==1)Tell("翼尖音刃 · 扇形间隙保持移动",1,()=>Fan(lockedPosition,7+Phase*2,damage*.6f,11));
                    else Tell("静音水域 · 离开脚下的共鸣漩涡",.8f,()=>ThreatField.Vortex(game,lockedPosition,3.2f,1.1f,damage*.45f,Color.magenta));
                    break;
                case 2:
                    if(move==0)Tell("毒根追踪 · 三段尾迹，沿弧线撤离",.9f,()=>{for(int i=0;i<3;i++)ThreatField.Pool(game,lockedPosition+across*(i-1)*3,1.8f,.8f+i*.4f,damage*.38f,Color.green,SeaTrait.Venom);});
                    else if(move==1)Tell("潜地猎杀 · 波纹锁定后再闪避",1,()=>{game.Warn(lockedPosition,3,.7f,damage*1.2f);game.Warn(lockedPosition+Vector3.forward*4,2.4f,1.6f,damage);});
                    else Tell("根须孵化 · 优先清理追猎幼体",1.2f,()=>{game.SpawnMinion(Owner);Fan(lockedPosition,3,damage*.6f,9);});
                    break;
                case 3:
                    if(move==0)Tell("日轮棋盘 · 在轰炸格之间换位",1,()=>{for(int x=-1;x<=1;x++)for(int z=-1;z<=1;z++)if((x+z+Phase)%2==0)game.Warn(lockedPosition+new Vector3(x*3.4f,0,z*3.4f),1.5f,.8f,damage);});
                    else if(move==1)Tell("星轨折射 · 前后两道光线依次扫过",1,()=>{Line(lockedPosition,across,25,damage);DelayedLine(lockedPosition+Vector3.forward*3,across,25,damage,1.4f);});
                    else Tell("龟甲震荡 · 准备跳跃与头部反击",.8f,()=>ThreatField.Ring(game,lockedPosition,damage,Color.yellow));
                    break;
                case 4:
                    if(move==0)Tell("沉钟牵引 · 先冲出涡心，再留意回旋刃",1,()=>{ThreatField.Vortex(game,lockedPosition,4.2f,.8f,damage*.35f,Owner.Spec.color);Fan(lockedPosition,5,damage*.55f,9,true);});
                    else if(move==1)Tell("锚链绞杀 · 两条平行锁链留下中间通道",1,()=>{Line(lockedPosition+across*2.5f,Vector3.forward,30,damage);Line(lockedPosition-across*2.5f,Vector3.forward,30,damage);});
                    else Tell("午夜钟声 · 波纹会折返",1.1f,()=>{ThreatField.Ring(game,Owner.transform.position,damage,Color.cyan);Fan(lockedPosition,3,damage*.5f,8,true);});
                    break;
                case 5:
                    if(move==0){Tell("独角猎线 · 侧移离开锁定的冰脊",1.25f,()=>Line(lockedPosition,Vector3.forward,45,damage*1.2f));PreviewCross(p,Vector3.forward,1.25f,false);}
                    else if(move==1)Tell("冰刺编织 · 旧站位将被冰封",.9f,()=>{for(int i=0;i<Phase+1;i++)game.Warn(lockedPosition+across*(i-1)*3,1.7f,.6f+i*.4f,damage);});
                    else Tell("破冰落震 · 跳过落地潮环",1,()=>ThreatField.Ring(game,lockedPosition,damage,Color.cyan));
                    break;
                case 6:
                    if(move==0){Tell("雷极十字 · 离开交叉导线",1.25f,()=>{Line(lockedPosition,across,40,damage);DelayedLine(lockedPosition,Vector3.forward,40,damage,.9f);});PreviewCross(p,across,1.25f);}
                    else if(move==1)Tell("电容脉冲 · 交错弹道之间穿行",1,()=>{Fan(lockedPosition,9,damage*.55f,10);ThreatField.Ring(game,lockedPosition+Vector3.forward*7,damage,Color.yellow);});
                    else Tell("雷暴追针 · 先引雷，再换位",.8f,()=>{game.Warn(lockedPosition,2.1f,.65f,damage);game.Warn(lockedPosition-across*3,2,1.35f,damage);});
                    break;
                case 7:
                    if(move==0)Tell("熔潮封路 · 三条岩浆沟之间保留退路",1,()=>{for(int i=-1;i<=1;i++)ThreatField.Pool(game,lockedPosition+across*i*4,1.7f,1.1f,damage*.5f,new Color(1,.3f,.08f));});
                    else if(move==1)Tell("锻炉超压 · 第一击后继续移动",.8f,()=>{game.Warn(lockedPosition,3,.6f,damage);game.Warn(lockedPosition+across*4,2.5f,1.3f,damage);ThreatField.RingDelayed(game,lockedPosition,damage,Color.red,2.2f);});
                    else Tell("熔甲碎片 · 近距扇射后甲壳骤冷",1.2f,()=>Fan(lockedPosition,11,damage*.65f,13));
                    break;
                case 8:
                    if(move==0){var echoes=recentPositions.ToArray();Tell("镜渊记录 · 重演你最近四个站位，离开旧轨迹",1.1f,()=>{for(int i=0;i<echoes.Length;i++)game.Warn(echoes[i],2.4f,.5f+i*.45f,damage);});}
                    else if(move==1)Tell("镜面对折 · 内外两层潮环",1,()=>{ThreatField.Ring(game,lockedPosition,damage,Color.cyan);ThreatField.RingDelayed(game,Owner.transform.position,damage,Color.magenta,1.6f);});
                    else Tell("归航悖论 · 击退回声，争取关机窗口",1,()=>{game.SpawnMinion(Owner);if(game.Run.storyChoice==1)game.SpawnMinion(Owner);Fan(lockedPosition,5+Phase*2,damage*.55f,11);});
                    break;
                case 9:
                    if(move==0)Tell("八腕封海 · "+KrakenSlamCount+" 段拍岸，斩断登陆触腕可削减连击",1,()=>{for(int i=0;i<KrakenSlamCount;i++)game.Warn(lockedPosition+across*(i-1)*2.7f,2.2f,.6f+i*.45f,damage);});
                    else if(move==1)Tell("墨潮吞岸 · 脱离墨涡，准备跳浪",1,()=>{ThreatField.Vortex(game,lockedPosition,4.3f,.8f,damage*.45f,new Color(.6f,.2f,.8f));ThreatField.RingDelayed(game,Owner.transform.position,damage,Color.magenta,Phase==3?1.1f:1.9f);});
                    else Tell("古神怒目 · 交叉触腕后，眼部暴露",1.3f,()=>{Line(lockedPosition,across,45,damage);DelayedLine(lockedPosition,Vector3.forward,45,damage,1);if(Phase==3)Fan(lockedPosition,9,damage*.5f,12);});
                    break;
                default:
                    if(move==0)Tell("白鲸猎线 · 蓝线追踪，变橙锁定后横向冲刺",.45f,()=>{TrackingBreach.Create(game,Owner.transform.position,damage*1.2f);if(Phase>=2)TrackingBreach.Create(game,Owner.transform.position+Vector3.right*(Phase==3?-14:14),damage,1.45f);});
                    else if(move==1)Tell("鲸歌三拍 · 跳过第一拍，再处理回声",1,()=>{ThreatField.Ring(game,Owner.transform.position,damage,Color.cyan);ThreatField.RingDelayed(game,Owner.transform.position,damage,Color.white,2.1f);if(Phase==3)ThreatField.RingDelayed(game,Owner.transform.position,damage,Color.cyan,3.4f);});
                    else Tell("跃鲸碎冰 · 冰雨封路，留意返程回声",.8f,()=>{for(int i=-2;i<=2;i++)game.Warn(lockedPosition+across*i*2.5f,1.35f,.7f+Mathf.Abs(i)*.3f,damage);Fan(lockedPosition,3,damage*.5f,10,true);if(Phase==3)ThreatField.Vortex(game,lockedPosition,4,.8f,damage*.3f,Color.cyan);});
                    break;
            }
        }
        void PreviewCross(Vector3 p, Vector3 direction, float delay, bool cross=true)
        {
            ThreatField.Line(game,p-direction*18,p+direction*18,.9f,delay+ .1f,0,new Color(1,.62f,.2f));
            if(cross)ThreatField.Line(game,p-Vector3.forward*18,p+Vector3.forward*18,.9f,delay+.1f,0,new Color(1,.62f,.2f));
        }
        void Line(Vector3 p, Vector3 d, float length, float damage) { DelayedLine(p,d,length,damage,.25f); }
        void DelayedLine(Vector3 p,Vector3 d,float length,float damage,float delay)
        { ThreatField.Line(game,p-d*length*.5f,p+d*length*.5f,1.15f,delay,damage,new Color(1,.36f,.22f)); }
        void Fan(Vector3 target,int count,float damage,float speed,bool returns=false)
        { for(int i=0;i<count;i++){Vector3 dir=Quaternion.Euler(0,(i-(count-1)*.5f)*9,0)*(target-Owner.transform.position).normalized;Owner.EmitBolt(Owner.transform.position+dir*25,speed,damage,returns?1.6f:0); } }
        void ClearTargets(){foreach(var t in Targets)if(t)Destroy(t.gameObject);Targets.Clear();}
        void OnDestroy(){ClearTargets();}
    }

    public class EncounterTarget : MonoBehaviour
    {
        public string Label;
        public float Health,Maximum;
        public bool Dead {get;private set;}
        GameDirector game;Action<EncounterTarget> destroyed;Transform crystal;float age;Material coreMaterial;
        public static EncounterTarget Create(GameDirector game,Vector3 p,string name,float hp,Color color,Action<EncounterTarget> onBreak)
        {
            var root=new GameObject(name);root.transform.position=p;
            var t=root.AddComponent<EncounterTarget>();t.game=game;t.Label=name;t.Health=t.Maximum=hp;t.destroyed=onBreak;
            // Keep the original aiming hull and bobbing motion. A framed, smaller
            // crystal replaces the full-bright sphere, so nearby targets don't fill the view.
            var part=Shape.Part("Breakable core",PrimitiveType.Sphere,root.transform,Vector3.zero,new Vector3(.85f,1.2f,.85f),color,true,false);t.crystal=part.transform;
            part.GetComponent<MeshRenderer>().enabled=false;
            Color pigment=Color.Lerp(new Color(.3f,.59f,.52f),color,.21f);
            var jewel=CreatureSurfaceArt.Facet(part.transform,"Layered sea crystal",Vector3.zero,new Vector3(.63f,.72f,.63f),pigment);
            t.coreMaterial=new Material(Shape.Mat(pigment));t.coreMaterial.SetColor("_EmissionColor",pigment*.075f);t.coreMaterial.SetFloat("_Glossiness",.38f);t.coreMaterial.SetFloat("_Metallic",.12f);
            jewel.GetComponent<MeshRenderer>().sharedMaterial=t.coreMaterial;
            CoastalMesh.Ring(part.transform,Vector3.zero,.28f,.023f,new Color(.81f,.68f,.39f),Quaternion.Euler(90,0,0));
            BuildSilhouette(root.transform,name,color);
            var light=root.AddComponent<Light>();light.color=pigment;light.intensity=.33f;light.range=2.2f;
            return t;
        }
        static void BuildSilhouette(Transform root,string label,Color color)
        {
            Color shell=Color.Lerp(new Color(.12f,.19f,.21f),color,.13f);
            if(label.Contains("触腕")){
                var points=new Vector3[20];var widths=new float[20];
                Vector3 a=new Vector3(-1.1f,-1.65f,0),b=new Vector3(-2,1.8f,0),c=new Vector3(1.6f,1.9f,.3f),d=new Vector3(.6f,-.3f,.1f);
                for(int i=0;i<20;i++){float t=i/19f,u=1-t;points[i]=u*u*u*a+3*u*u*t*b+3*u*t*t*c+t*t*t*d;widths[i]=Mathf.Lerp(.5f,.035f,t);}
                CoastalMesh.Tube("Stranded tentacle",root,points,widths,new Color(.34f,.16f,.32f),new Color(.65f,.34f,.45f),10);
                for(int i=2;i<17;i+=2)Shape.Part("Sucker",PrimitiveType.Sphere,root,points[i]+Vector3.forward*widths[i],Vector3.one*(widths[i]*.66f),new Color(.84f,.58f,.7f));
            }else if(label.Contains("冰")||label.Contains("碎片")){
                for(int i=0;i<4;i++){var shard=Shape.Part("Fractured shell",PrimitiveType.Cube,root,new Vector3((i%2==0?-1:1)*.65f,-.35f+i*.22f,0),new Vector3(.22f,1.35f,.65f),Color.Lerp(shell,Color.white,.35f));shard.transform.localRotation=Quaternion.Euler(12,i*53,25-i*17);}
            }else if(label.Contains("钳锁")){
                for(int s=-1;s<=1;s+=2){Shape.Beam(root,new Vector3(s*.7f,-1.5f,0),new Vector3(s*.85f,.6f,0),.2f,shell);Shape.Beam(root,new Vector3(s*.85f,.6f,0),new Vector3(s*.25f,.9f,0),.24f,shell);}
            }else{
                Color brass=new Color(.58f,.42f,.23f),rim=new Color(.75f,.64f,.42f);
                Shape.Part("Lantern heel",PrimitiveType.Cylinder,root,new Vector3(0,-.59f,0),new Vector3(.81f,.055f,.81f),shell);
                Shape.Part("Lantern crown",PrimitiveType.Cylinder,root,new Vector3(0,.57f,0),new Vector3(.8f,.065f,.8f),shell);
                CreatureSurfaceArt.Facet(root,"Ventilated lantern cap",new Vector3(0,.67f,0),new Vector3(.66f,.19f,.66f),shell);
                CoastalMesh.Ring(root,new Vector3(0,-.54f,0),.39f,.022f,brass,Quaternion.Euler(90,0,0));
                CoastalMesh.Ring(root,new Vector3(0,.5f,0),.39f,.022f,rim,Quaternion.Euler(90,0,0));
                for(int i=0;i<4;i++){
                    float a=(45+i*90)*Mathf.Deg2Rad;Vector3 side=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));
                    CoastalMesh.Tube("Lantern protective rib",root,new[]{side*.3f+Vector3.down*.56f,side*.46f+Vector3.down*.32f,side*.46f+Vector3.up*.29f,side*.3f+Vector3.up*.55f},new[]{.033f,.024f,.024f,.033f},brass,shell,7);
                    Shape.Part("Lantern rivet",PrimitiveType.Sphere,root,side*.32f+Vector3.up*.59f,Vector3.one*.055f,rim);
                }
                CoastalMesh.Ring(root,new Vector3(0,.86f,0),.1f,.018f,brass,Quaternion.identity);
                if(label.Contains("阀"))CoastalMesh.Ring(root,new Vector3(0,0,.42f),.61f,.055f,brass,Quaternion.identity);
            }
        }
        public void Hit(float damage)
        {
            if(Dead||!game||game.Paused)return;Health-=damage;game.UI.HitMarker(false,damage);game.Effect(transform.position,Color.cyan,4,.07f);game.Audio.Cue("impact");
            if(Health>0)return;Dead=true;destroyed?.Invoke(this);game.Effect(transform.position,Color.cyan,20,.13f);Destroy(gameObject);
        }
        void Update(){if(!game||game.Paused)return;age+=Time.deltaTime;crystal.localRotation=Quaternion.Euler(10,age*42,0);crystal.localPosition=Vector3.up*Mathf.Sin(age*2)*.08f;}
        void OnDestroy(){if(coreMaterial)Destroy(coreMaterial);}
    }
}
