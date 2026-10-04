using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Tidebreak
{
    public partial class SmokePilot
    {
        bool campaignOk;float campaignStart,campaignDistance;int campaignShots;
        readonly List<string> campaignTrace=new List<string>();
        void CampaignEvent(string name)
        {
            campaignTrace.Add(F(Time.time-campaignStart)+","+g.Run.seed+","+g.Run.stage+","+name+","+g.Run.coins+","+F(g.Run.health)+","+g.Run.questStep+","+F(campaignDistance)+","+campaignShots);
            File.WriteAllLines(Path.Combine(output,"campaign.csv"),campaignTrace);
        }
        void CampaignRequire(bool ok,string text)
        {
            Check(ok,"campaign: "+text);if(ok)return;campaignOk=false;CampaignEvent("FAILED");Capture("campaign-failure-"+results.Count);
        }
        Button CampaignButton(string words)
        {
            return FindObjectsOfType<Button>().FirstOrDefault(b=>b.isActiveAndEnabled&&b.interactable&&b.GetComponentsInChildren<TMP_Text>().Any(t=>t.text.Contains(words)));
        }
        bool CampaignClick(string words)
        {
            var button=CampaignButton(words);if(!button)return false;button.onClick.Invoke();return true;
        }
        IEnumerator CampaignWalk(Vector3 goal,string label,float distance=2.5f,bool viaHub=true)
        {
            Vector3[] route=g.World.NavigationRoute(g.Player.transform.position,goal);
            foreach(var point in route){
                float start=Time.time;float radius=GameDirector.FlatDistance(point,goal)<.01f?distance:.65f;
                while(g.IsPlaying&&g.Run.health>0&&GameDirector.FlatDistance(g.Player.transform.position,point)>radius&&Time.time-start<65){
                    Vector3 before=g.Player.transform.position;V05Move(point,false);campaignDistance+=GameDirector.FlatDistance(before,g.Player.transform.position);yield return null;
                }
                if(GameDirector.FlatDistance(g.Player.transform.position,point)>radius+.15f){CampaignRequire(false,"walkable player route reaches "+label+" without teleporting");yield break;}
            }
            CampaignEvent("walk_"+label);yield return null;
        }
        IEnumerator CampaignDialogue(string finalButton)
        {
            int pages=0;
            while(g.State==VoyageState.Dialogue&&pages++<12){
                yield return null;
                if(CampaignClick("听下去")){yield return null;continue;}
                CampaignRequire(CampaignClick(finalButton),"segmented dialogue exposes its legitimate final "+finalButton+" button");yield return null;break;
            }
            CampaignRequire(g.State==VoyageState.Sailing&&pages>=2,"reading dialogue pages then clicking confirmation returns to the island");
        }
        IEnumerator CampaignPurchase(string id)
        {
            var offer=ShopCatalog.Find(id);string[] tabs={"枪械与配件","钓具与拟饵","防护与补给"};
            CampaignRequire(g.State==VoyageState.Shop&&offer!=null,"physical workshop is open before purchase "+id);if(!campaignOk)yield break;
            CampaignRequire(CampaignClick(tabs[offer.category]),"workshop category button opens "+tabs[offer.category]);yield return null;if(!campaignOk)yield break;
            var title=FindObjectsOfType<TMP_Text>().FirstOrDefault(t=>t.isActiveAndEnabled&&t.text.StartsWith(offer.name));
            Button purchase=null;
            if(title){Vector2 at=title.rectTransform.anchoredPosition;purchase=FindObjectsOfType<Button>().Where(b=>b.isActiveAndEnabled&&b.interactable&&b.GetComponentsInChildren<TMP_Text>().Any(t=>t.text.Contains("金币")&&t.text.Contains("购买")))
                    .Where(b=>Mathf.Abs(((RectTransform)b.transform).anchoredPosition.x-at.x)<4&&((RectTransform)b.transform).anchoredPosition.y<at.y)
                    .OrderByDescending(b=>((RectTransform)b.transform).anchoredPosition.y).FirstOrDefault();}
            int price=g.Price(id),wallet=g.Run.coins,oldLevel=offer.level(g.Run);float healthBefore=g.Run.health;
            CampaignRequire(purchase&&price>=0&&wallet>=price,"visible offer "+id+" is affordable using this voyage's earnings");if(!campaignOk)yield break;
            purchase.onClick.Invoke();yield return null;
            CampaignRequire(g.Run.coins==wallet-price&&(id=="heal"?g.Run.health>healthBefore:offer.level(g.Run)>oldLevel),"clicking "+offer.name+" charges exactly "+price+" earned coins and applies the item");
            CampaignEvent("buy_"+id);
        }
        IEnumerator CampaignFight(Vector3 center,bool bossFight=false)
        {
            float start=Time.time;int waypoint=0,phaseMask=0,solvedMask=0;Enemy boss=null;
            Vector3[] route={center+new Vector3(-2,0,1),center+new Vector3(2,0,1),center+new Vector3(2,0,-1),center+new Vector3(-2,0,-1)};
            while(g.State==VoyageState.Combat&&Time.time-start<(bossFight?240:120)){
                boss=g.Enemies.FirstOrDefault(e=>e&&!e.dead&&e.IsBoss);
                if(boss){var m=boss.Encounter.Mechanism;phaseMask|=1<<(m.Phase-1);if(m.Active)V05Operate(m);if(!m.Active){solvedMask|=1<<(m.Phase-1);if(GameDirector.FlatDistance(g.Player.transform.position,route[waypoint])<.8f)waypoint=(waypoint+1)%route.Length;V05Move(route[waypoint]);}}
                else {if(GameDirector.FlatDistance(g.Player.transform.position,route[waypoint])<.8f)waypoint=(waypoint+1)%route.Length;V05Move(route[waypoint]);}
                var victim=g.Enemies.FirstOrDefault(e=>e&&!e.dead&&!e.IsBoss);if(!victim)victim=boss;
                if(victim){g.Player.SetRod(false);var device=V06PriorityTarget(victim);g.Player.AimAt(device?V06DeviceAim(device):V05Aim(victim));if(g.Player.Fire())campaignShots++;if(g.Player.QuickReloadAvailable&&g.Player.ReloadProgress>=.59f&&g.Player.ReloadProgress<=.67f)g.Player.Reload();}
                yield return null;
            }
            CampaignRequire(g.State==VoyageState.Sailing&&g.Run.health>0,"real "+(bossFight?"giant crab encounter":"live catch / lighthouse combat")+" is won with finite health and real shots");
            if(bossFight)CampaignRequire(phaseMask==7&&solvedMask==7&&g.Run.bossTrophy,"actual giant crab fight solves all three mechanisms and awards its trophy");
            CampaignEvent(bossFight?"boss_cleared":"fight_cleared");
        }
        IEnumerator CampaignCatchAndSell()
        {
            yield return CampaignWalk(new Vector3(0,0,23),"fishing_pier",.7f);if(!campaignOk)yield break;
            g.Player.AimAt(new Vector3(0,0,50));g.Player.SetRod(true);g.CastCharge=.55f;g.Cast();
            CampaignRequire(g.State==VoyageState.Fishing,"walking to the pier permits a genuine water cast");if(!campaignOk)yield break;
            float start=Time.time;while(g.State==VoyageState.Fishing&&Time.time-start<42){g.TickFishing(!g.Surge,Time.deltaTime);yield return null;}
            CampaignRequire(g.State==VoyageState.Combat,"legitimate tension control hooks an actual local fish");if(!campaignOk)yield break;
            yield return CampaignFight(new Vector3(0,0,17));if(!campaignOk)yield break;
            CampaignRequire(g.Loot.Count>0,"defeated catch exists physically before appraisal");if(!campaignOk)yield break;
            var loot=g.Loot.Last();start=Time.time;
            while(loot&&g.World.GroundAt(loot.transform.position)<-.3f&&Time.time-start<7)yield return null;
            start=Time.time;while(loot&&!g.Player.HeldFish&&Time.time-start<30){Vector3 before=g.Player.transform.position;V05Move(loot.transform.position,false);campaignDistance+=GameDirector.FlatDistance(before,g.Player.transform.position);g.Interact();yield return null;}
            CampaignRequire(g.Player.HeldFish==loot&&loot.Registered,"walking to the fish and pressing the shared E action picks it up");if(!campaignOk)yield break;
            int appraisal=loot.Data.value;g.Player.StowHeld();yield return CampaignWalk(g.World.SellPoint,"fish_market",3);if(!campaignOk)yield break;
            int wallet=g.Run.coins,bag=g.Run.BagValue;g.Interact();yield return null;
            CampaignRequire(bag>=appraisal&&g.Run.coins==wallet+bag&&g.Run.bag.Count==0,"physical market interaction pays exact appraised value into the wallet");CampaignEvent("sale");
        }
        IEnumerator V05CampaignContracts()
        {
            // No At/Teleport, direct damage, money/gear/quest-state grants, health
            // assignment, or test immunity occurs anywhere in this campaign path.
            campaignOk=true;campaignDistance=0;campaignShots=0;campaignStart=Time.time;campaignTrace.Clear();campaignTrace.Add("seconds,seed,island,event,coins,health,quest_step,walked_metres,shots");
            g.ReturnHarbor();yield return null;CampaignRequire(CampaignClick("开始远征"),"harbor Start Voyage button creates a real new run");yield return null;Time.timeScale=v05Speed;v05DashUntil=0;
            if(!campaignOk)yield break;CampaignEvent("new_voyage");
            CampaignRequire(g.Run.stage==1&&g.Run.weaponLevel==0&&g.Run.maxIsland==1&&g.Run.health==g.Run.MaxHealth,"new voyage starts with its real starting economy and equipment");
            yield return CampaignWalk(g.World.QuestPoint,"first_guide",2.7f);if(!campaignOk)yield break;g.Interact();yield return null;
            CampaignRequire(g.State==VoyageState.Dialogue,"walking to the guide and shared E action opens the first story");if(!campaignOk)yield break;
            Capture("campaign-first-dialogue");yield return CampaignDialogue("答应帮忙");if(!campaignOk)yield break;
            CampaignRequire(g.Run.questStep==1,"accepting the final dialogue page requests a first real fish");
            int catches=0;while(g.Run.coins<110&&catches++<6&&campaignOk)yield return CampaignCatchAndSell();if(!campaignOk)yield break;
            CampaignRequire(g.Run.landed>0&&g.Run.coins>=g.Price("weapon"),"catch earnings can finance the first damage upgrade");if(!campaignOk)yield break;
            yield return CampaignWalk(g.World.ShopPoint,"first_workshop",3.2f);if(!campaignOk)yield break;g.Interact();yield return null;
            CampaignRequire(!g.Run.shotgun&&g.Price("shotgun")<0,"future-island shotgun is still locked at the first workshop");yield return CampaignPurchase("weapon");if(!campaignOk)yield break;
            if(g.Run.health<85&&g.Run.coins>=25)yield return CampaignPurchase("heal");if(!campaignOk)yield break;
            CampaignRequire(CampaignClick("返回岛屿"),"workshop return button returns to the physical island");yield return null;
            yield return CampaignWalk(g.SitePoint(0),"lighthouse_battery",2.4f);if(!campaignOk)yield break;g.Interact();yield return null;
            CampaignRequire((g.Run.eventMask&1)!=0,"real battery-site interaction records the carried power source");
            yield return CampaignWalk(g.SitePoint(1),"lighthouse_defense",2.4f);if(!campaignOk)yield break;g.Interact();yield return null;
            CampaignRequire(g.MissionActive&&g.State==VoyageState.Combat,"installing the battery through E begins the actual defense");if(!campaignOk)yield break;
            yield return CampaignFight(g.SitePoint(1));if(!campaignOk)yield break;
            CampaignRequire(g.Run.questStep==3&&g.Run.maxIsland==1,"surviving actual timed defense grants boss access without prematurely unlocking island two");Capture("campaign-lighthouse-restored");
            // All healing here is the real paid soup, at the physical workshop.
            if(g.Run.health<85&&g.Run.coins>=25){yield return CampaignWalk(g.World.ShopPoint,"pre_boss_workshop",3.2f);if(!campaignOk)yield break;g.Interact();yield return null;yield return CampaignPurchase("heal");if(!campaignOk)yield break;CampaignClick("返回岛屿");yield return null;}
            yield return CampaignWalk(new Vector3(0,0,23),"hunt_bell",.7f);if(!campaignOk)yield break;g.Interact();yield return null;
            CampaignRequire(g.Enemies.Any(e=>e&&e.Spec.id==108),"shared E action at the actual hunt bell summons the licensed giant crab");if(!campaignOk)yield break;
            yield return CampaignFight(new Vector3(0,0,4),true);if(!campaignOk)yield break;Capture("campaign-earned-crab-victory");
            int beforeHandIn=g.Run.coins;yield return CampaignWalk(g.World.QuestPoint,"trophy_hand_in",2.7f);if(!campaignOk)yield break;g.Interact();yield return null;
            yield return CampaignDialogue("交付潮核");if(!campaignOk)yield break;
            CampaignRequire(g.Run.questStep==4&&g.Run.maxIsland==2&&!g.Run.bossTrophy&&g.Run.coins==beforeHandIn+53,"real trophy hand-in pays once and opens only the next island");CampaignEvent("island_two_unlocked");
            yield return CampaignWalk(g.World.ChartPoint,"route_chart",2.4f);if(!campaignOk)yield break;g.Interact();yield return null;
            CampaignRequire(g.State==VoyageState.Route&&CampaignClick("前往此岛"),"physical chart offers the newly unlocked second-island travel button");yield return null;
            CampaignRequire(g.Run.stage==2&&g.World.Region==1&&g.Run.maxIsland==2,"legitimate travel reaches coral island without granting later licenses");if(!campaignOk)yield break;
            yield return CampaignWalk(g.World.ShopPoint,"coral_workshop",3.2f);if(!campaignOk)yield break;g.Interact();yield return null;
            CampaignRequire(g.Run.coins>=g.Price("shotgun")&&g.Price("shotgun")==150,"earned combat and quest rewards finance the newly licensed shotgun");if(!campaignOk)yield break;
            yield return CampaignPurchase("shotgun");if(!campaignOk)yield break;
            CampaignRequire(g.Run.shotgun&&!g.Run.carbine&&g.Price("carbine")<0,"first new weapon is paid for while the third-island weapon stays locked");
            int savedCoins=g.Run.coins;CampaignClick("返回岛屿");yield return null;g.TogglePause();yield return null;
            CampaignRequire(CampaignClick("保存进度并返回港口"),"pause-menu Save and Return button saves the real expedition");yield return null;
            CampaignRequire(CampaignClick("继续 ·"),"harbor Continue button resumes the saved expedition");yield return null;
            CampaignRequire(g.Run.stage==2&&g.Run.coins==savedCoins&&g.Run.shotgun&&g.Run.weaponLevel==1&&g.Run.islands[0]!=null&&g.Run.islands[0].step==4,"resumed real campaign preserves earned money, both purchases, island-two access and completed first-island story");
            CampaignRequire(g.Player.InvulnerableUntil!=float.PositiveInfinity&&campaignDistance>150&&campaignShots>0,"continuous campaign traversed actual roads and combat with no QA immunity");
            Capture("campaign-resumed-coral");CampaignEvent("completed");
        }
    }
}
