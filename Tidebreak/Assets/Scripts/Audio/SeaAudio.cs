using System.Collections.Generic;
using UnityEngine;

namespace Tidebreak
{
    // Original PCM is composed offline; runtime only selects tracks and mixes envelopes.
    public class SeaAudio : MonoBehaviour
    {
        static readonly string[] CueNames={"shot","shotgun","harpoon","carbine","burst","arc","reload","ready","dash","hurt","splash","coin","select","boss","cast","bite","win","lose","step","reel","catch","impact","weak","discovery","heal","sonar","beam","explosion","freeze","equip","steam","danger","heartbeat"};
        readonly Dictionary<string,AudioClip> clips=new Dictionary<string,AudioClip>();
        readonly Dictionary<string,AudioClip> themes=new Dictionary<string,AudioClip>();
        readonly Dictionary<string,float> lastCue=new Dictionary<string,float>();
        readonly List<AudioClip> fallbackClips=new List<AudioClip>();
        readonly System.Random pitchRng=new System.Random(5051);
        readonly AudioSource[] music=new AudioSource[2],percussion=new AudioSource[2],effects=new AudioSource[12];
        readonly AudioSource[] tells=new AudioSource[4];int nextTell;float tellAt;
        readonly float[] blend=new float[2],effectGains=new float[12];
        AudioSource ambience,priority,mechanismTone;
        int activeBank,nextEffect;
        float duckUntil,duckGain=1,smoothDuck=1,heartbeatAt,dangerAt,switchAfter;
        bool combatRequested,initialized;
        string profile="";
        GameDirector game;
        public string CurrentTheme {get{return profile;}}
        public float MusicDuck {get{return smoothDuck;}}
        public float PercussionIntensity {get;private set;}
        public int LoadedCueCount {get{return clips.Count;}}
        public int LoadedThemeCount {get{return themes.Count;}}
        public int MissingImportedCueCount {get{return fallbackClips.Count;}}

