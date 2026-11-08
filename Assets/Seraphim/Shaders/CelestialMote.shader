Shader "Seraphim/Celestial Mote"
{
    Properties { _BaseColor("Color",Color)=(3,2.2,1,1) }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha One
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS:POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; };
            struct Varyings { float4 positionCS:SV_POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; };
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
            CBUFFER_END
            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS=TransformObjectToHClip(input.positionOS.xyz);
                output.color=input.color;
                output.uv=input.uv;
                return output;
            }
            half4 frag(Varyings input):SV_Target
            {
                float2 p=input.uv*2-1;
                half core=pow(saturate(1-dot(p,p)),2.5);
                half4 color=_BaseColor*input.color;
                color.a*=core;
                return color;
            }
            ENDHLSL
        }
    }
}
