using UnityEngine;

namespace Tidebreak
{
    public class AnglerController : MonoBehaviour
    {
        public GameDirector game;
        public Camera View;
        public int Ammo {get;private set;}
        public bool Reloading {get{return reloadEnd>Time.time;}}
        public float ReloadProgress {get{return Reloading?Mathf.Clamp01(1-(reloadEnd-Time.time)/Mathf.Max(.1f,reloadDuration)):0;}}
        public bool QuickReloadAvailable {get{return Reloading&&!quickReloadAttempted;}}
        public BuildSynergy Synergy {get;private set;}
        WeaponMotion gunMotion;bool quickReloadAttempted;
        public int Capacity {get{return Balance.Weapons[(int)weapon].magazine+game.Run.magazineRelics*2;}}
        public float DashReady {get{return Mathf.Clamp01(1-(dashReady-Time.time)/game.Run.DodgeCooldown);}}
        public WeaponKind weapon;
        public bool RodEquipped=true;
        public FishLoot HeldFish;
        public float InvulnerableUntil;
        public Vector3 RodTip {get{return tip.position;}}
        public bool Grounded {get{return motor&&motor.isGrounded;}}
        public CharacterController Motor {get{return motor;}}
        float kickPitch,kickYaw,kickVelocity,kickYawVelocity,recoilVelocity,bloom,slowUntil,poisonUntil,poisonAt,reloadDuration;
        int burstRemaining;float burstAt;int[] storedAmmo=new int[6];
        public float AimBloom {get{return bloom;}}
        public string StatusEffect {get{return Time.time<poisonUntil?"中毒":Time.time<slowUntil?"冰霜减速":"";}}
        float yaw,pitch,fireAt,reloadEnd,dashReady,dashEnd,recoil,shake,vertical,castAnim,stepAt,swayX,swayY;
        Vector3 dashDir;
        Transform equipment,muzzle,tip,reel;
        GameObject gun,rod,carryHands;
        CharacterController motor;
        public void Init(GameDirector director)
        {
            game=director;gameObject.layer=9;
            motor=gameObject.AddComponent<CharacterController>();motor.height=1.75f;motor.radius=.3f;motor.center=new Vector3(0,-.72f,0);motor.stepOffset=.38f;motor.slopeLimit=52;motor.skinWidth=.035f;
            var cameraObject=new GameObject("Captain camera");cameraObject.transform.SetParent(transform,false);View=cameraObject.AddComponent<Camera>();View.fieldOfView=75;View.nearClipPlane=.045f;View.farClipPlane=460;View.allowHDR=true;View.cullingMask=~(1<<30);
            View.clearFlags=CameraClearFlags.Skybox;cameraObject.tag="MainCamera";cameraObject.AddComponent<AudioListener>();
            equipment=new GameObject("Handheld tools").transform;equipment.SetParent(cameraObject.transform,false);
            Synergy=gameObject.AddComponent<BuildSynergy>();Synergy.Init(this);
            rod=ToolArt.Rod(equipment,out tip,out reel);carryHands=ToolArt.CarryHands(equipment);BuildGun();ResetForEncounter();
        }
        void BuildGun(){if(gun){gun.SetActive(false);Destroy(gun);}gun=ToolArt.Gun(equipment,weapon,out muzzle);gunMotion=gun.AddComponent<WeaponMotion>();gunMotion.Init(this);}
        public void Teleport(Vector3 pos){bool enabled=motor.enabled;motor.enabled=false;transform.position=pos;motor.enabled=enabled;vertical=0;}
        public void ResetForEncounter()
        {
            Teleport(game.World.Spawn);yaw=0;pitch=9;reloadEnd=0;dashReady=0;dashEnd=0;InvulnerableUntil=0;if(Synergy)Synergy.Reset();
            transform.rotation=Quaternion.identity;View.transform.localRotation=Quaternion.Euler(pitch,0,0);weapon=(WeaponKind)Mathf.Clamp(game.Run.selectedWeapon,0,5);if(!game.Run.Owns(weapon))weapon=WeaponKind.Revolver;for(int i=0;i<6;i++)storedAmmo[i]=Balance.Weapons[i].magazine+game.Run.magazineRelics*2;Ammo=Capacity;burstRemaining=0;kickPitch=kickYaw=slowUntil=poisonUntil=0;fireAt=Time.time+.3f;BuildGun();SetRod(true);
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
            if(Input.GetKeyDown(KeyCode.Space))Jump();
            vertical-=19*Time.deltaTime;float pace=(1+game.Run.bootsLevel*.06f)*(Time.time<slowUntil?.65f:1)*(Time.time<game.TonicUntil?1.25f:1);motor.Move((move*5.2f*pace+Vector3.up*vertical)*Time.deltaTime);
            if(Time.time<poisonUntil&&Time.time>poisonAt){poisonAt=Time.time+1;TakeDamage(2.5f);}bloom=Mathf.MoveTowards(bloom,0,Time.deltaTime*.028f);
            if(transform.position.y<-.75f){Teleport(game.World.Spawn);game.Notice("海流把你送回岸边 · 走码头更安全",3);if(game.State==VoyageState.Combat)TakeDamage(8);}
            if(move.sqrMagnitude>.2f&&motor.isGrounded&&Time.time>stepAt){stepAt=Time.time+.39f;game.Audio.Cue("step");}
            recoil=Mathf.SmoothDamp(recoil,0,ref recoilVelocity,.07f);kickPitch=Mathf.SmoothDamp(kickPitch,0,ref kickVelocity,.16f);kickYaw=Mathf.SmoothDamp(kickYaw,0,ref kickYawVelocity,.13f);shake=Mathf.MoveTowards(shake,0,Time.deltaTime*3);castAnim=Mathf.MoveTowards(castAnim,0,Time.deltaTime*2);
            float bob=move.magnitude>.1f?Mathf.Sin(Time.time*11)*.012f:Mathf.Sin(Time.time*1.8f)*.003f;
            View.transform.localRotation=Quaternion.Euler(pitch-kickPitch,kickYaw,game.Log.shake?Mathf.Sin(Time.time*35)*shake:0);View.transform.localPosition=new Vector3(0,bob,0);
            bool ads=Input.GetMouseButton(1)&&!RodEquipped&&!HeldFish;float tilt=Reloading?Mathf.Sin((reloadEnd-Time.time)/Mathf.Max(.1f,reloadDuration)*Mathf.PI)*43:0;
            equipment.localRotation=Quaternion.Euler(-recoil*12-castAnim*21+(RodEquipped?game.CastCharge*-12+game.Tension*5:0),tilt*.5f,tilt*.65f+Mathf.Sin(Time.time*3)*bob*40);
            Vector3 desired=new Vector3(swayX+(ads?-.245f:0),swayY+bob+(ads?.17f:0),-recoil*.12f+(ads?.04f:0));equipment.localPosition=Vector3.Lerp(equipment.localPosition,desired,Time.deltaTime*16);
            if(game.State==VoyageState.Fishing&&Input.GetMouseButton(0))reel.Rotate(Vector3.right,Time.deltaTime*420,Space.Self);
            View.fieldOfView=Mathf.Lerp(View.fieldOfView,ads?52:75+recoil*.7f,Time.deltaTime*11);
            if(!game.Automation){
                if(Input.GetKeyDown(KeyCode.Alpha1)){StowHeld();SetRod(true);}if(Input.GetKeyDown(KeyCode.Alpha2))Equip(WeaponKind.Revolver);if(Input.GetKeyDown(KeyCode.Alpha3))Equip(WeaponKind.Scattergun);if(Input.GetKeyDown(KeyCode.Alpha4))Equip(WeaponKind.Harpoon);if(Input.GetKeyDown(KeyCode.Alpha5))Equip(WeaponKind.Carbine);if(Input.GetKeyDown(KeyCode.Alpha6))Equip(WeaponKind.BurstRifle);if(Input.GetKeyDown(KeyCode.Alpha7))Equip(WeaponKind.ArcCaster);
                if(Input.GetKeyDown(KeyCode.F))StowHeld();if(Input.GetKeyDown(KeyCode.Q)&&HeldFish)ThrowHeld();
            }
            if(burstRemaining>0&&Time.time>=burstAt&&!RodEquipped&&!Reloading){if(Ammo>0)FireRound();burstRemaining--;burstAt=Time.time+.11f/game.Run.FireRateMultiplier;if(burstRemaining==0)fireAt=Time.time+.3f/game.Run.FireRateMultiplier;}
            if(reloadEnd>0&&Time.time>=reloadEnd){Ammo=Capacity;storedAmmo[(int)weapon]=Ammo;reloadEnd=0;game.Audio.Cue("ready");}
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
        public bool Jump(){if(!game.IsPlaying||game.Paused||!motor.enabled||!motor.isGrounded)return false;vertical=6.3f;return true;}
        public void Dash(Vector3 direction){if(Time.time<dashReady)return;dashDir=direction.normalized;dashEnd=Time.time+.19f;InvulnerableUntil=Time.time+.3f;dashReady=Time.time+game.Run.DodgeCooldown*(Time.time<game.TonicUntil?.5f:1)*(game.Run.HasKeystone(8)&&game.Run.health<game.Run.MaxHealth*.35f?.8f:1);Synergy.Dash();game.Audio.Cue("dash");}
        public void Equip(WeaponKind kind)
        {
            if(!game.Run.Owns(kind)){game.Notice("到达新岛后，在工坊购买这件武器",2);return;}
            if(!StowHeld())return;SetRod(false);if(kind==weapon)return;
            storedAmmo[(int)weapon]=Ammo;weapon=kind;game.Run.selectedWeapon=(int)kind;reloadEnd=0;burstRemaining=0;Ammo=storedAmmo[(int)kind];fireAt=Time.time+.32f;recoil=.7f;BuildGun();game.Audio.Cue("equip");
        }
        public void Refill(){for(int i=0;i<6;i++)storedAmmo[i]=Balance.Weapons[i].magazine+game.Run.magazineRelics*2;Ammo=Capacity;reloadEnd=0;burstRemaining=0;}
        public void Reload()
        {
            if(Reloading){if(quickReloadAttempted)return;quickReloadAttempted=true;if(ReloadProgress>=.55f&&ReloadProgress<=.72f){reloadEnd=Time.time+.12f;game.Audio.Cue("ready");game.Notice("精准装填 · 上膛",1);}return;}
            if(Ammo>=Capacity)return;burstRemaining=0;quickReloadAttempted=false;reloadDuration=Balance.Weapons[(int)weapon].reload/((1+Mathf.Min(4,game.Run.hasteRelics)*.04f)*Synergy.ReloadSpeed);reloadEnd=Time.time+reloadDuration;game.Audio.Cue("reload");
        }
        public void ApplyStatus(SeaTrait trait){if(trait==SeaTrait.Frost)slowUntil=Time.time+2.5f/(1+game.Run.bootsLevel*.35f);if(trait==SeaTrait.Venom)poisonUntil=Time.time+3;if(trait==SeaTrait.Electric)slowUntil=Time.time+.65f;}
        public bool Fire()
        {
            if(!game.IsPlaying||game.State==VoyageState.Fishing||RodEquipped||HeldFish||game.Paused||Reloading||Time.time<fireAt||burstRemaining>0)return false;
            if(Ammo<=0){Reload();return false;}FireRound();if(weapon==WeaponKind.BurstRifle){burstRemaining=Mathf.Min(2,Ammo);burstAt=Time.time+.11f/game.Run.FireRateMultiplier;}return true;
        }
        void FireRound()
        {
            var w=Balance.Weapons[(int)weapon];Ammo--;storedAmmo[(int)weapon]=Ammo;fireAt=Time.time+w.interval/game.Run.FireRateMultiplier;
            gunMotion.Shot();float shotBoost=Synergy.ShotMultiplier();bool shotHit=false,shotWeak=false;
            bool ads=Input.GetMouseButton(1);float kick=weapon==WeaponKind.Revolver?1.6f:weapon==WeaponKind.Scattergun?3.6f:weapon==WeaponKind.Harpoon?3:weapon==WeaponKind.Carbine?.65f:1.1f;
            recoil=weapon==WeaponKind.Carbine?.36f:.85f;kickPitch+=kick*Mathf.Max(.25f,1-game.Run.brakeLevel*.18f)*(ads?.72f:1);kickYaw+=Random.Range(-.32f,.32f)*kick;
            bloom=Mathf.Min(.018f,bloom+(weapon==WeaponKind.Carbine?.0023f:.0011f));game.Audio.Cue(weapon==WeaponKind.Scattergun?"shotgun":weapon==WeaponKind.Harpoon?"harpoon":weapon==WeaponKind.ArcCaster?"arc":weapon==WeaponKind.Carbine?"carbine":"shot");
            var flash=Shape.Part("Muzzle flash",PrimitiveType.Sphere,null,muzzle.position,new Vector3(.11f,.11f,.27f),new Color(1,.73f,.26f),false,true);flash.transform.rotation=muzzle.rotation;Destroy(flash,.035f);
            var light=flash.AddComponent<Light>();light.color=new Color(1,.68f,.25f);light.range=3;light.intensity=1.5f;
            if(weapon!=WeaponKind.Harpoon&&weapon!=WeaponKind.ArcCaster){var shell=Shape.Part("Ejected brass",PrimitiveType.Cylinder,null,equipment.position+View.transform.forward*.45f+View.transform.right*.2f,new Vector3(.019f,.032f,.019f),new Color(.7f,.53f,.25f));var body=shell.AddComponent<Rigidbody>();body.velocity=View.transform.right*1.8f+Vector3.up*1.3f;body.angularVelocity=Random.insideUnitSphere*12;shell.AddComponent<BrassCasing>();}
            for(int i=0;i<w.pellets;i++){
                float spread=(w.spread+(weapon==WeaponKind.Carbine?bloom:0))*(ads?.5f/(1+game.Run.scopeLevel*.25f):1);
                var d=(View.transform.forward+View.transform.right*Random.Range(-spread,spread)+View.transform.up*Random.Range(-spread,spread)).normalized;
                RaycastHit hit;Vector3 endpoint=View.transform.position+d*100;
                if(Physics.Raycast(View.transform.position,d,out hit,120,~((1<<9)|(1<<30)))){
                    endpoint=hit.point;var e=hit.collider.GetComponentInParent<Enemy>();var target=hit.collider.GetComponentInParent<EncounterTarget>();
                    if(target){target.Hit(w.damage*game.Run.DamageMultiplier*shotBoost*(weapon==WeaponKind.Harpoon?1.5f:1));shotHit=true;}
                    else if(e){bool crit=game.Rng.NextDouble()<game.Run.CriticalChance;bool weak=hit.collider.GetComponent<HitRegion>()!=null;float falloff=weapon==WeaponKind.Scattergun?Mathf.Lerp(1,.22f,Mathf.InverseLerp(8,27,hit.distance)):1;float damage=w.damage*game.Run.DamageMultiplier*(crit?1.75f:1)*falloff*shotBoost;
                        if(weak&&hit.distance>12&&game.Run.HasKeystone(6))damage*=1.35f;
                        e.Hit(damage,weak,crit);bool landed=e.LastDamageApplied>0;shotHit|=landed;shotWeak|=weak&&landed;if(landed)Synergy.OnHit(e,weak,damage);
                        if(weapon==WeaponKind.ArcCaster){int chains=0;foreach(var other in game.Enemies.ToArray())if(other&&other!=e&&Vector3.Distance(other.transform.position,hit.point)<8&&chains<2+game.Run.chainLevel){game.Tracer(hit.point,other.transform.position,Color.cyan);other.Hit(damage*.5f);other.Stun(.25f);chains++;}}
                        game.Effect(hit.point,new Color(.84f,.95f,.74f),4,.035f);
                    }else{var loot=hit.collider.GetComponentInParent<FishLoot>();if(loot)loot.Push(d*3);game.Effect(hit.point,new Color(.66f,.58f,.4f),4,.035f);}
                }
                game.Tracer(muzzle.position,endpoint,weapon==WeaponKind.ArcCaster?Color.cyan:weapon==WeaponKind.Harpoon?new Color(.5f,.87f,.8f):new Color(1,.83f,.53f));
            }
            Synergy.RegisterShot(shotHit,shotWeak);
        }
        public void TakeDamage(float amount)
        {
            if(game.State!=VoyageState.Combat||game.Paused||amount<=0)return;
            if(Time.time<InvulnerableUntil){if(Time.time<dashEnd+.11f)Synergy.AvoidedAttack();return;}
            game.Run.health=Mathf.Max(0,game.Run.health-amount*game.Run.DamageTakenMultiplier);InvulnerableUntil=Time.time+.6f;shake=game.Log.shake?1:0;game.UI.DamageFlash();game.Audio.Cue("hurt");if(game.Run.health<=0)game.EndVoyage(false);
        }
    }
}
