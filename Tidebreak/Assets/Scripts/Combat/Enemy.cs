using UnityEngine;
namespace Tidebreak
{
    public class Enemy : MonoBehaviour
    {
        public GameDirector game;
        public SpeciesDefinition Spec;
        public CreatureKind kind;
        public bool elite,dead,lastWeak,Summoned,NaturalHook,Minion;
        public int quality,phase=1;
        public int CaughtHabitat=-1;
        public int CaughtIsland=-1;
        int inheritedCombatIsland=-1;
        public int CombatIsland {get{return inheritedCombatIsland>=0?inheritedCombatIsland:NaturalHook&&CaughtIsland>=0?Mathf.Clamp(CaughtIsland,0,8):Spec.island;}}
        public int CatchValue {get{return NaturalHook&&!IsBoss?25+CombatIsland*7+Spec.id%12*2:Spec.value;}}
        public float weight,health,maxHealth,exposedUntil;
        public bool IsBoss {get{return Spec!=null&&Spec.boss;}}
        public bool Exposed {get{return Encounter?Encounter.Recovering:!IsBoss||Time.time<exposedUntil;}}
        public bool Airborne {get{return !IsBoss&&Time.time<launchUntil+.6f;}}
        public int AttackExecutions {get;private set;}
        public EncounterDirector Encounter {get;private set;}
        public EliteTactics Elite {get;private set;}
        public CreatureMotion Motion {get;private set;}
        public RegionalTactics Regional {get;private set;}
        public bool Stunned {get{return Time.time<stunUntil;}}
        public float LastDamageApplied {get;private set;}
        public string DisplayName {get{return Spec.name;}}
        public float AttackWindup {get{return normalWindup>0?Mathf.Clamp01(1-(normalWindup-Time.time)/windupLength):0;}}
        public Vector3 LockedAim {get{return attackAim;}}
        public string Telegraph {get{return Encounter?Encounter.Cue:Elite?Elite.Cue:healing?"愈合蓄势 · 射击打断":normalWindup>0?ExpeditionContent.AttackNames[(int)Spec.attack]+" · "+ExpeditionContent.Counters[(int)Spec.attack]:Time.time<normalRecovery?"出招收势 · 弱点机会":"";}}
        Transform rig,tail;Vector3 origin,chargeDirection;
        Rigidbody body;Renderer[] renderers;MaterialPropertyBlock properties;Color[] baseTints;
        float age,nextAttack,flash,stagger,staggerReady,hopAt,launchUntil,chargeUntil,slowUntil,stunUntil,burnUntil,burnTick,blinkAt,impulseReady;
        bool healing;float healAt,normalWindup,windupLength,normalRecovery,visualDepth;Vector3 attackAim;
        float routeAt;Vector3[] route;int routeIndex;
        Vector3 LandDirection(Vector3 to)
        {
            if(Time.time>=routeAt){routeAt=Time.time+.8f;route=null;routeIndex=0;
                if(transform.position.z<10&&to.magnitude>5){
                    for(int i=1;i<5;i++)if(game.World.GroundAt(Vector3.Lerp(transform.position,game.Player.transform.position,i/5f))<0){route=game.World.NavigationRoute(transform.position,game.Player.transform.position);break;}
                }
            }
            if(route==null)return to.normalized;
            while(routeIndex<route.Length&&GameDirector.FlatDistance(transform.position,route[routeIndex])<1.7f)routeIndex++;
            if(routeIndex>=route.Length)return to.normalized;
            Vector3 direction=route[routeIndex]-transform.position;direction.y=0;return direction.normalized;
        }
        public void Init(GameDirector director,CreatureKind type,bool isElite,Vector3 position)
        {InitSpecies(director,ExpeditionContent.Species[type>=CreatureKind.Crab?108:(int)type],isElite,position);}
        public void InitSpecies(GameDirector director,SpeciesDefinition species,bool isElite,Vector3 position)
        {
            game=director;Spec=species;elite=isElite;origin=position;transform.position=position;
            kind=Spec.id==117?CreatureKind.Kraken:Spec.id==118?CreatureKind.WhiteWhale:IsBoss?CreatureKind.Crab:Spec.body==BodyFamily.Puffer?CreatureKind.Puffer:CreatureKind.Snapper;
            double rarity=game.Rng.NextDouble();quality=IsBoss?0:rarity<(game.Chummed?.1:.045)?2:rarity<.16?1:0;
            weight=(float)(game.Rng.NextDouble()*2.2+1.3)*(elite?1.7f:1)*(quality==1?2.1f:1)*(1+Spec.island*.15f);
            maxHealth=Spec.hp*(IsBoss?1:elite?3.65f:1.38f)*(game.Run.route==RouteKind.Hunt?1.12f:1)*(quality==1?1.25f:1);health=maxHealth;
            rig=SpeciesArt.Build(transform,Spec,elite);if(quality==1)rig.localScale*=1.3f;
            foreach(var t in rig.GetComponentsInChildren<Transform>())if(t.name=="Tail fin")tail=t;
            renderers=GetComponentsInChildren<Renderer>();properties=new MaterialPropertyBlock();baseTints=new Color[renderers.Length];for(int i=0;i<renderers.Length;i++){renderers[i].GetPropertyBlock(properties);baseTints[i]=properties.isEmpty?renderers[i].sharedMaterial.GetColor("_Color"):properties.GetColor("_Color");}
            if(!IsBoss){body=gameObject.AddComponent<Rigidbody>();body.mass=elite?4.2f:2.7f;body.drag=.45f;body.angularDrag=3;body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;body.constraints=RigidbodyConstraints.FreezeRotation;Regional=gameObject.AddComponent<RegionalTactics>();Regional.Init(this);}
            nextAttack=Time.time+(IsBoss?3.5f:1.25f);hopAt=Time.time+1;blinkAt=Time.time+7;game.Enemies.Add(this);Tint(false);
            Motion=gameObject.AddComponent<CreatureMotion>();Motion.Init(this,rig);
            if(IsBoss){Encounter=gameObject.AddComponent<EncounterDirector>();Encounter.Init(this);}
            else if(elite){Elite=gameObject.AddComponent<EliteTactics>();Elite.Init(this);}
            else gameObject.AddComponent<CreatureIntent>().Init(this);
        }
        public void LaunchToward(Vector3 point)
        {
            if(IsBoss)return;float t=1.3f;point.y=game.World.GroundAt(point)+.7f;if(point.y<0){point=game.Player.transform.position+game.Player.transform.forward;point.y=game.World.GroundAt(point)+.7f;}
            body.velocity=(point-transform.position-Physics.gravity*t*t*.5f)/t;launchUntil=Time.time+t;game.Splash(transform.position);
        }
        public void SetCatchOrigin(int island,int habitat,bool natural)
        {
            // Migrants retain their anatomy and ecology, but fight on the current
            // island's progression budget rather than importing endgame numbers.
            int previousIsland=CombatIsland;
            NaturalHook=natural;CaughtIsland=Mathf.Clamp(island,0,8);CaughtHabitat=habitat;
            if(IsBoss)return;
            float scale=(1+CombatIsland*.19f)/(1+previousIsland*.19f);maxHealth*=scale;health*=scale;
        }
        public void InheritCombatBudget(Enemy source)
        {
            if(!source||IsBoss)return;int previousIsland=CombatIsland;
            inheritedCombatIsland=source.CombatIsland;
            float scale=(1+CombatIsland*.19f)/(1+previousIsland*.19f);maxHealth*=scale;health*=scale;
        }
        void Update()
        {
            if(dead||!game||game.State!=VoyageState.Combat||game.Paused)return;age+=Time.deltaTime;
            if(Time.time<burnUntil&&Time.time>burnTick){burnTick=Time.time+.5f;Hit(3+game.Run.fireRelics*2,false,false,false);if(dead)return;}
            float slow=Time.time<slowUntil?.48f:1;if(Time.time<stunUntil)return;
            Vector3 to=game.Player.transform.position-transform.position;to.y=0;
            if(to.sqrMagnitude>.01f)transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(to)*Quaternion.Euler(0,Encounter?Encounter.FacingOffset:0,0),Time.deltaTime*(Elite&&Elite.Windup>0?.8f:3.6f));
            if(IsBoss){visualDepth=Mathf.MoveTowards(visualDepth,Encounter?Encounter.Submerge:0,Time.deltaTime*4.6f);transform.position=origin+new Vector3(Mathf.Sin(age*.24f)*4,Mathf.Sin(age*.8f)*.24f-Mathf.Max(0,1-age/1.4f)*4-visualDepth,Mathf.Cos(age*.3f)*1.4f)+(Encounter?Encounter.PoseOffset:Vector3.zero);float wind=Encounter?Encounter.Windup:0;rig.localEulerAngles=new Vector3(wind*-10+Mathf.Sin(age*2)*2,0,Mathf.Sin(age)*2);
                if(Spec.id==117)for(int i=0;i<rig.childCount;i++){var arm=rig.GetChild(i);if(arm.name.StartsWith("Tentacle")){int index=int.Parse(arm.name.Substring(9));arm.localRotation=Quaternion.Euler(Mathf.Sin(age*1.4f+index)*8-wind*20,index*45,Mathf.Sin(age+index)*8);}}
            }else if(Time.time>launchUntil){
                bool floating=Spec.body==BodyFamily.Jelly||Spec.body==BodyFamily.Ray||Spec.body==BodyFamily.Squid;
                if(Time.time<chargeUntil){body.velocity=chargeDirection*(8+CombatIsland*.35f);if(to.magnitude<1.1f){DealContact();chargeUntil=0;}}
                else if(floating){body.useGravity=false;Vector3 desired=to.normalized*(to.magnitude>6?Spec.speed:to.magnitude<3?-2:0)+Vector3.Cross(Vector3.up,to.normalized)*Mathf.Sin(age)*1.5f;desired.y=(game.World.GroundAt(transform.position)+2.3f+Mathf.Sin(age*2)*.35f-transform.position.y)*3;body.velocity=Vector3.Lerp(body.velocity,desired*slow*(normalWindup>0||Elite&&Elite.Windup>0?.3f:1),Time.deltaTime*4);}
                else if(normalWindup==0&&(!Elite||Elite.Windup==0)&&Time.time>hopAt&&transform.position.y-game.World.GroundAt(transform.position)<1.25f){body.useGravity=true;Vector3 dir=to.magnitude<3&&Spec.attack!=AttackStyle.Bite?-to.normalized:LandDirection(to);float speed=Spec.speed*slow;body.velocity=dir*speed+Vector3.up*(Spec.body==BodyFamily.Crab?2.1f:3.8f);hopAt=Time.time+(Spec.trait==SeaTrait.Frenzy&&health<maxHealth*.5f?.6f:1.08f);}
                float wind=Mathf.Clamp01(1-(nextAttack-Time.time)/.8f);rig.localEulerAngles=new Vector3(Mathf.Sin(age*7)*4-wind*18,0,Mathf.Sin(age*9)*8);
                if(transform.position.y<-1.6f){var p=game.World.Layout.ProjectToRoute(transform.position);p.y=game.World.GroundAt(p)+1;transform.position=p;body.velocity=Vector3.up*2;}
                if(Spec.trait==SeaTrait.Blinker&&Time.time>blinkAt){blinkAt=Time.time+7;Vector3 p=transform.position+transform.right*(Mathf.Sin(age)>0?3:-3);if(game.World.GroundAt(p)>0){game.Effect(transform.position,Spec.color,8,.1f);p.y=game.World.GroundAt(p)+1;transform.position=p;}}
            }
            if(tail)tail.localRotation=Quaternion.Euler(0,Mathf.Sin(age*12)*22,0);
            if(healing&&Time.time>healAt){healing=false;health=Mathf.Min(maxHealth,health+maxHealth*.12f);foreach(var e in game.Enemies)if(e!=this)e.health=Mathf.Min(e.maxHealth,e.health+e.maxHealth*.08f);game.Effect(transform.position,Color.green,14,.13f);}
            if(!Encounter&&!Elite){
                if(normalWindup>0&&Time.time>=normalWindup){normalWindup=0;Attack();normalRecovery=Time.time+.75f;nextAttack=Time.time+Mathf.Max(.9f,Spec.tempo*.86f-windupLength)/slow;}
                else if(normalWindup==0&&Time.time>=nextAttack&&Time.time>launchUntil){attackAim=game.Player.transform.position;windupLength=(Spec.attack==AttackStyle.Charge||Spec.attack==AttackStyle.Leap?.9f:Spec.attack==AttackStyle.Heal||Spec.attack==AttackStyle.Split?1.25f:.7f)*(game.Run.easy?1.3f:1);normalWindup=Time.time+windupLength;game.Audio.ThreatTell(transform.position,false);}
            }
            if(flash>0){flash-=Time.deltaTime;if(flash<=0)Tint(false);}
        }
        void DealContact(){float before=game.Run.health;game.Player.TakeDamage((elite?14:10)+CombatIsland*1.3f,transform.position);if(game.Run.health<before)game.Player.ApplyStatus(Spec.trait);if(Spec.trait==SeaTrait.Leech)health=Mathf.Min(maxHealth,health+maxHealth*.06f);}
        void Tint(bool hit)
        {for(int i=0;i<renderers.Length;i++)if(renderers[i]){properties.Clear();properties.SetColor("_Color",hit?new Color(1.7f,1.4f,1.15f):quality==2?new Color(1.4f,1.1f,.38f):baseTints[i]);renderers[i].SetPropertyBlock(properties);}}
        void Bolt(Vector3 target,float speed,float damage,float curve=0)
        {var g=Shape.Part("Creature water bolt",PrimitiveType.Sphere,game.Hazards,transform.position+Vector3.up*.4f,Vector3.one*(IsBoss?.43f:.26f),Spec.color,false,true);var b=g.AddComponent<SeaProjectile>();b.game=game;b.velocity=(target-g.transform.position).normalized*speed;b.damage=damage;b.trait=Spec.trait;b.returnAfter=curve;}
        public void EmitBolt(Vector3 target,float speed,float damage,float curve=0){Bolt(target,speed,damage,curve);}
        public void Lunge(Vector3 target,float seconds){if(IsBoss||!body)return;chargeDirection=target-transform.position;chargeDirection.y=0;chargeDirection.Normalize();chargeUntil=Time.time+seconds;body.velocity=Vector3.up*2.6f;}
        public void NotifyAttackExecuted(){AttackExecutions++;if(Motion)Motion.Attack();}
        void Attack()
        {
            AttackExecutions++;if(Motion)Motion.Attack();Vector3 p=IsBoss?game.Player.transform.position:attackAim;float damage=IsBoss?14+CombatIsland*1.2f:12+CombatIsland*1.2f;float delay=game.Run.easy?1.8f:1.15f;
            if(Spec.id==117){game.Warn(p,2.6f,delay,damage);game.Warn(p+game.Player.transform.right*3,2.3f,delay+.7f,damage);if(phase==2)game.Warn(p-game.Player.transform.right*3,2.3f,delay+1.4f,damage);for(int i=-1;i<=1;i++)Bolt(p+Vector3.right*i*2,10,10);}
            else switch(Spec.attack){
                case AttackStyle.Bite:if(Vector3.Distance(transform.position,p)<3.5f)game.Warn(p,1.35f,.65f,damage);else Bolt(p,7,damage*.6f);break;
                case AttackStyle.Charge:chargeDirection=(p-transform.position).normalized;chargeDirection.y=0;chargeUntil=Time.time+.75f;if(IsBoss){ThreatField.Line(game,transform.position,p,1.1f,delay,damage,Spec.color);}else{body.velocity=Vector3.up*3;game.Warn(p,1.5f,.75f,damage);}break;
                case AttackStyle.Fan:for(int i=-2;i<=2;i++)Bolt(p+game.Player.transform.right*i*1.8f,8+CombatIsland*.4f,damage*.7f);break;
                case AttackStyle.Mortar:game.Warn(p,IsBoss?2.8f:1.7f,delay+.25f,damage);if(phase==2)game.Warn(p+game.Player.transform.forward*3,2,delay+.8f,damage);break;
                case AttackStyle.Ring:ThreatField.Ring(game,transform.position,damage,Spec.color);break;
                case AttackStyle.Beam:ThreatField.Line(game,transform.position+Vector3.up,p,IsBoss?1.15f:.6f,delay,damage,Spec.color);break;
                case AttackStyle.Mine:ThreatField.Pool(game,p+game.Player.transform.forward*2,IsBoss?2.2f:1.3f,delay,damage*.45f,Spec.color,Spec.trait);break;
                case AttackStyle.Leap:game.Warn(p,IsBoss?2.6f:1.5f,delay,damage);if(!IsBoss){body.velocity=(p-transform.position)*1.1f+Vector3.up*6;launchUntil=Time.time+.8f;}break;
                case AttackStyle.Spiral:for(int i=0;i<7;i++){float a=age+i*Mathf.PI*2/7;Bolt(transform.position+new Vector3(Mathf.Cos(a),.1f,Mathf.Sin(a))*10,7,damage*.6f);}Bolt(p,8,damage*.65f);break;
                case AttackStyle.Pull:ThreatField.Vortex(game,p+Vector3.forward*2,IsBoss?4:2.3f,delay,damage,Spec.color);break;
                case AttackStyle.Heal:healing=true;healAt=Time.time+1.4f;game.Effect(transform.position,Color.green,8,.08f);Bolt(p,8,damage*.5f);break;
                case AttackStyle.Split:if(!Summoned)game.SpawnMinion(this);game.Warn(p,1.4f,delay,damage*.6f);break;
                case AttackStyle.Boomerang:for(int i=-1;i<=1;i++)Bolt(p+game.Player.transform.right*i*2,9,damage*.65f,1.2f);break;
                case AttackStyle.Burrow:game.Warn(p,1.9f,delay+.35f,damage);if(!IsBoss){var to=p+game.Player.transform.forward*2;to.y=game.World.GroundAt(to)+.8f;game.Effect(transform.position,Spec.color,10,.09f);transform.position=to;body.velocity=Vector3.up*2;}break;
            }
            if(IsBoss&&phase==2&&Spec.id!=117){var offset=game.Player.transform.right*(Mathf.Sin(age)>0?3:-3);game.Warn(p+offset,1.9f,delay+.65f,damage*.8f);}
            exposedUntil=Time.time+delay+1.65f;
            if(Regional)Regional.AfterAttack(p);
        }
        public void Slow(float seconds){slowUntil=Mathf.Max(slowUntil,Time.time+seconds);}
        public void Stun(float seconds){stunUntil=Mathf.Max(stunUntil,Time.time+(IsBoss?seconds*.4f:seconds));healing=false;if(!IsBoss&&!elite){normalWindup=0;nextAttack=Mathf.Max(nextAttack,stunUntil+.55f);}}
        public void Hit(float damage,bool weak=false,bool critical=false,bool effects=true)
        {
            LastDamageApplied=0;if(dead||game.State!=VoyageState.Combat)return;lastWeak=weak;float mult=Encounter?Encounter.DamageFactor:Elite?Elite.DamageFactor(weak):1;
            if(Regional)mult*=Regional.DamageFactor(weak);if(!IsBoss&&Spec.trait==SeaTrait.Armored&&!weak)mult*=.75f;if(weak)mult*=1.4f;if(health/maxHealth<.25f)mult*=1+Mathf.Min(3,game.Run.executeRelics)*.2f;
            float applied=damage*mult;if(Encounter)applied=Encounter.ClampPhaseDamage(applied);LastDamageApplied=applied;
            if(applied<=0){if(effects){game.UI.HitMarker(false,0);game.Audio.Cue("impact");}return;}
            health-=applied;healing=false;if(Elite)Elite.OnHit(weak);if(Regional)Regional.OnHit(weak);if(Motion)Motion.Hit();
            if(weak&&normalWindup>0&&(Spec.attack==AttackStyle.Heal||Spec.attack==AttackStyle.Split)){normalWindup=0;normalRecovery=Time.time+1.5f;nextAttack=normalRecovery;game.Notice("弱点打断 · "+ExpeditionContent.AttackNames[(int)Spec.attack]+"失败",1.6f);}
            if(effects){game.UI.HitMarker(critical||weak,applied,health<=0);game.Audio.Cue(weak?"weak":"impact");flash=.075f;Tint(true);if(game.Run.fireRelics>0)burnUntil=Time.time+2.5f;if(game.Run.iceRelics>0)Slow(1+game.Run.iceRelics*.4f);if(game.Run.shockRelics>0&&Random.value<.1f*game.Run.shockRelics)Stun(.55f);}
            // A shotgun volley is one physical impact; elites hold their ground while winding up.
            if(body&&Time.time>=impulseReady){impulseReady=Time.time+.16f;float poise=elite?(Elite&&Elite.Windup>0?.12f:.35f):1;body.AddForce((game.Player.View.transform.forward*(.9f+game.Run.staggerRelics*.3f)+Vector3.up*.1f)*poise,ForceMode.Impulse);}
            stagger+=applied*(weak?1.6f:1)*(1+game.Run.staggerRelics*.2f);if(stagger>maxHealth*(IsBoss?.2f:.45f)&&Time.time>=staggerReady){stagger=0;staggerReady=Time.time+(IsBoss?9:2.4f);exposedUntil=Time.time+(IsBoss?3:1);nextAttack=Mathf.Max(nextAttack,Time.time+(IsBoss?.5f:.5f));if(!IsBoss)Stun(.2f);}
            if(health<=0){dead=true;if(Spec.trait==SeaTrait.Volatile)game.Warn(transform.position,1.8f,1.1f,10+CombatIsland);game.EnemyKilled(this);game.Effect(transform.position,Spec.color,14,.09f);Destroy(gameObject);}
        }
    }
    public class SeaProjectile : MonoBehaviour
    {
        public GameDirector game;
        public Vector3 velocity;
        public float damage,returnAfter;
        public SeaTrait trait;
        float age;bool returned;Vector3 launchOrigin;
        float life=8;
        void Start(){launchOrigin=transform.position;var trail=gameObject.AddComponent<TrailRenderer>();trail.time=.18f;trail.minVertexDistance=.08f;trail.startWidth=.13f;trail.endWidth=.015f;trail.sharedMaterial=Shape.Mat(new Color(1,.77f,.4f),true);trail.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;}
        void Update() {
            if(game.Paused)return;
            if(game.State!=VoyageState.Combat){Destroy(gameObject);return;}
            age+=Time.deltaTime;if(returnAfter>0&&!returned&&age>returnAfter){returned=true;velocity=(launchOrigin-transform.position).normalized*velocity.magnitude;}
            Vector3 prev=transform.position; transform.position+=velocity*Time.deltaTime;
            if(Physics.Linecast(prev,transform.position,SeaWorld.GroundMask)){game.Effect(transform.position,new Color(.63f,.75f,.7f),5,.05f);Destroy(gameObject);return;}
            Vector3 player=game.Player.transform.position;
            Vector3 delta=transform.position-prev;
            float f=delta.sqrMagnitude>0?Mathf.Clamp01(Vector3.Dot(player-prev,delta)/delta.sqrMagnitude):0;
            if(Vector3.Distance(player,prev+delta*f)<.75f){float before=game.Run.health;game.Player.TakeDamage(damage,prev);if(game.Run.health<before)game.Player.ApplyStatus(trait);Destroy(gameObject);return;}
            life-=Time.deltaTime;if(life<0 || transform.position.y<-.5f)Destroy(gameObject);
        }
    }
    public class DeckWarning : MonoBehaviour
    {
        public GameDirector game;
        public float radius, delay, damage;
        float age;
        public float Remaining { get { return Mathf.Max(0,delay-age); } }
        Transform disk;
        Transform arm;
        LineRenderer boundary,progress;
        public void Init() {
            boundary=gameObject.AddComponent<LineRenderer>();boundary.useWorldSpace=true;boundary.loop=true;boundary.positionCount=64;
            boundary.startWidth=boundary.endWidth=.095f;boundary.material=Shape.Mat(new Color(1,.36f,.18f),true);
            var obj=new GameObject("Strike countdown arc");obj.transform.SetParent(transform,false);progress=obj.AddComponent<LineRenderer>();progress.useWorldSpace=true;progress.startWidth=progress.endWidth=.16f;progress.material=Shape.Mat(new Color(1,.88f,.47f),true);
            for(int i=0;i<64;i++){float a=i*Mathf.PI*2/64;var p=transform.position+new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius);p.y=Mathf.Max(-.35f,game.World.GroundAt(p))+.1f;boundary.SetPosition(i,p);}
            // Four inward chevrons communicate the full hit radius from the first frame.
            for(int i=0;i<4;i++){float a=i*Mathf.PI*.5f;var p=transform.position+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*radius*.73f;p.y=Mathf.Max(-.35f,game.World.GroundAt(p))+.11f;var mark=new GameObject("Strike radius chevron");mark.transform.SetParent(transform,false);var line=mark.AddComponent<LineRenderer>();line.material=boundary.sharedMaterial;line.startWidth=line.endWidth=.065f;line.positionCount=3;Vector3 radial=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a)),side=Vector3.Cross(radial,Vector3.up);line.SetPosition(0,p+side*.22f+radial*.2f);line.SetPosition(1,p);line.SetPosition(2,p-side*.22f+radial*.2f);}
            foreach(var e in game.Enemies)if(e&&e.kind==CreatureKind.Kraken) {
                arm=CoastalMesh.Tube("Descending Kraken arm",transform,new[]{new Vector3(0,-1,-1),new Vector3(0,2,-.7f),new Vector3(0,4,.2f),new Vector3(0,5,1)},new[]{.72f,.65f,.42f,.04f},new Color(.35f,.2f,.4f),new Color(.62f,.39f,.45f),9);
                arm.localScale=Vector3.zero;break;
            }
        }
        void Update() {
            if(game.Paused)return;
            if(game.State!=VoyageState.Combat){Destroy(gameObject);return;}
            age+=Time.deltaTime;int points=Mathf.Clamp(Mathf.CeilToInt(age/Mathf.Max(.1f,delay)*64),2,64);progress.positionCount=points;for(int i=0;i<points;i++)progress.SetPosition(i,boundary.GetPosition(i)+Vector3.up*.025f);
            if(age>delay*.78f)Shape.TintLine(boundary,Color.Lerp(new Color(1,.2f,.12f),new Color(1,.83f,.5f),.5f+.5f*Mathf.Sin(age*32)));
            if(arm){arm.localScale=Vector3.one*Mathf.Clamp01(age/delay*2);arm.localRotation=Quaternion.Euler(Mathf.Lerp(-45,60,Mathf.Pow(age/delay,5)),0,0);}
            if(age<delay)return;
            Vector3 p=game.Player.transform.position-transform.position;p.y=0;
            if(p.magnitude<radius)game.Player.TakeDamage(damage,transform.position);
            game.Effect(transform.position+Vector3.up*.4f,new Color(1,.45f,.22f),18,.23f);game.Audio.Cue("splash");Destroy(gameObject);
        }
    }
    public class Fleck : MonoBehaviour
    {
        public Vector3 velocity;
        public float life=.5f;
        float initial;
        Vector3 scale;
        void Start(){initial=life;scale=transform.localScale;}
        void Update(){life-=Time.deltaTime;transform.position+=velocity*Time.deltaTime;velocity+=Vector3.down*6*Time.deltaTime;transform.localScale=scale*Mathf.Max(0,life/initial);if(life<=0)Destroy(gameObject);}
    }
    public class SurfaceRipple : MonoBehaviour
    {
        LineRenderer ring;float age;
        void Start(){ring=gameObject.AddComponent<LineRenderer>();ring.useWorldSpace=false;ring.loop=true;ring.positionCount=48;ring.material=Shape.Mat(new Color(.58f,.77f,.73f));}
        void Update(){age+=Time.deltaTime;float r=.12f+age*2.2f;ring.startWidth=ring.endWidth=.065f*(1-age/1.2f);for(int i=0;i<48;i++){float a=i*Mathf.PI*2/48;ring.SetPosition(i,new Vector3(Mathf.Cos(a)*r,0,Mathf.Sin(a)*r));}if(age>=1.2f)Destroy(gameObject);}
    }
}
