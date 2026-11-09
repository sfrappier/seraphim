Shader "Seraphim/Imported Particle"
{
    Properties
    {
        _BaseMap("Texture",2D)="white"{}
        [HDR]_TintColor("Tint",Color)=(1,1,1,1)
        [Enum(UnityEngine.Rendering.BlendMode)]_DstBlend("Destination blend",Float)=1
    }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent"}
        Pass
        {
            Blend SrcAlpha [_DstBlend]
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes {float4 positionOS:POSITION;half4 color:COLOR;float2 uv:TEXCOORD0;};
            struct Varyings {float4 positionCS:SV_POSITION;half4 color:COLOR;float2 uv:TEXCOORD0;};
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _TintColor;
            CBUFFER_END
            Varyings vert(Attributes input)
            {
                Varyings output;output.positionCS=TransformObjectToHClip(input.positionOS.xyz);
                output.color=input.color*_TintColor;output.uv=TRANSFORM_TEX(input.uv,_BaseMap);return output;
            }
            half4 frag(Varyings input):SV_Target
            {
                half4 texel=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,input.uv);
                return texel*input.color;
            }
            ENDHLSL
        }
    }
}
