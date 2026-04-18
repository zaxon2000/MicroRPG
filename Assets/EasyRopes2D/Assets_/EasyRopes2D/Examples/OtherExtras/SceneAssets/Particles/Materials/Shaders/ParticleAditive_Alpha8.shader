// URP 2D compatible rewrite. The original used fixed-function pipeline calls
// (BindChannels / SetTexture / combine) which URP does not support, making
// every particle material invisible. This additive, alpha-tinted replacement
// produces the same visual effect with a vertex/fragment program.

Shader "Custom/Additive Alpha8"
{
    Properties
    {
        _MainTex ("Particle Texture (Alpha8)", 2D) = "white" {}
        _TintColor ("Tint Color", Color) = (0.5, 0.5, 0.5, 0.5)
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

        Blend SrcAlpha One
        Cull Off
        Lighting Off
        ZWrite Off

        Pass
        {
            Name "ParticleAdditive"

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4    _MainTex_ST;
            fixed4    _TintColor;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex   = UnityObjectToClipPos(v.vertex);
                o.color    = v.color * _TintColor * 2.0;
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return i.color * tex2D(_MainTex, i.texcoord);
            }
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}
