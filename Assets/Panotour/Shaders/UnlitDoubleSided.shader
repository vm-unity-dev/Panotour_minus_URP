Shader "Unlit/UnlitDoubleSided"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_LOD("LOD",Float) = 1
		_Tint("Tint", Color) = (1,1,1,1)
	}
		SubShader
	{
		Tags { "RenderType" = "Opaque" }
		LOD 100
		Cull Off
		ZWrite On
		Pass
		{
			Cull Off
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			// make fog work
			#pragma multi_compile_fog
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				UNITY_FOG_COORDS(1)
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			float _LOD;
			float4 _Tint;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				// sample the texture
				//fixed4 col = tex2D(_MainTex, float2(1-i.uv.x,i.uv.y));
				fixed4 col = tex2Dlod(_MainTex, float4(1-i.uv.x,i.uv.y,0,_LOD));
				// apply fog
				UNITY_APPLY_FOG(i.fogCoord, col);
				return col * _Tint;
			}
			ENDCG
		}
	}
}
