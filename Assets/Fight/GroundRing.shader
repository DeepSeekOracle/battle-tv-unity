Shader "BattleTv/GroundRing"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
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
        // Straight-alpha additive. RGB stays unmultiplied; the blend adds RGB * A.
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
                // The quad is already a flat ground ellipse. A circle in this UV
                // is that ellipse. Spin is in angle, so the light travels around
                // her feet instead of tumbling the quad.
                float2 uv = i.texcoord;
                float seam = uv.y - 0.5;
                float seamA = 1.0;
                if (_Half > 0.5)
                    seamA = smoothstep(-0.07, 0.035, seam);
                else if (_Half < -0.5)
                    seamA = smoothstep(0.07, -0.035, seam);

                float2 p = uv * 2.0 - 1.0;
                float r = length(p);
                float aa = max(fwidth(r) * 1.4, 0.0025);
                float ang = atan2(p.y, p.x);
                float t = _Time.y;

                float breathe = 0.008 * sin(t * 1.7);
                float outerR = 0.74 + breathe;
                float innerR = 0.50 - breathe * 0.5;

                float outerD = r - outerR;
                float innerD = r - innerR;
                float outerGlow = exp(-(outerD * outerD) / 0.010);
                float innerGlow = exp(-(innerD * innerD) / 0.0045);
                float outerCore = 1.0 - smoothstep(0.0, aa * 2.8, abs(outerD) - 0.006);
                float innerCore = 1.0 - smoothstep(0.0, aa * 2.4, abs(innerD) - 0.004);
                float halo = exp(-(outerD * outerD) / 0.028) * 0.42;

                float head = atan2(sin(ang - t * 1.45), cos(ang - t * 1.45));
                float comet = exp(-head * head * 3.4);
                float tail = atan2(sin(ang + t * 0.95 + 2.2), cos(ang + t * 0.95 + 2.2));
                float cometIn = exp(-tail * tail * 4.2);

                float band = smoothstep(innerR + 0.035, innerR + 0.07, r) * (1.0 - smoothstep(outerR - 0.07, outerR - 0.03, r));
                float runeU = frac((ang + t * 0.9) * 1.2732395);
                float rune = smoothstep(0.06, 0.18, runeU) * (1.0 - smoothstep(0.46, 0.72, runeU));
                rune *= band;

                float sp = ang - t * 1.55 + log(max(r, 0.045)) * 2.6;
                float spiral = exp(-(sin(sp) * sin(sp)) * 26.0);
                float spiralMask = smoothstep(0.05, 0.14, r) * (1.0 - smoothstep(0.46, 0.60, r));

                float pool = exp(-(r * r) * 2.8) * smoothstep(0.10, 0.34, r) * 0.35;

                float sparkSeed = sin(ang * 14.0 - t * 2.4) * sin(r * 48.0 + t * 1.3);
                float spark = smoothstep(0.72, 0.97, sparkSeed * sparkSeed) * outerGlow;

                float pulse = 0.84 + 0.16 * sin(t * 2.4);
                float3 cyan = float3(0.42, 0.90, 1.0);
                float3 gold = float3(1.0, 0.80, 0.38);
                float3 violet = float3(0.58, 0.30, 1.0);
                float3 hot = float3(0.90, 0.98, 1.0);

                float3 col = cyan * (outerGlow * 0.75 + outerCore * 1.15 + halo);
                col += hot * comet * outerGlow * 1.35;
                col += gold * (innerGlow * 0.80 + innerCore * 1.05);
                col += hot * cometIn * innerGlow * 0.85;
                col += gold * rune * 0.95;
                col += violet * spiral * spiralMask * 1.05;
                col += cyan * pool * 0.10;
                col += hot * spark * 0.35;

                float alpha = outerGlow * 0.72 + outerCore + halo * 0.75;
                alpha += innerGlow * 0.62 + innerCore * 0.9;
                alpha += comet * outerGlow * 0.85 + cometIn * innerGlow * 0.45;
                alpha += rune * 0.8 + spiral * spiralMask * 0.75 + pool * 0.08 + spark * 0.25;
                alpha = saturate(alpha);
                // Die before the quad edge so the light is an ellipse, not a box.
                alpha *= seamA * pulse * (1.0 - smoothstep(0.86, 0.98, r));
                alpha *= tex2D(_MainTex, uv).a * i.color.a;

                #ifdef UNITY_UI_CLIP_RECT
                alpha *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif

                if (alpha < 0.004)
                    discard;

                return fixed4(col * i.color.rgb, alpha);
            }
            ENDCG
        }
    }
}
