using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Tidebreak
{
    // A visible consequence layer, deliberately outside the statically batched
    // scenery. Existing quest/save state is its only authority.
    public sealed class IslandRestoration : MonoBehaviour
    {
        sealed class Motion
        {
            public Transform item;
            public Vector3 origin, end, scale;
            public Quaternion rotation;
            public int kind;
            public float phase, amount;
        }
        SeaWorld world;
        GameDirector game;
        Transform display;
        readonly List<Motion> motion=new List<Motion>();
        readonly List<Material> ownedMaterials=new List<Material>();
        readonly List<LineRenderer> streams=new List<LineRenderer>();
        float nextCheck, createdAt;
        int previousKey=-1, previousLevel=-1, island, level;
        string pendingNotice;
        readonly Color timber=new Color(.34f,.25f,.16f), metal=new Color(.14f,.25f,.28f), paper=new Color(.88f,.84f,.68f), brass=new Color(.75f,.53f,.25f), clean=new Color(.37f,.79f,.74f), warm=new Color(1,.69f,.3f);

        public int VisibleLevel {get{return level;}}
        public Transform DisplayRoot {get{return display;}}
        public void Init(SeaWorld owner)
        {
            world=owner;game=GameDirector.Instance;island=owner.Region+1;
            Refresh();nextCheck=Time.unscaledTime+.5f;
        }
        void Update()
        {
            if(!world)return;if(!game)game=GameDirector.Instance;if(!game)return;
            if(Time.unscaledTime>=nextCheck){nextCheck=Time.unscaledTime+.5f;Refresh();}
            if(game.Paused)return;
            Animate();
            if(!string.IsNullOrEmpty(pendingNotice)&&game.State==VoyageState.Sailing&&!game.CinematicActive&&Time.unscaledTime>=game.MessageUntil){game.Notice(pendingNotice,5);pendingNotice=null;}
        }
        void Refresh()
        {
            if(!world||!game||game.Run==null)return;
            var run=game.Run;
            // Legendary arenas borrow Region 8. They are not a tenth/eleventh
            // mirror-temple visit, and must never replay its departure tableau.
            if(run.stage<1||run.stage>9){if(display)display.gameObject.SetActive(false);pendingNotice=null;return;}
            int step=0,explored=0;bool boss=false;
            if(run.stage==island){step=run.questStep;explored=run.exploredMask;boss=run.bossCleared;}
            else if(run.islands!=null&&island<=run.islands.Length&&run.islands[island-1]!=null){var old=run.islands[island-1];step=old.step;explored=old.explored;boss=old.boss;}
            int newLevel=boss||step>=4?2:step>=3?1:0;
            int key=newLevel*2+((explored&4)!=0?1:0);
            if(key==previousKey){if(display)display.gameObject.SetActive(true);return;}
            if(previousLevel>=0&&newLevel>previousLevel&&run.stage==island)pendingNotice=Summary(island,newLevel);
            previousLevel=level=newLevel;previousKey=key;
            ClearDisplay();createdAt=Time.time;
            display=new GameObject("Island consequence "+island+" / level "+level).transform;display.SetParent(world.transform,false);
            switch(island){case 1:BuildLighthouse();break;case 2:BuildCoral();break;case 3:BuildCleanWater();break;case 4:BuildStarlight();break;case 5:BuildNames();break;case 6:BuildRescue();break;case 7:BuildGrounding();break;case 8:BuildForge();break;case 9:BuildDeparture();break;}
            if((explored&4)!=0)BuildEvidenceCopy();
        }
        public static string Summary(int stage,int state)
        {
            string[] repaired={"归航灯恢复供电，光束重新落在岸边。","珊瑚开始轮流回应，不再只重复一个音。","清水流回净水渠，救援舟已经靠岸。","断开的星光链重新接通，石盘记录真正的方位。","黑匣副本已保住，七块船名终于能完整展开。","救生舱打开了。阿澜站在伊芙身边，手里还握着船票。","接地阵列按顺序导流，雷电有了通往地下的路。","断潮钥已成形，炉台留下适合继续工作的暖色。","七艘船的航灯依次亮起，它们正在等待自己的出港时刻。"};
            string[] released={"航道不再被封住。灯塔开始守望真正的远方。","海面恢复各自的声音，音贝不必再整齐唱同一首歌。","净水舟驶向下游。它没有被逆流拖回来。","日轮与星图持续转动；这次，明天的方向会不同。","七船名旗升起。洛恩把所有名字留在可以看见的地方。","封住港湾的冰脊松开，救生舱外升起温暖的归航灯。","避雷阵列停止乱闪，稳定的电流让船坞重新亮起。","成品钥匙放上出航架，停下的炉火等待下一位有需要的人。","七艘小船带着各自的灯驶离。航路终于不再把它们带回同一天。"};
            return (state>1?released:repaired)[Mathf.Clamp(stage-1,0,8)];
        }
        Vector3 Ground(Vector3 at){at.y=world.Height(at.x,at.z)+.045f;return at;}
        Transform Group(string name,Vector3 at)
        {var root=new GameObject(name).transform;root.SetParent(display,false);root.localPosition=at;return root;}
        Transform Local(Transform parent,string name,Vector3 at)
        {var root=new GameObject(name).transform;root.SetParent(parent,false);root.localPosition=at;return root;}
        GameObject Block(Transform parent,string name,Vector3 at,Vector3 size,Color color)
        {return Shape.Part(name,PrimitiveType.Cube,parent,at,size,color);}
        void Move(Transform item,int kind,float phase=0,float amount=1,Vector3? end=null)
        {motion.Add(new Motion{item=item,origin=item.localPosition,rotation=item.localRotation,scale=item.localScale,kind=kind,phase=phase,amount=amount,end=end??Vector3.zero});}
        LineRenderer Ribbon(Transform parent,string name,Vector3[] points,float width,Color color,bool transparent=false)
        {
            var obj=new GameObject(name);obj.transform.SetParent(parent,false);var line=obj.AddComponent<LineRenderer>();line.useWorldSpace=false;line.positionCount=points.Length;line.SetPositions(points);line.startWidth=line.endWidth=width;
            if(transparent){var material=new Material(Shader.Find("Sprites/Default"));material.color=color;ownedMaterials.Add(material);line.sharedMaterial=material;}
            else line.sharedMaterial=Shape.Mat(color,level>0);
            return line;
        }
        void Lantern(Transform parent,Vector3 at,float size,Color color)
        {
            var frame=Local(parent,"Framed navigation lantern",at);
            Block(frame,"Lantern foot",Vector3.zero,new Vector3(size,.08f,size),metal);
            Block(frame,"Lantern glass",Vector3.up*size*.6f,new Vector3(size*.72f,size,size*.72f),color).GetComponent<Renderer>().sharedMaterial=Shape.Mat(color,true);
            Block(frame,"Lantern cap",Vector3.up*size*1.2f,new Vector3(size,.08f,size),metal);
            for(int s=-1;s<=1;s+=2)Shape.Beam(frame,new Vector3(s*size*.4f,0,size*.4f),new Vector3(s*size*.4f,size*1.2f,size*.4f),.027f,metal);
        }
        void BuildLighthouse()
        {
            var basePoint=Ground(new Vector3(0,0,-34));var lens=Group("Restored lighthouse optical assembly",basePoint+Vector3.up*11.4f);
            CoastalMesh.Ring(lens,Vector3.zero,1.12f,.07f,brass,Quaternion.Euler(90,0,0));
            if(level==0){Shape.Beam(lens,new Vector3(-.7f,-.7f,-.75f),new Vector3(.7f,.7f,-.75f),.13f,metal);return;}
            var rotor=Local(lens,"Shore-facing navigation lens",Vector3.zero);
            for(int i=0;i<2;i++){
                var line=Ribbon(rotor,"Restored guiding beam",new[]{new Vector3(i==0?-.65f:.65f,0,0),new Vector3(i==0?-3.5f:3.5f,-7,level==1?18:36)},.16f,new Color(1,.84f,.46f,.32f),true);streams.Add(line);
            }
            Move(rotor,1,0,level==1?7:18);
            Lantern(lens,new Vector3(0,-.25f,.85f),.48f,warm);
            if(level==2){for(int i=0;i<3;i++){Vector3 p=Ground(new Vector3(-3.6f,0,-15+i*7));var marker=Group("Safe harbor approach lamp",p);Shape.Beam(marker,Vector3.zero,Vector3.up*.7f,.08f,timber);Lantern(marker,Vector3.up*.7f,.22f,warm);}}
        }
        void BuildCoral()
        {
            Vector3 point=Ground(world.SitePoints[0]+new Vector3(-3.5f,0,-.8f));var bed=Group("Listening coral garden",point);
            for(int i=0;i<5;i++){
                float x=(i-2)*.68f;Shape.Rock(bed,new Vector3(x,.02f,0),new Vector3(.65f,.18f,.55f),metal,6);
                var shell=Local(bed,"Resonance shell "+(i+1),new Vector3(x,.18f,0));
                var mesh=new CoastalMesh();for(int rib=0;rib<7;rib++){float a=(rib-3)*.22f,b=(rib-2)*.22f;mesh.Tri(Vector3.zero,new Vector3(Mathf.Sin(a)*.55f,.04f,Mathf.Cos(a)*.7f),new Vector3(Mathf.Sin(b)*.55f,.04f,Mathf.Cos(b)*.7f),Color.Lerp(paper,world.Definition.accent,rib*.055f));}
                mesh.Build("Fan shell lower valve",shell);
                var lid=Local(shell,"Opening shell upper valve",Vector3.up*.016f);mesh.Build("Shell upper valve",lid);
                lid.localRotation=Quaternion.Euler(level==0?0:-30,0,0);if(level>0)Move(lid,2,i*(level==1?.7f:1.31f),level==1?13:24);
                Shape.Part("Pearlescent shell ridge",PrimitiveType.Sphere,shell,new Vector3(0,.025f,.28f),new Vector3(.2f,.055f,.18f),brass);
            }
            if(level==2)for(int i=0;i<3;i++){var reed=Local(bed,"New coral branch",new Vector3((i-1)*1.2f,.15f,1.1f));Shape.Beam(reed,Vector3.zero,Vector3.up*.8f,.07f,world.Definition.accent);Shape.Beam(reed,new Vector3(0,.4f,0),new Vector3(.35f,.7f,0),.05f,world.Definition.accent);Move(reed,3,i,.7f);}
        }
        void BuildCleanWater()
        {
            var station=Group("Recovered filtration outflow",Ground(world.SitePoints[1]+new Vector3(3.7f,0,-1)));
            for(int s=-1;s<=1;s+=2)Block(station,"Flume bank",new Vector3(s*.66f,.27f,.5f),new Vector3(.2f,.5f,3.6f),timber);
            Block(station,"Filter lattice",new Vector3(0,.37f,-1.12f),new Vector3(1.2f,.15f,.13f),brass);
            for(int i=0;i<6;i++)Block(station,"Filter reed",new Vector3((i-2.5f)*.18f,.2f,-1.15f),new Vector3(.06f,.6f,.06f),timber);
            if(level==0){Block(station,"Stopped dark water",new Vector3(0,.13f,.4f),new Vector3(1.15f,.04f,3.2f),new Color(.25f,.3f,.14f));return;}
            var water=Ribbon(station,"Moving clean-water ribbon",new[]{new Vector3(0,.22f,-1.05f),new Vector3(0,.22f,.6f),new Vector3(0,.18f,2.1f)},.94f,clean);streams.Add(water);
            for(int i=0;i<4;i++){var foam=Block(station,"Downstream foam",new Vector3(0,.25f,-.8f),new Vector3(.57f,.016f,.05f),paper);Move(foam.transform,4,i*.5f,1,new Vector3(0,.25f,1.85f));}
            var boat=BuildBoat("Rescue skiff returning to service",new Vector3(-10,-.47f,27),.85f,false);
            if(level==1)Move(boat,0,0,.04f);else Move(boat,5,0,.45f,new Vector3(-22,-.47f,47));
        }
        void BuildStarlight()
        {
            var dial=Group("Reconnected astrolabe witnesses",Ground(world.SitePoints[1]+new Vector3(4,0,-1.2f)));
            var wheel=Local(dial,"Celestial route wheel",new Vector3(0,1.38f,0));
            Block(dial,"Astrolabe stand",new Vector3(0,.6f,0),new Vector3(.45f,1.2f,.48f),metal);
            CoastalMesh.Ring(wheel,Vector3.zero,1.1f,.075f,brass,Quaternion.identity);
            for(int i=0;i<3;i++){float a=i*Mathf.PI*2/3;Vector3 pos=new Vector3(Mathf.Cos(a)*.92f,Mathf.Sin(a)*.92f,.05f);Shape.Rock(wheel,pos,new Vector3(.13f,.16f,.06f),paper,4);}
            if(level==0)return;
            Vector3[] points={new Vector3(.92f,0,.05f),new Vector3(-.46f,.796f,.05f),new Vector3(-.46f,-.796f,.05f),new Vector3(.92f,0,.05f)};
            Ribbon(wheel,"Unbroken three-witness constellation",points,.032f,warm);
            if(level==2){Move(wheel,6,0,4);var sun=Local(wheel,"Independently moving sun",Vector3.zero);CoastalMesh.Ring(sun,Vector3.zero,.37f,.045f,clean,Quaternion.Euler(70,0,0));Move(sun,1,0,28);}
        }
        void BuildNames()
        {
            var archive=Group("Northstar black-box archive",Ground(world.SitePoints[1]+new Vector3(3.8f,0,1)));
            Block(archive,"Recovered archive table",new Vector3(0,.71f,0),new Vector3(2.6f,.17f,1.2f),timber);
            for(int s=-1;s<=1;s+=2)Block(archive,"Archive trestle",new Vector3(s*.9f,.35f,0),new Vector3(.13f,.7f,.8f),timber);
            Block(archive,"Ship black box",new Vector3(0,1.0f,0),new Vector3(1,.46f,.65f),metal);
            for(int i=0;i<2;i++)CoastalMesh.Ring(archive,new Vector3((i-.5f)*.49f,1.25f,0),.17f,.04f,brass,Quaternion.Euler(90,0,0));
            if(level==0)return;
            Block(archive,"Saved recording pages",new Vector3(.83f,.83f,.15f),new Vector3(.57f,.03f,.7f),paper);
            for(int s=-1;s<=1;s+=2)Shape.Beam(archive,new Vector3(s*2.7f,0,-.75f),new Vector3(s*2.7f,level==1?1.7f:3.2f,-.75f),.065f,timber);
            float height=level==1?1.65f:3.1f;Shape.Beam(archive,new Vector3(-2.7f,height,-.75f),new Vector3(2.7f,height,-.75f),.018f,brass);
            string[] names={"北星","归帆","海燕","微光","远岬","望潮","晨风"};
            for(int i=0;i<7;i++){
                var flag=Local(archive,"Ship name pennant "+names[i],new Vector3((i-3)*.72f,height-.15f,-.75f));
                var mesh=new CoastalMesh();mesh.Quad(new Vector3(-.25f,0,0),new Vector3(.25f,0,0),new Vector3(.22f,-.61f,0),new Vector3(-.23f,-.58f,0),i%2==0?paper:world.Definition.accent);mesh.Build("Whole sewn name flag",flag);
                Label(flag,names[i],new Vector3(0,-.28f,-.015f),.16f,metal);Move(flag,3,i*.7f,level==1?.5f:1.3f);
            }
        }
        void BuildRescue()
        {
            var pod=Group("Opened passenger rescue pod",Ground(world.SitePoints[1]+new Vector3(4.1f,0,-.9f)));
            Shape.Part("Insulated rescue cradle",PrimitiveType.Capsule,pod,new Vector3(0,.58f,0),new Vector3(1.35f,1.3f,1.15f),metal).transform.localRotation=Quaternion.Euler(90,0,0);
            Block(pod,"Cradle bedding",new Vector3(0,.67f,0),new Vector3(.86f,.16f,1.95f),paper);
            var lid=Local(pod,"Hinged rescue lid",new Vector3(-.62f,.7f,0));Block(lid,"Frosted observation window",new Vector3(.61f,.1f,0),new Vector3(1.2f,.14f,2.26f),new Color(.56f,.76f,.79f));
            lid.localRotation=Quaternion.Euler(0,0,level==0?0:level==1?78:112);
            if(level==0)return;
            var passenger=BuildPassenger(Ground(world.QuestPoint+new Vector3(2.2f,0,.25f)));Move(passenger,7,0,.013f);
            if(level==2){Lantern(pod,new Vector3(1.1f,.1f,.7f),.28f,warm);for(int i=0;i<3;i++)Shape.Rock(pod,new Vector3(-1.1f+i*.75f,.06f,1.7f),new Vector3(.45f,.13f,.4f),new Color(.56f,.74f,.75f),5);}
        }
        Transform BuildPassenger(Vector3 at)
        {
            var person=Group("A-Lan / rescued passenger holding her ticket",at);Color coat=new Color(.77f,.46f,.25f),skin=new Color(.72f,.51f,.36f);
            for(int s=-1;s<=1;s+=2){Shape.Beam(person,new Vector3(s*.12f,.16f,0),new Vector3(s*.12f,.56f,0),.12f,metal);Block(person,"Passenger boot",new Vector3(s*.12f,.08f,.06f),new Vector3(.18f,.16f,.27f),timber);}
            Shape.Part("Passenger winter coat",PrimitiveType.Capsule,person,new Vector3(0,.82f,0),new Vector3(.5f,.37f,.34f),coat);
            Shape.Part("Passenger face",PrimitiveType.Sphere,person,new Vector3(0,1.27f,.04f),new Vector3(.35f,.41f,.33f),skin);
            Shape.Part("Knitted cap",PrimitiveType.Sphere,person,new Vector3(0,1.41f,-.03f),new Vector3(.41f,.26f,.37f),paper);
            for(int s=-1;s<=1;s+=2){Shape.Part("Passenger eye",PrimitiveType.Sphere,person,new Vector3(s*.066f,1.29f,.193f),Vector3.one*.035f,metal);Shape.Beam(person,new Vector3(s*.21f,1.0f,0),new Vector3(s*.19f,.78f,.23f),.13f,coat);}
            Block(person,"Student ferry ticket",new Vector3(0,.83f,.29f),new Vector3(.35f,.2f,.02f),paper);Block(person,"Ticket validation stripe",new Vector3(.06f,.84f,.308f),new Vector3(.028f,.15f,.003f),brass);
            return person;
        }
        void BuildGrounding()
        {
            var array=Group("Sequential grounding array",Ground(world.SitePoints[1]+new Vector3(3.4f,0,-1)));
            for(int i=0;i<3;i++){
                Vector3 at=new Vector3((i-1)*1.2f,0,0);Shape.Beam(array,at,at+Vector3.up*(1.6f+i*.22f),.065f,metal);
                for(int j=0;j<3;j++)CoastalMesh.Ring(array,at+Vector3.up*(.8f+j*.22f),.24f,.045f,brass,Quaternion.Euler(90,0,0));
                Shape.Beam(array,at+new Vector3(-.27f,1.7f+i*.22f,0),at+new Vector3(.27f,1.7f+i*.22f,0),.05f,brass);
                if(level>0){var bead=Block(array,"Directed charge packet",at+Vector3.up*1.7f,new Vector3(.08f,.21f,.08f),clean);bead.GetComponent<Renderer>().sharedMaterial=Shape.Mat(clean,true);Move(bead.transform,4,i*.8f,level==1?1:1.8f,at+Vector3.up*.15f);}
            }
            Ribbon(array,"Grounding cable",new[]{new Vector3(-1.2f,.09f,0),new Vector3(0,.09f,.3f),new Vector3(1.2f,.09f,0),new Vector3(1.2f,.09f,1.2f)},.04f,brass);
            if(level==2){Lantern(array,new Vector3(1.2f,.05f,1.2f),.23f,warm);var weather=Local(array,"Freed weather vane",new Vector3(0,2.15f,0));Shape.Beam(weather,Vector3.left*.55f,Vector3.right*.55f,.035f,brass);Block(weather,"Weather vane fin",new Vector3(-.38f,.08f,0),new Vector3(.3f,.18f,.025f),paper);Move(weather,1,0,35);}
        }
        void BuildForge()
        {
            var forge=Group("Finished tide-key workbench",Ground(world.SitePoints[1]+new Vector3(3.8f,0,1)));
            Block(forge,"Anvil base",new Vector3(0,.48f,0),new Vector3(1.1f,.94f,.9f),metal);Block(forge,"Anvil face",new Vector3(0,1,0),new Vector3(1.8f,.18f,1.04f),metal);
            var blank=Local(forge,"Tide-key blank / completed teeth",new Vector3(-.3f,1.14f,.1f));
            Shape.Beam(blank,new Vector3(-.7f,0,0),new Vector3(.66f,0,0),.085f,level==0?metal:brass);
            CoastalMesh.Ring(blank,new Vector3(-.7f,0,0),.23f,.058f,level==0?metal:brass,Quaternion.Euler(90,0,0));
            if(level>0){for(int i=0;i<3;i++)Block(blank,"Forged key tooth",new Vector3(.2f+i*.17f,.045f,.12f+(i%2)*.08f),new Vector3(.1f,.12f,.3f),brass);}
            var hearth=Local(forge,"Controlled forge hearth",new Vector3(2.2f,0,0));for(int i=0;i<7;i++){float a=i*Mathf.PI*2/7;Shape.Rock(hearth,new Vector3(Mathf.Cos(a)*.65f,.13f,Mathf.Sin(a)*.65f),new Vector3(.37f,.25f,.36f),metal,5);}
            if(level<2){for(int i=0;i<3;i++){var coal=Shape.Rock(hearth,new Vector3((i-1)*.23f,.15f,0),new Vector3(.21f,level==0?.4f:.16f,.24f),level==0?new Color(.95f,.26f,.12f):warm,5);Move(coal.transform,7,i,.035f);}}
            if(level==2){blank.localPosition=new Vector3(.2f,1.7f,-.7f);blank.localRotation=Quaternion.Euler(0,0,15);Shape.Beam(forge,new Vector3(-.9f,1.06f,-.7f),new Vector3(-.9f,2,-.7f),.055f,timber);Shape.Beam(forge,new Vector3(.9f,1.06f,-.7f),new Vector3(.9f,2,-.7f),.055f,timber);Lantern(forge,new Vector3(1.1f,1.1f,.25f),.24f,warm);}
        }
        void BuildDeparture()
        {
            var mooring=Group("Seven separate departure lines",Ground(world.QuestPoint+new Vector3(-2.5f,0,-.2f)));
            for(int i=0;i<7;i++){
                var cleat=Local(mooring,"Passenger route "+(i+1),new Vector3((i-3)*.28f,.16f,0));Block(cleat,"Route register",Vector3.zero,new Vector3(.16f,.24f,.13f),level==0?metal:brass);
            }
            if(level==0)return;
            for(int i=0;i<7;i++){
                float x=(i<4?-1:1)*(12+(i%4)*3.5f);Vector3 start=new Vector3(x,-.48f,31+(i%4)*3.6f);
                var boat=BuildBoat("Released ship "+(i+1),start,.8f+i*.075f,true);
                if(level==1)Move(boat,0,i,.025f);
                else Move(boat,5,i*1.8f,.24f,new Vector3(x*1.8f+(i-3)*1.4f,-.48f,81+i*5.5f));
            }
        }
        Transform BuildBoat(string name,Vector3 at,float size,bool sail)
        {
            var boat=Group(name,at);boat.localScale=Vector3.one*size;
            var mesh=new CoastalMesh();var bow=new Vector3(0,.27f,1.25f);var stern=new Vector3(0,.08f,-1.12f);var left=new Vector3(-.54f,.23f,-.35f);var right=new Vector3(.54f,.23f,-.35f);var keel=new Vector3(0,-.22f,-.1f);
            mesh.Tri(bow,keel,left,timber);mesh.Tri(bow,right,keel,timber*.8f);mesh.Tri(left,keel,stern,timber*.9f);mesh.Tri(stern,keel,right,timber*.72f);mesh.Quad(bow,left,stern,right,paper*.65f);mesh.Build("Faceted timber hull",boat);
            Shape.Beam(boat,new Vector3(-.51f,.24f,-.34f),bow,.035f,brass);Shape.Beam(boat,new Vector3(.51f,.24f,-.34f),bow,.035f,brass);
            if(sail){Shape.Beam(boat,new Vector3(0,.22f,0),new Vector3(0,1.88f,0),.042f,timber);var canvas=new CoastalMesh();canvas.Tri(new Vector3(.04f,1.83f,0),new Vector3(.04f,.45f,0),new Vector3(.89f,.47f,.04f),paper);canvas.Tri(new Vector3(.04f,1.83f,0),new Vector3(.89f,.47f,.04f),new Vector3(.04f,.45f,0),paper*.9f);canvas.Build("Whole sailing canvas",boat);}
            else Block(boat,"Rescue provisions",new Vector3(0,.35f,-.37f),new Vector3(.52f,.25f,.49f),paper);
            Lantern(boat,new Vector3(0,.3f,.57f),.16f,warm);return boat;
        }
        void BuildEvidenceCopy()
        {
            var at=Ground(world.QuestPoint+new Vector3(-1.8f,0,.55f));var notebook=Group("Collected evidence retained by guide",at);
            Block(notebook,"Evidence stool",new Vector3(0,.27f,0),new Vector3(.48f,.54f,.48f),timber);
            Block(notebook,"Bound evidence record",new Vector3(0,.58f,0),new Vector3(.47f,.04f,.33f),paper);
            for(int i=0;i<4;i++)Block(notebook,"Handwritten record",new Vector3(0,.604f,(i-1.5f)*.06f),new Vector3(.32f,.003f,.009f),metal);
        }
        void Label(Transform parent,string value,Vector3 point,float height,Color color)
        {
            var font=Resources.Load<TMP_FontAsset>("Fonts/SeaFont");if(!font)return;
            var text=new GameObject("Readable restored ship name").AddComponent<TextMeshPro>();text.transform.SetParent(parent,false);text.transform.localPosition=point;text.transform.localRotation=Quaternion.Euler(0,180,0);text.font=font;text.text=value;text.fontSize=height*10;text.color=color;text.alignment=TextAlignmentOptions.Center;text.rectTransform.sizeDelta=new Vector2(.48f,.32f);text.enableWordWrapping=false;text.renderer.sortingOrder=1;
        }
        void Animate()
        {
            if(!display||!display.gameObject.activeSelf)return;
            float t=Time.time-createdAt;
            foreach(var m in motion){if(!m.item)continue;float a=t+m.phase;
                switch(m.kind){
                    case 0:m.item.localPosition=m.origin+Vector3.up*Mathf.Sin(a*1.3f)*m.amount;m.item.localRotation=m.rotation*Quaternion.Euler(0,0,Mathf.Sin(a*.8f)*2);break;
                    case 1:m.item.localRotation=m.rotation*Quaternion.Euler(0,Mathf.Sin(a*.19f)*m.amount,0);break;
                    case 2:m.item.localRotation=m.rotation*Quaternion.Euler(Mathf.Sin(a*1.4f)*m.amount,0,0);break;
                    case 3:m.item.localRotation=m.rotation*Quaternion.Euler(Mathf.Sin(a*1.8f)*m.amount*4,Mathf.Sin(a*.9f)*m.amount*5,Mathf.Cos(a)*m.amount*2);break;
                    case 4:float progress=Mathf.Repeat(a*m.amount,2.4f)/2.4f;m.item.localPosition=Vector3.Lerp(m.origin,m.end,progress);m.item.localScale=m.scale*(.55f+.45f*Mathf.Sin(progress*Mathf.PI));break;
                    case 5:float voyage=Mathf.Repeat(a*m.amount+2,50)/50;float fade=Mathf.SmoothStep(0,1,Mathf.Min(voyage*25,(1-voyage)*25));m.item.localPosition=Vector3.Lerp(m.origin,m.end,Mathf.SmoothStep(0,1,voyage))+Vector3.up*Mathf.Sin(a)*.035f;m.item.localRotation=Quaternion.LookRotation(m.end-m.origin)*Quaternion.Euler(0,0,Mathf.Sin(a*.7f)*2);m.item.localScale=m.scale*fade;break;
                    case 6:m.item.localRotation=m.rotation*Quaternion.Euler(0,0,a*m.amount);break;
                    case 7:m.item.localScale=Vector3.Scale(m.scale,new Vector3(1,1+Mathf.Sin(a*1.9f)*m.amount,1));break;
                }
            }
            for(int i=0;i<streams.Count;i++)if(streams[i])streams[i].startColor=streams[i].endColor=Color.Lerp(new Color(.8f,.93f,.91f,.8f),Color.white,(Mathf.Sin(t*1.5f+i)+1)*.5f);
        }
        void ClearDisplay()
        {
            motion.Clear();streams.Clear();if(display){display.gameObject.SetActive(false);Destroy(display.gameObject);display=null;}
            foreach(var material in ownedMaterials)if(material)Destroy(material);ownedMaterials.Clear();
        }
        void OnDestroy(){ClearDisplay();}
    }
}
