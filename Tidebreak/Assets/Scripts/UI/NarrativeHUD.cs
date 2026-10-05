using System;
using System.Linq;
using TMPro;
using UnityEngine;

namespace Tidebreak
{
    public partial class SeaHUD
    {
        bool resonanceVisualAid;

        public void ShowNarrativeStory(bool talking)
        { BuildStoryPage(talking,0); }

        static string[] DialoguePages(string text)
        {
            var paragraphs=text.Split(new[]{"\n\n"},StringSplitOptions.RemoveEmptyEntries);
            var pages=new System.Collections.Generic.List<string>();string page="";
            foreach(var paragraph in paragraphs){
                if(page.Length>0&&(page.Length+paragraph.Length>145||page.Contains("\n\n"))){pages.Add(page);page="";}
                page+=(page.Length>0?"\n\n":"")+paragraph;
            }
            if(page.Length>0)pages.Add(page);return pages.Count>0?pages.ToArray():new[]{text};
        }
        void JournalTabs(RectTransform p,int current)
        {
            Button(p,80,267,277,41,"航行纪事",()=>ShowNarrativeStory(false),current==0);
            Button(p,372,267,277,41,"证据收藏",()=>ShowEvidenceJournal(),current==1);
            Button(p,664,267,277,41,"生态笔记",ShowResearchJournal,current==2);
        }
        void BuildStoryPage(bool talking,int pageIndex)
        {
            var p=NewModal();var r=game.Run;var chapter=NarrativeContent.Chapter(r.stage);
            Header(p,"VOICES OF THE NINE TIDES / 九潮纪事",game.Island.title,talking?game.Island.npc+" · "+NarrativeContent.BeatLabel(r,game.MissionActive):"船长日志 · 每一次真实的行动，都在改变这段航程");
            if(!talking)JournalTabs(p,0);
            float top=talking?281:326,height=talking?435:390;
            var pages=DialoguePages(NarrativeContent.Dialogue(r,game.MissionActive));pageIndex=Mathf.Clamp(pageIndex,0,pages.Length-1);
            Plate(p,80,top,855,height,Paper,true);Box(p,96,top+22,2,height-44,new Color(PaperInk.r,PaperInk.g,PaperInk.b,.20f));
            Text(p,121,top+21,771,31,NarrativeContent.BeatLabel(r,game.MissionActive),18,PaperInk);
            Stitch(p,121,top+63,771,new Color(PaperInk.r,PaperInk.g,PaperInk.b,.28f));
            Text(p,121,top+85,770,height-147,pages[pageIndex],27,PaperInk);
            Text(p,121,top+height-41,770,29,"航行记录  "+(pageIndex+1)+" / "+pages.Length,15,PaperMuted,TextAlignmentOptions.Right);
            Plate(p,963,top,557,height,Panel);IslandIllustration(p,977,top+14,529,140,r.stage);
            Text(p,989,top+172,505,27,"下一步 · "+game.Island.name,17,Gold);
            var rules=Text(p,989,top+216,505,height-239,r.questStep>=4?NarrativeContent.Aftermath(r.stage,r.storyChoice):r.questStep==3&&!r.bossCleared?BossNarrative.Teaching(107+r.stage):NarrativeContent.Instructions(r),19,Cream);
            rules.overflowMode=TextOverflowModes.Ellipsis;
            Text(p,80,737,1440,46,game.QuestObjective,18,Mint);
            bool ready=r.questStep==0||r.questStep==3&&r.bossCleared&&r.bossTrophy;
            if(pageIndex>0){int previous=pageIndex-1;Button(p,80,805,235,56,"← 上一句",()=>BuildStoryPage(talking,previous));}
            if(pageIndex<pages.Length-1){int next=pageIndex+1;Button(p,333,805,577,56,"听下去 →",()=>BuildStoryPage(talking,next),true);}
            else if(talking&&ready)Button(p,333,805,577,56,r.questStep==0?"答应帮忙 · 记下这件事":"交付潮核 · 与向导告别",()=>game.AdvanceStory(),true);
            else if(r.stage==2&&(r.eventMask&1)!=0&&r.questStep<3)Button(p,333,805,577,56,"回放记录的珊瑚旋律",game.PlayMissionMelody);
            Button(p,1090,805,430,56,"返回海岛",()=>{if(talking)game.CloseDialogue();else game.TogglePause();});
        }
        public void ShowEvidenceJournal(int selectedStage=0)
        {
            var entries=IslandEvidence.JournalEntries(game.Run);var p=NewModal();
            Header(p,"THE THINGS WE KEPT / 证据收藏","带回来的，不只有鱼获",entries.Count+" / 9 份证物 · 这些文字在世界里真实存在；重读时，也许会听见先前忽略的一句。");
            JournalTabs(p,1);
            if(selectedStage==0)selectedStage=entries.Count>0?entries[entries.Count-1].Stage:game.Run.stage;
            var chosen=entries.FirstOrDefault(e=>e.Stage==selectedStage);
            for(int i=1;i<=9;i++){int stage=i;var entry=entries.FirstOrDefault(e=>e.Stage==stage);Button(p,80,329+(i-1)*46,382,37,entry!=null?i.ToString("00")+"  "+entry.Title:i.ToString("00")+"  尚未找到的记录",()=>ShowEvidenceJournal(stage),stage==selectedStage,entry!=null);}
            Plate(p,493,329,1027,411,Paper,true);Box(p,514,350,3,367,new Color(.57f,.37f,.2f,.4f));
            Text(p,546,354,917,40,chosen!=null?chosen.Title:"让海岛留下更多故事",28,Ink);
            Text(p,546,421,917,270,chosen!=null?chosen.Text:"寻找每座岛上的隐藏现场。比较当事人留下的信件、记录与物件，作出有依据的判断。\n\n所有答案都在现场线索里，读错可以继续尝试。发现后，记录会留在这里。",25,Ink);
            Text(p,80,764,958,58,"地图上的第三处发现藏着一份证物。复访向导时，他也会回应你的发现。",18,Muted);
            Button(p,1120,805,400,56,"返回海岛",game.TogglePause,true);
        }
        public void ShowResearchJournal()
        {
            var p=NewModal();int record=game.ResearchRecord;
            Header(p,"A COAST WORTH KNOWING / 生态笔记",game.Island.name+" · 三处不同的海",game.ResearchHint);
            JournalTabs(p,2);
            Plate(p,80,329,1440,113,Paper,true);
            Text(p,105,350,1390,73,"西  礁隙  ◇ ───────── 船长码头 / 湾心水道 ───────── ◇  海草  东\n转动视角，让浮漂真正落进不同水域；拿起当地鱼获，才算完成一次观察。",22,Ink,TextAlignmentOptions.Center);
            for(int i=0;i<3;i++){
                float x=80+i*486;bool done=(record&(1<<i))!=0;int start=Mathf.Clamp(game.Run.stage-1,0,8)*12;
                int found=game.Run.islandCaught.Count(id=>id>=start&&id<start+12&&(id-start)%3==i);
                Plate(p,x,464,464,252,Panel);Icon(p,x+390,477,47,done?NauticalMark.Seal:NauticalMark.Fish,done?Mint:Gold);
                Text(p,x+23,485,416,37,(done?"◆ ":"◇ ")+GameDirector.HabitatNames[i],27,done?Mint:Cream);
                Text(p,x+23,542,416,90,GameDirector.HabitatClues[i],22,Cream);
                Text(p,x+23,659,416,32,(done?"已记录":"等待第一份样本")+"  ·  本趟收获 "+found+" / 4 种",18,Gold);
            }
            Text(p,80,742,1020,81,"每岛首次完成三处观察：研究津贴 +35 金币。每处钓场四种生物，拟饵升级逐步开放。\n新的拟饵值得带回旧岛；金币用于工坊升级，发现记录不会因出售鱼获而丢失。",18,Muted);
            Button(p,1130,743,390,49,"查看本地物种图鉴",()=>{codexIsland=Mathf.Clamp(game.Run.stage-1,0,8);BuildCodex();});
            Button(p,1130,809,390,53,"返回海岛",game.TogglePause,true);
        }

