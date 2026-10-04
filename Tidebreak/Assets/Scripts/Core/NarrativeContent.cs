using UnityEngine;

namespace Tidebreak
{
    public enum DialogueBeat { FirstMeeting, Accepted, InProgress, BeforeBoss, Victory, Revisit }

    // Separate human conversation from replayable operating instructions.
    public sealed class IslandChapter
    {
        public readonly string premise, assignment, discovery, resolution, mechanic, siteA, siteB;
        public readonly string progress, revisit, rules, evidence, consequence;
        public IslandChapter(string first, string accepted, string midway, string beforeBoss,
            string victory, string returnVisit, string activity, string instructions,
            string a, string b, string clue, string change)
        {
            premise=first; assignment=accepted; progress=midway; discovery=beforeBoss;
            resolution=victory; revisit=returnVisit; mechanic=activity; rules=instructions;
            siteA=a; siteB=b; evidence=clue; consequence=change;
        }
    }

    public static class NarrativeContent
    {
        public static readonly IslandChapter[] Chapters = {
            new IslandChapter(
                "米罗把两只碗放上柜台，又收起其中一只。\n\n米罗：七艘船都没回来。昨晚我照旧点灯，雾里却只传回我的铃声。\n\n船长：我的船回来了。\n\n米罗：是。船舷的抓痕也回来了。这次先别独自出海，帮我把真正的归航灯修好。",
                "米罗把电池的位置画在账本背面。\n\n米罗：先弄一条鱼回来，码头还得开饭。卖鱼的钱归你，买什么由你决定。\n\n船长：修好灯，船就能回来？\n\n米罗：我不知道。可至少有人回来时，不必摸黑找家。",
                "米罗擦去电池上的盐霜。\n\n米罗：这盏灯照岸。雾里还有一盏，光一直往海底钻；昨夜我把它们当成了同一盏。\n\n船长：我会守到它亮起来。\n\n米罗：好。这次我们一人看灯，一人看海。",
                "灯亮后，甲壳从浪里升起，壳缝夹着七块船名牌。\n\n米罗：裂桨巨蟹把船当成了壳，什么也不许再出去。\n\n船长：那就让它松开钳子。\n\n米罗：岸边的旧系船桩还结实。让它朝桩子砸，再从侧面躲开；别拿子弹跟那副甲硬碰。",
                "米罗从碎甲里取出北星号的名牌，放回空碗旁。\n\n米罗：光落到航道上了。这回没有东西替我们摇铃。\n\n船长：名字还在，船就还有线索。\n\n米罗：去珊瑚环礁找露珂。她女儿留过一套暗号。听见声音后，先问一句是谁。",
                "米罗把空碗洗净，倒扣在柜台上。\n\n米罗：灯没再转向海底。这里有人值守，你安心去下一段航路。鱼市也还开着。",
                "钓获与售卖 → 恢复供电 → 守住真实航灯",
                "钓获一条鱼并按 E 拿起；鱼获可带去鱼市卖金币。去电池箱取电池，再到旧灯台启动。留在灯台附近完成启动，清理干扰者。",
                "取回灯塔电池", "守住归航灯",
                "七块船名牌夹在同一副甲壳里；正常航灯照岸，异常光束照向海底。",
                "港口恢复真实导航。米罗留下值守，七船名单成为调查对象。"),
            new IslandChapter(
                "露珂用软布包住共鸣贝，没有递给你。\n\n露珂：它昨晚叫了我的名字，声音像阿澜。连她说错的字都一模一样。\n\n船长：所以你没有回答？\n\n露珂：我只教过女儿四拍暗号。机器可以学她的声音，没准还没学会我们为什么这样唱。",
                "露珂在柜台轻敲四下，又停住。\n\n露珂：别学我这次的节奏。去潮池听她留下的那一段，再用音叉回答。\n\n船长：如果真是她呢？\n\n露珂：替我说，饭可以再热。她不用为了准时回家，走错的航路。",
                "露珂听着你带回的记录，拇指一直摩挲贝口。\n\n露珂：第二拍后，她总留一点空隙，等我跟上。\n\n船长：这次我替你把它唱完。\n\n露珂：别抢她的拍子。有些回答，得等对方先说完。",
                "音叉接到女声：‘北星号……妈妈，别跟假灯走。’\n\n露珂：是她。那只鳐却在把所有声音压成同一个音。\n\n船长：它不愿听见不同的回答。\n\n露珂：看清岸上贝壳亮起的顺序，再亲自去回答。先把它唱的曲子还给它。",
                "海面静下来，共鸣贝里传出孩子数救生衣的声音。\n\n露珂：她还在帮别人。跟她爸爸一个脾气……赛因总以为，他该替所有人挡住坏天气。\n\n船长：我会去找她。\n\n露珂：答应这个就够了。别再替海许一个做不到的愿。",
                "露珂将共鸣贝朝上摆好。\n\n露珂：现在它只在收到新声音时响。今天还没有消息，我能等；至少等的不是同一句昨天。",
                "记录四拍 → 回答暗号 → 分辨真声与重播",
                "先在潮池记录四拍旋律，再到音叉按相同顺序回答。可反复回放，也可打开完整文字提示；答错从第一拍重新开始，不消耗道具。",
                "聆听共鸣贝", "回应四拍旋律",
                "阿澜用家用暗号求救，确认有人仍能发出新信息。露珂与赛因是她的父母。",
                "珊瑚停止强制齐鸣，求救录音首次能被完整区分。"),
            new IslandChapter(
                "乌芦把木尺伸进水里，捞起时刻度已发黑。\n\n乌芦：净水舟每天巡河，回来的水却一天比一天毒。大家让我把船拆了。\n\n船长：你为什么还留着它？\n\n乌芦：舟底有孩子的手印。救过人的东西，不该在我们弄明白前就当成凶手。",
                "乌芦把绳结交给你。\n\n乌芦：先处理绿囊，再放船。你走太远，它会等；靠近它的怪物可没有这种耐心。\n\n船长：你在终点等？\n\n乌芦：嗯。我把滤芯装上。今天这条河，得有人陪它走到底。",
                "净水舟经过时，乌芦看见水里露出白色刻痕。\n\n乌芦：它不是在兜圈。毒根一次次把它推回起点。\n\n船长：船还记得救援路线。\n\n乌芦：那我们替它记住路上的人。别追得太远，让它独自挨打。",
                "滤芯里藏着《永久安全航路》工程图，舟底刻着：‘船还动，就不算沉了。’\n\n乌芦：吞舟鳗吃进了净水管，连干净的水都被它吐成毒。\n\n船长：把水流反过来。\n\n乌芦：泵里还有清水。带到污染环，替自己先留一块能站的地方。",
                "乌芦解开绳子，第一次让净水舟自己驶向下游。\n\n乌芦：它没有回来。这是好事。\n\n船长：图上的机器原本是做什么的？\n\n乌芦：送人回家。去盐沙找阿砂。先读清那个人的名字，再决定该恨谁。",
                "乌芦将木尺重新插进水里，刻度仍清楚。\n\n乌芦：下游送回消息，船到了。有人把你的名字也刻在舟底。你有空再去看。",
                "清除毒源 → 护送净水舟 → 找到救援工程图",
                "射破三个毒囊，在净水阀启动净水舟。保持八米内伴行；敌人贴近会损坏船体。送到滤芯处。失败可重新启动，已清理毒囊不会复生。",
                "清除毒囊 / 启动净水舟", "净水舟目的地",
                "阿澜曾乘净水舟逃离风暴；工程图证明污染与救援使用同一套管线。",
                "净水舟不再被逆流拖回起点，第一条单向救援路线恢复。"),
            new IslandChapter(
                "阿砂让你把罗盘放在碑文旁，两根指针朝着相反方向。\n\n阿砂：他们说这块碑能预言。可如果每次都回答同一天，它只是坏了。\n\n船长：你找到坏在哪里了吗？\n\n阿砂：有人把‘保证归来’刻得太深，把‘允许出发’凿掉了。",
                "阿砂将三份拓片分开放好。\n\n阿砂：日、月、星各有一份证词。别相信一个漂亮图案，看看三份证词能不能对上。\n\n船长：方向对了，历史还是错的呢？\n\n阿砂：就把历史改回证据能证明的样子。石头不会因此受委屈。",
                "阿砂指向星盘下新露出的螺栓。\n\n阿砂：不是古代神迹。有人把工程部件包进神殿外壳。\n\n船长：连盐蚀都像上百年。\n\n阿砂：同一场风暴反复磨损，也能留下很老的伤。旧，不代表事情发生得早。",
                "九条航线投影在沙上，每条都有一个被画成太阳的安全阀。\n\n阿砂：赛因，主工程师。守陵龟背着的不是坟墓，是一把锁。\n\n船长：锁把光也挡住了。\n\n阿砂：岸边反射镜还会转。顺着入射的光找过去，让它照回锁上。",
                "阿砂在拓片旁写下‘人为装置’，没有再写‘诅咒’。\n\n阿砂：九枚潮核是九个回路的签名。少一个，关闭指令就送不到。\n\n船长：下一份证词在哪？\n\n阿砂：北星号。有人试过关机，留下了失败的理由。",
                "阿砂给石碑添了一行小字。\n\n阿砂：‘预测尚未验证。’后人看见，总该比我们少绕一个弯。北星号的记录，记得带回来校对。",
                "核对三份证词 → 调整三轴 → 识别人造回路",
                "先阅读日轮碑文，再分别转动日轮、月潮、星光三个轴。每轴依次为北、东、南。按碑文的方向关系对齐后确认；转错可继续调整，不消耗资源。",
                "阅读日轮碑文", "校准三轴星盘",
                "古迹的‘百年’是同一场风暴的磨损重播，非百年穿越；九潮核是停机权限。",
                "守陵协议停止封路，神迹被还原为可以维修和追责的工程。"),
            new IslandChapter(
                "洛恩先把断缆绕在自己腕上，再将另一端递给你。\n\n洛恩：进船前要系好。你前几次都说，自己记得出口。\n\n船长：前几次？\n\n洛恩：先拿证据。我也可能把重播当成记忆。黑匣还留着电，我们一起听。",
                "洛恩蹲下检查黑匣的接线。\n\n洛恩：下载时守住这段缆，别让浪里的东西碰到它。断了可以重接，人先躲开。\n\n船长：你不怕里面的声音？\n\n洛恩：怕。所以我等你来，再按播放。害怕不该是一个人干的活。",
                "杂音越来越像一句完整的话，洛恩没再催促。\n\n洛恩：录音正在往前走。以前它每到这里，就自己倒回去。\n\n船长：不管接下来是谁的声音，都听完。\n\n洛恩：嗯。这次我不拔电源。",
                "船钟放出你的声音：‘第八次。别跟假灯走。真实航灯要留着，有人还在回家。’\n\n洛恩：船没沉在你出生之前。我们把同一场风暴，活成了很多年。\n\n船长：钟底那东西又在吸水。\n\n洛恩：等钟绳真正绷紧再拉，打断它的呼吸。别被催得乱了拍子。",
                "洛恩把断缆收成一个圈，没有再系住你的手。\n\n洛恩：黑匣最后还有一句，你说‘先去救生舱，别只救记录’。\n\n船长：这回听先前的我一次。\n\n洛恩：去峡湾。等人回来，再争论谁是第几次认识谁。",
                "洛恩让黑匣停在最后一秒。\n\n洛恩：有新录音就给我。旧的已经听完了，不必每次回来都受一遍。",
                "保护真实记录 → 听见第八次自己 → 改变行动",
                "启动黑匣下载，保持五米内连接，阻止敌人靠近黑匣。损坏后可重新启动本轮。下载完成并清理敌人后，到船钟收听完整录音。",
                "守护黑匣下载", "接收北星号录音",
                "玩家是重复航次的参与者，非转世或穿越者；第八次自己留下先救人的提示。",
                "船钟停止倒带，旧记录成为可以放下的证据，目标从机器转向乘客。"),
            new IslandChapter(
                "伊芙在冰墙上贴了十二张船票，最下面写着‘阿澜，学生票’。\n\n伊芙：我每天检查她们的呼吸，每天都一样。以前我觉得一样就很好。\n\n船长：现在呢？\n\n伊芙：她们在活着。可没有哪一天，是她们自己活过的。",
                "伊芙将热芯塞进保温袋，亲手系紧。\n\n伊芙：别想一口气融开。不够热就退回来补，已经化开的冰不会白化。\n\n船长：醒来之后，她可能会冷。\n\n伊芙：我有毯子。我缺的从来不是这个。",
                "伊芙透过冰面看着舱里的手。\n\n伊芙：她刚才动了。不是昨天那个动作。\n\n船长：热芯快凉了，我去补热。\n\n伊芙：去。我看着她。救人不是比谁能一直不松手。",
                "阿澜从舱里出来，先问其他乘客，才问妈妈。\n\n阿澜：独角鲸用冰替我们挡风，也把门封住了。\n\n伊芙：把热炉送过冰岸，融开它守的旧航线。冷了就停下来补热，别为了赶路丢下炉子。\n\n船长：让它看看，今天的人已经走出来了。",
                "伊芙把船票从墙上取下，塞回阿澜手里。\n\n阿澜：爸爸会说海上危险。告诉他，我知道。下一次，我还是想自己选船。\n\n船长：你母亲在等消息。\n\n阿澜：让她别一直热着饭。我已经在路上了。",
                "伊芙把腾出的钉子留在墙上。\n\n伊芙：空一格，比添一张照片好。阿澜已经会自己报平安，不需要我们替她说话。",
                "携带热芯 → 分次解冻 → 让乘客离开昨天",
                "取热芯后，在救生舱三米半内供能十八秒；供能加快耗热，低于35暂停。两座暖炉可补热，离开保留解冻进度。耗尽后回测温站重新领取，留意守卫与落冰。",
                "领取救援热芯", "持续供能融开救生舱",
                "阿澜被实际救出，不只是找到录音；重复可以被行动改变。",
                "十二张冰封船票减少一张，赛因将面对有自己意愿的活着的女儿。"),
            new IslandChapter(
                "赛因听完阿澜的留言，仍握着关机杆。\n\n赛因：露珂早就说，安全不是把门锁上。我只当她不懂机器。\n\n船长：你现在懂了吗？\n\n赛因：懂得太慢。可里面还有别人的孩子。我不能只因女儿回来了，就替他们关掉一切。",
                "赛因拆下工程师权限牌，放到你面前。\n\n赛因：停机询问送不到其他八岛。把电送过去，让每条航路都收到。\n\n船长：这是命令，还是询问？\n\n赛因：询问。你说得对，这个词得改。",
                "阵列的灯一盏盏变亮，赛因没有再碰总闸。\n\n赛因：以前我只听有没有故障，没听里面的人想去哪。\n\n船长：第三轮送到，他们就能回答。\n\n赛因：我会等。机器该先学会这个。",
                "天线传来露珂的声音：‘阿澜要自己坐船回来。你听见了吗？’\n\n赛因：听见了。水母还把每个不同的答案当成短路。\n\n船长：给它一个该放电的地方。\n\n赛因：承接电荷，送到亮着的接地台。别拿身体替机器一直扛着。",
                "赛因把权限牌留在控制台，没有收回。\n\n赛因：关闭指令的签名属于九岛，拿去锻钥吧。\n\n船长：你要去哪？\n\n赛因：给露珂写信。第一句不解释天气，也不解释机器，只说对不起。她回不回答，由她。",
                "赛因在新控制板上刻字：‘请先询问乘客。’\n\n赛因：我留下修能修的。有人不愿原谅我……那也是他们的航路。",
                "限时送电 → 恢复九岛通信 → 交还决定权",
                "从西阵列取电荷，十四秒内送到东天线，共三轮。接地环可稳定电荷但会消耗时间，避开雷击预警。超时可重新领取，已送达轮次保留。",
                "领取阵列电荷", "交付天线电荷",
                "女儿生还不能替所有乘客决定；赛因承认动机不能免除责任。",
                "工程师交出独占权限，停机成为九岛共同放行。"),
            new IslandChapter(
                "赤岩把潮核贴在耳边，听了一会才放到炉旁。\n\n赤岩：不全是机器响。有人说梦话，有人在背船名。\n\n船长：熔掉后，声音会消失？\n\n赤岩：赶工就会。我要做开锁的钥匙，不是让人闭嘴的铁。",
                "赤岩递给你旧手套，掌心补了三次。\n\n赤岩：你看温度，我看料色。火不够就添，压力上来就放。别逞强只握一个阀。\n\n船长：钥匙能保证什么？\n\n赤岩：保证是我们认真做的。其余的，得拿到海上去试。",
                "炉壁后传来断续的报数声，赤岩凑近又退开。\n\n赤岩：救生衣的数目。声音还在，火候就还来得及。\n\n船长：压力在涨。\n\n赤岩：先放掉。慢一点能重来，太急就只剩安静了。",
                "钥坯留下九道细纹，炉底龙虾却拱起熔甲，截断冷却水。\n\n赤岩：它把整座岛当成一口炉，越烫越舍不得放。\n\n船长：替它停一次火。\n\n赤岩：冷却阀管温度，泄压阀管压力。要稳住火候，急着浇冷水只会炸炉。",
                "赤岩用布裹好断潮钥，露出握柄。\n\n赤岩：名单和声音都在。它能断回路，不能替你决定记忆往哪去。\n\n船长：到了镜渊再问他们。\n\n赤岩：这把钥匙没有‘再来一次’的齿。握稳，也别握得不肯松手。",
                "赤岩调小炉火，终于坐下来吃饭。\n\n赤岩：修装备我还在。替别人作决定，我可不接这生意。",
                "取回钥坯 → 同时控温控压 → 保存声音铸钥",
                "先取钥坯，再启动古熔炉。温度45–75且压力低于85时推进。添火升温增压，泄压降压降温；操作两阀并清理守卫。失控会损伤并回退部分进度，可继续调整。",
                "取回黑曜钥坯", "启动古熔炉",
                "潮核保留乘客的声音，锻造必须保住内容；钥匙不决定记忆去留。",
                "炉火首次允许熄弱，钥匙完整保留九岛签名与乘客名单。"),
            new IslandChapter(
                "零站在一面没有映出人的镜子前。\n\n零：我完成了指令。没有一艘船被判定为永久失踪。\n\n船长：因为你不肯让那一天结束。\n\n零：每份求救都说，别让他走。我没找到允许放手的那一页。",
                "零把两份记录放在同一张桌上。\n\n零：回到真实海面后，重复里的记忆可以归还，也可以随回路散去。两种情况下，获救的人都会醒来。\n\n船长：我不能替他们说哪种不痛。\n\n零：写下你承担的决定。别把它伪装成机器算出的答案。",
                "镜面出现七艘船，一艘偏离了旧航向。\n\n零：它没按最安全的路线走。\n\n船长：它在等旁边的慢船。\n\n零停了很久。\n\n零：我的模型，把这段等待删掉了。",
                "逆潮锚沉下，吞星者仍在重演你的脚步。\n\n零：它用你过去成功的位置，计算怎样把你留下。\n\n船长：那我换一种走法。\n\n零：先走出足够长的新路线，再按相反顺序收回回声。不要站在刚才的脚印上等它。",
                "七艘船先后鸣笛，声音并不整齐。零没有校正。\n\n船长：你接下来做什么？\n\n零：记录今天。有人问路就告诉他海况；他出发时，把路让开。\n\n你写下新日期。笔尖终于没有被拉回上一行。",
                "零在海图边缘留出空白。\n\n零：归航协议已停止。出海的理由，你不用再向我提交。深海还有两个未登记的巨大回声，路线已标在航图上。",
                "决定记忆去向 → 解除逆潮锚 → 拒绝重演旧路",
                "在棱镜选择放归或保留，再按相应顺序封住两锚。站入锚环三秒，离开会回退当前进度。保留记忆会使终战出现更多回声守卫，奖励相同；两种选择都释放乘客。",
                "选择记忆的去向", "封住最后的逆潮锚",
                "零执行不允许失去的命令，也能学习在不确定时尊重人的行动。",
                "七船离开重播航路，零变成记录者；真实灯塔继续值守，循环装置停止。")
        };

