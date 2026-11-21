// Soft white glow band along a connection beam (segment _LineA -> _LineB in world space).
// Brightness falls off with a Gaussian of the distance to the segment, measured in pixels
// on the world pixel grid, so the band stays pixel-art crisp at any angle. Additive.
Shader "Hackathon/CircuitGlowBeam"
{
    Properties
    {
        _GlowColor ("Glow Color", Color) = (1, 1, 1, 1)
        _Intensity ("Intensity", Range(0, 2)) = 1
        _HalfWidthPx ("Half Width (px)", Range(0.5, 16)) = 4
        _PixelsPerUnit ("Pixels Per Unit", Float) = 16
        [Toggle] _PixelSnap ("Pixel Snap", Float) = 1
        [PerRendererData] _LineA ("Line Start (world)", Vector) = (0, 0, 0, 0)
        [PerRendererData] _LineB ("Line End (world)", Vector) = (1, 0, 0, 0)
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Blend SrcAlpha One
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 positionWS : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _GlowColor;
                float _Intensity;
                float _HalfWidthPx;
                float _PixelsPerUnit;
                float _PixelSnap;
                float4 _LineA;
                float4 _LineB;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 ws = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(ws);
                output.positionWS = ws.xy;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float ppu = _PixelsPerUnit;
                float2 p = input.positionWS * ppu;
                if (_PixelSnap > 0.5)
                    p = floor(p) + 0.5;

                float2 a = _LineA.xy * ppu;
                float2 b = _LineB.xy * ppu;
                float2 ab = b - a;
                float t = saturate(dot(p - a, ab) / max(dot(ab, ab), 1e-5));
                float d = length(p - (a + ab * t));

                float sigma = max(_HalfWidthPx * 0.45, 0.5);
                float glow = exp(-d * d / (2 * sigma * sigma));
                return half4(_GlowColor.rgb, glow * _GlowColor.a * _Intensity);
            }
            ENDHLSL
        }
    }
}
