using UnityEngine;

namespace Tidebreak
{
    public class AnglerController : MonoBehaviour
    {
        public GameDirector game;
        public Camera View;
        public int Ammo {get;private set;}
        public bool Reloading {get{return reloadEnd>Time.time;}}
        public int Capacity {get{return Balance.Weapons[(int)weapon].magazine+game.Run.magazineRelics*2;}}
        public float DashReady {get{return Mathf.Clamp01(1-(dashReady-Time.time)/game.Run.DodgeCooldown);}}
        public WeaponKind weapon;
        public bool RodEquipped=true;
        public FishLoot HeldFish;
        public float InvulnerableUntil;
        public Vector3 RodTip {get{return tip.position;}}
        public bool Grounded {get{return motor&&motor.isGrounded;}}
        public CharacterController Motor {get{return motor;}}
        float yaw,pitch,fireAt,reloadEnd,dashReady,dashEnd,recoil,shake,vertical,castAnim,stepAt,swayX,swayY;
        Vector3 dashDir;
        Transform equipment,muzzle,tip,reel;
        GameObject gun,rod,carryHands;
        CharacterController motor;
        public void Init(GameDirector director)
        {
            game=director;gameObject.layer=9;
            motor=gameObject.AddComponent<CharacterController>();motor.height=1.75f;motor.radius=.3f;motor.center=new Vector3(0,-.72f,0);motor.stepOffset=.38f;motor.slopeLimit=52;motor.skinWidth=.035f;
            var cameraObject=new GameObject("Captain camera");cameraObject.transform.SetParent(transform,false);View=cameraObject.AddComponent<Camera>();View.fieldOfView=75;View.nearClipPlane=.045f;View.farClipPlane=460;View.allowHDR=true;
            View.clearFlags=CameraClearFlags.Skybox;cameraObject.tag="MainCamera";cameraObject.AddComponent<AudioListener>();
            equipment=new GameObject("Handheld tools").transform;equipment.SetParent(cameraObject.transform,false);
            rod=ToolArt.Rod(equipment,out tip,out reel);carryHands=ToolArt.CarryHands(equipment);BuildGun();ResetForEncounter();
        }
        void BuildGun(){if(gun){gun.SetActive(false);Destroy(gun);}gun=ToolArt.Gun(equipment,weapon,out muzzle);}
        public void Teleport(Vector3 pos){bool enabled=motor.enabled;motor.enabled=false;transform.position=pos;motor.enabled=enabled;vertical=0;}
        public void ResetForEncounter()
        {
            Teleport(game.World.Spawn);yaw=0;pitch=9;reloadEnd=0;dashReady=0;dashEnd=0;InvulnerableUntil=0;
            transform.rotation=Quaternion.identity;View.transform.localRotation=Quaternion.Euler(pitch,0,0);weapon=(WeaponKind)Mathf.Clamp(game.Run.selectedWeapon,0,2);Ammo=Capacity;fireAt=Time.time+.3f;BuildGun();SetRod(true);
        }
        public void SetRod(bool active){if(game.State==VoyageState.Fishing&&!active)game.CancelFishing();RodEquipped=active;}
        public void CastAnimation(){castAnim=1;}
        void Update()
        {
            if(!game||!View)return;
            bool active=game.IsPlaying&&!game.Paused;motor.enabled=active;
            rod.SetActive(active&&RodEquipped&&!HeldFish);gun.SetActive(active&&!RodEquipped&&!HeldFish);carryHands.SetActive(active&&HeldFish);
            if(!active)return;
            if(!game.Automation){float mx=Input.GetAxisRaw("Mouse X"),my=Input.GetAxisRaw("Mouse Y");yaw+=mx*1.7f*game.Log.sensitivity;pitch=Mathf.Clamp(pitch-my*1.7f*game.Log.sensitivity,-78,82);swayX=Mathf.Lerp(swayX,-mx*.008f,Time.deltaTime*12);swayY=Mathf.Lerp(swayY,-my*.008f,Time.deltaTime*12);}
            transform.rotation=Quaternion.Euler(0,yaw,0);
            Vector3 move=transform.right*Input.GetAxisRaw("Horizontal")+transform.forward*Input.GetAxisRaw("Vertical");move=Vector3.ClampMagnitude(move,1);
            if(Input.GetKeyDown(KeyCode.LeftShift)&&Time.time>=dashReady)Dash(move.sqrMagnitude>.1f?move:transform.forward);
            if(Time.time<dashEnd)move=dashDir*3;
            if(motor.isGrounded&&vertical<0)vertical=-2;
            if(Input.GetKeyDown(KeyCode.Space)&&motor.isGrounded)vertical=6.3f;
            vertical-=19*Time.deltaTime;motor.Move((move*5.2f+Vector3.up*vertical)*Time.deltaTime);
            if(transform.position.y<-.75f){Teleport(game.World.Spawn);game.Notice("海流把你送回岸边 · 走码头更安全",3);if(game.State==VoyageState.Combat)TakeDamage(8);}
            if(move.sqrMagnitude>.2f&&motor.isGrounded&&Time.time>stepAt){stepAt=Time.time+.39f;game.Audio.Cue("step");}
            recoil=Mathf.MoveTowards(recoil,0,Time.deltaTime*6);shake=Mathf.MoveTowards(shake,0,Time.deltaTime*3);castAnim=Mathf.MoveTowards(castAnim,0,Time.deltaTime*2);
            float bob=move.magnitude>.1f?Mathf.Sin(Time.time*11)*.012f:Mathf.Sin(Time.time*1.8f)*.003f;
            View.transform.localRotation=Quaternion.Euler(pitch-recoil*2,0,game.Log.shake?Mathf.Sin(Time.time*35)*shake:0);View.transform.localPosition=new Vector3(0,bob,0);
            bool ads=Input.GetMouseButton(1)&&!RodEquipped&&!HeldFish;float tilt=Reloading?Mathf.Sin((reloadEnd-Time.time)/Balance.Weapons[(int)weapon].reload*Mathf.PI)*43:0;
            equipment.localRotation=Quaternion.Euler(-recoil*8-castAnim*21+(RodEquipped?game.CastCharge*-12+game.Tension*5:0),tilt,Mathf.Sin(Time.time*3)*bob*40);
            Vector3 desired=new Vector3(swayX+(ads?-.245f:0),swayY+bob+(ads?.17f:0),-recoil*.06f+(ads?.04f:0));equipment.localPosition=Vector3.Lerp(equipment.localPosition,desired,Time.deltaTime*16);
            if(game.State==VoyageState.Fishing&&Input.GetMouseButton(0))reel.Rotate(Vector3.right,Time.deltaTime*420,Space.Self);
            View.fieldOfView=Mathf.Lerp(View.fieldOfView,ads?57:75,Time.deltaTime*11);
            if(!game.Automation){
                if(Input.GetKeyDown(KeyCode.Alpha1)){StowHeld();SetRod(true);}if(Input.GetKeyDown(KeyCode.Alpha2))Equip(WeaponKind.Revolver);if(Input.GetKeyDown(KeyCode.Alpha3))Equip(WeaponKind.Scattergun);if(Input.GetKeyDown(KeyCode.Alpha4))Equip(WeaponKind.Harpoon);
                if(Input.GetKeyDown(KeyCode.F))StowHeld();if(Input.GetKeyDown(KeyCode.Q)&&HeldFish)ThrowHeld();
            }
            if(reloadEnd>0&&Time.time>=reloadEnd){Ammo=Capacity;reloadEnd=0;game.Audio.Cue("ready");}
            if(!RodEquipped&&!HeldFish&&!game.Automation){if(Input.GetKeyDown(KeyCode.R))Reload();if(Input.GetMouseButton(0)&&Time.time>=fireAt&&!Reloading)Fire();}
        }
        public void PickUp(FishLoot fish)
        {
            if(!fish||HeldFish||Vector3.Distance(transform.position,fish.transform.position)>4)return;
            HeldFish=fish;game.RegisterCatch(fish);fish.Hold(View.transform);game.Audio.Cue("ready");
        }
        public bool StowHeld(){return !HeldFish||game.Stow(HeldFish);}
        public void ThrowHeld(){if(!HeldFish)return;var f=HeldFish;HeldFish=null;f.Release(View.transform.position+View.transform.forward*.9f,View.transform.forward*9+Vector3.up*2);game.Audio.Cue("cast");}
        public void AimAt(Vector3 point){var a=Quaternion.LookRotation(point-View.transform.position).eulerAngles;yaw=a.y;pitch=a.x>180?a.x-360:a.x;transform.rotation=Quaternion.Euler(0,yaw,0);View.transform.localRotation=Quaternion.Euler(pitch,0,0);}
        public void Dash(Vector3 direction){if(Time.time<dashReady)return;dashDir=direction.normalized;dashEnd=Time.time+.19f;InvulnerableUntil=Time.time+.3f;dashReady=Time.time+game.Run.DodgeCooldown;game.Audio.Cue("dash");}
        public void Equip(WeaponKind kind)
        {
            if(kind==WeaponKind.Scattergun&&!game.Run.shotgun||kind==WeaponKind.Harpoon&&!game.Run.harpoon){game.Notice("先在老船长工坊用金币购买这件武器",2);return;}
            if(!StowHeld())return;SetRod(false);if(kind==weapon)return;weapon=kind;game.Run.selectedWeapon=(int)kind;reloadEnd=Time.time+.6f;Ammo=0;BuildGun();game.Audio.Cue("reload");
        }
        public void Refill(){Ammo=Capacity;reloadEnd=0;}
        public void Reload(){if(Reloading||Ammo>=Capacity)return;reloadEnd=Time.time+Balance.Weapons[(int)weapon].reload/(1+game.Run.hasteRelics*.05f);game.Audio.Cue("reload");}
        public bool Fire()
        {
            if(!game.IsPlaying||game.State==VoyageState.Fishing||RodEquipped||HeldFish||game.Paused||Reloading||Time.time<fireAt)return false;
            if(Ammo<=0){Reload();return false;}
            var w=Balance.Weapons[(int)weapon];Ammo--;fireAt=Time.time+w.interval/game.Run.FireRateMultiplier;recoil=weapon==WeaponKind.Revolver?.43f:.85f;
            game.Audio.Cue(weapon==WeaponKind.Scattergun?"shotgun":weapon==WeaponKind.Harpoon?"harpoon":"shot");
            var flash=Shape.Part("Muzzle flash",PrimitiveType.Sphere,null,muzzle.position,Vector3.one*.085f,new Color(1,.74f,.3f),false,true);Destroy(flash,.035f);
            for(int i=0;i<w.pellets;i++) {
                float spread=Input.GetMouseButton(1)?w.spread*.6f:w.spread;
                var d=(View.transform.forward+View.transform.right*Random.Range(-spread,spread)+View.transform.up*Random.Range(-spread,spread)).normalized;
                RaycastHit hit;Vector3 endpoint=View.transform.position+d*90;
                if(Physics.Raycast(View.transform.position,d,out hit,100,~(1<<9))) {
                    endpoint=hit.point;var e=hit.collider.GetComponentInParent<Enemy>();
                    if(e){bool crit=game.Rng.NextDouble()<game.Run.CriticalChance;float falloff=weapon==WeaponKind.Scattergun?Mathf.Lerp(1,.36f,Mathf.InverseLerp(10,35,hit.distance)):1;e.Hit(w.damage*game.Run.DamageMultiplier*(crit?1.75f:1)*falloff,hit.collider.GetComponent<HitRegion>()!=null,crit);}
                    else {var loot=hit.collider.GetComponentInParent<FishLoot>();if(loot)loot.Push(d*3);game.Effect(hit.point,new Color(.62f,.58f,.43f),3,.04f);}
                }
                game.Tracer(muzzle.position,endpoint,weapon==WeaponKind.Harpoon?new Color(.5f,.87f,.8f):new Color(1,.83f,.53f));
            }
            return true;
        }
        public void TakeDamage(float amount)
        {
            if(game.State!=VoyageState.Combat||game.Paused||Time.time<InvulnerableUntil)return;
            game.Run.health=Mathf.Max(0,game.Run.health-amount*game.Run.DamageTakenMultiplier);InvulnerableUntil=Time.time+.6f;shake=game.Log.shake?1:0;game.UI.DamageFlash();game.Audio.Cue("hurt");if(game.Run.health<=0)game.EndVoyage(false);
        }
    }
}