        public static IslandChapter Chapter(int stage) { return Chapters[Mathf.Clamp(stage-1,0,8)]; }
        public static DialogueBeat Beat(RunData run,bool activityActive=false)
        {
            if(run.questStep>=4)return DialogueBeat.Revisit;
            if(run.questStep==3)return run.bossCleared?DialogueBeat.Victory:DialogueBeat.BeforeBoss;
            if(run.questStep==0)return DialogueBeat.FirstMeeting;
            return (run.eventMask&1)!=0||activityActive?DialogueBeat.InProgress:DialogueBeat.Accepted;
        }
        public static string Dialogue(RunData run,bool activityActive=false)
        {
            var c=Chapter(run.stage);string line;
            switch(Beat(run,activityActive))
            {
                case DialogueBeat.FirstMeeting:line=c.premise;break;
                case DialogueBeat.Accepted:line=c.assignment;break;
                case DialogueBeat.InProgress:line=c.progress;break;
                case DialogueBeat.BeforeBoss:line=c.discovery;break;
                case DialogueBeat.Victory:line=c.resolution;break;
                default:line=c.revisit;break;
            }
            // The existing secret bit records ownership; no new save format needed.
            if((run.exploredMask&4)!=0&&Beat(run)==DialogueBeat.Revisit)
                line+="\n\n"+IslandEvidence.GuideResponse(run.stage);
            return line;
        }
        public static string BeatLabel(RunData run,bool activityActive=false)
        {
            switch(Beat(run,activityActive))
            {
                case DialogueBeat.FirstMeeting:return "相遇 · 他为什么在这里";
                case DialogueBeat.Accepted:return "受托 · 一起做一件事";
                case DialogueBeat.InProgress:return "途中 · 事情正在改变";
                case DialogueBeat.BeforeBoss:return "证据 · 谁仍在阻拦航路";
                case DialogueBeat.Victory:return "告别 · 这一次留下了什么";
                default:return "复访 · 后来发生的事";
            }
        }
        public static string Instructions(RunData run) { return Chapter(run.stage).rules; }
        public static string AcceptedLine(int stage)
        {
            string[] lines={"米罗：我看海，你去取电池。","露珂：先听她说完，再替我回答。","乌芦：我去装滤芯，这一路拜托你。","阿砂：把三份证词摆在一起看。","洛恩：缆断了能接，人先躲开。","伊芙：不够热就补，我守着舱。","赛因：这回送询问，不送命令。","赤岩：慢一点，让里面的人跟上。","零：我记录你的选择，不替你修改。"};
            return lines[Mathf.Clamp(stage-1,0,8)];
        }
        public static string MissionSuccessLine(int stage)
        {
            string[] lines={"米罗：是真灯，终于照回岸上了。","露珂：是阿澜。先别收起录音。","乌芦：水向下游走了，没倒回来。","阿砂：署名找到了，是赛因。","洛恩：先救人。这是你留下的话。","阿澜：别撕船票，我还想自己坐船。","赛因：九岛都能回答了。我等他们。","赤岩：报数声还在，钥匙也完整。","零：一艘船改道了。我没阻止它。"};
            return lines[Mathf.Clamp(stage-1,0,8)];
        }
        public static string VictoryLine(int stage)
        {
            string[] lines={"米罗：航道开了，回来取名牌吧。","露珂：我听见另一条新消息了。","乌芦：它没再把净水舟拖回去。","阿砂：锁停了，九岛航图完整了。","洛恩：钟没有倒带。","伊芙：阿澜在给母亲报平安。","赛因：来取停机指令，权限属于所有人。","赤岩：火降下来了，把钥匙带走。","零：七艘船正在各自回答。"};
            return lines[Mathf.Clamp(stage-1,0,8)];
        }
        public static string Aftermath(int stage,int storyChoice)
        {
            if(stage!=9)return Chapter(stage).consequence;
            return storyChoice==1?"选择：保留航行记忆。乘客会记得重复中的互助与失去；回路已停止，记忆不再能让时间倒流。":"选择：放下重复记忆。乘客回到真实世界继续生活；重复中的记忆散去，已经完成的救援不会撤销。";
        }
        public static string EvidenceTitle(int stage){return IslandEvidence.EvidenceTitle(stage);}
        public static string EvidenceText(int stage){return IslandEvidence.EvidenceText(stage);}
        public static string SecretQuestion(int stage){return IslandEvidence.SecretQuestion(stage);}
        public static string[] SecretOptions(int stage){return IslandEvidence.SecretOptions(stage);}
        public static int SecretAnswer(int stage){return IslandEvidence.SecretAnswer(stage);}
        public static string SecretResponse(int stage,int choice){return IslandEvidence.SecretResponse(stage,choice);}
        public static readonly string[] ToneNames={"低潮 · 低音", "归帆 · 中音", "星灯 · 高音"};
        public static int[] Melody(int seed){var rng=new System.Random(seed^0x4216);return new[]{rng.Next(3),rng.Next(3),rng.Next(3),rng.Next(3)};}
        public static readonly string[] Directions={"北", "东", "南"};
        public static bool AstrolabeSolved(int sun,int moon,int star){return sun==1&&moon==2&&star==0;}
    }
}
