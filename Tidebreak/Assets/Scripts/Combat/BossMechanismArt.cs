using UnityEngine;
using TMPro;

namespace Tidebreak
{
    public partial class BossMechanism
    {
        Vector3 Point(float x,float z)
        {
            var p=new Vector3(x,0,z);p.y=game.World.GroundAt(p)+.12f;return p;
        }
        void Add(string label,float x,float z,Color color,string kind){AddAt(label,Point(x,z),color,kind);}
        void AddAt(string label,Vector3 p,Color color,string kind)
        {
            nodes.Add(MechanismMarker.Create(stageRoot,p,label,color,kind,nodes.Count+1));
        }
        void AddLine(Color color)
        {
            var go=new GameObject("Readable mechanism guide");go.transform.SetParent(stageRoot,false);
            var line=go.AddComponent<LineRenderer>();line.sharedMaterial=Shape.Mat(Color.white,true);Shape.TintLine(line,color);line.useWorldSpace=true;
            line.positionCount=2;line.startWidth=line.endWidth=.065f;line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            guides.Add(line);
        }
        void SetGuide(int index,Vector3 from,Vector3 to,Color color)
        {
            var line=guides[index];line.SetPosition(0,from);line.SetPosition(1,to);Shape.TintLine(line,color);
        }
        void BuildNotes()
        {
            noteSource=gameObject.AddComponent<AudioSource>();noteSource.spatialBlend=0;noteSource.volume=.3f;notes=new AudioClip[3];
            float[] pitches={261.63f,329.63f,392};const int rate=22050;
            for(int n=0;n<3;n++){
                var data=new float[(int)(rate*.45f)];
                for(int i=0;i<data.Length;i++){float t=(float)i/rate;float envelope=Mathf.Min(1,t/.015f)*Mathf.Exp(-t*7);data[i]=(Mathf.Sin(2*Mathf.PI*pitches[n]*t)+.2f*Mathf.Sin(4*Mathf.PI*pitches[n]*t))*envelope*.5f;}
                notes[n]=AudioClip.Create("Boss melody "+n,data.Length,1,rate,false);notes[n].SetData(data,0);
            }
        }
        void PlayNote(int index){if(notes!=null)game.Audio.PlayMechanismTone(notes[index],.36f);}
    }

