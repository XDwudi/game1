Shader "Tidebreak/CoastalSky" {
 Properties { _SkyTint("Zenith",Color)=(.3,.52,.73,1) _Horizon("Horizon",Color)=(.75,.84,.84,1) _Cloud("Cloud",Color)=(.96,.95,.87,1) _SunDirection("Sun direction",Vector)=(.4,.6,.3,0) _Exposure("Exposure",Float)=1 }
 SubShader {
 Tags {"Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox"} Cull Off ZWrite Off
 Pass {
 CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 struct appdata {float4 vertex:POSITION;}; struct v2f {float4 pos:SV_POSITION;float3 dir:TEXCOORD0;};
 float4 _SkyTint,_Horizon,_Cloud,_SunDirection;float _Exposure;
 float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
 float noise(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);}
 float fbm(float2 p){float n=0,a=.55;for(int i=0;i<5;i++){n+=a*noise(p);p=mul(float2x2(1.7,-1.2,1.2,1.7),p)+13.5;a*=.48;}return n;}
 v2f vert(appdata v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.dir=v.vertex.xyz;return o;}
 fixed4 frag(v2f i):SV_Target {
  float3 d=normalize(i.dir);float h=max(0,d.y);float3 c=lerp(_Horizon.rgb,_SkyTint.rgb,pow(saturate(h),.45));
  float2 uv=d.xz/max(.065,h)*2.7+float2(_Time.y*.006,0);
  float n=fbm(uv);float cloud=smoothstep(.55,.79,n)*smoothstep(.06,.24,h);
  c=lerp(c,_Cloud.rgb*(.76+n*.25),cloud*.82);
  float sun=pow(saturate(dot(d,normalize(_SunDirection.xyz))),900);float halo=pow(saturate(dot(d,normalize(_SunDirection.xyz))),25)*.06;
  c+=(sun*.8+halo)*float3(1,.81,.55);return fixed4(c*_Exposure,1);
 }
 ENDCG
 }
 }
}
