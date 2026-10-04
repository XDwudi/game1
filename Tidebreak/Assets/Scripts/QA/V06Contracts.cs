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
        float v06Speed=2;bool v06WalkOk;
        readonly List<string> v06World=new List<string>{"island,label,waypoint,x,y,z,ground,seconds,distance,max_frame_displacement,success"};
        readonly List<string> v06Combat=new List<string>{"species,elite,weapon_rank,seed,total_seconds,wait_for_behavior,seconds_before_first_shot,owner_seconds_after_first_shot,shots,attacks,regional_actions,incoming_damage,health,outcome"};
        IEnumerator V06Contracts()
        {
            output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Artifacts/V06QA",V05Arg("-v06Run","manual")));Directory.CreateDirectory(output);
            float.TryParse(V05Arg("-v06Speed","2"),System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out v06Speed);
            v06Speed=Mathf.Clamp(v06Speed,1,3);v05Speed=v06Speed;Time.timeScale=v06Speed;
            string mode=V05Arg("-v06Mode","All");
            Check(new[]{"All","Systems","World","Combat","Campaign"}.Contains(mode),"v06 recognized independent test mode "+mode);
            if(mode=="All"||mode=="Systems")yield return V06Systems();
            if(mode=="All"||mode=="World")yield return V06WorldRoutes();
            if(mode=="All"||mode=="Combat")yield return V06CombatTrials();
            if(mode=="All"||mode=="Campaign")yield return V06Campaign();
            File.WriteAllLines(Path.Combine(output,"world.csv"),v06World);File.WriteAllLines(Path.Combine(output,"combat.csv"),v06Combat);
            Time.timeScale=1;
        }
        IEnumerator V06New(int stage=1)
        {
            g.StartVoyage();g.Run.maxIsland=9;if(stage>1){g.ShowMap();g.Travel(stage);}g.SkipCinematic();Time.timeScale=v06Speed;v05DashUntil=0;
            yield return new WaitForSeconds(.2f);
        }
        IEnumerator V06Systems()
        {
            Check(ExpeditionContent.Species.Length==119&&ExpeditionContent.Species.Take(108).Count(s=>s.endemic)==18,"v06 stable 119 IDs include eighteen endemic species");
            var rows=new List<string>{"island,lure,native,total,native_fraction,max_overlap"};
            for(int island=0;island<9;island++)for(int lure=0;lure<4;lure++){
                var pool=ExpeditionContent.IslandPool(island+1,lure);var ids=pool.Select(s=>s.id).ToArray();int native=pool.Count(s=>s.island==island);
                float overlap=0;for(int other=0;other<9;other++)if(other!=island)overlap=Mathf.Max(overlap,ids.Intersect(ExpeditionContent.IslandPool(other+1,lure).Select(s=>s.id)).Count()/(float)pool.Count);
                Check(pool.Count==ids.Distinct().Count()&&native/(float)pool.Count>.7f&&overlap<.3f,"island "+(island+1)+" lure "+lure+" unique runtime pool has >70% native and <30% overlap with every other island");
                Check(pool.Where(s=>s.endemic).All(s=>s.island==island)&&pool.Count(s=>s.endemic)==2,"island "+(island+1)+" lure "+lure+" retains two exclusive basic-access specimens");
                for(int habitat=0;habitat<3;habitat++){var local=ExpeditionContent.IslandPool(island+1,lure,habitat);Check(local.Count(s=>s.island==island)>=2&&local.All(s=>s.habitat==habitat),"island "+(island+1)+" lure "+lure+" habitat "+habitat+" is accessible and internally consistent");}
                rows.Add((island+1)+","+lure+","+native+","+pool.Count+","+F(native/(float)pool.Count)+","+F(overlap));
            }
            File.WriteAllLines(Path.Combine(output,"ecology.csv"),rows);
            yield return V06New();g.Run.coins=20000;Shop();
            Check(ShopCatalog.All.Length==38,"v06 workshop contains 23 existing plus 6 general plus 9 research offers");
            for(int island=0;island<9;island++){
                string id="mastery"+island;int wallet=g.Run.coins;
                Check(!g.Buy(id)&&g.Run.coins==wallet,"research "+island+" cannot be bought before a legitimate specimen record");
                foreach(int speciesId in IslandMastery.RequiredSpecimens(island)){
                    var s=ExpeditionContent.Species[speciesId];
                    Check(!IslandMastery.RecordNaturalKill(g.Run,s,island,s.habitat,false,false)&&!IslandMastery.RecordNaturalKill(g.Run,s,island,s.habitat,true,true)&&!IslandMastery.RecordNaturalKill(g.Run,s,(island+1)%9,s.habitat,true,false)&&!IslandMastery.RecordNaturalKill(g.Run,s,island,(s.habitat+1)%3,true,false),"specimen "+speciesId+" rejects no-source summoned foreign-island and wrong-habitat records");
                    Check(IslandMastery.RecordNaturalKill(g.Run,s,island,s.habitat,true,false)&&!IslandMastery.RecordNaturalKill(g.Run,s,island,s.habitat,true,false),"specimen "+speciesId+" valid provenance records exactly once (data fixture)");
                    int rank=IslandMastery.Level(g.Run,island),price=g.Price(id);wallet=g.Run.coins;
                    Check(price==55+island*8+rank*40&&g.Buy(id)&&IslandMastery.Level(g.Run,island)==rank+1&&g.Run.coins==wallet-price,"specimen "+speciesId+" unlocks a paid rank with exact wallet deduction");
                    wallet=g.Run.coins;Check(!g.Buy(id)&&g.Run.coins==wallet,"research "+island+" next exploration gate rejects repeated purchase without spending");
                }
                g.Run.habitatRecords[island]=7;int third=g.Price(id);wallet=g.Run.coins;
                Check(g.Buy(id)&&IslandMastery.Level(g.Run,island)==3&&g.Run.coins==wallet-third,"research "+island+" completed three-habitat fixture opens only the third paid rank");
                wallet=g.Run.coins;Check(!g.Buy(id)&&g.Run.coins==wallet,"research "+island+" finite cap cannot consume more coins");
            }
            foreach(string id in new[]{"reload","handling","precision","bearing","salvage","dressing"}){
                var offer=ShopCatalog.Find(id);for(int rank=0;rank<3;rank++){int price=g.Price(id),wallet=g.Run.coins;Check(g.Buy(id)&&offer.level(g.Run)==rank+1&&g.Run.coins==wallet-price,"general upgrade "+id+" rank "+(rank+1)+" costs its exact displayed price");}
                int capped=g.Run.coins;Check(!g.Buy(id)&&g.Run.coins==capped,"general upgrade "+id+" has an enforced three-rank cap");
            }
            g.UI.ShowIslandMastery();yield return null;Capture("research-workshop-unlocked");
            g.CloseShop();int savedWallet=g.Run.coins;g.Checkpoint(false);g.ReturnHarbor();g.StartVoyage(true);Time.timeScale=v06Speed;yield return null;
            Check(g.Run.coins==savedWallet&&Enumerable.Range(0,9).All(i=>IslandMastery.Level(g.Run,i)==3&&IslandMastery.SpecimenCount(g.Run,i)==2)&&g.Run.fieldDressingLevel==3,"research specimens paid ranks general upgrades and wallet survive actual checkpoint reload");
            var legacy=new RunData{masteryLevels=null,endemicRecords=new[]{1}};IslandMastery.Ensure(legacy);Check(legacy.masteryLevels.Length==9&&legacy.endemicRecords.Length==9&&IslandMastery.SpecimenCount(legacy,0)==1&&IslandMastery.Level(legacy,8)==0,"legacy null and short research arrays migrate safely without granting abilities");
            yield return V06MigrantPickup();yield return V06OriginNormalization();yield return V06EquipmentConsumers();yield return V06CrabBreach();yield return V06NaturalEliteAnchors();
        }
        IEnumerator V06MigrantPickup()
        {
            yield return V06New(2);int before=g.Run.coins;
            // Physical loot fixtures isolate provenance bookkeeping, not natural
            // blueprint acquisition. The Campaign mode uses real casts/kills.
            var old=new CatchData{speciesId=13,habitat=1,fieldSample=true,value=25};
            yield return V06PickupFixture(old);
            Check(g.Run.habitatRecords[1]==0&&g.Run.habitatRecords[0]==0&&g.Run.coins==before,"old fish without v06 origin cannot mint research rewards");
            yield return V06PickupFixture(new CatchData{speciesId=1,habitat=1,fieldSample=true,naturalHook=true,caughtIsland=1,value=25});
            Check(g.Run.habitatRecords[1]==2&&g.Run.habitatRecords[0]==0,"migrating fish records its actual caught island instead of the species native island");
            yield return V06PickupFixture(new CatchData{speciesId=12,habitat=0,fieldSample=true,naturalHook=true,caughtIsland=1,value=25});
            yield return V06PickupFixture(new CatchData{speciesId=14,habitat=2,fieldSample=true,naturalHook=true,caughtIsland=1,value=25});
            Check(g.Run.habitatRecords[1]==7&&g.Run.coins==before+35,"three physical habitat pickups pay exactly one stipend at the actual island");
            yield return V06PickupFixture(new CatchData{speciesId=1,habitat=1,fieldSample=true,naturalHook=true,caughtIsland=1,value=25});
            Check(g.Run.coins==before+35,"repeat migrant pickup cannot repeat the stipend");
        }
        IEnumerator V06PickupFixture(CatchData data)
        {
            var obj=new GameObject("v06 provenance pickup fixture");obj.transform.position=g.Player.transform.position+g.Player.transform.forward;
            var loot=obj.AddComponent<FishLoot>();loot.Init(g,data,Quaternion.identity);g.Loot.Add(loot);g.Player.PickUp(loot);Check(g.Player.HeldFish==loot,"physical provenance fixture is picked up within normal interaction range");g.Player.StowHeld();yield return null;
        }
        IEnumerator V06EquipmentConsumers()
        {
            yield return V06New();g.Player.Equip(WeaponKind.Revolver);yield return new WaitForSeconds(.35f);g.Player.Fire();
            float start=Time.time;g.Player.Reload();while(g.Player.Reloading)yield return null;yield return null;float baseline=Time.time-start;
            g.Run.reloadLevel=3;g.Player.Fire();start=Time.time;g.Player.Reload();while(g.Player.Reloading)yield return null;yield return null;float trained=Time.time-start;
            Check(trained<baseline*.87f&&trained>baseline*.7f,"paid reload training shortens a real reload by its 1.24 speed factor");
            g.Run.precisionReloadLevel=3;Check(Mathf.Approximately(g.Player.ReloadWindowStart,.475f)&&Mathf.Approximately(g.Player.ReloadWindowEnd,.795f),"precision rail expands the actual shared reload window to 47.5..79.5 percent");
            g.Run.fieldDressingLevel=3;g.Run.medkits=1;g.Run.health=10;g.UseUtility("medkit");Check(Mathf.Approximately(g.Run.health,79)&&g.Run.medkits==0,"upgraded real medkit restores 69 health and consumes one purchased-use slot");
            g.Run.handlingLevel=0;g.Run.carbine=true;g.Player.Equip(WeaponKind.Carbine);yield return new WaitForSeconds(.4f);g.Player.Fire();float bloom=g.Player.AimBloom;yield return new WaitForSeconds(.03f);
            Check(bloom>0&&g.Player.AimBloom>=bloom*.95f,"carbine spread remains during the intentional post-shot recovery delay");
            yield return new WaitForSeconds(.1f);float baselineBloom=g.Player.AimBloom;
            yield return new WaitForSeconds(.4f);g.Run.handlingLevel=3;g.Player.Fire();yield return new WaitForSeconds(.13f);float trainedBloom=g.Player.AimBloom;
            Check(baselineBloom<bloom&&trainedBloom<baselineBloom,"actual paid handling reduces more carbine spread over the same 0.13 second ceasefire");
            // An exact step in fishing exercises the real consumer with the same input.
            At(new Vector3(0,0,23),new Vector3(0,0,50));g.Player.SetRod(true);g.CastCharge=.5f;g.Cast();g.FishingAge=3;g.Tension=.7f;g.Run.reelBearingLevel=0;g.TickFishing(false,.1f);float tension=g.Tension;
            g.Tension=.7f;g.Run.reelBearingLevel=3;g.TickFishing(false,.1f);Check(.7f-g.Tension>(.7f-tension)*1.5f,"line bearing increases real released-line tension relief");g.CancelFishing();
            yield return V06MasteryConsumers();
        }
        Enemy V06Enemy(int id,bool elite=false)
        {
            g.SetState(VoyageState.Combat);At(new Vector3(0,0,6),new Vector3(0,2,13));g.Player.Equip(WeaponKind.Revolver);
            var enemy=new GameObject("v06 encounter fixture "+id).AddComponent<Enemy>();enemy.InitSpecies(g,ExpeditionContent.Species[id],elite,new Vector3(0,g.World.GroundAt(new Vector3(0,0,13))+1,13));return enemy;
        }
        IEnumerator V06MasteryConsumers()
        {
            for(int island=0;island<9;island++){
                yield return V06New();g.Run.masteryLevels[island]=1;var synergy=g.Player.Synergy;
                var enemy=V06Enemy(island==3?2:0,island==3);yield return null;
                // Small pipeline fixtures isolate each effect. Full Campaign and
                // Combat trials below never change a living enemy's health.
                switch(island){
                    case 0:
                        g.Player.Dash(Vector3.right);
                        Check(Mathf.Approximately(synergy.TargetDamageMultiplier(enemy,false),1)&&Mathf.Approximately(synergy.TargetDamageMultiplier(enemy,true),1.14f)&&Mathf.Approximately(synergy.TargetDamageMultiplier(enemy,true),1),"pine research actual dash primes exactly one weak-point damage modifier");break;
                    case 1:
                        var other=new GameObject("v06 refraction neighbour").AddComponent<Enemy>();other.InitSpecies(g,ExpeditionContent.Species[0],false,enemy.transform.position+Vector3.right*2);float otherHp=other.health;
                        enemy.Hit(10,true,false,false);synergy.OnHit(enemy,true,10);float after=other.health;synergy.OnHit(enemy,true,10);
                        Check(otherHp>after&&Mathf.Approximately(other.health,after),"coral actual neighbour takes one nonrecursive refraction and respects cooldown");break;
                    case 2:
                        g.Run.health=60;enemy.NaturalHook=true;enemy.CaughtIsland=0;enemy.CaughtHabitat=0;enemy.Hit(100000,false,false,false);
                        Check(Mathf.Approximately(g.Run.health,64),"root extraction runs through real EnemyKilled provenance and heals four (kill fixture)");break;
                    case 3:
                        float charge=Time.time;while(enemy&&enemy.Elite.Windup<=0&&Time.time-charge<5)yield return null;
                        Check(enemy&&enemy.Elite.Windup>0&&Mathf.Approximately(synergy.TargetDamageMultiplier(enemy,true),1.15f)&&Mathf.Approximately(synergy.TargetDamageMultiplier(enemy,true),1),"sun research recognizes a real elite windup and has a cooldown");break;
                    case 4:
                        yield return new WaitForSeconds(.4f);g.Player.AimAt(new Vector3(0,20,50));g.Player.Fire();g.Player.Reload();
                        while(g.Player.Reloading&&g.Player.ReloadProgress<.6f)yield return null;
                        g.Player.Reload();while(g.Player.Reloading)yield return null;yield return null;
                        Check(Mathf.Approximately(synergy.ShotMultiplier(),1.1f)&&Mathf.Approximately(synergy.ShotMultiplier(),1),"wreck research actual precision reload primes and consumes a single next-shot bonus");break;
                    case 5:
                        At(enemy.transform.position+Vector3.back*3,enemy.transform.position);g.Player.Dash(Vector3.right);
                        var field=typeof(Enemy).GetField("slowUntil",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
                        Check(field!=null&&(float)field.GetValue(enemy)>Time.time,"frost research actual dash applies enemy slow in its five-metre radius");break;
                    case 6:
                        g.Player.Dash(Vector3.right);g.Player.TakeDamage(10);yield return new WaitForSeconds(.35f);float hp=g.Run.health;g.Player.TakeDamage(10);float guarded=hp-g.Run.health;
                        yield return new WaitForSeconds(.65f);hp=g.Run.health;g.Player.TakeDamage(10);
                        Check(Mathf.Abs(guarded-8)<.05f&&Mathf.Abs(hp-g.Run.health-10)<.05f,"storm research actual avoided damage protects one later injury then expires");break;
                    case 7:
                        enemy.health=enemy.maxHealth*.25f;
                        Check(Mathf.Approximately(synergy.TargetDamageMultiplier(enemy,true),1.2f)&&Mathf.Approximately(synergy.TargetDamageMultiplier(enemy,true),1),"cinder conditional modifier requires wounded target and respects cooldown");break;
                    case 8:
                        enemy.Hit(10,true,false,false);synergy.OnHit(enemy,true,10);g.Run.shotgun=true;g.Player.Equip(WeaponKind.Scattergun);
                        Check(Mathf.Approximately(synergy.TargetDamageMultiplier(enemy,true),1.1f)&&Mathf.Approximately(synergy.TargetDamageMultiplier(enemy,true),1),"mirror research recognizes the real weapon change and cannot repeat without a cooldown");break;
                }
                yield return null;
            }
            yield return V06New();
        }
        IEnumerator V06CombatTrials()
        {
            // Paired starter-vs-paid fixture loadouts share a seed, species and
            // arena. Actual aiming, shots, finite health and enemy AI are retained.
            for(int rank=0;rank<=3;rank+=3)foreach(int id in new[]{0,4,8})yield return V06CombatTrial(id,false,rank,false);
            yield return V06CombatTrial(4,true,0,false);yield return V06CombatTrial(4,true,3,false);
            for(int island=0;island<9;island++)yield return V06CombatTrial(island*12+2,true,Mathf.Min(4,island/2),true);
        }
        EncounterTarget V06PriorityTarget(Enemy enemy)
        {
            if(!enemy)return null;
            if(enemy.Elite&&enemy.Elite.Anchor&&!enemy.Elite.Anchor.Dead)return enemy.Elite.Anchor;
            if(!enemy.Regional||enemy.Regional.Activations<=0)return null;
            return FindObjectsOfType<EncounterTarget>().Where(t=>t&&!t.Dead&&new[]{"根芽","回收浮灯","导流角","镜像锚"}.Any(prefix=>t.Label.StartsWith(prefix))&&GameDirector.FlatDistance(t.transform.position,enemy.transform.position)<24)
                .OrderBy(t=>GameDirector.FlatDistance(t.transform.position,g.Player.transform.position)).FirstOrDefault();
        }
        Vector3 V06DeviceAim(EncounterTarget target)
        {
            var core=target.GetComponentsInChildren<Collider>().FirstOrDefault(c=>c.enabled&&c.name=="Breakable core");
            return core?core.bounds.center:target.transform.position;
        }
        IEnumerator V06CombatTrial(int id,bool elite,int rank,bool waitRegional)
        {
            yield return V06New(id/12+1);g.Run.seed=61000+id;g.Rng=new System.Random(g.Run.seed);UnityEngine.Random.InitState(g.Run.seed);
            g.Run.easy=false;g.Run.weaponLevel=rank;g.Run.hullLevel=elite?3:0;g.Run.health=g.Run.MaxHealth;g.Run.medkits=elite?2:0;
            var enemy=V06Enemy(id,elite);float initialMax=enemy.maxHealth,started=Time.time,damage=0,previous=g.Run.health,firstShot=-1,ownerDeath=-1;int shots=0,attacks=0,regional=0,corner=0;bool photographed=false;
            var patrol=new[]{new Vector3(-8,0,3),new Vector3(8,0,3),new Vector3(8,0,10),new Vector3(-8,0,10)};
            while(g.State==VoyageState.Combat&&g.Run.health>0&&Time.time-started<150){
                if(GameDirector.FlatDistance(g.Player.transform.position,patrol[corner])<1)corner=(corner+1)%patrol.Length;V05Move(patrol[corner]);
                if(enemy){attacks=Mathf.Max(attacks,enemy.AttackExecutions);regional=Mathf.Max(regional,enemy.Regional?enemy.Regional.Activations:0);}
                if((!enemy||enemy.dead)&&ownerDeath<0)ownerDeath=Time.time;
                bool fire=!waitRegional||regional>0&&attacks>=2||enemy&&enemy.Spec.ecology==EcologyStyle.WreckScavenger&&enemy.health>enemy.maxHealth*.65f;
                var target=enemy&&!enemy.dead?enemy:g.Enemies.FirstOrDefault(e=>e&&!e.dead);
                if(target&&fire){var device=V06PriorityTarget(target);g.Player.AimAt(device?V06DeviceAim(device):V05Aim(target));if(g.Player.Fire()){shots++;if(firstShot<0)firstShot=Time.time;}if(g.Player.QuickReloadAvailable&&g.Player.ReloadProgress>.59f&&g.Player.ReloadProgress<.69f)g.Player.Reload();}
                if(!photographed&&enemy&&enemy.AttackExecutions>0){Capture("combat-"+id+"-"+(elite?"elite":"normal")+"-rank-"+rank);photographed=true;}
                damage+=Mathf.Max(0,previous-g.Run.health);if(g.Run.health<g.Run.MaxHealth*.4f&&g.Run.medkits>0)g.UseUtility("medkit");previous=g.Run.health;
                yield return null;
            }
            if(enemy){attacks=Mathf.Max(attacks,enemy.AttackExecutions);regional=Mathf.Max(regional,enemy.Regional?enemy.Regional.Activations:0);}
            if(ownerDeath<0&&(!enemy||enemy.dead))ownerDeath=Time.time;
            v06Combat.Add(id+","+elite+","+rank+","+g.Run.seed+","+F(Time.time-started)+","+waitRegional+","+F(firstShot<0?-1:firstShot-started)+","+F(ownerDeath<0||firstShot<0?-1:ownerDeath-firstShot)+","+shots+","+attacks+","+regional+","+F(damage)+","+F(g.Run.health)+","+g.State);
            File.WriteAllLines(Path.Combine(output,"combat.csv"),v06Combat);
            Check(g.State==VoyageState.Sailing&&g.Run.health>0&&shots>0,"v06 "+(elite?"elite":"ordinary")+" "+id+" rank "+rank+" resolves with real starter-weapon shots and finite health");
            if(elite)Check(attacks>=(waitRegional?2:1)&&(!waitRegional||regional>=1)&&initialMax>ExpeditionContent.Species[id].hp*2,"elite "+id+" "+(waitRegional?"behavior observation":"immediate-fire trial")+" executes attacks and retains meaningful endurance");
            Check(g.Player.InvulnerableUntil!=float.PositiveInfinity,"combat "+id+" has no permanent test immunity");
        }
        sealed class V06EndOfPoolRandom : System.Random
        {
            // Explicit fixture selection: maximum native ID in this habitat and
            // an elite rarity roll. Physics still begins with a real cast/reel.
            readonly double roll;
            public V06EndOfPoolRandom(double value=.01){roll=value;}
            public override double NextDouble(){return roll;}
            public override int Next(int maxValue){return Math.Max(0,maxValue-1);}
            public override int Next(int minValue,int maxValue){return Math.Max(minValue,maxValue-1);}
        }
        IEnumerator V06OriginNormalization()
        {
            int[] ids={97,1,13},stages={1,2,2};
            for(int i=0;i<ids.Length;i++){
                yield return V06New(stages[i]);var enemy=V06Enemy(ids[i]);float maximum=enemy.maxHealth,health=enemy.health;var species=enemy.Spec;
                int island=stages[i]-1;enemy.SetCatchOrigin(island,species.habitat,true);float scale=(1+island*.19f)/(1+species.island*.19f);
                Check(enemy.Spec==species&&enemy.Spec==ExpeditionContent.Species[ids[i]]&&enemy.CombatIsland==island&&enemy.Spec.ecology==(EcologyStyle)species.island,"origin fixture "+ids[i]+" retains native identity and ecology while using the encountered island's combat budget");
                Check(Mathf.Abs(enemy.maxHealth-maximum*scale)<.02f&&Mathf.Abs(enemy.health-health*scale)<.02f&&enemy.CatchValue==25+island*7+ids[i]%12*2,"origin fixture "+ids[i]+" adjusts HP and appraisal exactly to island "+stages[i]+" without changing a local fish");
            }
            for(int stage=1;stage<=2;stage++){
                yield return V06New(stage);g.Run.easy=false;g.Rng=new V06EndOfPoolRandom(.95);
                At(new Vector3(0,0,23),new Vector3(0,0,50));g.Player.SetRod(true);g.CastCharge=.75f;g.Cast();float start=Time.time;
                while(g.State==VoyageState.Fishing&&Time.time-start<45){g.TickFishing(!g.Surge,Time.deltaTime);yield return null;}
                var enemy=g.Enemies.FirstOrDefault(e=>e&&!e.dead);int expected=stage==1?97:1;
                Check(enemy&&enemy.Spec.id==expected&&enemy.NaturalHook&&!enemy.elite&&enemy.quality==0&&enemy.CaughtIsland==stage-1,"real fixed-flow cast brings native-island "+(stage==1?9:1)+" migrant into island "+stage);
                if(enemy){float localHp=ExpeditionContent.Species[(stage-1)*12+1].hp*1.38f;
                    Check(Mathf.Abs(enemy.maxHealth-localHp)<.02f&&enemy.CombatIsland==stage-1&&enemy.CatchValue==27+(stage-1)*7,"naturally hooked migrant "+expected+" actually enters with local ordinary-fish HP damage tier and base value");
                }
                if(stage==1&&enemy){
                    // Let the actual Split attack call SpawnMinion. A child has
                    // the encounter budget, never the parent's catch authority.
                    start=Time.time;Enemy minion=null;
                    while(g.State==VoyageState.Combat&&Time.time-start<9){minion=g.Enemies.FirstOrDefault(e=>e&&e.Minion);if(minion)break;yield return null;}
                    Check(minion&&enemy.AttackExecutions>0,"native-island-nine migrant naturally executes Split and creates its real summoned child");
                    if(minion){
                        Check(minion.Spec.id==96&&minion.CombatIsland==0&&Mathf.Abs(minion.maxHealth-ExpeditionContent.Species[0].hp*1.38f*.35f)<.02f,"migrating parent's child inherits island-one damage tier and local same-family HP times 0.35");
                        Check(minion.Summoned&&minion.Minion&&!minion.NaturalHook&&minion.CaughtIsland<0&&minion.CaughtHabitat<0,"inherited combat budget does not grant summoned child any natural research provenance");
                        int records=IslandMastery.SpecimenCount(g.Run,8),habitats=g.ResearchRecord,coins=g.Run.coins;start=Time.time;
                        while(minion&&!minion.dead&&g.State==VoyageState.Combat&&Time.time-start<8){g.Player.AimAt(V05Aim(minion));g.Player.Fire();yield return null;}
                        Check((!minion||minion.dead)&&IslandMastery.SpecimenCount(g.Run,8)==records&&g.ResearchRecord==habitats&&g.Run.coins==coins&&!g.Loot.Any(f=>f&&f.Data.speciesId==96),"real summoned-child kill creates no endemic blueprint research source loot or sale coins");
                    }
                }
            }
            yield return V06New();g.BeginCombat(false);
            Check(g.Enemies.Count>0&&g.Enemies.All(e=>!e.NaturalHook),"direct combat fixtures without a completed fishing cycle cannot claim natural catch provenance");
        }
        IEnumerator V06CrabBreach()
        {
            yield return V06New();g.Run.easy=false;var crab=V06Enemy(4,true);float start=Time.time;
            while(crab&&crab.Elite.Windup<=0&&Time.time-start<4)yield return null;
            int attacks=crab.AttackExecutions,shots=0;float weakDamage=0;bool breached=false;start=Time.time;
            while(crab&&crab.Elite.Windup>0&&Time.time-start<2){
                g.Player.AimAt(V05Aim(crab));float before=crab.health;if(g.Player.Fire()){shots++;if(crab.lastWeak)weakDamage+=Mathf.Max(0,before-crab.health);}
                if(crab.Elite.Cue.Contains("眼甲裂开")&&crab.Elite.DamageFactor(true)>1){breached=true;break;}
                yield return null;
            }
            Check(breached&&shots>=1&&weakDamage>=crab.maxHealth*.12f&&crab.AttackExecutions==attacks&&crab.Elite.Windup>0,"elite crab's actual weak-point shots during windup open a temporary breach without cancelling its committed attack");
            if(crab){start=Time.time;while(crab&&crab.AttackExecutions==attacks&&Time.time-start<3)yield return null;
                Check(crab&&crab.AttackExecutions>attacks,"breached crab still executes its real attack");
                start=Time.time;while(crab&&crab.Elite.DamageFactor(true)>1&&Time.time-start<4.5f)yield return null;
                Check(crab&&Mathf.Approximately(crab.Elite.DamageFactor(true),1),"crab breach and recovery weak-point bonus expires without more weak-point pressure");
            }
            yield return V06New();
        }
        IEnumerator V06NaturalEliteAnchors()
        {
            foreach(int id in new[]{5,9}){
                yield return V06New();g.Run.easy=false;g.Run.lureTier=g.Run.selectedLure=id==9?2:0;g.Run.salvageLevel=3;g.Rng=new V06EndOfPoolRandom();
                int habitat=id%3;At(new Vector3(0,0,23),new Vector3((habitat-1)*30,0,48));g.Player.SetRod(true);g.CastCharge=.75f;g.Cast();
                float start=Time.time;while(g.State==VoyageState.Fishing&&Time.time-start<45){g.TickFishing(!g.Surge,Time.deltaTime);yield return null;}
                var enemy=g.Enemies.FirstOrDefault(e=>e&&!e.dead);
                Check(enemy&&enemy.Spec.id==id&&enemy.elite&&enemy.NaturalHook&&enemy.CaughtIsland==0&&enemy.CaughtHabitat==habitat,"elite "+id+" deterministic-selection fixture passes through real cast reel and LaunchToward");
                if(!enemy||enemy.Spec.id!=id||!enemy.Elite)continue;
                start=Time.time;while(enemy&&(enemy.Airborne||!enemy.Elite.Anchor)&&Time.time-start<7)yield return null;
                var anchor=enemy?enemy.Elite.Anchor:null;
                Check(anchor&&!enemy.Airborne,"elite "+id+" only creates its symbiosis anchor after actual flight ends");if(!anchor)continue;
                float ground=g.World.GroundAt(anchor.transform.position);g.Player.AimAt(anchor.transform.position);yield return null;
                Vector3 viewport=g.Player.View.WorldToViewportPoint(anchor.transform.position);
                bool visible=!Physics.Linecast(g.Player.View.transform.position,anchor.transform.position,SeaWorld.GroundMask,QueryTriggerInteraction.Ignore);
                Check(ground>=-.3f&&anchor.transform.position.y-ground>=1.2f&&viewport.z>0&&viewport.x>0&&viewport.x<1&&viewport.y>0&&viewport.y<1&&visible,"elite "+id+" naturally landed anchor is above reachable shore with an unobstructed visible aiming point");
                Capture("natural-elite-"+id+"-anchor");float factor=enemy.Elite.DamageFactor(false);start=Time.time;int shots=0;
                while(anchor&&g.State==VoyageState.Combat&&g.Run.health>0&&Time.time-start<8){g.Player.AimAt(anchor.transform.position);if(g.Player.Fire())shots++;yield return null;}
                Check(!anchor&&enemy&&enemy.Elite.Anchor==null&&enemy.Elite.DamageFactor(false)>factor&&shots>0,"elite "+id+" visible anchor breaks through real gunfire and releases its protective mechanic");
                Capture("natural-elite-"+id+"-anchor-result");
                // The three purchased salvage ranks and lure are disclosed
                // fixture equipment. No HP grants, direct damage or immunity.
                start=Time.time;int waypoint=0;var patrol=new[]{new Vector3(-7,0,6),new Vector3(7,0,6),new Vector3(7,0,12),new Vector3(-7,0,12)};
                while(g.State==VoyageState.Combat&&g.Run.health>0&&Time.time-start<90){
                    if(GameDirector.FlatDistance(g.Player.transform.position,patrol[waypoint])<1)waypoint=(waypoint+1)%patrol.Length;V05Move(patrol[waypoint]);
                    var target=g.Enemies.FirstOrDefault(e=>e&&!e.dead&&!e.Summoned);if(!target)target=g.Enemies.FirstOrDefault(e=>e&&!e.dead);
                    if(target){var device=V06PriorityTarget(target);g.Player.AimAt(device?V06DeviceAim(device):V05Aim(target));g.Player.Fire();if(g.Player.QuickReloadAvailable&&g.Player.ReloadProgress>.59f&&g.Player.ReloadProgress<.69f)g.Player.Reload();}
                    yield return null;
                }
                Check(g.State==VoyageState.Sailing&&g.Run.health>0,"natural elite "+id+" and its minions are defeated with real fire movement and finite starting health");
                var loot=g.Loot.FirstOrDefault(f=>f&&f.Data.speciesId==id&&f.Data.elite);
                if(loot){var data=loot.Data;float baseValue=ExpeditionContent.Species[id].value*1.65f*(data.quality==2?2.6f:data.quality==1?1.6f:1)*(data.airshot?1.25f:1)*(data.weakshot?1.15f:1)*(1+g.Run.fortuneRelics*.15f)*(g.Run.route==RouteKind.Shoal?1.15f:1);int expected=Mathf.RoundToInt(baseValue*1.3f);
                    Check(data.naturalHook&&data.caughtIsland==0&&data.value==expected&&data.value>Mathf.RoundToInt(baseValue),"natural elite "+id+" actual drop appraisal applies exactly the paid 30-percent salvage premium to its real quality weakshot and route factors");
                }else Check(false,"natural elite "+id+" must leave its own elite catch for salvage-price verification");
                Capture("natural-elite-"+id+"-salvage-result");
            }
            yield return V06New();
        }
        IEnumerator V06Campaign()
        {
            campaignOk=true;campaignDistance=0;campaignShots=0;campaignStart=Time.time;campaignTrace.Clear();campaignTrace.Add("seconds,seed,island,event,coins,health,quest_step,walked_metres,shots");
            g.ReturnHarbor();yield return null;CampaignRequire(CampaignClick("开始远征"),"v06 new expedition starts through the visible harbor button");yield return null;Time.timeScale=v06Speed;v05DashUntil=0;
            if(!campaignOk)yield break;
            int[] habitats={0,0,2,2,1};
            foreach(int habitat in habitats){
                yield return V06NaturalCatch(habitat);if(!campaignOk)yield break;
                if(g.Run.health<75&&g.Run.coins>=25){yield return CampaignWalk(g.World.ShopPoint,"research_healing",3.2f);if(!campaignOk)yield break;g.Interact();yield return null;yield return CampaignPurchase("heal");if(!campaignOk)yield break;CampaignClick("返回岛屿");yield return null;}
            }
            CampaignRequire(IslandMastery.SpecimenCount(g.Run,0)==2&&g.ResearchRecord==7,"five naturally hooked fights discover both local specimens and all three real habitats");if(!campaignOk)yield break;
            if(g.Run.coins<55){yield return V06NaturalCatch(1);if(!campaignOk)yield break;}
            yield return CampaignWalk(g.World.ShopPoint,"research_purchase",3.2f);if(!campaignOk)yield break;g.Interact();yield return null;
            CampaignRequire(CampaignClick("岛屿专研"),"sixth workshop tab exposes the actual research page");yield return null;
            int wallet=g.Run.coins,price=g.Price("mastery0");Capture("campaign-research-earned");
            CampaignRequire(price==55&&wallet>=price&&CampaignClick("金币 · 升级"),"earned fish sales can pay for the visible unlocked research offer");yield return null;
            CampaignRequire(IslandMastery.Level(g.Run,0)==1&&g.Run.coins==wallet-price,"research purchase via UI consumes exact earned gold and gives only one ability rank");
            CampaignClick("返回岛屿");yield return null;int savedCoins=g.Run.coins;g.TogglePause();yield return null;
            CampaignRequire(CampaignClick("保存进度并返回港口"),"research campaign saves via the actual pause menu");yield return null;CampaignRequire(CampaignClick("继续 ·"),"research campaign resumes through the harbor button");yield return null;
            CampaignRequire(IslandMastery.Level(g.Run,0)==1&&IslandMastery.SpecimenCount(g.Run,0)==2&&g.ResearchRecord==7&&g.Run.coins==savedCoins,"naturally earned research progression survives save and reload");
            CampaignRequire(campaignShots>0&&campaignDistance>100&&g.Player.InvulnerableUntil!=float.PositiveInfinity,"research campaign used physical exploration and finite combat without health gear gold or quest grants");CampaignEvent("v06_research_completed");Capture("campaign-research-resumed");
        }
        IEnumerator V06NaturalCatch(int habitat)
        {
            yield return CampaignWalk(new Vector3(0,0,23),"research_pier_"+habitat,.7f);if(!campaignOk)yield break;
            g.Player.AimAt(new Vector3((habitat-1)*30,0,48));g.Player.SetRod(true);g.CastCharge=.75f;g.Cast();
            CampaignRequire(g.State==VoyageState.Fishing,"habitat "+habitat+" accepts a genuine cast from the walked pier");if(!campaignOk)yield break;
            float start=Time.time;while(g.State==VoyageState.Fishing&&Time.time-start<45){g.TickFishing(!g.Surge,Time.deltaTime);yield return null;}
            var enemy=g.Enemies.FirstOrDefault(e=>e&&!e.dead);
            CampaignRequire(enemy&&enemy.NaturalHook&&enemy.CaughtIsland==0&&enemy.CaughtHabitat==habitat&&ExpeditionContent.IslandPool(1,0,habitat).Contains(enemy.Spec),"real fishing generates legal provenance and the actual habitat pool species");if(!campaignOk)yield break;
            int specimen=enemy.Spec.id;yield return CampaignFight(new Vector3(0,0,13));if(!campaignOk)yield break;
            CampaignRequire(!ExpeditionContent.Species[specimen].endemic||IslandMastery.IsSpecimenKnown(g.Run,specimen),"naturally defeating the exclusive catch immediately archives its blueprint");
            var loot=g.Loot.LastOrDefault();CampaignRequire(loot,"natural fish leaves physical saleable loot");if(!campaignOk)yield break;
            start=Time.time;while(loot&&g.World.GroundAt(loot.transform.position)<-.3f&&Time.time-start<7)yield return null;
            start=Time.time;while(loot&&!g.Player.HeldFish&&Time.time-start<30){Vector3 before=g.Player.transform.position;V05Move(loot.transform.position,false);campaignDistance+=GameDirector.FlatDistance(before,g.Player.transform.position);g.Interact();yield return null;}
            CampaignRequire(g.Player.HeldFish==loot&&loot.Registered&&loot.Data.caughtIsland==0&&loot.Data.naturalHook,"normal shared pickup preserves natural catch provenance");if(!campaignOk)yield break;
            g.Player.StowHeld();yield return CampaignWalk(g.World.SellPoint,"research_sale",3);if(!campaignOk)yield break;
            int coins=g.Run.coins,value=g.Run.BagValue;g.Interact();yield return null;CampaignRequire(value>0&&g.Run.coins==coins+value&&g.Run.bag.Count==0,"actual market pays the full fish appraisal without fabricated coins");CampaignEvent("v06_natural_sale_"+habitat);
        }
        IEnumerator V06WorldRoutes()
        {
            for(int stage=1;stage<=9;stage++){
                yield return V06New(stage);var layout=g.World.Layout;
                Check(layout.Region==stage-1&&layout.LoopCount>=2&&layout.Points.Select(p=>p.y).Max()-layout.Points.Select(p=>p.y).Min()>1,"island "+stage+" has its own looping layout and meaningful height change");
                Vector3[] goals={g.World.SellPoint,g.World.ShopPoint,g.World.QuestPoint,g.SitePoint(0),g.SitePoint(1),g.SitePoint(2)};
                string[] labels={"market","workshop","guide","objective_a","objective_b","evidence"};
                for(int i=0;i<goals.Length;i++){
                    yield return V06Walk(goals[i],labels[i],2.2f);
                    if(v06WalkOk){yield return null;Capture("island-"+stage+"-"+labels[i]);}
                    yield return V06Walk(g.World.Spawn,"return_from_"+labels[i],1);
                }
                // Route stress follows the actual mission connection, including
                // wetland and frost bridges. No synthetic player teleport.
                yield return V06Walk(g.SitePoint(0),"mission_connection_a",2.2f);yield return V06Walk(g.SitePoint(1),"mission_connection_b",2.2f);
                var camera=g.Player.View.transform;Vector3 position=camera.position;Quaternion rotation=camera.rotation;var tools=camera.Find("Handheld tools");bool toolsActive=tools&&tools.gameObject.activeSelf;var canvas=FindObjectsOfType<Canvas>().First(c=>c.name=="Tidebreak UI");bool uiVisible=canvas.enabled;
                try{if(tools)tools.gameObject.SetActive(false);canvas.enabled=false;camera.position=new Vector3(65,66,41);camera.LookAt(new Vector3(0,2,-24));Capture("island-"+stage+"-aerial");}
                finally{camera.position=position;camera.rotation=rotation;if(tools)tools.gameObject.SetActive(toolsActive);canvas.enabled=uiVisible;}
                yield return V06Walk(g.World.Spawn,"final_return",1);
                Check(g.Player.InvulnerableUntil!=float.PositiveInfinity,"island "+stage+" route test never grants immunity");
            }
        }
        IEnumerator V06Walk(Vector3 goal,string label,float radius=2.2f)
        {
            v06WalkOk=true;var route=g.World.NavigationRoute(g.Player.transform.position,goal);float begin=Time.time,walked=0,maxFrame=0;bool recovered=false;int last=route.Length-1;
            Check(route.Length>=2,"route "+g.Run.stage+"/"+label+" supplies explicit navigation waypoints");
            for(int i=0;i<route.Length;i++){
                float tolerance=i==last?radius:.5f,started=Time.time;
                while(g.IsPlaying&&GameDirector.FlatDistance(g.Player.transform.position,route[i])>tolerance&&Time.time-started<9){
                    Vector3 before=g.Player.transform.position,delta=route[i]-before;delta.y=0;
                    g.Player.AimAt(route[i]+Vector3.up*1.6f);
                    if(g.Player.Motor.enabled)g.Player.Motor.Move(delta.normalized*Mathf.Min(delta.magnitude,5.2f*(1+g.Run.bootsLevel*.06f)*Time.deltaTime));
                    yield return null;
                    float distance=GameDirector.FlatDistance(before,g.Player.transform.position);walked+=distance;maxFrame=Mathf.Max(maxFrame,distance);
                    if(distance>Mathf.Max(2.5f,12*Time.deltaTime)||g.Player.transform.position.y<-.3f){recovered=true;v06WalkOk=false;break;}
                }
                if(GameDirector.FlatDistance(g.Player.transform.position,route[i])>tolerance+.1f)v06WalkOk=false;
                Vector3 p=g.Player.transform.position;v06World.Add(g.Run.stage+","+label+","+i+","+F(p.x)+","+F(p.y)+","+F(p.z)+","+F(g.World.GroundAt(p))+","+F(Time.time-begin)+","+F(walked)+","+F(maxFrame)+","+v06WalkOk);
                if(!v06WalkOk){Capture("route-failed-"+g.Run.stage+"-"+label+"-"+i);break;}
            }
            Check(v06WalkOk&&!recovered&&GameDirector.FlatDistance(g.Player.transform.position,goal)<=radius+.1f,"island "+g.Run.stage+" physically walks "+label+" using every route waypoint without a water respawn or teleport");
            File.WriteAllLines(Path.Combine(output,"world.csv"),v06World);
        }
    }
}
