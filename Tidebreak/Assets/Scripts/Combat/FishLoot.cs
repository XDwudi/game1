using UnityEngine;

namespace Tidebreak
{
    public class FishLoot : MonoBehaviour
    {
        public CatchData Data;
        public bool Held,Registered;
        GameDirector game;
        Rigidbody body;
        Collider[] colliders;
        Vector3 originalScale;
        public void Init(GameDirector director,CatchData data,Quaternion orientation)
        {
            game=director;Data=data;transform.rotation=orientation;
            var rig=CreatureArt.Build(transform,data.kind,data.elite);
            float size=data.quality==1?1.35f:1;rig.localScale=Vector3.one*size;
            foreach(var r in rig.GetComponentsInChildren<HitRegion>()){r.GetComponent<Collider>().enabled=false;Destroy(r.GetComponent<Collider>());Destroy(r);}
            if(data.quality==2)foreach(var r in rig.GetComponentsInChildren<Renderer>()){var block=new MaterialPropertyBlock();block.SetColor("_Color",new Color(1,.85f,.32f));r.SetPropertyBlock(block);}
            body=gameObject.AddComponent<Rigidbody>();body.mass=2;body.drag=.55f;body.angularDrag=.9f;body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
            body.velocity=Vector3.up*2;body.angularVelocity=Random.insideUnitSphere*3;colliders=GetComponentsInChildren<Collider>();originalScale=transform.localScale;
        }
        public void Hold(Transform camera)
        {
            Held=true;body.isKinematic=true;foreach(var c in colliders)if(c)c.enabled=false;
            transform.SetParent(camera,false);transform.localPosition=new Vector3(.13f,-.35f,1.1f);transform.localRotation=Quaternion.Euler(10,74,20);transform.localScale=originalScale*.43f;
        }
        public void Release(Vector3 point,Vector3 velocity)
        {Held=false;transform.SetParent(null,true);transform.localScale=originalScale;transform.position=point;foreach(var c in colliders)if(c)c.enabled=true;body.isKinematic=false;body.velocity=velocity;body.angularVelocity=Random.insideUnitSphere*4;}
        public void Push(Vector3 impulse){if(!Held)body.AddForce(impulse,ForceMode.Impulse);}
        void Update()
        {
            if(Held){transform.localPosition=new Vector3(.13f,-.35f+Mathf.Sin(Time.time*2)*.012f,1.1f);return;}
            if(transform.position.y < -1.5f) {
                Vector3 p=game.Player.transform.position+game.Player.transform.forward*1.5f;p.y=game.World.GroundAt(p)+.65f;
                if(p.y<0)p=game.World.Spawn-Vector3.up;transform.position=p;body.velocity=Vector3.zero;
                game.Notice("鱼获被浪花冲回岸边 · E 拿起",2);
            }
        }
    }
}
