Shader "Fade/Diffuse"
{
	Properties
	{
		_Color("Main Color", Color) = (1,1,1,1)
		_MainTex("Base (RGB) Trans (A)", 2D) = "white" {}
		_AlphaTex("Alpha", 2D) = "white" {}
		_Cutoff("Alpha cutoff", Range(0,1)) = 0.5
	}

		SubShader
		{
			Tags {"Queue" = "AlphaTest" "IgnoreProjector" = "True" "RenderType" = "TransparentCutout"}
			LOD 300
			Cull off

			CGPROGRAM
			#pragma surface surf Lambert alphatest:_Cutoff

			sampler2D _MainTex;
			sampler2D _AlphaTex;
			fixed4 _Color;

			struct Input
			{
				float2 uv_MainTex;
				float2 uv_AlphaTex;
			};

			void surf(Input IN, inout SurfaceOutput o)
			{
				fixed4 MAIN = tex2D(_MainTex, IN.uv_MainTex) * _Color;
				fixed4 ALPHA = tex2D(_AlphaTex, IN.uv_AlphaTex);
				o.Albedo = MAIN.rgb;
				o.Alpha = ALPHA;
			}
			ENDCG
		}

			FallBack "Transparent/Cutout/Diffuse"
}