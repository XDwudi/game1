using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Tidebreak
{
    public partial class SmokePilot
    {
        // Independently callable by the presentation pass. This isolates chapter
        // bookkeeping from boss mechanics, which the combat suite already drives.
        IEnumerator PostgameRevisionContracts()
        {
            g.StartVoyage();g.Run.maxIsland=9;g.ShowMap();g.Travel(9);yield return null;
            g.Run.questStep=3;g.Run.bossCleared=true;g.Run.bossTrophy=true;
            g.Run.exploredMask=7;g.Run.eventMask=3|(3<<10);g.Run.landed=6;g.Run.shopMask=5;
            g.Run.islandCaught=new List<int>{96,101};g.Run.storyChoice=1;g.Run.rareSignal=true;g.Run.coins=1000;
            g.Checkpoint(false);int victories=g.Log.victories;Guide();int beforeHandIn=g.Run.coins;
            Check(g.AdvanceStory()&&g.State==VoyageState.Victory&&g.Log.victories==victories+1,
                "main-story hand-in records its victory and reaches the ending exactly once");
            Check(g.Run.coins==beforeHandIn+117&&!g.Run.bossTrophy,
                "main-story hand-in pays once and consumes the outstanding trophy");
            var ninth=JsonUtility.FromJson<IslandProgress>(JsonUtility.ToJson(g.Run.islands[8]));
            string[] workshop=g.Choices.Select(x=>x.name).ToArray();
            g.ExploreAfterEnding();Check(PostgameNinthMatches(ninth,5),"main ending returns to its exact completed ninth-island snapshot");

            // Deliberately depart from an early island whose fields all differ.
            g.ShowMap();g.Travel(1);g.Run.questStep=4;g.Run.bossCleared=true;g.Run.bossTrophy=false;
            g.Run.exploredMask=0;g.Run.eventMask=0;g.Run.landed=2;g.Run.shopMask=0;g.Run.islandCaught=new List<int>{0};g.Checkpoint(false);
            g.ShowMap();g.ChallengeKraken();yield return null;
            Check(g.Run.stage==10&&g.Run.shopMask==5&&g.Choices.Select(x=>x.name).SequenceEqual(workshop),
                "legendary departure from island one loads the ninth workshop's stock and purchase mask");
            At(g.SitePoint(2));int beforeCache=g.Run.coins;g.UseSite(2);
            Check(g.State==VoyageState.Sailing&&g.Run.coins==beforeCache,
                "legendary waters cannot reopen a copied story cache for duplicate rewards");
            At(g.World.QuestPoint);g.TalkGuide();Check(g.State==VoyageState.Sailing,
                "legendary waters do not open a misleading ninth-island story hand-in");

            Shop();int relicPrice=g.Price("relic1"),wallet=g.Run.coins;
            Check(relicPrice>0&&g.Buy("relic1")&&g.Run.coins==wallet-relicPrice&&g.Run.shopMask==7,
                "legendary workshop charges the real price and consumes the shared remaining stock slot");
            var saved=SaveStore.Read<RunData>("voyage");
            Check(saved.stage==10&&saved.islands[8].shopMask==7&&saved.islands[8].eventMask==ninth.eventMask&&saved.islands[8].explored==ninth.explored,
                "legendary purchases persist only the shared stock mask without overwriting ninth-island investigation");
            g.ReturnHarbor();g.StartVoyage(true);yield return null;
            Check(g.Run.stage==10&&!g.Run.bossCleared&&g.Run.shopMask==7&&g.Choices.Select(x=>x.name).SequenceEqual(workshop),
                "unfinished legendary voyage resumes with the same workshop and bought slots");
            Shop();Check(g.Price("relic1")==-1,"a legendary stock purchase cannot be bought again after reload");g.CloseShop();

            // Simulate the legitimate victory state, with an outstanding rare trophy.
            g.Run.bossCleared=true;g.Run.bossTrophy=true;g.Run.krakenDefeated=true;
            g.SetState(VoyageState.Combat);g.EndVoyage(true);int walletAfterRare=g.Run.coins;
            g.ExploreAfterEnding();yield return null;
            Check(PostgameNinthMatches(ninth,7)&&g.Run.storyChoice==1&&g.Run.krakenDefeated,
                "rare victory returns all ninth-island story fields while retaining the global legendary achievement");
            Guide();Check(!g.AdvanceStory()&&g.Run.coins==walletAfterRare,
                "returning with a rare trophy cannot pay the main-story hand-in twice");
            At(g.SitePoint(2));g.UseSite(2);Check(g.State==VoyageState.Sailing&&g.Run.coins==walletAfterRare,
                "ninth-island secret completion survives a rare victory and cannot pay twice");
            Shop();Check(g.Price("relic1")==-1&&g.Run.shopMask==7,
                "ninth-island workshop respects the stock slot bought in legendary waters");g.CloseShop();

            // Leaving from the victory screen uses the saved rare state, not Explore.
            g.ShowMap();g.ChallengeWhiteWhale();g.Run.bossCleared=true;g.Run.bossTrophy=true;g.Run.whaleDefeated=true;
            g.SetState(VoyageState.Combat);g.EndVoyage(true);g.ReturnHarbor();g.StartVoyage(true);yield return null;
            Check(PostgameNinthMatches(ninth,7)&&g.Run.whaleDefeated&&g.State==VoyageState.Sailing,
                "resuming a completed legendary save returns to the restored ninth island instead of a disabled hunt bell");
            Check(g.Log.victories==victories+1,"legendary returns and reloads do not increment main-story victory count");

            // An interrupted final film must remain pending until its callback runs.
            g.Run.endingPending=true;g.Run.cinematicMask&=~(1<<12);g.Checkpoint(false);g.ReturnHarbor();
            int completedBefore=g.Log.victories;g.StartVoyage(true);yield return null;
            Check(g.CinematicActive&&g.CinematicSequence==12&&g.Run.endingPending,
                "a checkpoint interrupted during the ending resumes the pending final film");
            g.ReturnHarbor();g.StartVoyage(true);yield return null;
            Check(g.CinematicActive&&g.Run.endingPending&&g.Log.victories==completedBefore,
                "cancelling the resumed ending preserves its pending status without recording an early victory");
            g.SkipCinematic();yield return null;
            Check(g.State==VoyageState.Victory&&!g.Run.endingPending&&g.Log.victories==completedBefore+1,
                "finishing or skipping the pending ending records victory once and clears the pending flag");
            g.ReturnHarbor();g.StartVoyage(true);yield return null;
            Check(!g.CinematicActive&&!g.Run.endingPending&&g.Log.victories==completedBefore+1,
                "subsequent reload does not replay or recount an already settled ending");
            Time.timeScale=1;
        }

        bool PostgameNinthMatches(IslandProgress expected,int mask)
        {
            var r=g.Run;return r.stage==9&&r.questStep==expected.step&&r.landed==expected.landed&&r.exploredMask==expected.explored&&
                r.eventMask==expected.eventMask&&r.bossCleared==expected.boss&&r.bossTrophy==expected.trophy&&r.shopMask==mask&&
                r.islandCaught.SequenceEqual(expected.caught)&&r.islands[8].shopMask==mask;
        }
    }
}
