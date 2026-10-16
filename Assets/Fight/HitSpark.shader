Shader "BattleTv/HitSpark"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha One
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            float4 _ClipRect;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 p = i.texcoord * 2.0 - 1.0;
                float r = length(p);
                float ang = atan2(p.y, p.x);
                float aa = max(fwidth(r), 0.004);
                float core = exp(-(r * r) * 22.0);
                float ringD = abs(r - 0.42);
                float ring = exp(-(ringD * ringD) / 0.004);
                float hot = 1.0 - smoothstep(0.0, aa * 2.0, r - 0.07);
                float ray = sin(ang * 5.0 + _Time.y * 14.0);
                ray = ray * ray;
                ray = ray * ray * ray;
                ray *= exp(-(r * r) * 2.4);
                float alpha = saturate(core * 1.15 + hot + ring * 0.55 + ray * 0.85);
                alpha *= 1.0 - smoothstep(0.78, 0.98, r);
                alpha *= tex2D(_MainTex, i.texcoord).a * i.color.a;
                #ifdef UNITY_UI_CLIP_RECT
                alpha *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif
                if (alpha < 0.004)
                    discard;
                float3 col = lerp(i.color.rgb, float3(1.0, 0.97, 0.9), hot * 0.85);
                return fixed4(col, alpha);
            }
            ENDCG
        }
    }
}
