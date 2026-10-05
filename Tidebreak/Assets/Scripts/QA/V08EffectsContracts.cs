using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Tidebreak
{
    public partial class SmokePilot
    {
        void ClearV08Effects()
        {
            foreach(var enemy in g.Enemies.ToArray())if(enemy)Destroy(enemy.gameObject);g.Enemies.Clear();
            for(int i=g.Hazards.childCount-1;i>=0;i--)Destroy(g.Hazards.GetChild(i).gameObject);
            EnemySkillFX.ClearFor(g);
        }
        IEnumerator V08EffectsContracts()
        {
            Screen.SetResolution(1600,900,false);yield return new WaitForSecondsRealtime(.25f);
            g.StartVoyage();yield return null;g.SetState(VoyageState.Combat);
            var fx=EnemySkillFX.For(g);
            File.WriteAllLines(Path.Combine(output,"skill-theme-coverage.csv"),new[]{"species_id,name,body,attack,trait,ecology,theme"}.Concat(ExpeditionContent.Species.Select(s=>s.id+","+s.name+","+s.body+","+s.attack+","+s.trait+","+s.ecology+","+SkillThemes.ForSpecies(s))).ToArray());
            Check(ExpeditionContent.Species.All(s=>Enum.IsDefined(typeof(SkillTheme),SkillThemes.ForSpecies(s))),"v08 all 119 species explicitly resolve a skill theme from identity traits and ecology");
            Check(ExpeditionContent.Species.Select(SkillThemes.ForSpecies).Distinct().Count()==11,"v08 real catalogue uses all eleven effect themes");
            Check(Enum.GetValues(typeof(SkillTheme)).Cast<SkillTheme>().Select(SkillThemes.Signature).Distinct().Count()==11,"v08 each theme declares a different geometry and particle signature");
            Check(fx.ResourceMeshes==29,"v08 nine shared authored forms plus twenty reused dynamic wave meshes");
            Vector3 center=new Vector3(0,0,15);center.y=g.World.GroundAt(center);
            foreach(SkillTheme theme in Enum.GetValues(typeof(SkillTheme))){
                ClearV08Effects();yield return null;g.Run.health=g.Run.MaxHealth;At(new Vector3(0,0,6),center+Vector3.up*1.5f);
                g.Notice("技能视觉验收 · "+theme+" / 预兆、释放与残留",6);
                g.Warn(center,2.6f,1.1f,4,theme:theme);
                var warning=FindObjectsOfType<DeckWarning>().Single();
                Check(warning.Theme==theme&&Mathf.Approximately(warning.radius,2.6f),"v08 "+theme+" strike keeps explicit theme and authoritative radius");
                yield return new WaitForSeconds(.55f);CapturePresentation("skill-"+theme+"-01-tell",1600,900);
                Check(warning&&warning.Remaining>.25f,"v08 "+theme+" visible premonition precedes damage");
                yield return new WaitForSeconds(.65f);CapturePresentation("skill-"+theme+"-02-release",1600,900);
                Check(!warning&&(fx.ActiveAccents+fx.ActivePaths+fx.ActiveWaves>0||fx.ParticleCount>0),"v08 "+theme+" real strike leaves a short themed release after hazard destruction");
                yield return new WaitForSeconds(1.35f);fx.Clear();
                ThreatField.Ring(g,center,3,SkillThemes.ColorOf(theme),theme:theme);
                ThreatField.Pool(g,center+Vector3.right*4,1.5f,.65f,2,SkillThemes.ColorOf(theme),theme:theme);
                yield return new WaitForSeconds(1.35f);CapturePresentation("skill-"+theme+"-03-wave-and-ground",1600,900);
                Check(fx.ActiveWaves>0,"v08 "+theme+" expanding ground hazard carries terrain-following three dimensional wavefront");
                g.Projectile(center+Vector3.up*1.6f+Vector3.left*3,center+Vector3.up*1.6f+Vector3.right*3,3,2,SkillThemes.ColorOf(theme),theme:theme);
                yield return new WaitForSeconds(.12f);
                Check(FindObjectsOfType<SeaProjectile>().Any(p=>p.Theme==theme),"v08 "+theme+" live projectile carries an explicit theme");
            }
            ClearV08Effects();yield return null;At(new Vector3(0,0,4),center);
            EnemySkillFX.Burst(g,SkillTheme.Frost,center+Vector3.up*6,1,113);yield return null;
            Check(fx.transform.Find("Bounded enemy skill effect pool").GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled).All(r=>r.bounds.center.y>center.y+5),"v08 airborne mechanism/contact bursts preserve their supplied world-space height");
            fx.Clear();
            ThreatField.Line(g,center-Vector3.right*3,center+Vector3.right*3,.8f,.1f,0,Color.yellow,theme:SkillTheme.Lightning);
            yield return new WaitForSeconds(.3f);
            Check(fx.ActivePaths==0,"v08 zero damage preview line does not emit an attacking lightning strike");
            ClearV08Effects();yield return null;
            g.Warn(center,2,1.5f,4,theme:SkillTheme.Cinder);yield return new WaitForSeconds(.4f);
            var pending=FindObjectsOfType<DeckWarning>().Single();g.TogglePause();yield return null;
            float remaining=pending.Remaining;int particles=fx.ParticleCount,accents=fx.ActiveAccents;
            yield return new WaitForSecondsRealtime(.4f);
            Check(Mathf.Approximately(pending.Remaining,remaining)&&particles==fx.ParticleCount&&accents==fx.ActiveAccents,"v08 pause freezes authoritative countdown and visual particle/mesh lifecycle");
            g.TogglePause();ClearV08Effects();yield return null;
            int meshCount=fx.ResourceMeshes,renderers=fx.transform.Find("Bounded enemy skill effect pool").GetComponentsInChildren<Renderer>(true).Length;
            int sounds=fx.PlayedReleaseSounds;
            for(int i=0;i<60;i++){
                var theme=(SkillTheme)(i%11);Vector3 p=center+new Vector3(i%6*2-5,0,i/6*1.1f);
                ThreatField.Pool(g,p,1.2f,.4f,1,SkillThemes.ColorOf(theme),theme:theme);
            }
            yield return new WaitForSeconds(.65f);
            Check(FindObjectsOfType<ThreatField>().Length==60,"v08 saturated visual pool never removes any of sixty authoritative hazards");
            Check(fx.PlayedReleaseSounds>sounds&&fx.PlayedReleaseSounds-sounds<=3,"v08 simultaneous skill releases share a short sound cooldown instead of stacking sixty impact sounds");
            Check(fx.ResourceMeshes==meshCount&&fx.transform.Find("Bounded enemy skill effect pool").GetComponentsInChildren<Renderer>(true).Length==renderers,"v08 saturated skill effects reuse the fixed mesh and renderer budget");
            Check(fx.ActiveAccents<=128&&fx.ActivePaths<=40&&fx.ActiveWaves<=20&&fx.ParticleCount<=1520,"v08 decorations stay inside their explicit rendering budgets");
            CapturePresentation("skill-saturation-real-hazards",1600,900);
            ClearV08Effects();yield return null;
            Check(fx.ActiveAccents==0&&fx.ActivePaths==0&&fx.ActiveWaves==0&&fx.ParticleCount==0,"v08 explicit hazard clear removes every mesh lightning wave and particle residue");
            // Execute all fourteen ordinary attack implementations, not just a VFX method.
            foreach(AttackStyle attack in Enum.GetValues(typeof(AttackStyle))){
                ClearV08Effects();yield return null;g.SetState(VoyageState.Combat);g.Run.health=g.Run.MaxHealth;At(new Vector3(0,0,8),new Vector3(0,2,15));
                var spec=ExpeditionContent.Species.First(s=>!s.boss&&s.attack==attack);
                var enemy=new GameObject("v08 real attack coverage "+attack).AddComponent<Enemy>();enemy.InitSpecies(g,spec,false,new Vector3(0,g.World.GroundAt(center)+1,15));
                int before=fx.Emitted;float until=Time.time+6;
                while(enemy&&enemy.AttackExecutions==0&&Time.time<until)yield return null;
                Check(enemy&&enemy.AttackExecutions>0&&fx.Emitted>before,"v08 live "+attack+" attack emits its element with ability-specific motion");
                if(attack==AttackStyle.Heal||attack==AttackStyle.Split||attack==AttackStyle.Burrow||attack==AttackStyle.Boomerang){yield return new WaitForSeconds(.1f);CapturePresentation("skill-real-attack-"+attack,1600,900);}
            }
            ClearV08Effects();yield return null;g.StartVoyage();yield return null;
            Check(fx.ActiveAccents==0&&fx.ActivePaths==0&&fx.ActiveWaves==0&&fx.ParticleCount==0,"v08 voyage reset clears every themed enemy effect");
            File.AppendAllText(Path.Combine(output,"scope.txt"),"Effect audit maps 119 species into eleven shared elemental/biome libraries, with fourteen ability motion patterns. It does not claim 119 independently authored particle systems. Theme screenshots use normal-time authored hazard fixtures on the real island; subsequent fixtures execute real ordinary enemy attack code. Budget stress is sixty simultaneous damage fields, deliberately heavier than a normal encounter.\n");
        }
    }
}
