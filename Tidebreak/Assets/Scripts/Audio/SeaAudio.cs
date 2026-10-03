using System.Collections.Generic;
using UnityEngine;

namespace Tidebreak
{
    public class SeaAudio : MonoBehaviour
    {
        Dictionary<string,AudioClip> clips=new Dictionary<string,AudioClip>();
        AudioSource effects, music, sea;
        public void Init()
        {
            effects=gameObject.AddComponent<AudioSource>();music=gameObject.AddComponent<AudioSource>();sea=gameObject.AddComponent<AudioSource>();
            foreach(string n in new[]{"shot","shotgun","harpoon","reload","ready","dash","hurt","splash","coin","select","boss","cast","bite","win","lose","step","reel","catch"})clips[n]=Synthesize(n);
            music.clip=MakeMusic();music.loop=true;music.volume=.15f;music.Play();
            sea.clip=MakeSea();sea.loop=true;sea.volume=.12f;sea.Play();
        }
        public void Cue(string name){AudioClip clip;if(clips.TryGetValue(name,out clip))effects.PlayOneShot(clip,name=="step"?.1f:name=="reel"?.045f:name=="shot"?.3f:.38f);}
        public void SetCombat(bool combat){if(music)music.pitch=combat?1.07f:1;}
        static AudioClip Synthesize(string name)
        {
            const int rate=22050;float duration=name=="boss"?1.8f:name=="win"?1.6f:.28f;
            var data=new float[(int)(rate*duration)];var rng=new System.Random(123);
            for(int i=0;i<data.Length;i++) {
                float t=(float)i/rate;float env=Mathf.Pow(1-t/duration,2);float freq=440;float noise=0;
                switch(name) {
                    case "step":freq=70;noise=.94f;env*=Mathf.Exp(-t*24);break;
                    case "reel":freq=2600;noise=.9f;env*=Mathf.Max(0,Mathf.Sin(t*180))*Mathf.Exp(-t*16);break;
                    case "catch":freq=680+t*700;noise=.12f;break;
                    case "shot":freq=150-300*t;noise=.7f;break;
                    case "shotgun":freq=90;noise=.9f;break;
                    case "harpoon":freq=850-2100*t;noise=.2f;break;
                    case "coin":freq=t<.1f?880:1320;break;
                    case "hurt":freq=90;noise=.35f;break;
                    case "boss":freq=65+Mathf.Sin(t*14)*9;noise=.1f;break;
                    case "cast":freq=380-t*800;noise=.3f;break;
                    case "splash":freq=120;noise=.85f;break;
                    case "bite":freq=900+Mathf.Sin(t*60)*250;break;
                    case "dash":freq=200;noise=.75f;break;
                    case "reload":freq=t<.1f?350:230;noise=.45f;break;
                    case "win":freq=440*Mathf.Pow(2,new[]{0,4,7,12}[(int)(t*3)%4]/12f);break;
                    case "lose":freq=220-t*90;break;
                }
                data[i]=(Mathf.Sin(2*Mathf.PI*freq*t)*(1-noise)+(float)(rng.NextDouble()*2-1)*noise)*env*.6f;
            }
            var c=AudioClip.Create(name,data.Length,1,rate,false);c.SetData(data,0);return c;
        }
        static AudioClip MakeMusic()
        {
            const int rate=22050;const float duration=24;var data=new float[(int)(rate*duration)];
            int[] notes={0,7,12,16,7,12,19,16, -3,4,9,12,4,9,16,12};
            for(int i=0;i<data.Length;i++) {
                float t=(float)i/rate;int beat=(int)(t/.75f);float dt=t-beat*.75f;
                float f=130.81f*Mathf.Pow(2,notes[beat%notes.Length]/12f);
                float pluck=(Mathf.Sin(t*f*6.283185f)+Mathf.Sin(t*f*12.56637f)*.25f)*Mathf.Exp(-dt*4)*.28f;
                float pad=Mathf.Sin(t*65.405f*6.283185f)*.1f+Mathf.Sin(t*98*6.283185f)*.05f;
                float fade=Mathf.Min(1,Mathf.Min(t,duration-t)*2);
                data[i]=(pluck+pad)*fade;
            }
            var c=AudioClip.Create("Wayfarer - original generative score",data.Length,1,rate,false);c.SetData(data,0);return c;
        }
        static AudioClip MakeSea()
        {
            const int rate=22050;var data=new float[rate*12];var r=new System.Random(7);float s=0;
            for(int i=0;i<data.Length;i++) {float t=(float)i/rate;s=Mathf.Lerp(s,(float)r.NextDouble()*2-1,.08f);data[i]=s*(.5f+.3f*Mathf.Sin(t*Mathf.PI/3))*Mathf.Min(1,Mathf.Min(t,12-t)*2);}
            var c=AudioClip.Create("Ocean air",data.Length,1,rate,false);c.SetData(data,0);return c;
        }
    }
}
