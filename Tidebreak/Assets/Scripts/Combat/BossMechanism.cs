using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tidebreak
{
    // These are encounter objectives, not destructible health gates. Every action below
    // checks the same position/equipment/timing conditions for players and test drivers.
    public partial class BossMechanism : MonoBehaviour
    {
        public Enemy Owner { get; private set; }
        public int BossIndex { get; private set; }
        public int Phase { get; private set; }
        public bool Active { get; private set; }
        public string Cue { get; private set; }
        public string TargetLabel { get; private set; }
        public Vector3 TargetPoint { get; private set; }
        public float Progress { get; private set; }
        public int CompletedActions { get; private set; }
        public int Failures { get; private set; }
        public int RequiredActions { get; private set; }
        public int StateStep { get; private set; }
        public int ObservationLeg {get;private set;}
        public float CycleProgress { get; private set; }
        public float Heat { get; private set; }
        public float Pressure { get; private set; }
        public float Tension { get; private set; }
        public bool Surge { get; private set; }
        public Vector3 HookPoint { get { return nodes.Count>0?nodes[Mathf.Clamp(selected,0,nodes.Count-1)].Position+Vector3.up*1.4f:transform.position; } }
        public Vector3 PullPoint { get { var p=HookPoint-Vector3.up*1.4f+Vector3.back*6+Vector3.right*(pullSide*3);p.y=game.World.GroundAt(p)+.14f;return p; } }
        public bool Braced { get { return BossIndex!=9||Phase<2||GameDirector.FlatDistance(Feet,PullPoint)<1.85f; } }
        public bool Carrying { get; private set; }
        public bool Listening { get; private set; }
        public int CurrentSignal { get; private set; }
        public bool TargetLocked { get { return locked; } }
        public bool TimingWindowOpen { get { return BossIndex==4 && timer>=3.5f && timer<=4.7f; } }
        public float ActionSeconds { get { return timer; } }
        public int CurrentTargetIndex { get { return selected; } }
        public IReadOnlyList<MechanismMarker> Nodes { get { return nodes; } }
        public IReadOnlyList<int> Melody { get { return melody; } }
        public IReadOnlyList<Vector3> RecordedRoute { get { return route; } }
        public string Status { get { return Cue; } }
        public string ActionLabel { get { string value; return TryInteraction(out value) ? value : TargetLabel; } }
        public float ExposureDuration { get { return EncounterTuning.ExposureDuration(BossIndex,Phase); } }
        public float ExposureMultiplier { get { return EncounterTuning.ExposureMultiplier(BossIndex); } }
        public bool ShouldRunAttackPattern { get { return !Active || BossIndex==2 || BossIndex==5 || BossIndex==7 || BossIndex==8; } }
        public float DamageMultiplier {
            get {
                if(!Active)return ExposureMultiplier;
                switch(BossIndex){
                    case 0:return 0;
                    case 10:return age<briefExposure?1.15f:0;
                    case 1:return age<briefExposure?1.25f:.25f;
                    case 2:return .2f+CompletedActions*.22f;
                    case 3:return .1f+CompletedActions*.2f;
                    case 4:return age<briefExposure?1.3f:.3f;
                    case 5:return Heat>70?.6f:.2f;
                    case 6:return age<briefExposure?1.4f:Carrying?.9f:.25f;
                    case 7:return Heat>=25&&Heat<=45&&Pressure<65?1.35f:.2f;
                    case 8:return StateStep==2?.7f:.2f;
                    case 9:return .18f+CompletedActions*.24f;
                    default:return .3f;
                }
            }
        }
        public Vector3 PoseOffset {
            get {
                if(BossIndex==0&&age<strikeUntil){float t=1-(strikeUntil-age)/.6f;return strikeOffset*Mathf.Sin(Mathf.Clamp01(t)*Mathf.PI);}
                if(BossIndex==5&&Active&&nodes.Count>0)return new Vector3(nodes[0].Position.x*.35f,0,0);
                if(BossIndex==10)return new Vector3(Active?(selected-1)*10:Mathf.Sin(age*.8f)*2,0,0);
                return Vector3.zero;
            }
        }
        public float PosePitch {get{return BossIndex==0&&age<strikeUntil?-22:BossIndex==4?Mathf.Sin(timer*3)*5:BossIndex==10&&!Active?-12:0;}}
        public float FacingOffset {get{return BossIndex==10&&!Active?Mathf.Sin(age*.8f)*38:0;}}
        public float Submerge {get{return BossIndex==10&&Active&&age>=briefExposure?7:0;}}
        public readonly int[] MirrorTurns = new int[3];
        public readonly int[] MirrorSolution = new int[3];
        public static readonly string[] Instructions = {
            "引钳撞桩：站在木桩处等蓝线锁定变橙，再侧闪，让重钳撞断桩。",
            "听音回应：看清三只音贝的亮灯顺序，走近后按 E 依序回应；屏幕保留完整乐谱。",
            "净水造岸：在净水泵按 E 取水，走进污染圈按住 E 净化；绿圈能隔绝毒潮。",
            "折光破甲：走近反光镜按 E 转动，将连续光路从日轮接到龟甲；光束指明去向。",
            "拉钟截潮：留在悬钟附近，在钟摆变绿时按 E 拉绳；提前拉无效，迟到会迎来潮击。",
            "护火破冰：靠近热炉伴行，按住 E 停车补热；低温停走，躲开落冰后回来继续。",
            "导雷接地：到蓄电台按 E 承接电荷，在倒计时结束前跑进闪亮接地圈。",
            "双阀淬甲：两侧阀门按 E 冷却 / 泄压；温度 25–45、压力低于 65 时淬火累积。",
            "逆行归忆：按 E 开始记录，走出有间距的足迹，再沿自己的路线倒着回收回声。",
            "钓竿牵腕：按 1 切竿，瞄准触腕按 E 挂钩；平潮收线，红潮松手。第二阶段起，换到金圈才拉得动。",
            "声呐追鲸：发脉冲辨认双环真回波，在对应观测点锁定；引出破冰后侧闪，才会保留观测结果。"
        };

        GameDirector game;
        Action solved;
        Transform stageRoot;
        readonly List<MechanismMarker> nodes = new List<MechanismMarker>();
        readonly List<Vector3> route = new List<Vector3>();
        readonly List<int> melody = new List<int>();
        readonly bool[] done = new bool[6];
        readonly List<LineRenderer> guides = new List<LineRenderer>();
        float age, timer, nextDanger, actionCooldown, partial, lastRouteSample, briefExposure, strikeUntil;
        bool held, locked,wasArmSurge;
        Vector3 lockedPoint, checkpoint, originalPosition, strikeOffset;
        int selected, replayIndex, pullSide=1, tunePlayed = -1;
        AudioSource noteSource;
        AudioClip[] notes;
        static readonly Color Gold = new Color(1, .72f, .25f);
        static readonly Color Cyan = new Color(.2f, .88f, .88f);
        static readonly Color Red = new Color(1, .27f, .18f);
        static readonly Color Green = new Color(.38f, .96f, .55f);

        public void Init(Enemy enemy, Action onSolved)
        {
            Owner = enemy; game = enemy.game; BossIndex = enemy.Spec.id - 108; solved = onSolved; originalPosition=enemy.transform.position;
        }
        public void BeginPhase(int phase)
        {
            ClearStage(); Phase = phase; Active = true; Cue = Instructions[BossIndex];
            CompletedActions = 0; StateStep = ObservationLeg = 0; Progress = CycleProgress = 0;
            age = timer = partial = 0; nextDanger = 5; actionCooldown = briefExposure = strikeUntil = 0;
            Carrying = Listening = locked = held = wasArmSurge = false; Heat = 78; Pressure = 35; Tension = .12f;
            selected = replayIndex = 0; route.Clear(); melody.Clear(); Array.Clear(done, 0, done.Length);
            stageRoot = new GameObject("Boss mechanism " + Owner.Spec.id + " phase " + phase).transform;
            // Kept outside Hazards so a late minion cleanup cannot erase an unfinished objective.
            switch (BossIndex) {
                case 0:
                    RequiredActions = phase == 1 ? 1 : 2;
                    Add("引钳桩", -6, 3, Gold, "stake"); Add("引钳桩", 6, 3, Gold, "stake");
                    AddLine(Cyan); timer = 2.3f; break;
                case 1:
                    RequiredActions = phase + 2;
                    Add("低音贝", -7, 2, Cyan, "shell"); Add("中音贝", 0, -5, Gold, "shell"); Add("高音贝", 7, 2, new Color(.9f,.5f,.9f), "shell");
                    for (int i = 0; i < RequiredActions; i++) melody.Add(((game.Run.seed + phase * 7 + i * 5 + i / 2) % 3+3)%3);
                    Listening = true; timer = 0; BuildNotes(); break;
                case 2:
                    RequiredActions = phase == 1 ? 2 : 3;
                    Add("净水泵", 0, -4, Cyan, "pump");
                    Add("污染环", -7, 4, Red, "pool"); Add("污染环", 7, 4, Red, "pool");
                    if (RequiredActions == 3) Add("污染环", 0, 9, Red, "pool");
                    break;
                case 3:
                    RequiredActions = 3;
                    Add("反光镜", -7, -3, Gold, "mirror"); Add("反光镜", 0, 4, Gold, "mirror"); Add("反光镜", 7, -3, Gold, "mirror");
                    for (int i = 0; i < 3; i++) { MirrorTurns[i] = 0; MirrorSolution[i] = i < phase ? (phase + i) % 3 + 1 : 0; AddLine(Gold); }
                    break;
                case 4:
                    RequiredActions = phase;
                    Add("悬钟拉绳", 0, 0, Gold, "bell"); timer = 0; break;
                case 5:
                    RequiredActions = phase + 1;
                    Add("伴行热炉", -7, -3, Gold, "brazier"); checkpoint = nodes[0].Position;
                    for (int i = 0; i < RequiredActions; i++) route.Add(Point(i % 2 == 0 ? 6 : -6, 2 + i * 2.8f));
                    foreach (var p in route) AddAt("破冰停泊点", p, Cyan, "waypoint");
                    Heat = 78; break;
                case 6:
                    RequiredActions = phase;
                    Add("蓄电台", 0, -4, Gold, "coil"); Add("西接地圈", -8, 5, Cyan, "ground"); Add("东接地圈", 8, 5, Cyan, "ground");
                    selected = phase % 2 + 1; break;
                case 7:
                    RequiredActions = 3 + phase * 2;
                    Add("冷却阀", -5.5f, 0, Cyan, "valve"); Add("泄压阀", 5.5f, 0, Gold, "valve"); Heat = 68; break;
                case 8:
                    RequiredActions = phase + 2;
                    Add("记忆起点", 0, -3, Cyan, "memory"); break;
                case 9:
                    RequiredActions = phase;
                    for (int i = 0; i < RequiredActions; i++) Add("登陆触腕", (i - (RequiredActions - 1) * .5f) * 7, 5, new Color(.8f,.4f,.72f), "arm");
                    AddLine(Cyan); AddLine(Gold); guides[1].positionCount=40;guides[1].loop=true;guides[1].enabled=false;break;
                case 10:
                    RequiredActions = phase == 3 ? 3 : 2;
                    Add("发射声呐台", 0, -4, Cyan, "sonar");
                    Add("西观测点", -8, 6, Gold, "sonar"); Add("中观测点", 0, 8, Gold, "sonar"); Add("东观测点", 8, 6, Gold, "sonar");
                    for (int i = 0; i < 3; i++) AddAt("海面回波", new Vector3((i - 1) * 11, 4, 34), Cyan, "echo");
                    selected = ((game.Run.seed + phase) % 3+3)%3; foreach (var n in nodes) if(n.Kind == "echo") n.Root.gameObject.SetActive(false);
                    break;
            }
            TargetPoint = nodes[0].Position; TargetLabel = nodes[0].Label;
            game.Notice("第 " + phase + " 阶段 · " + Instructions[BossIndex], 7);
        }

        public void SetActionHeld(bool value) { held = value; }
        public bool TryInteraction(out string prompt)
        {
            prompt = ""; if (!Active || !game || game.State != VoyageState.Combat) return false;
            int near = Nearest(3.1f);
            if (BossIndex == 0) return false;
            if (BossIndex == 9) { prompt = Carrying ? Surge?"松开 E · 换侧避开横扫":Braced?"按住 E · 收线":"进入金色牵引圈再收线" : "按 1 切换鱼竿 · 瞄准触腕按 E 挂钩"; return true; }
            if (BossIndex == 8 && StateStep == 1) return false;
            if (near < 0) return false;
            prompt = "E · " + nodes[near].Label;
            if (BossIndex == 2 && near > 0) prompt = Carrying ? "按住 E · 净化污染环" : "先到净水泵按 E 取水";
            if (BossIndex == 5) prompt = "按住 E · 停车补热，松手继续伴行";
            if (BossIndex == 4) prompt = "E · 钟摆绿色时拉绳";
            if (BossIndex == 1 && Listening) prompt = "聆听 / 观察亮灯顺序，结束后回应";
            return true;
        }
        public bool UseAction()
        {
            if (!Active || game.Paused || game.State != VoyageState.Combat || age < actionCooldown) return false;
            int near = Nearest(3.1f); actionCooldown = age + .28f;
            switch (BossIndex) {
                case 1:
                    if (Listening || near < 0) return false;
                    PlayNote(near);
                    if (near != melody[CompletedActions]) { Fail("回应错拍 · 本轮乐谱重新播放，准备再试", EncounterTuning.FailureDamage(BossIndex,Phase)); CompletedActions = 0; Listening = true; timer = 0; tunePlayed = -1; }
                    else { CompletedActions++; briefExposure=age+1.2f; nodes[near].Flash(Green, .45f); if (CompletedActions == RequiredActions) Solve(); }
                    return true;
                case 2:
                    if (near == 0) { Carrying = true; game.Audio.Cue("splash"); game.Notice("净水已装满 · 到红色污染圈按住 E", 3); return true; }
                    return near > 0 && Carrying;
                case 3:
                    if (near < 0) return false;
                    MirrorTurns[near] = (MirrorTurns[near] + 1) % 4; game.Audio.Cue("ready"); return true;
                case 4:
                    if (near != 0) return false;
                    if (timer >= 3.5f && timer <= 4.7f) { CompletedActions++; briefExposure=age+2.5f; game.Audio.Cue("boss"); game.Effect(nodes[0].Position + Vector3.up, Gold, 16, .18f); timer = -1.5f; if (CompletedActions == RequiredActions) Solve(); }
                    else { Fail("空拉 · 等钟摆亮绿后再拉，不会丢失已成功节拍", 0); actionCooldown = age + .9f; }
                    return true;
                case 5: return near == 0;
                case 6:
                    if (near != 0 || Carrying) return false;
                    Carrying = true; timer = game.Run.easy ? 6 : 4.4f; game.Audio.Cue("arc"); return true;
                case 7:
                    if (near < 0) return false;
                    if (near == 0) { Heat -= 25; Pressure += 19; } else { Pressure = Mathf.Max(0, Pressure - 38); Heat += 6; }
                    actionCooldown = age + 1.0f; game.Audio.Cue("freeze"); return true;
                case 8:
                    if (StateStep != 0 || near != 0) return false;
                    StateStep = 1; route.Clear(); route.Add(Feet); nodes[0].Move(Feet); lastRouteSample = age; timer = 0; game.Audio.Cue("sonar"); return true;
                case 9:
                    if (Carrying || !game.Player.RodEquipped) return false;
                    for (int i = 0; i < nodes.Count; i++) if (!done[i]) {
                        Vector3 aim = nodes[i].Position + Vector3.up * 1.4f - game.Player.View.transform.position;
                        if (aim.magnitude <= 21 && Vector3.Dot(game.Player.View.transform.forward, aim.normalized) > .93f) { selected = i; pullSide=i%2==0?1:-1;Carrying = true;wasArmSurge=false; partial = 0; Tension = .12f; timer = 0; game.Player.CastAnimation(); game.Audio.Cue("cast"); return true; }
                    }
                    game.Notice("用准星瞄准岸上触腕，再按 E 挂钩", 2); return false;
                case 10:
                    if (StateStep == 0 && near == 0) { StartSonar(); return true; }
                    if (StateStep == 1 && near >= 1 && near <= 3) {
                        if (near == selected + 1) {
                            StateStep=2;ObservationLeg=0;timer=0;foreach (var n in nodes) if(n.Kind=="echo")n.Root.gameObject.SetActive(false);
                            game.Notice("真回波已锁定 · 引出破冰，蓝线变橙后横向侧闪！",4);
                            TrackingBreach.Create(game,Owner.transform.position,EncounterTuning.FailureDamage(BossIndex,Phase),0,ResolveObservation);
                            Owner.NotifyAttackExecuted();
                        }
                        else { Fail("假回波 · 白鲸已锁定此处，横移避开冲锋；回声呐台重新定位", 0); TrackingBreach.Create(game, Owner.transform.position, 22 + Phase * 3); StateStep = 0; foreach(var n in nodes)if(n.Kind=="echo")n.Root.gameObject.SetActive(false); }
                        return true;
                    }
                    return false;
            }
            return false;
        }

        Vector3 Feet { get { var p = game.Player.transform.position; p.y = game.World.GroundAt(p) + .12f; return p; } }
        void Update()
        {
            if (!game || !Owner || Owner.dead || game.Paused || game.State != VoyageState.Combat) return;
            float dt = Time.deltaTime; age += dt;
            if(!Active){foreach(var n in nodes)n.Tick(dt,game.Player.View.transform);return;}
            if (!game.Automation) held = Input.GetKey(KeyCode.E);
            switch (BossIndex) {
                case 0: TickCrab(dt); break;
                case 1: TickMelody(dt); break;
                case 2: TickPurify(dt); break;
                case 3: TickMirrors(dt); break;
                case 4: TickBell(dt); break;
                case 5: TickEscort(dt); break;
                case 6: TickCharge(dt); break;
                case 7: TickValves(dt); break;
                case 8: TickMemory(dt); break;
                case 9: TickArms(dt); break;
                case 10: TickSonar(dt); break;
            }
            foreach(var n in nodes)n.Tick(dt, game.Player.View.transform);
        }

        void TickCrab(float dt)
        {
            selected = CompletedActions % 2; var target = nodes[selected]; Target(target, "引钳撞桩 · 蓝线变橙后侧闪");
            timer -= dt; CycleProgress = Mathf.Clamp01(1 - timer / 2.3f);
            if (!locked) {
                lockedPoint = Feet; SetGuide(0, new Vector3(0, target.Position.y + .3f, 17), lockedPoint, Cyan);
                Cue = "引钳 " + CompletedActions + "/" + RequiredActions + " · 站到亮桩处，等蓝线变橙后侧闪";
                if (timer <= 0) { locked = true; timer = 1.1f; nodes[selected].Flash(Gold, 1.1f); }
            } else {
                SetGuide(0, new Vector3(0, target.Position.y + .3f, 17), lockedPoint, Red);
                Cue = "钳击已锁定 · 现在侧闪！";
                if (timer > 0) return;
                bool hitStake = GameDirector.FlatDistance(lockedPoint, target.Position) < 2.25f;
                if (GameDirector.FlatDistance(Feet, lockedPoint) < 2.4f) game.Player.TakeDamage(EncounterTuning.FailureDamage(BossIndex,Phase));
                game.Effect(lockedPoint + Vector3.up, Gold, 22, .18f); Owner.NotifyAttackExecuted();strikeUntil=age+.6f;strikeOffset=lockedPoint-originalPosition;strikeOffset.y=0;
                if (Phase == 3) game.Warn(lockedPoint, 2.4f, .75f, 17);
                if (hitStake) { CompletedActions++; target.Complete(); game.Audio.Cue("explosion"); }
                else Fail("重钳没有撞桩 · 下一次站到亮桩处引导，再侧闪", 0);
                if (CompletedActions >= RequiredActions) { Solve(); return; }
                locked = false; timer = 2.8f; CycleProgress = 0;
            }
            Progress = (float)CompletedActions / RequiredActions;
        }
        void TickMelody(float dt)
        {
            timer += dt; string score = ""; for(int i=0;i<melody.Count;i++)score+=(i>0?" → ":"")+(melody[i]+1);
            if (Listening) {
                int index = Mathf.FloorToInt(timer / .9f);
                if(index < melody.Count) { CurrentSignal=melody[index]; if(tunePlayed!=index){tunePlayed=index;nodes[CurrentSignal].Flash(Green,.6f);PlayNote(CurrentSignal);} }
                else { Listening=false; timer=0; nextDanger=age+7; }
                Cue = "听音 / 看亮灯 · 乐谱 " + score;
                TargetPoint = nodes[CurrentSignal].Position; TargetLabel = "亮灯音贝 " + (CurrentSignal+1);
            } else {
                Cue="按 E 依序回应 · "+score+" · 已完成 "+CompletedActions+"/"+RequiredActions;
                Target(nodes[melody[Mathf.Min(CompletedActions,melody.Count-1)]],"音贝 · 走近按 E 回应");
                if(age>=nextDanger){nextDanger=age+7;ThreatField.Ring(game,Owner.transform.position,12+Phase*2,Cyan);Owner.NotifyAttackExecuted();}
            }
            Progress=(float)CompletedActions/RequiredActions;
        }
        void TickPurify(float dt)
        {
            int next=1;while(next<nodes.Count&&done[next])next++;
            Target(Carrying?nodes[Mathf.Min(next,nodes.Count-1)]:nodes[0],Carrying?"污染环 · 按住 E 净化":"净水泵 · E 取水");
            int near=Nearest(2.7f);
            if(Carrying&&near>0&&!done[near]&&held){partial+=dt;if(partial>=1.4f){done[near]=true;nodes[near].Complete();CompletedActions++;Carrying=false;partial=0;game.Audio.Cue("splash");if(CompletedActions>=RequiredActions){Solve();return;}}}
            else partial=Mathf.Max(0,partial-dt*.3f);
            bool safe=GameDirector.FlatDistance(Feet,nodes[0].Position)<3;
            for(int i=1;i<nodes.Count;i++)if(done[i]&&GameDirector.FlatDistance(Feet,nodes[i].Position)<3.1f)safe=true;
            if(age>=nextDanger){nextDanger=age+3.6f;if(!safe)game.Player.TakeDamage(6+Phase);game.Audio.Cue("splash");Owner.NotifyAttackExecuted();}
            Cue="净化 "+CompletedActions+"/"+RequiredActions+" · "+(Carrying?"携水中，污染圈内按住 E "+partial.ToString("F1")+"/1.4 秒":"到泵取水；蓝圈 / 绿圈内免受毒潮")+" · 毒潮 "+Mathf.CeilToInt(nextDanger-age)+" 秒";
            Progress=(CompletedActions+partial/1.4f)/RequiredActions;
        }
        void TickMirrors(float dt)
        {
            int linked=0;
            for(int i=0;i<3;i++){
                Vector3 from=nodes[i].Position+Vector3.up*1.6f,to=i<2?nodes[i+1].Position+Vector3.up*1.6f:Owner.transform.position+Vector3.up;
                bool aligned=MirrorTurns[i]==MirrorSolution[i];if(aligned&&linked==i)linked++;
                Vector3 direction=(to-from).normalized;direction=Quaternion.Euler(0,(MirrorTurns[i]-MirrorSolution[i])*90,0)*direction;
                SetGuide(i,from,from+direction*(to-from).magnitude,aligned?Gold:Red);
                nodes[i].SetRotation(direction);nodes[i].Tint(aligned?Gold:Cyan);
            }
            CompletedActions=linked;Progress=linked/3f;
            int first=Mathf.Clamp(linked,0,2);Target(nodes[first],"反光镜 · E 转动，沿光束查看下一个目标");
            Cue="折光链 "+linked+"/3 · 用 E 转镜，使红色光束接到下一面镜；金线表示已对准";
            if(linked==3){partial+=dt;if(partial>=.9f){Solve();return;}}else partial=0;
            if(age>=nextDanger){nextDanger=age+6;game.Warn(Feet,1.7f,1.6f,15+Phase*2);Owner.NotifyAttackExecuted();}
        }
        void TickBell(float dt)
        {
            timer+=dt;CycleProgress=Mathf.Clamp01(timer/5.6f);bool window=timer>=3.5f&&timer<=4.7f;
            nodes[0].Tint(window?Green:Gold);nodes[0].AnimateBell(timer);Target(nodes[0],"悬钟 · 绿色节拍按 E 拉绳");
            Cue="截潮 "+CompletedActions+"/"+RequiredActions+" · "+(window?"现在拉绳！ E":"钟声蓄势 · 绿区在倒数 2–1 秒")+" · "+Mathf.CeilToInt(Mathf.Max(0,5.6f-timer))+" 秒";
            Progress=(float)CompletedActions/RequiredActions;
            if(timer>=5.6f){timer=-1.5f;Fail("错过钟拍 · 跳过来潮后准备下一次；成功节拍保留",0);ThreatField.Ring(game,Owner.transform.position,18+Phase*2,Gold);Owner.NotifyAttackExecuted();}
        }
        void TickEscort(float dt)
        {
            var furnace=nodes[0];bool nearby=GameDirector.FlatDistance(Feet,furnace.Position)<4.3f;
            bool heating=held&&GameDirector.FlatDistance(Feet,furnace.Position)<3;
            Heat=Mathf.Clamp(Heat+dt*(heating?32:-3.8f),0,100);
            if(nearby&&!heating&&Heat>20){var goal=route[CompletedActions];furnace.Move(Vector3.MoveTowards(furnace.Position,goal,dt*2.1f));if(GameDirector.FlatDistance(furnace.Position,goal)<.2f){CompletedActions++;checkpoint=furnace.Position;Heat=Mathf.Max(Heat,45);nodes[CompletedActions].Complete();if(CompletedActions>=RequiredActions){Solve();return;}}}
            if(Heat<=0){Fail("热炉熄火 · 回到上个停泊点，按住 E 重新加热；已走路段保留",0);furnace.Move(checkpoint);Heat=22;}
            if(age>=nextDanger){nextDanger=age+5.2f;game.Warn(Feet,1.9f,1.6f,17+Phase*2);Owner.NotifyAttackExecuted();}
            Target(furnace,"伴行热炉 · "+(Heat<40?"近处按住 E 补热":"保持四米内护送"));
            Cue="护火 "+CompletedActions+"/"+RequiredActions+" · 热量 "+Mathf.CeilToInt(Heat)+" · "+(heating?"正在补热，松手行进":!nearby?"离热炉太远，停航等待":Heat<=20?"温度过低，按住 E 补热":"伴行中 · 落冰时离开预警圈");
            Progress=(float)CompletedActions/RequiredActions;
        }
        void TickCharge(float dt)
        {
            Target(Carrying?nodes[selected]:nodes[0],Carrying?"闪亮接地圈 · 进入自动泄放":"蓄电台 · E 承接电荷");
            nodes[selected].Tint(Carrying?Green:Cyan);
            if(Carrying){timer-=dt;if(GameDirector.FlatDistance(Feet,nodes[selected].Position)<2.7f){CompletedActions++;briefExposure=age+2.5f;Carrying=false;nodes[selected].Flash(Green,1);game.Audio.Cue("arc");selected=selected==1?2:1;if(CompletedActions>=RequiredActions){Solve();return;}}
                else if(timer<=0){Carrying=false;Fail("电荷失控 · 本轮需重新领取，已接地的轮次保留",EncounterTuning.FailureDamage(BossIndex,Phase));ThreatField.Ring(game,Feet,8,Gold);}}
            if(age>=nextDanger&&!Carrying){nextDanger=age+6;game.Warn(Feet,1.8f,1.8f,15+Phase);Owner.NotifyAttackExecuted();}
            Cue="接地 "+CompletedActions+"/"+RequiredActions+" · "+(Carrying?"带电！赶往"+(selected==1?"西":"东")+"接地圈 · "+timer.ToString("F1")+" 秒":"到中央蓄电台按 E 领取电荷");
            CycleProgress=Carrying?Mathf.Clamp01(timer/(game.Run.easy?6:4.4f)):0;Progress=(float)CompletedActions/RequiredActions;
        }
        void TickValves(float dt)
        {
            Heat=Mathf.Clamp(Heat+dt*(Phase==3?3.8f:3),0,105);Pressure=Mathf.Clamp(Pressure+dt*(Heat>45?2.2f:-1.5f),0,105);
            bool balanced=Heat>=25&&Heat<=45&&Pressure<65;
            if(balanced)partial+=dt;
            if(Heat>=100||Pressure>=100){Fail("过热 / 超压 · 退回一秒淬火，阀门仍可使用",EncounterTuning.FailureDamage(BossIndex,Phase));partial=Mathf.Max(0,partial-1);Heat=68;Pressure=35;game.Warn(Feet,2,1.2f,12);}
            Progress=Mathf.Clamp01(partial/RequiredActions);CompletedActions=Mathf.FloorToInt(partial);
            Target(nodes[Pressure>59?1:0],Pressure>59?"泄压阀 · E 降压":"冷却阀 · E 降温");
            Cue="淬火 "+partial.ToString("F1")+"/"+RequiredActions+" 秒 · 温度 "+Mathf.CeilToInt(Heat)+" [25–45] · 压力 "+Mathf.CeilToInt(Pressure)+" [<65]";
            nodes[0].Tint(balanced?Green:Cyan);nodes[1].Tint(Pressure>59?Red:Gold);
            if(partial>=RequiredActions){Solve();return;}
            if(age>=nextDanger){nextDanger=age+7;game.Warn(Feet,1.5f,1.6f,16+Phase);Owner.NotifyAttackExecuted();}
        }
        void TickMemory(float dt)
        {
            if(StateStep==0){Target(nodes[0],"记忆起点 · E 开始记录路线");Cue="在记忆起点按 E，再走出 "+RequiredActions+" 个间隔三米的足迹；之后逆走自己留下的路线";return;}
            timer+=dt;
            if(StateStep==1){
                Cue="记录足迹 "+route.Count+"/"+RequiredActions+" · 继续走，每个足迹至少相隔三米";
                var suggested=route[route.Count-1]+(route.Count%2==0?Vector3.right:Vector3.left)*4;suggested.z=3;TargetPoint=suggested;TargetLabel="走出新足迹 · 随后逆行回收";
                if(age-lastRouteSample>=.8f&&GameDirector.FlatDistance(Feet,route[route.Count-1])>=3.5f){route.Add(Feet);lastRouteSample=age;AddAt("记录足迹",Feet,Cyan,"memory");game.Audio.Cue("step");}
                if(route.Count>=RequiredActions){StateStep=2;replayIndex=route.Count-1;timer=0;nextDanger=age+2.4f;game.Notice("路线已记录 · 现在沿金色足迹逆行；回收后离开原位置",5);}
            }else{
                TargetPoint=route[replayIndex];TargetLabel="回收足迹 "+(replayIndex+1)+" · 逆着刚才的路线";
                Cue="逆行回收 "+CompletedActions+"/"+RequiredActions+" · 走进金色足迹，回收后立即继续移动";
                for(int i=1;i<nodes.Count;i++)nodes[i].Tint(i==replayIndex?Gold:Cyan);
                if(GameDirector.FlatDistance(Feet,route[replayIndex])<1.8f){var old=route[replayIndex];CompletedActions++;game.Warn(old,1.8f,1.8f,17+Phase);Owner.NotifyAttackExecuted();game.Audio.Cue("ready");replayIndex--;if(replayIndex<0){Solve();return;}}
                if(age>=nextDanger){nextDanger=age+5.5f;game.Warn(Feet,1.5f,1.6f,14+Phase);}
            }
            Progress=(float)CompletedActions/RequiredActions;
        }
        void TickArms(float dt)
        {
            if(!Carrying){selected=0;while(selected<nodes.Count&&done[selected])selected++;Target(nodes[Mathf.Min(selected,nodes.Count-1)],"触腕 · 按 1 切竿，瞄准后 E 挂钩");Cue="牵制触腕 "+CompletedActions+"/"+RequiredActions+" · 鱼竿瞄准岸上触腕按 E；不消耗鱼饵";guides[0].enabled=false;guides[1].enabled=false;}
            else {
                timer+=dt;Surge=Mathf.Sin((timer-.7f)*2.2f)>.22f;bool reeling=held&&game.Player.RodEquipped;
                if(Surge&&!wasArmSurge&&Phase>=2){pullSide=-pullSide;Vector3 across=Feet;ThreatField.Line(game,across-Vector3.forward*10,across+Vector3.forward*10,.85f,1.25f,20+Phase*2,Gold);Owner.NotifyAttackExecuted();if(Phase==3&&RequiredActions-CompletedActions>1)ThreatField.RingDelayed(game,nodes[selected].Position,18,Cyan,1.9f);}
                wasArmSurge=Surge;
                Tension=Mathf.Clamp01(Tension+dt*(reeling?(Surge?.66f:.13f):-.4f));
                if(reeling&&Braced)partial+=dt*(Surge?.07f:.26f);else partial=Mathf.Max(0,partial-dt*.018f);
                SetGuide(0,game.Player.RodTip,HookPoint,Surge?Red:Cyan);guides[0].enabled=true;
                guides[1].enabled=Phase>=2;
                if(Phase>=2){for(int i=0;i<40;i++){float angle=i*Mathf.PI*2/40;guides[1].SetPosition(i,PullPoint+new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*1.8f);}Shape.TintLine(guides[1],Braced?Green:Gold);}
                Cue="牵引 "+Mathf.FloorToInt(partial*100)+"% · 张力 "+Mathf.CeilToInt(Tension*100)+"% · "+(Surge?(Phase>=2?"松手换侧：移入新的金圈":"红潮挣扎：松开 E"):Braced?"已站稳：按住 E 收线":"进入金圈：侧向借力才拉得动");
                TargetPoint=Phase>=2?PullPoint:nodes[selected].Position;TargetLabel=Phase>=2?"金色牵引圈 · "+(Braced?"平潮收线":"换侧借力"):Surge?"松开 E 降张力":"按住 E 收线";CycleProgress=partial;
                if(GameDirector.FlatDistance(Feet,nodes[selected].Position)>24||Tension>=1){Carrying=false;partial=0;Fail("鱼线断开 · 调整距离 / 红潮松手，重新瞄准挂钩即可",0);}
                else if(partial>=1){done[selected]=true;nodes[selected].Complete();Carrying=false;CompletedActions++;game.Audio.Cue("explosion");if(CompletedActions>=RequiredActions){Solve();return;}}
            }
            if(age>=nextDanger){nextDanger=age+6.5f;game.Warn(Feet,2,1.5f,18+Phase*2);Owner.NotifyAttackExecuted();}
            Progress=(CompletedActions+partial)/RequiredActions;
        }
        void StartSonar()
        {
            StateStep=1;timer=0;game.Audio.Cue("sonar");
            for(int i=0;i<3;i++){var n=nodes[i+4];n.Root.gameObject.SetActive(true);n.Tint(i==selected?Green:new Color(.48f,.58f,.61f));n.SetEcho(i==selected);}
        }
        void ResolveObservation(bool evaded)
        {
            if(!this||!Owner||Owner.dead||!Active||StateStep!=2||game.State!=VoyageState.Combat)return;
            if(evaded&&Phase==3&&ObservationLeg==0){ObservationLeg=1;timer=0;game.Notice("白鲸折返！第二道蓝线会重新追踪 · 等变橙再换方向",4);TrackingBreach.Create(game,Owner.transform.position+Vector3.right*(selected==0?18:-18),EncounterTuning.FailureDamage(BossIndex,Phase),.45f,ResolveObservation);Owner.NotifyAttackExecuted();return;}
            StateStep=0;timer=0;nextDanger=age+5;
            if(!evaded){Fail("破冰击中观测者 · 本次信号丢失；已完成观测保留，再发一次声呐",0);return;}
            CompletedActions++;briefExposure=age+3.5f;game.Audio.Cue("weak");
            if(CompletedActions>=RequiredActions){Solve();return;}
            selected=(selected+1+(Phase==2?1:0))%3;
            game.Notice("成功避开破冰 · 观测 "+CompletedActions+"/"+RequiredActions+" 已保存，白鲸短暂浮出；再追新的航线",5);
        }
        void TickSonar(float dt)
        {
            timer+=dt;
            if(StateStep==0){Target(nodes[0],"声呐台 · E 发射免费脉冲");Cue="白鲸潜航 · 到声呐台按 E；识别双环真回波对应的西 / 中 / 东观测点";}
            else if(StateStep==1){
                for(int i=0;i<3;i++){Vector3 p=new Vector3((i-1)*11+Mathf.Sin(timer*(i==selected?.8f:1.7f)+i)*(i==selected?2:4),4,34+Mathf.Cos(timer*.8f+i)*2);nodes[i+4].Move(p);}
                if(Phase==1){Target(nodes[selected+1],"双环真回波对应观测点 · E 锁定");Cue="双环真回波在"+(selected==0?"西":selected==1?"中":"东")+"侧 · 观测点按 E 引出破冰，随后避开锁定线";}
                else {TargetPoint=new Vector3(0,4,34);TargetLabel="观察声呐双环 · 找对应西 / 中 / 东观测点";Cue="新航线 · 观察双环而非单环，选择对应观测点按 E；蓝线变橙后横移以保留信号";}
                if(timer>12){StateStep=0;timer=0;Fail("回波消散 · 回到发射台重发，已完成的锁定保留",0);foreach(var n in nodes)if(n.Kind=="echo")n.Root.gameObject.SetActive(false);}
            }
            else {TargetPoint=Feet;TargetLabel="破冰追踪 · 等蓝线锁定后横向闪开";Cue=Phase==3?"折返破冰 "+(ObservationLeg+1)+" / 2 · 每道蓝线分别锁定，重新横移；两次都避开才保留本轮记录":"观测需要你活着完成 · 现在避开锁定破冰线；成功后留下本次航线记录";}
            if(age>=nextDanger&&StateStep!=2){nextDanger=age+8;TrackingBreach.Create(game,Owner.transform.position,20+Phase*2);Owner.NotifyAttackExecuted();}
            Progress=(float)CompletedActions/RequiredActions;
        }

        int Nearest(float range)
        {
            int best=-1;float distance=range;
            for(int i=0;i<nodes.Count;i++){if(nodes[i].Kind=="echo"||nodes[i].Kind=="waypoint")continue;float d=GameDirector.FlatDistance(Feet,nodes[i].Position);if(d<distance){distance=d;best=i;}}
            return best;
        }
        void Target(MechanismMarker node,string label){TargetPoint=node.Position;TargetLabel=label;}
        void Fail(string message,float damage){Failures++;if(damage>0)game.Player.TakeDamage(damage);game.Notice(message,4);game.Audio.Cue("impact");}
        void Solve()
        {
            if(!Active)return;Active=false;Progress=1;held=false;Carrying=false;Cue="破招成功 · 核心暴露，换枪反击！";
            foreach(var n in nodes)n.Complete();foreach(var line in guides)if(line)line.enabled=false;
            game.Notice(Cue,4);game.Audio.Cue("weak");solved?.Invoke();
        }
        void ClearStage()
        {
            if(stageRoot){stageRoot.gameObject.SetActive(false);Destroy(stageRoot.gameObject);}nodes.Clear();guides.Clear();
            if(noteSource){noteSource.Stop();Destroy(noteSource);}if(notes!=null){foreach(var clip in notes)if(clip)Destroy(clip);notes=null;}
        }
        void OnDestroy(){ClearStage();}
    }
}