        public void ShowResonancePuzzle(string status="")
        {
            var p=NewModal();Header(p,"THE SEA ANSWERS / 珊瑚回声","在另一端，回答那个声音","四拍旋律来自潮池的共鸣贝。按同样顺序回应；每个音都有文字与形状提示。");
            Box(p,80,289,1440,102,Panel);
            Text(p,103,306,1394,64,status==""?"已回应 "+game.MelodyStep+" / 4 拍 · 可以先回放，再按下面三个音":status,25,Gold,TextAlignmentOptions.Center);
            Color[] tones={new Color(.34f,.66f,.73f),new Color(.88f,.66f,.34f),new Color(.69f,.55f,.86f)};
            string[] marks={"○", "△", "◇"};
            for(int i=0;i<3;i++){
                int note=i;float x=95+i*491;Box(p,x,424,428,171,Panel);Box(p,x,424,428,5,tones[i]);
                Text(p,x+17,443,394,49,marks[i]+"  "+NarrativeContent.ToneNames[i],28,Cream,TextAlignmentOptions.Center);
                Button(p,x+24,518,380,52,"回应这个音",()=>game.AnswerMissionTone(note));
            }
            Button(p,80,645,432,52,"聆听 / 重播完整旋律",game.PlayMissionMelody,true);
            Button(p,539,645,432,52,resonanceVisualAid?"收起完整文字提示":"显示完整文字提示",()=>{resonanceVisualAid=!resonanceVisualAid;ShowResonancePuzzle();});
            if(resonanceVisualAid){var notes=NarrativeContent.Melody(game.Run.seed).Select(n=>NarrativeContent.ToneNames[n]);Text(p,80,730,1438,55,string.Join("  →  ",notes),23,Mint,TextAlignmentOptions.Center);}
            else Text(p,80,726,1438,54,"文字提示可随时开启。音量、听力或记忆不应阻止你继续这段故事。",18,Muted,TextAlignmentOptions.Center);
            Button(p,1100,805,420,55,"暂时离开音叉",game.CloseDialogue);
        }

