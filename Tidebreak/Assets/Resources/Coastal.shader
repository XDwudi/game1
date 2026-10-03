Shader "Tidebreak/Coastal" {
 Properties { _Color("Tint",Color)=(1,1,1,1) }
 SubShader {
 Tags { "RenderType"="Opaque" }
 Cull Off
 CGPROGRAM
 #pragma surface surf Standard fullforwardshadows
 #pragma target 3.0
 struct Input { float4 color:COLOR; float3 worldPos; };
 fixed4 _Color;
 void surf(Input IN,inout SurfaceOutputStandard o) {
   float grain=sin(IN.worldPos.x*37+sin(IN.worldPos.z*14))*sin(IN.worldPos.z*31+IN.worldPos.y*23);
   o.Albedo=GammaToLinearSpace(IN.color.rgb)*_Color.rgb*(.98+grain*.025); o.Metallic=0; o.Smoothness=.12; o.Alpha=1;
 }
 ENDCG
 }
 Fallback "Diffuse"
}