        public void Init()
        {
            if(initialized)return;initialized=true;game=GameDirector.Instance;
            for(int i=0;i<2;i++){music[i]=Source("Music crossfade "+i,true,170);percussion[i]=Source("Boss percussion "+i,true,160);}
            for(int i=0;i<effects.Length;i++)effects[i]=Source("Action voice "+i,false,100);
            for(int i=0;i<tells.Length;i++){tells[i]=Source("Spatial enemy warning "+i,false,55);tells[i].spatialBlend=.8f;tells[i].rolloffMode=AudioRolloffMode.Linear;tells[i].minDistance=4;tells[i].maxDistance=35;}
            priority=Source("Critical feedback",false,40);mechanismTone=Source("Mechanism musical language",false,32);
            ambience=Source("Coastal atmosphere",true,220);ambience.clip=Resources.Load<AudioClip>("Audio/Ambience/coastal_air");if(ambience.clip)ambience.Play();
            foreach(string name in CueNames){var clip=Resources.Load<AudioClip>("Audio/Effects/"+name);clips[name]=clip?clip:Fallback(name);}
            foreach(string name in new[]{"explore_act1","explore_act2","explore_act3","combat","boss","legend","boss_drums","legend_drums"}){var clip=Resources.Load<AudioClip>("Audio/Music/"+name);if(clip)themes[name]=clip;}
            SelectTheme("explore_act1",true);
        }
        AudioSource Source(string label,bool loop,int sourcePriority)
        {
            var child=new GameObject(label);child.transform.SetParent(transform,false);var source=child.AddComponent<AudioSource>();
            source.playOnAwake=false;source.loop=loop;source.spatialBlend=0;source.priority=sourcePriority;source.volume=0;source.dopplerLevel=0;return source;
        }
        void Update()
        {
            if(!initialized||!game||game.Log==null)return;
            float dt=Mathf.Min(.1f,Time.unscaledDeltaTime);string desired=DesiredTheme();
            if(desired!=profile&&Time.unscaledTime>=switchAfter)SelectTheme(desired,false);
            bool talking=game.CinematicActive||game.State==VoyageState.Dialogue;
            var mechanism=game.ActiveBossMechanism;bool listening=mechanism&&mechanism.Active&&(mechanism.BossIndex==1||mechanism.BossIndex==4);
            float wantedDuck=talking?.22f:listening?.35f:1;
            if(Time.unscaledTime<duckUntil)wantedDuck=Mathf.Min(wantedDuck,duckGain);else duckGain=1;
            if(game.Paused)wantedDuck*=.55f;
            smoothDuck=Mathf.MoveTowards(smoothDuck,wantedDuck,dt*(wantedDuck<smoothDuck?5:1.25f));
            int phase=mechanism?mechanism.Phase:1;float layer=profile=="boss"||profile=="legend"?phase==3?1:phase==2?.68f:.38f:0;
            PercussionIntensity=Mathf.MoveTowards(PercussionIntensity,layer,dt*.7f);
            float musicGain=Mathf.Clamp01(game.Log.musicVolume)*.43f*smoothDuck;if(game.State==VoyageState.Defeat)musicGain*=.3f;
            for(int i=0;i<2;i++){
                blend[i]=Mathf.MoveTowards(blend[i],i==activeBank?1:0,dt/1.6f);
                float envelope=Mathf.Sqrt(blend[i]);music[i].volume=musicGain*envelope;percussion[i].volume=musicGain*envelope*PercussionIntensity*.7f;
                if(i!=activeBank&&blend[i]<=0){if(music[i].isPlaying)music[i].Stop();if(percussion[i].isPlaying)percussion[i].Stop();}
            }
            float effectGain=Mathf.Clamp01(game.Log.effectsVolume);priority.volume=effectGain;mechanismTone.volume=effectGain;
            for(int i=0;i<effects.Length;i++)effects[i].volume=effectGain*effectGains[i];
            ambience.volume=Mathf.Clamp01(game.Log.ambienceVolume)*(game.State==VoyageState.Combat?.105f:.19f)*(talking?.65f:1)*(game.Paused?.6f:1);
            bool danger=game.IsPlaying&&!game.Paused&&game.Run.health>0&&game.Run.health<game.Run.MaxHealth*.28f;
            if(danger&&Time.unscaledTime>=heartbeatAt){heartbeatAt=Time.unscaledTime+1.25f;Critical("heartbeat",.2f);}
            if(danger&&Time.unscaledTime>=dangerAt){dangerAt=Time.unscaledTime+8;Critical("danger",.3f);DuckMusic(.6f,.65f);}
        }
        string DesiredTheme()
        {
            if(game.State==VoyageState.Combat){foreach(var enemy in game.Enemies)if(enemy&&enemy.IsBoss)return enemy.Spec.id>=117?"legend":"boss";return "combat";}
            if(combatRequested&&game.State==VoyageState.Fishing)return "combat";
            return "explore_act"+(Mathf.Clamp((game.Run.stage-1)/3,0,2)+1);
        }
        void SelectTheme(string name,bool immediate)
        {
            AudioClip clip;if(!themes.TryGetValue(name,out clip)||!clip)return;
            int incoming=immediate?0:1-activeBank;music[incoming].Stop();percussion[incoming].Stop();music[incoming].clip=clip;music[incoming].pitch=1;
            AudioClip drums;percussion[incoming].clip=themes.TryGetValue(name+"_drums",out drums)?drums:null;
            double start=AudioSettings.dspTime+.12;music[incoming].PlayScheduled(start);if(percussion[incoming].clip)percussion[incoming].PlayScheduled(start);
            blend[incoming]=immediate?1:0;activeBank=incoming;profile=name;switchAfter=Time.unscaledTime+.35f;
        }
        public void SetCombat(bool combat){combatRequested=combat;}
        public void ThreatTell(Vector3 position,bool elite)
        {
            if(!initialized||Time.unscaledTime<tellAt)return;tellAt=Time.unscaledTime+.3f;
            AudioClip clip;if(!clips.TryGetValue("danger",out clip)||!clip)return;
            var source=tells[nextTell++%tells.Length];source.transform.position=position;source.clip=clip;
            source.pitch=elite?1.13f:1.38f;source.volume=Mathf.Clamp01(game.Log.effectsVolume)*(elite?.24f:.14f);source.Play();
        }
        public void DuckMusic(float seconds,float amount=.25f)
        {
            if(Time.unscaledTime>=duckUntil)duckGain=1;duckUntil=Mathf.Max(duckUntil,Time.unscaledTime+Mathf.Max(0,seconds));duckGain=Mathf.Min(duckGain,Mathf.Clamp01(amount));
        }
        public void PlayMechanismTone(AudioClip clip,float volume=.3f)
        {
            if(!clip||!mechanismTone)return;mechanismTone.volume=game&&game.Log!=null?Mathf.Clamp01(game.Log.effectsVolume):.85f;
            mechanismTone.pitch=1;mechanismTone.PlayOneShot(clip,Mathf.Clamp01(volume));DuckMusic(.85f,.2f);
        }
        public void Cue(string name)
        {
            if(!initialized)return;AudioClip clip;if(!clips.TryGetValue(name,out clip)||!clip)return;
            float interval=name=="impact"?.055f:name=="weak"?.045f:name=="hurt"?.18f:name=="step"?.18f:name=="reel"?.09f:name=="boss"?.3f:name=="sonar"?.16f:0;
            float previous;if(lastCue.TryGetValue(name,out previous)&&Time.unscaledTime-previous<interval)return;lastCue[name]=Time.unscaledTime;
            if(name=="hurt"||name=="danger"||name=="heartbeat"||name=="boss"||name=="sonar"){
                Critical(name,name=="boss"?.48f:name=="sonar"?.34f:name=="hurt"?.53f:.3f);DuckMusic(name=="boss"?1.7f:.7f,name=="sonar"?.28f:.6f);return;
            }
            float gain=name=="step"?.13f:name=="reel"?.08f:name=="impact"?.20f:name=="weak"?.28f:name=="shotgun"?.78f:name=="shot"?.72f:name=="carbine"?.54f:name=="burst"?.6f:name=="harpoon"?.66f:name=="arc"?.65f:name=="explosion"?.68f:.38f;
            int index=nextEffect++%effects.Length;var source=effects[index];source.Stop();source.clip=clip;effectGains[index]=gain;
            source.volume=game&&game.Log!=null?Mathf.Clamp01(game.Log.effectsVolume)*gain:gain*.85f;
            source.pitch=name=="coin"||name=="win"||name=="lose"||name=="ready"?1:.982f+(float)pitchRng.NextDouble()*.036f;source.Play();
            if(name=="win"||name=="discovery")DuckMusic(1,.5f);
        }
        void Critical(string name,float gain)
        {
            AudioClip clip;if(!priority||!clips.TryGetValue(name,out clip)||!clip)return;
            priority.volume=game&&game.Log!=null?Mathf.Clamp01(game.Log.effectsVolume):.85f;priority.pitch=1;priority.PlayOneShot(clip,gain);
        }
        AudioClip Fallback(string name)
        {
            // Only a missing import uses this quiet diagnostic cue.
            const int rate=22050;var data=new float[rate/8];
            for(int i=0;i<data.Length;i++){float t=(float)i/rate;data[i]=Mathf.Sin(2*Mathf.PI*330*t)*Mathf.Exp(-t*35)*.12f*Mathf.Min(1,t/.003f);}
            var clip=AudioClip.Create("Missing imported cue: "+name,data.Length,1,rate,false);clip.SetData(data,0);fallbackClips.Add(clip);return clip;
        }
        void OnDestroy(){foreach(var clip in fallbackClips)if(clip)Destroy(clip);}
    }
}
