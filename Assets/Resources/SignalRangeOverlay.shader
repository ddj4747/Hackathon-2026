Shader "Game/SignalRangeOverlay"
{
    Properties
    {
        _OverlayColor ("Range color", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _OverlayColor;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 edges : COLOR;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.edges = input.color;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                // One shader pixel matches one pixel of a 16 x 16 grid tile.
                float2 pixel = floor(saturate(input.uv) * 15.999);
                // A tile edge is 16 pixels, an exact multiple of the eight-pixel
                // pattern, so the dashes continue around adjoining tiles and turns.
                float stepOffset = floor(_Time.y * 6.0);
                float4 distanceAlong = float4(pixel.y, 15.0 - pixel.y,
                    15.0 - pixel.x, pixel.x);
                float4 phase = frac((distanceAlong - stepOffset) / 8.0) * 8.0;
                bool outline = (pixel.x < 1.0 && input.edges.r > 0.5 && phase.r < 4.0) ||
                    (pixel.x >= 15.0 && input.edges.g > 0.5 && phase.g < 4.0) ||
                    (pixel.y < 1.0 && input.edges.b > 0.5 && phase.b < 4.0) ||
                    (pixel.y >= 15.0 && input.edges.a > 0.5 && phase.a < 4.0);
                return half4(_OverlayColor.rgb, outline ? _OverlayColor.a : 0.0h);
            }
            ENDHLSL
        }
    }
}
