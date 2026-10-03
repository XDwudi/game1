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
   struct v2f { float4 pos:SV_POSITION; float3 world:TEXCOORD0; UNITY_FOG_COORDS(1) };
   fixed4 _DeepColor, _CrestColor;
   v2f vert(appdata v) { v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.world=mul(unity_ObjectToWorld,v.vertex).xyz; UNITY_TRANSFER_FOG(o,o.pos); return o; }
   fixed4 frag(v2f i):SV_Target {
    float2 p=i.world.xz; float t=_Time.y;
    float w=sin(p.x*.42+t*.7+sin(p.y*.22+t*.22)*1.5)*sin(p.y*.39-t*.6);
    float fine=sin(p.x*1.8+p.y*.44+t)*sin(p.y*1.2-t*.7);
    float crest=smoothstep(.64,.92,w)*.5+smoothstep(.82,.97,fine)*.13;
    float3 view=normalize(_WorldSpaceCameraPos-i.world);
    float sheen=pow(saturate(1-view.y),4)*.19;
    fixed4 c=lerp(_DeepColor,_CrestColor,crest+sheen);
    c.rgb+=pow(saturate(sin(p.x*.12+p.y*.03)*.5+.5),24)*.03;
    UNITY_APPLY_FOG(i.fogCoord,c); return c;
   }
   ENDCG
  }
 }
}
