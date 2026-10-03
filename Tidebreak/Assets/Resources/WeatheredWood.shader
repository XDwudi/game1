Shader "Tidebreak/WeatheredWood" {
 Properties {_Color("Wood colour",Color)=(.39,.25,.15,1)}
 SubShader {
 Tags {"RenderType"="Opaque"}
 CGPROGRAM
 #pragma surface surf Standard fullforwardshadows
 #pragma target 3.0
 struct Input {float3 worldPos;};fixed4 _Color;
 float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
 float noise(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);}
 void surf(Input IN,inout SurfaceOutputStandard o) {
  float3 p=IN.worldPos;float2 uv=float2(p.x*.8+p.y*.2,p.z*28+p.y*17);
  float streak=noise(uv+noise(uv*.3)*2);float fine=sin(uv.y*6+sin(uv.x*3)*2)*.024;
  o.Albedo=_Color.rgb*(.88+streak*.22+fine);o.Metallic=0;o.Smoothness=.15;o.Alpha=1;
 }
 ENDCG
 }
 Fallback "Diffuse"
}
