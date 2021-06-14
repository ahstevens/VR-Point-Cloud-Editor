 Shader "TintedHeight" {
 
    Properties 
    {
      _MainTex ("Base (RGB)", 2D) = "white" {}
      _HeightMin ("Height Min", Float) = -30
      _HeightMid("Height Mid", Float) = -20
      _HeightMax ("Height Max", Float) = 0
      _HeightWarning("Height Warning", Float) = 0
      _ColorMin ("Tint Color At Min", Color) = (0.1,0.3,0.8,1)
      _ColorMid("Tint Color At Mid", Color) = (0.01,0.2,0.6,1)
      _ColorMax ("Tint Color At Max", Color) = (0,0.6,1,1)
	  _ColorAbove ("Tint Color Above Water", Color) = (0.0,0.5,0.0,0.0)
      _ColorWarning("Tint Color Warning", Color) = (1.0,0.0,0.0,0.0)
    }
   
     SubShader {
		 Blend SrcAlpha OneMinusSrcAlpha
         Pass {
 
             CGPROGRAM
 
			 #pragma vertex vert
             #pragma fragment frag
			 #include "UnityCG.cginc"
 
             float _HeightWarning;
             float _HeightMax;
             float _HeightMin;
             float _HeightMid;
             fixed4 _ColorMin;
             fixed4 _ColorMid;
             fixed4 _ColorMax;
			 fixed4 _ColorAbove;
             fixed4 _ColorWarning;
 
             struct Input
          {
            float3 worldPos;
          };
 
             struct v2f {
                 float4 pos : SV_POSITION;
                 fixed3 color : COLOR0;
             };
 
             v2f vert (appdata_base v)
             {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                float3 worldPos = mul (unity_ObjectToWorld, v.vertex).xyz;
                if (worldPos.y >= -0.01)
                {
                    o.color = (0.62, 0.67, 0.27, 0.0);
                }
                else if (worldPos.y >= _HeightWarning) 
				{ 
					o.color = _ColorWarning.rgba; 
				} 
				else 
				{
                    if (worldPos.y >= _HeightMid)
                    {
                        float h = (_HeightMax - worldPos.y) / (_HeightMax - _HeightMid);
                        o.color = lerp(_ColorMax.rgba, _ColorMid.rgba, h);
                    }
                    else
                    {
                        float h = (_HeightMid - worldPos.y) / (_HeightMid - _HeightMin);
                        o.color = lerp(_ColorMid.rgba, _ColorMin.rgba, h);
                    }
					
				}
                 
                 return o;    
             }
 
             fixed4 frag (v2f i) : SV_Target
             {
                 return fixed4 (i.color, 1);
             }
             ENDCG
 
         }
     }
 }