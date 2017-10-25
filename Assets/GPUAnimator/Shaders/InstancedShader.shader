// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

  Shader "Instanced/InstancedShader" {
    Properties {
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
    }
    SubShader {

        Pass {

            Tags {"LightMode"="ForwardBase"}

            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight
            #pragma target 4.5

            #include "UnityCG.cginc"
            #include "UnityLightingCommon.cginc"
            #include "AutoLight.cginc"

            sampler2D _MainTex;
			float4x4 transformMtx;

        #if SHADER_TARGET >= 45
            StructuredBuffer<float4> vertexBuffer;
			StructuredBuffer<float3> positionBuffer;
			StructuredBuffer<int> triangleBuffer;
        #endif

            struct v2f
            {
                float4 pos : POSITION;
                float2 uv_MainTex : TEXCOORD0;
                float3 ambient : TEXCOORD1;
                float3 diffuse : TEXCOORD2;
                float3 color : TEXCOORD3;
                SHADOW_COORDS(4)
            };

			float4 GetPosition(int trianlgeIdx, float u, float v)
			{
				int t1 = triangleBuffer[(trianlgeIdx * 3)];
				int t2 = triangleBuffer[(trianlgeIdx * 3) + 1];
				int t3 = triangleBuffer[(trianlgeIdx * 3) + 2];

				float3 p1 = vertexBuffer[t1];
				float3 p2 = vertexBuffer[t2];
				float3 p3 = vertexBuffer[t3];

				float a = 1 - u - v;
				float b = u;
				float c = v;

				float3 pointOnMesh = a * p1 + b * p2 + c * p3;

				return float4(pointOnMesh, 0);
			}

            void rotate2D(inout float2 v, float r)
            {
                float s, c;
                sincos(r, s, c);
                v = float2(v.x * c - v.y * s, v.x * s + v.y * c);
            }

            v2f vert (appdata_full v, uint instanceID : SV_InstanceID)
            {
				float3 d = positionBuffer[instanceID];
            #if SHADER_TARGET >= 45

				float4 data = GetPosition((int)d.x, d.y, d.z); //vertexBuffer[instanceID];
            #else
                float4 data = 0;
            #endif

                float rotation = data.w * data.w * _Time.x * 0.5f;
                rotate2D(data.xz, rotation);

                float3 localPosition = v.vertex.xyz * 0.02;
                float3 worldPosition = data.xyz + localPosition;

                float3 worldNormal = v.normal;



                float3 ndotl = saturate(dot(worldNormal, _WorldSpaceLightPos0.xyz));
                float3 ambient = ShadeSH9(float4(worldNormal, 1.0f));
                float3 diffuse = (ndotl * _LightColor0.rgb);
                float3 color = v.color;

                v2f o;

				o.pos = mul(transformMtx, float4(worldPosition, 1.0f));
				o.pos = UnityObjectToClipPos(o.pos);
				

                o.uv_MainTex = v.texcoord;
                o.ambient = ambient;
                o.diffuse = diffuse;
                o.color = color;
                TRANSFER_SHADOW(o)
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed shadow = SHADOW_ATTENUATION(i);
                fixed4 albedo = tex2D(_MainTex, i.uv_MainTex);
                float3 lighting = i.diffuse * shadow + i.ambient;
                fixed4 output = fixed4(albedo.rgb * i.color * lighting, albedo.w);
                UNITY_APPLY_FOG(i.fogCoord, output);
                return output;
            }

            ENDCG
        }
    }
}