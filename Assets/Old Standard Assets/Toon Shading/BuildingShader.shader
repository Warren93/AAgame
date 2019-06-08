Shader "BuildingShader" {
    Properties {
    	_Color1 ("Color 1", Color) = (1.0,1.0,1.0,1)
		_Color2 ("Color 2", Color) = (0.8,0.8,0.8,1)
		_Color3 ("Color 3", Color) = (0.6,0.6,0.6,1)
		_Color4 ("Color 4", Color) = (0.4,0.4,0.4,1)
		_Color5 ("Color 5", Color) = (0.2,0.2,0.2,1)
      	_MainTex ("Texture", 2D) = "white" {}
    }
    SubShader {
      Tags { "RenderType" = "Opaque" }
      CGPROGRAM
      #pragma surface surf SimpleLambert
      
      struct Input {
          float2 uv_MainTex;
      };
      
      sampler2D _MainTex;
      float4 _Color1;
      float4 _Color2;
      float4 _Color3;
      float4 _Color4;
      float4 _Color5;
      
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
		
		// if texture is all black or all white here, return either black or white
		// since we want the building textures will have black-framed white windows
		// that we want to be unaffected by lighting
		if (s.Albedo.r == 0 && s.Albedo.g == 0 && s.Albedo.b == 0)
			return half4(0, 0, 0, 1);
		else if (s.Albedo.r == 1 && s.Albedo.g == 1 && s.Albedo.b == 1)
			return half4(1, 1, 1, 1);
		
		//if (c.rgb == half3(0, 0, 0) || c.rgb == half3(1, 1, 1))
		//	return c;
		
		float avg = (c.r + c.g + c.b) / 3;
		if (avg > 0.8)
			c = _Color1;
		else if (avg > 0.6)
			c = _Color2;
		else if (avg > 0.4)
			c = _Color3;
		else if (avg > 0.2)
			c = _Color4;
		else if (avg > 0)
			c = _Color5;
		return c;
      }
      
      void surf (Input IN, inout SurfaceOutput o) {
      	o.Albedo = tex2D (_MainTex, IN.uv_MainTex).rgb;
      }
      
      ENDCG
    }
    Fallback "Diffuse"
  }