        public void ShowAstrolabeEvidence()
        {
            var p=NewModal();Header(p,"THREE WITNESSES / 日轮碑文","不再重复的方向","阿砂：三个刻盘可以各自转动。留意它们彼此的关系，不需要猜一长串密码。");
            string[] evidence={"第一行：星光面向北方。","第二行：日轮在星光的右手一侧。","第三行：月潮与星光背向而立。"};
            for(int i=0;i<3;i++){Box(p,140,306+i*110,1320,91,Panel);Text(p,172,328+i*110,1254,52,evidence[i],29,Cream);}
            Text(p,140,674,1320,57,"记录已收入日志。去另一端的沉沙星盘，分别转动日、月、星三个轴。",23,Gold);
            Button(p,1080,805,440,55,"收好记录，前往星盘",game.CloseDialogue,true);
        }

        public void ShowAstrolabe(string status="")
        {
            var p=NewModal();Header(p,"THE SKY HAS STOPPED / 沉沙星盘","为三颗星找回各自的方向","星光向北；日轮在它右侧；月潮与星光背向。请根据三条证据校准星盘。");
            string[] names={"日轮", "月潮", "星光"};
            for(int i=0;i<3;i++){
                int axis=i;float x=95+i*491;Box(p,x,310,428,316,Panel);Text(p,x+20,334,388,43,names[i],28,Gold,TextAlignmentOptions.Center);
                Text(p,x+20,404,388,76,NarrativeContent.Directions[game.AstrolabeAxes[i]],61,Cream,TextAlignmentOptions.Center);
                Button(p,x+30,536,368,56,"转动一格",()=>game.RotateAstrolabe(axis));
            }
            Text(p,95,668,1410,60,status==""?"三个轴独立调整。对齐后，按下方按钮验证整幅航图。":status,22,Mint,TextAlignmentOptions.Center);
            Button(p,80,805,720,55,"确认星盘方位",()=>game.ConfirmAstrolabe(),true);Button(p,1090,805,430,55,"暂时离开星盘",game.CloseDialogue);
        }

        public void ShowMemoryChoice()
        {
            var p=NewModal();Header(p,"A TOMORROW OF THEIR OWN / 最后的选择","机器能保存什么，人又能放下什么","零：关闭循环之后，这些被重复的航行，该留在世界里，还是与囚笼一起结束？");
            Box(p,80,310,700,375,Panel);Box(p,812,310,708,375,Panel);
            Text(p,109,337,642,48,"让他们放下昨天",31,Gold);Text(p,841,337,650,48,"把航行记忆带回去",31,Mint);
            Text(p,109,408,642,196,"被困者不会再听见反复响起的求救。\n\n他们得到安静的明天，却可能永远不知道，有多少人在昨天为他们留下过一盏灯。\n\n封锚路径：东侧 → 西侧。",24,Cream);
            Text(p,841,408,650,196,"幸存者会记得那些没有归来的名字。\n\n痛苦也会随记忆留下，但他们可以讲述这次航行，让迟到的告别真正发生。\n\n封锚路径：西侧 → 东侧。",24,Cream);
            Text(p,80,710,1440,72,"保留记忆会让终战召来更多回声守卫。两种选择都能结束囚禁，金币奖励相同。\n这个决定会改变封锚顺序，并留在你的结局与航海日志里。",20,Muted,TextAlignmentOptions.Center);
            Button(p,109,615,642,50,"放归被困者",()=>game.ChooseMemory(0),true);Button(p,841,615,650,50,"保留航行记忆",()=>game.ChooseMemory(1),true);
            Button(p,1130,812,390,49,"先离开，再想一想",game.CloseDialogue);
        }
    }
}
