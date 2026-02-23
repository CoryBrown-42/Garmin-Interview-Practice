Shader "Garmin/FairwayPulse"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _HighlightColor ("Highlight Color", Color) = (1,0.8,0,1) // Amber for highlight
        _HighlightIntensity ("Highlight Intensity", Range(0,1)) = 0.5
        _PulseSpeed ("Pulse Speed", Range(0.1, 10.0)) = 2.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0 // Targeting mobile-friendly shader model

            #include "UnityCG.cginc" // For _Time and standard structs

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _HighlightColor;
            half _HighlightIntensity;
            float _PulseSpeed; // Use float for speed as it's directly used in time multiplication

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            // Function to generate a triangular wave (0-1 range)
            half TriangularWave(float time, float speed)
            {
                // Calculate the phase based on time and speed
                float phase = frac(time * speed * 0.5); // *0.5 makes it one full cycle per 'speed' unit

                // Convert phase (0-1) to a triangular wave (0-1-0)
                // If phase is 0-0.5, it goes 0 to 1 (phase * 2)
                // If phase is 0.5-1, it goes 1 to 0 (1 - (phase - 0.5) * 2)
                // Simplified using abs: abs(phase * 2 - 1) gives 1-0-1 pattern, so 1 - abs(phase * 2 - 1) gives 0-1-0.
                // Or, more directly:
                return 1.0 - abs(phase * 2.0 - 1.0);
            }

            // Alternative (more common) triangular wave that goes 0-1-0-1...
            // half abs_triangular_wave(float time, float speed) {
            //     float val = fmod(time * speed, 2.0); // Cycle between 0 and 2
            //     return 1.0 - abs(val - 1.0);         // Map 0->1, 1->0, then again
            // }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 baseColor = tex2D(_MainTex, i.uv) * _Color;

                // --- Optimal Pulse Factor Calculation (Triangular Wave) ---
                // _Time.y is the total time since scene start.
                half pulseFactor = TriangularWave(_Time.y, _PulseSpeed);

                // --- Alternative (Smoother but slightly more expensive Sine Wave) ---
                // half pulseFactor = (sin(_Time.y * _PulseSpeed) * 0.5 + 0.5);


                // Apply the pulse factor to highlight the fairway
                fixed4 finalColor = lerp(baseColor, _HighlightColor, pulseFactor * _HighlightIntensity);

                return finalColor;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}