    // Distinct physical props communicate the player's verb before the HUD is read.
    // Sonar pedestals have a narrow solid base to keep the camera outside the
    // dish. Other props leave combat routes open and do not absorb shots.
    public sealed class MechanismMarker
    {
        public Transform Root {get;private set;}
        public string Label {get;private set;}
        public string Kind {get;private set;}
        public Vector3 Position {get{return Root.position;}}
        Transform moving,secondEcho;
        TextMeshPro label;
        Renderer indicator,perimeter,substrate;
        Color baseColor,flashColor;
        float flashRemaining;
        bool completed;
        readonly MaterialPropertyBlock tint=new MaterialPropertyBlock();
        public static MechanismMarker Create(Transform parent,Vector3 at,string name,Color color,string kind,int number)
        {
            var marker=new MechanismMarker{Label=name,Kind=kind,baseColor=color};
            marker.Root=new GameObject(name+" · "+kind).transform;marker.Root.SetParent(parent,false);marker.Root.position=at;
            var root=marker.Root;Color wood=new Color(.28f,.17f,.105f),metal=new Color(.18f,.27f,.3f),bone=new Color(.68f,.68f,.54f);
            float ringRadius=kind=="pool"?2.75f:kind=="ground"?2.5f:kind=="waypoint"?1.2f:1.7f;
            if(kind!="echo")marker.perimeter=CoastalMesh.Ring(root,Vector3.up*.06f,ringRadius,.055f,Color.white,Quaternion.Euler(90,0,0)).GetComponent<Renderer>();
            switch(kind){
                case "stake":
                    for(int side=-1;side<=1;side+=2){Shape.Part("Impact timber",PrimitiveType.Cube,root,new Vector3(side*.5f,.6f,0),new Vector3(.3f,1.2f,.4f),wood);Shape.Beam(root,new Vector3(side*.5f,.2f,0),new Vector3(0,.95f,0),.09f,metal);}
                    Shape.Part("Heavy crossbrace",PrimitiveType.Cube,root,new Vector3(0,.85f,0),new Vector3(1.6f,.23f,.45f),wood);
                    break;
                case "shell":
                    for(int i=-3;i<=3;i++){var rib=Shape.Part("Acoustic shell rib",PrimitiveType.Capsule,root,new Vector3(i*.2f,.7f,0),new Vector3(.22f,1.1f-Mathf.Abs(i)*.1f,.4f),bone);rib.transform.localRotation=Quaternion.Euler(0,0,i*13);}
                    Shape.Part("Resonant throat",PrimitiveType.Sphere,root,new Vector3(0,.45f,-.22f),new Vector3(.65f,.48f,.4f),metal);
                    break;
                case "pump":
                    Shape.Part("Purifier tank",PrimitiveType.Cylinder,root,new Vector3(0,.7f,0),new Vector3(.7f,.7f,.7f),metal);
                    Shape.Beam(root,new Vector3(0,1.35f,0),new Vector3(.7f,1.35f,0),.14f,bone);
                    Shape.Beam(root,new Vector3(.7f,1.35f,0),new Vector3(.7f,.95f,0),.14f,bone);
                    Shape.Part("Fresh water basin",PrimitiveType.Cylinder,root,new Vector3(.7f,.22f,0),new Vector3(.7f,.18f,.7f),color);
                    break;
                case "pool":
                    for(int i=0;i<6;i++){float angle=i*Mathf.PI/3;Shape.Part("Contaminated roots",PrimitiveType.Capsule,root,new Vector3(Mathf.Cos(angle)*1.9f,.22f,Mathf.Sin(angle)*1.9f),new Vector3(.22f,.6f,.22f),new Color(.22f,.34f,.12f));}
                    marker.substrate=Shape.Part("Poison stain",PrimitiveType.Cylinder,root,new Vector3(0,.035f,0),new Vector3(5.2f,.02f,5.2f),new Color(.25f,.35f,.12f)).GetComponent<Renderer>();
                    break;
                case "mirror":
                    Shape.Beam(root,Vector3.zero,new Vector3(0,1.6f,0),.16f,bone);
                    marker.moving=new GameObject("Rotating reflector").transform;marker.moving.SetParent(root,false);marker.moving.localPosition=Vector3.up*1.6f;
                    Shape.Part("Brass mirror frame",PrimitiveType.Cube,marker.moving,Vector3.zero,new Vector3(1.55f,1.35f,.2f),bone);
                    Shape.Part("Polished mirror face",PrimitiveType.Cube,marker.moving,new Vector3(0,0,-.13f),new Vector3(1.3f,1.12f,.05f),new Color(.67f,.86f,.87f),false,true);
                    Shape.Beam(marker.moving,Vector3.zero,Vector3.forward*1.3f,.05f,color,true);
                    break;
                case "bell":
                    for(int side=-1;side<=1;side+=2)Shape.Beam(root,new Vector3(side*1.1f,0,0),new Vector3(side*1.1f,3.2f,0),.18f,wood);
                    Shape.Beam(root,new Vector3(-1.1f,3.2f,0),new Vector3(1.1f,3.2f,0),.2f,wood);
                    marker.moving=new GameObject("Suspended bell").transform;marker.moving.SetParent(root,false);marker.moving.localPosition=new Vector3(0,2.8f,0);
                    Shape.Part("Bell shoulder",PrimitiveType.Sphere,marker.moving,new Vector3(0,-.4f,0),new Vector3(.8f,.8f,.8f),bone);
                    Shape.Part("Bell mouth",PrimitiveType.Cylinder,marker.moving,new Vector3(0,-.75f,0),new Vector3(1.1f,.17f,1.1f),bone);
                    Shape.Beam(root,new Vector3(.8f,2.9f,0),new Vector3(.8f,.7f,0),.035f,wood);break;
                case "brazier":
                    Shape.Part("Sled bed",PrimitiveType.Cube,root,new Vector3(0,.35f,0),new Vector3(1.35f,.3f,1.8f),wood);
                    for(int side=-1;side<=1;side+=2)Shape.Beam(root,new Vector3(side*.65f,.1f,-1),new Vector3(side*.65f,.1f,1),.13f,metal);
                    Shape.Part("Mobile furnace",PrimitiveType.Cylinder,root,new Vector3(0,.95f,0),new Vector3(.8f,.6f,.8f),metal);
                    Shape.Part("Furnace flame",PrimitiveType.Sphere,root,new Vector3(0,1.55f,0),new Vector3(.55f,.9f,.55f),color,false,true);break;
                case "coil":
                    Shape.Beam(root,Vector3.zero,Vector3.up*2.3f,.14f,metal);
                    for(int i=0;i<5;i++)CoastalMesh.Ring(root,Vector3.up*(.7f+i*.3f),.5f,.065f,bone,Quaternion.Euler(90,0,0));
                    Shape.Part("Lightning collector",PrimitiveType.Sphere,root,Vector3.up*2.5f,Vector3.one*.55f,color,false,true);break;
                case "ground":
                    Shape.Part("Conductive ground grid",PrimitiveType.Cylinder,root,Vector3.up*.05f,new Vector3(4.6f,.03f,4.6f),metal);
                    for(int i=-1;i<=1;i++)Shape.Beam(root,new Vector3(-1.7f,.12f,i*.65f),new Vector3(1.7f,.12f,i*.65f),.035f,bone);
                    Shape.Beam(root,Vector3.zero,Vector3.up*1.1f,.12f,bone);break;
                case "valve":
                    Shape.Beam(root,Vector3.zero,Vector3.up*1.25f,.23f,metal);
                    CoastalMesh.Ring(root,Vector3.up*1.4f,.7f,.085f,bone,Quaternion.identity);
                    for(int i=0;i<4;i++){float a=i*Mathf.PI*.5f;Shape.Beam(root,Vector3.up*1.4f,new Vector3(Mathf.Cos(a)*.65f,1.4f+Mathf.Sin(a)*.65f,0),.045f,bone);}
                    break;
                case "memory":
                    for(int i=-1;i<=1;i++)Shape.Part("Prismatic memory shard",PrimitiveType.Cube,root,new Vector3(i*.35f,.55f+Mathf.Abs(i)*.16f,0),new Vector3(.2f,1,.18f),new Color(.64f,.65f,.85f),false,true).transform.localRotation=Quaternion.Euler(0,i*20,i*17);
                    break;
                case "arm":
                    var points=new Vector3[18];var widths=new float[18];
                    for(int i=0;i<18;i++){float t=i/17f;points[i]=new Vector3(Mathf.Sin(t*5)*.8f,.25f+t*2.7f,Mathf.Cos(t*4)*.5f);widths[i]=Mathf.Lerp(.48f,.06f,t);}
                    CoastalMesh.Tube("Tetherable curling arm",root,points,widths,new Color(.35f,.16f,.33f),new Color(.71f,.4f,.63f),10);
                    for(int i=2;i<15;i+=2)Shape.Part("Tentacle sucker",PrimitiveType.Sphere,root,points[i]+Vector3.back*widths[i],Vector3.one*.22f,new Color(.9f,.64f,.7f));
                    break;
                case "sonar":
                    var solid=root.gameObject.AddComponent<CapsuleCollider>();solid.radius=.75f;solid.height=2;solid.center=Vector3.up;
                    // Keep the console below the horizon: players must read all
                    // three sea projections while standing at its controls.
                    Shape.Part("Sonar pedestal",PrimitiveType.Cylinder,root,Vector3.up*.3f,new Vector3(.65f,.3f,.65f),metal);
                    Shape.Part("Acoustic dish",PrimitiveType.Cylinder,root,new Vector3(0,.78f,0),new Vector3(1.1f,.1f,1.1f),bone).transform.localRotation=Quaternion.Euler(-25,0,0);
                    Shape.Beam(root,new Vector3(0,.78f,0),new Vector3(0,1.15f,.3f),.045f,metal);break;
                case "echo":
                    // Upright sonar projections remain visible above the raised pier.
                    // The second ring communicates truth without requiring color vision.
                    marker.perimeter=CoastalMesh.Ring(root,Vector3.zero,1.35f,.09f,Color.white,Quaternion.identity).GetComponent<Renderer>();
                    marker.secondEcho=CoastalMesh.Ring(root,Vector3.zero,2.1f,.09f,color,Quaternion.identity);
                    break;
            }
            if(kind!="echo"&&kind!="pool"&&kind!="waypoint"){
                marker.indicator=Shape.Part("Readable action indicator",PrimitiveType.Cube,root,new Vector3(0,kind=="bell"?3.4f:2.75f,0),new Vector3(.45f,.12f,.45f),color,false,true).GetComponent<Renderer>();
                var text=new GameObject("World action label");text.transform.SetParent(root,false);text.transform.localPosition=new Vector3(0,kind=="bell"?4.05f:3.4f,0);
                marker.label=text.AddComponent<TextMeshPro>();marker.label.font=Resources.Load<TMP_FontAsset>("Fonts/SeaFont");marker.label.fontSize=3.8f;marker.label.alignment=TextAlignmentOptions.Center;marker.label.text=number+" · "+name;marker.label.color=new Color(1,.94f,.76f);marker.label.rectTransform.sizeDelta=new Vector2(7,1.3f);marker.label.enableWordWrapping=false;
            }
            marker.Tint(color);return marker;
        }
        public void Tint(Color color){baseColor=color;ApplyTint(flashRemaining>0?flashColor:color);}
        void ApplyTint(Color color){tint.SetColor("_Color",color);tint.SetColor("_EmissionColor",color*1.7f);if(indicator)indicator.SetPropertyBlock(tint);if(perimeter)perimeter.SetPropertyBlock(tint);}
        public void Flash(Color color,float duration){flashColor=color;flashRemaining=duration;ApplyTint(color);}
        public void Complete(){completed=true;Tint(new Color(.38f,.96f,.55f));if(label)label.text="完成 · "+Label;if(substrate)substrate.sharedMaterial=Shape.Mat(new Color(.19f,.53f,.43f));if(Kind=="arm")Root.localScale=new Vector3(1,.18f,1);if(Kind=="stake"){Root.localRotation=Quaternion.Euler(0,0,76);if(indicator)indicator.enabled=false;if(label)label.enabled=false;}}
        public void Move(Vector3 position){Root.position=position;}
        public void SetRotation(Vector3 direction){if(moving)moving.rotation=Quaternion.LookRotation(direction);}
        public void AnimateBell(float time){if(moving)moving.localRotation=Quaternion.Euler(0,0,Mathf.Sin(time*3)*Mathf.Clamp(time,0,4)*6);}
        public void SetEcho(bool real){if(secondEcho)secondEcho.gameObject.SetActive(real);}
        public void Tick(float dt,Transform camera)
        {
            if(flashRemaining>0){flashRemaining-=dt;if(flashRemaining<=0)ApplyTint(baseColor);}
            if(label){float distance=Vector3.Distance(Root.position,camera.position);label.enabled=distance>3.8f&&distance<42&&!(Kind=="stake"&&completed);label.transform.localScale=Vector3.one*Mathf.Clamp(distance/15f,.22f,1);var direction=label.transform.position-camera.position;direction.y=0;if(direction.sqrMagnitude>.01f)label.transform.rotation=Quaternion.LookRotation(direction);}
            if(Kind=="memory"&&!completed&&Root.childCount>0)Root.GetChild(0).Rotate(0,dt*25,0);
            if(Kind=="echo"){var bearing=Root.position-camera.position;bearing.y=0;if(bearing.sqrMagnitude>.01f)Root.rotation=Quaternion.LookRotation(bearing);}
        }
    }
}
