Shader "Stairs/Pastel"
{
    Properties { _RevealTint("Reveal color",Range(0,1))=1 _StoneShade("Stone underside",Float)=0  _ContactTint("Contact shading",Float)=0  _PropDim("Prop focus",Range(0,1))=1  _Color ("Pastel color",Color)=(1,1,1,1) }
    SubShader {
        Tags { "RenderType"="Opaque" }
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            fixed4 _Color;float _PropDim,_RevealTint;
            float _ContactTint,_StoneShade;
            struct appdata {float4 vertex:POSITION;float3 normal:NORMAL;fixed4 color:COLOR;UNITY_VERTEX_INPUT_INSTANCE_ID};
            struct v2f {float4 pos:SV_POSITION;float light:TEXCOORD0;fixed3 color:COLOR;};
            v2f vert(appdata v){UNITY_SETUP_INSTANCE_ID(v);v2f o;o.pos=UnityObjectToClipPos(v.vertex);float3 n=UnityObjectToWorldNormal(v.normal);o.light=.73+.27*saturate(dot(normalize(n),normalize(float3(-.4,1,-.65))));o.light*=1-.28*_StoneShade*saturate(-n.y);o.color=lerp(_Color.rgb,float3(.94,.91,.79)*v.color.rgb,(1-v.color.a)*_ContactTint);return o;}
            fixed4 frag(v2f i):SV_Target{return fixed4(lerp(float3(.014,.014,.014),i.color*i.light,_RevealTint)*_PropDim,1);}
            ENDCG
        }
    }
}


