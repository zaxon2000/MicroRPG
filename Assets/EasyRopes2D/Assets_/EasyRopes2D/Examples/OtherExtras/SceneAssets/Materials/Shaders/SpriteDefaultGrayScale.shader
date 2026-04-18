// URP 2D compatible rewrite of Easy Ropes 2D's grayscale sprite shader.
// Uses HLSLPROGRAM with only core Unity macros so it runs under both URP
// (2D and 3D) and the legacy built-in pipeline.

Shader "Kilt/SpriteDefaultGrayScale"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"            = "Transparent"
            "IgnoreProjector"  = "True"
            "RenderType"       = "Transparent"
            "PreviewType"      = "Plane"
            "CanUseSpriteAtlas"= "True"
            "RenderPipeline"   = "UniversalPipeline"
        }

        LOD 100
        Cull   Off
        Lighting Off
        ZWrite Off
        Blend  SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "SpriteGrayscale"

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile _ PIXELSNAP_ON
            #pragma target 2.0

            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                half2  texcoord : TEXCOORD0;
            };

            sampler2D _MainTex;
            fixed4    _Color;

            v2f vert (appdata_t IN)
            {
                v2f OUT;
                OUT.vertex   = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color    = IN.color * _Color;
                #ifdef PIXELSNAP_ON
                    OUT.vertex = UnityPixelSnap(OUT.vertex);
                #endif
                return OUT;
            }

            fixed4 frag (v2f IN) : SV_Target
            {
                half4 texcol = tex2D(_MainTex, IN.texcoord) * IN.color;
                // luminance weights
                texcol.rgb = dot(texcol.rgb, float3(0.3, 0.59, 0.11));
                return texcol;
            }
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}
