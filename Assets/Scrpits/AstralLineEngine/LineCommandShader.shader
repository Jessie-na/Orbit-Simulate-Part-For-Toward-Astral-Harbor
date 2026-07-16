Shader "ALE/ALE_Line" {
    SubShader {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+1000" }
        Pass {
            ZWrite Off
            ZTest Always
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag

            struct v2f {
                float4 pos : SV_POSITION;
                float4 col : COLOR;
            };

            struct LineData {
                float3 start;
                float3 end;
                float4 color;
                float width;
            };

            StructuredBuffer<LineData> _LineBuffer;
            float4x4 _ALE_VP;
            float _ALE_FlipY;

            v2f vert (uint id : SV_VertexID) {
                v2f o;
                LineData l = _LineBuffer[id / 2];
                float3 worldPos = (id % 2 == 0) ? l.start : l.end;

                o.pos = mul(_ALE_VP, float4(worldPos, 1.0));
                // HDRP CustomPass 渲染到 RenderTexture 时修正 Y 轴方向
                o.pos.y *= _ALE_FlipY;
                o.col = l.color;
                return o;
            }

            float4 frag (v2f i) : SV_Target {
                return i.col;
            }
            ENDHLSL
        }
    }
}
