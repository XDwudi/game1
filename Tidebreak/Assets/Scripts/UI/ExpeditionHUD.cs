using System;
using System.Linq;
using TMPro;
using UnityEngine;
namespace Tidebreak
{
    public partial class SeaHUD
    {
        int shopTab,codexIsland,codexSelected=-1,bagPage;RouteKind chartRoute;Camera specimenCamera;
        GameObject specimenRoot;RenderTexture specimenTexture;
        TextMeshProUGUI supplies,navigator;
        void UpdateExpeditionHUD()
        {
            if(!supplies){Box(hud,26,185,410,71,new Color(Ink.r,Ink.g,Ink.b,.83f));supplies=Text(hud,353,815,854,28,"",13,Cream,TextAlignmentOptions.Center);navigator=Text(hud,39,185,410,70,"",15,Gold);}
            var r=game.Run;supplies.text="拟饵："+ExpeditionContent.Lures[r.selectedLure]+"    Z 急救 "+r.medkits+"   X 震爆 "+r.bombs+"   V 冰封 "+r.frostbombs+"   C 声呐 "+r.sonarCharges+"   G 加速 "+r.tonics;
            Vector3 goal=game.MissionTarget;string label=game.MissionTargetLabel;
            if(game.State==VoyageState.Combat){var boss=game.Enemies.FirstOrDefault(e=>e&&e.IsBoss);if(boss){var target=boss.Encounter.Targets.Where(t=>t).OrderBy(t=>GameDirector.FlatDistance(t.transform.position,game.Player.transform.position)).FirstOrDefault();goal=target?target.transform.position:boss.transform.position;label=target?target.Label:"首领核心";}}
            Vector3 dir=goal-game.Player.transform.position;dir.y=0;float angle=Vector3.SignedAngle(game.Player.transform.forward,dir,Vector3.up);
            navigator.text=(angle>25?"→ ":angle< -25?"← ":"↑ ")+label+"  "+Mathf.RoundToInt(dir.magnitude)+" m";
            if(Time.time<game.SonarUntil){
                var remaining=Enumerable.Range(0,3).Where(i=>(r.exploredMask&(1<<i))==0).OrderBy(i=>GameDirector.FlatDistance(game.Player.transform.position,game.SitePoint(i))).ToArray();
                if(remaining.Length==0)navigator.text+="\n声呐：本岛遗迹已探索完毕";
                else {int index=remaining[0];Vector3 echo=game.SitePoint(index)-game.Player.transform.position;echo.y=0;float bearing=Vector3.SignedAngle(game.Player.transform.forward,echo,Vector3.up);navigator.text+="\n声呐 "+(bearing>25?"→ ":bearing< -25?"← ":"↑ ")+(index==0?game.Island.siteA:index==1?game.Island.siteB:game.Island.secret)+" "+Mathf.RoundToInt(echo.magnitude)+"m";}
            }
            if(game.Player.StatusEffect!="")navigator.text+="\n"+game.Player.StatusEffect;
            crosshair.localScale=Vector3.one*(1+game.Player.AimBloom*24);
        }
        void BuildShopPage()
        {
            var r=game.Run;var p=NewModal();Header(p,"ISLAND WORKSHOP / 金币工坊",game.Island.name+" · 装备许可证 "+r.maxIsland+" / 9","持有 "+r.coins+" 金币   ·   新岛屿开放新商品；所有装备与配件仍需金币购买");
            string[] tabs={"枪械与配件","钓具与拟饵","防护与补给","本岛随机库存","流派专精"};
            for(int i=0;i<5;i++){int tab=i;Button(p,80+i*291,266,277,43,tabs[i],()=>{shopTab=tab;BuildShopPage();},shopTab==i);}
            if(shopTab==4){BuildKeystoneStock(p);Button(p,1130,813,390,52,"返回岛屿  ESC",game.CloseShop,true);return;}
            if(shopTab<3){var items=ShopCatalog.All.Where(x=>x.category==shopTab).ToArray();for(int i=0;i<items.Length;i++){
                var item=items[i];float x=80+i%4*364,y=327+i/4*177;Box(p,x,y,346,161,Panel);Text(p,x+16,y+12,314,31,item.name+(item.max<10&&item.max>1?" "+item.level(r)+" / "+item.max:""),21,Cream);Text(p,x+16,y+49,314,50,item.description,15,Muted);
                string locked=ShopCatalog.Lock(r,item);int price=game.Price(item.id);bool can=locked==""&&price>=0&&r.coins>=price;string action=locked!=""?locked:price<0?"已拥有 / 已满":price+" 金币 · "+(can?"购买":"金币不足");
                Button(p,x+16,y+115,314,34,action,()=>game.Buy(item.id),can,can);
            }}else {for(int i=0;i<3;i++){int slot=i;var relic=game.Choices[i];float x=80+i*485;Box(p,x,340,460,310,Panel);Text(p,x+24,368,410,43,relic.name,30,Gold);Text(p,x+24,430,410,110,relic.description,21,Muted);int price=game.Price("relic"+i);bool can=price>=0&&r.coins>=price;Button(p,x+24,575,410,49,price<0?"已购 / 属性已满":price+" 金币 · "+(can?"购买":"金币不足"),()=>game.Buy("relic"+slot),can,can);}Text(p,80,693,1400,58,"共 20 类随机配件，随航路逐步加入库存。每岛三件限购；返回旧岛不会重置已购记录。",19,Muted);}
            Button(p,1130,813,390,52,"返回岛屿  ESC",game.CloseShop,true);
        }
        void BuildIslandMap()
        {
            var p=NewModal();Header(p,"THE NINE TIDES / 群岛航图","归航不是直线，而是九段故事","各岛有独立调查与机关；向向导交付首领潮核后开启新航路。航海插画记录着每一站的线索。");
            for(int i=0;i<9;i++){
                int stage=i+1;var island=ExpeditionContent.Islands[i];float x=80+i%3*486,y=270+i/3*146;bool open=stage<=game.Run.maxIsland,current=stage==game.Run.stage;
                Box(p,x,y,464,132,Panel);Box(p,x,y,5,132,island.accent);IslandIllustration(p,x+13,y+12,108,65,stage,open?1:.24f);Text(p,x+134,y+12,310,31,(i+1).ToString("00")+"  "+island.name,24,open?Cream:Muted);Text(p,x+134,y+49,310,28,island.title,15,Muted);
                Button(p,x+20,y+86,421,35,current?"当前岛屿":open?"前往 · 探索本地生态":"交付前一岛的潮核后解锁",()=>game.Travel(stage,chartRoute),open&&!current,open&&!current);
            }
            string[] routes={"丰渔航路 · 售价 +15%","猎场航路 · 精英更多","秘境航路 · 初访回复生命"};for(int i=0;i<3;i++){int route=i;Button(p,80+i*355,713,335,37,routes[i],()=>{chartRoute=(RouteKind)route;BuildIslandMap();},(int)chartRoute==i);}
            if(game.StoryComplete){Button(p,80,762,335,37,"传说航路 · 克拉肯",game.ChallengeKraken,game.Run.abyssBait||game.Run.rareSignal,game.Run.abyssBait||game.Run.rareSignal);Button(p,435,762,335,37,"传说航路 · 幽海白鲸",game.ChallengeWhiteWhale,game.Run.rareSignal,game.Run.rareSignal);Text(p,790,762,620,37,"收集古神信号 / 购买禁忌鱼饵后开放",15,Muted);}
            Text(p,80,816,1000,55,"B 切换拟饵 · I 打开物种图鉴 · J 查看当前剧情任务\n可重访早期岛屿，用后期拟饵寻找尚未发现的生物。",16,Muted);Button(p,1150,805,370,55,"返回海岛",game.CloseShop,true);
        }
        public void ShowStory(bool talking){ShowNarrativeStory(talking);}
        public void ShowRune()
        {
            string[] symbols={"日轮","月潮","星光"};int first=(game.Run.stage-1)%3;var p=NewModal();Header(p,"A LOST CAPTAIN'S CACHE / 隐藏发现",game.Island.secret,"日志提示："+symbols[first]+" → "+symbols[(first+1)%3]+" → "+symbols[(first+2)%3]+"。按照这段记忆激活机关。");
            Text(p,80,302,1440,70,"已正确点亮 "+game.SecretStep+" / 3 处刻印",32,Gold,TextAlignmentOptions.Center);
            for(int i=0;i<3;i++){int choice=i;Box(p,100+i*493,411,413,196,Panel);Button(p,127+i*493,471,359,72,symbols[i],()=>game.SolveRune(choice),true);}
            Text(p,80,661,1440,70,"选错会惊醒宝箱守卫。正确解开或击败守卫都能取回日志、金币与古神信号。",21,Muted,TextAlignmentOptions.Center);Button(p,1120,794,400,58,"暂时离开",game.CloseDialogue);
        }
        public void ShowSpecies(int id){codexIsland=id>=117?9:id>=108?id-108:id/12;codexSelected=id;BuildCodex();}
        void BuildCodex()
        {
            var p=NewModal();var island=ExpeditionContent.Islands[Mathf.Min(8,codexIsland)];bool rare=codexIsland==9;int seen=game.Log.speciesSeen.Count(v=>v);Header(p,"FIELD GUIDE / 海洋图鉴","已发现 "+seen+" / 119 种生物",rare?"传说海域 · 克拉肯与白鲸 · 完成主线后继续追踪":""+island.name+" · 12 种本地生物与 1 位守关巨物 · 变体不计作新物种");
            int[] ids=rare?new[]{117,118}:Enumerable.Range(codexIsland*12,12).Concat(new[]{108+codexIsland}).ToArray();
            if(!ids.Contains(codexSelected))codexSelected=ids[0];
            for(int i=0;i<ids.Length;i++){int id=ids[i];var spec=ExpeditionContent.Species[id];bool known=game.Log.speciesSeen[id];float x=80+i%4*263,y=278+i/4*113;Box(p,x,y,249,100,Panel);Button(p,x+9,y+9,231,39,known?spec.name:spec.boss?"未知守关巨物":"未发现物种 "+(i+1).ToString("00"),()=>{codexSelected=id;BuildCodex();},id==codexSelected);Text(p,x+15,y+58,221,29,spec.boss?"完成本岛调查":ExpeditionContent.Lures[spec.lure],15,known?Mint:Muted);}
            var selected=ExpeditionContent.Species[codexSelected];bool discovered=game.Log.speciesSeen[codexSelected];Box(p,1155,278,365,451,Panel);RenderSpecimen(p,selected,discovered);
            Text(p,1170,560,332,34,discovered?selected.name:"等待你的发现",22,Gold,TextAlignmentOptions.Center);
            Text(p,1177,611,319,107,discovered?(selected.boss?EncounterDirector.Mechanics[selected.id-108]:ExpeditionContent.AttackNames[(int)selected.attack]+"\n"+ExpeditionContent.Counters[(int)selected.attack]+"\n精英："+EliteTactics.Names[(int)selected.body]):rare?"寻找古神信号或禁忌鱼饵。\n主线完成后，在结局界面\n选择传说海域。":"栖息地："+island.name+"\n拟饵："+ExpeditionContent.Lures[selected.lure]+"\n升级拟饵后可以重访此岛。",15,Muted);
            Text(p,80,754,1440,33,discovered?"收集纪录 · 最大体重 "+game.Log.speciesWeight[codexSelected].ToString("F1")+" kg · 击败 "+game.Log.speciesKills[codexSelected]+" 次 · "+(game.Log.speciesGold[codexSelected]?"金鳞已收录":"金鳞尚未发现"):"未知剪影代表尚未收集的物种。带着新的拟饵重访旧海域，也会找到新的惊喜。",17,Muted);
            Button(p,80,806,280,53,"← 上一片海域",()=>{codexIsland=(codexIsland+9)%10;BuildCodex();});Button(p,379,806,280,53,"下一片海域 →",()=>{codexIsland=(codexIsland+1)%10;BuildCodex();});Button(p,1140,806,380,53,"返回",()=>{if(game.Paused)game.TogglePause();else ShowHarbor();},true);
        }
        void RenderSpecimen(RectTransform p,SpeciesDefinition spec,bool known)
        {
            specimenRoot=new GameObject("Codex specimen studio");specimenRoot.transform.position=new Vector3(0,300,0);var model=SpeciesArt.Build(specimenRoot.transform,spec,false);
            foreach(var co in model.GetComponentsInChildren<Collider>()){co.enabled=false;Destroy(co);}foreach(var t in specimenRoot.GetComponentsInChildren<Transform>())t.gameObject.layer=30;
            if(!known)foreach(var r in model.GetComponentsInChildren<Renderer>()){var block=new MaterialPropertyBlock();block.SetColor("_Color",new Color(.08f,.14f,.17f));r.SetPropertyBlock(block);}
            model.gameObject.AddComponent<SpecimenRotation>();specimenTexture=new RenderTexture(384,320,16);specimenTexture.Create();var cameraObj=new GameObject("Codex camera");cameraObj.transform.SetParent(specimenRoot.transform,false);var cam=cameraObj.AddComponent<Camera>();cam.cullingMask=1<<30;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=Panel;cam.orthographic=true;cam.orthographicSize=spec.boss?spec.size*2:2.65f;cameraObj.transform.localPosition=new Vector3(3,2,6)*(spec.boss?3:1);cameraObj.transform.LookAt(specimenRoot.transform.position);cam.targetTexture=specimenTexture;cam.depth=-10;cam.enabled=false;specimenCamera=cam;var bounds=new Bounds(model.position,Vector3.zero);foreach(var renderer in model.GetComponentsInChildren<Renderer>())bounds.Encapsulate(renderer.bounds);float radius=Mathf.Max(.35f,bounds.extents.magnitude);cam.orthographicSize=radius*.8f;cam.transform.position=bounds.center+new Vector3(1,.35f,1).normalized*radius*3.5f;cam.transform.LookAt(bounds.center);cam.nearClipPlane=.03f;cam.farClipPlane=radius*8;cam.Render();
            var rect=Rect("Specimen portrait",p,1169,292,337,257);var raw=rect.gameObject.AddComponent<UnityEngine.UI.RawImage>();raw.texture=specimenTexture;raw.raycastTarget=false;
        }
        void LateUpdate(){if(specimenCamera)specimenCamera.Render();}
        void ClearSpecimen(){specimenCamera=null;if(specimenRoot)Destroy(specimenRoot);if(specimenTexture){specimenTexture.Release();Destroy(specimenTexture);}specimenRoot=null;specimenTexture=null;}
        void BuildBagPage()
        {
            var r=game.Run;bagPage=Mathf.Clamp(bagPage,0,Mathf.Max(0,(r.BagCapacity-1)/12));var p=NewModal();Header(p,"CATCH MANIFEST / 鱼篓","今天的收获",r.bag.Count+" / "+r.BagCapacity+" 条鱼 · 合计 "+r.BagValue+" 金币 · 到鱼获收购柜台按 E 出售");
            for(int i=0;i<12;i++){int index=bagPage*12+i;if(index>=r.BagCapacity)break;float x=80+i%4*366,y=278+i/4*147;Box(p,x,y,346,130,Panel);if(index>=r.bag.Count){Text(p,x+20,y+45,306,40,"空鱼篓",19,Muted,TextAlignmentOptions.Center);continue;}var f=r.bag[index];Text(p,x+17,y+12,312,36,f.Label,21,f.quality==2?Gold:Cream);Text(p,x+17,y+61,312,28,f.weight.ToString("F1")+" kg · "+f.value+" 金币",18,Mint);Text(p,x+17,y+99,312,21,(f.airshot?"凌空 +25% ":"")+(f.weakshot?"弱点 +15%":""),12,Muted);}
            Button(p,80,789,280,53,"← 上一页",()=>{bagPage--;BuildBagPage();},false,bagPage>0);Button(p,380,789,280,53,"下一页 →",()=>{bagPage++;BuildBagPage();},false,(bagPage+1)*12<r.BagCapacity);Button(p,1120,789,400,53,"继续探索  ESC",game.TogglePause,true);
        }
    }
    public class SpecimenRotation : MonoBehaviour {void Update(){transform.Rotate(0,Time.unscaledDeltaTime*18,0);}}
}
