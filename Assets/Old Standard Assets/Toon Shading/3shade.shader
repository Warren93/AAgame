Shader "3shade" {
    Properties {
    	_Color1 ("Color 1", Color) = (1.0,1.0,1.0,1)
		_Color2 ("Color 2", Color) = (0.5,0.5,0.5,1)
		_Color3 ("Color 3", Color) = (0.0,0.0,0.0,1)
      	_MainTex ("Texture", 2D) = "white" {}
      	_OutlineColor ("Outline Color", Color) = (0,0,0,1)
		//_Outline ("Outline width", Range (.002, 0.03)) = .005
		//_Outline ("Outline width", Range (0.001, 0.003)) = .002
		_Outline ("Outline width", float) = 0.001
    }
    SubShader {
      Tags { "RenderType" = "Opaque" }
      UsePass "Toon/Basic Outline/OUTLINE"
      CGPROGRAM
      #pragma surface surf SimpleLambert
      
      struct Input {
          float2 uv_MainTex;
      };
      
      sampler2D _MainTex;
      float4 _Color1;
      float4 _Color2;
      float4 _Color3;
      
      half4 LightingSimpleLambert (SurfaceOutput s, half3 lightDir, half atten) {
		#ifndef USING_DIRECTIONAL_LIGHT
		lightDir = normalize(lightDir);
		#endif
	
		half d = dot (s.Normal, lightDir)*0.5 + 0.5;
	
		half4 s_initial = half4(0, 0, 0, 0);
		s_initial.rgb = (s.Albedo + s.Normal + s.Emission).rgb;
		float n = (s_initial.r + s_initial.g + s_initial.b) / 3;
		s_initial.rgb = (n, n, n);

		half4 c = half4(0, 0, 0, 0);
		c.rgb = s_initial * _LightColor0.rgb * d * (atten * 2);
		c.a = 0;
		float avg = (c.r + c.g + c.b) / 3;
		if (avg > 0.66)
			c.rgb = _Color1;
		else if (avg > 0.33)
			c.rgb = _Color2;
		else if (avg > 0)
			c.rgb = _Color3;
		return c;
      }
      
      void surf (Input IN, inout SurfaceOutput o) {
      	o.Albedo = tex2D (_MainTex, IN.uv_MainTex).rgb;
      }
      
      ENDCG
    }
    Fallback "Diffuse"
  }