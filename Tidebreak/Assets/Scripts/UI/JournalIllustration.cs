using UnityEngine;
namespace Tidebreak
{
    public partial class SeaHUD
    {
        Texture2D journalAtlas;
        void IslandIllustration(Transform parent,float x,float y,float w,float h,int stage,float opacity=1)
        {
            if(!journalAtlas)journalAtlas=Resources.Load<Texture2D>("Art/IslandJournalAtlas");if(!journalAtlas)return;
            int cell=Mathf.Clamp(stage-1,0,8);var frame=Rect("Hand-painted nautical journal",parent,x,y,w,h);
            var image=frame.gameObject.AddComponent<UnityEngine.UI.RawImage>();image.texture=journalAtlas;image.raycastTarget=false;image.color=new Color(1,1,1,opacity);
            float cellAspect=journalAtlas.width/(float)journalAtlas.height;float cropHeight=Mathf.Min(1,cellAspect/(w/h));
            image.uvRect=new Rect(cell%3/3f,(2-cell/3)/3f+(1-cropHeight)/6f,1/3f,cropHeight/3f);
        }
    }
}
