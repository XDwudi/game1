using UnityEngine;

namespace Tidebreak
{
    // Village signs are navigation aids, not tactical cover. Hide their renderers
    // during combat so they cannot conceal a foe while bullets pass through.
    public sealed class HarborSignVisibility : MonoBehaviour
    {
        Renderer[] parts;bool visible=true;
        void Start(){parts=GetComponentsInChildren<Renderer>();}
        void LateUpdate()
        {
            var game=GameDirector.Instance;if(!game||parts==null)return;
            bool next=game.State!=VoyageState.Combat;
            if(next==visible)return;visible=next;
            foreach(var part in parts)if(part)part.enabled=visible;
        }
    }
}
