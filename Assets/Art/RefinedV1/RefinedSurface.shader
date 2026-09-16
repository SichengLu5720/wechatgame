Shader "Stairs/RefinedSurface"
{
    Properties {
        _ClothColor("Cloth color",Color)=(.72,.2,.2,1)
        _Grain("Shared material microdetail",2D)="gray" {}
        _StoneTex("Ivory limestone base color",2D)="white" {}
        _StoneDetail("Stone material variation",Range(0,1))=0
        _GameplayLighting("Fixed mobile lighting",Range(0,1))=0
        _RevealTint("Reveal color",Range(0,1))=1
        _PropDim("Prop focus",Range(0,1))=1
        _Detail("Detail amount",Range(0,1))=.35
        _FogStrength("Optional height haze",Range(0,1))=0
        _FogColor("Haze color",Color)=(.247,.616,.651,1)
        _FogLevel("Haze starts at world Y",Float)=-2.5
        _FogDepth("Haze depth",Float)=7
    }
    SubShader {
        Tags {"RenderType"="Opaque" "Queue"="Geometry"}
        Pass {
            Tags {"LightMode"="ForwardBase"}
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"
            sampler2D _Grain,_StoneTex;float4 _ClothColor,_FogColor;float _Detail,_FogStrength,_FogLevel,_FogDepth,_StoneDetail,_GameplayLighting;
            UNITY_INSTANCING_BUFFER_START(ActorProperties)
                UNITY_DEFINE_INSTANCED_PROP(float, _RevealTint)
                UNITY_DEFINE_INSTANCED_PROP(float, _PropDim)
            UNITY_INSTANCING_BUFFER_END(ActorProperties)
            struct appdata {float4 vertex:POSITION;float3 normal:NORMAL;float4 color:COLOR;float2 uv:TEXCOORD0;float2 surface:TEXCOORD1;UNITY_VERTEX_INPUT_INSTANCE_ID};
            struct v2f {float4 pos:SV_POSITION;float3 world:TEXCOORD0;float3 normal:TEXCOORD1;float4 color:COLOR;float2 uv:TEXCOORD2;float2 surface:TEXCOORD3;SHADOW_COORDS(4) UNITY_VERTEX_INPUT_INSTANCE_ID};
            v2f vert(appdata v){UNITY_SETUP_INSTANCE_ID(v);v2f o;UNITY_TRANSFER_INSTANCE_ID(v,o);o.pos=UnityObjectToClipPos(v.vertex);o.world=mul(unity_ObjectToWorld,v.vertex).xyz;o.normal=UnityObjectToWorldNormal(v.normal);o.color=v.color;o.uv=v.uv;o.surface=v.surface;TRANSFER_SHADOW(o);return o;}
            fixed4 frag(v2f i):SV_Target {
                UNITY_SETUP_INSTANCE_ID(i);
                float revealTint=UNITY_ACCESS_INSTANCED_PROP(ActorProperties,_RevealTint);
                float propDim=UNITY_ACCESS_INSTANCED_PROP(ActorProperties,_PropDim);
                float3 n=normalize(i.normal),l=normalize(float3(-.4,1,-.65)),eye=normalize(_WorldSpaceCameraPos-i.world);
                if(_GameplayLighting<.5&&dot(_WorldSpaceLightPos0.xyz,_WorldSpaceLightPos0.xyz)>.01)l=normalize(_WorldSpaceLightPos0.xyz);
                float3 keyColor=lerp(_LightColor0.rgb,float3(1.1,1.07,1.02),_GameplayLighting);
                float grain=tex2D(_Grain,i.uv*2.5).r;float3 base=i.color.rgb*lerp(float3(1,1,1),_ClothColor.rgb,i.surface.y);
                base*=1+(grain-.5)*_Detail*.22;
                float stone=dot(tex2D(_StoneTex,i.uv*.48).rgb,float3(.299,.587,.114))/.84;
                base*=lerp(1,stone,_StoneDetail*saturate((i.surface.x-.6)*5));
                float shade=lerp(SHADOW_ATTENUATION(i),1,_GameplayLighting);float key=saturate(dot(n,l));
                float sky=.5+.5*n.y;float3 ambient=lerp(float3(.38,.40,.41),float3(.65,.65,.61),sky);
                float3 lit=base*(ambient+keyColor*key*shade*.58)*i.color.a;
                float3 halfDir=normalize(l+eye);float shine=pow(saturate(dot(n,halfDir)),lerp(80,12,i.surface.x));
                lit+=keyColor*shine*(1-i.surface.x)*.30*shade;
                float rim=pow(1-saturate(dot(eye,n)),3)*saturate(n.y+.4)*.045;
                lit+=base*rim;lit=lerp(float3(.014,.014,.014),lit,revealTint)*propDim;
                lit=lerp(lit,_FogColor.rgb*propDim,saturate((_FogLevel-i.world.y)/max(.01,_FogDepth))*_FogStrength);
                return fixed4(lit,1);
            }
            ENDCG
        }
        Pass {
            Tags {"LightMode"="ShadowCaster"}
            ZWrite On ZTest LEqual
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_shadowcaster
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            struct v2f {V2F_SHADOW_CASTER;};
            v2f vert(appdata_base v){UNITY_SETUP_INSTANCE_ID(v);v2f o;TRANSFER_SHADOW_CASTER_NORMALOFFSET(o);return o;}
            float4 frag(v2f i):SV_Target{SHADOW_CASTER_FRAGMENT(i)}
            ENDCG
        }
    }
    Fallback Off
}
