Shader "Caravan/CameraStudy/Solid"
{
    Properties
    {
        _BaseColor ("Цвет поверхности", Color) = (0.3, 0.2, 0.1, 1)
        _LightDirection ("Направление света", Vector) = (-0.4, 0.8, -0.5, 0)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            // Поддерживается и 2D Renderer проекта: свет нужен только для объёма макета.
            Tags { "LightMode"="SRPDefaultUnlit" }
            ZWrite On
            Cull Back
            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float4 _LightDirection;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; };
            Varyings Vertex(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }
            half4 Fragment(Varyings input) : SV_Target
            {
                half lighting = .45h + .55h * saturate(dot(normalize(input.normalWS), normalize(_LightDirection.xyz)));
                return half4(_BaseColor.rgb * lighting, 1);
            }
            ENDHLSL
        }
    }
}
