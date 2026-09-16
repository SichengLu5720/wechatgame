Shader "Stairs/HiddenCharacter"
{
    Properties { _PropDim("Prop focus",Range(0,1))=1 _QuestionVisibility("Question visibility",Range(0,1))=1 }
    SubShader {
        Tags { "RenderType"="Opaque" }
        Cull Off ZWrite On
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            UNITY_INSTANCING_BUFFER_START(HiddenProperties)
                UNITY_DEFINE_INSTANCED_PROP(float, _PropDim)
                UNITY_DEFINE_INSTANCED_PROP(float, _QuestionVisibility)
            UNITY_INSTANCING_BUFFER_END(HiddenProperties)
            struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; fixed4 color:COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 position:SV_POSITION; fixed3 color:COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            v2f vert(appdata v){
                UNITY_SETUP_INSTANCE_ID(v);v2f o;UNITY_TRANSFER_INSTANCE_ID(v,o);o.position=UnityObjectToClipPos(v.vertex);
                float light=.65+.35*saturate(dot(UnityObjectToWorldNormal(v.normal),normalize(float3(-.4,1,-.65))));
                float glyph=step(.5,v.color.r);
                o.color=lerp(v.color.rgb*light,lerp(float3(.014,.014,.014),float3(1,1,1),UNITY_ACCESS_INSTANCED_PROP(HiddenProperties,_QuestionVisibility)),glyph);return o;
            }
            fixed4 frag(v2f i):SV_Target {UNITY_SETUP_INSTANCE_ID(i);return fixed4(i.color*UNITY_ACCESS_INSTANCED_PROP(HiddenProperties,_PropDim),1);}
            ENDCG
        }
    }
}
