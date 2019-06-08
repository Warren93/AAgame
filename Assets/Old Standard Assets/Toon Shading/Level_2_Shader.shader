Shader "Level_2_Shader" {
	Properties {
		_Color ("Main Color", Color) = (0.5,0.5,0.5,1)
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_Ramp ("Toon Ramp (RGB)", 2D) = "gray" {}
		_OutlineColor("Outline Color", Color) = (0,0,0,1)
		_Outline("Outline width", float) = 0.001
	}

	SubShader {
		Tags { "RenderType"="Opaque" }
		UsePass "Toon/Basic Outline/OUTLINE"
		
CGPROGRAM
#pragma surface surf ToonRamp

sampler2D _Ramp;

		struct SurfaceOutputCustom
		{
			fixed3 Albedo;
			fixed3 Normal;
			fixed3 Emission;
			half Specular;
			fixed Gloss;
			fixed Alpha;

			half4 Override;
		};

// custom lighting function that uses a texture ramp based
// on angle between light direction and normal
#pragma lighting ToonRamp exclude_path:prepass
inline half4 LightingToonRamp (SurfaceOutputCustom s, half3 lightDir, half atten)
{

	if (s.Override.r == 1)
		return s.Override;

	#ifndef USING_DIRECTIONAL_LIGHT
	lightDir = normalize(lightDir);
	#endif
	
	half d = dot (s.Normal, lightDir)*0.5 + 0.5;
	half3 ramp = tex2D (_Ramp, float2(d,d)).rgb;
	
	half4 c;
	c.rgb = s.Albedo * _LightColor0.rgb * ramp * (atten * 2);
	c.a = 0;
	return c;
}


sampler2D _MainTex;
float4 _Color;

struct Input {
	float2 uv_MainTex : TEXCOORD0;
};

void surf (Input IN, inout SurfaceOutputCustom o) {
	half4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
	o.Albedo = c.rgb;
	o.Alpha = c.a;

	half4 n = tex2D(_MainTex, IN.uv_MainTex);
	if (n.r == 1 && n.g == 1 && n.b == 1 && n.a == 1)
		o.Override = half4(1, 1, 1, 1);
	else
		o.Override.r = 0.5;
}
ENDCG

	} 

	Fallback "Diffuse"
}
