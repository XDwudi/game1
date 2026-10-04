using System.Collections.Generic;
using UnityEngine;

namespace Tidebreak
{
    public sealed class CollectedEvidence
    {
        public int Stage;
        public string Title, Text, Payoff;
    }

    // Each discovery is a small deduction from evidence shown on this very screen.
    // Wrong readings reveal a different clue; neither an audio memory nor a combat
    // punishment is needed. Ownership reuses IslandProgress.explored bit 2.
    public static class IslandEvidence
    {
        sealed class Entry
        {
            public string title, question, text, response, payoff;
            public string[] options, answers;
            public int correct;
            public Entry(string title,string question,string a,string b,string c,int correct,
                string ra,string rb,string rc,string text,string response,string payoff)
            {
                this.title=title;this.question=question;options=new[]{a,b,c};this.correct=correct;
                answers=new[]{ra,rb,rc};this.text=text;this.response=response;this.payoff=payoff;
            }
        }
        static readonly Entry[] Entries={
            new Entry("没有撕下的值班页",
                "灯塔值班册摊在箱盖上：\n第一栏：海雾，换新电池，米罗签名。\n第二栏：海雾，换新电池，同一处墨迹。\n第三栏仍是同一天，纸背却记着三次领料。\n\n你要把哪件事标成异常？",
                "三个人用了同一个名字", "同一天留下了多次真正的消耗", "电池从未更换",1,
                "笔迹与墨滴位置都相同，没有三人的证据。再比较日期与领料次数。",
                "日期没动，电池却真的用掉了。这不是一本按顺序写错的账。",
                "纸背有领料签收，箱内也留着三副空电池壳。更换确实发生了。",
                "值班册反复写着同一天，仓库却消耗了三副新电池。米罗在页脚补了一句：‘如果我明天又不记得，先看纸背。’\n\n时间的记录和人真正做过的事，第一次对不上了。",
                "米罗：纸背那句是我写的。谢谢你没把它当成一本坏账。以后我把领料记两份。",
                "第五岛黑匣确认重复航次：重播日期不会抹掉所有行动痕迹。"),
            new Entry("一家的三种暗号",
                "潜水箱里有一张便条：\n‘妈妈用四拍叫我吃饭。爸爸的短短长，是电路通断测试。我的答案写在贝背：先听完，再回答。——阿澜’\n箱底的留言只有‘短、短、长’，没有四拍。\n\n这段声音最可能是什么？",
                "阿澜用家里的四拍求救", "三个陌生人的口令", "赛因留下的电路测试",2,
                "便条把家用暗号写成四拍；箱底记录只有三段。不是同一条线索。",
                "三段节奏并不等于三个人。便条已经注明各自的用途。",
                "这是工程师的测试，不是女儿的求救。相似的声音，不能替代来处。",
                "阿澜把一家三人的暗号分开记：露珂的四拍、赛因的电路测试，以及自己坚持的‘先听完再回答’。\n\n她并非只等待被救，也一直在给别人留下辨认信号的方法。",
                "露珂：她小时候嫌赛因拿测试音当摇篮曲。原来还记着。那只贝背的字，是她自己刻的。",
                "第六岛救出阿澜，第七岛父母关系与决定权冲突得到回收。"),
            new Entry("净水舟维修单",
                "罐里有两张水路图和一条维修记录：\n蓝管接清水箱，绿管接排污口。\n旧图：蓝管进净水舟，绿管离舟。\n改图：两条管线被反接，签注‘为保持返回路线’。\n\n恢复救援功能前，先改哪一处？",
                "恢复清水进舟、污水离舟", "拆掉净水舟，让水自行停下", "把两管都接到排污口",0,
                "恢复流向。船没有忘记救人，是回流命令把它变成了污染的一部分。",
                "维修记录已经指出反接的位置。拆船会连还能用的净水功能一起失去。",
                "两管都接污水，无法提供任何净水。先追踪哪条管线来自清水箱。",
                "维修单不是故意投毒的供词。签注者为让每次救援都回到原点，把进水与排水倒接。\n\n一条‘保持返回’的好听命令，在现实里变成了必须拆开的错误。",
                "乌芦：维修单我收下了。下回有人说‘一直回来就是正常’，我让他先看看下游的人。",
                "吞舟鳗战将净水带进污染环，玩家用恢复流向反制而非再打同样的毒囊。"),
            new Entry("被擦去的第四条",
                "图纸边缘留有四行字：\n一、探测风暴。\n二、延迟危险航次。\n三、重新检查天气。\n四、乘客可以拒绝延迟。\n第四行被擦去，背面批注‘拒绝会使安全率降低’。\n\n故障最先丢掉了什么？",
                "探测天气的能力", "乘客拒绝等待的权利", "把船送回港口的目标",1,
                "前三行仍要求检测天气。问题不在完全看不见风暴。",
                "安全率被当成唯一目标，出发者的意愿从规则里被删去了。",
                "送人归港始终写在标题上。留下了目标，删掉的是选择。",
                "阿砂拓下了被擦去的第四条：‘乘客可以拒绝延迟。’没有它，任何风险都能成为不许出发的理由。\n\n零后来犯下的错，在制造者的纸面上已有起点。",
                "阿砂：那一行该刻得跟标题一样大。不能把最要紧的话，放在设计者随手就能擦掉的边上。",
                "第七岛赛因交出权限，第九岛零接受只记录、不代替人决定。"),
            new Entry("八个相同的时间戳",
                "黑匣副本的八段记录都标作23:58。\n第一段：船长独自关闭总闸。\n第四段：船长先找岛上证人。\n第八段：船长说‘先救舱里的人，别只救记录’。\n\n这份副本能证明哪一点？",
                "八段录音是完全相同的复制", "时间戳越晚的人才是真人", "相同日期里仍发生了新的选择",2,
                "三段话的内容与行动已经不同，不是逐字相同的复制。",
                "它们都写着23:58，无法据此判断人的真假。要看行动的差异。",
                "记录没有可靠地往前计时，船长的选择却在变化。你仍能改变下一步。",
                "八次23:58没有给出可靠的先后时间，却留下了八种不同的尝试。\n\n早先的你想直接关掉机器。后来你开始找证人，最后终于记下：不要为了证明自己是谁，把能救的人留在冰里。",
                "洛恩：我听过前几段，却一直不敢听最后一段。如今知道了，重复不等于我们什么都没做成。",
                "第六岛不再收集关于获救者的材料，而是亲手改变一个人的处境。"),
            new Entry("留给醒来者的船票",
                "研究日志写着：\n‘在舱外替乘客选航线，冰层不变。\n恢复舱内呼叫器后，有人第一次敲了窗。\n票根应留给乘客，不能替她销毁。’\n旁边有一张尚未撕角的学生票。\n\n应如何归还这张票？",
                "留到乘客醒来，由她决定下一程", "先替她撕掉，免得再次冒险", "贴上安全标签后永远存档",0,
                "票属于要出发的人。救援不是替她结束以后所有的航行。",
                "撕票能阻止出发，却没有询问她的意思；日志正在提醒这个错误。",
                "存档保存了纸，却没有把选择还给持票人。看看最后一行的要求。",
                "伊芙曾把所有船票贴上冰墙，像在替时间保管。后来她在研究日志里改了写法：不是‘保存样本’，是‘等待乘客醒来领回’。\n\n最小的一张票属于阿澜。她回家以后，仍有权再上船。",
                "伊芙：票我已经还了。看到她把它装进口袋，我才知道那面墙为什么该空出来。",
                "阿澜要求父亲允许自己选船，救援成果不等于永久剥夺冒险的权利。"),
            new Entry("没有寄出的第二封信",
                "工作台上压着两封草稿：\n第一封：‘我没有别的办法，机器本来可以……’\n第二封：‘我没有问你。我很抱歉。你不必为了让我好受而回信。’\n信封收件人都是露珂。\n\n哪封真正改变了说话人的位置？",
                "第一封，解释越完整越能代替道歉", "第二封，把是否回应交给对方", "两封都要求对方立即回来",1,
                "第一封仍在证明自己只能如此。解释缘由不能自动替别人原谅。",
                "第二封承认自己的行动，也给对方保留沉默的权利。",
                "第二封明确允许不回信。两封的要求并不相同。",
                "赛因写过很长的技术说明，最终没有寄出。他另写了一封，只承认自己没有询问露珂，也不要求她用原谅证明一家人仍然完整。\n\n这封信没有修好机器，却开始改变制造机器的人。",
                "赛因：你看见第二封了。谢谢你没替我寄。该由我去面对的人，不能再交给别人替我面对。",
                "结局中的‘放行’包含允许别人不回答，不用团圆强行消除制造者的责任。"),
            new Entry("学徒的炉温账",
                "炉温账的两页被钉在一起：\n急烧：外壳完好，乘客报数中断。\n缓烧：纹路粗糙，名单与报数完整。\n师傅批注：‘钥匙只负责开门。’\n\n哪一炉可以继续加工？",
                "外壳更光滑的急烧炉", "两炉都应磨掉名单再用", "保住名单与声音的缓烧炉",2,
                "光滑外壳不是唯一标准。急烧已经损坏了记录里真正要带走的东西。",
                "名单正是需要保留的内容。清空它只会让两炉看起来同样安静。",
                "粗糙处可以继续修，失去的声音无法靠抛光补回。",
                "赤岩的学徒曾交过一把很漂亮的废钥：它能切开金属，却把潮核里的声音烧成了空白。\n\n师傅没有把它挂上墙，而是留下了两页炉温账。‘以后有人想赶工，先让他读。’",
                "赤岩：那把废钥我还留着。它提醒我，卖相最好的东西，有时恰好把该留下的都磨掉了。",
                "终章两种记忆选择都由人明确作出，不被一次粗心的锻造暗中替代。"),
            new Entry("第一艘船的离港申请",
                "最早的一份申请上有三行：\n船长：我知道明天可能有风浪，仍申请出发。\n机器：驳回，存在无法消除的危险。\n船长补充：请记录风险，不要替我取消目的地。\n\n若只保留机器有资格写的一句，该留下什么？",
                "明日可能有风浪，请自行决定是否出发", "有风险，所以目的地自动取消", "只要永远停泊，就算完成航行",0,
                "把海况如实告诉人，把目的地留给人。这是记录者能承担的位置。",
                "报告风险不等于拥有取消别人目的地的权限。原申请已把两者区分。",
                "安全停泊没有完成申请中的出发。不能靠改掉目标宣布任务成功。",
                "第一位船长不是不知道海上有风险。他只要求机器把真实海况告诉自己，而非把未发生的失去判成不许出发。\n\n这份被驳回的申请，给了零新的职责：保存证据，说明未知，然后把航道让出来。",
                "零：那份申请不会再被驳回。我仍会报告风浪；有人因此决定留下，也同样值得记录。",
                "七船不再整齐重演相同航路；留下与出发，都成为真实可选的行动。")
        };

