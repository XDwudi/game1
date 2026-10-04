using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Tidebreak
{
    public partial class SmokePilot
    {
        readonly List<string> v05Telemetry = new List<string> {
            "kind,id,seed,weapon,weapon_rank,hull_rank,seconds,incoming_damage,health,medkits_used,shots,phase_mask,actions,failures,outcome"
        };
        readonly List<string> v05Phases = new List<string> { "id,seed,phase,mechanic_seconds,actions,failures" };
        float v05Speed=2,v05DashUntil; bool v05Heating;
        string V05Arg(string name,string fallback)
        {
            string[] args=Environment.GetCommandLineArgs(); int i=Array.IndexOf(args,name);
            return i>=0&&i+1<args.Length?args[i+1]:fallback;
        }
        IEnumerator V05Contracts()
        {
            output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Artifacts/V05QA",V05Arg("-v05Run","manual")));
            Directory.CreateDirectory(output);
            float.TryParse(V05Arg("-v05Speed","2"),System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out v05Speed);
            v05Speed=Mathf.Clamp(v05Speed,1,3);Time.timeScale=v05Speed;
            string mode=V05Arg("-v05Mode","All");int only;int.TryParse(V05Arg("-v05Boss","0"),out only);
            Check(BossMechanism.Instructions.Length==11&&BossMechanism.Instructions.Distinct().Count()==11,"v05 eleven different player-action briefs exist");
            if(mode=="All"||mode=="Systems") {
                yield return RevisionCinematics();yield return IslandRevisionContracts();yield return PostgameRevisionContracts();
                Time.timeScale=v05Speed;yield return RevisionFishingEconomy();Time.timeScale=v05Speed;
                yield return V05CoastalCatches();yield return V05Research();yield return V05Secrets();yield return V05Synergies();yield return V05Telegraphs();
                yield return RevisionIslandPaths();yield return RevisionBuildPurchases();yield return RevisionWeaponCycles();
                yield return RevisionCreatureAnimation();yield return RevisionEliteContracts();yield return V05EliteCounterContracts();
                yield return V05SoundAndRestoration();
            }
            if(mode=="Coastal")yield return V05CoastalCatches();
            if(mode=="All"||mode=="Campaign")yield return V05CampaignContracts();
            if(mode=="All"||mode=="Negative")for(int id=108;id<=118;id++)if(only==0||only==id)yield return V05Negative(id);
            if(mode=="All"||mode=="Bosses")for(int id=108;id<=118;id++)if(only==0||only==id)yield return V05Boss(id);
            g.StartVoyage();yield return null;
            Check(FindObjectsOfType<BossMechanism>().Length==0&&!FindObjectsOfType<Transform>().Any(t=>t.name.StartsWith("Boss mechanism ")),"v05 new voyage releases boss controllers and mechanism props");
            File.WriteAllLines(Path.Combine(output,"combat.csv"),v05Telemetry);File.WriteAllLines(Path.Combine(output,"phases.csv"),v05Phases);
            Time.timeScale=1;
        }
        Enemy V05BossFixture(int id)
        {
            var boss=RevisionCreateBoss(id,true);Time.timeScale=v05Speed;v05Heating=false;v05DashUntil=0;
            // RevisionCreateBoss grants only a disclosed stage-appropriate paid loadout,
            // teleports once BEFORE combat, and resets all temporary invulnerability.
            Check(g.Player.InvulnerableUntil<=Time.time,"boss "+id+" fixture begins without granted invulnerability");
            return boss;
        }
        Vector3 V05Aim(Enemy actor)
        {
            var region=actor.GetComponentsInChildren<HitRegion>().FirstOrDefault(h=>h.GetComponent<Collider>()&&h.GetComponent<Collider>().enabled);
            return region?region.GetComponent<Collider>().bounds.center:actor.transform.position+Vector3.up*.4f;
        }
        void V05Move(Vector3 target,bool avoid=true)
        {
            Vector3 p=g.Player.transform.position,delta=target-p;delta.y=0;
            Vector3 move=delta.magnitude>.55f?delta.normalized:Vector3.zero;
            bool emergency=false;
            if(avoid){
                foreach(var warning in FindObjectsOfType<DeckWarning>())if(warning.Remaining<.9f&&GameDirector.FlatDistance(p,warning.transform.position)<warning.radius+1){
                    Vector3 away=p-warning.transform.position;away.y=0;if(away.sqrMagnitude<.1f)away=Vector3.right;
                    move=away.normalized;emergency=true;break;
                }
                foreach(var field in FindObjectsOfType<ThreatField>()){
                    string response;if(!field.Threatens(p,out response))continue;
                    if(field.Kind==1){var line=field.GetComponent<LineRenderer>();if(line&&line.positionCount>0){float r=GameDirector.FlatDistance(line.GetPosition(0),field.transform.position);float gap=GameDirector.FlatDistance(p,field.transform.position)-r;if(gap>0&&gap<1.65f)g.Player.Jump();}}
                    else if(field.Kind!=0||field.Remaining<.85f){
                        Vector3 away=p-field.transform.position;away.y=0;
                        if(field.Kind==0){Vector3 axis=field.End-field.transform.position;axis.y=0;Vector3 side=Vector3.Cross(Vector3.up,axis.normalized);away=side*(Vector3.Dot(away,side)>=0?1:-1);}
                        if(away.sqrMagnitude<.1f)away=Vector3.right;move=away.normalized;emergency=true;
                    }
                }
                foreach(var breach in FindObjectsOfType<TrackingBreach>()){
                    var line=breach.GetComponentsInChildren<LineRenderer>().FirstOrDefault(l=>l.name=="Sonar bearing");
                    if(!line||!line.enabled||line.startColor.r<.8f)continue;
                    Vector3 a=line.GetPosition(0),b=line.GetPosition(line.positionCount-1),axis=b-a,q=p-a;axis.y=q.y=0;
                    float along=Mathf.Clamp01(Vector3.Dot(q,axis)/Mathf.Max(.01f,axis.sqrMagnitude));
                    if((q-axis*along).magnitude<2.2f){Vector3 side=Vector3.Cross(Vector3.up,axis.normalized);move=side*(Vector3.Dot(q,side)>=0?1:-1);emergency=true;}
                }
            }
            if(emergency&&g.Player.DashReady>=.99f){g.Player.Dash(move);v05DashUntil=Time.time+.19f;}
            float pace=(1+g.Run.bootsLevel*.06f)*(g.Player.StatusEffect=="冰霜减速"?.65f:1)*(Time.time<g.TonicUntil?1.25f:1);
            // AnglerController already moves the real dash. Do not add a second
            // artificial walking displacement on top of those dash frames.
            if(g.Player.Motor.enabled&&Time.time>=v05DashUntil)g.Player.Motor.Move(move*5.2f*pace*Time.deltaTime);
        }
        void V05Operate(BossMechanism m)
        {
            m.SetActionHeld(false);Vector3 goal=m.TargetPoint;bool action=false;
            switch(m.BossIndex){
                case 0:if(m.TargetLocked)goal=m.TargetPoint+Vector3.right*(m.TargetPoint.x<0?-4.2f:4.2f);break;
                case 1:if(!m.Listening){goal=m.Nodes[m.Melody[Mathf.Min(m.CompletedActions,m.Melody.Count-1)]].Position;action=true;}break;
                case 2:action=!m.Carrying;m.SetActionHeld(m.Carrying);break;
                case 3:{int index=0;while(index<3&&m.MirrorTurns[index]==m.MirrorSolution[index])index++;if(index<3){goal=m.Nodes[index].Position;action=true;}break;}
                case 4:goal=m.Nodes[0].Position;action=m.TimingWindowOpen;break;
                case 5:if(m.Heat<37)v05Heating=true;if(m.Heat>92)v05Heating=false;goal=m.Nodes[0].Position+Vector3.back*.8f;m.SetActionHeld(v05Heating);break;
                case 6:action=!m.Carrying;break;
                case 7:{int index=m.Pressure>60?1:0;goal=m.Nodes[index].Position;action=index==1||m.Heat>43;break;}
                case 8:if(m.StateStep==0)action=true;else if(m.StateStep==1){int n=m.RecordedRoute.Count;goal=new Vector3(n%2==0?6:-6,0,3+n*.8f);}break;
                case 9:
                    g.Player.SetRod(true);goal=m.Carrying&&m.Phase>=2?m.PullPoint:m.HookPoint-Vector3.up*1.4f+Vector3.back*6;g.Player.AimAt(m.HookPoint);
                    if(!m.Carrying)m.UseAction();m.SetActionHeld(m.Carrying&&m.Braced&&!m.Surge&&m.Tension<.74f);break;
                case 10:
                    // Explicitly omniscient regression driving in later phases:
                    // human players read the double-ring echo, not this property.
                    if(m.StateStep==1)goal=m.Nodes[m.CurrentTargetIndex+1].Position;
                    action=m.StateStep!=2;break;
            }
            V05Move(goal,m.BossIndex!=0||!m.TargetLocked);
            if(action&&GameDirector.FlatDistance(g.Player.transform.position,goal)<2.45f)m.UseAction();
        }
        IEnumerator V05Boss(int id)
        {
            var boss=V05BossFixture(id);var encounter=boss.Encounter;var mechanism=encounter.Mechanism;
            float begin=Time.time,previous=g.Run.health,damage=0,phaseStart=Time.time;int phase=1,mask=0,shots=0,actions=0,failures=0,medkits=g.Run.medkits;
            bool solved=false,shotBoss=false;float bracedSince=-1;int patrol=0,damageMask=0,armSurgePhotos=0,armBracePhotos=0;Vector3[] path={new Vector3(-7,0,7),new Vector3(7,0,7),new Vector3(7,0,-4),new Vector3(-7,0,-4)};
            WeaponKind weapon=g.Player.weapon;
            while(boss&&!boss.dead&&g.State==VoyageState.Combat&&Time.time-begin<420){
                damage+=Mathf.Max(0,previous-g.Run.health);if(g.Run.health<g.Run.MaxHealth*.4f&&g.Run.medkits>0)g.UseUtility("medkit");previous=g.Run.health;
                mask|=1<<(encounter.Phase-1);
                if(phase!=encounter.Phase){phase=encounter.Phase;phaseStart=Time.time;solved=false;v05Heating=false;bracedSince=-1;yield return null;Capture("boss-"+id+"-phase-"+phase);}
                if(mechanism.Active)V05Operate(mechanism);
                if(!mechanism.Active){
                    if(!solved){solved=true;actions+=mechanism.CompletedActions;failures=mechanism.Failures;v05Phases.Add(id+","+g.Run.seed+","+phase+","+F(Time.time-phaseStart)+","+mechanism.CompletedActions+","+mechanism.Failures);File.WriteAllLines(Path.Combine(output,"phases.csv"),v05Phases);yield return null;yield return new WaitForEndOfFrame();Capture("boss-"+id+"-mechanic-result-"+phase+"-view-phase-"+encounter.Phase);}
                    if(GameDirector.FlatDistance(g.Player.transform.position,path[patrol])<1)patrol=(patrol+1)%path.Length;V05Move(path[patrol]);
                }
                if(!mechanism.Active||mechanism.BossIndex!=9){
                    g.Player.Equip(weapon);var minion=g.Enemies.FirstOrDefault(e=>e&&!e.IsBoss&&!e.dead);var target=minion?minion:boss;
                    float before=target.health;g.Player.AimAt(V05Aim(target));if(g.Player.Fire())shots++;if(target==boss&&(!boss||boss.health<before)){shotBoss=true;if((damageMask&(1<<(phase-1)))==0)Capture("boss-"+id+"-visible-hit-"+phase);damageMask|=1<<(phase-1);}
                    if(g.Player.QuickReloadAvailable&&g.Player.ReloadProgress>=.59f&&g.Player.ReloadProgress<=.67f)g.Player.Reload();
                }
                if(id==117&&mechanism.Active&&mechanism.Carrying&&mechanism.Phase>=2&&!mechanism.Surge&&mechanism.Braced){if(bracedSince<0)bracedSince=Time.time;}else bracedSince=-1;
                if(id==117&&mechanism.Active&&mechanism.Carrying&&mechanism.Phase>=2){
                    int bit=1<<mechanism.Phase;
                    if(mechanism.Surge&&(armSurgePhotos&bit)==0){yield return null;yield return new WaitForEndOfFrame();if(mechanism.Active&&mechanism.Carrying&&mechanism.Surge){armSurgePhotos|=bit;V05KrakenRenderedColours(mechanism);Capture("boss-117-red-tide-side-change-phase-"+mechanism.Phase);}}
                    else if(bracedSince>=0&&Time.time-bracedSince>=.25f&&mechanism.CycleProgress>=.15f&&(armBracePhotos&bit)==0){yield return null;yield return new WaitForEndOfFrame();if(mechanism.Active&&mechanism.Carrying&&!mechanism.Surge&&mechanism.Braced&&mechanism.CycleProgress>=.15f){armBracePhotos|=bit;V05KrakenRenderedColours(mechanism);Capture("boss-117-calm-braced-reel-phase-"+mechanism.Phase);}}
                }
                yield return null;
            }
            bool killed=!boss||boss.dead;float cleanup=Time.time;
            while(killed&&g.State==VoyageState.Combat&&Time.time-cleanup<45){
                var remaining=g.Enemies.FirstOrDefault(e=>e&&!e.dead);
                V05Move(path[patrol]);if(GameDirector.FlatDistance(g.Player.transform.position,path[patrol])<1)patrol=(patrol+1)%path.Length;
                // Volatile enemies leave a real, dodgeable death explosion and
                // settle the encounter 1.25 seconds later. Continue ordinary
                // movement until the game itself resolves, bounded by cleanup.
                if(!remaining){yield return null;continue;}
                g.Player.Equip(weapon);g.Player.AimAt(V05Aim(remaining));if(g.Player.Fire())shots++;
                if(g.Run.health<g.Run.MaxHealth*.4f&&g.Run.medkits>0)g.UseUtility("medkit");yield return null;
            }
            damage+=Mathf.Max(0,previous-g.Run.health);bool victory=killed&&(g.State==VoyageState.Sailing||g.State==VoyageState.Victory);
            Check(shotBoss,"boss "+id+" receives actual camera-ray weapon damage");
            Check(damageMask==7,"boss "+id+" weapon ray reaches damageable anatomy in each phase, including surfaced whale");
            Check(mask==7&&actions>=3,"boss "+id+" completes independent mechanisms in all three real phases");
            Check(victory,"boss "+id+" defeated with normal-speed movement, shared actions, real shooting and finite medicine; "+F(Time.time-begin)+" seconds");
            Check(g.Player.InvulnerableUntil!=float.PositiveInfinity,"boss "+id+" battle never enables test invulnerability");
            Capture("boss-"+id+"-outcome");
            v05Telemetry.Add("boss,"+id+","+g.Run.seed+","+weapon+","+g.Run.weaponLevel+","+g.Run.hullLevel+","+F(Time.time-begin)+","+F(damage)+","+F(g.Run.health)+","+(medkits-g.Run.medkits)+","+shots+","+mask+","+actions+","+failures+","+(victory?"cleared":g.State.ToString()));
            File.WriteAllLines(Path.Combine(output,"combat.csv"),v05Telemetry);g.StartVoyage();yield return null;
        }
        IEnumerator V05Negative(int id)
        {
            var boss=V05BossFixture(id);var m=boss.Encounter.Mechanism;float begin=Time.time;int shots=0;bool crossed=false;
            while(boss&&!boss.dead&&g.State==VoyageState.Combat&&Time.time-begin<18){crossed|=boss.Encounter.Phase>1;g.Player.AimAt(V05Aim(boss));if(g.Player.Fire())shots++;yield return null;}
            bool playerLost=g.State==VoyageState.Defeat,won=g.State==VoyageState.Victory||g.State==VoyageState.Sailing;
            Check(!crossed&&!won&&(playerLost||boss&&!boss.dead&&m.Active),"boss "+id+" stationary shooting cannot bypass its required opening action; player state "+g.State);
            Check(shots>0,"boss "+id+" shooting-only negative control actually fired");
            g.StartVoyage();yield return null;
            boss=V05BossFixture(id);m=boss.Encounter.Mechanism;int completed=m.CompletedActions;
            // Negative unit fixture may place the player; the full fight above never does.
            At(new Vector3(14,0,-9));yield return null;bool used=m.UseAction();
            Check(!used&&m.CompletedActions==completed,"boss "+id+" wrong location or missing tool cannot instantly progress a mechanism");
            if(id==117){
                g.Player.SetRod(false);g.Player.AimAt(m.Nodes[0].Position+Vector3.up*1.4f);yield return new WaitForSeconds(.3f);Check(!m.UseAction()&&!m.Carrying,"Kraken hook rejects gun equipment");
                At(m.Nodes[0].Position+Vector3.back*5,m.Nodes[0].Position+Vector3.up*1.4f);g.Player.SetRod(true);yield return new WaitForSeconds(.3f);m.UseAction();m.SetActionHeld(true);
                float tug=Time.time;while(m.Carrying&&Time.time-tug<10&&g.State==VoyageState.Combat)yield return null;
                m.SetActionHeld(false);Check(m.Active&&!m.Carrying&&m.CompletedActions==0&&m.Failures>0,"Kraken holding through red-tide struggle breaks the line without granting an arm");
                yield return new WaitForSeconds(.3f);g.Player.AimAt(m.Nodes[0].Position+Vector3.up*1.4f);m.UseAction();Check(m.Carrying,"Kraken broken line is retryable with the same free rod action");
            }
            if(id==112){At(m.Nodes[0].Position);yield return new WaitForSeconds(.3f);m.UseAction();Check(m.Active&&m.CompletedActions==0&&m.Failures>0,"bell rejects an early pull and remains retryable");}
            if(id==109){while(m.Listening)yield return null;At(m.Nodes[(m.Melody[0]+1)%3].Position);m.UseAction();Check(m.Active&&m.CompletedActions==0&&m.Listening,"wrong melody restarts playback without losing encounter access");}
            if(id==118){
                Vector3 station=m.Nodes[0].Position,observationPoint=station+Vector3.back*2.2f;At(observationPoint);float approach=Time.time;
                while(Time.time-approach<1){V05Move(station,false);yield return null;}
                Check(GameDirector.FlatDistance(g.Player.transform.position,station)>=.97f,"normal walking cannot enter the solid sonar station (one metre with .03 skin tolerance)");
                approach=Time.time;while(GameDirector.FlatDistance(g.Player.transform.position,observationPoint)>.6f&&Time.time-approach<3){V05Move(observationPoint,false);yield return null;}
                yield return new WaitForSeconds(.3f);m.UseAction();
                yield return V05WhaleEchoVisibility(m);
                At(m.Nodes[m.CurrentTargetIndex+1].Position+Vector3.back*2.2f);yield return new WaitForSeconds(.3f);m.UseAction();
                Check(m.StateStep==2&&m.CompletedActions==0,"whale correct echo selection only starts its required breach-evasion trial");
                yield return V05BreachRenderedColours();
                yield return new WaitForSeconds(2.1f);
                Check(m.Active&&m.CompletedActions==0&&m.StateStep==0&&m.Failures>0,"whale standing in the locked breach loses the attempted observation and permits a fresh sonar retry");
                // Isolated phase-three fixture, not a full clear: first evade is
                // performed through the real line and normal movement. No direct
                // callback, progress injection or completion method is invoked.
                m.BeginPhase(3);At(m.Nodes[0].Position+Vector3.back*2.2f);yield return new WaitForSeconds(.3f);m.UseAction();
                yield return V05WhaleEchoVisibility(m);
                At(m.Nodes[m.CurrentTargetIndex+1].Position+Vector3.back*2.2f);yield return new WaitForSeconds(.3f);m.UseAction();
                float observation=Time.time;
                while(m.StateStep==2&&m.ObservationLeg==0&&Time.time-observation<4){V05Move(g.Player.transform.position);yield return null;}
                Check(m.StateStep==2&&m.ObservationLeg==1&&m.CompletedActions==0,"final whale phase requires a second separately locked return breach after evading the first");
                g.TogglePause();yield return new WaitForSecondsRealtime(.3f);
                Check(m.StateStep==2&&m.ObservationLeg==1&&m.CompletedActions==0,"pausing between whale breach legs preserves the pending second-leg callback");g.TogglePause();Time.timeScale=v05Speed;
                int mistakes=m.Failures;observation=Time.time;
                while(m.StateStep==2&&Time.time-observation<4)yield return null;
                Check(m.Active&&m.StateStep==0&&m.CompletedActions==0&&m.Failures>mistakes,"dodging only the first whale leg and taking the return hit awards no observation and stays retryable");
            }
            g.StartVoyage();yield return null;
        }
        IEnumerator V05CoastalCatches()
        {
            g.StartVoyage();Time.timeScale=v05Speed;g.Run.seed=50503;g.Rng=new System.Random(g.Run.seed);UnityEngine.Random.InitState(g.Run.seed);
            // This is a new-captain loadout; no granted upgrades, gold, medicine or
            // invulnerability. Travel between casting and pickup positions is a fixture.
            for(int habitat=0;habitat<3&&g.Run.health>0;habitat++){
                At(new Vector3(0,0,23),new Vector3((habitat-1)*30,0,48));g.Player.SetRod(true);g.CastCharge=.75f;g.Cast();
                Check(g.State==VoyageState.Fishing,"habitat "+habitat+" accepts a real water cast");
                float begin=Time.time;while(g.State==VoyageState.Fishing&&Time.time-begin<40){g.TickFishing(!g.Surge,Time.deltaTime);yield return null;}
                Check(g.State==VoyageState.Combat&&g.Enemies.Count>0,"habitat "+habitat+" tension/reeling produces a live encounter");
                if(g.State!=VoyageState.Combat||g.Enemies.Count==0)break;
                var species=g.Enemies[0];Check(species.Spec.id%3==habitat&&species.CaughtHabitat==habitat,"habitat "+habitat+" rolls its own ecological family pool and tags the catch origin");
                begin=Time.time;int waypoint=0;Vector3[] patrol={new Vector3(-5,0,10),new Vector3(5,0,10),new Vector3(5,0,16),new Vector3(-5,0,16)};
                while(g.State==VoyageState.Combat&&Time.time-begin<65){
                    var enemy=g.Enemies.FirstOrDefault(e=>e&&!e.dead);if(enemy){if(GameDirector.FlatDistance(g.Player.transform.position,patrol[waypoint])<1)waypoint=(waypoint+1)%patrol.Length;V05Move(patrol[waypoint]);g.Player.AimAt(V05Aim(enemy));g.Player.Fire();}
                    yield return null;
                }
                Check(g.State==VoyageState.Sailing&&g.Loot.Count>0,"habitat "+habitat+" fish is defeated with ordinary movement and real starter weapon");
                if(g.State!=VoyageState.Sailing||g.Loot.Count==0)break;
                var loot=g.Loot.Last();int wallet=g.Run.coins;float pickupStart=Time.time;
                var pickupTrace=new List<string>{"seconds,habitat,event,player_x,player_y,player_z,fish_x,fish_y,fish_z,distance"};
                Action<string> tracePickup=label=>{if(!loot)return;Vector3 p=g.Player.transform.position,f=loot.transform.position;pickupTrace.Add(F(Time.time-pickupStart)+","+habitat+","+label+","+F(p.x)+","+F(p.y)+","+F(p.z)+","+F(f.x)+","+F(f.y)+","+F(f.z)+","+F(Vector3.Distance(p,f)));};
                tracePickup("defeated");
                // A slain airborne fish receives upward velocity, and a catch above
                // water can need the ordinary wash-ashore recovery. Wait for land,
                // then walk toward it and use E until the real 3D pickup check passes.
                while(loot&&g.World.GroundAt(loot.transform.position)<-.3f&&Time.time-pickupStart<7)yield return null;
                if(loot&&g.World.GroundAt(loot.transform.position)>=-.3f)At(loot.transform.position+Vector3.back,loot.transform.position);
                tracePickup("approach_start");float approachStart=Time.time;
                while(loot&&!g.Player.HeldFish&&Time.time-approachStart<20){V05Move(loot.transform.position,false);g.Interact();yield return null;}
                tracePickup(g.Player.HeldFish==loot?"picked_up":"pickup_timeout");File.WriteAllLines(Path.Combine(output,"habitat-"+habitat+"-pickup.csv"),pickupTrace);
                bool picked=g.Player.HeldFish==loot&&loot.Registered;
                if(!picked)Capture("habitat-"+habitat+"-pickup-failed");
                Check(picked&&g.ResearchCount==habitat+1,"habitat "+habitat+" actual caught fish records only after physical pickup");
                Check(g.Run.coins==wallet+(habitat==2?35:0),"live habitat pickup pays only the final one-time research stipend");
                g.Player.StowHeld();Sell();Capture("habitat-"+habitat+"-catch");
            }
            Check(g.ResearchRecord==7,"all three coastal ecological pools are reachable with the starting lure");
        }
        IEnumerator V05Research()
        {
            g.StartVoyage();Time.timeScale=v05Speed;int before=g.Run.coins;
            // Data/catch-handling fixture isolates once-only payment. The fishing loop
            // separately validates live hooks, combat, physical pickup and gold upgrades.
            for(int habitat=0;habitat<3;habitat++){
                var item=new CatchData{speciesId=habitat,habitat=habitat,fieldSample=true,value=25};
                var obj=new GameObject("Research specimen fixture");obj.transform.position=g.Player.transform.position+Vector3.forward;
                var fish=obj.AddComponent<FishLoot>();fish.Init(g,item,Quaternion.identity);g.Loot.Add(fish);g.Player.PickUp(fish);g.Player.StowHeld();yield return null;
                Check(g.ResearchCount==habitat+1,"research records distinct habitat "+habitat+" through physical catch handling");
            }
            Check(g.Run.coins==before+35&&g.ResearchRecord==7,"three habitat records pay exactly one 35-coin research stipend");
            var duplicate=new GameObject("Duplicate research specimen").AddComponent<FishLoot>();duplicate.transform.position=g.Player.transform.position+Vector3.forward;
            duplicate.Init(g,new CatchData{speciesId=0,habitat=0,fieldSample=true,value=25},Quaternion.identity);g.Loot.Add(duplicate);g.Player.PickUp(duplicate);g.Player.StowHeld();
            Check(g.Run.coins==before+35,"duplicate habitat cannot farm the research stipend");
            g.Checkpoint(false);g.ReturnHarbor();g.StartVoyage(true);Check(g.ResearchRecord==7&&g.Run.coins==before+35,"habitat records and once-only stipend survive reload");yield return null;
        }
        IEnumerator V05Secrets()
        {
            g.StartVoyage();g.Run.maxIsland=9;
            for(int island=1;island<=9;island++){
                if(island>1){g.ShowMap();g.Travel(island);}At(g.SitePoint(2));g.UseSite(2);int wallet=g.Run.coins,answer=NarrativeContent.SecretAnswer(island);
                g.SolveRune((answer+1)%3);Check(g.State==VoyageState.Dialogue&&(g.Run.exploredMask&4)==0&&g.Run.coins==wallet&&!string.IsNullOrEmpty(g.SecretFeedback),"island "+island+" wrong evidence choice gives feedback and permits retry");
                g.SolveRune(answer);Check((g.Run.exploredMask&4)!=0&&g.Run.coins==wallet+50+island*8,"island "+island+" correct evidence is archived and rewarded once");
                wallet=g.Run.coins;g.UseSite(2);g.SolveRune(answer);Check(g.Run.coins==wallet,"island "+island+" archived evidence cannot be paid twice");
                g.Checkpoint(false);var saved=SaveStore.Read<RunData>("voyage");Check(saved!=null&&(saved.exploredMask&4)!=0,"island "+island+" discovered evidence persists");yield return null;
            }
        }
        IEnumerator V05Synergies()
        {
            g.StartVoyage();g.Run.keystoneMask=1;var s=g.Player.Synergy;s.Reset();s.RegisterShot(true,false);s.RegisterShot(true,true);s.RegisterShot(false,false);
            Check(s.Rhythm==2,"rhythm uses hit+1, weak+2 and one-point miss loss");s.RegisterShot(true,true);s.RegisterShot(true,true);
            Check(Mathf.Approximately(s.ShotMultiplier(),1.85f)&&Mathf.Approximately(s.ShotMultiplier(),1),"six rhythm charge empowers exactly one shot by 85 percent");
            g.Run.keystoneMask=(1<<1)|(1<<7);s.Reset();g.SetState(VoyageState.Combat);At(new Vector3(0,0,7));
            var specimen=new GameObject("Element counter fixture").AddComponent<Enemy>();specimen.InitSpecies(g,ExpeditionContent.Species[0],false,new Vector3(0,2,15));specimen.health=specimen.maxHealth=3000;
            float hp=specimen.health;for(int i=0;i<4;i++)s.OnHit(specimen,false,10);Check(specimen.health==hp&&s.SteamHits==4&&s.ThunderHits==4,"element effects require their real hit counters");
            s.OnHit(specimen,false,10);Check(Mathf.Approximately(hp-specimen.health,24)&&s.SteamHits==0&&s.ThunderHits==5,"fifth hit procs steam independently");hp=specimen.health;s.OnHit(specimen,false,10);s.OnHit(specimen,false,10);
            Check(Mathf.Approximately(hp-specimen.health,38)&&s.ThunderHits==0,"seventh hit procs thermal lightning independently");
            hp=specimen.health;for(int i=0;i<10;i++)s.OnHit(specimen,false,10);Check(specimen.health==hp,"charged effects do not bypass their cooldowns");
            yield return new WaitForSeconds(4.1f);hp=specimen.health;s.OnHit(specimen,false,10);Check(Mathf.Approximately(hp-specimen.health,24),"steam returns after four seconds without prematurely resetting thermal cooldown");
            yield return new WaitForSeconds(1.1f);hp=specimen.health;s.OnHit(specimen,false,10);Check(Mathf.Approximately(hp-specimen.health,38),"thermal lightning returns after five seconds on its independent cooldown");g.StartVoyage();yield return null;
        }
        IEnumerator V05Telegraphs()
        {
            g.StartVoyage();g.SetState(VoyageState.Combat);At(new Vector3(0,0,4));
            ThreatField.Line(g,new Vector3(-8,0,4),new Vector3(8,0,4),2,2,10,Color.yellow);yield return null;
            var field=FindObjectsOfType<ThreatField>().First(f=>f.Kind==0);string response;
            Check(field.Threatens(new Vector3(0,0,5.9f),out response)&&!field.Threatens(new Vector3(0,0,6.4f),out response),"line threat covers its visible full-width corridor");
            var border=field.GetComponentsInChildren<LineRenderer>().FirstOrDefault(l=>l.loop&&l.positionCount>=4);
            Check(border&&Mathf.Abs(GameDirector.FlatDistance(border.GetPosition(0),border.GetPosition(border.positionCount-1))-4)<.05f,"line boundary renders the actual four-metre damage width");
            V05RenderedColour(field.GetComponent<LineRenderer>(),Color.yellow,"strike corridor uses its actual yellow material");
            V05RenderedColour(border,new Color(1,.63f,.26f),"strike corridor boundary uses its actual amber material");
            float remaining=field.Remaining,hp=g.Run.health;g.TogglePause();yield return new WaitForSecondsRealtime(.35f);
            Check(Mathf.Approximately(field.Remaining,remaining)&&Mathf.Approximately(g.Run.health,hp),"paused combat freezes attack timing and damage");g.TogglePause();
            yield return new WaitForSeconds(2.1f);Check(g.Run.health<hp,"grounded captain inside the displayed corridor takes the actual strike");g.StartVoyage();yield return null;
            g.SetState(VoyageState.Combat);At(new Vector3(0,0,8));hp=g.Run.health;
            ThreatField.Pool(g,g.Player.transform.position,2,.1f,4,Color.magenta);yield return new WaitForSeconds(.25f);
            Check(g.Run.health<hp&&g.Player.StatusEffect=="","ordinary ink or heat pool hurts without silently poisoning the captain");
            g.StartVoyage();yield return null;g.SetState(VoyageState.Combat);At(new Vector3(0,0,8));hp=g.Run.health;
            ThreatField.Pool(g,g.Player.transform.position,2,.1f,4,Color.green,SeaTrait.Venom);yield return new WaitForSeconds(.25f);
            Check(g.Run.health<hp&&g.Player.StatusEffect=="中毒","explicit venom pool applies its advertised poison after real damage");g.StartVoyage();yield return null;
        }
    }
}
