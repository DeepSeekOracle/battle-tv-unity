Shader "BattleTv/ArenaSigil"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Kind ("Kind", Float) = 1
        _Half ("Half", Float) = 0
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
            float _Kind;
            float _Half;
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
                float2 uv = i.texcoord;
                float2 p = uv * 2.0 - 1.0;
                float r = length(p);
                float ang = atan2(p.y, p.x);
                float t = _Time.y;
                float3 tint = i.color.rgb;
                float3 hot = lerp(tint, float3(1.0, 0.96, 0.88), 0.78);
                float alpha = 0.0;
                float3 col = tint;

                float seamA = 1.0;
                if (_Half > 0.5)
                    seamA = smoothstep(-0.07, 0.035, uv.y - 0.5);
                else if (_Half < -0.5)
                    seamA = smoothstep(0.07, -0.035, uv.y - 0.5);

                if (_Kind < 0.5)
                {
                    // Comet along the rect. The view aims the rect. Do not draw a circle here.
                    float head = uv.x;
                    float across = abs(uv.y - 0.5) * 2.0;
                    float body = exp(-(across * across) * 7.0) * smoothstep(0.0, 0.08, head);
                    float core = exp(-(across * across) * 28.0) * head;
                    col = tint * body + hot * core;
                    alpha = saturate(body * 0.85 + core);
                }
                else if (_Kind < 1.5)
                {
                    float breathe = 0.008 * sin(t * 1.7);
                    float outerR = 0.74 + breathe;
                    float innerR = 0.50 - breathe * 0.5;
                    float outerD = r - outerR;
                    float innerD = r - innerR;
                    float outerGlow = exp(-(outerD * outerD) / 0.010);
                    float innerGlow = exp(-(innerD * innerD) / 0.0045);
                    float aa = max(fwidth(r) * 1.4, 0.0025);
                    float outerCore = 1.0 - smoothstep(0.0, aa * 2.8, abs(outerD) - 0.006);
                    float innerCore = 1.0 - smoothstep(0.0, aa * 2.4, abs(innerD) - 0.004);
                    float halo = exp(-(outerD * outerD) / 0.028) * 0.42;
                    float head = atan2(sin(ang - t * 1.45), cos(ang - t * 1.45));
                    float comet = exp(-head * head * 3.4);
                    float band = smoothstep(innerR + 0.035, innerR + 0.07, r) * (1.0 - smoothstep(outerR - 0.07, outerR - 0.03, r));
                    float runeU = frac((ang + t * 0.9) * 1.2732395);
                    float rune = smoothstep(0.06, 0.18, runeU) * (1.0 - smoothstep(0.46, 0.72, runeU));
                    rune *= band;
                    col = tint * (outerGlow * 0.85 + outerCore + halo);
                    col += hot * comet * outerGlow;
                    col += hot * (innerGlow * 0.65 + innerCore);
                    col += hot * rune;
                    alpha = outerGlow * 0.72 + outerCore + halo * 0.7;
                    alpha += innerGlow * 0.55 + innerCore * 0.85 + comet * outerGlow * 0.7 + rune * 0.65;
                    alpha *= seamA * (1.0 - smoothstep(0.86, 0.98, r));
                }
                else if (_Kind < 2.5)
                {
                    float pool = exp(-(r * r) * 2.2) * smoothstep(0.02, 0.2, r);
                    float rise = smoothstep(0.15, 0.85, uv.y) * exp(-(p.x * p.x) * 3.0);
                    col = tint * pool + hot * rise;
                    alpha = saturate(pool * 0.55 + rise * 0.45) * (1.0 - smoothstep(0.8, 0.98, r));
                }
                else if (_Kind < 3.5)
                {
                    float breathe = 0.02 * sin(t * 3.2);
                    float ringR = 0.62 + breathe;
                    float d = r - ringR;
                    float glow = exp(-(d * d) / 0.02);
                    float aa = max(fwidth(r) * 1.4, 0.0025);
                    float core = 1.0 - smoothstep(0.0, aa * 3.0, abs(d) - 0.01);
                    float head = atan2(sin(ang - t * 2.4), cos(ang - t * 2.4));
                    float comet = exp(-head * head * 2.6);
                    col = tint * (glow + core) + hot * comet * glow;
                    alpha = saturate(glow * 0.8 + core + comet * glow * 0.6);
                    alpha *= (1.0 - smoothstep(0.84, 0.98, r));
                }
                else if (_Kind < 4.5)
                {
                    float across = abs(uv.y - 0.5) * 2.0;
                    float body = exp(-(across * across) * 18.0);
                    float core = exp(-(across * across) * 60.0);
                    col = tint * body + hot * core;
                    alpha = saturate(body * 0.75 + core) * smoothstep(0.0, 0.04, uv.x) * (1.0 - smoothstep(0.96, 1.0, uv.x));
                }
                else
                {
                    float core = exp(-(r * r) * 10.0);
                    float spark = exp(-(r * r) * 3.2);
                    col = hot * core + tint * spark;
                    alpha = saturate(core + spark * 0.45);
                }

                alpha = saturate(alpha) * i.color.a * tex2D(_MainTex, uv).a;
                #ifdef UNITY_UI_CLIP_RECT
                alpha *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif
                if (alpha < 0.004)
                    discard;
                return fixed4(col, alpha);
            }
            ENDCG
        }
    }
}
