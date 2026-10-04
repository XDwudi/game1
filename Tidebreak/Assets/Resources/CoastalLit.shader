Shader "Tidebreak/Coastal Lit" {
 Properties {
  _Color("Pigment",Color)=(1,1,1,1)
  _Glossiness("Smoothness",Range(0,1))=.24
  _Metallic("Metal",Range(0,1))=0
  _EmissionColor("Emission",Color)=(0,0,0,0)
 }
 SubShader {
  Tags { "RenderType"="Opaque" }
  CGPROGRAM
  #pragma surface surf Standard fullforwardshadows
  #pragma target 3.0
  struct Input { float3 worldPos; float3 viewDir; };
  fixed4 _Color, _EmissionColor; half _Glossiness, _Metallic;
  void surf(Input IN,inout SurfaceOutputStandard o) {
   // Broad pigment variation preserves readable silhouettes at gameplay distance.
   float grain=sin(IN.worldPos.x*21+sin(IN.worldPos.z*9))*sin(IN.worldPos.y*29+IN.worldPos.z*23);
   o.Albedo=_Color.rgb*(.98+grain*.018);
   o.Metallic=_Metallic; o.Smoothness=_Glossiness; o.Occlusion=1;
   float rim=pow(1-saturate(dot(normalize(IN.viewDir),float3(0,0,1))),4);
   o.Emission=_EmissionColor.rgb+_Color.rgb*rim*.045; o.Alpha=1;
  }
  ENDCG
 }
 Fallback "Standard"
}
