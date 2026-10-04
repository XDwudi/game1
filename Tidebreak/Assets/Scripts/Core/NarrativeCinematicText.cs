using UnityEngine;

namespace Tidebreak
{
    // Short lines sized for existing four/five-second real-time shots. These are
    // consistent with the journal; camera direction remains in VoyageCinematics.
    public static class NarrativeCinematicText
    {
        static readonly string[] opening={
            "风暴停了。七艘船没有归港，海面的航迹却像被同一阵浪擦过。",
            "我记得离家时，灯塔的光照向岸。昨夜看见的那束光，照向了海底。",
            "先把真灯修好。这次出海，让岸上有人知道你要去哪。",
            "先问清一个人的去向，再决定下一段航路。"
        };
        static readonly string[] observations={
            "透镜朝向港内。海底另有一束光，它的节拍和灯塔一模一样。",
            "所有珊瑚唱着同一个音。布包里的小贝，却一直想多唱一拍。",
            "净水舟走过同一条河。每次靠岸，舟底都会多一道手指刻痕。",
            "碑上的盐痕很老，背面的螺栓却和灯塔电池箱用着同一规格。",
            "不同船钟停在同一分钟。黑匣仍在录，红灯已经亮了很久。",
            "冰墙上贴着十二张船票。舱窗里面，一只手还握着没撕角的票根。",
            "一张家庭照片压在总闸下。工程师的手遮着关机杆，也在发抖。",
            "潮核里传来数救生衣的声音。铸潮匠把火调小，先听完最后一个数。",
            "镜子里有七艘船。每次将要驶出画面，它们就回到原来的位置。"
        };
        static readonly string[] arrivals={
            "先把岸灯修好。回家的人不该跟着一束来路不明的光。",
            "声音像我女儿，不够。我要听见她知道怎样回答。",
            "净水舟救过人。先弄清是谁改了水路，再决定该拆什么。",
            "同一天出现得再多，也不叫预言。我们看看它背面写了什么。",
            "别急着回答黑匣里的名字。这次，我陪你把录音听完。",
            "票不是样本。人醒来以后，我得亲手还给她。",
            "阿澜还活着。可别人的孩子也在里面，我不能只替自己松一口气。",
            "我要保住的不只是钥匙的形状。你听，里面还有人在说话。",
            "我一直在等一份没有风险的出发申请。你们似乎不会那样写。"
        };
        static readonly string[] blackBox={
            "第八次。别跟海底的假灯走。真实航灯要留着，有人还在回家。",
            "是你的声音。同一场风暴磨旧了船，也磨掉了我们对次数的记忆。",
            "第八次尝试：乘客尚未全数离开。归航协议再次重播今日。",
            "先去救生舱，别只救记录。这一次，我听先前的自己。"
        };
        static readonly string[] confession={
            "露珂刚回了消息。阿澜想自己选一班船回家，她还让我不要拦着。",
            "我救回了女儿，也没有资格替其他乘客决定何时醒来、要去哪。",
            "我把安全写成了唯一的答案，却没问她们到底在为什么出发。",
            "把九岛的关闭指令带去火山。权限不该再只握在我一个人手里。"
        };
        public static string Opening(int shot){return opening[Mathf.Clamp(shot,0,3)];}
        public static string ArrivalObservation(int stage){return observations[Mathf.Clamp(stage-1,0,8)];}
        public static string ArrivalReply(int stage){return arrivals[Mathf.Clamp(stage-1,0,8)];}
        public static string BlackBox(int shot){return blackBox[Mathf.Clamp(shot,0,3)];}
        public static string Confession(int shot){return confession[Mathf.Clamp(shot,0,3)];}
        public static string BlackBoxDocument {get{return "北星号 · 第八份记录\n\n如果你再次听见自己，\n先去找救生舱里的人。\n\n别只救这份记录。";}}
        public static string ConfessionDocument {get{return "阿澜留给父亲的口信\n\n爸，我知道海上有风险。\n下一班船，我想自己选。\n\n等我到家，再教你钓鱼。";}}
        public static string Ending(int shot,int choice)
        {
            bool remember=choice==1;
            if(shot==0)return remember?"重复中的记忆已归还乘客。它不能倒转时间，只属于真实活过这些事的人。":"重复中的记忆随回路散去。已经获救的人仍会醒来，救援没有被撤销。";
            if(shot==1)return remember?"留下他们的名字。今后讲起这段航行，也让他们自己决定从哪一句说起。":"关掉朝海底照的假灯。家里的归航灯，留给每一艘回来的船。";
            if(shot==2)return "听见了吗？七艘船的铃声各不相同。港口的钟，终于走过了午夜。";
            return "今天没有保证。但有风，有海，还有一条我们可以自己选择的航线。";
        }
        public static string Legendary(int speciesId,int shot)
        {
            bool kraken=speciesId==117;
            if(shot==0)return kraken?"没有机器在命令它。水下那只眼睛，正在决定要不要把航道让出来。":"它跟着冰里的假回音转了很久。白色山脊一动，整片海都跟着转身。";
            if(shot==1)return kraken?"腕能砸断船，也会被潮带走。用竿挂住它，别试着一次拉赢整个海。":"先看两道相伴的回波，再从岸边另一处确认。第一眼看到的，不一定是它。";
            return kraken?"平潮收线，红潮松手。等触腕退去，再把枪口送向海上的巨眼。":"发出脉冲，换位核验。把真假分开，才有机会看清它破冰的一口呼吸。";
        }

        public static string TextFor(int sequence,int shotIndex,string fallback,int storyChoice)
        {
            if(sequence==0)return Opening(shotIndex);
            if(sequence>=1&&sequence<=9)
            {
                if(shotIndex==0)return ArrivalObservation(sequence);
                if(shotIndex==2)return ArrivalReply(sequence);
                return fallback; // Keep the landscape's island/chapter title.
            }
            if(sequence==10||sequence==11)return Legendary(sequence==10?117:118,shotIndex);
            if(sequence==12)return Ending(shotIndex,storyChoice);
            if(sequence==13)return BlackBox(shotIndex);
            if(sequence==14)return Confession(shotIndex);
            return fallback;
        }
        public static string DocumentFor(int sequence,int shotIndex,string fallback)
        {
            if(sequence==13&&shotIndex==2)return BlackBoxDocument;
            if(sequence==14&&shotIndex==2)return ConfessionDocument;
            return fallback;
        }
    }
}
