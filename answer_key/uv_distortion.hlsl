Shader "Garmin/HomeTeeHero/WaterHazardDistortion"
{
    Properties
    {
        _MainTex ("Water Base Texture (RGB)", 2D) = "white" {}
        _Color ("Tint Color (RGBA)", Color) = (0.3,0.5,0.7,0.8) // Default water blue, slightly transparent
        _NoiseTex ("Distortion Noise (R)", 2D) = "gray" {} // Grayscale noise texture
        _NoiseScale ("Noise Tiling Scale", Float) = 1.0
        _NoiseSpeed ("Noise Scroll Speed (X, Y)", Vector) = (0.1, 0.05, 0, 0)
        _DistortionStrength ("Distortion Strength", Range(0.0, 0.1)) = 0.02
        _FresnelColor ("Fresnel Reflection Color", Color) = (0.8, 0.9, 1.0, 1.0)
        _FresnelPower ("Fresnel Power (Sharpness)", Range(0.5, 8.0)) = 2.0
    }
    SubShader
    {
        // For transparent water:
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100

        Pass
        {
            // Blend mode for standard alpha blending
            Blend SrcAlpha OneMinusSrcAlpha
            // Disable ZWrite for proper transparency sorting (often required for transparent objects)
            ZWrite Off
            // Cull Back is standard for single-sided geometry
            Cull Back

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // Optimize for mobile platforms (GLES3, Metal)
            #pragma target 3.0 // Minimum target for robust half precision support
            #pragma multi_compile_fog // Support Unity's built-in fog

            #include "UnityCG.cginc" // Provides _Time, UnityObjectToClipPos, etc.

            // --- Vertex Input Structure ---
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL; // Needed for Fresnel effect
            };

            // --- Vertex to Fragment Output Structure ---
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                half3 worldNormal : TEXCOORD1; // half for mobile
                half3 worldPos : TEXCOORD2;    // half for mobile
                UNITY_FOG_COORDS(3) // For Unity's built-in fog
            };

            // --- Shader Properties ---
            sampler2D _MainTex;
            float4 _MainTex_ST; // Tiling and offset from material inspector
            sampler2D _NoiseTex;
            float4 _NoiseTex_ST; // Not directly used in frag, but useful to keep in mind
            float4 _Color;
            half _NoiseScale;
            half2 _NoiseSpeed;
            half _DistortionStrength;
            half4 _FresnelColor;
            half _FresnelPower;

            // --- Vertex Shader ---
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                // TRANSFORM_TEX macro applies _MainTex_ST (tiling/offset) to UVs
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                // Convert normal and position to world space for lighting/fresnel calculation
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                UNITY_TRANSFER_FOG(o,o.vertex); // Pass fog coords
                return o;
            }

            // --- Fragment Shader ---
            fixed4 frag (v2f i) : SV_Target
            {
                // 1. Calculate Scrolling Noise UVs
                // These UVs combine base UVs, user-defined scale, and time-based scrolling.
                // Using half precision for these calculations for mobile efficiency.
                half2 noiseUV_base = i.uv * _NoiseScale + _NoiseSpeed * _Time.y;

                // 2. Sample Noise Texture for Distortion
                // We sample the noise texture twice with slightly different offsets and scales
                // to create a more turbulent and less repetitive distortion effect.
                // The .r swizzle assumes a grayscale noise texture, saving bandwidth.
                half noiseValue1 = tex2D(_NoiseTex, noiseUV_base).r;
                // Second sample with altered scale, speed, and offset for varied turbulence
                half noiseValue2 = tex2D(_NoiseTex, noiseUV_base * 0.8 + half2(0.1, 0.3) + _NoiseSpeed.yx * _Time.y * 0.7).r;

                // Combine noise values and normalize to a [-0.5, 0.5] range.
                // This centers the distortion, so some parts move left/down, others right/up.
                half distortionOffset = (noiseValue1 + noiseValue2) * 0.5 - 0.5;

                // 3. Apply Distortion to Main Texture UVs
                // This is the "dependent texture read" portion. We minimize its impact.
                half2 distortedUV = i.uv + distortionOffset * _DistortionStrength;

                // 4. Sample Main Water Texture
                // Use the calculated distorted UVs.
                fixed4 col = tex2D(_MainTex, distortedUV) * _Color;

                // 5. Calculate Simple Fresnel Effect for Reflections
                // Fresnel makes surfaces more reflective at grazing angles (looking across the surface).
                half3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);
                // 1.0 - saturate(dot(viewDir, normal)) gives a value close to 0 when looking straight at the surface,
                // and close to 1 when looking at a grazing angle.
                half fresnel = 1.0 - saturate(dot(viewDir, i.worldNormal));
                // Power function makes the effect sharper or softer.
                fresnel = pow(fresnel, _FresnelPower);

                // Add the Fresnel reflection color, weighted by its alpha.
                // Using additive blending for the Fresnel effect is common for water.
                col.rgb += _FresnelColor.rgb * fresnel * _FresnelColor.a;

                // 6. Apply Unity's Built-in Fog
                UNITY_APPLY_FOG(i.fogCoord, col);

                return col;
            }
            ENDCG
        }
    }
    FallBack "Hidden/InternalErrorShader" // Fallback to a simple error shader if ours fails
}