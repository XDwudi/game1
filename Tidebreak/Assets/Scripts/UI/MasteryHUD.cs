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
            Plate(p,80,523,473,261,Paper,true);IslandIllustration(p,91,534,451,218,masteryIsland+1,1);
            Text(p,99,758,435,21,"FIELD NOTES  /  "+island.name,12,PaperInk);
            Text(p,80,496,470,29,"专研来自这片海域独有的生命。",17,Muted);
            Plate(p,588,329,932,455,Paper,true,"Research field sheet");
            Text(p,612,350,670,41,IslandMastery.Names[masteryIsland],30,PaperInk).fontStyle=FontStyles.Bold;
            int current=IslandMastery.Level(run,masteryIsland);
            Text(p,1336,354,161,34,current+" / 3",23,PaperInk,TextAlignmentOptions.Right);
            for(int i=0;i<3;i++)Icon(p,1224+i*34,354,29,NauticalMark.Seal,i<current?PaperInk:new Color(PaperInk.r,PaperInk.g,PaperInk.b,.22f));
            Stitch(p,612,395,881,new Color(PaperInk.r,PaperInk.g,PaperInk.b,.25f));
            Text(p,612,413,878,78,IslandMastery.Description(run,masteryIsland),21,PaperInk);
            string requirement=IslandMastery.UnlockHint(run,masteryIsland);
            Text(p,612,506,878,49,requirement,17,PaperMuted);
            int[] specimens=IslandMastery.RequiredSpecimens(masteryIsland);
            for(int i=0;i<specimens.Length;i++){
                int id=specimens[i];bool known=IslandMastery.IsSpecimenKnown(run,id);var species=ExpeditionContent.Species[id];float x=612+i*447;
                Plate(p,x,566,431,91,new Color(.12f,.245f,.27f));
                Icon(p,x+369,576,39,known?NauticalMark.Seal:NauticalMark.Fish,known?Mint:Gold);
                Text(p,x+16,577,348,31,species.name,20,known?Mint:Cream);
                Text(p,x+16,617,397,28,known?"标本已研究 · 蓝图永久记录本航次":"本岛专属 · "+ExpeditionContent.Lures[species.lure]+" · 钓获后击败",14,Muted);
            }
            if(offer!=null){
                int price=game.Price(offer.id);string locked=ShopCatalog.Lock(run,offer);bool can=locked==""&&price>=0&&run.coins>=price;
                var gate=Text(p,612,678,880,27,locked!=""?locked:current>=3?"完整研究已记录在本次航海日志":"蓝图已解锁 · 购买后立即获得能力",16,PaperInk);gate.gameObject.name="Shop requirement "+offer.id;
                Text(p,612,724,463,30,"发现留下记录，金币决定成长。",17,PaperMuted);
                PurchaseStrip(p,1114,715,382,48,offer.id,locked!=""?offer.Price(run):price,locked!=""?"待解锁":price<0?"已完成全部专研":can?"升级":"金币不足",()=>game.Buy(offer.id),can);
            }
        }
    }
}