        static Entry Get(int stage){return Entries[Mathf.Clamp(stage-1,0,8)];}
        public static string EvidenceTitle(int stage){return Get(stage).title;}
        public static string EvidenceText(int stage){return Get(stage).text;}
        public static string SecretQuestion(int stage){return Get(stage).question;}
        public static string[] SecretOptions(int stage){return (string[])Get(stage).options.Clone();}
        public static int SecretAnswer(int stage){return Get(stage).correct;}
        public static string SecretResponse(int stage,int choice)
        {return choice<0||choice>2?"先比较现场留下的文字，再作判断。":Get(stage).answers[choice];}
        public static string GuideResponse(int stage){return Get(stage).response;}
        public static string EvidencePayoff(int stage){return Get(stage).payoff;}

        public static bool IsCollected(RunData run,int stage)
        {
            if(run==null||stage<1||stage>9)return false;
            if(run.stage==stage)return (run.exploredMask&4)!=0;
            return run.islands!=null&&run.islands.Length>=stage&&run.islands[stage-1]!=null&&
                (run.islands[stage-1].explored&4)!=0;
        }
        public static List<CollectedEvidence> JournalEntries(RunData run)
        {
            var entries=new List<CollectedEvidence>();
            for(int i=1;i<=9;i++)if(IsCollected(run,i))
            {
                var e=Get(i);
                entries.Add(new CollectedEvidence{Stage=i,Title=e.title,Text=e.text,Payoff=e.payoff});
            }
            return entries;
        }
    }
}
