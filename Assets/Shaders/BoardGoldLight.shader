Shader "Switchboard/UI/Board Gold Light"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _GoldTint ("Gold light", Color) = (1,.87,.53,1)
        _LightRadius ("Radius", Float) = 180
        _LightIntensity ("Intensity", Range(0,1)) = .22
        _Shimmer ("Shimmer", Range(0,1)) = .1
        [HideInInspector] _Pointer ("Pointer", Vector) = (0,0,0,0)
        [HideInInspector] _BoardSize ("Board Size", Vector) = (1120,860,0,0)
        [HideInInspector] _SpriteUV ("Sprite UV", Vector) = (0,0,1,1)
        [HideInInspector] _LightTime ("Light Time", Float) = 0
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            struct appdata { float4 vertex:POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 vertex:SV_POSITION; fixed4 color:COLOR; float2 uv:TEXCOORD0; half4 mask:TEXCOORD1; UNITY_VERTEX_OUTPUT_STEREO };
            sampler2D _MainTex;
            fixed4 _Color, _TextureSampleAdd, _GoldTint;
            float4 _ClipRect, _Pointer, _BoardSize, _SpriteUV;
            float _UIMaskSoftnessX, _UIMaskSoftnessY, _LightRadius, _LightIntensity, _Shimmer, _LightTime;
            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                float2 pixelSize = o.vertex.w / abs(mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy));
                float4 rect = clamp(_ClipRect, -2e10, 2e10);
                o.mask = half4(v.vertex.xy * 2 - rect.xy - rect.zw, .25 / (.25 * half2(_UIMaskSoftnessX, _UIMaskSoftnessY) + abs(pixelSize)));
                return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                half4 sample = tex2D(_MainTex, i.uv) + _TextureSampleAdd;
                half3 pigment = sample.rgb;
                #ifndef UNITY_COLORSPACE_GAMMA
                pigment = LinearToGammaSpace(pigment);
                #endif
                half gold = smoothstep(.13, .23, pigment.g - pigment.b)
                    * (1 - smoothstep(.08, .15, pigment.r - pigment.g))
                    * smoothstep(-.03, .015, pigment.r - pigment.g);
                float2 boardUV = (i.uv - _SpriteUV.xy) / max(.0001, _SpriteUV.zw - _SpriteUV.xy);
                float2 boardPoint = boardUV * _BoardSize.xy;
                float2 delta = (boardUV - _Pointer.xy) * _BoardSize.xy / max(1, _LightRadius);
                float dist2 = dot(delta, delta);
                half proximity = exp2(-3 * dist2) * (1 - smoothstep(1, 2.25, dist2));
                half ripple = 1 - _Shimmer * (.5 + .5 * sin(_LightTime * 1.3 + boardPoint.x * .012 + boardPoint.y * .008));
                half light = saturate(gold * proximity * _LightIntensity * _Pointer.z * ripple);
                sample.rgb = lerp(sample.rgb, max(sample.rgb, _GoldTint.rgb), light);
                half4 color = sample * i.color;
                #ifdef UNITY_UI_CLIP_RECT
                half2 mask = saturate((_ClipRect.zw - _ClipRect.xy - abs(i.mask.xy)) * i.mask.zw);
                color.a *= mask.x * mask.y;
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - .001);
                #endif
                return color;
            }
            ENDCG
        }
    }
}
