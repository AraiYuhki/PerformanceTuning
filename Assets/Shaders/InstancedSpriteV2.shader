Shader "Sprites/InstancedSpriteV2"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_local _ PIXELSNAP_ON

            #include "UnityCG.cginc"

            fixed4 _Color;
            sampler2D _MainTex;

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float2 texcoord : TEXCOORD0;
                uint instancedId : SV_InstanceID;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            StructuredBuffer<float3> _Positions;
            StructuredBuffer<float4> _Rotations;
            StructuredBuffer<float3> _Scales;
            StructuredBuffer<float4> _SpriteRects;

            float3 RotateByQuaternion(float4 quaternion, float3 position)
            {
                float3 t = 2.0 * cross(quaternion.xyz, position);
                return position + quaternion.w * t + cross(quaternion.xyz, t);
            }


            v2f vert(appdata_t IN)
            {
                v2f OUT;

                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                uint instancedId = IN.instancedId;

                float3 position = _Positions[instancedId];
                float4 rotation = _Rotations[instancedId];
                float3 scale = _Scales[instancedId];

                float3 resultPosition = IN.vertex.xyz * scale;
                resultPosition = RotateByQuaternion(rotation, resultPosition);
                resultPosition += position;
                OUT.vertex = mul(UNITY_MATRIX_VP, float4(resultPosition, 1.0));

                // _SpriteRect: xy = scale (tiling), zw = offset
                float4 rect = _SpriteRects[instancedId];
                OUT.texcoord = IN.texcoord * rect.xy + rect.zw;

                #ifdef PIXELSNAP_ON
                OUT.vertex = UnityPixelSnap(OUT.vertex);
                #endif

                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 color = tex2D(_MainTex, IN.texcoord) * _Color;
                clip(color.a - 0.01);
                return color;
            }
            ENDCG
        }
    }

    Fallback "Sprites/Default"
}

