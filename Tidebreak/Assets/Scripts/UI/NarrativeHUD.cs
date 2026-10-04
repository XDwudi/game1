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
        {
            var p=NewModal();var r=game.Run;var chapter=NarrativeContent.Chapter(r.stage);
            Header(p,"VOICES OF THE NINE TIDES / 九潮纪事",game.Island.title,talking?game.Island.npc+" · "+game.Island.name:"船长日志 · 委托、证据与尚未抵达的明天");
            string words=r.questStep==0?chapter.premise:r.questStep<3?chapter.assignment:r.questStep==3&&!r.bossCleared?chapter.discovery:chapter.resolution;
            Box(p,80,281,855,435,Panel);Box(p,80,281,5,435,game.Island.accent);
            Text(p,111,301,791,37,r.questStep==0?"初次相遇":r.questStep<3?"正在进行的委托":r.questStep==3&&!r.bossCleared?"你亲手发现的证据":"离岛之前",19,Gold);
            Text(p,111,361,790,318,words,25,Cream);
            Box(p,963,281,557,435,Panel);
            IslandIllustration(p,977,295,529,181,r.stage);
            Text(p,989,486,505,27,"航海手记 · "+game.Island.name,16,Gold);
            Text(p,989,526,505,76,chapter.mechanic,20,Cream);
            Text(p,989,620,505,76,game.QuestObjective,18,Mint);
            string aftermath="调查进度会自动记录；进行中的护送、守护或操作可在失败后重新启动。";
            if(r.stage==9&&r.questStep>=3)aftermath=r.storyChoice==1?"你的决定：保留航行记忆。幸存者将带着共同的昨天，学习面对真正的明天。":"你的决定：放归被困者。重复的昨天会结束，每个人都能写下自己的新航线。";
            Text(p,80,737,1440,48,aftermath,18,Muted);
            bool ready=r.questStep==0||r.questStep==3&&r.bossCleared&&r.bossTrophy;
            if(talking&&ready)Button(p,80,805,830,56,r.questStep==0?"答应帮忙 · 开始这段航行":"交付潮核 · 与向导告别",()=>game.AdvanceStory(),true);
            else if(r.stage==2&&(r.eventMask&1)!=0&&r.questStep<3)Button(p,80,805,670,56,"回放已经记录的珊瑚旋律",game.PlayMissionMelody);
            Button(p,1090,805,430,56,"返回海岛",()=>{if(talking)game.CloseDialogue();else game.TogglePause();},!ready);
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
