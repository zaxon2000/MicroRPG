// URP 2D compatible replacement for the original "Kilt/Unlit Transparent Colored"
// shader that shipped with Easy Ropes 2D. The legacy CGPROGRAM version would not
// compile under URP and caused rope / chain / glass materials to render as
// invisible / magenta. This HLSL version works in BOTH the built-in pipeline
// and URP (2D and 3D), since it uses only core Unity macros that both pipelines
// expose.

Shader "Kilt/Unlit Transparent Colored"
{
    Properties
    {
        _MainTex ("Base (RGB), Alpha (A)", 2D) = "white" {}
        _Color   ("Tint",                    Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags
        {
            "Queue"           = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType"      = "Transparent"
            "PreviewType"     = "Plane"
            "RenderPipeline"  = "UniversalPipeline"
        }

        LOD 100

        Cull   Off
        Lighting Off
        ZWrite Off
        Blend  SrcAlpha OneMinusSrcAlpha
        Offset -1, -1

        Pass
        {
            Name "Unlit"

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma target   2.0

            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float2 texcoord : TEXCOORD0;
                fixed4 color    : COLOR;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                half2  texcoord : TEXCOORD0;
                fixed4 color    : COLOR;
            };

            sampler2D _MainTex;
            float4    _MainTex_ST;
            fixed4    _Color;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex   = UnityObjectToClipPos(v.vertex);
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color    = v.color * _Color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return tex2D(_MainTex, i.texcoord) * i.color;
            }
            ENDHLSL
        }
    }

    // Fallback so that if the active render pipeline doesn't accept this, at
    // least something renders.
    Fallback "Sprites/Default"
}
