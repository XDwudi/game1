Shader "Tidebreak/Ocean" {
 Properties { _DeepColor("Deep", Color)=(0.02,0.4,0.48,1) _CrestColor("Crest",Color)=(0.4,0.8,0.75,1) }
 SubShader {
  Tags { "RenderType"="Opaque" }
  Pass {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile_fog
   #include "UnityCG.cginc"
   struct appdata { float4 vertex:POSITION; };
   struct v2f { float4 pos:SV_POSITION; float3 world:TEXCOORD0; UNITY_FOG_COORDS(1) float4 screen:TEXCOORD2; float eye:TEXCOORD3; };
   UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
   fixed4 _DeepColor, _CrestColor;
   float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
   float noise(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);}
   v2f vert(appdata v) { v2f o; v.vertex.y+=sin(v.vertex.x*.36+v.vertex.z*.21+_Time.y*.8)*.075+sin(v.vertex.x*-.19+v.vertex.z*.31+_Time.y*1.1)*.06; o.pos=UnityObjectToClipPos(v.vertex); o.screen=ComputeScreenPos(o.pos);o.eye=-UnityObjectToViewPos(v.vertex).z;o.world=mul(unity_ObjectToWorld,v.vertex).xyz; UNITY_TRANSFER_FOG(o,o.pos); return o; }
   fixed4 frag(v2f i):SV_Target {
    float2 p=i.world.xz; float t=_Time.y;
    float distance=length(_WorldSpaceCameraPos-i.world);float fade=lerp(.1,1,saturate(1-distance/100));
    float warp=noise(p*.075+float2(t*.016,0))*9;
    float a=p.x*.63+p.y*.47+t*.9+warp,b=p.x*-.39+p.y*.86+t*.7+warp*.6;
    float2 flow=p*.21+float2(t*.035,t*-.027);
    float h=noise(flow),hx=noise(flow+float2(.12,0)),hz=noise(flow+float2(0,.12));
    float3 n=normalize(float3(((h-hx)*1.7-cos(a)*.028)*fade,1,((h-hz)*1.7+cos(b)*.024)*fade));
    float3 view=normalize(_WorldSpaceCameraPos-i.world);
    float fres=pow(1-saturate(dot(view,n)),4);
    float spec=pow(saturate(dot(n,normalize(view+normalize(_WorldSpaceLightPos0.xyz)))),180);
    // Convert eye-depth separation to vertical depth. Otherwise a camera angle
    // changes the shoal colour and reveals the finite seabed mesh boundary.
    float eyeGap=max(0,LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture,UNITY_PROJ_COORD(i.screen)))-i.eye);
    float waterDepth=eyeGap*abs(view.y)/max(.08,dot(view,UNITY_MATRIX_V[2].xyz));
    float shallow=1-smoothstep(.2,4,waterDepth);
    fixed4 c=lerp(_DeepColor,_CrestColor,shallow*.55+sin(a)*sin(b)*.028*fade+noise(p*.2+t*.035)*.05+.06);
    c.rgb=lerp(c.rgb,unity_FogColor.rgb*.8,fres*.33)+spec*.55;
    float ripples=sin(p.x*2.2+sin(p.y*1.3+t)*1.5+t)*sin(p.y*2.6-t);
    c.rgb+=smoothstep(.94,.99,ripples)*.028*saturate(1-distance/75);
    float foam=(1-smoothstep(.05,.55,waterDepth))*(.65+.35*sin(p.x*3+p.y*2-t*2));
    c.rgb=lerp(c.rgb,_CrestColor.rgb*1.25,saturate(foam)*.75);
    UNITY_APPLY_FOG(i.fogCoord,c); return c;
   }
   ENDCG
  }
 }
}
