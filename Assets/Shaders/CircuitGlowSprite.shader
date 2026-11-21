// Soft white glow around a sprite's silhouette. Drawn on a padded quad behind the sprite
// (created by CircuitAura). The sprite alpha is blurred with a Gaussian kernel; the result
// is added on top of what is behind (additive), optionally snapped to sprite pixels.
Shader "Hackathon/CircuitGlowSprite"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _GlowColor ("Glow Color", Color) = (1, 1, 1, 1)
        _Intensity ("Intensity", Range(0, 2)) = 1
        _RadiusPx ("Radius (sprite px)", Range(0.5, 12)) = 4
        _Strength ("Strength", Range(0.5, 4)) = 1.5
        [Toggle] _PixelSnap ("Pixel Snap", Float) = 1
        [PerRendererData] _SpriteRectPx ("Sprite Rect in texture (px)", Vector) = (0, 0, 16, 16)
        [PerRendererData] _TexSizePx ("Texture Size (px)", Vector) = (16, 16, 0, 0)
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

            #define TAPS 5

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0; // sprite pixel coordinates (0..width, 0..height), extends past the sprite by the padding
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 px : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _GlowColor;
                float _Intensity;
                float _RadiusPx;
                float _Strength;
                float _PixelSnap;
                float4 _SpriteRectPx;
                float4 _TexSizePx;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.px = input.uv;
                return output;
            }

            float AlphaAt(float2 px)
            {
                if (px.x < 0 || px.y < 0 || px.x >= _SpriteRectPx.z || px.y >= _SpriteRectPx.w)
                    return 0;
                float2 uv = (_SpriteRectPx.xy + floor(px) + 0.5) / _TexSizePx.xy;
                return SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, uv, 0).a;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 p = input.px;
                if (_PixelSnap > 0.5)
                    p = floor(p) + 0.5;

                float sigma = max(_RadiusPx * 0.5, 0.5);
                float stepPx = _RadiusPx / TAPS;
                float sum = 0;
                float weightSum = 0;

                [unroll] for (int y = -TAPS; y <= TAPS; y++)
                {
                    [unroll] for (int x = -TAPS; x <= TAPS; x++)
                    {
                        float2 offset = float2(x, y) * stepPx;
                        float w = exp(-dot(offset, offset) / (2 * sigma * sigma));
                        sum += AlphaAt(p + offset) * w;
                        weightSum += w;
                    }
                }

                float glow = saturate(sum / weightSum * _Strength);
                return half4(_GlowColor.rgb, glow * _GlowColor.a * _Intensity);
            }
            ENDHLSL
        }
    }
}
