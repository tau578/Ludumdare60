Shader "Switchboard/UI/TutorialSpotlight"
{
    // The tutorial's dark layer (D57): one tinted quad with up to 8 soft rounded holes. The quad's UVs carry its local
    // position, so the holes are given in the graphic's own units and stay sharp at every resolution.
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Softness ("Edge softness", Float) = 28
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        // Colour blends by alpha; the frame's own alpha stays opaque, or a browser would show the page through the game.
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #define HOLES 8
            struct appdata { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; fixed4 color : COLOR; float2 local : TEXCOORD0; };
            fixed4 _Color;
            float _Softness;
            float4 _Holes[HOLES];      // centre xy, half size zw
            float4 _HoleShapes[HOLES]; // x corner radius, y strength (0 = no hole)

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.local = v.uv;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float keep = 1;
                [unroll]
                for (int h = 0; h < HOLES; h++)
                {
                    float radius = _HoleShapes[h].x;
                    float2 q = abs(i.local - _Holes[h].xy) - _Holes[h].zw + radius;
                    float d = length(max(q, 0)) + min(max(q.x, q.y), 0) - radius;
                    float open = 1 - smoothstep(-.35 * _Softness, .65 * _Softness, d);
                    keep *= 1 - open * _HoleShapes[h].y;
                }
                fixed4 result = i.color;
                result.a *= keep;
                return result;
            }
            ENDCG
        }
    }
}
