Shader "Stairs/FixedTowerBackdrop"
{
    Properties {_Sky("Sky",Color)=(.247,.616,.651,1)}
    SubShader {Tags {"RenderType"="Opaque"} Cull Off ZWrite On
        Pass {CGPROGRAM
        #pragma vertex vert
        #pragma fragment frag
        #include "UnityCG.cginc"
        fixed4 _Sky;float _Aspect;
        struct input {float4 vertex:POSITION;fixed4 color:COLOR;float2 uv:TEXCOORD0;float2 anchor:TEXCOORD1;};
        struct output {float4 pos:SV_POSITION;fixed4 color:COLOR;float2 fog:TEXCOORD0;float4 screen:TEXCOORD1;};
        output vert(input v){output o;v.vertex.x+=v.anchor.x*_Aspect;o.pos=UnityObjectToClipPos(v.vertex);o.color=v.color;o.fog=v.uv;o.screen=ComputeScreenPos(o.pos);return o;}
        fixed4 frag(output i):SV_Target {
            float2 p=i.screen.xy/i.screen.w;
            fixed3 sky=lerp(_Sky.rgb+float3(.018,.032,.012),_Sky.rgb+float3(-.02,-.025,.018),smoothstep(0,1,p.y));
            float left=1-smoothstep(.05,1,length((p-float2(-.12,.17))*float2(1.8,3.1)));
            float right=1-smoothstep(.05,1,length((p-float2(1.08,.36))*float2(2.1,3.5)));
            float lower=1-smoothstep(0,.35,p.y);
            sky=lerp(sky,float3(.58,.76,.76),saturate(left*.18+right*.16+lower*.10));
            float a=smoothstep(.06,.82,i.fog.x)*i.fog.y;
            return fixed4(lerp(sky,i.color.rgb,a),1);
        }
        ENDCG}
    }
}
