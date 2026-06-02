Shader "Custom/HeatDistortion"
{
    Properties
    {
        _DistortionStrength ("Distortion Strength", Range(0, 0.1)) = 0.02
        _Speed ("Speed", Range(0, 10)) = 2.0
        _Scale ("Scale", Range(0, 50)) = 10.0
    }
    SubShader
    {
        Tags { "Queue"="Transparent+1" "RenderType"="Transparent" "IgnoreProjector"="True" }
        LOD 100
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        GrabPass { "_BackgroundTexture" }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 grabPos : TEXCOORD0;
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD1;
            };

            sampler2D _BackgroundTexture;
            float _DistortionStrength;
            float _Speed;
            float _Scale;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.grabPos = ComputeGrabScreenPos(o.pos);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Create a scrolling noise/sine wave effect mathematically
                float time = _Time.y * _Speed;
                
                // Mathematical pseudo-randomness for the heat waves
                float distortionX = sin(i.uv.y * _Scale + time) * cos(i.uv.x * _Scale * 0.5 + time * 0.8);
                float distortionY = cos(i.uv.x * _Scale + time) * sin(i.uv.y * _Scale * 0.5 + time * 1.2);
                
                // Fade distortion heavily at the edges of the quad so it blends smoothly with the ground
                float edgeFade = smoothstep(0.0, 0.2, i.uv.x) * smoothstep(1.0, 0.8, i.uv.x) * 
                                 smoothstep(0.0, 0.2, i.uv.y) * smoothstep(1.0, 0.8, i.uv.y);

                // BUG FIX 2: In a 2D Orthographic camera, UV offsets are absolute screen percentages.
                // If you set the slider to 0.1, it shifts the grab coordinate by 10% of the entire screen!
                // 10% of your screen is almost 2 full tiles wide, which is why it reached the Green tile!
                // We divide by 10 here to make the slider safe to use.
                float2 offset = float2(distortionX, distortionY) * (_DistortionStrength * 0.1) * edgeFade;
                
                i.grabPos.xy += offset * i.grabPos.w;
                
                fixed4 col = tex2Dproj(_BackgroundTexture, i.grabPos);
                return col;
            }
            ENDCG
        }
    }
}
