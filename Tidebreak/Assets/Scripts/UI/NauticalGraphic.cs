using UnityEngine;

namespace Tidebreak
{
    // Small, resolution-independent illustrations. These are authored silhouettes and
    // paths, not font glyphs, so equipment and currency remain legible in every locale.
    public enum NauticalMark { Compass, Coin, Anchor, Fish, Wrench, Revolver, Rifle, Harpoon, Arc,
        Reel, Bag, Lure, Coat, Soup, Medicine, Grenade, Snow, Sonar, Boot, Flask, Lock, Seal, Wave }

    public sealed class NauticalGraphic : UnityEngine.UI.MaskableGraphic
    {
        public NauticalMark Mark;
        UnityEngine.UI.VertexHelper mesh;
        Vector2 origin;float unit;
        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper vh)
        {
            vh.Clear();mesh=vh;Rect r=rectTransform.rect;unit=Mathf.Min(r.width,r.height)/100;
            origin=new Vector2(r.x+(r.width-100*unit)*.5f,r.y+r.height-(r.height-100*unit)*.5f);
            switch(Mark){
                case NauticalMark.Coin:
                    Ring(50,50,37,3);Ring(50,50,29,1.5f);
                    for(int i=0;i<12;i++){float a=i*Mathf.PI/6;Line(50+Mathf.Sin(a)*33,50+Mathf.Cos(a)*33,50+Mathf.Sin(a)*36,50+Mathf.Cos(a)*36,2);}
                    Poly(50,25,57,43,74,50,57,56,50,76,43,56,26,50,43,43);break;
                case NauticalMark.Compass:
                    Ring(50,50,37,1);Ring(50,50,44,1.3f);Ring(50,50,4,1.5f);
                    for(int i=0;i<8;i++){float a=i*Mathf.PI/4;float reach=i%2==0?33:22;
                        Vector2 tip=new Vector2(50+Mathf.Sin(a)*reach,50+Mathf.Cos(a)*reach);
                        Vector2 wing=new Vector2(Mathf.Cos(a)*5,-Mathf.Sin(a)*5);
                        Poly(50,50,tip.x,tip.y,50+wing.x,50+wing.y);
                        Line(50-wing.x,50-wing.y,tip.x,tip.y,1);
                    }
                    Line(50,1,50,10,2);Line(50,90,50,99,2);Line(1,50,10,50,2);Line(90,50,99,50,2);break;
                case NauticalMark.Anchor:
                    Ring(50,17,8,3);Line(50,25,50,82,5);Line(30,39,70,39,4);
                    Curve(20,57,20,78,39,89,50,82,4);Curve(80,57,80,78,61,89,50,82,4);
                    Poly(14,65,19,49,31,60);Poly(86,65,81,49,69,60);break;
                case NauticalMark.Fish:
                    Poly(15,51,27,33,46,26,68,32,79,45,93,34,91,65,78,57,62,71,42,72,23,64);
                    Stroke(27,35,34,50,27,64);Line(47,28,38,17,3);Line(39,70,48,83,3);
                    Ring(25,47,3,1.8f);break;
                case NauticalMark.Wrench:
                    Stroke(32,16,45,24,47,41,80,73,79,82,71,86,39,51,23,47,16,33,19,20,27,34,37,31,32,16);
                    Ring(74,77,3,2);break;
                case NauticalMark.Revolver:
                    Poly(14,32,86,32,91,39,86,46,54,46,46,54,40,54,45,78,25,85,19,73,28,51,17,49);
                    Line(50,34,50,47,3);Line(29,31,29,48,3);Line(19,29,81,29,2);Ring(45,52,11,2);break;
                case NauticalMark.Rifle:
                    Poly(8,48,24,42,29,34,54,35,57,39,88,39,94,43,90,47,54,48,49,56,32,56,17,70,9,64);
                    Poly(39,54,48,54,53,72,44,74);Line(58,32,78,32,4);Line(65,32,65,39,2);Line(86,35,86,45,3);break;
                case NauticalMark.Harpoon:
                    Line(17,78,74,21,5);Poly(71,16,88,10,80,31,79,22,69,24);
                    Poly(22,61,32,69,20,85,10,77);Ring(39,58,9,2);Line(37,72,44,79,4);break;
                case NauticalMark.Arc:
                    Poly(56,9,24,54,45,54,35,91,78,40,55,40);Ring(50,50,39,1.6f);break;
                case NauticalMark.Reel:
                    Ring(44,57,28,4);Ring(44,57,16,2);Ring(44,57,4,2);
                    for(int i=0;i<5;i++){float a=i*Mathf.PI*.4f;Line(44+Mathf.Cos(a)*19,57+Mathf.Sin(a)*19,44+Mathf.Cos(a)*24,57+Mathf.Sin(a)*24,2);}
                    Line(62,36,82,15,4);Line(72,66,83,66,4);Line(83,66,83,79,4);Line(33,25,49,25,4);break;
                case NauticalMark.Bag:
                    Poly(20,38,28,28,72,28,80,38,76,84,24,84);Stroke(34,29,34,20,42,14,58,14,66,20,66,29);
                    Line(21,47,79,47,3);Line(34,34,34,80,2);Line(65,34,65,80,2);Line(26,66,74,66,2);break;
                case NauticalMark.Lure:
                    Curve(28,19,5,31,39,61,65,59,4);Curve(28,19,58,18,84,34,65,59,4);
                    Ring(30,28,3,2);Line(67,54,83,58,3);Curve(83,58,94,76,71,84,69,67,3);Line(68,59,61,78,2);break;
                case NauticalMark.Coat:
                    Poly(32,13,41,24,58,24,67,13,83,25,76,85,24,85,17,25);
                    Stroke(34,15,31,40,24,46);Stroke(66,15,69,40,76,46);Line(50,28,50,83,3);
                    Stroke(29,53,41,53,41,68,29,68,29,53);Stroke(59,53,71,53,71,68,59,68,59,53);break;
                case NauticalMark.Soup:
                    Curve(16,48,20,86,79,87,84,48,4);Line(17,48,83,48,4);Line(34,86,66,86,3);
                    Curve(34,36,22,24,45,24,34,10,3);Curve(55,37,44,25,68,25,55,11,3);Line(72,47,87,20,4);break;
                case NauticalMark.Medicine:
                    Stroke(26,34,26,22,35,17,65,17,74,22,74,34);Poly(16,34,84,34,88,42,88,79,81,86,19,86,12,79,12,42);
                    Line(50,44,50,75,8);Line(35,60,65,60,8);break;
                case NauticalMark.Grenade:
                    Poly(40,26,62,26,74,41,77,67,64,86,35,86,22,67,26,41);
                    Stroke(37,26,37,15,67,15,81,37);Line(40,22,62,22,3);Line(30,51,70,51,2);Line(28,65,73,65,2);Line(46,30,46,81,2);break;
                case NauticalMark.Snow:
                    for(int i=0;i<6;i++){float a=i*Mathf.PI/3;Vector2 d=new Vector2(Mathf.Sin(a),Mathf.Cos(a)),n=new Vector2(-d.y,d.x);
                        Line(50,50,50+d.x*38,50+d.y*38,3);Line(50+d.x*24,50+d.y*24,50+d.x*17+n.x*10,50+d.y*17+n.y*10,3);Line(50+d.x*24,50+d.y*24,50+d.x*17-n.x*10,50+d.y*17-n.y*10,3);}
                    break;
                case NauticalMark.Sonar:
                    Ring(50,50,36,2);Ring(50,50,24,2);Ring(50,50,12,2);Line(50,50,77,23,4);Ring(32,66,4,2);break;
                case NauticalMark.Boot:
                    Poly(39,15,70,15,66,57,80,70,86,77,84,86,18,86,14,78,22,69,38,60);
                    Line(38,30,64,30,3);Line(38,41,61,41,3);Line(35,53,58,53,3);Line(21,78,79,78,2);break;
                case NauticalMark.Flask:
                    Stroke(38,13,62,13,62,37,80,66,81,78,72,86,28,86,19,78,20,66,38,37,38,13);
                    Line(33,12,67,12,4);Line(26,61,75,61,3);Curve(32,72,39,64,54,80,67,70,2);break;
                case NauticalMark.Lock:
                    Curve(30,43,24,8,76,8,70,43,5);Poly(23,43,77,43,81,49,81,80,74,86,26,86,19,80,19,49);
                    Ring(50,60,5,2);Line(50,65,50,74,3);break;
                case NauticalMark.Seal:
                    Ring(50,43,27,3);Ring(50,43,21,1.5f);Poly(50,25,56,37,68,38,59,47,61,61,50,54,38,61,41,47,32,38,44,37);
                    Stroke(31,65,24,92,39,84,46,92,50,74);Stroke(54,75,62,92,70,83,82,88,70,64);break;
                default:
                    for(int i=0;i<3;i++){float y=29+i*21;Curve(8,y,27,y-20,28,y+20,49,y,3);Curve(49,y,69,y-20,71,y+20,92,y,3);}break;
            }
        }
        Vector2 P(float x,float y){return origin+new Vector2(x,-y)*unit;}
        void Poly(params float[] points)
        {
            // Outlined specimen drawings keep internal details readable at 30 px.
            // Concave profiles use ear clipping rather than a triangle fan, which
            // would put stray diagonals across the harpoon, stock and coat cut-outs.
            int count=points.Length/2,start=mesh.currentVertCount;Color wash=color;wash.a*=.19f;
            var shape=new Vector2[count];var indices=new System.Collections.Generic.List<int>(count);float area=0;
            for(int i=0;i<count;i++){shape[i]=P(points[i*2],points[i*2+1]);mesh.AddVert(shape[i],wash,Vector2.zero);indices.Add(i);}
            for(int i=0;i<count;i++)area+=Cross(shape[i],shape[(i+1)%count]);float winding=Mathf.Sign(area);
            int guard=count*count;
            while(indices.Count>2&&guard-->0){bool clipped=false;
                for(int j=0;j<indices.Count;j++){
                    int a=indices[(j+indices.Count-1)%indices.Count],b=indices[j],c=indices[(j+1)%indices.Count];
                    if(Cross(shape[b]-shape[a],shape[c]-shape[b])*winding<=0)continue;bool inside=false;
                    foreach(int k in indices){if(k==a||k==b||k==c)continue;
                        if(Cross(shape[b]-shape[a],shape[k]-shape[a])*winding>=0&&Cross(shape[c]-shape[b],shape[k]-shape[b])*winding>=0&&Cross(shape[a]-shape[c],shape[k]-shape[c])*winding>=0){inside=true;break;}}
                    if(inside)continue;mesh.AddTriangle(start+a,start+b,start+c);indices.RemoveAt(j);clipped=true;break;
                }
                if(!clipped)break;
            }
            for(int i=0;i<count;i++){int j=(i+1)%count;Line(points[i*2],points[i*2+1],points[j*2],points[j*2+1],2.5f);}
        }
        static float Cross(Vector2 a,Vector2 b){return a.x*b.y-a.y*b.x;}
        void Line(float ax,float ay,float bx,float by,float width)
        {
            Vector2 a=P(ax,ay),b=P(bx,by),n=new Vector2(-(b-a).y,(b-a).x).normalized*width*unit*.5f;
            int k=mesh.currentVertCount;mesh.AddVert(a+n,color,Vector2.zero);mesh.AddVert(b+n,color,Vector2.zero);mesh.AddVert(b-n,color,Vector2.zero);mesh.AddVert(a-n,color,Vector2.zero);mesh.AddTriangle(k,k+1,k+2);mesh.AddTriangle(k,k+2,k+3);
        }
        void Stroke(params float[] points){for(int i=0;i<points.Length-2;i+=2)Line(points[i],points[i+1],points[i+2],points[i+3],3);}
        void Ring(float x,float y,float radius,float width)
        {for(int i=0;i<40;i++){float a=i*Mathf.PI/20,b=(i+1)*Mathf.PI/20;Line(x+Mathf.Cos(a)*radius,y+Mathf.Sin(a)*radius,x+Mathf.Cos(b)*radius,y+Mathf.Sin(b)*radius,width);}}
        void Curve(float ax,float ay,float bx,float by,float cx,float cy,float dx,float dy,float width)
        {
            Vector2 old=new Vector2(ax,ay);for(int i=1;i<=16;i++){float t=i/16f,s=1-t;Vector2 next=s*s*s*new Vector2(ax,ay)+3*s*s*t*new Vector2(bx,by)+3*s*t*t*new Vector2(cx,cy)+t*t*t*new Vector2(dx,dy);Line(old.x,old.y,next.x,next.y,width);old=next;}
        }
    }

    // A clipped sheet with an inset ink rule and a folded corner. Only regenerated
    // by uGUI when its rect/color changes; there are no Update allocations or textures.
    public sealed class NauticalPlateGraphic : UnityEngine.UI.Image
    {
        public bool Paper;
        public float Cut=10;
        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper vh)
        {
            if(type!=Type.Simple){base.OnPopulateMesh(vh);return;}
            vh.Clear();Rect r=GetPixelAdjustedRect();float c=Mathf.Min(Cut,Mathf.Min(r.width,r.height)*.24f);
            Vector2[] points={new Vector2(r.x,r.y+c),new Vector2(r.x+c,r.y),new Vector2(r.xMax,r.y),new Vector2(r.xMax,r.yMax-c),new Vector2(r.xMax-c,r.yMax),new Vector2(r.x,r.yMax)};
            Color top=color,bottom=Color.Lerp(color,Paper?new Color(.61f,.52f,.36f,color.a):Color.black,Paper?.045f:.16f);bottom.a=color.a;
            for(int i=0;i<points.Length;i++)vh.AddVert(points[i],Color.Lerp(bottom,top,Mathf.InverseLerp(r.y,r.yMax,points[i].y)),Vector2.zero);
            for(int i=1;i<points.Length-1;i++)vh.AddTriangle(0,i,i+1);
            Color trim=Paper?new Color(.28f,.32f,.28f,.17f):new Color(.80f,.69f,.46f,.29f);trim.a*=color.a;
            Rule(vh,new Vector2(r.x+12,r.yMax-5),new Vector2(r.xMax-c-4,r.yMax-5),.8f,trim);
            Rule(vh,new Vector2(r.x+c+4,r.y+5),new Vector2(r.xMax-12,r.y+5),.8f,trim);
            if(Paper){Color fold=new Color(.50f,.42f,.28f,.22f*color.a);int k=vh.currentVertCount;vh.AddVert(new Vector2(r.xMax-c-1,r.yMax-1),fold,Vector2.zero);vh.AddVert(new Vector2(r.xMax-1,r.yMax-c-1),fold,Vector2.zero);vh.AddVert(new Vector2(r.xMax-c-1,r.yMax-c-1),fold,Vector2.zero);vh.AddTriangle(k,k+1,k+2);}
        }
        static void Rule(UnityEngine.UI.VertexHelper vh,Vector2 a,Vector2 b,float width,Color tint)
        {Vector2 n=new Vector2(0,width);int k=vh.currentVertCount;vh.AddVert(a,tint,Vector2.zero);vh.AddVert(b,tint,Vector2.zero);vh.AddVert(b+n,tint,Vector2.zero);vh.AddVert(a+n,tint,Vector2.zero);vh.AddTriangle(k,k+1,k+2);vh.AddTriangle(k,k+2,k+3);}
    }
}
