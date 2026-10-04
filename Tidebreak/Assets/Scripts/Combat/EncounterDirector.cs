using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tidebreak
{
    // Each boss owns a moveset, phase gates and recovery windows. Health is never scaled to player gear.
    public class EncounterDirector : MonoBehaviour
    {
        public Enemy Owner { get; private set; }
        public int Phase { get; private set; } = 1;
        public int Moves { get; private set; }
        public int GatesBroken { get; private set; }
        public int SeveredArms { get; private set; }
        public int SeveredArmMask { get; private set; }
        public int KrakenSlamCount {get{return Mathf.Max(2,2+Phase-SeveredArms);}}
        public string Cue { get; private set; }
        public float Windup { get { return pending == null ? 0 : Mathf.Clamp01(1 - (executeAt - Time.time) / tellDuration); } }
        public bool Recovering { get { return Time.time < recoveryUntil && Targets.Count == 0; } }
        public float Submerge {get{return bossIndex==10&&pending!=null&&(turn-1)%3==0?Mathf.Sin(Windup*Mathf.PI)*2.4f:0;}}
        public float PosePitch {get{return bossIndex==10?((turn-1)%3==2?-18:12)*Windup:bossIndex==9?-4*Windup:0;}}
        public float FacingOffset {get{return bossIndex==10&&Recovering?Mathf.Sin(Time.time*.65f)*48:0;}}
        public readonly List<EncounterTarget> Targets = new List<EncounterTarget>();
        public float DamageFactor { get { return Targets.Count > 0 ? 0 : Recovering ? 1.35f : .48f; } }
        public static readonly string[] Mechanics = {
            "裂钳交叉：击碎两只钳锁，横移避开夹击；砸地后腹部暴露。",
            "逆音潮环：跳过声波，摧毁共鸣贝；双环之间保留一次跳跃。",
            "毒根潜袭：清除毒囊释放安全地面；尾迹锁定后再冲刺。",
            "日轮石甲：打碎两块日轮碎片；躲开棋盘轰击后攻击头部。",
            "沉钟牵引：先断锚链再躲回旋刃；鸣钟后的停顿是输出窗口。",
            "冰脊猎线：碎冰开放逃生通道；侧移躲冲锋，跳过落地冲击。",
            "雷极轮转：击毁接地电容，离开交叉雷线；放电后核心过载。",
            "熔甲锻炉：破坏冷却阀使甲壳骤冷；熔池会封锁旧站位。",
            "镜渊回响：击碎过去的三个锚点；记录你的位置，再依序重演。",
            "八腕封海：斩断登陆触腕，跳过潮环；第三阶段触腕与墨潮交替。",
            "白鲸追忆：击碎冰封声呐，引出潜航白鲸；双向破冰冲锋后反击。"
        };
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
            if (bossIndex == 0) CreateGate(2, "钳锁", new Color(1,.63f,.2f));
        }
        void Update()
        {
            if (!Owner || Owner.dead || game.State != VoyageState.Combat || game.Paused) return;
            Targets.RemoveAll(t => !t);
            if(Time.time>=recordAt){recordAt=Time.time+.8f;recentPositions.Enqueue(game.Player.transform.position);while(recentPositions.Count>4)recentPositions.Dequeue();}
            int desired = Owner.health <= Owner.maxHealth * .32f ? 3 : Owner.health <= Owner.maxHealth * .67f ? 2 : 1;
            if (desired > Phase) ChangePhase(desired);
            Owner.phase = Phase;
            if (pending != null && Time.time >= executeAt)
            {
                var action = pending; pending = null; action(); Moves++; Owner.NotifyAttackExecuted();
                recoveryUntil = Mathf.Max(recoveryUntil,Time.time + (Phase == 3 ? 2.1f : 2.8f));
                nextMove = recoveryUntil + (Targets.Count > 0 ? .15f : .7f);
            }
            if (Targets.Count > 0 && Time.time >= nextPressure)
            {
                nextPressure = Time.time + 5;
                game.Warn(game.Player.transform.position, 1.65f, 1.5f, 14 + bossIndex);
            }
            if (pending == null && Time.time >= nextMove) ScheduleMove();
        }
        void ChangePhase(int phase)
        {
            Phase = phase; pending = null; turn = 0; recoveryUntil = 0; SeveredArms=SeveredArmMask=0;
            ClearTargets();
            string[] names = { "钳锁", "共鸣贝", "毒囊", "日轮碎片", "锚链扣", "冰脊", "接地电容", "冷却阀", "记忆锚点", "登陆触腕", "冰封声呐" };
            CreateGate(bossIndex >= 8 ? 3 : 2, names[bossIndex], Owner.Spec.color);
            Cue = "第 " + Phase + " 阶段 · " + Mechanics[bossIndex];
            game.Notice(Cue, 6); game.Audio.Cue("boss"); nextMove = Time.time + 2;
        }
        public float ClampPhaseDamage(float amount)
        {
            // A huge critical hit cannot skip the next encounter phase.
            float floor = Targets.Count > 0 ? Owner.health : Phase == 1 ? Owner.maxHealth * .665f : Phase == 2 ? Owner.maxHealth * .315f : 0;
            return Mathf.Min(amount, Mathf.Max(0, Owner.health - floor));
        }
        void CreateGate(int count, string label, Color color)
        {
            if(bossIndex==9)color=new Color(.83f,.38f,.72f);
            for (int i = 0; i < count; i++)
            {
                int armIndex=i*2+1;
                Vector3 at = new Vector3((i-(count-1)*.5f)*6, 0, i%2==0 ? 5 : -1);
                at.y = game.World.GroundAt(at) + 1.8f;
                var t = EncounterTarget.Create(game, at, label + " " + (i+1), 48 + bossIndex*7 + Phase*9, color,
                    target => { Targets.Remove(target); GatesBroken++; if(bossIndex==9){SeveredArms++;SeveredArmMask|=1<<armIndex;game.Notice("触腕已退缩 · 本阶段拍岸连击减至 "+KrakenSlamCount+" 段",3);} if (Targets.Count == 0) { pending=null; recoveryUntil = Time.time + 7; nextMove = Time.time+(bossIndex==0?1:2.5f); Cue = "破招成功 · 核心暴露 7 秒，仍需躲避反扑"; game.Notice(Cue,4); game.Audio.Cue("weak"); } });
                Targets.Add(t);
            }
            nextPressure = Time.time + 3;
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
                    if(move==0)Tell("毒根追踪 · 三段尾迹，沿弧线撤离",.9f,()=>{for(int i=0;i<3;i++)ThreatField.Pool(game,lockedPosition+across*(i-1)*3,1.8f,.8f+i*.4f,damage*.38f,Color.green);});
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
        GameDirector game;Action<EncounterTarget> destroyed;Transform crystal;float age;
        public static EncounterTarget Create(GameDirector game,Vector3 p,string name,float hp,Color color,Action<EncounterTarget> onBreak)
        {
            var root=new GameObject(name);root.transform.position=p;
            var t=root.AddComponent<EncounterTarget>();t.game=game;t.Label=name;t.Health=t.Maximum=hp;t.destroyed=onBreak;
            var part=Shape.Part("Breakable core",PrimitiveType.Sphere,root.transform,Vector3.zero,new Vector3(.85f,1.2f,.85f),color,true,true);t.crystal=part.transform;
            CoastalMesh.Ring(root.transform,Vector3.zero,.7f,.055f,color,Quaternion.identity);
            BuildSilhouette(root.transform,name,color);
            var light=root.AddComponent<Light>();light.color=color;light.intensity=1.2f;light.range=4;
            return t;
        }
        static void BuildSilhouette(Transform root,string label,Color color)
        {
            Color shell=Color.Lerp(color,new Color(.15f,.2f,.22f),.6f);
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
                Shape.Part("Anchor plinth",PrimitiveType.Cylinder,root,new Vector3(0,-1.3f,0),new Vector3(1.3f,.2f,1.3f),shell);
                Shape.Beam(root,new Vector3(0,-1.2f,0),new Vector3(0,-.5f,0),.16f,shell);
                for(int s=-1;s<=1;s+=2)Shape.Beam(root,new Vector3(s*.7f,-.7f,0),new Vector3(s*.7f,.65f,0),.09f,shell);
                if(label.Contains("阀"))CoastalMesh.Ring(root,new Vector3(0,0,.35f),.8f,.09f,shell,Quaternion.identity);
            }
        }
        public void Hit(float damage)
        {
            if(Dead||!game||game.Paused)return;Health-=damage;game.UI.HitMarker(false,damage);game.Effect(transform.position,Color.cyan,4,.07f);game.Audio.Cue("impact");
            if(Health>0)return;Dead=true;destroyed?.Invoke(this);game.Effect(transform.position,Color.cyan,20,.13f);Destroy(gameObject);
        }
        void Update(){if(!game||game.Paused)return;age+=Time.deltaTime;crystal.localRotation=Quaternion.Euler(10,age*42,0);crystal.localPosition=Vector3.up*Mathf.Sin(age*2)*.08f;}
    }
}
