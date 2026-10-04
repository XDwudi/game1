using System.Collections.Generic;
using UnityEngine;

namespace Tidebreak
{
    // Each island is a authored connected place, with a main circuit, shortcuts,
    // distinct elevations and a separate landmark silhouette. Heights are metres.
    public sealed class IslandRoute
    {
        public int a,b; public float width; public bool bridge;
        public IslandRoute(int from,int to,bool overWater=false,float metres=4.6f){a=from;b=to;bridge=overWater;width=metres;}
    }
    public sealed class IslandLayout
    {
        public readonly int Region;
        public readonly string Name;
        // Dock, commons, market, workshop, guide, objective A, objective B,
        // optional evidence, west bend, east bend, rear crossing, inner crossing.
        public readonly Vector3[] Points;
        public readonly IslandRoute[] Routes;
        public readonly Vector3 Landmark;
        readonly Vector4[] land;
        readonly Vector4[] water;
        public int BridgeCount {get{int n=0;foreach(var route in Routes)if(route.bridge)n++;return n;}}
        public int LoopCount {get{return Routes.Length-Points.Length+1;}}
        public float RouteLength {get{float n=0;foreach(var route in Routes)n+=Vector3.Distance(Points[route.a],Points[route.b]);return n;}}
        public IslandLayout(int region,string name,Vector3[] points,IslandRoute[] routes,Vector3 landmark,Vector4[] landforms,Vector4[] inlets)
        {Region=region;Name=name;Points=points;Routes=routes;Landmark=landmark;land=landforms;water=inlets;}
        static float FlatDistance(Vector3 a,Vector3 b){return Vector2.Distance(new Vector2(a.x,a.z),new Vector2(b.x,b.z));}
        public static float SegmentDistance(Vector3 p,Vector3 a,Vector3 b,out float t)
        {a.y=b.y=p.y=0;Vector3 d=b-a;t=Mathf.Clamp01(Vector3.Dot(p-a,d)/Mathf.Max(.01f,d.sqrMagnitude));return Vector3.Distance(p,a+d*t);}
        static readonly float[,] merchantAngles={{165,215},{145,220},{125,260},{220,335},{235,145},{145,235},{260,150},{145,220},{220,140}};
        public float MerchantYaw(int node){return merchantAngles[Mathf.Clamp(Region,0,8),node==2?0:1];}
        float MerchantYardDistance(Vector3 p,int node)
        {
            Vector3 local=Quaternion.Euler(0,-MerchantYaw(node),0)*(p-Points[node]);
            float x=Mathf.Max(0,Mathf.Abs(local.x)-4.45f),z=Mathf.Max(0,Mathf.Abs(local.z+5.55f)-3.45f);
            return Mathf.Sqrt(x*x+z*z);
        }
        public bool IsTrail(float x,float z,float margin=0)
        {
            Vector3 p=new Vector3(x,0,z);float t;
            foreach(var route in Routes)if(SegmentDistance(p,Points[route.a],Points[route.b],out t)<route.width*.5f+margin)return true;
            for(int i=1;i<Points.Length;i++)if(FlatDistance(p,Points[i])<(i==2||i==3?7.8f:5.5f)+margin)return true;
            return FlatDistance(p,Landmark)<6||MerchantYardDistance(p,2)<1.3f+margin||MerchantYardDistance(p,3)<1.3f+margin;
        }
        public float Height(float x,float z)
        {
            Vector3 p=new Vector3(x,0,z);float h=-5;
            // Landform ellipses: centre x/z, radius, plateau elevation. Different
            // radii in x/z and their intersections create shelves and coves.
            foreach(var v in land){float d=Mathf.Sqrt(Mathf.Pow((x-v.x)/v.z,2)+Mathf.Pow((z-v.y)/(v.z*.87f),2));h=Mathf.Max(h,Mathf.Lerp(v.w,-5,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.65f,1.15f,d))));}
            if(h>0)h+=Mathf.PerlinNoise(x*.09f+Region*13,z*.08f+21)*.22f;
            foreach(var v in water){float d=Mathf.Sqrt(Mathf.Pow((x-v.x)/v.z,2)+Mathf.Pow((z-v.y)/(v.z*v.w),2));h=Mathf.Lerp(-2.6f,h,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.58f,1.08f,d)));}
            // The shared shoreline arena is intentional: existing boss and
            // fishing interactions have an unchanged physical safety envelope.
            float coast=Mathf.Pow(Mathf.Pow(Mathf.Abs(x)/29f,4)+Mathf.Pow(Mathf.Abs(z-4.5f)/16.5f,4),.25f);
            h=Mathf.Max(h,Mathf.Lerp(1.35f,-5,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.77f,1.13f,coast))));
            float best=1000,roadHeight=0,roadWidth=0;
            foreach(var route in Routes){if(route.bridge)continue;float t;float distance=SegmentDistance(p,Points[route.a],Points[route.b],out t);if(distance<best){best=distance;roadHeight=Mathf.Lerp(Points[route.a].y,Points[route.b].y,t);roadWidth=route.width;}}
            if(best<roadWidth*.5f+2.5f)h=Mathf.Lerp(roadHeight,h,Mathf.SmoothStep(0,1,Mathf.InverseLerp(roadWidth*.5f,roadWidth*.5f+2.5f,best)));
            // Shop yards are also level beneath their back walls, so merchants,
            // foundations and approach paths share an actual floor.
            for(int i=0;i<Points.Length;i++){
                float radius=i==2||i==3?7.2f:i>=5&&i<=7?5.2f:3.2f;
                float distance=FlatDistance(p,Points[i]);if(distance<radius+2)h=Mathf.Lerp(Points[i].y,h,Mathf.SmoothStep(0,1,Mathf.InverseLerp(radius,radius+2,distance)));
            }
            for(int i=2;i<=3;i++){float distance=MerchantYardDistance(p,i);if(distance<2.2f)h=Mathf.Lerp(Points[i].y,h,Mathf.SmoothStep(0,1,distance/2.2f));}
            return h;
        }
        public Vector3[] FindRoute(Vector3 from,Vector3 to)
        {
            // Anchor to the closest edge, not just a node. A player returning
            // halfway along a switchback does not walk across the intervening ravine.
            Vector3 start,end;int startEdge=NearestEdge(from,out start),endEdge=NearestEdge(to,out end);
            int count=Points.Length;float[] distance=new float[count];int[] previous=new int[count];bool[] used=new bool[count];
            for(int i=0;i<count;i++){distance[i]=float.PositiveInfinity;previous[i]=-1;}
            var first=Routes[startEdge];distance[first.a]=Vector3.Distance(start,Points[first.a]);distance[first.b]=Vector3.Distance(start,Points[first.b]);
            for(int step=0;step<count;step++){
                int u=-1;for(int i=0;i<count;i++)if(!used[i]&&(u<0||distance[i]<distance[u]))u=i;if(u<0)break;used[u]=true;
                foreach(var r in Routes){int v=r.a==u?r.b:r.b==u?r.a:-1;if(v<0)continue;float d=distance[u]+Vector3.Distance(Points[u],Points[v]);if(d<distance[v]){distance[v]=d;previous[v]=u;}}
            }
            var last=Routes[endEdge];int finish=distance[last.a]+Vector3.Distance(Points[last.a],end)<distance[last.b]+Vector3.Distance(Points[last.b],end)?last.a:last.b;
            var path=new List<Vector3>{from,start};var nodes=new List<Vector3>();
            if(startEdge!=endEdge){for(int n=finish;n>=0;n=previous[n])nodes.Add(Points[n]);nodes.Reverse();path.AddRange(nodes);}
            path.Add(end);path.Add(to);
            var result=new List<Vector3>();for(int i=0;i<path.Count-1;i++){int steps=Mathf.Max(1,Mathf.CeilToInt(FlatDistance(path[i],path[i+1])/2));for(int k=0;k<steps;k++){Vector3 p=Vector3.Lerp(path[i],path[i+1],k/(float)steps);if(result.Count==0||FlatDistance(result[result.Count-1],p)>.3f)result.Add(p);}}result.Add(to);return result.ToArray();
        }
        public Vector3 DecorationOffset(int node,float radius=2.65f)
        {
            Vector3 best=Vector3.back*radius;float clearance=-1;
            for(int n=0;n<24;n++){
                float angle=n*Mathf.PI/12;Vector3 offset=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*radius;float nearest=100;
                foreach(var r in Routes){float t;nearest=Mathf.Min(nearest,SegmentDistance(Points[node]+offset,Points[r.a],Points[r.b],out t));}
                if(nearest>clearance){clearance=nearest;best=offset;}
            }
            return best;
        }
        public Vector3 ProjectToRoute(Vector3 p){Vector3 point;NearestEdge(p,out point);return point;}
        int NearestEdge(Vector3 p,out Vector3 point)
        {int best=0;float distance=float.PositiveInfinity;point=Points[0];for(int i=0;i<Routes.Length;i++){float t;float d=SegmentDistance(p,Points[Routes[i].a],Points[Routes[i].b],out t);if(d<distance){best=i;distance=d;point=Vector3.Lerp(Points[Routes[i].a],Points[Routes[i].b],t);}}return best;}
    }

    public static class IslandLayouts
    {
        static Vector3 P(float x,float z,float y=1.7f){return new Vector3(x,y,z);}
        static Vector4 L(float x,float z,float radius,float height){return new Vector4(x,z,radius,height);}
        static IslandRoute R(int a,int b,bool bridge=false,float width=4.6f){return new IslandRoute(a,b,bridge,width);}
        static readonly IslandLayout[] all={
            new IslandLayout(0,"岬角灯塔 · 双坡环线",new[]{P(0,7,1.5f),P(0,-4),P(-12,-7),P(12,-12,2),P(-5,-20,2.8f),P(-25,-21,3.4f),P(-17,-40,6),P(24,-31,4.1f),P(-20,-10,2.2f),P(20,-21,3),P(0,-43,5.1f),P(8,-29,3.5f)},new[]{R(0,1),R(1,2),R(1,3),R(2,8),R(8,5),R(5,6),R(6,10),R(10,11),R(11,9),R(9,3),R(9,7),R(1,4),R(4,5),R(4,11)},P(-17,-47,6),new[]{L(-14,-20,25,2.6f),L(-15,-40,17,5.2f),L(19,-24,17,3.3f)},new Vector4[0]),
            new IslandLayout(1,"珊瑚双翼 · 潟湖回环",new[]{P(0,7,1.5f),P(0,-3),P(-17,-9),P(17,-9),P(-22,-22,2),P(-26,-33,2.1f),P(25,-36,2.4f),P(1,-53,3.2f),P(-27,-17,1.9f),P(27,-23,2.1f),P(0,-46,3),P(16,-47,2.8f)},new[]{R(0,1),R(1,2),R(1,3),R(2,8),R(8,4),R(4,5),R(5,10,true),R(10,11),R(11,6),R(6,9),R(9,3),R(10,7),R(1,9,true)},P(24,-44,2.4f),new[]{L(-24,-25,15,1.8f),L(24,-26,15,2),L(0,-49,21,2.8f)},new[]{L(0,-25,17,1.1f)}),
            new IslandLayout(2,"三汊湿地 · 折桥净水道",new[]{P(0,7,1.5f),P(0,-4),P(-17,-10,1.8f),P(17,-9,2),P(15,-25,2.6f),P(-25,-29,2.2f),P(19,-45,3.4f),P(-29,-49,2.7f),P(-20,-20,2),P(27,-33,3),P(-5,-46,2.8f),P(-5,-27,2.4f)},new[]{R(0,1),R(1,2),R(1,3),R(2,8),R(8,5),R(5,11,true),R(11,4,true),R(4,9),R(9,6),R(6,10,true),R(10,7,true),R(7,5),R(3,4),R(11,10,true)},P(19,-51,3.4f),new[]{L(-20,-22,16,1.9f),L(21,-29,16,2.6f),L(-18,-49,15,2.5f),L(18,-46,13,3)},new[]{L(-3,-26,7,2.5f),L(10,-39,10,.4f)}),
            new IslandLayout(3,"盐阶古城 · 三层台地",new[]{P(0,7,1.5f),P(0,-4),P(14,-9,2),P(-14,-13,2.4f),P(-4,-24,4),P(-26,-28,4.3f),P(7,-46,7.1f),P(29,-32,5.4f),P(-24,-15,3),P(24,-20,3.7f),P(-17,-45,6.2f),P(9,-30,5.1f)},new[]{R(0,1),R(1,2),R(1,3),R(3,8),R(8,5),R(5,10),R(10,6),R(6,11),R(11,9),R(9,2),R(9,7),R(1,4),R(4,11),R(4,5)},P(7,-54,7.1f),new[]{L(0,-19,29,2.7f),L(-19,-35,20,4.3f),L(9,-44,20,6.5f),L(29,-29,13,4.8f)},new Vector4[0]),
            new IslandLayout(4,"断桅曲湾 · 船坞外环",new[]{P(0,7,1.5f),P(0,-4),P(17,-9),P(-15,-11,2),P(21,-23,2.2f),P(-29,-35,2.7f),P(-8,-49,3.4f),P(28,-40,3),P(-26,-20,2.3f),P(27,-30,2.5f),P(-26,-48,3.2f),P(6,-46,3.1f)},new[]{R(0,1),R(1,2),R(1,3),R(3,8),R(8,5),R(5,10),R(10,6),R(6,11),R(11,7,true),R(7,9),R(9,4),R(4,2),R(8,4,true)},P(-27,-42,2.7f),new[]{L(-24,-29,18,2.2f),L(-13,-48,21,3),L(26,-26,13,2.1f)},new[]{L(3,-28,15,1.1f)}),
            new IslandLayout(5,"寒霜裂谷 · 两岸阶坡",new[]{P(0,7,1.5f),P(0,-4),P(-16,-10,2),P(16,-11,2.2f),P(-20,-22,3.2f),P(-24,-32,4.2f),P(22,-43,6),P(-26,-52,6.3f),P(-27,-18,2.8f),P(27,-28,4.1f),P(-5,-44,5.3f),P(8,-29,4.2f)},new[]{R(0,1),R(1,2),R(1,3),R(2,8),R(8,4),R(4,5),R(5,10,true),R(10,6,true),R(6,9),R(9,3),R(5,7),R(7,10),R(9,11),R(11,4,true)},P(22,-50,6),new[]{L(-24,-30,15,3.9f),L(-23,-50,13,5.8f),L(23,-31,15,4.5f),L(22,-47,12,5.7f)},new[]{L(0,-32,9,2)}),
            new IslandLayout(6,"雷暴山脊 · 高低维修环",new[]{P(0,7,1.5f),P(0,-4),P(16,-9,2),P(-14,-11,2.3f),P(16,-27,4.5f),P(-24,-32,5.3f),P(22,-45,7),P(-24,-52,6.2f),P(-26,-19,3.2f),P(27,-32,5.3f),P(-9,-48,6.4f),P(0,-38,5.9f)},new[]{R(0,1),R(1,2),R(1,3),R(3,8),R(8,5),R(5,11,true),R(11,6),R(6,9),R(9,4),R(4,2),R(5,7),R(7,10),R(10,6),R(11,4)},P(22,-53,7),new[]{L(-24,-24,15,3.2f),L(20,-30,17,4.2f),L(3,-45,30,5.8f)},new[]{L(-4,-26,10,.68f)}),
            new IslandLayout(7,"熔潮环槽 · 火口双线",new[]{P(0,7,1.5f),P(0,-4),P(-16,-9,2),P(16,-10,2.3f),P(-22,-22,3.4f),P(-28,-36,4.7f),P(24,-40,5.4f),P(1,-56,6),P(-27,-17,2.8f),P(28,-25,3.7f),P(-15,-50,5.5f),P(14,-51,5.7f)},new[]{R(0,1),R(1,2),R(1,3),R(2,8),R(8,4),R(4,5),R(5,10),R(10,7),R(7,11),R(11,6),R(6,9),R(9,3),R(4,9,true)},P(1,-35,4.5f),new[]{L(-22,-28,17,3.8f),L(23,-28,17,4),L(0,-52,26,5.5f)},new[]{L(0,-30,16,.86f)}),
            new IslandLayout(8,"镜渊断层 · 错层折桥",new[]{P(0,7,1.5f),P(0,-4),P(17,-10,2.1f),P(-17,-9,2.5f),P(-20,-25,4.3f),P(-28,-39,5.6f),P(26,-45,7.3f),P(-12,-58,6.7f),P(-29,-20,3.3f),P(29,-27,4.4f),P(-5,-48,6.3f),P(10,-31,4.9f)},new[]{R(0,1),R(1,2),R(1,3),R(3,8),R(8,4),R(4,5),R(5,10,true),R(10,6,true),R(6,9),R(9,2),R(10,7),R(1,11,true),R(11,9),R(11,4,true)},P(26,-53,7.3f),new[]{L(-25,-29,16,4.1f),L(-16,-49,17,5.9f),L(24,-29,16,4.3f),L(24,-48,14,6.7f)},new[]{L(0,-30,9,1.8f)})
        };
        public static IslandLayout Get(int region){return all[Mathf.Clamp(region,0,8)];}
    }
}
