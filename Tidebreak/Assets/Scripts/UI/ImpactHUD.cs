using TMPro;
using UnityEngine;

namespace Tidebreak
{
    public partial class SeaHUD
    {
        TextMeshProUGUI explorationKeys;
        UnityEngine.UI.Image dangerBack;
        readonly UnityEngine.UI.Image[] impactStrokes=new UnityEngine.UI.Image[4];
        readonly UnityEngine.UI.Image[] woundEdges=new UnityEngine.UI.Image[4];
        RectTransform damageBearing,impactReticle;
        ImpactArcGraphic damageArc;
        Vector3 damageDirection;bool damageDirectionKnown;float impactLifetime=.25f;bool lastImpactKill;
        void InitImpactPresentation()
        {
            impactReticle=Rect("Hit confirmation",hud,800,450,80,80);impactReticle.pivot=Vector2.one*.5f;
            for(int i=0;i<4;i++){
                float a=(45+i*90)*Mathf.Deg2Rad;
                var stroke=Box(impactReticle,40+Mathf.Cos(a)*14,40-Mathf.Sin(a)*14,11,2,Color.clear);
                stroke.rectTransform.pivot=new Vector2(.5f,.5f);stroke.rectTransform.localRotation=Quaternion.Euler(0,0,a*Mathf.Rad2Deg);impactStrokes[i]=stroke;
            }
            damageBearing=Rect("Directional wound bearing",hud,800,450,260,260);damageBearing.pivot=Vector2.one*.5f;
            damageArc=damageBearing.gameObject.AddComponent<ImpactArcGraphic>();damageArc.color=Color.clear;damageArc.raycastTarget=false;
            woundEdges[0]=Box(hud,0,0,1600,4,Color.clear);woundEdges[1]=Box(hud,0,896,1600,4,Color.clear);
            woundEdges[2]=Box(hud,0,0,4,900,Color.clear);woundEdges[3]=Box(hud,1596,0,4,900,Color.clear);
            // Fine copper rules and panel corners keep the canvas visually related to nautical instruments.
            Box(hud,26,23,3,148,new Color(Gold.r,Gold.g,Gold.b,.5f));
            Box(bossPanel,18,48+3,644,1,new Color(Cream.r,Cream.g,Cream.b,.12f));
        }
        void ShowImpactMarker(bool critical,float damage,bool killed)
        {
            Color color=damage<=0?Muted:killed?new Color(1,.86f,.51f):critical?Gold:Cream;
            impactLifetime=killed?.48f:critical?.34f:.25f;hitUntil=Time.unscaledTime+impactLifetime;lastImpactKill=killed;
            hit.color=Color.clear;foreach(var stroke in impactStrokes)if(stroke)stroke.color=color;
            damageNumber.color=color;damageNumber.text=damage<=0?"装甲":killed?"击破":Mathf.RoundToInt(damage)+(critical?" · 弱点":"");
        }
        void UpdateImpactPresentation(float danger)
        {
            float remaining=Mathf.Clamp01((hitUntil-Time.unscaledTime)/impactLifetime);
            impactReticle.gameObject.SetActive(game.IsPlaying&&!game.Paused&&remaining>0);
            impactReticle.localScale=Vector3.one*(1+(1-remaining)*(lastImpactKill?.42f:.18f));
            foreach(var stroke in impactStrokes){var color=stroke.color;color.a=remaining;stroke.color=color;}
            float wound=Mathf.Clamp01((damageUntil-Time.unscaledTime)/.55f);
            bool visible=game.IsPlaying&&!game.Paused;
            foreach(var edge in woundEdges)edge.color=new Color(.98f,.25f,.14f,visible?wound*.76f:0);
            damageBearing.gameObject.SetActive(visible&&wound>0&&damageDirectionKnown);
            if(damageDirectionKnown){
                Vector3 delta=damageDirection-game.Player.transform.position;delta.y=0;
                Vector3 forward=game.Player.View.transform.forward;forward.y=0;
                if(forward.sqrMagnitude<.0001f){forward=game.Player.transform.forward;forward.y=0;}
                float angle=delta.sqrMagnitude>.0001f?Vector3.SignedAngle(forward,delta,Vector3.up):0;
                damageBearing.localRotation=Quaternion.Euler(0,0,-angle);damageArc.color=new Color(1,.4f,.22f,wound*.95f);
            }
            if(dangerBack){float pulse=danger>=0&&danger<.65f?.09f*Mathf.Sin(Time.unscaledTime*15):0;dangerBack.color=new Color(.45f+pulse,.105f,.075f,.92f);}
            healthFill.color=game.Run.health/game.Run.MaxHealth<.3f?new Color(.96f,.42f,.26f):Mint;
        }
    }
    // A short arc indicates the attack bearing without covering the aiming target or depending on text.
    public sealed class ImpactArcGraphic : UnityEngine.UI.MaskableGraphic
    {
        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper vh)
        {
            vh.Clear();const int segments=18;const float radius=111,width=5;
            for(int i=0;i<=segments;i++){
                float a=(63+54*i/(float)segments)*Mathf.Deg2Rad;Vector3 dir=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0);Color c=color;c.a*=Mathf.Sin(Mathf.PI*i/segments)*.65f+.35f;
                vh.AddVert(dir*(radius-width),c,Vector2.zero);vh.AddVert(dir*radius,c,Vector2.one);
                if(i<segments){int k=i*2;vh.AddTriangle(k,k+1,k+3);vh.AddTriangle(k,k+3,k+2);}
            }
        }
    }
}
