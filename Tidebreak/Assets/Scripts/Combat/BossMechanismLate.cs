using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tidebreak
{
    // Late encounters alternate navigation, timing and precision shooting. These
    // are ordinary hittable targets: QA and human players use the same weapon ray.
    public partial class BossMechanism
    {
        public EncounterTarget MechanismTarget { get; private set; }
        public bool ChargeNeedsRelay { get { return Phase >= 2 && !chargeRelayed; } }
        public float ChargeBudget { get { return (Phase >= 2 ? 7.8f : 4.8f) * (game.Run.easy ? 1.3f : 1); } }
        public bool Whiteout { get { return BossIndex == 5 && Phase >= 2 && age >= blizzardAt && age < blizzardAt + 2.5f; } }
        public bool TargetVulnerable { get; private set; }
        public int WhaleConfirmNode { get { return whaleFirstStation % 3 + 1; } }
        public IReadOnlyList<Vector3> FalseFootprints { get { return falseFootprints; } }
        public float MemoryLockRemaining { get { return Mathf.Max(0, memoryLockUntil - age); } }
        public bool MemoryHasDeparted { get { return memoryDeparted; } }
        public string LateBrief {
            get {
                if(BossIndex<5)return "";
                if(BossIndex==5&&Phase>=2&&!Whiteout&&blizzardAt-age<3.5f)return "暴风雪 "+Mathf.CeilToInt(Mathf.Max(0,blizzardAt-age))+" 秒后抵岸！\n返回热炉四米暖圈，冷风持续 2.5 秒。";
                return MechanicDetail;
            }
        }
        public string LateInput {
            get {
                if(BossIndex<5)return "";
                if(MechanismTarget&&TargetVulnerable)return "2–7 换枪 · 鼠标左键击碎目标";
                if(BossIndex==5)return "炉边按住 E 补热 · 松手伴行";
                if(BossIndex==6)return Carrying?(ChargeNeedsRelay?"中继台按 E 反相":"跑入闪亮接地台"):"中央台按 E 接电";
                if(BossIndex==7)return Pressure>=65?"右侧阀门按 E 泄压":Heat<25?"停止冷却 · 等待温度回到 25":Heat<=45?"温压稳定 · 暂停转阀，准备射击":"左侧阀门按 E 冷却";
                if(BossIndex==8)return StateStep==0?"起点按 E 记录":StateStep==1?"WASD 走出有间隔的新足迹":"WASD 移动 · 逆序收双晶足迹";
                if(BossIndex==9)return Carrying?(!game.Player.RodEquipped?"1 切回鱼竿 · 平潮按住 E 收线":Surge?"松开 E · 横移换侧":Phase>=2?"金圈内按住 E 收线":"平潮按住 E 收线"):"1 切竿 · 瞄准触腕按 E";
                return StateStep==2?"蓝线变橙后横向闪避":"观测台旁按 E";
            }
        }
        public string LateMeter {
            get {
                if(BossIndex<5)return "";
                string progress=CompletedActions+" / "+RequiredActions;
                if(BossIndex==5)return "破冰 "+progress+" · 热量 "+Mathf.CeilToInt(Heat)+" · 融冰 ≥62 / 射击 ≥45";
                if(BossIndex==6)return "回路 "+progress+(Carrying?" · 电荷 "+timer.ToString("F1")+" 秒 · "+(ChargeNeedsRelay?"待反相":"已反相"):" · 指定接地台 "+(selected==1?"西":"东"));
                if(BossIndex==7)return "甲片 "+progress+" · 温度 "+Mathf.CeilToInt(Heat)+" [25–45] · 压力 "+Mathf.CeilToInt(Pressure)+" [<65]";
                if(BossIndex==8)return StateStep==1?"记录 "+route.Count+" / "+RequiredActions:"回收 "+progress+" · 双晶为真 / 单晶为假";
                if(BossIndex==9)return "退腕 "+progress+(MechanismTarget?" · 腕结已暴露":Carrying?" · 收线 "+Mathf.RoundToInt(partial*100)+"% / 张力 "+Mathf.RoundToInt(Tension*100)+"%":" · 等待挂钩");
                return "观测 "+progress+(StateStep==3?" · 第二方位 "+(WhaleConfirmNode==1?"西":WhaleConfirmNode==2?"中":"东"):StateStep==2?" · 破冰 "+(ObservationLeg+1)+" / "+(Phase==3?2:1):" · 双环是真回波");
            }
        }
        public string MechanicDetail {
            get {
                if(!Active)return "破招成功 · 利用暴露窗口反击";
                if(BossIndex==5)return Whiteout?"暴风雪 · 回到热炉四米内":StateStep==2?(TargetVulnerable?"冰锁已解冻 · 换枪击碎":"冰锁再次冻结 · 按住 E 补热") : StateStep==1?"补热至 62 · 融开冰锁":"伴行热炉 · 到站后融冰破锁";
                if(BossIndex==6)return !Carrying?(Phase>=2?"承接 → 反相 → 接地":"承接电荷 → 进入指定接地台"):ChargeNeedsRelay?"电荷未反相 · 中继台按 E":"极性正确 · 进入指定接地台";
                if(BossIndex==7)return Pressure>=65?"压力过高 · 先到右侧泄压阀":Heat<25?"温度过低 · 停止冷却，等待自动回温":MechanismTarget?(TargetVulnerable?"脆甲暴露 · 换枪射击锻台":"温度过高 · 冷却后再射击"):Heat<=45?"温压稳定 · 暂停转阀，淬火两秒":"降低温度至 25–45，再等待淬火";
                if(BossIndex==8)return StateStep==0?"起点按 E 记录路线 · 每步间隔三米":MemoryLockRemaining>0?"镜像干扰 · 离开单晶假足迹":StateStep==1?"真实足迹为双晶 · 记录后先离开终点":!memoryDeparted?"先离开最后一枚足迹三米 · 再开始逆行":"只收双晶真足迹 · 避开单晶倒影";
                if(BossIndex==9)return MechanismTarget?"牵引成功 · 换枪击碎腕结":Phase>=2?"平潮借力 → 红潮换侧 → 换枪断腕":"平潮收线 → 红潮松手 → 换枪断腕";
                if(BossIndex==10)return StateStep==3?"第二观测站交叉确认 · 不可重复原站":Phase>=2?"辨双环 → 交叉确认 → 避开破冰":"辨双环 → 引出破冰 → 侧闪";
                return TargetLabel;
            }
        }

        bool chargeRelayed, memoryDeparted;
        int whaleFirstStation;
        float blizzardAt, nextBlizzardHit, thaw, memoryLockUntil, falseTriggerAt;
        readonly List<Vector3> falseFootprints = new List<Vector3>();
        readonly List<MechanismMarker> falseMarkers = new List<MechanismMarker>();
        MechanismMarker aimMarker;

        void ResetLateMechanics()
        {
            MechanismTarget=null; aimMarker=null; TargetVulnerable=false; chargeRelayed=false;
            whaleFirstStation=0; blizzardAt=12; nextBlizzardHit=0; thaw=0;
            memoryDeparted=false; memoryLockUntil=falseTriggerAt=0;
            falseFootprints.Clear(); falseMarkers.Clear();
        }

        void CreateMechanismTarget(string label, Vector3 point, float health, Color color, Action onBroken)
        {
            MechanismTarget=EncounterTarget.Create(game,point,label,health,color,target=>{
                if(!this||!Active||!Owner||Owner.dead)return;
                MechanismTarget=null;TargetVulnerable=false;
                if(aimMarker!=null){nodes.Remove(aimMarker);if(aimMarker.Root)Destroy(aimMarker.Root.gameObject);aimMarker=null;}
                EnemySkillFX.Burst(Owner,point,1.4f);
                onBroken();
            });
            MechanismTarget.transform.SetParent(stageRoot,true);
            DressMechanismTarget(MechanismTarget);
            Owner.Encounter.Targets.Add(MechanismTarget);
            TargetVulnerable=true;
            // The aiming hull is deliberately outside the boss and apparatus.
            // All decorative geometry is non-colliding and cannot eat the ray.
            AddAt(label,point-Vector3.up*1.8f,color,"target-label");
            aimMarker=nodes[nodes.Count-1];
        }

        void SetMechanismTargetVulnerable(bool vulnerable)
        {
            if(!MechanismTarget)return;
            TargetVulnerable=vulnerable;
            foreach(var c in MechanismTarget.GetComponentsInChildren<Collider>())c.enabled=vulnerable;
            var light=MechanismTarget.GetComponent<Light>();
            if(light){light.intensity=vulnerable?1.2f:.1f;light.color=vulnerable?Gold:Cyan;}
        }

        void TickFrostEscort(float dt)
        {
            var furnace=nodes[0];
            float distance=GameDirector.FlatDistance(Feet,furnace.Position);
            bool heating=held&&distance<3;
            Heat=Mathf.Clamp(Heat+dt*(heating?32:Whiteout?-8:-3.8f),0,100);
            if(Phase>=2){
                if(Whiteout&&age>=nextBlizzardHit){
                    nextBlizzardHit=age+.85f;
                    EnemySkillFX.Burst(Owner,furnace.Position+Vector3.up*3,2.3f);
                    if(distance>4.3f){float before=game.Run.health;game.Player.TakeDamage(8+Phase,Owner.transform.position);if(game.Run.health<before)game.Player.ApplyStatus(SeaTrait.Frost);}
                }
                if(age>=blizzardAt+2.5f)blizzardAt=age+(Phase==3?11:14);
            }
            // A real warm-radius line remains visible during the whole escort;
            // the burst is cosmetic, this boundary is the actual shelter radius.
            var warmth=guides[0];warmth.positionCount=48;warmth.loop=true;
            for(int i=0;i<48;i++){float a=i*Mathf.PI*2/48;Vector3 p=furnace.Position+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*4.3f;p.y=game.World.GroundAt(p)+.17f;warmth.SetPosition(i,p);}
            Shape.TintLine(warmth,Whiteout?Gold:Color.Lerp(Cyan,Gold,Heat/100));
            if(StateStep==0&&distance<4.3f&&!heating&&Heat>20){
                Vector3 goal=route[CompletedActions];
                furnace.Move(Vector3.MoveTowards(furnace.Position,goal,dt*2.1f));
                if(GameDirector.FlatDistance(furnace.Position,goal)<.2f){StateStep=1;thaw=0;checkpoint=furnace.Position;game.Notice("热炉到站 · 补热至 62 融开冰锁，然后换枪击碎",4);}
            }
            if(StateStep==1){
                thaw=Heat>=62?thaw+dt:Mathf.Max(0,thaw-dt*.3f);
                if(thaw>=1.15f){
                    StateStep=2;
                    Vector3 point=furnace.Position+new Vector3(2.1f,2,-.25f);
                    CreateMechanismTarget("解冻冰锁 · 射击",point,42+Phase*9,Cyan,()=>{
                        nodes[CompletedActions+1].Complete();CompletedActions++;StateStep=0;Heat=Mathf.Max(Heat,50);briefExposure=age+2.8f;
                        if(CompletedActions>=RequiredActions)Solve();
                    });
                }
            }
            if(MechanismTarget)SetMechanismTargetVulnerable(Heat>=45);
            if(Heat<=0){Fail("热炉熄火 · 回上个停泊点补热，已破冰锁保留",0);furnace.Move(checkpoint);Heat=22;}
            if(age>=nextDanger){nextDanger=age+6;EnemySkillFX.Warn(Owner,Feet,1.65f,1.8f,17+Phase*2);Owner.NotifyAttackExecuted();}
            Target(furnace,"伴行热炉 · "+(StateStep==0?"保持四米内护送":"按住 E 补热 / 换枪破锁"));
            if(MechanismTarget&&TargetVulnerable){TargetPoint=MechanismTarget.transform.position;TargetLabel="解冻冰锁 · 换枪射击";}
            Cue="破冰 "+CompletedActions+"/"+RequiredActions+" · 热量 "+Mathf.CeilToInt(Heat)+" · "+MechanicDetail+(Phase>=2&&!Whiteout?" · 风雪 "+Mathf.CeilToInt(Mathf.Max(0,blizzardAt-age))+" 秒":"");
            Progress=(CompletedActions+(StateStep==2?.8f:thaw/1.15f*.5f))/RequiredActions;
        }

        void TickPolarityCircuit(float dt)
        {
            int targetIndex=!Carrying?0:ChargeNeedsRelay?3:selected;
            Target(nodes[targetIndex],!Carrying?"蓄电台 · E 承接电荷":ChargeNeedsRelay?"反相中继 · E 翻转极性":"指定接地台 · 进入自动泄放");
            for(int i=1;i<=2;i++)nodes[i].Tint(Carrying&&i==selected&&!ChargeNeedsRelay?Green:Cyan);
            if(nodes.Count>3)nodes[3].Tint(Carrying&&ChargeNeedsRelay?Gold:Cyan);
            Vector3 lineFrom=Carrying?Feet+Vector3.up*.6f:nodes[0].Position+Vector3.up;
            SetGuide(0,lineFrom,nodes[targetIndex].Position+Vector3.up*.6f,ChargeNeedsRelay?Gold:Cyan);
            if(Carrying){
                timer-=dt;
                int pad=GameDirector.FlatDistance(Feet,nodes[1].Position)<2.45f?1:GameDirector.FlatDistance(Feet,nodes[2].Position)<2.45f?2:0;
                if(pad!=0){
                    if(pad!=selected||ChargeNeedsRelay){
                        Carrying=false;Fail(ChargeNeedsRelay?"电荷未反相 · 先到中继按 E，再送指定接地台":"接错电极 · 按亮起导线重新接地；已完成回路保留",EncounterTuning.FailureDamage(BossIndex,Phase)*.55f);
                        EnemySkillFX.Burst(Owner,nodes[pad].Position+Vector3.up,1.7f);
                    }else{
                        Carrying=false;CompletedActions++;briefExposure=age+3;nodes[selected].Flash(Green,1);game.Audio.Cue("arc");EnemySkillFX.Burst(Owner,nodes[selected].Position+Vector3.up,1.8f);
                        selected=selected==1?2:1;
                        if(Phase==3&&nodes.Count>3)nodes[3].Move(Point(CompletedActions%2==0?-4:4,1));
                        if(CompletedActions>=RequiredActions){Solve();return;}
                        if(Phase>=2){ThreatField.RingDelayed(game,nodes[pad].Position,12+Phase*2,Cyan,1.8f,source:Owner);Owner.NotifyAttackExecuted();}
                    }
                }else if(timer<=0){Carrying=false;Fail("电荷失控 · 返回中央重接，已接地回路保留",EncounterTuning.FailureDamage(BossIndex,Phase));EnemySkillFX.Burst(Owner,Feet+Vector3.up,1.5f);}
            }
            if(age>=nextDanger&&!Carrying){nextDanger=age+6;EnemySkillFX.Warn(Owner,Feet,1.8f,1.8f,15+Phase);Owner.NotifyAttackExecuted();}
            Cue="接地 "+CompletedActions+"/"+RequiredActions+" · "+MechanicDetail+(Carrying?" · "+timer.ToString("F1")+" 秒":"");
            CycleProgress=Carrying?Mathf.Clamp01(timer/ChargeBudget):0;Progress=(float)CompletedActions/RequiredActions;
        }

        void TickQuenchForge(float dt)
        {
            Heat=Mathf.Clamp(Heat+dt*(Phase==3?2.4f:2),0,105);
            Pressure=Mathf.Clamp(Pressure+dt*(Heat>45?2.2f:-1.5f),0,105);
            bool balanced=Heat>=25&&Heat<=45&&Pressure<65;
            if(!MechanismTarget){
                partial=balanced?partial+dt:Mathf.Max(0,partial-dt*.25f);
                if(partial>=2){
                    int side=CompletedActions%2==0?-1:1;
                    Vector3 at=nodes[2].Position+new Vector3(side*1.8f,2,0);
                    CreateMechanismTarget("淬火脆甲 · 射击",at,48+Phase*12,Gold,()=>{
                        CompletedActions++;partial=0;Heat=68;Pressure=Mathf.Min(85,Pressure+12);briefExposure=age+2.5f;
                        if(CompletedActions>=RequiredActions)Solve();
                    });
                    game.Notice("脆甲已淬出 · 保持温度 25–45、压力低于 65，换枪击碎锻台上的甲片",4);
                }
            }
            SetMechanismTargetVulnerable(balanced);
            if(Heat>=100||Pressure>=100){Fail("炉膛失控 · 退回半秒淬火，已击碎甲片保留",EncounterTuning.FailureDamage(BossIndex,Phase));EnemySkillFX.Burst(Owner,Feet+Vector3.up*.6f,.65f);partial=Mathf.Max(0,partial-.5f);Heat=68;Pressure=35;EnemySkillFX.Warn(Owner,Feet,2,1.4f,12);}
            if(Pressure>=65)Target(nodes[1],"泄压阀 · E 降压");
            else if(Heat<25)Target(nodes[2],"停止冷却 · 等待温度自动回到 25");
            else if(Heat<=45)Target(nodes[2],"温压稳定 · 暂停转阀，等待脆甲");
            else Target(nodes[0],"冷却阀 · E 降温");
            if(MechanismTarget&&TargetVulnerable){TargetPoint=MechanismTarget.transform.position;TargetLabel="淬火脆甲 · 现在换枪击碎";}
            Cue="破甲 "+CompletedActions+"/"+RequiredActions+" · 温度 "+Mathf.CeilToInt(Heat)+" [25–45] · 压力 "+Mathf.CeilToInt(Pressure)+" [<65] · "+MechanicDetail;
            nodes[0].Tint(balanced?Green:Cyan);nodes[1].Tint(Pressure>59?Red:Gold);nodes[2].Tint(TargetVulnerable?Green:Red);
            Progress=(CompletedActions+Mathf.Clamp01(partial/2)*.5f)/RequiredActions;
            if(age>=nextDanger){
                nextDanger=age+7;
                // The valve being used vents a narrow, explicit steam jet. Its
                // perpendicular escape lane remains open, so no unavoidable trap.
                Vector3 from=nodes[CompletedActions%2].Position;
                ThreatField.Line(game,from+Vector3.back*3,from+Vector3.forward*9,.8f,1.8f,16+Phase,Red,source:Owner);
                Owner.NotifyAttackExecuted();
            }
        }

        void OpenKrakenBinding()
        {
            int arm=selected;
            nextDanger=age+3.5f;
            Vector3 at=nodes[arm].Position+new Vector3(0,1.9f,-1.1f);
            CreateMechanismTarget("露岸腕结 · 射击",at,54+Phase*12,new Color(.85f,.5f,.69f),()=>{
                done[arm]=true;nodes[arm].Complete();CompletedActions++;briefExposure=age+2.6f;game.Audio.Cue("explosion");
                if(CompletedActions>=RequiredActions)Solve();
            });
            game.Notice("触腕被牵至浅滩 · 换枪击碎露出的腕结，断腕后再挂下一腕",4);
        }

        void BeginObservationBreach()
        {
            StateStep=2;ObservationLeg=0;timer=0;
            ResetObservationVisuals();
            game.Notice("真航线已确认 · 引出破冰，蓝线变橙后横向侧闪！",4);
            EnemySkillFX.Burst(Owner,nodes[Phase>=2?WhaleConfirmNode:selected+1].Position+Vector3.up,1.4f);
            TrackingBreach.Create(game,Owner.transform.position,EncounterTuning.FailureDamage(BossIndex,Phase),0,ResolveObservation);
            Owner.NotifyAttackExecuted();
        }

        void ResetObservationVisuals()
        {
            if(guides.Count>0)guides[0].enabled=false;
            for(int i=1;i<=3&&i<nodes.Count;i++)nodes[i].Tint(Gold);
            foreach(var n in nodes)if(n.Kind=="echo")n.Root.gameObject.SetActive(false);
        }

        void BuildFalseFootprints()
        {
            if(Phase<2)return;
            for(int i=0;i<route.Count;i++){
                // Pick an adjacent reflection that never overlaps any real step.
                // This keeps the route player-authored without unavoidable traps.
                for(int attempt=0;attempt<12;attempt++){
                    float a=(i*83+attempt*30)*Mathf.Deg2Rad;
                    Vector3 p=route[i]+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*3.5f;
                    if(Mathf.Abs(p.x)>11||p.z<-7||p.z>13)continue;
                    bool clear=true;foreach(var real in route)if(GameDirector.FlatDistance(real,p)<2.8f){clear=false;break;}
                    foreach(var other in falseFootprints)if(GameDirector.FlatDistance(other,p)<2.6f){clear=false;break;}
                    if(!clear)continue;
                    p.y=game.World.GroundAt(p)+.12f;falseFootprints.Add(p);
                    falseMarkers.Add(MechanismMarker.Create(stageRoot,p,"单晶倒影 · 勿踏",new Color(.77f,.42f,.81f),"false-memory",i+1));break;
                }
            }
        }

        bool TickMemoryReflection(float dt)
        {
            foreach(var marker in falseMarkers)marker.Tick(dt,game.Player.View.transform);
            if(!memoryDeparted){
                Vector3 last=route[route.Count-1],previous=route[route.Count-2];
                Vector3 direction=previous-last;direction.y=0;direction.Normalize();
                // Give the approach marker sufficient margin beyond the 2.7m
                // departure radius. A 3m marker was inside a normal .55m arrival
                // tolerance and could leave both players and drivers stuck.
                Vector3 departure=last+direction*4.2f;
                if(game.World.GroundAt(departure)<.4f)departure=previous;
                departure.y=game.World.GroundAt(departure)+.12f;
                TargetPoint=departure;
                TargetLabel="沿来路先退离终点三米，再折返逆行";
                Cue="记录结束 · 沿来路走离终点三米，解除镜像定格，再折返回收";
                if(GameDirector.FlatDistance(Feet,route[route.Count-1])>2.7f)memoryDeparted=true;
                else return false;
            }
            if(age>=falseTriggerAt)foreach(var p in falseFootprints)if(GameDirector.FlatDistance(Feet,p)<1.15f){
                falseTriggerAt=age+2.2f;memoryLockUntil=age+1.1f;
                Fail("踩中单晶倒影 · 先离开镜刃，真足迹没有丢失",0);
                ThreatField.Line(game,p-Vector3.right*4,p+Vector3.right*4,.7f,1.15f,16+Phase,Red,source:Owner);
                Owner.NotifyAttackExecuted();
                EnemySkillFX.Burst(Owner,p+Vector3.up,1.5f);break;
            }
            return age>=memoryLockUntil;
        }
    }
}
