using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tidebreak
{
    public partial class GameDirector
    {
        VoyageCinematicPlayer cinematicPlayer;
        public bool CinematicActive { get { return cinematicPlayer && cinematicPlayer.Active; } }
        public int CinematicSequence { get { return CinematicActive ? cinematicPlayer.Sequence : -1; } }
        public float CinematicElapsed { get { return CinematicActive ? cinematicPlayer.Elapsed : 0; } }

        // 0 opening; 1..9 arrivals; 10/11 legendary beasts; 12 ending; 13/14 revelations.
        // A callback may open dialogue, start combat, or finish the voyage after camera restoration.
        public void PlayCinematic(int sequence, Action onComplete = null, bool force = false)
        {
            if (sequence < 0 || sequence > 14 || (!force &&
                (Automation || (Run.cinematicMask & (1 << sequence)) != 0)))
            {
                if (onComplete != null) onComplete();
                return;
            }
            if (!cinematicPlayer) cinematicPlayer = gameObject.AddComponent<VoyageCinematicPlayer>();
            cinematicPlayer.Play(this, sequence, onComplete);
        }

        public void SkipCinematic() { if (CinematicActive) cinematicPlayer.Complete(); }
        public void CancelCinematic() { if (CinematicActive) cinematicPlayer.Cancel(); }
        public void SeekCinematicForQA(float seconds) { if (CinematicActive) cinematicPlayer.Seek(seconds); }
    }

    // Uses the same world, characters and lighting as gameplay. No prerecorded or external CG.
    [DefaultExecutionOrder(800)]
    public sealed class VoyageCinematicPlayer : MonoBehaviour
    {
        sealed class Shot
        {
            public Vector3 from, to, lookFrom, lookTo;
            public float seconds, fov;
            public string speaker, line, document;
        }

        readonly List<Shot> shots = new List<Shot>();
        GameDirector game;
        Camera view;
        Action completed;
        VoyageState returnState;
        Vector3 oldLocalPosition;
        Quaternion oldLocalRotation;
        float oldFov, oldTimeScale, started, duration;
        bool oldPaused;
        string chapter;
        Transform preview, previewRig;
        readonly List<Transform> previewArms = new List<Transform>();
        readonly List<Quaternion> previewArmRotations = new List<Quaternion>();
        Transform previewFoam;
        Vector3 previewOrigin;
        Light portraitLight;
        GameObject guideSign;
        bool guideSignWasActive;
        IslandActorMotion actor;
        int lastShot = -1;

        public bool Active { get; private set; }
        public int Sequence { get; private set; }
        public float Elapsed { get { return Active ? Mathf.Clamp(Time.unscaledTime - started, 0, duration) : 0; } }

        public void Play(GameDirector director, int sequence, Action callback)
        {
            if (Active) Cancel();
            game = director; view = game.Player.View; Sequence = sequence; completed = callback;
            returnState = game.State; oldPaused = game.Paused; oldTimeScale = Time.timeScale;
            oldLocalPosition = view.transform.localPosition;
            oldLocalRotation = view.transform.localRotation; oldFov = view.fieldOfView;
            game.Paused = false; Time.timeScale = 1;
            game.SetState(VoyageState.Cinematic);
            Active = true; lastShot = -1;
            BuildSequence();
            started = Time.unscaledTime;
            actor = game.World.GetComponentInChildren<IslandActorMotion>();
            var sign = game.World.Scenery.Find(game.Island.npc);
            if (sign) { guideSign = sign.gameObject; guideSignWasActive = guideSign.activeSelf; guideSign.SetActive(false); }
            game.UI.BeginCinematicHUD();
            RenderAt(0);
        }

        void LateUpdate()
        {
            if (!Active) return;
            // A load, death, or an external state transition must never leave an orphaned camera.
            if (!game || !view || game.State != VoyageState.Cinematic) { Cancel(false); return; }
            if (!game.Automation && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Escape)))
            { Complete(); return; }
            float time = Time.unscaledTime - started;
            if (time >= duration) { Complete(); return; }
            RenderAt(time);
        }

        public void Seek(float seconds)
        {
            if (!Active) return;
            float time = Mathf.Clamp(seconds, 0, duration - .01f);
            started = Time.unscaledTime - time;
            RenderAt(time);
        }

        void RenderAt(float time)
        {
            if (shots.Count == 0) return;
            int index = 0;
            float local = time;
            while (index < shots.Count - 1 && local >= shots[index].seconds)
            { local -= shots[index].seconds; index++; }
            Shot shot = shots[index];
            float t = Mathf.Clamp01(local / shot.seconds);
            float eased = t * t * (3 - 2 * t);
            Vector3 position = Vector3.Lerp(shot.from, shot.to, eased);
            Vector3 look = Vector3.Lerp(shot.lookFrom, shot.lookTo, eased);
            view.transform.position = position;
            view.transform.rotation = Quaternion.LookRotation(look - position, Vector3.up);
            view.fieldOfView = shot.fov;
            float fade = Mathf.Max(1 - local / .35f, 1 - (shot.seconds - local) / .3f);
            game.UI.UpdateCinematicHUD(chapter, shot.speaker, shot.line, shot.document,
                time / duration, fade);
            if (actor) actor.Speaking = shot.speaker == game.Island.npc || Sequence == 14;
            if (index != lastShot)
            {
                lastShot = index;
                if (game.Audio && (Sequence == 10 || Sequence == 11) && index == 1) game.Audio.Cue("boss");
            }
            if (preview)
            {
                float reveal = Mathf.SmoothStep(0, 1, Mathf.Clamp01(time / 4));
                preview.position = previewOrigin + Vector3.up * Mathf.Lerp(-7, .15f, reveal);
                preview.rotation = Quaternion.Euler(Sequence == 11 ? Mathf.Lerp(-25, -3, reveal) : 0,
                    180 + Mathf.Sin(time * .3f) * 4, 0);
                if (previewRig) previewRig.localPosition = Vector3.up * Mathf.Sin(time * 1.3f) * .12f;
                for (int i = 0; i < previewArms.Count; i++)
                    previewArms[i].localRotation = previewArmRotations[i] * Quaternion.Euler(
                        Mathf.Sin(time * 1.2f - i * .7f) * 9 * reveal, 0,
                        Mathf.Cos(time * 1.1f - i * .7f) * 6 * reveal);
                if (previewFoam)
                {
                    previewFoam.position = new Vector3(previewOrigin.x, -.52f, previewOrigin.z);
                    previewFoam.localScale = Vector3.one * (1 + Mathf.Repeat(time * .15f, .45f));
                }
            }
        }

        public void Complete()
        {
            if (!Active) return;
            var callback = completed;
            if (game && game.Run != null) game.Run.cinematicMask |= 1 << Sequence;
            Restore(true);
            if (game && game.Run != null) game.Checkpoint(false);
            if (callback != null) callback();
        }

        public void Cancel() { Cancel(true); }
        void Cancel(bool restoreState) { if (Active) Restore(restoreState); }

        void Restore(bool restoreState)
        {
            Active = false; completed = null;
            if (view)
            {
                view.transform.localPosition = oldLocalPosition;
                view.transform.localRotation = oldLocalRotation; view.fieldOfView = oldFov;
            }
            if (actor) actor.Speaking = false;
            if (guideSign) guideSign.SetActive(guideSignWasActive);
            guideSign = null;
            if (preview) { preview.gameObject.SetActive(false); Destroy(preview.gameObject); }
            if (portraitLight) Destroy(portraitLight.gameObject);
            preview = previewRig = null; portraitLight = null;
            previewArms.Clear(); previewArmRotations.Clear(); previewFoam = null;
            if (!game) return;
            game.Paused = oldPaused; Time.timeScale = oldTimeScale;
            if (game.UI) game.UI.EndCinematicHUD();
            if (restoreState && game.State == VoyageState.Cinematic) game.SetState(returnState);
        }

        void OnDisable() { if (Active) Restore(false); }

        void Add(Vector3 from, Vector3 to, Vector3 look, float seconds, float fov,
            string speaker, string line, string document = "", Vector3? endLook = null)
        {
            shots.Add(new Shot { from = from, to = to, lookFrom = look, lookTo = endLook ?? look,
                seconds = seconds, fov = fov, speaker = speaker, line = line, document = document });
            duration += seconds;
        }

        Vector3 Land(float x, float z, float height)
        { return new Vector3(x, game.World.Height(x, z) + height, z); }

        void Landscape(float seconds, string speaker, string line)
        {
            Vector3 landmark = Land(0, -34, 4.5f);
            Add(new Vector3(34, 20, 28), new Vector3(24, 15, 18), landmark,
                seconds, 54, speaker, line);
        }

        void Landmark(float seconds, string speaker, string line, string document = "")
        {
            Vector3 target = Land(0, -34, game.World.Region == 0 ? 8 : 5);
            Add(Land(-12, -16, 11), Land(-9, -19, 9), target, seconds, 48,
                speaker, line, document);
        }

        void Portrait(float seconds, string speaker, string line, string document = "")
        {
            Vector3 target = game.World.QuestPoint;
            target.y = game.World.Height(target.x, target.z) + 1.7f;
            Add(target + new Vector3(1.35f, .12f, 4.6f), target + new Vector3(.9f, .08f, 3.8f),
                target - Vector3.up * .15f, seconds, 44, speaker, line, document);
            if (!portraitLight)
            {
                var light = new GameObject("Cinematic portrait fill");
                light.transform.position = target + new Vector3(-2, 2, 3);
                portraitLight = light.AddComponent<Light>(); portraitLight.color = new Color(1, .85f, .68f);
                portraitLight.intensity = .8f; portraitLight.range = 9;
            }
        }

        void BuildSequence()
        {
            shots.Clear(); duration = 0;
            if (Sequence == 0) { Opening(); return; }
            if (Sequence == 10 || Sequence == 11) { Legendary(); return; }
            if (Sequence == 12) { Ending(); return; }
            if (Sequence == 13) { BlackBox(); return; }
            if (Sequence == 14) { Confession(); return; }
            Arrival(Mathf.Clamp(Sequence, 1, 9));
        }

        void Opening()
        {
            chapter = "序章  /  灯熄之后";
            Add(new Vector3(-11, 2.5f, 37), new Vector3(-6, 3.1f, 28), Land(0, -34, 10),
                4.5f, 51, "航海日志 · 第零天", "风暴停了。海面上没有航标，也没有昨天留下的航迹。");
            Landmark(4.5f, "船长", "只有那座灯塔。我记得，离家时它还亮着。");
            Portrait(4.5f, game.Island.npc, "你的船第九次回到这里了。这一次，别只想着活着回来。");
            Landscape(4.5f, "航海日志", "修复航灯，走出九座岛组成的圆。把明天还给大海。");
        }

        void Arrival(int island)
        {
            var d = ExpeditionContent.Island(island);
            chapter = d.title + "  /  " + d.english;
            string[] first = {
                "灯塔的光没有照向海面。有人把透镜转向了岛内。",
                "潮水退去，整片珊瑚依然张着口。它们在等一个再也没有响起的音符。",
                "渔网挂在树梢，脚印却止于水底。这里的猎物学会了用根呼吸。",
                "沙下露出船锚。碑上刻着今天的日期，风化却已持续百年。",
                "船名不同，船钟却停在同一分钟。北星号的舷窗还亮着。",
                "冰里封住的不是尸骸，是一张张尚未褪色的船票。",
                "雷声有固定的间隔，像一台还在等待指令的机器。",
                "炉火从不熄灭。铸潮匠每天都在锻造同一把没有完成的钥匙。",
                "最后一座岛没有回声。每一步，却都比你的脚先响起。"
            };
            string[] npc = {
                "先把这盏灯修好。每一条新航线，都该有一个可以回来的地方。",
                "别急着开枪。听潮池的声音，错一个音，守礁者就会醒来。",
                "沼泽会记住你走过的路。打开净水阀，再去碰树屋里的东西。",
                "这不是藏宝图。这是一份让人忘记出口的说明书。",
                "黑匣子一直在叫你的名字。先别回答它。",
                "你认识这些船票上的字吗？最后一班船，从来没能开走。",
                "先让避雷针重新接地。等雷停了，我会把整件事告诉你。",
                "我要你带来的不是材料，是能承受代价的决心。",
                "欢迎回来，船长。这一次，你准备替谁选择明天？"
            };
            Landscape(4, "船长的观察", first[island - 1]);
            Landmark(4, "未绘之海 · " + island.ToString("00"), d.name + "\n" + d.title);
            Portrait(4, d.npc, npc[island - 1]);
        }

        void BlackBox()
        {
            chapter = "第五章 · 证物  /  第九次求救";
            Landmark(4, "北星号黑匣子 · 录音", "这里是北星号。不要把航灯点亮。它不是出口，是诱饵。");
            Portrait(4, game.Island.npc, "声音是你的。可这艘船沉没时，你还没出生。");
            Add(Land(-9, -18, 6), Land(-6, -22, 5), Land(0, -34, 5), 4, 43,
                "黑匣子 · 隐藏航次记录", "第八次尝试：为确保船员存活，归航协议已重置今日。",
                "北星号 · 最后一页\n\n如果你再次听见自己，\n请相信那个选择出发的人。\n\n不要替我保留今天。");
            Landscape(4, "船长", "我追的不是失踪的船。我在追一场被撤销了八次的告别。");
        }

        void Confession()
        {
            chapter = "第七章 · 口供  /  一个父亲的天气";
            Landmark(4, "赛因", "刚才是她的声音。露珂找了她那么久……你真的让她从冰里出来了。");
            Portrait(4, game.Island.npc, "可机器里还有别人的孩子。我救回了女儿，就能替他们按下关机吗？");
            Portrait(4, game.Island.npc, "我造它是为了救人。后来，我却连让他们自己出发都不敢了。",
                "未寄出的信\n\n爸，明天的海况很好。\n别再替我看天气了。\n等我回去，教你钓鱼。");
            Landscape(4, "赛因", "把钥匙带去火山吧。回家应该是每个人的选择，不是机器的命令。");
        }

        void Ending()
        {
            chapter = "终章  /  潮水终于向前";
            bool remember = game.Run.storyChoice != 0;
            Landmark(5, "潮汐记录者 · 零", remember ?
                "记忆已封存。它不会替你重来，但会记住所有曾经出发的人。" :
                "归航协议解除。没有人再被要求，永远平安地留在昨天。");
            Portrait(5, "船长", remember ?
                "留下他们的名字。让下一位走到这里的人，知道曾有人为明天付过代价。" :
                "熄灭这盏留住我们的灯吧。我们还有真正的灯塔需要回去。");
            Add(new Vector3(9, 4, 18), new Vector3(3, 3, 29), new Vector3(0, 1, 125), 5, 57,
                "归航电台 · 米罗", "听见了吗？潮水退了。村里的钟，终于走过了午夜。");
            Add(new Vector3(3, 3, 29), new Vector3(0, 5, 39), new Vector3(0, 3, 160), 5, 58,
                "航海日志 · 第一天", "今天没有保证。但有风，有海，有一条尚未走过的航线。",
                "致下一位船长\n\n海图到这里结束。\n航行从这里开始。");
        }

        void Legendary()
        {
            bool kraken = Sequence == 10;
            chapter = kraken ? "海洋禁录  /  无底之腕 · 克拉肯" : "海洋禁录  /  携冬者 · 白鲸";
            Vector3 target = new Vector3(0, 2.4f, 38);
            Enemy boss = null;
            foreach (var enemy in game.Enemies) if (enemy && enemy.IsBoss) { boss = enemy; break; }
            if (boss) target = boss.transform.position + Vector3.up * 2;
            else
            {
                preview = new GameObject(kraken ? "Kraken cinematic apparition" : "White whale cinematic apparition").transform;
                previewOrigin = new Vector3(0, kraken ? .7f : 1.3f, 38);
                preview.position = previewOrigin;
                previewRig = SpeciesArt.Build(preview, ExpeditionContent.Species[kraken ? 117 : 118], false);
                foreach (var collider in preview.GetComponentsInChildren<Collider>()) collider.enabled = false;
                foreach (var joint in previewRig.GetComponentsInChildren<Transform>())
                    if (joint.name.StartsWith("Tentacle ") || joint.name == "Fin membrane")
                    { previewArms.Add(joint); previewArmRotations.Add(joint.localRotation); }
                previewFoam = CoastalMesh.Ring(preview, Vector3.zero, kraken ? 8 : 6, .065f,
                    new Color(.64f, .88f, .87f), Quaternion.Euler(90, 0, 0));
            }
            Add(target + new Vector3(-17, 2.5f, -20), target + new Vector3(-13, 3, -15), target,
                4, 52, "船长", kraken ? "海面不再起伏。不是风停了，是整片海正在屏住呼吸。" : "风雪逆着海流移动。那座白色的山，正在向我们转身。");
            Add(target + new Vector3(12, 4, -13), target + new Vector3(8, 3, -11), target,
                4, 48, kraken ? "旧航海禁令 · 第十三条" : "极地口述史", kraken ?
                "数清露出海面的手。它真正要用的那一只，永远还在水下。" :
                "它会带着整个冬天潜下去。海面再次开裂前，不要相信你站的地方。");
            Add(target + new Vector3(0, 8, -19), target + new Vector3(0, 5, -16), target,
                4, 56, "猎潮令", kraken ? "斩断握住航道的腕足，再向无底之眼开火。" : "听回声，看冰裂。活过浮上海面的第一口呼吸，才有猎杀的机会。");
        }
    }
}
