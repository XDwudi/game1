using UnityEngine;

namespace Tidebreak
{
    // Island ecology changes the decision the player makes, independently of body-family skills.
    public sealed class RegionalTactics : MonoBehaviour
    {
        Enemy owner;GameDirector game;EncounterTarget device,secondDevice;
        int actions;float ready,resolveAt,guardUntil;Vector3 committed,origin;LineRenderer tether;
        bool repairUsed;
        public int Activations {get;private set;}
        public string Cue {get;private set;}
        public bool Active {get{return resolveAt>Time.time||Time.time<guardUntil;}}
        public void Init(Enemy enemy){owner=enemy;game=enemy.game;}
        public float DamageFactor(bool weak){return !weak&&Time.time<guardUntil?.68f:1;}
        public void OnHit(bool weak){if(weak&&Time.time<guardUntil){guardUntil=0;Cue="珊瑚甲裂开 · 继续攻击";game.Effect(owner.transform.position,Color.yellow,8,.065f);}}
        public void AfterAttack(Vector3 aim)
        {
            if(owner.Minion||Time.time<ready)return;
            actions++;
            ready=Time.time+(owner.elite?7:9);committed=aim;origin=owner.transform.position;
            var style=owner.Spec.ecology;float damage=(owner.elite?12:7)+owner.CombatIsland;
            if(style==EcologyStyle.TideRunner){
                Vector3 side=Vector3.Cross((aim-origin).normalized,Vector3.up);Vector3 flank=origin+side*(actions%2==0?3:-3);
                if(game.World.GroundAt(flank)<0)return;owner.Lunge(flank,.28f);Cue="借潮侧步 · 重新瞄准";
            }else if(style==EcologyStyle.ReefSentinel){
                guardUntil=Time.time+2.6f;Cue="珊瑚护甲 · 射击发光弱点破甲";EnemySkillFX.For(game).Guard(owner,origin);
            }else if(style==EcologyStyle.RootStalker){
                device=Target("根芽 · 射断可取消缠击",Ground(aim+owner.transform.right*2),28+owner.CombatIsland*3,new Color(.7f,1,.35f));
                resolveAt=Time.time+2.5f;Cue="根芽即将发射 · 射断发光种子";
            }else if(style==EcologyStyle.DuneBurrower){
                EnemySkillFX.Warn(owner,aim,1.7f,1.35f,damage,SkillTheme.SaltCrystal);EnemySkillFX.Warn(owner,origin,2,1.85f,damage,SkillTheme.SaltCrystal);Cue="双重沙陷 · 离开旧落点";
            }else if(style==EcologyStyle.WreckScavenger){
                if(repairUsed||owner.health>owner.maxHealth*.7f)return;repairUsed=true;
                device=Target("回收浮灯 · 击碎阻止修复",Ground(origin-owner.transform.forward*2),32+owner.CombatIsland*3,new Color(.35f,1,.72f));resolveAt=Time.time+3;Cue="回收修复 · 三秒内击碎浮灯";
            }else if(style==EcologyStyle.FrostDrifter){
                ThreatField.Pool(game,aim-owner.transform.right*1.8f,1.3f,1.3f,damage*.6f,new Color(.4f,.83f,1),SeaTrait.Frost,source:owner,theme:SkillThemes.ForEcology(owner.Spec.ecology));Cue="冻结侧路 · 从另一侧移动";
            }else if(style==EcologyStyle.StormConductor){
                Vector3 side=Vector3.Cross((aim-origin).normalized,Vector3.up);if(side.sqrMagnitude<.1f)side=Vector3.right;
                device=Target("导流角 A · 击碎任一端断电",Ground(aim-side*3),30,Color.cyan);secondDevice=Target("导流角 B · 击碎任一端断电",Ground(aim+side*3),30,Color.cyan);
                var obj=new GameObject("Charging conductor link");obj.transform.SetParent(transform,false);tether=obj.AddComponent<LineRenderer>();tether.material=Shape.Mat(new Color(1,.75f,.3f),true);tether.startWidth=tether.endWidth=.055f;resolveAt=Time.time+2.2f;Cue="双角导电 · 射断一端，或离开连线";
            }else if(style==EcologyStyle.CinderHunter){
                EnemySkillFX.Warn(owner,origin,2.1f,1.6f,damage,SkillTheme.Cinder);owner.Lunge(aim,.25f);Cue="留下热壳 · 离开它刚站过的位置";
            }else if(style==EcologyStyle.MirrorDancer){
                Vector3 side=owner.transform.right*(actions%2==0?3:-3);Vector3 mark=Ground(origin+side);EnemySkillFX.Burst(game,SkillTheme.Mirror,mark,.65f,owner.Spec.id);
                device=Target("镜像锚 · 击碎可阻止回声水刃",mark,34,new Color(.55f,.95f,1));resolveAt=Time.time+2.3f;Cue="镜像锚 · 射断锚点，或闪避第二道水刃";
            }
            Activations++;if(owner.elite)game.Notice(Cue,2.4f);
        }
        Vector3 Ground(Vector3 p){p.y=Mathf.Max(game.World.GroundAt(p),-.3f)+1.2f;return p;}
        EncounterTarget Target(string name,Vector3 point,float hp,Color color)
        {return EncounterTarget.Create(game,point,name,hp,color,t=>{if(device==t)device=null;if(secondDevice==t)secondDevice=null;Cue="生态装置已破坏 · 后续攻击取消";game.Audio.Cue("weak");});}
        void Update()
        {
            if(!owner||owner.dead||game.Paused||game.State!=VoyageState.Combat)return;
            if(tether){if(device&&secondDevice)ThreatField.DrawSurfaceLine(tether,game.World,device.transform.position,secondDevice.transform.position);else Destroy(tether.gameObject);}
            if(resolveAt<=0||Time.time<resolveAt)return;resolveAt=0;
            if(device){
                switch(owner.Spec.ecology){
                    case EcologyStyle.RootStalker:EnemySkillFX.Projectile(owner,device.transform.position,committed,7,10+owner.CombatIsland,SkillTheme.VenomRoot);break;
                    case EcologyStyle.WreckScavenger:owner.health=Mathf.Min(owner.maxHealth,owner.health+owner.maxHealth*.12f);EnemySkillFX.For(game).Action(owner,AttackStyle.Heal,owner.transform.position);break;
                    case EcologyStyle.StormConductor:if(secondDevice)ThreatField.Line(game,device.transform.position,secondDevice.transform.position,.7f,.85f,14+owner.CombatIsland,Color.cyan,source:owner,theme:SkillThemes.ForEcology(owner.Spec.ecology));break;
                    case EcologyStyle.MirrorDancer:EnemySkillFX.Projectile(owner,device.transform.position,committed,8,12,SkillTheme.Mirror);break;
                }
            }
            ClearDevices();
        }
        void ClearDevices(){if(device)Destroy(device.gameObject);if(secondDevice)Destroy(secondDevice.gameObject);if(tether)Destroy(tether.gameObject);}
        void OnDestroy(){ClearDevices();}
    }
}
