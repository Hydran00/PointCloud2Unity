Shader "Custom/PointCloudShader"
{
	Properties
	{
		_PointSize("PointSize", Float) = 1
	}

	SubShader
	{
		Pass
		{
			LOD 200

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			//
			#include "UnityCG.cginc"
			//
			struct VertexInput
			{
				float4 v : POSITION;
				float4 color: COLOR;
				//
				UNITY_VERTEX_INPUT_INSTANCE_ID
				//
			};

			struct VertexOutput
			{
				float4 pos : SV_POSITION;
				float4 col : COLOR;
				float size : PSIZE;
				//
				UNITY_VERTEX_OUTPUT_STEREO
				//
			};

			float _PointSize;

			VertexOutput vert(VertexInput v)
			{
				VertexOutput o;

				//
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_OUTPUT(VertexOutput, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				//

				o.pos = UnityObjectToClipPos(v.v);
				o.size = _PointSize;
				o.col = v.color;

				return o;
			}

			float4 frag(VertexOutput o) : COLOR
			{
				return o.col;
			}

			ENDCG
		}
	}
}
