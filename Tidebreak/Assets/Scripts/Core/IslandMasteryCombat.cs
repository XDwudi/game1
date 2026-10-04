using UnityEngine;

namespace Tidebreak
{
    // Called exclusively through the player's normal shot/dash/reload pipeline.
    // Extra hits use effects=false and never recurse through OnHit synergies.
    public sealed class IslandMasteryCombat
    {
        readonly AnglerController player;
        readonly GameDirector game;
        float dashUntil,dashReady,refractionReady,windupReady,reloadUntil,frostReady,guardUntil,executionReady,switchReady,lastHitAt;
        WeaponKind lastWeapon;
        bool hasLastHit;
        public IslandMasteryCombat(AnglerController p){player=p;game=p.game;}
        int Rank(int island){return IslandMastery.Level(game.Run,island);}
        public void Reset()
        {
            dashUntil=dashReady=refractionReady=windupReady=reloadUntil=frostReady=guardUntil=executionReady=switchReady=lastHitAt=0;hasLastHit=false;
        }
        public string Status
        {
            get {
                if(Time.time<guardUntil)return "避雷蓄能 · 下次受伤减轻";
                if(Time.time<reloadUntil)return "沉钟快装 · 下一枪强化";
                if(Time.time<dashUntil&&Time.time>=dashReady)return "松风追潮 · 瞄准弱点";
                return "";
            }
        }
        public void Dash()
        {
            if(Rank(0)>0)dashUntil=Time.time+3;
            int frost=Rank(5);
            if(frost>0&&Time.time>=frostReady){
                bool affected=false;
                foreach(var enemy in game.Enemies)if(enemy&&!enemy.dead&&!enemy.IsBoss&&Vector3.Distance(enemy.transform.position,player.transform.position)<=5){enemy.Slow(.3f+frost*.25f);affected=true;}
                if(affected){frostReady=Time.time+6;game.Effect(player.transform.position+Vector3.up*.2f,new Color(.48f,.83f,.95f),12,.11f);}
            }
        }
        public void AvoidedAttack()
        {
            if(Rank(6)>0){guardUntil=Time.time+5;game.Audio.Cue("ready");}
        }
        public float IncomingDamageMultiplier()
        {
            if(Time.time>=guardUntil)return 1;
            guardUntil=0;return 1-(.1f+.1f*Rank(6));
        }
        public void ReloadCompleted(bool precise)
        {if(precise&&Rank(4)>0)reloadUntil=Time.time+5;}
        public float ShotMultiplier()
        {
            if(Time.time>=reloadUntil)return 1;
            reloadUntil=0;return 1+.02f+.08f*Rank(4);
        }
        public float TargetDamageMultiplier(Enemy enemy,bool weak)
        {
            if(!enemy||enemy.dead||!weak||enemy.Encounter&&enemy.Encounter.DamageFactor<=0)return 1;
            float bonus=0;
            if(Rank(0)>0&&Time.time<dashUntil&&Time.time>=dashReady){bonus+=.06f+.08f*Rank(0);dashReady=Time.time+5;dashUntil=0;}
            bool charging=enemy.AttackWindup>0||enemy.Elite&&enemy.Elite.Windup>0;
            if(Rank(3)>0&&charging&&Time.time>=windupReady){bonus+=.05f+.1f*Rank(3);windupReady=Time.time+3;}
            if(Rank(7)>0&&enemy.health<=enemy.maxHealth*.3f&&Time.time>=executionReady){bonus+=.1f+.1f*Rank(7);executionReady=Time.time+4;}
            if(Rank(8)>0&&hasLastHit&&player.weapon!=lastWeapon&&Time.time-lastHitAt<=4&&Time.time>=switchReady){bonus+=.04f+.06f*Rank(8);switchReady=Time.time+3;}
            // Several conditional researches can overlap without multiplying an
            // unlimited burst. Weapon/keystone bonuses keep their own contracts.
            return 1+Mathf.Min(.65f,bonus);
        }
        public void OnHit(Enemy target,bool weak,float damage)
        {
            lastWeapon=player.weapon;lastHitAt=Time.time;hasLastHit=true;
            int coral=Rank(1);
            if(!target||!weak||coral<=0||Time.time<refractionReady)return;
            Enemy nearest=null;float range=6;
            foreach(var enemy in game.Enemies)if(enemy&&!enemy.dead&&enemy!=target){float d=Vector3.Distance(enemy.transform.position,target.transform.position);if(d<range){range=d;nearest=enemy;}}
            if(!nearest)return;
            refractionReady=Time.time+2.5f;
            float amount=Mathf.Min(damage,Mathf.Max(0,target.LastDamageApplied))*(.1f+coral*.1f);
            if(amount<=0)return;
            game.Tracer(target.transform.position+Vector3.up*.6f,nearest.transform.position+Vector3.up*.6f,new Color(.95f,.43f,.45f));
            nearest.Hit(amount,false,false,false);
        }
    }
}
