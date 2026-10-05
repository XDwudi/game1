using TMPro;
using UnityEngine;

namespace Tidebreak
{
    public partial class SeaHUD
    {
        TextMeshProUGUI explorationKeys;
        UnityEngine.UI.Image dangerBack;
        RectTransform damageBearing,impactReticle;
        ImpactArcGraphic damageArc;ImpactSealGraphic impactSeal;WoundVignetteGraphic woundVignette;
        Vector3 damageDirection;bool damageDirectionKnown;float impactLifetime=.25f;bool lastImpactKill;
        Color impactColor;
        void InitImpactPresentation()
        {
            impactReticle=Rect("Hit confirmation",hud,800,450,80,80);impactReticle.pivot=Vector2.one*.5f;
            impactSeal=impactReticle.gameObject.AddComponent<ImpactSealGraphic>();impactSeal.raycastTarget=false;impactSeal.color=Color.clear;
            damageBearing=Rect("Directional wound bearing",hud,800,450,260,260);damageBearing.pivot=Vector2.one*.5f;
            damageArc=damageBearing.gameObject.AddComponent<ImpactArcGraphic>();damageArc.color=Color.clear;damageArc.raycastTarget=false;
            woundVignette=Full("Feathered wound border",hud).gameObject.AddComponent<WoundVignetteGraphic>();
            woundVignette.raycastTarget=false;woundVignette.color=Color.clear;
            // Fine copper rules and panel corners keep the canvas visually related to nautical instruments.
            Box(hud,26,23,3,148,new Color(Gold.r,Gold.g,Gold.b,.5f));
            Box(bossPanel,18,48+3,644,1,new Color(Cream.r,Cream.g,Cream.b,.12f));
        }
        void ShowImpactMarker(bool critical,float damage,bool killed)
        {
            Color color=damage<=0?Muted:killed?new Color(1,.86f,.51f):critical?Gold:Cream;
            impactLifetime=killed?.48f:critical?.34f:.25f;hitUntil=Time.unscaledTime+impactLifetime;lastImpactKill=killed;
            hit.color=Color.clear;impactColor=color;impactSeal.Kind=killed?2:critical?1:0;impactSeal.SetVerticesDirty();impactSeal.color=color;
            damageNumber.color=color;damageNumber.text=damage<=0?"装甲":killed?"击破":Mathf.RoundToInt(damage)+(critical?" · 弱点":"");
        }
        void UpdateImpactPresentation(float danger)
        {
            float remaining=Mathf.Clamp01((hitUntil-Time.unscaledTime)/impactLifetime);
            impactReticle.gameObject.SetActive(game.IsPlaying&&!game.Paused&&remaining>0);
            float age=1-remaining;
            // Fast settle on contact, then a small release: the aim point stays empty.
            impactReticle.localScale=Vector3.one*(1+.25f*Mathf.Exp(-age*18)+age*(lastImpactKill?.19f:.06f));
            impactSeal.color=new Color(impactColor.r,impactColor.g,impactColor.b,remaining);
            if(remaining>0){var numberColor=damageNumber.color;numberColor.a=Mathf.Clamp01(remaining*2);damageNumber.color=numberColor;}
            float wound=Mathf.Clamp01((damageUntil-Time.unscaledTime)/.55f);
            bool visible=game.IsPlaying&&!game.Paused;
            woundVignette.color=new Color(.9f,.23f,.12f,visible?wound*.64f:0);
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
    // Four engraved spearheads leave an empty centre. Weak hits add a second register;
    // the interrupted outer seal is reserved for a confirmed kill, independent of colour.
    public sealed class ImpactSealGraphic : UnityEngine.UI.MaskableGraphic
    {
        public int Kind;
        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper vh)
        {
            vh.Clear();
            for(int i=0;i<4;i++){
                float a=(45+90*i)*Mathf.Deg2Rad;Vector3 d=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0),n=new Vector3(-d.y,d.x,0);
                int k=vh.currentVertCount;
                vh.AddVert(d*10,color,Vector2.zero);vh.AddVert(d*23+n*2.5f,color,Vector2.zero);
                vh.AddVert(d*20,color,Vector2.zero);vh.AddVert(d*23-n*2.5f,color,Vector2.zero);
                vh.AddTriangle(k,k+1,k+2);vh.AddTriangle(k,k+2,k+3);
                if(Kind>0){
                    k=vh.currentVertCount;Color c=color;c.a*=.68f;
                    vh.AddVert(d*27-n*2,c,Vector2.zero);vh.AddVert(d*29-n*2,c,Vector2.zero);
                    vh.AddVert(d*29+n*2,c,Vector2.zero);vh.AddVert(d*27+n*2,c,Vector2.zero);
                    vh.AddTriangle(k,k+1,k+2);vh.AddTriangle(k,k+2,k+3);
                }
                if(Kind==2)for(int s=0;s<6;s++){
                    float a0=(i*90+8+s*6)*Mathf.Deg2Rad,a1=a0+6*Mathf.Deg2Rad;
                    Vector3 p=new Vector3(Mathf.Cos(a0),Mathf.Sin(a0),0),q=new Vector3(Mathf.Cos(a1),Mathf.Sin(a1),0);
                    k=vh.currentVertCount;Color c=color;c.a*=.58f;
                    vh.AddVert(p*34,c,Vector2.zero);vh.AddVert(p*35.5f,c,Vector2.zero);
                    vh.AddVert(q*35.5f,c,Vector2.zero);vh.AddVert(q*34,c,Vector2.zero);
                    vh.AddTriangle(k,k+1,k+2);vh.AddTriangle(k,k+2,k+3);
                }
            }
        }
    }
    public sealed class WoundVignetteGraphic : UnityEngine.UI.MaskableGraphic
    {
        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper vh)
        {
            vh.Clear();Rect r=rectTransform.rect;float width=Mathf.Min(r.width,r.height)*.035f;
            Edge(vh,new Vector2(r.xMin,r.yMin),new Vector2(r.xMax,r.yMin),Vector2.up*width);
            Edge(vh,new Vector2(r.xMax,r.yMin),new Vector2(r.xMax,r.yMax),Vector2.left*width);
            Edge(vh,new Vector2(r.xMax,r.yMax),new Vector2(r.xMin,r.yMax),Vector2.down*width);
            Edge(vh,new Vector2(r.xMin,r.yMax),new Vector2(r.xMin,r.yMin),Vector2.right*width);
        }
        void Edge(UnityEngine.UI.VertexHelper vh,Vector2 a,Vector2 b,Vector2 inset)
        {
            int k=vh.currentVertCount;Color clear=color;clear.a=0;
            vh.AddVert(a,color,Vector2.zero);vh.AddVert(b,color,Vector2.zero);
            vh.AddVert(b+inset,clear,Vector2.zero);vh.AddVert(a+inset,clear,Vector2.zero);
            vh.AddTriangle(k,k+1,k+2);vh.AddTriangle(k,k+2,k+3);
        }
    }
    // A pointed instrument arc indicates attack bearing without covering the target.
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
            int arrow=vh.currentVertCount;
            vh.AddVert(new Vector3(-4,103,0),color,Vector2.zero);vh.AddVert(new Vector3(4,103,0),color,Vector2.zero);
            vh.AddVert(new Vector3(0,96,0),color,Vector2.zero);vh.AddTriangle(arrow,arrow+1,arrow+2);
            for(int i=0;i<5;i++){
                float a=(70+i*10)*Mathf.Deg2Rad;Vector3 d=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0),n=new Vector3(-d.y,d.x,0);
                int k=vh.currentVertCount;Color c=color;c.a*=.45f;
                vh.AddVert(d*117-n*.6f,c,Vector2.zero);vh.AddVert(d*122-n*.6f,c,Vector2.zero);
                vh.AddVert(d*122+n*.6f,c,Vector2.zero);vh.AddVert(d*117+n*.6f,c,Vector2.zero);
                vh.AddTriangle(k,k+1,k+2);vh.AddTriangle(k,k+2,k+3);
            }
        }
    }
}
