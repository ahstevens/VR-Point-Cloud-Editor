Shader "Custom/HeightShader" {
	Properties {
		zeroLevel ("zeroLevel", Float) = 55
		coloringSchema ("coloringSchema", Float) = 0
		hideNoData ("hideNoData", Float) = 0
		precisionNavUpperDepth ("precisionNavUpperDepth", Float) = 5
		precisionNavBottomDepth ("precisionNavBottomDepth", Float) = 10
		showContourLines ("showContourLines", Float) = 0
		
		contourStepSize ("contourStepSize", Float) = 0.05
		contourWidth ("contourWidth", Float) = 1.0
		
		_MainTex("noiseTexture", 2D) = "" {}
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		Pass
        {
			CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
			//#pragma geometry geom
			
			float zeroLevel;
			float coloringSchema;
			float hideNoData;
			float precisionNavUpperDepth;
			float precisionNavBottomDepth;
			float showContourLines;
			float contourStepSize;
			float contourWidth;

            struct appdata
            {
                float4 vertex : POSITION;
				float4 normal : NORMAL;
				float3 viewDir : TEXCOORD3;
				float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
				float4 normal : NORMAL;
				//float3 viewDir : VIEWDIRECTION;
				float3 lightDir : LIGHTDIRECTION;
				float4 vertexORGINAL : CUSTOM;
            };
			
			sampler2D _MainTex;
			
            v2f vert (appdata v)
            {
				if (v.vertex.y >= zeroLevel && hideNoData)
				{
					float count = 0;
					float sum = 10;
					sum /= count;
					v.vertex.y = sum;
				}
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
				//o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				//o.uv = v.uv * _MainTex.uv + _MainTex.zw
				o.uv = v.uv;
				
				o.vertexORGINAL = v.vertex;
				//o.viewDir = normalize(_WorldSpaceCameraPos - o.vertex);
				o.lightDir = normalize(_WorldSpaceLightPos0);
				o.normal = v.normal;
				
				
                return o;
            }
			
			//void geom(point v2f points[1], inout TriangleStream<v2f> triStream)
			//{
			//	if (points[0].vertex.y >= zeroLevel)
			//	{
			//		discard;
			//	}
			//}
			
			
            
			
			
            fixed4 frag (v2f i) : SV_Target
            {
				//if (i.vertexORGINAL.y >= zeroLevel)
				//	discard;
				
				fixed4 colBasedOnDepth = float4(0, 0, 0, 1);
				
				if (coloringSchema == 0)
				{
				
					float g = i.vertexORGINAL.y / (zeroLevel * 1.6);
					float b = 1 - g;
					
					colBasedOnDepth = float4(0, g, b, 1);
				}
				else if (coloringSchema == 1)
				{
					float rainbowMax = zeroLevel;
					float rainbowMin = 0;
					
					float stepSize = (rainbowMax - rainbowMin)/20.0;
					if (i.vertexORGINAL.y <= (rainbowMin + stepSize))
						colBasedOnDepth = float4(1.0, 0.0, 1.0, 1.0);
					else if (i.vertexORGINAL.y <= (rainbowMin + stepSize*2.0))
						colBasedOnDepth = float4(0.84, 0.0, 0.93, 1.0);
					else if (i.vertexORGINAL.y <= (rainbowMin + stepSize*3.0))
						colBasedOnDepth = float4(0.69, 0.0, 0.88, 1.0);
					else if (i.vertexORGINAL.y <= (rainbowMin + stepSize*4.0))
						colBasedOnDepth = float4(0.33, 0.0, 0.62, 1.0);
					else if (i.vertexORGINAL.y <= rainbowMin + (stepSize*5.0))
						colBasedOnDepth = float4(0.18, 0.0, 0.91, 1.0);
					else if (i.vertexORGINAL.y <= rainbowMin + (stepSize*6.0))
						colBasedOnDepth = float4(0.0, 0.18, 1.0, 1.0);
					else if (i.vertexORGINAL.y <= rainbowMin + (stepSize*7.0))
						colBasedOnDepth = float4(0.0, 0.55, 1.0, 1.0);
					else if (i.vertexORGINAL.y <= rainbowMin + (stepSize*8.0))
						colBasedOnDepth = float4(0.0, 0.78, 1.0, 1.0);
					else if (i.vertexORGINAL.y <= rainbowMin + (stepSize*9.0))
						colBasedOnDepth = float4(0.0, 1.0, 0.83, 1.0);
					else if (i.vertexORGINAL.y <= rainbowMin + (stepSize*10.0))
						colBasedOnDepth = float4(0.1, 1.0, 0.0, 1.0);
					else if (i.vertexORGINAL.y <= rainbowMin + (stepSize*11.0))
						colBasedOnDepth = float4(0.63, 1.0, 0.0, 1.0);
					else if (i.vertexORGINAL.y <= rainbowMin + (stepSize*12.0))
						colBasedOnDepth = float4(0.87, 1.0, 0.0, 1.0);
					else if (i.vertexORGINAL.y <= rainbowMin + (stepSize*13.0))
						colBasedOnDepth = float4(1.0, 0.98, 0.0, 1.0);
					else if (i.vertexORGINAL.y <= rainbowMin + (stepSize*14.0))
						colBasedOnDepth = float4(1.0, 0.85, 0.0, 1.0);
					else if (i.vertexORGINAL.y <= rainbowMin + (stepSize*15.0))
						colBasedOnDepth = float4(1.0, 0.6, 0.0, 1.0);
					else if (i.vertexORGINAL.y <= rainbowMin + (stepSize*16.0))
						colBasedOnDepth = float4(1.0, 0.42, 0.0, 1.0);
					else if (i.vertexORGINAL.y <= rainbowMin + (stepSize*17.0))
						colBasedOnDepth = float4(1.0, 0.0, 0.0, 1.0);
					else if (i.vertexORGINAL.y <= rainbowMin + (stepSize*18.0))
						colBasedOnDepth = float4(1.0, 0.58, 0.58, 1.0);
					else if (i.vertexORGINAL.y <= rainbowMin + (stepSize*19.0)) 
						colBasedOnDepth = float4(1.0, 0.85, 0.85, 1.0);
					else 
						colBasedOnDepth = float4(1.0, 1.0, 1.0, 1.0);
				}
				else if (coloringSchema == 2)
				{
					float rainbowMax = zeroLevel;
					float rainbowMin = 0;
					
					float factor = 6.0-((i.vertexORGINAL.y-rainbowMin)/(rainbowMax-rainbowMin))*6.0;
					int sextant = int(factor);
					float vsf = factor - float(sextant);
					float mid1 = vsf;
					float mid2 = 1.0 - vsf;
					if (sextant == 0)
						colBasedOnDepth = float4(1.0, 0.0, mid2, 1.0);
					else if (sextant == 1)
						colBasedOnDepth = float4(1.0, mid1, 0.0, 1.0);
					else if (sextant == 2)
						colBasedOnDepth = float4(mid2, 1.0, 0.0, 1.0);
					else if (sextant == 3)
						colBasedOnDepth = float4(0.0, 1.0, mid1, 1.0);
					else if (sextant == 4)
						colBasedOnDepth = float4(0.0, mid2, 1.0, 1.0);
					else if (sextant == 5)
						colBasedOnDepth = float4(mid1, 0.0, 1.0, 1.0);
				}
				else if (coloringSchema == 3)
				{
					if (abs(i.vertexORGINAL.y - zeroLevel) < precisionNavUpperDepth)
						colBasedOnDepth = float4(0.51, 0.76, 0.89, 1.0);
					else if (abs(i.vertexORGINAL.y - zeroLevel) > precisionNavUpperDepth && abs(i.vertexORGINAL.y - zeroLevel) <= precisionNavBottomDepth)
						colBasedOnDepth = float4(0.65, 0.88, 0.91, 1.0);
					else if (abs(i.vertexORGINAL.y - zeroLevel) > precisionNavBottomDepth)
						colBasedOnDepth = float4(0.82, 0.92, 0.89, 1.0);
				}
				
				if (showContourLines)
				{
					float contourSizeMajor = contourStepSize;
					float contourWidthMajor = contourWidth;
					
					
					
					//float3 fMajor  = abs(frac(abs(i.vertexORGINAL.y - zeroLevel) * contourSizeMajor) - 0.5);
					//float3 dfMajor = fwidth(abs(i.vertexORGINAL.y - zeroLevel) * contourSizeMajor);
					//float3 valMajor = contourWidthMajor * dfMajor;
					//float3 gMajor = smoothstep(-valMajor, (-valMajor * 0.05), fMajor) - smoothstep((valMajor * 0.05), valMajor, fMajor);
					//colBasedOnDepth = ( 0.5 * colBasedOnDepth) + ((1.0 - gMajor.z) * colBasedOnDepth * 0.5);
					
					float3 fMajor  = abs(frac(abs(i.vertexORGINAL.y - zeroLevel) * contourSizeMajor) ); //  - 0.5 * 0.105263158
					float3 dfMajor = fwidth(abs(i.vertexORGINAL.y - zeroLevel) * contourSizeMajor);
					float3 valMajor = contourWidthMajor * dfMajor;
					float3 gMajor = smoothstep(-valMajor, (-valMajor * 0.05), fMajor) - smoothstep((valMajor * 0.05), valMajor, fMajor);
					if (i.vertexORGINAL.y < zeroLevel * 0.99)
						colBasedOnDepth = ( 0.5 * colBasedOnDepth) + ((1.0 - gMajor.z) * colBasedOnDepth * 0.5);
				}
				
				float diffuseFactor = max(dot(i.normal, i.lightDir), 0.0); 
				float3 lighting = diffuseFactor * float3(1.0, 1.0, 1.0);
				
				colBasedOnDepth = colBasedOnDepth * 0.8 + float4(lighting * 0.1, 1.0);
				colBasedOnDepth = colBasedOnDepth * 0.9 + tex2D(_MainTex, i.uv * 500.0) * 0.1;

				return colBasedOnDepth;
				
            }
            ENDCG
		}
	}
}