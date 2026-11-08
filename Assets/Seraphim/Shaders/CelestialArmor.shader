Shader "Seraphim/Celestial Armor"
{
    Properties
    {
        _BaseMap("Original character palette",2D)="white" {}
        _BaseColor("Tint",Color)=(1,1,1,1)
        _EmissionColor("Emission",Color)=(0,0,0,1)
        _Cutoff("Cutoff",Float)=0.5
    }
    SubShader
    {
        Tags {"RenderType"="Opaque" "RenderPipeline"="UniversalPipeline"}
        Pass
        {
            Tags {"LightMode"="UniversalForward"}
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST,_BaseColor,_EmissionColor;
            float _Cutoff;
            CBUFFER_END
            struct A {float4 positionOS:POSITION; float3 normalOS:NORMAL; float2 uv:TEXCOORD0;};
            struct V {float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0; float3 normalWS:TEXCOORD1; float2 uv:TEXCOORD2; float fog:TEXCOORD3;};
            V Vert(A a)
            {
                V o; o.positionWS=TransformObjectToWorld(a.positionOS.xyz); o.positionCS=TransformWorldToHClip(o.positionWS);
                o.normalWS=TransformObjectToWorldNormal(a.normalOS); o.uv=TRANSFORM_TEX(a.uv,_BaseMap); o.fog=ComputeFogFactor(o.positionCS.z); return o;
            }
            half4 Frag(V i):SV_Target
            {
                half3 color=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).rgb;
                // Recolor the source red cape as ivory silk without a derived bitmap.
                if (color.r>color.g*1.5 && color.r>color.b*1.5 && color.r-color.g>.15) color=half3(.9,.91,.84);
                half3 n=normalize(i.normalWS); Light light=GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                half3 diffuse=SampleSH(n)+light.color*saturate(dot(n,light.direction))*light.shadowAttenuation;
                half3 h=normalize(light.direction+GetWorldSpaceNormalizeViewDir(i.positionWS));
                half spec=pow(saturate(dot(n,h)),48)*.35*light.shadowAttenuation;
                return half4(MixFog(color*_BaseColor.rgb*diffuse+spec*half3(1,.87,.56)+_EmissionColor.rgb,i.fog),1);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }
}
