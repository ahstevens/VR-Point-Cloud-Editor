Shader "Infinity Code/Online Maps/Tileset Cutout" 
{
	Properties 
	{
		_Color("Main Color", Color) = (1,1,1,1)
		_MainTex("Base (RGB) Trans (A)", 2D) = "white" {}
		_OverlayBackTex("Overlay Back Texture", 2D) = "black" {}
		_OverlayBackAlpha("Overlay Back Alpha", Range(0, 1)) = 1
		_TrafficTex("Traffic Texture", 2D) = "black" {}
		_OverlayFrontTex("Overlay Front Texture", 2D) = "black" {}
		_OverlayFrontAlpha("Overlay Front Alpha", Range(0, 1)) = 1
	}

	SubShader 
	{
		Tags {"Queue"="AlphaTest-300" "IgnoreProjector"="True" "RenderType"="TransparentCutout"}
		LOD 200

		CGPROGRAM
		#pragma surface surf Lambert alphatest:_Cutoff 

		sampler2D _MainTex;
		sampler2D _OverlayBackTex;
		half _OverlayBackAlpha;
		sampler2D _TrafficTex;
		sampler2D _OverlayFrontTex;
		half _OverlayFrontAlpha;
		fixed4 _Color;

		struct Input
		{
			float2 uv_MainTex;
			float2 uv_OverlayBackTex;
			float2 uv_TrafficTex;
			float2 uv_OverlayFrontTex;
		};

		void surf(Input IN, inout SurfaceOutput o)
		{
			fixed4 c = tex2D(_MainTex, IN.uv_MainTex);

			fixed4 t = tex2D(_OverlayBackTex, IN.uv_OverlayBackTex);
			fixed3 ct = lerp(c.rgb, t.rgb, t.a * _OverlayBackAlpha);

			t = tex2D(_TrafficTex, IN.uv_TrafficTex);
			ct = lerp(ct, t.rgb, t.a);

			t = tex2D(_OverlayFrontTex, IN.uv_OverlayFrontTex);
			ct = lerp(ct, t.rgb, t.a * _OverlayFrontAlpha);

			ct = ct * _Color;
			o.Albedo = ct;
			o.Alpha = c.a * _Color.a;
		}
		ENDCG
	}

	Fallback "Transparent/Cutout/Diffuse"
}
