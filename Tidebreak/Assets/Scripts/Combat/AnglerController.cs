using UnityEngine;

namespace Tidebreak
{
    public class AnglerController : MonoBehaviour
    {
        public GameDirector game;
        public Camera View;
        public int Ammo { get; private set; }
        public bool Reloading { get { return reloadEnd>Time.time; } }
        public int Capacity { get { return Balance.Weapons[(int)weapon].magazine+game.Run.magazineRelics*2; } }
        public float DashReady { get { return Mathf.Clamp01(1-(dashReady-Time.time)/game.Run.DodgeCooldown); } }
        public WeaponKind weapon;
        public float InvulnerableUntil;
        float yaw,pitch,fireAt,reloadEnd,dashReady,dashEnd,recoil,shake;
        Vector3 dashDir;
        Transform equipment,muzzle;
        GameObject gun,rod;
        public void Init(GameDirector director)
        {
            game=director; transform.position=new Vector3(0,1.9f,0);
            var cameraObject=new GameObject("Captain camera");cameraObject.transform.SetParent(transform,false);
            View=cameraObject.AddComponent<Camera>(); View.fieldOfView=73;View.nearClipPlane=.07f;View.farClipPlane=450;
            View.clearFlags=CameraClearFlags.Skybox; cameraObject.tag="MainCamera";cameraObject.AddComponent<AudioListener>();
            equipment=new GameObject("Viewmodel").transform;equipment.SetParent(cameraObject.transform,false);
            BuildRod();BuildGun();ResetForEncounter();
        }
        public void ResetForEncounter()
        {
            transform.position=new Vector3(0,1.9f,0);yaw=0;pitch=2;reloadEnd=0;dashReady=0;dashEnd=0;InvulnerableUntil=0;
            transform.rotation=Quaternion.identity;View.transform.localRotation=Quaternion.Euler(pitch,0,0);
            weapon=(WeaponKind)game.Run.selectedWeapon;Ammo=Capacity;fireAt=Time.time+.4f;
        }
        void BuildRod()
        {
            rod=new GameObject("Fishing rod");rod.transform.SetParent(equipment,false);
            Shape.Beam(rod.transform,new Vector3(.38f,-.55f,.35f),new Vector3(.55f,.65f,2.1f),.035f,new Color(.14f,.25f,.28f));
            Shape.Beam(rod.transform,new Vector3(.38f,-.55f,.35f),new Vector3(.43f,-.25f,.8f),.08f,new Color(.59f,.37f,.18f));
            Shape.Part("Reel",PrimitiveType.Cylinder,rod.transform,new Vector3(.47f,-.25f,.65f),new Vector3(.15f,.055f,.15f),new Color(.92f,.69f,.34f)).transform.localRotation=Quaternion.Euler(0,0,90);
            Shape.Part("Glove",PrimitiveType.Capsule,rod.transform,new Vector3(.36f,-.48f,.55f),new Vector3(.19f,.21f,.22f),new Color(.22f,.35f,.36f)).transform.localRotation=Quaternion.Euler(40,0,0);
        }
        void BuildGun()
        {
            if(gun)Destroy(gun);
            gun=new GameObject("Weapon");gun.transform.SetParent(equipment,false);
            var gunmetal=new Color(.13f,.22f,.26f);var brass=new Color(.91f,.65f,.31f);
            float length=weapon==WeaponKind.Revolver?.5f:.85f;
            Shape.Part("Receiver",PrimitiveType.Cube,gun.transform,new Vector3(.38f,-.29f,.63f),new Vector3(.18f,.18f,.33f),gunmetal);
            Shape.Part("Brass cylinder",PrimitiveType.Cylinder,gun.transform,new Vector3(.38f,-.27f,.54f),new Vector3(.19f,.12f,.19f),brass).transform.localRotation=Quaternion.Euler(90,0,0);
            Shape.Beam(gun.transform,new Vector3(.38f,-.23f,.62f),new Vector3(.38f,-.23f,.62f+length),weapon==WeaponKind.Scattergun?.12f:.06f,gunmetal);
            Shape.Part("Stock",PrimitiveType.Cube,gun.transform,new Vector3(.38f,-.43f,.47f),new Vector3(.15f,.25f,.17f),new Color(.51f,.29f,.15f)).transform.localRotation=Quaternion.Euler(-16,0,0);
            Shape.Part("Gloved hand",PrimitiveType.Capsule,gun.transform,new Vector3(.38f,-.5f,.43f),new Vector3(.24f,.17f,.25f),new Color(.22f,.35f,.36f)).transform.localRotation=Quaternion.Euler(50,0,0);
            Shape.Part("Sight",PrimitiveType.Cube,gun.transform,new Vector3(.38f,-.14f,.6f+length),new Vector3(.025f,.07f,.025f),brass);
            if(weapon==WeaponKind.Harpoon)Shape.Beam(gun.transform,new Vector3(.38f,-.2f,.5f),new Vector3(.38f,-.2f,1.7f),.025f,new Color(.36f,1,.85f),true);
            muzzle=new GameObject("Muzzle").transform;muzzle.SetParent(gun.transform,false);muzzle.localPosition=new Vector3(.38f,-.23f,.7f+length);
        }
        void Update()
        {
            if(!game || !View)return;
            bool active=game.IsPlaying&&!game.Paused;
            bool fishing=game.State==VoyageState.Sailing||game.State==VoyageState.Fishing;
            rod.SetActive(fishing);gun.SetActive(!fishing&&game.State==VoyageState.Combat);
            if(!active)return;
            if(!game.Automation) {
                yaw+=Input.GetAxisRaw("Mouse X")*2*game.Log.sensitivity;
                pitch=Mathf.Clamp(pitch-Input.GetAxisRaw("Mouse Y")*2*game.Log.sensitivity,-70,75);
            }
            transform.rotation=Quaternion.Euler(0,yaw,0);
            Vector3 move=transform.right*Input.GetAxisRaw("Horizontal")+transform.forward*Input.GetAxisRaw("Vertical");
            move=Vector3.ClampMagnitude(move,1);
            if(Input.GetKeyDown(KeyCode.LeftShift)&&Time.time>=dashReady)Dash(move.sqrMagnitude>.1f?move:transform.forward);
            if(Time.time<dashEnd)move=dashDir*3.5f;
            var pos=transform.position+move*5*Time.deltaTime;
            pos.x=Mathf.Clamp(pos.x,-5.8f,5.8f);pos.z=Mathf.Clamp(pos.z,-3.6f,6.1f);pos.y=1.9f;transform.position=pos;
            recoil=Mathf.MoveTowards(recoil,0,Time.deltaTime*7);shake=Mathf.MoveTowards(shake,0,Time.deltaTime*3);
            float bob=move.magnitude>.1f?Mathf.Sin(Time.time*10)*.018f:Mathf.Sin(Time.time*1.6f)*.006f;
            View.transform.localRotation=Quaternion.Euler(pitch-recoil*2.8f,0,game.Log.shake?Mathf.Sin(Time.time*35)*shake:0);
            View.transform.localPosition=new Vector3(0,bob,0);
            float reloadTilt=Reloading?Mathf.Sin((reloadEnd-Time.time)/Balance.Weapons[(int)weapon].reload*Mathf.PI)*55:0;
            equipment.localRotation=Quaternion.Euler(-recoil*7,reloadTilt,0);
            equipment.localPosition=new Vector3(Mathf.Sin(Time.time*4)*bob,bob,-recoil*.08f);
            View.fieldOfView=Mathf.Lerp(View.fieldOfView,Input.GetMouseButton(1)&&!fishing?53:73,Time.deltaTime*10);
            if(fishing)return;
            if(Input.GetKeyDown(KeyCode.Alpha1))Equip(WeaponKind.Revolver);
            if(Input.GetKeyDown(KeyCode.Alpha2)&&game.Run.shotgun)Equip(WeaponKind.Scattergun);
            if(Input.GetKeyDown(KeyCode.Alpha3)&&game.Run.harpoon)Equip(WeaponKind.Harpoon);
            if(reloadEnd>0&&Time.time>=reloadEnd){Ammo=Capacity;reloadEnd=0;game.Audio.Cue("ready");}
            if(Input.GetKeyDown(KeyCode.R))Reload();
            if(Input.GetMouseButton(0)&&Time.time>=fireAt&&!Reloading)Fire();
        }
        public void AimAt(Vector3 point)
        { var a=Quaternion.LookRotation(point-View.transform.position).eulerAngles;yaw=a.y;pitch=a.x>180?a.x-360:a.x;transform.rotation=Quaternion.Euler(0,yaw,0);View.transform.localRotation=Quaternion.Euler(pitch,0,0); }
        public void Dash(Vector3 direction)
        { if(Time.time<dashReady)return;dashDir=direction.normalized;dashEnd=Time.time+.18f;InvulnerableUntil=Time.time+.3f;dashReady=Time.time+game.Run.DodgeCooldown;game.Audio.Cue("dash"); }
        public void Equip(WeaponKind kind)
        {
            if(kind==WeaponKind.Scattergun&&!game.Run.shotgun ||kind==WeaponKind.Harpoon&&!game.Run.harpoon)return;
            if(kind==weapon)return;
            weapon=kind;game.Run.selectedWeapon=(int)kind;reloadEnd=Time.time+.65f;Ammo=0;BuildGun();game.Audio.Cue("reload");
        }
        public void Refill(){Ammo=Capacity;reloadEnd=0;}
        public void Reload()
        { if(Reloading||Ammo>=Capacity)return;reloadEnd=Time.time+Balance.Weapons[(int)weapon].reload/(1+game.Run.hasteRelics*.05f);game.Audio.Cue("reload"); }
        public bool Fire()
        {
            if(game.State!=VoyageState.Combat||game.Paused||Reloading||Time.time<fireAt)return false;
            if(Ammo<=0){Reload();return false;}
            var w=Balance.Weapons[(int)weapon];Ammo--;fireAt=Time.time+w.interval/game.Run.FireRateMultiplier;recoil=weapon==WeaponKind.Revolver?.5f:1;
            game.Audio.Cue(weapon==WeaponKind.Scattergun?"shotgun":weapon==WeaponKind.Harpoon?"harpoon":"shot");
            game.Effect(muzzle.position,new Color(1,.78f,.38f),4,.04f);
            for(int i=0;i<w.pellets;i++) {
                var d=(View.transform.forward+View.transform.right*Random.Range(-w.spread,w.spread)+View.transform.up*Random.Range(-w.spread,w.spread)).normalized;
                RaycastHit hit;Vector3 endpoint=View.transform.position+d*80;
                if(Physics.Raycast(View.transform.position,d,out hit,100)) {
                    endpoint=hit.point;var e=hit.collider.GetComponentInParent<Enemy>();
                    if(e){bool crit=game.Rng.NextDouble()<game.Run.CriticalChance;e.Hit(w.damage*game.Run.DamageMultiplier*(crit?1.75f:1),hit.collider.GetComponent<HitRegion>()!=null,crit);}
                }
                game.Tracer(muzzle.position,endpoint,weapon==WeaponKind.Harpoon?new Color(.4f,1,.85f):new Color(1,.8f,.43f));
            }
            return true;
        }
        public void TakeDamage(float amount)
        {
            if(game.State!=VoyageState.Combat||game.Paused||Time.time<InvulnerableUntil)return;
            game.Run.health=Mathf.Max(0,game.Run.health-amount*game.Run.DamageTakenMultiplier);InvulnerableUntil=Time.time+.6f;
            shake=game.Log.shake?1.7f:0;game.UI.DamageFlash();game.Audio.Cue("hurt");
            if(game.Run.health<=0)game.EndVoyage(false);
        }
    }
}
