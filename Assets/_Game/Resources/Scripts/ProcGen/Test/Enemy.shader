Shader "Minimap/Enemy" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_Color ("Main Color", Color) = (1,1,1,1)

	}
	SubShader {
		Tags { "RenderType"="Enemy" }
		LOD 200
		
		
		
		CGPROGRAM
		#pragma surface surf Lambert
		

		sampler2D _MainTex;
		half4 _Color;

		struct Input {
			float2 uv_MainTex;
		};

		void surf (Input IN, inout SurfaceOutput o) {
			half4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
			o.Albedo = c.rgb;
			o.Alpha = c.a;
		}
		ENDCG
		
	} 
	FallBack "Diffuse"
}
