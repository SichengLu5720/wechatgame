Shader "Stairs/CloudColumn"
{
    Properties { _PropDim("Prop focus",Range(0,1))=1  _Color("Stone",Color)=(1,1,1,1) _FadeTop("Cloud top",Float)=0 _FadeBottom("Cloud depth",Float)=-10 _MistColor("Depth mist",Color)=(.247,.616,.651,1) }
    SubShader {
        Tags { "Queue"="Transparent-10" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha ZWrite Off
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Color,_MistColor;float _PropDim;float _FadeTop,_FadeBottom;
            struct appdata {float4 vertex:POSITION;float3 normal:NORMAL;};
            struct v2f {float4 pos:SV_POSITION;float light:TEXCOORD0;float height:TEXCOORD1;};
            v2f vert(appdata v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.height=mul(unity_ObjectToWorld,v.vertex).y;float3 n=UnityObjectToWorldNormal(v.normal);o.light=.73+.27*saturate(dot(normalize(n),normalize(float3(-.4,1,-.65))));return o;}
            fixed4 frag(v2f i):SV_Target {float a=smoothstep(_FadeBottom,_FadeTop,i.height);float fog=1-a;return fixed4(lerp(_Color.rgb*i.light,_MistColor.rgb,smoothstep(0,.85,fog))*_PropDim,a);}
            ENDCG
        }
    }
}
