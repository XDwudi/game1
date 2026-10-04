using System.Linq;
using TMPro;
using UnityEngine;

namespace Tidebreak
{
    public partial class SeaHUD
    {
        int masteryIsland=-1;
        public void ShowIslandMastery(){masteryIsland=Mathf.Clamp(game.Run.stage-1,0,8);shopTab=5;BuildShopPage();}
        void BuildMasteryStock(RectTransform p)
        {
            var run=game.Run;if(masteryIsland<0)masteryIsland=Mathf.Clamp(run.stage-1,0,8);
            for(int i=0;i<9;i++){
                int index=i,level=IslandMastery.Level(run,i);float x=80+i%3*162,y=329+i/3*58;
                string name=ExpeditionContent.Islands[i].name;
                Button(p,x,y,149,46,(i+1).ToString("00")+" "+name,()=>{masteryIsland=index;BuildShopPage();},masteryIsland==i);
                if(level>0)Box(p,x+4,y+43,level*45,3,Gold);
            }
            var island=ExpeditionContent.Islands[masteryIsland];var offer=ShopCatalog.All.FirstOrDefault(x=>x.id=="mastery"+masteryIsland);
            IslandIllustration(p,93,530,459,243,masteryIsland+1,1);
            Text(p,80,496,470,29,"专研来自这片海域独有的生命。",17,Muted);
            Box(p,588,329,932,437,Panel);Box(p,588,329,3,437,island.accent);
            Text(p,612,350,670,41,IslandMastery.Names[masteryIsland],30,Cream);
            int current=IslandMastery.Level(run,masteryIsland);
            Text(p,1300,355,194,34,current+" / 3",23,Gold,TextAlignmentOptions.Right);
            Text(p,612,404,878,83,IslandMastery.Description(run,masteryIsland),21,Cream);
            string requirement=IslandMastery.UnlockHint(run,masteryIsland);
            Text(p,612,504,878,49,requirement,17,current>=3?Mint:Muted);
            int[] specimens=IslandMastery.RequiredSpecimens(masteryIsland);
            for(int i=0;i<specimens.Length;i++){
                int id=specimens[i];bool known=IslandMastery.IsSpecimenKnown(run,id);var species=ExpeditionContent.Species[id];float x=612+i*447;
                Box(p,x,573,431,91,new Color(Ink.r,Ink.g,Ink.b,.75f));Box(p,x,573,3,91,known?Mint:Gold);
                Text(p,x+16,586,397,29,species.name,20,known?Mint:Cream);
                Text(p,x+16,622,397,28,known?"标本已研究 · 蓝图永久记录本航次":"本岛专属 · "+ExpeditionContent.Lures[species.lure]+" · 钓获后击败",14,Muted);
            }
            if(offer!=null){
                int price=game.Price(offer.id);string locked=ShopCatalog.Lock(run,offer);bool can=locked==""&&price>=0&&run.coins>=price;
                Text(p,612,699,480,43,"蓝图解锁之后，用金币选择升级。",17,Muted);
                string label=locked!=""?locked:price<0?"已完成全部专研":price+" 金币 · "+(can?"升级":"金币不足");
                Button(p,1114,698,382,48,label,()=>game.Buy(offer.id),can,can);
            }
        }
    }
}
