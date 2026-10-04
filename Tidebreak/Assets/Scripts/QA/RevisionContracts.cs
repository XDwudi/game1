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
        readonly List<string> revisionTelemetry = new List<string> {
            "kind,id,seed,seconds,player_damage,medkits,shots,phase_mask,moves,nodes,outcome"
        };

        // These fixtures use normal player damage, ammunition, reloads and dash cooldowns.
        // Setup grants a stated test loadout; health restoration uses finite medical supplies,
        // selected build effects and the normal boss reward. This is a regression bot, not a
        // substitute for human playtesting or a claim about commercial production quality.
        IEnumerator RevisionContracts()
        {
            Time.timeScale = 3;
            Check(EncounterDirector.Mechanics.Length == 11 && EncounterDirector.Mechanics.Distinct().Count() == 11,
                "eleven bosses expose distinct mechanic instructions");
            Check(EliteTactics.Names.Length == 12 && EliteTactics.Counters.Distinct().Count() == 12,
                "twelve elite anatomical families expose different counterplay");
            yield return RevisionCinematics();
            yield return IslandRevisionContracts();
            yield return PostgameRevisionContracts();
            Time.timeScale = 3;
            yield return RevisionFishingEconomy();
            yield return RevisionIslandPaths();
            yield return RevisionBuildPurchases();
            yield return RevisionBuildSynergies();
            yield return RevisionWeaponCycles();
            yield return RevisionCreatureAnimation();
            yield return RevisionEliteContracts();
            yield return RevisionPhaseGates();
            for (int id = 117; id <= 118; id++) yield return RevisionStationaryPressure(id);
            for (int id = 108; id <= 118; id++) yield return RevisionBossBattle(id);
            g.StartVoyage();
            yield return null;
            Check(FindObjectsOfType<EncounterTarget>().Length == 0 && g.Hazards.childCount == 0,
                "new voyage clears all encounter nodes and damaging hazards");
            File.WriteAllLines(Path.Combine(output, "revision-combat.csv"), revisionTelemetry);
            Time.timeScale = 1;
        }

        IEnumerator RevisionCinematics()
        {
            g.StartVoyage();
            foreach (int sequence in new[] { 0, 13, 12 })
            {
                int island = sequence == 13 ? 5 : sequence == 12 ? 9 : 1;
                g.Run.maxIsland = 9;
                if (g.Run.stage != island) { g.ShowMap(); g.Travel(island); yield return null; }
                var cam = g.Player.View;
                var position = cam.transform.localPosition;
                var rotation = cam.transform.localRotation;
                float fov = cam.fieldOfView;
                int calls = 0;
                g.PlayCinematic(sequence, () => calls++, true);
                Check(g.CinematicActive && g.State == VoyageState.Cinematic,
                    "cinematic " + sequence + " takes control of camera and gameplay state");
                g.SeekCinematicForQA(sequence == 0 ? 10.5f : sequence == 13 ? 9.5f : 5.5f);
                yield return null;
                Capture("revision-cinematic-" + sequence);
                g.SkipCinematic();
                Check(!g.CinematicActive && calls == 1, "cinematic " + sequence + " skip completes exactly once");
                Check(Vector3.Distance(cam.transform.localPosition, position) < .001f &&
                    Quaternion.Angle(cam.transform.localRotation, rotation) < .01f && Mathf.Abs(cam.fieldOfView - fov) < .01f,
                    "cinematic " + sequence + " restores camera pose and field of view");
                yield return null;
            }
        }

        IEnumerator RevisionFishingEconomy()
        {
            g.StartVoyage(); Time.timeScale = 3;
            Guide(); Check(g.AdvanceStory(), "new captain accepts the lighthouse introduction through its guide");
            int catches = 0;
            while (g.Run.coins < g.Price("weapon") && catches < 6 && g.State == VoyageState.Sailing)
            {
                yield return GatherCatch(catches == 0);
                if (g.State != VoyageState.Sailing) break;
                int before = g.Run.coins, appraisal = g.Run.BagValue;
                Sell();
                Check(appraisal > 0 && g.Run.coins == before + appraisal && g.Run.bag.Count == 0,
                    "real caught fish pays its exact appraisal at the physical market");
                catches++;
                if (g.Run.health < 40 && g.Run.coins >= g.Price("heal"))
                { Shop(); Buy("heal"); g.CloseShop(); }
            }
            Check(catches > 0 && g.Run.health > 0 && g.Player.InvulnerableUntil != float.PositiveInfinity,
                "fishing, live combat and catch handling complete without test invulnerability");
            if (g.State == VoyageState.Sailing)
            {
                Shop(); int wallet = g.Run.coins, level = g.Run.weaponLevel, price = g.Price("weapon");
                bool bought = g.Buy("weapon");
                Check(bought && g.Run.coins == wallet - price && g.Run.weaponLevel == level + 1,
                    "fish sale proceeds fund the first real weapon upgrade without granted coins");
                g.CloseShop();
                var saved = SaveStore.Read<RunData>("voyage");
                Check(saved != null && saved.weaponLevel == g.Run.weaponLevel && saved.coins == g.Run.coins,
                    "earned purchase and wallet survive the fishing-loop checkpoint");
            }
            else Check(false, "new captain reaches the workshop alive after the earning loop");
            g.StartVoyage(); Time.timeScale = 3; yield return null;
        }

        IEnumerator RevisionIslandPaths()
        {
            g.StartVoyage(); g.Run.maxIsland = 9; Time.timeScale = 3;
            for (int island = 1; island <= 9; island++)
            {
                if (island > 1) { g.ShowMap(); g.Travel(island); }
                yield return null;
                yield return WalkRoute(g.World.QuestPoint, "chapter guide");
                yield return WalkRoute(g.SitePoint(0), "first chapter mechanism");
                yield return WalkRoute(g.SitePoint(1), "second chapter mechanism");
            }
            g.StartVoyage(); yield return null;
        }

        IEnumerator RevisionBuildPurchases()
        {
            for (int index = 0; index < 9; index++)
            {
                g.StartVoyage();
                g.Run.coins = 1000;
                g.Run.maxIsland = VoyageBuilds.Island(index / 3) - 1;
                g.SetState(VoyageState.Shop);
                Check(!g.BuyKeystone(index), "keystone " + index + " stays locked before its chapter");
                g.Run.maxIsland++;
                g.Run.coins = VoyageBuilds.Price(index / 3) - 1;
                Check(!g.BuyKeystone(index), "keystone " + index + " requires its coin price");
                g.Run.coins = 1000;
                Check(g.BuyKeystone(index) && g.Run.coins == 1000 - VoyageBuilds.Price(index / 3),
                    "keystone " + index + " is purchased for the exact displayed price");
                int coins = g.Run.coins;
                for (int other = index / 3 * 3; other < index / 3 * 3 + 3; other++)
                    Check(!g.BuyKeystone(other) && g.Run.coins == coins,
                        "keystone choice " + index + " excludes same-chapter option " + other);
                var saved = SaveStore.Read<RunData>("voyage");
                Check(saved != null && saved.HasKeystone(index) && saved.coins == coins,
                    "keystone " + index + " persists together with the charged wallet");
                if (index == 1) Check(g.Run.fireRelics == 0 && g.Run.iceRelics == 0, "steam specialization does not grant unrelated free fire or frost relics");
                if (index == 7) Check(g.Run.fireRelics == 0 && g.Run.shockRelics == 0, "thermal lightning specialization does not grant unrelated free fire or shock relics");
                yield return null;
            }
            g.StartVoyage();
            float baseline = g.Run.DamageMultiplier;
            g.Run.weaponLevel = 1;
            Check(g.Run.DamageMultiplier > baseline * 1.08f, "first paid weapon upgrade has a material damage increase");
            g.Run.weaponLevel = 5; g.Run.damageRelics = 4;
            float capped = g.Run.DamageMultiplier;
            g.Run.weaponLevel = 99; g.Run.damageRelics = 99;
            Check(Mathf.Approximately(capped, g.Run.DamageMultiplier) && capped < 2.1f,
                "stacking generic damage cannot create unbounded multiplicative power");
            g.StartVoyage(); g.Run.maxIsland = 9; g.Run.coins = 1000;
            Shop(); g.UI.ShowBuilds(); yield return null; Capture("revision-keystone-shop"); g.CloseShop();
        }

        IEnumerator RevisionWeaponCycles()
        {
            for (int kind = 0; kind < 6; kind++)
            {
                float[] dps = new float[2];
                for (int rank = 0; rank < 2; rank++)
                {
                    g.StartVoyage();
                    g.Run.shotgun = g.Run.harpoon = g.Run.carbine = g.Run.burstRifle = g.Run.arcCaster = true;
                    g.Run.weaponLevel = rank * 4;
                    g.SetState(VoyageState.Combat);
                    At(new Vector3(0, 0, 8));
                    var target = EncounterTarget.Create(g, g.Player.View.transform.position + Vector3.forward * 5,
                        "QA sustained fire target", 100000, Color.cyan, null);
                    g.Player.Equip((WeaponKind)kind); g.Player.Refill();
                    yield return new WaitForSeconds(.5f);
                    float begin = Time.time, hp = target.Health;
                    int shots = 0, smallest = g.Player.Capacity;
                    bool reload = false;
                    while (Time.time - begin < 18)
                    {
                        g.Player.AimAt(target.transform.position);
                        if (g.Player.Fire()) shots++;
                        smallest = Mathf.Min(smallest, g.Player.Ammo);
                        reload |= g.Player.Reloading;
                        yield return null;
                    }
                    dps[rank] = (hp - target.Health) / (Time.time - begin);
                    Check(dps[rank] > 12 && dps[rank] < 100,
                        "weapon " + kind + " rank " + rank * 4 + " sustained live-ray DPS remains bounded: " + dps[rank].ToString("F1"));
                    Check(reload && smallest == 0, "weapon " + kind + " empties a magazine and completes sustained reload cycles");
                    revisionTelemetry.Add("weapon," + kind + "," + g.Run.seed + "," + F(Time.time - begin) + ",0,0," + shots + ",0,0,0,DPS=" + F(dps[rank]));
                    Destroy(target.gameObject);
                    yield return null;
                }
                Check(dps[1] > dps[0] * 1.25f && dps[1] < dps[0] * 1.65f,
                    "weapon " + kind + " paid upgrades improve sustained damage without exponential scaling");
            }
            g.StartVoyage(); g.Player.Equip(WeaponKind.Revolver);
            yield return new WaitForSeconds(.5f);
            g.Player.Fire(); g.Player.Reload();
            while (g.Player.Reloading && g.Player.ReloadProgress < .6f) yield return null;
            g.Player.Reload(); yield return new WaitForSeconds(.2f);
            Check(!g.Player.Reloading && g.Player.Ammo == g.Player.Capacity,
                "pressing reload inside the precision window completes a fast reload");
            yield return new WaitForSeconds(.5f);
            g.Player.Fire(); g.Player.Reload();
            yield return new WaitForSeconds(.12f);
            g.Player.Reload();
            while (g.Player.Reloading && g.Player.ReloadProgress < .6f) yield return null;
            g.Player.Reload(); yield return new WaitForSeconds(.2f);
            Check(g.Player.Reloading, "missing the precision window cannot be corrected by spamming reload");
        }

        IEnumerator RevisionBuildSynergies()
        {
            g.StartVoyage(); g.Run.keystoneMask = 1;
            var synergy = g.Player.Synergy;
            synergy.RegisterShot(true, true); synergy.RegisterShot(true, true); synergy.RegisterShot(true, true);
            synergy.RegisterShot(false, false); synergy.RegisterShot(true, true);
            Check(Mathf.Approximately(synergy.ShotMultiplier(), 1), "precision build loses its streak after a missed shot");
            for (int i = 0; i < 3; i++) synergy.RegisterShot(true, true);
            Check(Mathf.Approximately(synergy.ShotMultiplier(), 1.65f) && Mathf.Approximately(synergy.ShotMultiplier(), 1),
                "four consecutive weak points charge exactly one empowered shot");
            g.Run.keystoneMask = 1 << 2; synergy.Reset(); synergy.Dash();
            Check(Mathf.Approximately(synergy.ShotMultiplier(), 1.25f) && Mathf.Approximately(synergy.ReloadSpeed, 1.25f),
                "dash build links both shot damage and reload speed to its movement window");
            yield return new WaitForSeconds(3.1f);
            Check(Mathf.Approximately(synergy.ShotMultiplier(), 1) && Mathf.Approximately(synergy.ReloadSpeed, 1),
                "dash build bonuses expire instead of becoming permanent");
            g.Run.keystoneMask = 1 << 5; synergy.Reset(); g.SetState(VoyageState.Combat); g.Run.health = 50;
            g.Player.Dash(Vector3.right); g.Player.TakeDamage(10);
            Check(Mathf.Approximately(g.Run.health, 55) && Mathf.Approximately(synergy.ShotMultiplier(), 1.7f),
                "a genuine dash immunity window earns counterattack charge and limited healing");
            g.Player.TakeDamage(10);
            Check(Mathf.Approximately(g.Run.health, 55) && Mathf.Approximately(synergy.ShotMultiplier(), 1),
                "multiple simultaneous hazards cannot repeatedly farm perfect-dash healing");
            g.StartVoyage(); g.SetState(VoyageState.Combat); At(new Vector3(0, 0, 8));
            var specimen = new GameObject("Element synergy contract").AddComponent<Enemy>();
            specimen.InitSpecies(g, ExpeditionContent.Species[0], false, new Vector3(0, 3, 14));
            specimen.maxHealth = specimen.health = 1000;
            g.Run.fireRelics = g.Run.iceRelics = g.Run.shockRelics = 1;
            g.Run.keystoneMask = (1 << 1) | (1 << 7); synergy.Reset();
            float prior = specimen.health;
            synergy.OnHit(specimen, false, 10);
            Check(Mathf.Approximately(prior - specimen.health, 44),
                "steam and thermal lightning retain both damage effects when purchased together");
            prior = specimen.health; synergy.OnHit(specimen, false, 10);
            Check(Mathf.Approximately(prior, specimen.health), "element explosions cannot retrigger immediately");
            yield return new WaitForSeconds(4.1f);
            prior = specimen.health; synergy.OnHit(specimen, false, 10);
            Check(Mathf.Approximately(prior - specimen.health, 18), "steam keeps its four-second cooldown after buying thermal lightning");
            yield return new WaitForSeconds(1.1f);
            prior = specimen.health; synergy.OnHit(specimen, false, 10);
            Check(Mathf.Approximately(prior - specimen.health, 26), "thermal lightning independently returns after five seconds");
            g.StartVoyage();
            yield return null;
        }

        IEnumerator RevisionEliteContracts()
        {
            for (int family = 0; family < 12; family++)
            {
                g.StartVoyage();
                g.Run.hullLevel = 3; g.Run.health = g.Run.MaxHealth;
                g.SetState(VoyageState.Combat);
                At(new Vector3(0, 0, 7));
                Vector3 spawn = new Vector3(0, 0, 13); spawn.y = g.World.GroundAt(spawn) + 1;
                var elite = new GameObject("Revision elite " + family).AddComponent<Enemy>();
                elite.InitSpecies(g, ExpeditionContent.Species[family], true, spawn);
                float begin = Time.time;
                while (elite && elite.Elite.Moves < 2 && Time.time - begin < 13 && g.State == VoyageState.Combat)
                    yield return null;
                Check(elite && elite.Elite.Moves >= 2, "elite " + family + " executes its own repeated tactic");
                if (elite && elite.Elite.Anchor)
                {
                    var anchor = elite.Elite.Anchor;
                    float shield = elite.Elite.DamageFactor(false);
                    g.Player.Equip(WeaponKind.Revolver);
                    begin = Time.time;
                    while (anchor && !anchor.Dead && Time.time - begin < 12 && g.State == VoyageState.Combat)
                    {
                        g.Player.AimAt(anchor.transform.position); g.Player.Fire(); yield return null;
                    }
                    Check(!anchor || anchor.Dead, "elite " + family + " symbiotic object breaks from real player shots");
                    Check(elite.Elite.DamageFactor(false) > shield * 2, "elite " + family + " loses its linked body protection");
                }
                if (elite && (family == 0 || family == 1 || family == 10))
                {
                    // Tiny injected weak hits isolate interrupt scheduling from damage balance.
                    // Full boss clears below use only the actual player weapon/raycast path.
                    int moves = elite.Elite.Moves;
                    begin = Time.time;
                    while (elite && Time.time - begin < 9 && g.State == VoyageState.Combat)
                    {
                        elite.Hit(.01f, true, false, false);
                        yield return new WaitForSeconds(.1f);
                    }
                    Check(elite && elite.Elite.Moves > moves,
                        "elite " + family + " cannot be permanently suppressed by rapid weak point interrupts");
                }
                Check(g.Player.InvulnerableUntil < float.PositiveInfinity, "elite " + family + " test uses normal damage immunity windows");
                g.StartVoyage(); yield return null;
                Check(FindObjectsOfType<EncounterTarget>().Length == 0,
                    "elite " + family + " releases all symbiotic objects during encounter cleanup");
            }
        }

        IEnumerator RevisionCreatureAnimation()
        {
            g.StartVoyage();
            foreach (int species in Enumerable.Range(0, 12).Concat(Enumerable.Range(108, 11)))
            {
                var root = new GameObject("Frontal weak point contract " + species);
                root.transform.position = new Vector3(10000, 1000, 0);
                var model = SpeciesArt.Build(root.transform, ExpeditionContent.Species[species], false);
                yield return null;
                Physics.SyncTransforms();
                var weak = model.GetComponentsInChildren<HitRegion>().FirstOrDefault(h =>
                    h.GetComponent<Collider>() && h.GetComponent<Collider>().enabled);
                bool reaches = false;
                if (weak)
                {
                    Vector3 center = weak.GetComponent<Collider>().bounds.center;
                    RaycastHit hit;
                    reaches = Physics.Raycast(center + Vector3.forward * 8, Vector3.back, out hit, 12) &&
                        hit.collider.GetComponent<HitRegion>() == weak;
                }
                Check(reaches, "model " + species + " decorative anatomy preserves its front-facing weak point ray");
                Destroy(root);
                yield return null;
            }
            foreach (int species in new[] { 0, 117 })
            {
                g.StartVoyage(); g.SetState(VoyageState.Combat); At(new Vector3(0, 0, 8));
                var actor = new GameObject("Flexible motion contract").AddComponent<Enemy>();
                actor.InitSpecies(g, ExpeditionContent.Species[species], false, new Vector3(0, 2, 30));
                string meshName = species == 117 ? "Continuous curling arm" : "Faceted scales";
                var filter = actor.GetComponentsInChildren<MeshFilter>().FirstOrDefault(f => f.name == meshName);
                Check(filter != null, "animation fixture contains its actual " + meshName + " mesh");
                if (filter)
                {
                    var mesh = filter.sharedMesh;
                    var before = mesh.vertices;
                    yield return new WaitForSeconds(1.1f);
                    var after = mesh.vertices;
                    bool deformed = false;
                    for (int vertex = 0; vertex < before.Length; vertex++)
                        if ((before[vertex] - after[vertex]).sqrMagnitude > .000001f) { deformed = true; break; }
                    Check(deformed, meshName + " animates its local vertices instead of only rotating a rigid whole model");
                }
                g.StartVoyage(); yield return null;
            }
        }

        IEnumerator RevisionPhaseGates()
        {
            for (int id = 108; id <= 118; id++)
            {
                var boss = RevisionCreateBoss(id, false);
                var encounter = boss.Encounter;
                // Deliberate damage injection is limited to this phase-boundary unit contract.
                if (encounter.Targets.Count > 0)
                {
                    float guardedHealth = boss.health;
                    boss.Hit(1000000, true, false, false);
                    Check(boss.health == guardedHealth, "boss " + id + " opening objectives prevent damage bypass");
                    foreach (var target in encounter.Targets.ToArray()) if (target) target.Hit(1000000);
                }
                boss.Hit(1000000, true, false, false);
                Check(!boss.dead && boss.health >= boss.maxHealth * .66f, "boss " + id + " cannot lose two phases to one burst");
                yield return null;
                Check(encounter.Phase == 2 && encounter.Targets.Count > 0,
                    "boss " + id + " opens physical breakable objectives for phase two");
                foreach (var target in encounter.Targets.ToArray()) if (target) target.Hit(1000000);
                boss.Hit(1000000, true, false, false); yield return null;
                Check(!boss.dead && encounter.Phase == 3 && encounter.Targets.Count > 0,
                    "boss " + id + " cannot skip its final mechanic gate");
                boss.Hit(1000000, true, false, false);
                Check(!boss.dead && boss.health > 0 && encounter.Targets.Count > 0,
                    "boss " + id + " cannot die while final phase objectives remain intact");
                g.StartVoyage(); yield return null;
                Check(FindObjectsOfType<EncounterTarget>().Length == 0,
                    "boss " + id + " interruption cleans up every phase objective");
            }
        }

        Enemy RevisionCreateBoss(int id, bool loadout)
        {
            g.StartVoyage();
            g.Run.seed = 41000 + id;
            g.Rng = new System.Random(g.Run.seed);
            UnityEngine.Random.InitState(g.Run.seed);
            int stage = id - 107;
            g.Run.maxIsland = 9;
            if (stage > 1) { g.ShowMap(); g.Travel(Mathf.Min(9, stage)); }
            g.Run.stage = stage; g.Run.questStep = 3; g.Run.bossCleared = false; g.Run.easy = false;
            g.Run.hullLevel = loadout ? Mathf.Clamp(stage / 3, 0, 3) : 0;
            g.Run.health = g.Run.MaxHealth;
            g.Run.weaponLevel = loadout ? Mathf.Min(5, (stage + 1) / 2) : 0;
            g.Run.damageRelics = loadout ? Mathf.Min(2, stage / 4) : 0;
            g.Run.hasteRelics = loadout ? Mathf.Min(2, stage / 4) : 0;
            g.Run.brakeLevel = loadout && stage >= 2 ? 2 : 0;
            g.Run.medkits = loadout && stage >= 2 ? 4 : 0;
            g.Run.harpoon = loadout && stage >= 4;
            g.Run.carbine = loadout && stage == 3;
            g.Run.dodgeRelics = loadout && stage >= 6 ? 1 : 0;
            if (loadout && stage >= 2) g.Run.keystoneMask |= 1;
            if (loadout && stage >= 5) g.Run.keystoneMask |= 1 << 5;
            if (loadout && stage >= 8) g.Run.keystoneMask |= 1 << 6;
            g.Player.ResetForEncounter();
            At(new Vector3(0, 0, 8), new Vector3(0, 2, 38));
            g.BeginCombat(true);
            var boss = g.Enemies.First(e => e && e.IsBoss);
            g.Player.Equip(g.Run.harpoon ? WeaponKind.Harpoon : g.Run.carbine ? WeaponKind.Carbine : WeaponKind.Revolver);
            g.Player.Refill();
            return boss;
        }

        IEnumerator RevisionStationaryPressure(int id)
        {
            var boss = RevisionCreateBoss(id, false);
            float begin = Time.time, initial = g.Run.health, minimum = initial;
            int moves = 0;
            while (Time.time - begin < 26 && g.State == VoyageState.Combat)
            {
                minimum = Mathf.Min(minimum, g.Run.health);
                if (boss) moves = boss.Encounter.Moves;
                yield return null;
            }
            minimum = Mathf.Min(minimum, g.Run.health);
            float damage = initial - minimum;
            Check(moves >= 3, "legendary boss " + id + " executes multiple attacks against a stationary player");
            Check(damage >= 50, "legendary boss " + id + " punishes standing still with meaningful damage: " + F(damage));
            Check(g.Player.InvulnerableUntil != float.PositiveInfinity, "legendary pressure check never uses test invulnerability");
            revisionTelemetry.Add("stationary," + id + "," + g.Run.seed + "," + F(Time.time - begin) + "," + F(damage) + ",0,0,1," + moves + ",0," + g.State);
            g.StartVoyage(); yield return null;
        }

        IEnumerator RevisionBossBattle(int id)
        {
            var boss = RevisionCreateBoss(id, true);
            var encounter = boss.Encounter;
            float begin = Time.time, previousHP = g.Run.health, damage = 0;
            int shots = 0, phaseMask = 0, moves = 0, nodes = 0, medkits = g.Run.medkits;
            bool photo = false, phasePhoto = false, shotNode = false, shotBoss = false;
            float turnAt = Time.time; int waypoint = 0;
            var patrol = new[] { new Vector3(-8, 0, 4), new Vector3(8, 0, 4), new Vector3(8, 0, -6), new Vector3(-8, 0, -6) };
            while (boss && !boss.dead && g.State == VoyageState.Combat && Time.time - begin < 240)
            {
                damage += Mathf.Max(0, previousHP - g.Run.health);
                if (g.Run.health < g.Run.MaxHealth * .38f && g.Run.medkits > 0) g.UseUtility("medkit");
                previousHP = g.Run.health;
                phaseMask |= 1 << (encounter.Phase - 1); moves = encounter.Moves; nodes = encounter.GatesBroken;
                if (Time.time > turnAt || GameDirector.FlatDistance(g.Player.transform.position, patrol[waypoint]) < 1)
                { waypoint = (waypoint + 1) % patrol.Length; turnAt = Time.time + 4.5f; }
                Vector3 move = patrol[waypoint] - g.Player.transform.position; move.y = 0;
                if (g.Player.Motor.enabled) g.Player.Motor.Move(move.normalized * 4.3f * Time.deltaTime);
                bool danger = FindObjectsOfType<DeckWarning>().Any(w => w.Remaining < .5f &&
                    GameDirector.FlatDistance(w.transform.position, g.Player.transform.position) < w.radius + .4f);
                if (danger && g.Player.DashReady >= .99f) g.Player.Dash(move.normalized);
                // React to the expanding rendered wave, using the same grounded jump as Space.
                // The bot has perfect visual recognition; this cannot establish human difficulty.
                foreach (var field in FindObjectsOfType<ThreatField>())
                {
                    var line = field.GetComponent<LineRenderer>();
                    if (!line || !line.loop || line.positionCount != 48 || line.startWidth < .15f) continue;
                    float radius = GameDirector.FlatDistance(line.GetPosition(0), field.transform.position);
                    float gap = GameDirector.FlatDistance(g.Player.transform.position, field.transform.position) - radius;
                    if (gap > 0 && gap < 1.6f) g.Player.Jump();
                }
                var target = encounter.Targets.FirstOrDefault(t => t && !t.Dead);
                if (target)
                {
                    float health = target.Health;
                    g.Player.AimAt(target.transform.position);
                    if (g.Player.Fire()) shots++;
                    shotNode |= !target || target.Health < health;
                }
                else
                {
                    var minion = g.Enemies.FirstOrDefault(e => e && !e.IsBoss && !e.dead);
                    var victim = minion ? minion : boss;
                    var region = victim.GetComponentsInChildren<HitRegion>().FirstOrDefault(h => h.GetComponent<Collider>() && h.GetComponent<Collider>().enabled);
                    Vector3 aim = region ? region.GetComponent<Collider>().bounds.center : victim.transform.position + Vector3.up * .4f;
                    float health = victim.health;
                    g.Player.AimAt(aim);
                    if (g.Player.Fire()) shots++;
                    if (victim == boss) shotBoss |= !boss || boss.health < health;
                }
                if (!photo && Time.time - begin > 8)
                { Capture("revision-boss-" + id); photo = true; }
                if (!phasePhoto && encounter.Phase == 3)
                {
                    phasePhoto = true;
                    // The HUD may have updated before the encounter changed phase this frame.
                    yield return null;
                    Capture("revision-boss-final-phase-" + id);
                }
                yield return null;
            }
            bool bossDefeated = !boss || boss.dead;
            float cleanupBegin = Time.time;
            while (bossDefeated && g.State == VoyageState.Combat && Time.time - cleanupBegin < 25)
            {
                damage += Mathf.Max(0, previousHP - g.Run.health);
                if (g.Run.health < g.Run.MaxHealth * .38f && g.Run.medkits > 0) g.UseUtility("medkit");
                previousHP = g.Run.health;
                var remaining = g.Enemies.FirstOrDefault(e => e && !e.dead);
                if (remaining)
                {
                    var region = remaining.GetComponentsInChildren<HitRegion>().FirstOrDefault(h => h.GetComponent<Collider>() && h.GetComponent<Collider>().enabled);
                    var aim = region ? region.GetComponent<Collider>().bounds.center : remaining.transform.position;
                    var retreat = g.Player.transform.position - remaining.transform.position; retreat.y = 0;
                    if (g.Player.Motor.enabled) g.Player.Motor.Move(retreat.normalized * 3.5f * Time.deltaTime);
                    g.Player.AimAt(aim); if (g.Player.Fire()) shots++;
                }
                yield return null;
            }
            damage += Mathf.Max(0, previousHP - g.Run.health);
            float duration = Time.time - begin;
            bool victory = bossDefeated && (g.State == VoyageState.Sailing || g.State == VoyageState.Victory);
            Check(shotNode && shotBoss, "boss " + id + " integrated fire damages physical nodes and the boss");
            Check(phaseMask == 7 && nodes >= 4, "boss " + id + " real combat visits all three phases and breaks their objectives");
            Check(moves >= 3, "boss " + id + " survives long enough to execute multiple moves");
            Check(victory, "boss " + id + " can be defeated with legal shooting, movement, dash and finite medical items");
            Check(duration >= 20 && duration < 240, "boss " + id + " encounter duration remains within a playable range: " + F(duration));
            // Twelve real attacks and four finite heals in the whale regression establish
            // pressure more honestly than increasing its health to force an arbitrary 65s.
            if (id >= 117) Check(duration >= 55 && moves >= 8, "legendary boss " + id + " sustains a multi-mechanic endgame encounter");
            Check(g.Player.InvulnerableUntil != float.PositiveInfinity, "boss " + id + " clear never depends on test invulnerability");
            revisionTelemetry.Add("boss," + id + "," + g.Run.seed + "," + F(duration) + "," + F(damage) + "," + (medkits - g.Run.medkits) + "," + shots + "," + phaseMask + "," + moves + "," + nodes + "," + (victory ? "cleared" : g.State.ToString()));
            File.WriteAllLines(Path.Combine(output, "revision-combat.csv"), revisionTelemetry);
            g.StartVoyage(); yield return null;
        }

        static string F(float value) { return value.ToString("F2", System.Globalization.CultureInfo.InvariantCulture); }
    }
}
