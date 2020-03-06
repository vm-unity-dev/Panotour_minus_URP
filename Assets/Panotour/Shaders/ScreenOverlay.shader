Shader "Mbryonic/ScreenOverlay" {
	Properties{
		_Color("Main Color", Color) = (1,1,1,1)
	}

		SubShader{
		Tags{ "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
		LOD 100
		Fog{ Mode Off }
		ZTest Always Cull Off ZWrite Off
		ColorMask RGB
		Blend SrcAlpha OneMinusSrcAlpha
		Color[_Color]

		Pass{}
	} 
}
