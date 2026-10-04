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
        TextMeshProUGUI supplies,navigator,waypointLabel,waypointGlyph;
        RectTransform waypointPanel,navigationPanel;
        void UpdateExpeditionHUD()
        {
            if(!supplies){navigationPanel=Rect("Voyage navigation",hud,26,181,348,92);Box(navigationPanel,0,0,348,92,new Color(Ink.r,Ink.g,Ink.b,.83f));supplies=Text(hud,338,827,900,21,"",12,Cream,TextAlignmentOptions.Center);navigator=Text(navigationPanel,16,10,316,75,"",15,Gold);navigator.overflowMode=TextOverflowModes.Ellipsis;
                waypointPanel=Rect("World action bearing",hud,670,354,260,58);Box(waypointPanel,0,0,35,35,new Color(Ink.r,Ink.g,Ink.b,.73f));Box(waypointPanel,38,0,222,54,new Color(Ink.r,Ink.g,Ink.b,.8f));waypointGlyph=Text(waypointPanel,0,0,35,35,"◇",28,Gold,TextAlignmentOptions.Center);waypointLabel=Text(waypointPanel,44,3,210,48,"",14,Cream);waypointLabel.overflowMode=TextOverflowModes.Ellipsis;}
            var r=game.Run;supplies.text="拟饵："+ExpeditionContent.Lures[r.selectedLure]+"    Z 急救 "+r.medkits+"   X 震爆 "+r.bombs+"   V 冰封 "+r.frostbombs+"   C 声呐 "+r.sonarCharges+"   G 加速 "+r.tonics;
            Vector3 goal=game.MissionTarget;string label=game.MissionTargetLabel;
            var mechanism=game.ActiveBossMechanism;
            navigationPanel.gameObject.SetActive(!mechanism);
            if(game.State==VoyageState.Combat){var boss=game.Enemies.FirstOrDefault(e=>e&&e.IsBoss);if(boss){goal=mechanism&&mechanism.Active?mechanism.TargetPoint:boss.transform.position;label=mechanism&&mechanism.Active?mechanism.TargetLabel:"首领核心 · 破绽可攻击";}}
            Vector3 dir=goal-game.Player.transform.position;dir.y=0;float angle=Vector3.SignedAngle(game.Player.transform.forward,dir,Vector3.up);
            navigator.text=(angle>25?"→ ":angle< -25?"← ":"↑ ")+label+"  "+Mathf.RoundToInt(dir.magnitude)+" m";
            if(game.State!=VoyageState.Combat&&game.Player.RodEquipped)navigator.text+="\n"+game.ResearchHint;
            if(Time.time<game.SonarUntil){
                var remaining=Enumerable.Range(0,3).Where(i=>(r.exploredMask&(1<<i))==0).OrderBy(i=>GameDirector.FlatDistance(game.Player.transform.position,game.SitePoint(i))).ToArray();
                if(remaining.Length==0)navigator.text+="\n声呐：本岛遗迹已探索完毕";
                else {int index=remaining[0];Vector3 echo=game.SitePoint(index)-game.Player.transform.position;echo.y=0;float bearing=Vector3.SignedAngle(game.Player.transform.forward,echo,Vector3.up);navigator.text+="\n声呐 "+(bearing>25?"→ ":bearing< -25?"← ":"↑ ")+(index==0?game.Island.siteA:index==1?game.Island.siteB:game.Island.secret)+" "+Mathf.RoundToInt(echo.magnitude)+"m";}
            }
            if(game.Player.StatusEffect!="")navigator.text+="\n"+game.Player.StatusEffect;
            bool waypoint=game.IsPlaying&&!game.Paused&&!game.CinematicActive&&game.State!=VoyageState.Fishing&&!(mechanism&&mechanism.Active&&mechanism.BossIndex==10&&mechanism.StateStep==2);
            waypointPanel.gameObject.SetActive(waypoint);
            if(waypoint)UpdateWorldBearing(goal,mechanism?label.Split('·')[0].Trim():label,mechanism&&mechanism.Active?Gold:Mint);
            crosshair.localScale=Vector3.one*(1+game.Player.AimBloom*24);
        }
        void UpdateWorldBearing(Vector3 goal,string label,Color color)
        {
            var view=game.Player.View;Vector3 viewport=view.WorldToViewportPoint(goal+Vector3.up*1.4f);
            bool behind=viewport.z<=0;float x=viewport.x*1600,y=(1-viewport.y)*900;
            if(behind){x=viewport.x<.5f?1510:55;y=420;}
            bool edge=behind||x<55||x>1320||y<365||y>655;
            x=Mathf.Clamp(x,55,1320);y=Mathf.Clamp(y,365,655);
            // A label near the aiming reticle is lifted above it; the core stays readable.
            if(x>550&&x<1050&&y>405&&y<615)y=365;
            waypointPanel.anchoredPosition=new Vector2(x,-y);
            Vector3 direction=goal-game.Player.transform.position;direction.y=0;
            float angle=Vector3.SignedAngle(game.Player.transform.forward,direction,Vector3.up);
            waypointGlyph.text=edge?(angle>15?"→":angle< -15?"←":"↑"):"◇";
            waypointGlyph.color=color;waypointLabel.text=label+"\n"+Mathf.RoundToInt(direction.magnitude)+" m";
        }
        void BuildShopPage()
        {
            var r=game.Run;var p=NewModal();Header(p,"ISLAND WORKSHOP / 金币工坊",game.Island.name+" · 装备许可证 "+r.maxIsland+" / 9","持有 "+r.coins+" 金币   ·   新岛屿开放新商品；所有装备与配件仍需金币购买");
            string[] tabs={"枪械与配件","钓具与拟饵","防护与补给","本岛随机库存","流派专精","岛屿专研"};
            for(int i=0;i<6;i++){int tab=i;Button(p,80+i*242,266,228,43,tabs[i],()=>{shopTab=tab;BuildShopPage();},shopTab==i);}
            if(shopTab==5){BuildMasteryStock(p);Button(p,1130,813,390,52,"返回岛屿  ESC",game.CloseShop,true);return;}
            if(shopTab==4){BuildKeystoneStock(p);Button(p,1130,813,390,52,"返回岛屿  ESC",game.CloseShop,true);return;}
            if(shopTab<3){var items=ShopCatalog.All.Where(x=>x.category==shopTab).ToArray();for(int i=0;i<items.Length;i++){
                var item=items[i];float x=80+i%4*364,y=327+i/4*158;Box(p,x,y,346,145,Panel);Text(p,x+16,y+12,314,31,item.name+(item.max<10&&item.max>1?" "+item.level(r)+" / "+item.max:""),21,Cream);Text(p,x+16,y+46,314,48,item.Description(r),14,Muted);
                string locked=ShopCatalog.Lock(r,item);int price=game.Price(item.id);bool can=locked==""&&price>=0&&r.coins>=price;string action=locked!=""?locked:price<0?"已拥有 / 已满":price+" 金币 · "+(can?"购买":"金币不足");
                Button(p,x+16,y+105,314,32,action,()=>game.Buy(item.id),can,can);
            }}else {for(int i=0;i<3;i++){int slot=i;var relic=game.Choices[i];float x=80+i*485;Box(p,x,340,460,310,Panel);Text(p,x+24,368,410,43,relic.name,30,Gold);Text(p,x+24,430,410,110,relic.description,21,Muted);int price=game.Price("relic"+i);bool can=price>=0&&r.coins>=price;Button(p,x+24,575,410,49,price<0?"已购 / 属性已满":price+" 金币 · "+(can?"购买":"金币不足"),()=>game.Buy("relic"+slot),can,can);}Text(p,80,693,1400,58,"共 20 类随机配件，随航路逐步加入库存。每岛三件限购；返回旧岛不会重置已购记录。",19,Muted);}
            Button(p,1130,813,390,52,"返回岛屿  ESC",game.CloseShop,true);
        }
        void BuildIslandMap()
        {
            var p=NewModal();Header(p,"THE NINE TIDES / 群岛航图","沿着他人留下的灯，找到自己的航路","交付潮核开启下一站；每座岛还有三处生态观察和一份值得带走的证物。");
            Color paper=new Color(.84f,.87f,.79f),chartInk=new Color(.17f,.33f,.33f);
            Box(p,80,269,1440,421,paper);
            for(int x=110;x<1500;x+=88)Box(p,x,279,1,397,new Color(chartInk.r,chartInk.g,chartInk.b,.12f));
            for(int y=282;y<684;y+=55)Box(p,93,y,1414,1,new Color(chartInk.r,chartInk.g,chartInk.b,.12f));
            Text(p,1290,290,194,61,"N  ↑\n九潮水道",16,chartInk,TextAlignmentOptions.Center);
            var points=new[]{new Vector2(201,333),new Vector2(562,333),new Vector2(923,333),new Vector2(1230,444),new Vector2(850,480),new Vector2(475,480),new Vector2(201,609),new Vector2(562,609),new Vector2(923,609)};
            for(int i=1;i<points.Length;i++)ChartLine(p,points[i-1],points[i],i<game.Run.maxIsland?chartInk:new Color(.5f,.6f,.57f));
            for(int i=0;i<9;i++){
                int stage=i+1;var island=ExpeditionContent.Islands[i];Vector2 at=points[i];bool open=stage<=game.Run.maxIsland,current=stage==game.Run.stage;
                IslandIllustration(p,at.x-55,at.y-49,110,67,stage,open?1:.3f);
                Text(p,at.x+64,at.y-47,206,29,stage.ToString("00")+"  "+island.name,20,open?Ink:chartInk);
                bool evidence=IslandEvidence.IsCollected(game.Run,stage);
                Text(p,at.x+64,at.y-11,204,43,current?"你在这里 · 继续探索":open?(evidence?"证物已入册":"尚有故事等待发现"):"航路仍未开放",14,chartInk);
                Button(p,at.x-63,at.y+27,184,31,current?"当前停泊":open?"前往此岛 →":"等待解锁",()=>game.Travel(stage,chartRoute),current,open&&!current);
            }
            string[] routes={"丰渔航路 · 售价 +15%","猎场航路 · 精英更多","秘境航路 · 初访回复生命"};for(int i=0;i<3;i++){int route=i;Button(p,80+i*355,713,335,37,routes[i],()=>{chartRoute=(RouteKind)route;BuildIslandMap();},(int)chartRoute==i);}
            if(game.StoryComplete){Button(p,80,762,335,37,"传说航路 · 克拉肯",game.ChallengeKraken,game.Run.abyssBait||game.Run.rareSignal,game.Run.abyssBait||game.Run.rareSignal);Button(p,435,762,335,37,"传说航路 · 幽海白鲸",game.ChallengeWhiteWhale,game.Run.rareSignal,game.Run.rareSignal);Text(p,790,762,620,37,"收集古神信号 / 购买禁忌鱼饵后开放",15,Muted);}
            Text(p,80,816,1000,55,"B 切换拟饵 · I 物种图鉴 · J 航行纪事 / 证据 / 生态\n带新拟饵重访旧海域，发现先前无法钓起的生物。",16,Muted);Button(p,1150,805,370,55,"返回海岛",game.CloseShop,true);
        }
        void ChartLine(RectTransform p,Vector2 a,Vector2 b,Color color)
        {
            Vector2 delta=b-a;int pieces=Mathf.Max(1,Mathf.CeilToInt(delta.magnitude/18));
            for(int i=0;i<pieces;i++){
                Vector2 point=Vector2.Lerp(a,b,(i+.5f)/pieces);var line=Box(p,point.x,point.y,9,2,color);line.rectTransform.pivot=new Vector2(.5f,.5f);line.rectTransform.localRotation=Quaternion.Euler(0,0,-Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg);
            }
        }
        public void ShowStory(bool talking){ShowNarrativeStory(talking);}
        public void ShowRune()
        {
            int stage=game.Run.stage;var p=NewModal();Header(p,"EVIDENCE AT THE WATERLINE / 现场推理",NarrativeContent.EvidenceTitle(stage),"比较现场的记录，作出你的判断。发现的证据会收进船长日志，而不是消失在任务完成后。");
            Box(p,80,282,814,439,new Color(.91f,.87f,.74f));Box(p,98,296,3,411,new Color(.58f,.39f,.24f,.5f));
            Text(p,119,308,734,380,NarrativeContent.SecretQuestion(stage),24,Ink);
            var options=NarrativeContent.SecretOptions(stage);
            for(int i=0;i<options.Length;i++){int choice=i;Button(p,934,292+i*99,586,78,(i+1)+"  "+options[i],()=>game.SolveRune(choice));}
            Text(p,948,610,552,112,string.IsNullOrEmpty(game.SecretFeedback)?"每个判断都有依据。暂时读错也可以继续比较，不会扣除金币。":game.SecretFeedback,20,string.IsNullOrEmpty(game.SecretFeedback)?Muted:Gold);
            Text(p,80,752,960,70,"记录真实的行动，也记录没有说出口的话。\nJ 打开日志，可以重读所有已发现的证物。",19,Muted);Button(p,1120,804,400,58,"收好现场，稍后再来",game.CloseDialogue);
        }
        public void ShowSpecies(int id){codexIsland=id>=117?9:id>=108?id-108:id/12;codexSelected=id;BuildCodex();}
        void BuildCodex()
        {
            var p=NewModal();var island=ExpeditionContent.Islands[Mathf.Min(8,codexIsland)];bool rare=codexIsland==9;int seen=game.Log.speciesSeen.Count(v=>v);Header(p,"FIELD GUIDE / 海洋图鉴","已发现 "+seen+" / 119 种生物",rare?"传说海域 · 克拉肯与白鲸 · 完成主线后继续追踪":""+island.name+" · 12 种原生生物 · 80% 本地池 · 2 种专研标本");
            int[] ids=rare?new[]{117,118}:Enumerable.Range(codexIsland*12,12).Concat(new[]{108+codexIsland}).ToArray();
            if(!ids.Contains(codexSelected))codexSelected=ids[0];
            for(int i=0;i<ids.Length;i++){int id=ids[i];var spec=ExpeditionContent.Species[id];bool known=game.Log.speciesSeen[id];float x=80+i%4*263,y=278+i/4*113;Box(p,x,y,249,100,Panel);Button(p,x+9,y+9,231,39,known?spec.name:spec.boss?"未知守关巨物":"未发现物种 "+(i+1).ToString("00"),()=>{codexSelected=id;BuildCodex();},id==codexSelected);Text(p,x+15,y+58,221,29,spec.boss?"完成本岛调查":spec.endemic?"专研标本 · "+(IslandMastery.IsSpecimenKnown(game.Run,id)?"已研究":"未研究"):ExpeditionContent.Lures[spec.lure],15,known?Mint:Muted);}
            var selected=ExpeditionContent.Species[codexSelected];bool discovered=game.Log.speciesSeen[codexSelected];Box(p,1155,278,365,451,Panel);RenderSpecimen(p,selected,discovered);
            Text(p,1170,560,332,34,discovered?selected.name:"等待你的发现",22,Gold,TextAlignmentOptions.Center);
            Text(p,1177,611,319,107,discovered?(selected.boss?BossNarrative.Get(selected.id).motive+"\n"+BossNarrative.Teaching(selected.id):ExpeditionContent.AttackNames[(int)selected.attack]+"\n"+ExpeditionContent.Counters[(int)selected.attack]+"\n精英："+EliteTactics.Names[(int)selected.body]):rare?"寻找古神信号或禁忌鱼饵。\n主线完成后，在结局界面\n选择传说海域。":"栖息地："+island.name+"\n拟饵："+ExpeditionContent.Lures[selected.lure]+"\n升级拟饵后可以重访此岛。",15,Muted);
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
