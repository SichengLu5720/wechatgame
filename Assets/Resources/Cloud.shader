Shader "Stairs/Cloud" {
 Properties { _NoiseTex("Shared noise lattice",2D)="gray" {} _PropDim("Prop focus",Range(0,1))=1 _Color("Mist",Color)=(.61,.80,.79,.38) _Ceiling("Mist ceiling",Float)=0 }
 SubShader { Tags {"Queue"="Transparent" "RenderType"="Transparent"} Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
 Pass { CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 fixed4 _Color;float _PropDim,_Ceiling;sampler2D _NoiseTex;
 struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;};
 struct v2f {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;float3 world:TEXCOORD1;};
 v2f vert(appdata v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv*2-1;o.world=mul(unity_ObjectToWorld,v.vertex).xyz;return o;}
 float noise(float2 p){float2 f=frac(p);f=f*f*(3-2*f);return tex2D(_NoiseTex,(floor(p)+f+64.5)/128).r;}
 fixed4 frag(v2f i):SV_Target {
   float n=noise(i.world.xz*.15)+.35*noise(i.world.xz*.38);
   float bend=(n-.65)*.38;
   float2 p=i.uv+float2(0,bend);
   float middle=1-smoothstep(.12,1,length(p*float2(1.5,1.65)));
   float left=1-smoothstep(.1,1,length((p-float2(-.40,-.1))*float2(2.3,2.5)));
   float right=1-smoothstep(.1,1,length((p-float2(.38,.12))*float2(2.2,2.25)));
   float horizontal=max(middle,max(left,right));
   float vertical=1-smoothstep(.65,1,abs(i.uv.x));
   float ceiling=1-smoothstep(_Ceiling-2,_Ceiling,i.world.y);
   float alpha=horizontal*vertical*ceiling*(.7+.3*n)*_Color.a;
   return fixed4(_Color.rgb*_PropDim,alpha);
 }
 ENDCG }
 }
}

