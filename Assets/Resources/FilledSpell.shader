Shader "Emberfall/Filled Spell Volume"
{
    Properties
    {
        _Color ("Tint", Color) = (0.25,0.75,1,1)
        _Opacity ("Opacity", Range(0,1)) = 1
        _Progress ("Lifetime", Range(0,1)) = 0
        _Style ("Dissolve", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest LEqual
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; float2 uv:TEXCOORD0; };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; float3 normal:TEXCOORD1; };
            fixed4 _Color; float _Opacity, _Progress, _Style;
            v2f vert(appdata v)
            {v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.normal=UnityObjectToWorldNormal(v.normal);return o;}
            fixed4 frag(v2f i):SV_Target
            {
                float light=.6+.4*abs(dot(normalize(i.normal),normalize(float3(.35,.8,.4))));
                float edge=saturate(i.uv.y);float grain=.5+.5*sin(i.uv.x*47+i.uv.y*29+sin(i.uv.y*17)*2);
                float dissolve=saturate((_Progress-.5)*2.0)*_Style;
                float alpha=_Color.a*_Opacity*saturate((grain+.45-dissolve)*3);
                clip(alpha-.025);
                float3 tint=lerp(_Color.rgb*.55,lerp(_Color.rgb,1,edge*.72),edge)*light;
                return fixed4(tint,alpha);
            }
            ENDCG
        }
    }
}
