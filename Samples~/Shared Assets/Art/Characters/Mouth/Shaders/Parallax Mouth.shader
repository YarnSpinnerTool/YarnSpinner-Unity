Shader "Yarn Spinner/Parallax Mouth"
    {
        Properties
        {
            [NoScaleOffset]_Texture("Texture", 2D) = "white" {}
            _Teeth_Depth("Teeth Depth", Float) = 0
            _Tongue_Depth("Tongue Depth", Float) = 0
            _Mouth_Color("Mouth Color", Color) = (0, 0, 0, 0)
            _Tongue_Color("Tongue Color", Color) = (1, 0, 0, 1)
            _Teeth_Color("Teeth Color", Color) = (1, 1, 1, 1)
            _Teeth_Edge_Color("Teeth Edge Color", Color) = (0.894, 0.894, 0.894, 1)
            _Teeth_Thickness("Teeth Thickness", Float) = 0.1
            _Mouth_Depth("Mouth Depth", Float) = 0
            [ToggleUI]_Flip_X("Flip X", Float) = 0
            [HideInInspector]_QueueOffset("_QueueOffset", Float) = 0
            [HideInInspector]_QueueControl("_QueueControl", Float) = -1
            [HideInInspector][NoScaleOffset]unity_Lightmaps("unity_Lightmaps", 2DArray) = "" {}
            [HideInInspector][NoScaleOffset]unity_LightmapsInd("unity_LightmapsInd", 2DArray) = "" {}
            [HideInInspector][NoScaleOffset]unity_ShadowMasks("unity_ShadowMasks", 2DArray) = "" {}
        }
        SubShader
        {
            Tags
            {
                "RenderPipeline"="UniversalPipeline"
                "RenderType"="Transparent"
                "UniversalMaterialType" = "Lit"
                "Queue"="Transparent"
                "DisableBatching"="False"
                "ShaderGraphShader"="true"
                "ShaderGraphTargetId"="UniversalLitSubTarget"
            }
            Pass
            {
                Name "Universal Forward"
                Tags
                {
                    "LightMode" = "UniversalForward"
                }
            
            // Render State
            Cull Back
                Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
                ZTest LEqual
                ZWrite Off
            
            // Debug
            // <None>
            
            // --------------------------------------------------
            // Pass
            
            HLSLPROGRAM
            
            // Pragmas
            #pragma target 2.0
                #pragma multi_compile_instancing
                #pragma multi_compile_fog
                #pragma instancing_options renderinglayer
                #pragma vertex vert
                #pragma fragment frag
            
            // Keywords
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
                #pragma multi_compile _ LIGHTMAP_ON
                #pragma multi_compile _ DYNAMICLIGHTMAP_ON
                #pragma multi_compile _ DIRLIGHTMAP_COMBINED
                #pragma multi_compile _ USE_LEGACY_LIGHTMAPS
                #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
                #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
                #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
                #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
                #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
                #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
                #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
                #pragma multi_compile _ SHADOWS_SHADOWMASK
                #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
                #pragma multi_compile_fragment _ _LIGHT_LAYERS
                #pragma multi_compile_fragment _ DEBUG_DISPLAY
                #pragma multi_compile_fragment _ _LIGHT_COOKIES
                #pragma multi_compile _ _FORWARD_PLUS
                #pragma multi_compile _ EVALUATE_SH_MIXED EVALUATE_SH_VERTEX
            // GraphKeywords: <None>
            
            // Defines
            
            #define _NORMALMAP 1
            #define _NORMAL_DROPOFF_TS 1
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define ATTRIBUTES_NEED_TEXCOORD0
            #define ATTRIBUTES_NEED_TEXCOORD1
            #define ATTRIBUTES_NEED_TEXCOORD2
            #define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
            #define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
            #define VARYINGS_NEED_POSITION_WS
            #define VARYINGS_NEED_NORMAL_WS
            #define VARYINGS_NEED_TANGENT_WS
            #define VARYINGS_NEED_TEXCOORD0
            #define VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
            #define VARYINGS_NEED_SHADOW_COORD
            #define FEATURES_GRAPH_VERTEX
            /* WARNING: $splice Could not find named fragment 'PassInstancing' */
            #define SHADERPASS SHADERPASS_FORWARD
                #define _FOG_FRAGMENT 1
                #define _SURFACE_TYPE_TRANSPARENT 1
            
            
            // custom interpolator pre-include
            /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
            
            // Includes
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ProbeVolumeVariants.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
            
            // --------------------------------------------------
            // Structs and Packing
            
            // custom interpolators pre packing
            /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
            
            struct Attributes
                {
                     float3 positionOS : POSITION;
                     float3 normalOS : NORMAL;
                     float4 tangentOS : TANGENT;
                     float4 uv0 : TEXCOORD0;
                     float4 uv1 : TEXCOORD1;
                     float4 uv2 : TEXCOORD2;
                    #if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
                     uint instanceID : INSTANCEID_SEMANTIC;
                    #endif
                };
                struct Varyings
                {
                     float4 positionCS : SV_POSITION;
                     float3 positionWS;
                     float3 normalWS;
                     float4 tangentWS;
                     float4 texCoord0;
                    #if defined(LIGHTMAP_ON)
                     float2 staticLightmapUV;
                    #endif
                    #if defined(DYNAMICLIGHTMAP_ON)
                     float2 dynamicLightmapUV;
                    #endif
                    #if !defined(LIGHTMAP_ON)
                     float3 sh;
                    #endif
                    #if defined(USE_APV_PROBE_OCCLUSION)
                     float4 probeOcclusion;
                    #endif
                     float4 fogFactorAndVertexLight;
                    #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                     float4 shadowCoord;
                    #endif
                    #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
                     uint instanceID : CUSTOM_INSTANCE_ID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                     uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                     uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                     FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
                    #endif
                };
                struct SurfaceDescriptionInputs
                {
                     float3 TangentSpaceNormal;
                     float3 ObjectSpaceViewDirection;
                     float3 WorldSpaceViewDirection;
                     float4 uv0;
                };
                struct VertexDescriptionInputs
                {
                     float3 ObjectSpaceNormal;
                     float3 ObjectSpaceTangent;
                     float3 ObjectSpacePosition;
                };
                struct PackedVaryings
                {
                     float4 positionCS : SV_POSITION;
                    #if defined(LIGHTMAP_ON)
                     float2 staticLightmapUV : INTERP0;
                    #endif
                    #if defined(DYNAMICLIGHTMAP_ON)
                     float2 dynamicLightmapUV : INTERP1;
                    #endif
                    #if !defined(LIGHTMAP_ON)
                     float3 sh : INTERP2;
                    #endif
                    #if defined(USE_APV_PROBE_OCCLUSION)
                     float4 probeOcclusion : INTERP3;
                    #endif
                    #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                     float4 shadowCoord : INTERP4;
                    #endif
                     float4 tangentWS : INTERP5;
                     float4 texCoord0 : INTERP6;
                     float4 fogFactorAndVertexLight : INTERP7;
                     float3 positionWS : INTERP8;
                     float3 normalWS : INTERP9;
                    #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
                     uint instanceID : CUSTOM_INSTANCE_ID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                     uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                     uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                     FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
                    #endif
                };
            
            PackedVaryings PackVaryings (Varyings input)
                {
                    PackedVaryings output;
                    ZERO_INITIALIZE(PackedVaryings, output);
                    output.positionCS = input.positionCS;
                    #if defined(LIGHTMAP_ON)
                    output.staticLightmapUV = input.staticLightmapUV;
                    #endif
                    #if defined(DYNAMICLIGHTMAP_ON)
                    output.dynamicLightmapUV = input.dynamicLightmapUV;
                    #endif
                    #if !defined(LIGHTMAP_ON)
                    output.sh = input.sh;
                    #endif
                    #if defined(USE_APV_PROBE_OCCLUSION)
                    output.probeOcclusion = input.probeOcclusion;
                    #endif
                    #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                    output.shadowCoord = input.shadowCoord;
                    #endif
                    output.tangentWS.xyzw = input.tangentWS;
                    output.texCoord0.xyzw = input.texCoord0;
                    output.fogFactorAndVertexLight.xyzw = input.fogFactorAndVertexLight;
                    output.positionWS.xyz = input.positionWS;
                    output.normalWS.xyz = input.normalWS;
                    #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
                    output.instanceID = input.instanceID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                    output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                    output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                    output.cullFace = input.cullFace;
                    #endif
                    return output;
                }
                
                Varyings UnpackVaryings (PackedVaryings input)
                {
                    Varyings output;
                    output.positionCS = input.positionCS;
                    #if defined(LIGHTMAP_ON)
                    output.staticLightmapUV = input.staticLightmapUV;
                    #endif
                    #if defined(DYNAMICLIGHTMAP_ON)
                    output.dynamicLightmapUV = input.dynamicLightmapUV;
                    #endif
                    #if !defined(LIGHTMAP_ON)
                    output.sh = input.sh;
                    #endif
                    #if defined(USE_APV_PROBE_OCCLUSION)
                    output.probeOcclusion = input.probeOcclusion;
                    #endif
                    #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                    output.shadowCoord = input.shadowCoord;
                    #endif
                    output.tangentWS = input.tangentWS.xyzw;
                    output.texCoord0 = input.texCoord0.xyzw;
                    output.fogFactorAndVertexLight = input.fogFactorAndVertexLight.xyzw;
                    output.positionWS = input.positionWS.xyz;
                    output.normalWS = input.normalWS.xyz;
                    #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
                    output.instanceID = input.instanceID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                    output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                    output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                    output.cullFace = input.cullFace;
                    #endif
                    return output;
                }
                
            
            // --------------------------------------------------
            // Graph
            
            // Graph Properties
            CBUFFER_START(UnityPerMaterial)
                float _Teeth_Depth;
                float4 _Texture_TexelSize;
                float _Tongue_Depth;
                float4 _Mouth_Color;
                float4 _Tongue_Color;
                float4 _Teeth_Color;
                float4 _Teeth_Edge_Color;
                float _Teeth_Thickness;
                float _Mouth_Depth;
                float _Flip_X;
                UNITY_TEXTURE_STREAMING_DEBUG_VARS;
                CBUFFER_END
                
                
                // Object and Global properties
                SAMPLER(SamplerState_Linear_Repeat);
                TEXTURE2D(_Texture);
                SAMPLER(sampler_Texture);
            
            // Graph Includes
            // GraphIncludes: <None>
            
            // -- Property used by ScenePickingPass
            #ifdef SCENEPICKINGPASS
            float4 _SelectionID;
            #endif
            
            // -- Properties used by SceneSelectionPass
            #ifdef SCENESELECTIONPASS
            int _ObjectId;
            int _PassValue;
            #endif
            
            // Graph Functions
            
                void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
                {
                    Out = A * B;
                }
                
                void Unity_Branch_float(float Predicate, float True, float False, out float Out)
                {
                    Out = Predicate ? True : False;
                }
                
                void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
                {
                    RGBA = float4(R, G, B, A);
                    RGB = float3(R, G, B);
                    RG = float2(R, G);
                }
                
                void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
                {
                Out = A * B;
                }
                
                void Unity_Negate_float2(float2 In, out float2 Out)
                {
                    Out = -1 * In;
                }
                
                // unity-custom-func-begin
                void ParallaxUV_float(float2 UV, float3 ViewDirection, float2 Depth, out float2 OutUV){
                OutUV = saturate(
                  UV  + (ViewDirection.xz
                  * Depth)
                );
                }
                // unity-custom-func-end
                
                struct Bindings_ParallaxSampleTexture2D_ae45c5063979a430984f11ebf013c35c_float
                {
                float3 ObjectSpaceViewDirection;
                half4 uv0;
                };
                
                void SG_ParallaxSampleTexture2D_ae45c5063979a430984f11ebf013c35c_float(UnityTexture2D _Texture, float2 _Depth, float _Flip_X, Bindings_ParallaxSampleTexture2D_ae45c5063979a430984f11ebf013c35c_float IN, out float Out_R_2, out float Out_G_3, out float Out_B_1)
                {
                UnityTexture2D _Property_dad3badd99624fdbab11879bd30d376a_Out_0_Texture2D = _Texture;
                float4 _UV_4592539f7377446791ce24767a34c92a_Out_0_Vector4 = IN.uv0;
                float _Property_eb437bd110674c94aa025481bbeb9168_Out_0_Boolean = _Flip_X;
                float _Branch_2db3155f235b4e9aad311b592b7680c6_Out_3_Float;
                Unity_Branch_float(_Property_eb437bd110674c94aa025481bbeb9168_Out_0_Boolean, float(-1), float(1), _Branch_2db3155f235b4e9aad311b592b7680c6_Out_3_Float);
                float4 _Combine_e831df117fad41e7a7aca27e3cb0828c_RGBA_4_Vector4;
                float3 _Combine_e831df117fad41e7a7aca27e3cb0828c_RGB_5_Vector3;
                float2 _Combine_e831df117fad41e7a7aca27e3cb0828c_RG_6_Vector2;
                Unity_Combine_float(_Branch_2db3155f235b4e9aad311b592b7680c6_Out_3_Float, float(1), float(1), float(1), _Combine_e831df117fad41e7a7aca27e3cb0828c_RGBA_4_Vector4, _Combine_e831df117fad41e7a7aca27e3cb0828c_RGB_5_Vector3, _Combine_e831df117fad41e7a7aca27e3cb0828c_RG_6_Vector2);
                float3 _Multiply_5b8a4ed7c54545d98ac880a3d084e129_Out_2_Vector3;
                Unity_Multiply_float3_float3(IN.ObjectSpaceViewDirection, _Combine_e831df117fad41e7a7aca27e3cb0828c_RGB_5_Vector3, _Multiply_5b8a4ed7c54545d98ac880a3d084e129_Out_2_Vector3);
                float2 _Property_db40aa4398dc42d2a16ff4e25da19fb8_Out_0_Vector2 = _Depth;
                float2 _Negate_9ba8a9d8334f438fb84d14622429bed5_Out_1_Vector2;
                Unity_Negate_float2(_Property_db40aa4398dc42d2a16ff4e25da19fb8_Out_0_Vector2, _Negate_9ba8a9d8334f438fb84d14622429bed5_Out_1_Vector2);
                float2 _ParallaxUVCustomFunction_d355531d1569421bb20205b595d4c55c_OutUV_3_Vector2;
                ParallaxUV_float((_UV_4592539f7377446791ce24767a34c92a_Out_0_Vector4.xy), _Multiply_5b8a4ed7c54545d98ac880a3d084e129_Out_2_Vector3, _Negate_9ba8a9d8334f438fb84d14622429bed5_Out_1_Vector2, _ParallaxUVCustomFunction_d355531d1569421bb20205b595d4c55c_OutUV_3_Vector2);
                float4 _SampleTexture2D_f5331c8e752f4566b67010e5d21b5a3b_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_dad3badd99624fdbab11879bd30d376a_Out_0_Texture2D.tex, _Property_dad3badd99624fdbab11879bd30d376a_Out_0_Texture2D.samplerstate, _Property_dad3badd99624fdbab11879bd30d376a_Out_0_Texture2D.GetTransformedUV(_ParallaxUVCustomFunction_d355531d1569421bb20205b595d4c55c_OutUV_3_Vector2) );
                float _SampleTexture2D_f5331c8e752f4566b67010e5d21b5a3b_R_4_Float = _SampleTexture2D_f5331c8e752f4566b67010e5d21b5a3b_RGBA_0_Vector4.r;
                float _SampleTexture2D_f5331c8e752f4566b67010e5d21b5a3b_G_5_Float = _SampleTexture2D_f5331c8e752f4566b67010e5d21b5a3b_RGBA_0_Vector4.g;
                float _SampleTexture2D_f5331c8e752f4566b67010e5d21b5a3b_B_6_Float = _SampleTexture2D_f5331c8e752f4566b67010e5d21b5a3b_RGBA_0_Vector4.b;
                float _SampleTexture2D_f5331c8e752f4566b67010e5d21b5a3b_A_7_Float = _SampleTexture2D_f5331c8e752f4566b67010e5d21b5a3b_RGBA_0_Vector4.a;
                Out_R_2 = _SampleTexture2D_f5331c8e752f4566b67010e5d21b5a3b_R_4_Float;
                Out_G_3 = _SampleTexture2D_f5331c8e752f4566b67010e5d21b5a3b_G_5_Float;
                Out_B_1 = _SampleTexture2D_f5331c8e752f4566b67010e5d21b5a3b_B_6_Float;
                }
                
                void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
                {
                    Out = A * B;
                }
                
                void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
                {
                    Out = lerp(A, B, T);
                }
                
                void Unity_Add_float(float A, float B, out float Out)
                {
                    Out = A + B;
                }
                
                void Unity_Branch_float3(float Predicate, float3 True, float3 False, out float3 Out)
                {
                    Out = Predicate ? True : False;
                }
            
            // Custom interpolators pre vertex
            /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
            
            // Graph Vertex
            struct VertexDescription
                {
                    float3 Position;
                    float3 Normal;
                    float3 Tangent;
                };
                
                VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
                {
                    VertexDescription description = (VertexDescription)0;
                    description.Position = IN.ObjectSpacePosition;
                    description.Normal = IN.ObjectSpaceNormal;
                    description.Tangent = IN.ObjectSpaceTangent;
                    return description;
                }
            
            // Custom interpolators, pre surface
            #ifdef FEATURES_GRAPH_VERTEX
            Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
            {
            return output;
            }
            #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
            #endif
            
            // Graph Pixel
            struct SurfaceDescription
                {
                    float3 BaseColor;
                    float3 NormalTS;
                    float3 Emission;
                    float Metallic;
                    float Smoothness;
                    float Occlusion;
                    float Alpha;
                };
                
                SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
                {
                    SurfaceDescription surface = (SurfaceDescription)0;
                    float4 _Property_c6e5cb5bdf714fe8a6ff8a4db984ddee_Out_0_Vector4 = _Mouth_Color;
                    float4 _Property_6a93ac1325de4cb79d98f6caea13fdd8_Out_0_Vector4 = _Mouth_Color;
                    UnityTexture2D _Property_4dc7fd4f69e743aab88f1c4097303f71_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_Texture);
                    float _Property_e7251618caa74325ad70e40dd4b915cf_Out_0_Float = _Tongue_Depth;
                    float2 _Vector2_2e29f538f8a44455a027aee5898dae58_Out_0_Vector2 = float2(float(1), float(1));
                    float2 _Multiply_b2d5eac04cba4b1f887426a6bfc69bb5_Out_2_Vector2;
                    Unity_Multiply_float2_float2((_Property_e7251618caa74325ad70e40dd4b915cf_Out_0_Float.xx), _Vector2_2e29f538f8a44455a027aee5898dae58_Out_0_Vector2, _Multiply_b2d5eac04cba4b1f887426a6bfc69bb5_Out_2_Vector2);
                    float _Property_6097c9c4024844b7a53ab897340bc2b0_Out_0_Boolean = _Flip_X;
                    Bindings_ParallaxSampleTexture2D_ae45c5063979a430984f11ebf013c35c_float _ParallaxSampleTexture2D_c29d966c4be34c4bbdd1fe8a01531312;
                    _ParallaxSampleTexture2D_c29d966c4be34c4bbdd1fe8a01531312.ObjectSpaceViewDirection = IN.ObjectSpaceViewDirection;
                    _ParallaxSampleTexture2D_c29d966c4be34c4bbdd1fe8a01531312.uv0 = IN.uv0;
                    float _ParallaxSampleTexture2D_c29d966c4be34c4bbdd1fe8a01531312_OutR_2_Float;
                    float _ParallaxSampleTexture2D_c29d966c4be34c4bbdd1fe8a01531312_OutG_3_Float;
                    float _ParallaxSampleTexture2D_c29d966c4be34c4bbdd1fe8a01531312_OutB_1_Float;
                    SG_ParallaxSampleTexture2D_ae45c5063979a430984f11ebf013c35c_float(_Property_4dc7fd4f69e743aab88f1c4097303f71_Out_0_Texture2D, _Multiply_b2d5eac04cba4b1f887426a6bfc69bb5_Out_2_Vector2, _Property_6097c9c4024844b7a53ab897340bc2b0_Out_0_Boolean, _ParallaxSampleTexture2D_c29d966c4be34c4bbdd1fe8a01531312, _ParallaxSampleTexture2D_c29d966c4be34c4bbdd1fe8a01531312_OutR_2_Float, _ParallaxSampleTexture2D_c29d966c4be34c4bbdd1fe8a01531312_OutG_3_Float, _ParallaxSampleTexture2D_c29d966c4be34c4bbdd1fe8a01531312_OutB_1_Float);
                    float4 _Property_af6a9a8c31b845a6af3143281d2bf53b_Out_0_Vector4 = _Tongue_Color;
                    float4 _Multiply_e4efce35b80a4b2cafb8b9adf899cba8_Out_2_Vector4;
                    Unity_Multiply_float4_float4((_ParallaxSampleTexture2D_c29d966c4be34c4bbdd1fe8a01531312_OutB_1_Float.xxxx), _Property_af6a9a8c31b845a6af3143281d2bf53b_Out_0_Vector4, _Multiply_e4efce35b80a4b2cafb8b9adf899cba8_Out_2_Vector4);
                    float4 _Lerp_c14b8c38c754445685b381d8451856d3_Out_3_Vector4;
                    Unity_Lerp_float4(_Property_6a93ac1325de4cb79d98f6caea13fdd8_Out_0_Vector4, _Multiply_e4efce35b80a4b2cafb8b9adf899cba8_Out_2_Vector4, (_ParallaxSampleTexture2D_c29d966c4be34c4bbdd1fe8a01531312_OutB_1_Float.xxxx), _Lerp_c14b8c38c754445685b381d8451856d3_Out_3_Vector4);
                    float4 _Property_361f5fa06ce3451f93bb5d7380c2da3e_Out_0_Vector4 = _Teeth_Edge_Color;
                    UnityTexture2D _Property_532fa8ab3762489185678100eb2df2b3_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_Texture);
                    float _Property_37191fa8d35f47f2b1e681595f7e55f4_Out_0_Float = _Teeth_Depth;
                    float _Property_b3e5f83350924b82878b321ad1c8327f_Out_0_Float = _Teeth_Thickness;
                    float _Add_562c66b73f9a4fc59ae477a4c3b081fc_Out_2_Float;
                    Unity_Add_float(_Property_37191fa8d35f47f2b1e681595f7e55f4_Out_0_Float, _Property_b3e5f83350924b82878b321ad1c8327f_Out_0_Float, _Add_562c66b73f9a4fc59ae477a4c3b081fc_Out_2_Float);
                    Bindings_ParallaxSampleTexture2D_ae45c5063979a430984f11ebf013c35c_float _ParallaxSampleTexture2D_e67be38c032a40e087adc2e477807e7d;
                    _ParallaxSampleTexture2D_e67be38c032a40e087adc2e477807e7d.ObjectSpaceViewDirection = IN.ObjectSpaceViewDirection;
                    _ParallaxSampleTexture2D_e67be38c032a40e087adc2e477807e7d.uv0 = IN.uv0;
                    float _ParallaxSampleTexture2D_e67be38c032a40e087adc2e477807e7d_OutR_2_Float;
                    float _ParallaxSampleTexture2D_e67be38c032a40e087adc2e477807e7d_OutG_3_Float;
                    float _ParallaxSampleTexture2D_e67be38c032a40e087adc2e477807e7d_OutB_1_Float;
                    SG_ParallaxSampleTexture2D_ae45c5063979a430984f11ebf013c35c_float(_Property_532fa8ab3762489185678100eb2df2b3_Out_0_Texture2D, (_Add_562c66b73f9a4fc59ae477a4c3b081fc_Out_2_Float.xx), 0, _ParallaxSampleTexture2D_e67be38c032a40e087adc2e477807e7d, _ParallaxSampleTexture2D_e67be38c032a40e087adc2e477807e7d_OutR_2_Float, _ParallaxSampleTexture2D_e67be38c032a40e087adc2e477807e7d_OutG_3_Float, _ParallaxSampleTexture2D_e67be38c032a40e087adc2e477807e7d_OutB_1_Float);
                    float4 _Lerp_5f6226b27af847188fbc7109af235632_Out_3_Vector4;
                    Unity_Lerp_float4(_Lerp_c14b8c38c754445685b381d8451856d3_Out_3_Vector4, _Property_361f5fa06ce3451f93bb5d7380c2da3e_Out_0_Vector4, (_ParallaxSampleTexture2D_e67be38c032a40e087adc2e477807e7d_OutG_3_Float.xxxx), _Lerp_5f6226b27af847188fbc7109af235632_Out_3_Vector4);
                    float4 _Property_695a3e46fe5548aa87913f3d4c0ecf05_Out_0_Vector4 = _Teeth_Color;
                    UnityTexture2D _Property_b738848bca344d979f28a5e8709ce50a_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_Texture);
                    float _Property_6497ac1f7d7f451f98435734295f617c_Out_0_Float = _Teeth_Depth;
                    float _Property_d9e84b7e76784f3e8aa00bd8bfe14599_Out_0_Boolean = _Flip_X;
                    Bindings_ParallaxSampleTexture2D_ae45c5063979a430984f11ebf013c35c_float _ParallaxSampleTexture2D_26eb78f900f5474b9ded770a5d15b9c7;
                    _ParallaxSampleTexture2D_26eb78f900f5474b9ded770a5d15b9c7.ObjectSpaceViewDirection = IN.ObjectSpaceViewDirection;
                    _ParallaxSampleTexture2D_26eb78f900f5474b9ded770a5d15b9c7.uv0 = IN.uv0;
                    float _ParallaxSampleTexture2D_26eb78f900f5474b9ded770a5d15b9c7_OutR_2_Float;
                    float _ParallaxSampleTexture2D_26eb78f900f5474b9ded770a5d15b9c7_OutG_3_Float;
                    float _ParallaxSampleTexture2D_26eb78f900f5474b9ded770a5d15b9c7_OutB_1_Float;
                    SG_ParallaxSampleTexture2D_ae45c5063979a430984f11ebf013c35c_float(_Property_b738848bca344d979f28a5e8709ce50a_Out_0_Texture2D, (_Property_6497ac1f7d7f451f98435734295f617c_Out_0_Float.xx), _Property_d9e84b7e76784f3e8aa00bd8bfe14599_Out_0_Boolean, _ParallaxSampleTexture2D_26eb78f900f5474b9ded770a5d15b9c7, _ParallaxSampleTexture2D_26eb78f900f5474b9ded770a5d15b9c7_OutR_2_Float, _ParallaxSampleTexture2D_26eb78f900f5474b9ded770a5d15b9c7_OutG_3_Float, _ParallaxSampleTexture2D_26eb78f900f5474b9ded770a5d15b9c7_OutB_1_Float);
                    float4 _Lerp_3951173a3ad147bda8daa33a464ba7e4_Out_3_Vector4;
                    Unity_Lerp_float4(_Lerp_5f6226b27af847188fbc7109af235632_Out_3_Vector4, _Property_695a3e46fe5548aa87913f3d4c0ecf05_Out_0_Vector4, (_ParallaxSampleTexture2D_26eb78f900f5474b9ded770a5d15b9c7_OutG_3_Float.xxxx), _Lerp_3951173a3ad147bda8daa33a464ba7e4_Out_3_Vector4);
                    UnityTexture2D _Property_a67db6e8d526453c89a445ef89b619e3_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_Texture);
                    float _Property_764a5b6e97fb4e6cb52af22e014d2cb7_Out_0_Float = _Mouth_Depth;
                    float2 _Vector2_1dc9ddcac0504b7daf87eeaf74d1347b_Out_0_Vector2 = float2(float(1), float(1));
                    float2 _Multiply_4a47a872e85a4a4c8c8494908dbe7767_Out_2_Vector2;
                    Unity_Multiply_float2_float2((_Property_764a5b6e97fb4e6cb52af22e014d2cb7_Out_0_Float.xx), _Vector2_1dc9ddcac0504b7daf87eeaf74d1347b_Out_0_Vector2, _Multiply_4a47a872e85a4a4c8c8494908dbe7767_Out_2_Vector2);
                    float _Property_40b6c2c1ebd649ee85152cbb137d4219_Out_0_Boolean = _Flip_X;
                    Bindings_ParallaxSampleTexture2D_ae45c5063979a430984f11ebf013c35c_float _ParallaxSampleTexture2D_bb9439882a6f45b096dccfb671f40ade;
                    _ParallaxSampleTexture2D_bb9439882a6f45b096dccfb671f40ade.ObjectSpaceViewDirection = IN.ObjectSpaceViewDirection;
                    _ParallaxSampleTexture2D_bb9439882a6f45b096dccfb671f40ade.uv0 = IN.uv0;
                    float _ParallaxSampleTexture2D_bb9439882a6f45b096dccfb671f40ade_OutR_2_Float;
                    float _ParallaxSampleTexture2D_bb9439882a6f45b096dccfb671f40ade_OutG_3_Float;
                    float _ParallaxSampleTexture2D_bb9439882a6f45b096dccfb671f40ade_OutB_1_Float;
                    SG_ParallaxSampleTexture2D_ae45c5063979a430984f11ebf013c35c_float(_Property_a67db6e8d526453c89a445ef89b619e3_Out_0_Texture2D, _Multiply_4a47a872e85a4a4c8c8494908dbe7767_Out_2_Vector2, _Property_40b6c2c1ebd649ee85152cbb137d4219_Out_0_Boolean, _ParallaxSampleTexture2D_bb9439882a6f45b096dccfb671f40ade, _ParallaxSampleTexture2D_bb9439882a6f45b096dccfb671f40ade_OutR_2_Float, _ParallaxSampleTexture2D_bb9439882a6f45b096dccfb671f40ade_OutG_3_Float, _ParallaxSampleTexture2D_bb9439882a6f45b096dccfb671f40ade_OutB_1_Float);
                    float4 _Lerp_9412d0275e2b47429cb89b3b89798977_Out_3_Vector4;
                    Unity_Lerp_float4(_Property_c6e5cb5bdf714fe8a6ff8a4db984ddee_Out_0_Vector4, _Lerp_3951173a3ad147bda8daa33a464ba7e4_Out_3_Vector4, (_ParallaxSampleTexture2D_bb9439882a6f45b096dccfb671f40ade_OutR_2_Float.xxxx), _Lerp_9412d0275e2b47429cb89b3b89798977_Out_3_Vector4);
                    float3 _Branch_48ee12d07e0447a8a1b11405ef31386c_Out_3_Vector3;
                    Unity_Branch_float3(0, IN.ObjectSpaceViewDirection, (_Lerp_9412d0275e2b47429cb89b3b89798977_Out_3_Vector4.xyz), _Branch_48ee12d07e0447a8a1b11405ef31386c_Out_3_Vector3);
                    UnityTexture2D _Property_fc4d5cb8867142b7af1000f12a0b5617_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_Texture);
                    float4 _SampleTexture2D_39637566a9c84bb8aa66f76590d11702_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_fc4d5cb8867142b7af1000f12a0b5617_Out_0_Texture2D.tex, _Property_fc4d5cb8867142b7af1000f12a0b5617_Out_0_Texture2D.samplerstate, _Property_fc4d5cb8867142b7af1000f12a0b5617_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
                    float _SampleTexture2D_39637566a9c84bb8aa66f76590d11702_R_4_Float = _SampleTexture2D_39637566a9c84bb8aa66f76590d11702_RGBA_0_Vector4.r;
                    float _SampleTexture2D_39637566a9c84bb8aa66f76590d11702_G_5_Float = _SampleTexture2D_39637566a9c84bb8aa66f76590d11702_RGBA_0_Vector4.g;
                    float _SampleTexture2D_39637566a9c84bb8aa66f76590d11702_B_6_Float = _SampleTexture2D_39637566a9c84bb8aa66f76590d11702_RGBA_0_Vector4.b;
                    float _SampleTexture2D_39637566a9c84bb8aa66f76590d11702_A_7_Float = _SampleTexture2D_39637566a9c84bb8aa66f76590d11702_RGBA_0_Vector4.a;
                    surface.BaseColor = _Branch_48ee12d07e0447a8a1b11405ef31386c_Out_3_Vector3;
                    surface.NormalTS = IN.TangentSpaceNormal;
                    surface.Emission = float3(0, 0, 0);
                    surface.Metallic = float(0);
                    surface.Smoothness = float(0.1);
                    surface.Occlusion = float(1);
                    surface.Alpha = _SampleTexture2D_39637566a9c84bb8aa66f76590d11702_R_4_Float;
                    return surface;
                }
            
            // --------------------------------------------------
            // Build Graph Inputs
            #ifdef HAVE_VFX_MODIFICATION
            #define VFX_SRP_ATTRIBUTES Attributes
            #define VFX_SRP_VARYINGS Varyings
            #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
            #endif
            VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
                {
                    VertexDescriptionInputs output;
                    ZERO_INITIALIZE(VertexDescriptionInputs, output);
                
                    output.ObjectSpaceNormal =                          input.normalOS;
                    output.ObjectSpaceTangent =                         input.tangentOS.xyz;
                    output.ObjectSpacePosition =                        input.positionOS;
                #if UNITY_ANY_INSTANCING_ENABLED
                #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
                #endif
                
                    return output;
                }
                
            SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
                {
                    SurfaceDescriptionInputs output;
                    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
                
                #ifdef HAVE_VFX_MODIFICATION
                #if VFX_USE_GRAPH_VALUES
                    uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
                    /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
                #endif
                    /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
                
                #endif
                
                    
                
                
                
                    output.TangentSpaceNormal = float3(0.0f, 0.0f, 1.0f);
                
                
                    output.WorldSpaceViewDirection = GetWorldSpaceNormalizeViewDir(input.positionWS);
                    output.ObjectSpaceViewDirection = TransformWorldToObjectDir(output.WorldSpaceViewDirection);
                
                    #if UNITY_UV_STARTS_AT_TOP
                    #else
                    #endif
                
                
                    output.uv0 = input.texCoord0;
                #if UNITY_ANY_INSTANCING_ENABLED
                #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
                #endif
                #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
                #else
                #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
                #endif
                #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
                
                        return output;
                }
                
            
            // --------------------------------------------------
            // Main
            
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/PBRForwardPass.hlsl"
            
            // --------------------------------------------------
            // Visual Effect Vertex Invocations
            #ifdef HAVE_VFX_MODIFICATION
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
            #endif
            
            ENDHLSL
            }
            Pass
            {
                Name "GBuffer"
                Tags
                {
                    "LightMode" = "UniversalGBuffer"
                }
            
            // Render State
            Cull Back
                Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
                ZTest LEqual
                ZWrite Off
            
            // Debug
            // <None>
            
            // --------------------------------------------------
            // Pass
            
            HLSLPROGRAM
            
            // Pragmas
            #pragma target 4.5
                #pragma exclude_renderers gles3 glcore
                #pragma multi_compile_instancing
                #pragma multi_compile_fog
                #pragma instancing_options renderinglayer
                #pragma vertex vert
                #pragma fragment frag
            
            // Keywords
            #pragma multi_compile _ LIGHTMAP_ON
                #pragma multi_compile _ DYNAMICLIGHTMAP_ON
                #pragma multi_compile _ DIRLIGHTMAP_COMBINED
                #pragma multi_compile _ USE_LEGACY_LIGHTMAPS
                #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
                #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
                #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
                #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
                #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
                #pragma multi_compile _ SHADOWS_SHADOWMASK
                #pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE
                #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
                #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
                #pragma multi_compile_fragment _ _RENDER_PASS_ENABLED
                #pragma multi_compile_fragment _ DEBUG_DISPLAY
            // GraphKeywords: <None>
            
            // Defines
            
            #define _NORMALMAP 1
            #define _NORMAL_DROPOFF_TS 1
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define ATTRIBUTES_NEED_TEXCOORD0
            #define ATTRIBUTES_NEED_TEXCOORD1
            #define ATTRIBUTES_NEED_TEXCOORD2
            #define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
            #define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
            #define VARYINGS_NEED_POSITION_WS
            #define VARYINGS_NEED_NORMAL_WS
            #define VARYINGS_NEED_TANGENT_WS
            #define VARYINGS_NEED_TEXCOORD0
            #define VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
            #define VARYINGS_NEED_SHADOW_COORD
            #define FEATURES_GRAPH_VERTEX
            /* WARNING: $splice Could not find named fragment 'PassInstancing' */
            #define SHADERPASS SHADERPASS_GBUFFER
                #define _FOG_FRAGMENT 1
                #define _SURFACE_TYPE_TRANSPARENT 1
            
            
            // custom interpolator pre-include
            /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
            
            // Includes
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ProbeVolumeVariants.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
            
            // --------------------------------------------------
            // Structs and Packing
            
            // custom interpolators pre packing
            /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
            
            struct Attributes
                {
                     float3 positionOS : POSITION;
                     float3 normalOS : NORMAL;
                     float4 tangentOS : TANGENT;
                     float4 uv0 : TEXCOORD0;
                     float4 uv1 : TEXCOORD1;
                     float4 uv2 : TEXCOORD2;
                    #if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
                     uint instanceID : INSTANCEID_SEMANTIC;
                    #endif
                };
                struct Varyings
                {
                     float4 positionCS : SV_POSITION;
                     float3 positionWS;
                     float3 normalWS;
                     float4 tangentWS;
                     float4 texCoord0;
                    #if defined(LIGHTMAP_ON)
                     float2 staticLightmapUV;
                    #endif
                    #if defined(DYNAMICLIGHTMAP_ON)
                     float2 dynamicLightmapUV;
                    #endif
                    #if !defined(LIGHTMAP_ON)
                     float3 sh;
                    #endif
                    #if defined(USE_APV_PROBE_OCCLUSION)
                     float4 probeOcclusion;
                    #endif
                     float4 fogFactorAndVertexLight;
                    #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                     float4 shadowCoord;
                    #endif
                    #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
                     uint instanceID : CUSTOM_INSTANCE_ID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                     uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                     uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                     FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
                    #endif
                };
                struct SurfaceDescriptionInputs
                {
                     float3 TangentSpaceNormal;
                     float3 ObjectSpaceViewDirection;
                     float3 WorldSpaceViewDirection;
                     float4 uv0;
                };
                struct VertexDescriptionInputs
                {
                     float3 ObjectSpaceNormal;
                     float3 ObjectSpaceTangent;
                     float3 ObjectSpacePosition;
                };
                struct PackedVaryings
                {
                     float4 positionCS : SV_POSITION;
                    #if defined(LIGHTMAP_ON)
                     float2 staticLightmapUV : INTERP0;
                    #endif
                    #if defined(DYNAMICLIGHTMAP_ON)
                     float2 dynamicLightmapUV : INTERP1;
                    #endif
                    #if !defined(LIGHTMAP_ON)
                     float3 sh : INTERP2;
                    #endif
                    #if defined(USE_APV_PROBE_OCCLUSION)
                     float4 probeOcclusion : INTERP3;
                    #endif
                    #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                     float4 shadowCoord : INTERP4;
                    #endif
                     float4 tangentWS : INTERP5;
                     float4 texCoord0 : INTERP6;
                     float4 fogFactorAndVertexLight : INTERP7;
                     float3 positionWS : INTERP8;
                     float3 normalWS : INTERP9;
                    #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
                     uint instanceID : CUSTOM_INSTANCE_ID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                     uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                     uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                     FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
                    #endif
                };
            
            PackedVaryings PackVaryings (Varyings input)
                {
                    PackedVaryings output;
                    ZERO_INITIALIZE(PackedVaryings, output);
                    output.positionCS = input.positionCS;
                    #if defined(LIGHTMAP_ON)
                    output.staticLightmapUV = input.staticLightmapUV;
                    #endif
                    #if defined(DYNAMICLIGHTMAP_ON)
                    output.dynamicLightmapUV = input.dynamicLightmapUV;
                    #endif
                    #if !defined(LIGHTMAP_ON)
                    output.sh = input.sh;
                    #endif
                    #if defined(USE_APV_PROBE_OCCLUSION)
                    output.probeOcclusion = input.probeOcclusion;
                    #endif
                    #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                    output.shadowCoord = input.shadowCoord;
                    #endif
                    output.tangentWS.xyzw = input.tangentWS;
                    output.texCoord0.xyzw = input.texCoord0;
                    output.fogFactorAndVertexLight.xyzw = input.fogFactorAndVertexLight;
                    output.positionWS.xyz = input.positionWS;
                    output.normalWS.xyz = input.normalWS;
                    #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
                    output.instanceID = input.instanceID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                    output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                    output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                    output.cullFace = input.cullFace;
                    #endif
                    return output;
                }
                
                Varyings UnpackVaryings (PackedVaryings input)
                {
                    Varyings output;
                    output.positionCS = input.positionCS;
                    #if defined(LIGHTMAP_ON)
                    output.staticLightmapUV = input.staticLightmapUV;
                    #endif
                    #if defined(DYNAMICLIGHTMAP_ON)
                    output.dynamicLightmapUV = input.dynamicLightmapUV;
                    #endif
                    #if !defined(LIGHTMAP_ON)
                    output.sh = input.sh;
                    #endif
                    #if defined(USE_APV_PROBE_OCCLUSION)
                    output.probeOcclusion = input.probeOcclusion;
                    #endif
                    #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                    output.shadowCoord = input.shadowCoord;
                    #endif
                    output.tangentWS = input.tangentWS.xyzw;
                    output.texCoord0 = input.texCoord0.xyzw;
                    output.fogFactorAndVertexLight = input.fogFactorAndVertexLight.xyzw;
                    output.positionWS = input.positionWS.xyz;
                    output.normalWS = input.normalWS.xyz;
                    #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
                    output.instanceID = input.instanceID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                    output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                    output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                    output.cullFace = input.cullFace;
                    #endif
                    return output;
                }
                
            
            // --------------------------------------------------
            // Graph
            
            // Graph Properties
            CBUFFER_START(UnityPerMaterial)
                float _Teeth_Depth;
                float4 _Texture_TexelSize;
                float _Tongue_Depth;
                float4 _Mouth_Color;
                float4 _Tongue_Color;
                float4 _Teeth_Color;
                float4 _Teeth_Edge_Color;
                float _Teeth_Thickness;
                float _Mouth_Depth;
                float _Flip_X;
                UNITY_TEXTURE_STREAMING_DEBUG_VARS;
                CBUFFER_END
                
                
                // Object and Global properties
                SAMPLER(SamplerState_Linear_Repeat);
                TEXTURE2D(_Texture);
                SAMPLER(sampler_Texture);
            
            // Graph Includes
            // GraphIncludes: <None>
            
            // -- Property used by ScenePickingPass
            #ifdef SCENEPICKINGPASS
            float4 _SelectionID;
            #endif
            
            // -- Properties used by SceneSelectionPass
            #ifdef SCENESELECTIONPASS
            int _ObjectId;
            int _PassValue;
            #endif
            
            // Graph Functions
            
                void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
                {
                    Out = A * B;
                }
                
                void Unity_Branch_float(float Predicate, float True, float False, out float Out)
                {
                    Out = Predicate ? True : False;
                }
                
                void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
                {
                    RGBA = float4(R, G, B, A);
                    RGB = float3(R, G, B);
                    RG = float2(R, G);
                }
                
                void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
                {
                Out = A * B;
                }
                
                void Unity_Negate_float2(float2 In, out float2 Out)
                {
                    Out = -1 * In;
                }
                
                // unity-custom-func-begin
                void ParallaxUV_float(float2 UV, float3 ViewDirection, float2 Depth, out float2 OutUV){
                OutUV = saturate(
                  UV  + (ViewDirection.xz
                  * Depth)
                );
                }
                // unity-custom-func-end
                
                struct Bindings_ParallaxSampleTexture2D_ae45c5063979a430984f11ebf013c35c_float
                {
                float3 ObjectSpaceViewDirection;
                half4 uv0;
                };
                
                void SG_ParallaxSampleTexture2D_ae45c5063979a430984f11ebf013c35c_float(UnityTexture2D _Texture, float2 _Depth, float _Flip_X, Bindings_ParallaxSampleTexture2D_ae45c5063979a430984f11ebf013c35c_float IN, out float Out_R_2, out float Out_G_3, out float Out_B_1)
                {
                UnityTexture2D _Property_dad3badd99624fdbab11879bd30d376a_Out_0_Texture2D = _Texture;
                float4 _UV_4592539f7377446791ce24767a34c92a_Out_0_Vector4 = IN.uv0;
                float _Property_eb437bd110674c94aa025481bbeb9168_Out_0_Boolean = _Flip_X;
                float _Branch_2db3155f235b4e9aad311b592b7680c6_Out_3_Float;
                Unity_Branch_float(_Property_eb437bd110674c94aa025481bbeb9168_Out_0_Boolean, float(-1), float(1), _Branch_2db3155f235b4e9aad311b592b7680c6_Out_3_Float);
                float4 _Combine_e831df117fad41e7a7aca27e3cb0828c_RGBA_4_Vector4;
                float3 _Combine_e831df117fad41e7a7aca27e3cb0828c_RGB_5_Vector3;
                float2 _Combine_e831df117fad41e7a7aca27e3cb0828c_RG_6_Vector2;
                Unity_Combine_float(_Branch_2db3155f235b4e9aad311b592b7680c6_Out_3_Float, float(1), float(1), float(1), _Combine_e831df117fad41e7a7aca27e3cb0828c_RGBA_4_Vector4, _Combine_e831df117fad41e7a7aca27e3cb0828c_RGB_5_Vector3, _Combine_e831df117fad41e7a7aca27e3cb0828c_RG_6_Vector2);
                float3 _Multiply_5b8a4ed7c54545d98ac880a3d084e129_Out_2_Vector3;
                Unity_Multiply_float3_float3(IN.ObjectSpaceViewDirection, _Combine_e831df117fad41e7a7aca27e3cb0828c_RGB_5_Vector3, _Multiply_5b8a4ed7c54545d98ac880a3d084e129_Out_2_Vector3);
                float2 _Property_db40aa4398dc42d2a16ff4e25da19fb8_Out_0_Vector2 = _Depth;
                float2 _Negate_9ba8a9d8334f438fb84d14622429bed5_Out_1_Vector2;
                Unity_Negate_float2(_Property_db40aa4398dc42d2a16ff4e25da19fb8_Out_0_Vector2, _Negate_9ba8a9d8334f438fb84d14622429bed5_Out_1_Vector2);
                float2 _ParallaxUVCustomFunction_d355531d1569421bb20205b595d4c55c_OutUV_3_Vector2;
                ParallaxUV_float((_UV_4592539f7377446791ce24767a34c92a_Out_0_Vector4.xy), _Multiply_5b8a4ed7c54545d98ac880a3d084e129_Out_2_Vector3, _Negate_9ba8a9d8334f438fb84d14622429bed5_Out_1_Vector2, _ParallaxUVCustomFunction_d355531d1569421bb20205b595d4c55c_OutUV_3_Vector2);
                float4 _SampleTexture2D_f5331c8e752f4566b67010e5d21b5a3b_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_dad3badd99624fdbab11879bd30d376a_Out_0_Texture2D.tex, _Property_dad3badd99624fdbab11879bd30d376a_Out_0_Texture2D.samplerstate, _Property_dad3badd99624fdbab11879bd30d376a_Out_0_Texture2D.GetTransformedUV(_ParallaxUVCustomFunction_d355531d1569421bb20205b595d4c55c_OutUV_3_Vector2) );
                float _SampleTexture2D_f5331c8e752f4566b67010e5d21b5a3b_R_4_Float = _SampleTexture2D_f5331c8e752f4566b67010e5d21b5a3b_RGBA_0_Vector4.r;
                float _SampleTexture2D_f5331c8e752f4566b67010e5d21b5a3b_G_5_Float = _SampleTexture2D_f5331c8e752f4566b67010e5d21b5a3b_RGBA_0_Vector4.g;
                float _SampleTexture2D_f5331c8e752f4566b67010e5d21b5a3b_B_6_Float = _SampleTexture2D_f5331c8e752f4566b67010e5d21b5a3b_RGBA_0_Vector4.b;
                float _SampleTexture2D_f5331c8e752f4566b67010e5d21b5a3b_A_7_Float = _SampleTexture2D_f5331c8e752f4566b67010e5d21b5a3b_RGBA_0_Vector4.a;
                Out_R_2 = _SampleTexture2D_f5331c8e752f4566b67010e5d21b5a3b_R_4_Float;
                Out_G_3 = _SampleTexture2D_f5331c8e752f4566b67010e5d21b5a3b_G_5_Float;
                Out_B_1 = _SampleTexture2D_f5331c8e752f4566b67010e5d21b5a3b_B_6_Float;
                }
                
                void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
                {
                    Out = A * B;
                }
                
                void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
                {
                    Out = lerp(A, B, T);
                }
                
                void Unity_Add_float(float A, float B, out float Out)
                {
                    Out = A + B;
                }
                
                void Unity_Branch_float3(float Predicate, float3 True, float3 False, out float3 Out)
                {
                    Out = Predicate ? True : False;
                }
            
            // Custom interpolators pre vertex
            /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
            
            // Graph Vertex
            struct VertexDescription
                {
                    float3 Position;
                    float3 Normal;
                    float3 Tangent;
                };
                
                VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
                {
                    VertexDescription description = (VertexDescription)0;
                    description.Position = IN.ObjectSpacePosition;
                    description.Normal = IN.ObjectSpaceNormal;
                    description.Tangent = IN.ObjectSpaceTangent;
                    return description;
                }
            
            // Custom interpolators, pre surface
            #ifdef FEATURES_GRAPH_VERTEX
            Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
            {
            return output;
            }
            #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
            #endif
            
            // Graph Pixel
            struct SurfaceDescription
                {
                    float3 BaseColor;
                    float3 NormalTS;
                    float3 Emission;
                    float Metallic;
                    float Smoothness;
                    float Occlusion;
                    float Alpha;
                };
                
                SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
                {
                    SurfaceDescription surface = (SurfaceDescription)0;
                    float4 _Property_c6e5cb5bdf714fe8a6ff8a4db984ddee_Out_0_Vector4 = _Mouth_Color;
                    float4 _Property_6a93ac1325de4cb79d98f6caea13fdd8_Out_0_Vector4 = _Mouth_Color;
                    UnityTexture2D _Property_4dc7fd4f69e743aab88f1c4097303f71_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_Texture);
                    float _Property_e7251618caa74325ad70e40dd4b915cf_Out_0_Float = _Tongue_Depth;
                    float2 _Vector2_2e29f538f8a44455a027aee5898dae58_Out_0_Vector2 = float2(float(1), float(1));
                    float2 _Multiply_b2d5eac04cba4b1f887426a6bfc69bb5_Out_2_Vector2;
                    Unity_Multiply_float2_float2((_Property_e7251618caa74325ad70e40dd4b915cf_Out_0_Float.xx), _Vector2_2e29f538f8a44455a027aee5898dae58_Out_0_Vector2, _Multiply_b2d5eac04cba4b1f887426a6bfc69bb5_Out_2_Vector2);
                    float _Property_6097c9c4024844b7a53ab897340bc2b0_Out_0_Boolean = _Flip_X;
                    Bindings_ParallaxSampleTexture2D_ae45c5063979a430984f11ebf013c35c_float _ParallaxSampleTexture2D_c29d966c4be34c4bbdd1fe8a01531312;
                    _ParallaxSampleTexture2D_c29d966c4be34c4bbdd1fe8a01531312.ObjectSpaceViewDirection = IN.ObjectSpaceViewDirection;
                    _ParallaxSampleTexture2D_c29d966c4be34c4bbdd1fe8a01531312.uv0 = IN.uv0;
                    float _ParallaxSampleTexture2D_c29d966c4be34c4bbdd1fe8a01531312_OutR_2_Float;
                    float _ParallaxSampleTexture2D_c29d966c4be34c4bbdd1fe8a01531312_OutG_3_Float;
                    float _ParallaxSampleTexture2D_c29d966c4be34c4bbdd1fe8a01531312_OutB_1_Float;
                    SG_ParallaxSampleTexture2D_ae45c5063979a430984f11ebf013c35c_float(_Property_4dc7fd4f69e743aab88f1c4097303f71_Out_0_Texture2D, _Multiply_b2d5eac04cba4b1f887426a6bfc69bb5_Out_2_Vector2, _Property_6097c9c4024844b7a53ab897340bc2b0_Out_0_Boolean, _ParallaxSampleTexture2D_c29d966c4be34c4bbdd1fe8a01531312, _ParallaxSampleTexture2D_c29d966c4be34c4bbdd1fe8a01531312_OutR_2_Float, _ParallaxSampleTexture2D_c29d966c4be34c4bbdd1fe8a01531312_OutG_3_Float, _ParallaxSampleTexture2D_c29d966c4be34c4bbdd1fe8a01531312_OutB_1_Float);
                    float4 _Property_af6a9a8c31b845a6af3143281d2bf53b_Out_0_Vector4 = _Tongue_Color;
                    float4 _Multiply_e4efce35b80a4b2cafb8b9adf899cba8_Out_2_Vector4;
                    Unity_Multiply_float4_float4((_ParallaxSampleTexture2D_c29d966c4be34c4bbdd1fe8a01531312_OutB_1_Float.xxxx), _Property_af6a9a8c31b845a6af3143281d2bf53b_Out_0_Vector4, _Multiply_e4efce35b80a4b2cafb8b9adf899cba8_Out_2_Vector4);
                    float4 _Lerp_c14b8c38c754445685b381d8451856d3_Out_3_Vector4;
                    Unity_Lerp_float4(_Property_6a93ac1325de4cb79d98f6caea13fdd8_Out_0_Vector4, _Multiply_e4efce35b80a4b2cafb8b9adf899cba8_Out_2_Vector4, (_ParallaxSampleTexture2D_c29d966c4be34c4bbdd1fe8a01531312_OutB_1_Float.xxxx), _Lerp_c14b8c38c754445685b381d8451856d3_Out_3_Vector4);
                    float4 _Property_361f5fa06ce3451f93bb5d7380c2da3e_Out_0_Vector4 = _Teeth_Edge_Color;
                    UnityTexture2D _Property_532fa8ab3762489185678100eb2df2b3_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_Texture);
                    float _Property_37191fa8d35f47f2b1e681595f7e55f4_Out_0_Float = _Teeth_Depth;
                    float _Property_b3e5f83350924b82878b321ad1c8327f_Out_0_Float = _Teeth_Thickness;
                    float _Add_562c66b73f9a4fc59ae477a4c3b081fc_Out_2_Float;
                    Unity_Add_float(_Property_37191fa8d35f47f2b1e681595f7e55f4_Out_0_Float, _Property_b3e5f83350924b82878b321ad1c8327f_Out_0_Float, _Add_562c66b73f9a4fc59ae477a4c3b081fc_Out_2_Float);
                    Bindings_ParallaxSampleTexture2D_ae45c5063979a430984f11ebf013c35c_float _ParallaxSampleTexture2D_e67be38c032a40e087adc2e477807e7d;
                    _ParallaxSampleTexture2D_e67be38c032a40e087adc2e477807e7d.ObjectSpaceViewDirection = IN.ObjectSpaceViewDirection;
                    _ParallaxSampleTexture2D_e67be38c032a40e087adc2e477807e7d.uv0 = IN.uv0;
                    float _ParallaxSampleTexture2D_e67be38c032a40e087adc2e477807e7d_OutR_2_Float;
                    float _ParallaxSampleTexture2D_e67be38c032a40e087adc2e477807e7d_OutG_3_Float;
                    float _ParallaxSampleTexture2D_e67be38c032a40e087adc2e477807e7d_OutB_1_Float;
                    SG_ParallaxSampleTexture2D_ae45c5063979a430984f11ebf013c35c_float(_Property_532fa8ab3762489185678100eb2df2b3_Out_0_Texture2D, (_Add_562c66b73f9a4fc59ae477a4c3b081fc_Out_2_Float.xx), 0, _ParallaxSampleTexture2D_e67be38c032a40e087adc2e477807e7d, _ParallaxSampleTexture2D_e67be38c032a40e087adc2e477807e7d_OutR_2_Float, _ParallaxSampleTexture2D_e67be38c032a40e087adc2e477807e7d_OutG_3_Float, _ParallaxSampleTexture2D_e67be38c032a40e087adc2e477807e7d_OutB_1_Float);
                    float4 _Lerp_5f6226b27af847188fbc7109af235632_Out_3_Vector4;
                    Unity_Lerp_float4(_Lerp_c14b8c38c754445685b381d8451856d3_Out_3_Vector4, _Property_361f5fa06ce3451f93bb5d7380c2da3e_Out_0_Vector4, (_ParallaxSampleTexture2D_e67be38c032a40e087adc2e477807e7d_OutG_3_Float.xxxx), _Lerp_5f6226b27af847188fbc7109af235632_Out_3_Vector4);
                    float4 _Property_695a3e46fe5548aa87913f3d4c0ecf05_Out_0_Vector4 = _Teeth_Color;
                    UnityTexture2D _Property_b738848bca344d979f28a5e8709ce50a_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_Texture);
                    float _Property_6497ac1f7d7f451f98435734295f617c_Out_0_Float = _Teeth_Depth;
                    float _Property_d9e84b7e76784f3e8aa00bd8bfe14599_Out_0_Boolean = _Flip_X;
                    Bindings_ParallaxSampleTexture2D_ae45c5063979a430984f11ebf013c35c_float _ParallaxSampleTexture2D_26eb78f900f5474b9ded770a5d15b9c7;
                    _ParallaxSampleTexture2D_26eb78f900f5474b9ded770a5d15b9c7.ObjectSpaceViewDirection = IN.ObjectSpaceViewDirection;
                    _ParallaxSampleTexture2D_26eb78f900f5474b9ded770a5d15b9c7.uv0 = IN.uv0;
                    float _ParallaxSampleTexture2D_26eb78f900f5474b9ded770a5d15b9c7_OutR_2_Float;
                    float _ParallaxSampleTexture2D_26eb78f900f5474b9ded770a5d15b9c7_OutG_3_Float;
                    float _ParallaxSampleTexture2D_26eb78f900f5474b9ded770a5d15b9c7_OutB_1_Float;
                    SG_ParallaxSampleTexture2D_ae45c5063979a430984f11ebf013c35c_float(_Property_b738848bca344d979f28a5e8709ce50a_Out_0_Texture2D, (_Property_6497ac1f7d7f451f98435734295f617c_Out_0_Float.xx), _Property_d9e84b7e76784f3e8aa00bd8bfe14599_Out_0_Boolean, _ParallaxSampleTexture2D_26eb78f900f5474b9ded770a5d15b9c7, _ParallaxSampleTexture2D_26eb78f900f5474b9ded770a5d15b9c7_OutR_2_Float, _ParallaxSampleTexture2D_26eb78f900f5474b9ded770a5d15b9c7_OutG_3_Float, _ParallaxSampleTexture2D_26eb78f900f5474b9ded770a5d15b9c7_OutB_1_Float);
                    float4 _Lerp_3951173a3ad147bda8daa33a464ba7e4_Out_3_Vector4;
                    Unity_Lerp_float4(_Lerp_5f6226b27af847188fbc7109af235632_Out_3_Vector4, _Property_695a3e46fe5548aa87913f3d4c0ecf05_Out_0_Vector4, (_ParallaxSampleTexture2D_26eb78f900f5474b9ded770a5d15b9c7_OutG_3_Float.xxxx), _Lerp_3951173a3ad147bda8daa33a464ba7e4_Out_3_Vector4);
                    UnityTexture2D _Property_a67db6e8d526453c89a445ef89b619e3_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_Texture);
                    float _Property_764a5b6e97fb4e6cb52af22e014d2cb7_Out_0_Float = _Mouth_Depth;
                    float2 _Vector2_1dc9ddcac0504b7daf87eeaf74d1347b_Out_0_Vector2 = float2(float(1), float(1));
                    float2 _Multiply_4a47a872e85a4a4c8c8494908dbe7767_Out_2_Vector2;
                    Unity_Multiply_float2_float2((_Property_764a5b6e97fb4e6cb52af22e014d2cb7_Out_0_Float.xx), _Vector2_1dc9ddcac0504b7daf87eeaf74d1347b_Out_0_Vector2, _Multiply_4a47a872e85a4a4c8c8494908dbe7767_Out_2_Vector2);
                    float _Property_40b6c2c1ebd649ee85152cbb137d4219_Out_0_Boolean = _Flip_X;
                    Bindings_ParallaxSampleTexture2D_ae45c5063979a430984f11ebf013c35c_float _ParallaxSampleTexture2D_bb9439882a6f45b096dccfb671f40ade;
                    _ParallaxSampleTexture2D_bb9439882a6f45b096dccfb671f40ade.ObjectSpaceViewDirection = IN.ObjectSpaceViewDirection;
                    _ParallaxSampleTexture2D_bb9439882a6f45b096dccfb671f40ade.uv0 = IN.uv0;
                    float _ParallaxSampleTexture2D_bb9439882a6f45b096dccfb671f40ade_OutR_2_Float;
                    float _ParallaxSampleTexture2D_bb9439882a6f45b096dccfb671f40ade_OutG_3_Float;
                    float _ParallaxSampleTexture2D_bb9439882a6f45b096dccfb671f40ade_OutB_1_Float;
                    SG_ParallaxSampleTexture2D_ae45c5063979a430984f11ebf013c35c_float(_Property_a67db6e8d526453c89a445ef89b619e3_Out_0_Texture2D, _Multiply_4a47a872e85a4a4c8c8494908dbe7767_Out_2_Vector2, _Property_40b6c2c1ebd649ee85152cbb137d4219_Out_0_Boolean, _ParallaxSampleTexture2D_bb9439882a6f45b096dccfb671f40ade, _ParallaxSampleTexture2D_bb9439882a6f45b096dccfb671f40ade_OutR_2_Float, _ParallaxSampleTexture2D_bb9439882a6f45b096dccfb671f40ade_OutG_3_Float, _ParallaxSampleTexture2D_bb9439882a6f45b096dccfb671f40ade_OutB_1_Float);
                    float4 _Lerp_9412d0275e2b47429cb89b3b89798977_Out_3_Vector4;
                    Unity_Lerp_float4(_Property_c6e5cb5bdf714fe8a6ff8a4db984ddee_Out_0_Vector4, _Lerp_3951173a3ad147bda8daa33a464ba7e4_Out_3_Vector4, (_ParallaxSampleTexture2D_bb9439882a6f45b096dccfb671f40ade_OutR_2_Float.xxxx), _Lerp_9412d0275e2b47429cb89b3b89798977_Out_3_Vector4);
                    float3 _Branch_48ee12d07e0447a8a1b11405ef31386c_Out_3_Vector3;
                    Unity_Branch_float3(0, IN.ObjectSpaceViewDirection, (_Lerp_9412d0275e2b47429cb89b3b89798977_Out_3_Vector4.xyz), _Branch_48ee12d07e0447a8a1b11405ef31386c_Out_3_Vector3);
                    UnityTexture2D _Property_fc4d5cb8867142b7af1000f12a0b5617_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_Texture);
                    float4 _SampleTexture2D_39637566a9c84bb8aa66f76590d11702_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_fc4d5cb8867142b7af1000f12a0b5617_Out_0_Texture2D.tex, _Property_fc4d5cb8867142b7af1000f12a0b5617_Out_0_Texture2D.samplerstate, _Property_fc4d5cb8867142b7af1000f12a0b5617_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
                    float _SampleTexture2D_39637566a9c84bb8aa66f76590d11702_R_4_Float = _SampleTexture2D_39637566a9c84bb8aa66f76590d11702_RGBA_0_Vector4.r;
                    float _SampleTexture2D_39637566a9c84bb8aa66f76590d11702_G_5_Float = _SampleTexture2D_39637566a9c84bb8aa66f76590d11702_RGBA_0_Vector4.g;
                    float _SampleTexture2D_39637566a9c84bb8aa66f76590d11702_B_6_Float = _SampleTexture2D_39637566a9c84bb8aa66f76590d11702_RGBA_0_Vector4.b;
                    float _SampleTexture2D_39637566a9c84bb8aa66f76590d11702_A_7_Float = _SampleTexture2D_39637566a9c84bb8aa66f76590d11702_RGBA_0_Vector4.a;
                    surface.BaseColor = _Branch_48ee12d07e0447a8a1b11405ef31386c_Out_3_Vector3;
                    surface.NormalTS = IN.TangentSpaceNormal;
                    surface.Emission = float3(0, 0, 0);
                    surface.Metallic = float(0);
                    surface.Smoothness = float(0.1);
                    surface.Occlusion = float(1);
                    surface.Alpha = _SampleTexture2D_39637566a9c84bb8aa66f76590d11702_R_4_Float;
                    return surface;
                }
            
            // --------------------------------------------------
            // Build Graph Inputs
            #ifdef HAVE_VFX_MODIFICATION
            #define VFX_SRP_ATTRIBUTES Attributes
            #define VFX_SRP_VARYINGS Varyings
            #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
            #endif
            VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
                {
                    VertexDescriptionInputs output;
                    ZERO_INITIALIZE(VertexDescriptionInputs, output);
                
                    output.ObjectSpaceNormal =                          input.normalOS;
                    output.ObjectSpaceTangent =                         input.tangentOS.xyz;
                    output.ObjectSpacePosition =                        input.positionOS;
                #if UNITY_ANY_INSTANCING_ENABLED
                #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
                #endif
                
                    return output;
                }
                
            SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
                {
                    SurfaceDescriptionInputs output;
                    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
                
                #ifdef HAVE_VFX_MODIFICATION
                #if VFX_USE_GRAPH_VALUES
                    uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
                    /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
                #endif
                    /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
                
                #endif
                
                    
                
                
                
                    output.TangentSpaceNormal = float3(0.0f, 0.0f, 1.0f);
                
                
                    output.WorldSpaceViewDirection = GetWorldSpaceNormalizeViewDir(input.positionWS);
                    output.ObjectSpaceViewDirection = TransformWorldToObjectDir(output.WorldSpaceViewDirection);
                
                    #if UNITY_UV_STARTS_AT_TOP
                    #else
                    #endif
                
                
                    output.uv0 = input.texCoord0;
                #if UNITY_ANY_INSTANCING_ENABLED
                #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
                #endif
                #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
                #else
                #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
                #endif
                #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
                
                        return output;
                }
                
            
            // --------------------------------------------------
            // Main
            
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityGBuffer.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/PBRGBufferPass.hlsl"
            
            // --------------------------------------------------
            // Visual Effect Vertex Invocations
            #ifdef HAVE_VFX_MODIFICATION
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
            #endif
            
            ENDHLSL
            }
            Pass
            {
                Name "MotionVectors"
                Tags
                {
                    "LightMode" = "MotionVectors"
                }
            
            // Render State
            Cull Back
                ZTest LEqual
                ZWrite On
                ColorMask RG
            
            // Debug
            // <None>
            
            // --------------------------------------------------
            // Pass
            
            HLSLPROGRAM
            
            // Pragmas
            #pragma target 3.5
                #pragma multi_compile_instancing
                #pragma vertex vert
                #pragma fragment frag
            
            // Keywords
            // PassKeywords: <None>
            // GraphKeywords: <None>
            
            // Defines
            
            #define _NORMALMAP 1
            #define _NORMAL_DROPOFF_TS 1
            #define ATTRIBUTES_NEED_TEXCOORD0
            #define VARYINGS_NEED_TEXCOORD0
            #define FEATURES_GRAPH_VERTEX
            /* WARNING: $splice Could not find named fragment 'PassInstancing' */
            #define SHADERPASS SHADERPASS_MOTION_VECTORS
            
            
            // custom interpolator pre-include
            /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
            
            // Includes
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
            
            // --------------------------------------------------
            // Structs and Packing
            
            // custom interpolators pre packing
            /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
            
            struct Attributes
                {
                     float3 positionOS : POSITION;
                     float4 uv0 : TEXCOORD0;
                    #if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
                     uint instanceID : INSTANCEID_SEMANTIC;
                    #endif
                };
                struct Varyings
                {
                     float4 positionCS : SV_POSITION;
                     float4 texCoord0;
                    #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
                     uint instanceID : CUSTOM_INSTANCE_ID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                     uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                     uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                     FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
                    #endif
                };
                struct SurfaceDescriptionInputs
                {
                     float4 uv0;
                };
                struct VertexDescriptionInputs
                {
                     float3 ObjectSpacePosition;
                };
                struct PackedVaryings
                {
                     float4 positionCS : SV_POSITION;
                     float4 texCoord0 : INTERP0;
                    #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
                     uint instanceID : CUSTOM_INSTANCE_ID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                     uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                     uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                     FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
                    #endif
                };
            
            PackedVaryings PackVaryings (Varyings input)
                {
                    PackedVaryings output;
                    ZERO_INITIALIZE(PackedVaryings, output);
                    output.positionCS = input.positionCS;
                    output.texCoord0.xyzw = input.texCoord0;
                    #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
                    output.instanceID = input.instanceID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                    output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                    output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                    output.cullFace = input.cullFace;
                    #endif
                    return output;
                }
                
                Varyings UnpackVaryings (PackedVaryings input)
                {
                    Varyings output;
                    output.positionCS = input.positionCS;
                    output.texCoord0 = input.texCoord0.xyzw;
                    #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
                    output.instanceID = input.instanceID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                    output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                    output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                    output.cullFace = input.cullFace;
                    #endif
                    return output;
                }
                
            
            // --------------------------------------------------
            // Graph
            
            // Graph Properties
            CBUFFER_START(UnityPerMaterial)
                float _Teeth_Depth;
                float4 _Texture_TexelSize;
                float _Tongue_Depth;
                float4 _Mouth_Color;
                float4 _Tongue_Color;
                float4 _Teeth_Color;
                float4 _Teeth_Edge_Color;
                float _Teeth_Thickness;
                float _Mouth_Depth;
                float _Flip_X;
                UNITY_TEXTURE_STREAMING_DEBUG_VARS;
                CBUFFER_END
                
                
                // Object and Global properties
                SAMPLER(SamplerState_Linear_Repeat);
                TEXTURE2D(_Texture);
                SAMPLER(sampler_Texture);
            
            // Graph Includes
            // GraphIncludes: <None>
            
            // -- Property used by ScenePickingPass
            #ifdef SCENEPICKINGPASS
            float4 _SelectionID;
            #endif
            
            // -- Properties used by SceneSelectionPass
            #ifdef SCENESELECTIONPASS
            int _ObjectId;
            int _PassValue;
            #endif
            
            // Graph Functions
            // GraphFunctions: <None>
            
            // Custom interpolators pre vertex
            /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
            
            // Graph Vertex
            struct VertexDescription
                {
                    float3 Position;
                };
                
                VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
                {
                    VertexDescription description = (VertexDescription)0;
                    description.Position = IN.ObjectSpacePosition;
                    return description;
                }
            
            // Custom interpolators, pre surface
            #ifdef FEATURES_GRAPH_VERTEX
            Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
            {
            return output;
            }
            #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
            #endif
            
            // Graph Pixel
            struct SurfaceDescription
                {
                    float Alpha;
                };
                
                SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
                {
                    SurfaceDescription surface = (SurfaceDescription)0;
                    UnityTexture2D _Property_fc4d5cb8867142b7af1000f12a0b5617_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_Texture);
                    float4 _SampleTexture2D_39637566a9c84bb8aa66f76590d11702_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_fc4d5cb8867142b7af1000f12a0b5617_Out_0_Texture2D.tex, _Property_fc4d5cb8867142b7af1000f12a0b5617_Out_0_Texture2D.samplerstate, _Property_fc4d5cb8867142b7af1000f12a0b5617_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
                    float _SampleTexture2D_39637566a9c84bb8aa66f76590d11702_R_4_Float = _SampleTexture2D_39637566a9c84bb8aa66f76590d11702_RGBA_0_Vector4.r;
                    float _SampleTexture2D_39637566a9c84bb8aa66f76590d11702_G_5_Float = _SampleTexture2D_39637566a9c84bb8aa66f76590d11702_RGBA_0_Vector4.g;
                    float _SampleTexture2D_39637566a9c84bb8aa66f76590d11702_B_6_Float = _SampleTexture2D_39637566a9c84bb8aa66f76590d11702_RGBA_0_Vector4.b;
                    float _SampleTexture2D_39637566a9c84bb8aa66f76590d11702_A_7_Float = _SampleTexture2D_39637566a9c84bb8aa66f76590d11702_RGBA_0_Vector4.a;
                    surface.Alpha = _SampleTexture2D_39637566a9c84bb8aa66f76590d11702_R_4_Float;
                    return surface;
                }
            
            // --------------------------------------------------
            // Build Graph Inputs
            #ifdef HAVE_VFX_MODIFICATION
            #define VFX_SRP_ATTRIBUTES Attributes
            #define VFX_SRP_VARYINGS Varyings
            #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
            #endif
            VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
                {
                    VertexDescriptionInputs output;
                    ZERO_INITIALIZE(VertexDescriptionInputs, output);
                
                    output.ObjectSpacePosition =                        input.positionOS;
                #if UNITY_ANY_INSTANCING_ENABLED
                #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
                #endif
                
                    return output;
                }
                
            SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
                {
                    SurfaceDescriptionInputs output;
                    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
                
                #ifdef HAVE_VFX_MODIFICATION
                #if VFX_USE_GRAPH_VALUES
                    uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
                    /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
                #endif
                    /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
                
                #endif
                
                    
                
                
                
                
                
                
                    #if UNITY_UV_STARTS_AT_TOP
                    #else
                    #endif
                
                
                    output.uv0 = input.texCoord0;
                #if UNITY_ANY_INSTANCING_ENABLED
                #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
                #endif
                #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
                #else
                #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
                #endif
                #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
                
                        return output;
                }
                
            
            // --------------------------------------------------
            // Main
            
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/MotionVectorPass.hlsl"
            
            // --------------------------------------------------
            // Visual Effect Vertex Invocations
            #ifdef HAVE_VFX_MODIFICATION
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
            #endif
            
            ENDHLSL
            }
            Pass
            {
                Name "DepthNormals"
                Tags
                {
                    "LightMode" = "DepthNormals"
                }
            
            // Render State
            Cull Back
                ZTest LEqual
                ZWrite On
            
            // Debug
            // <None>
            
            // --------------------------------------------------
            // Pass
            
            HLSLPROGRAM
            
            // Pragmas
            #pragma target 2.0
                #pragma multi_compile_instancing
                #pragma vertex vert
                #pragma fragment frag
            
            // Keywords
            // PassKeywords: <None>
            // GraphKeywords: <None>
            
            // Defines
            
            #define _NORMALMAP 1
            #define _NORMAL_DROPOFF_TS 1
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define ATTRIBUTES_NEED_TEXCOORD0
            #define ATTRIBUTES_NEED_TEXCOORD1
            #define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
            #define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
            #define VARYINGS_NEED_NORMAL_WS
            #define VARYINGS_NEED_TANGENT_WS
            #define VARYINGS_NEED_TEXCOORD0
            #define FEATURES_GRAPH_VERTEX
            /* WARNING: $splice Could not find named fragment 'PassInstancing' */
            #define SHADERPASS SHADERPASS_DEPTHNORMALS
            
            
            // custom interpolator pre-include
            /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
            
            // Includes
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
            
            // --------------------------------------------------
            // Structs and Packing
            
            // custom interpolators pre packing
            /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
            
            struct Attributes
                {
                     float3 positionOS : POSITION;
                     float3 normalOS : NORMAL;
                     float4 tangentOS : TANGENT;
                     float4 uv0 : TEXCOORD0;
                     float4 uv1 : TEXCOORD1;
                    #if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
                     uint instanceID : INSTANCEID_SEMANTIC;
                    #endif
                };
                struct Varyings
                {
                     float4 positionCS : SV_POSITION;
                     float3 normalWS;
                     float4 tangentWS;
                     float4 texCoord0;
                    #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
                     uint instanceID : CUSTOM_INSTANCE_ID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                     uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                     uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                     FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
                    #endif
                };
                struct SurfaceDescriptionInputs
                {
                     float3 TangentSpaceNormal;
                     float4 uv0;
                };
                struct VertexDescriptionInputs
                {
                     float3 ObjectSpaceNormal;
                     float3 ObjectSpaceTangent;
                     float3 ObjectSpacePosition;
                };
                struct PackedVaryings
                {
                     float4 positionCS : SV_POSITION;
                     float4 tangentWS : INTERP0;
                     float4 texCoord0 : INTERP1;
                     float3 normalWS : INTERP2;
                    #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
                     uint instanceID : CUSTOM_INSTANCE_ID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                     uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                     uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                     FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
                    #endif
                };
            
            PackedVaryings PackVaryings (Varyings input)
                {
                    PackedVaryings output;
                    ZERO_INITIALIZE(PackedVaryings, output);
                    output.positionCS = input.positionCS;
                    output.tangentWS.xyzw = input.tangentWS;
                    output.texCoord0.xyzw = input.texCoord0;
                    output.normalWS.xyz = input.normalWS;
                    #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
                    output.instanceID = input.instanceID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                    output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                    output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                    output.cullFace = input.cullFace;
                    #endif
                    return output;
                }
                
                Varyings UnpackVaryings (PackedVaryings input)
                {
                    Varyings output;
                    output.positionCS = input.positionCS;
                    output.tangentWS = input.tangentWS.xyzw;
                    output.texCoord0 = input.texCoord0.xyzw;
                    output.normalWS = input.normalWS.xyz;
                    #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
                    output.instanceID = input.instanceID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                    output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                    output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                    output.cullFace = input.cullFace;
                    #endif
                    return output;
                }
                
            
            // --------------------------------------------------
            // Graph
            
            // Graph Properties
            CBUFFER_START(UnityPerMaterial)
                float _Teeth_Depth;
                float4 _Texture_TexelSize;
                float _Tongue_Depth;
                float4 _Mouth_Color;
                float4 _Tongue_Color;
                float4 _Teeth_Color;
                float4 _Teeth_Edge_Color;
                float _Teeth_Thickness;
                float _Mouth_Depth;
                float _Flip_X;
                UNITY_TEXTURE_STREAMING_DEBUG_VARS;
                CBUFFER_END
                
                
                // Object and Global properties
                SAMPLER(SamplerState_Linear_Repeat);
                TEXTURE2D(_Texture);
                SAMPLER(sampler_Texture);
            
            // Graph Includes
            // GraphIncludes: <None>
            
            // -- Property used by ScenePickingPass
            #ifdef SCENEPICKINGPASS
            float4 _SelectionID;
            #endif
            
            // -- Properties used by SceneSelectionPass
            #ifdef SCENESELECTIONPASS
            int _ObjectId;
            int _PassValue;
            #endif
            
            // Graph Functions
            // GraphFunctions: <None>
            
            // Custom interpolators pre vertex
            /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
            
            // Graph Vertex
            struct VertexDescription
                {
                    float3 Position;
                    float3 Normal;
                    float3 Tangent;
                };
                
                VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
                {
                    VertexDescription description = (VertexDescription)0;
                    description.Position = IN.ObjectSpacePosition;
                    description.Normal = IN.ObjectSpaceNormal;
                    description.Tangent = IN.ObjectSpaceTangent;
                    return description;
                }
            
            // Custom interpolators, pre surface
            #ifdef FEATURES_GRAPH_VERTEX
            Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
            {
            return output;
            }
            #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
            #endif
            
            // Graph Pixel
            struct SurfaceDescription
                {
                    float3 NormalTS;
                    float Alpha;
                };
                
                SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
                {
                    SurfaceDescription surface = (SurfaceDescription)0;
                    UnityTexture2D _Property_fc4d5cb8867142b7af1000f12a0b5617_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_Texture);
                    float4 _SampleTexture2D_39637566a9c84bb8aa66f76590d11702_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_fc4d5cb8867142b7af1000f12a0b5617_Out_0_Texture2D.tex, _Property_fc4d5cb8867142b7af1000f12a0b5617_Out_0_Texture2D.samplerstate, _Property_fc4d5cb8867142b7af1000f12a0b5617_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
                    float _SampleTexture2D_39637566a9c84bb8aa66f76590d11702_R_4_Float = _SampleTexture2D_39637566a9c84bb8aa66f76590d11702_RGBA_0_Vector4.r;
                    float _SampleTexture2D_39637566a9c84bb8aa66f76590d11702_G_5_Float = _SampleTexture2D_39637566a9c84bb8aa66f76590d11702_RGBA_0_Vector4.g;
                    float _SampleTexture2D_39637566a9c84bb8aa66f76590d11702_B_6_Float = _SampleTexture2D_39637566a9c84bb8aa66f76590d11702_RGBA_0_Vector4.b;
                    float _SampleTexture2D_39637566a9c84bb8aa66f76590d11702_A_7_Float = _SampleTexture2D_39637566a9c84bb8aa66f76590d11702_RGBA_0_Vector4.a;
                    surface.NormalTS = IN.TangentSpaceNormal;
                    surface.Alpha = _SampleTexture2D_39637566a9c84bb8aa66f76590d11702_R_4_Float;
                    return surface;
                }
            
            // --------------------------------------------------
            // Build Graph Inputs
            #ifdef HAVE_VFX_MODIFICATION
            #define VFX_SRP_ATTRIBUTES Attributes
            #define VFX_SRP_VARYINGS Varyings
            #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
            #endif
            VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
                {
                    VertexDescriptionInputs output;
                    ZERO_INITIALIZE(VertexDescriptionInputs, output);
                
                    output.ObjectSpaceNormal =                          input.normalOS;
                    output.ObjectSpaceTangent =                         input.tangentOS.xyz;
                    output.ObjectSpacePosition =                        input.positionOS;
                #if UNITY_ANY_INSTANCING_ENABLED
                #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
                #endif
                
                    return output;
                }
                
            SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
                {
                    SurfaceDescriptionInputs output;
                    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
                
                #ifdef HAVE_VFX_MODIFICATION
                #if VFX_USE_GRAPH_VALUES
                    uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
                    /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
                #endif
                    /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
                
                #endif
                
                    
                
                
                
                    output.TangentSpaceNormal = float3(0.0f, 0.0f, 1.0f);
                
                
                
                    #if UNITY_UV_STARTS_AT_TOP
                    #else
                    #endif
                
                
                    output.uv0 = input.texCoord0;
                #if UNITY_ANY_INSTANCING_ENABLED
                #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
                #endif
                #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
                #else
                #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
                #endif
                #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
                
                        return output;
                }
                
            
            // --------------------------------------------------
            // Main
            
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/DepthNormalsOnlyPass.hlsl"
            
            // --------------------------------------------------
            // Visual Effect Vertex Invocations
            #ifdef HAVE_VFX_MODIFICATION
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
            #endif
            
            ENDHLSL
            }
            Pass
            {
                Name "Meta"
                Tags
                {
                    "LightMode" = "Meta"
                }
            
            // Render State
            Cull Off
            
            // Debug
            // <None>
            
            // --------------------------------------------------
            // Pass
            
            HLSLPROGRAM
            
            // Pragmas
            #pragma target 2.0
                #pragma vertex vert
                #pragma fragment frag
            
            // Keywords
            #pragma shader_feature _ EDITOR_VISUALIZATION
            // GraphKeywords: <None>
            
            // Defines
            
            #define _NORMALMAP 1
            #define _NORMAL_DROPOFF_TS 1
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define ATTRIBUTES_NEED_TEXCOORD0
            #define ATTRIBUTES_NEED_TEXCOORD1
            #define ATTRIBUTES_NEED_TEXCOORD2
            #define ATTRIBUTES_NEED_INSTANCEID
            #define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
            #define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
            #define VARYINGS_NEED_POSITION_WS
            #define VARYINGS_NEED_TEXCOORD0
            #define VARYINGS_NEED_TEXCOORD1
            #define VARYINGS_NEED_TEXCOORD2
            #define FEATURES_GRAPH_VERTEX
            /* WARNING: $splice Could not find named fragment 'PassInstancing' */
            #define SHADERPASS SHADERPASS_META
                #define _FOG_FRAGMENT 1
            
            
            // custom interpolator pre-include
            /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
            
            // Includes
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
            
            // --------------------------------------------------
            // Structs and Packing
            
            // custom interpolators pre packing
            /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
            
            struct Attributes
                {
                     float3 positionOS : POSITION;
                     float3 normalOS : NORMAL;
                     float4 tangentOS : TANGENT;
                     float4 uv0 : TEXCOORD0;
                     float4 uv1 : TEXCOORD1;
                     float4 uv2 : TEXCOORD2;
                    #if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
                     uint instanceID : INSTANCEID_SEMANTIC;
                    #endif
                };
                struct Varyings
                {
                     float4 positionCS : SV_POSITION;
                     float3 positionWS;
                     float4 texCoord0;
                     float4 texCoord1;
                     float4 texCoord2;
                    #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
                     uint instanceID : CUSTOM_INSTANCE_ID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                     uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                     uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                     FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
                    #endif
                };
                struct SurfaceDescriptionInputs
                {
                     float3 ObjectSpaceViewDirection;
                     float3 WorldSpaceViewDirection;
                     float4 uv0;
                };
                struct VertexDescriptionInputs
                {
                     float3 ObjectSpaceNormal;
                     float3 ObjectSpaceTangent;
                     float3 ObjectSpacePosition;
                };
                struct PackedVaryings
                {
                     float4 positionCS : SV_POSITION;
                     float4 texCoord0 : INTERP0;
                     float4 texCoord1 : INTERP1;
                     float4 texCoord2 : INTERP2;
                     float3 positionWS : INTERP3;
                    #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
                     uint instanceID : CUSTOM_INSTANCE_ID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                     uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                     uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                     FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
                    #endif
                };
            
            PackedVaryings PackVaryings (Varyings input)
                {
                    PackedVaryings output;
                    ZERO_INITIALIZE(PackedVaryings, output);
                    output.positionCS = input.positionCS;
                    output.texCoord0.xyzw = input.texCoord0;
                    output.texCoord1.xyzw = input.texCoord1;
                    output.texCoord2.xyzw = input.texCoord2;
                    output.positionWS.xyz = input.positionWS;
                    #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
                    output.instanceID = input.instanceID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                    output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                    output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                    output.cullFace = input.cullFace;
                    #endif
                    return output;
                }
                
                Varyings UnpackVaryings (PackedVaryings input)
                {
                    Varyings output;
                    output.positionCS = input.positionCS;
                    output.texCoord0 = input.texCoord0.xyzw;
                    output.texCoord1 = input.texCoord1.xyzw;
                    output.texCoord2 = input.texCoord2.xyzw;
                    output.positionWS = input.positionWS.xyz;
                    #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
                    output.instanceID = input.instanceID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                    output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                    output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                    output.cullFace = input.cullFace;
                    #endif
                    return output;
                }
                
            
            // --------------------------------------------------
            // Graph
            
            // Graph Properties
            CBUFFER_START(UnityPerMaterial)
                float _Teeth_Depth;
                float4 _Texture_TexelSize;
                float _Tongue_Depth;
                float4 _Mouth_Color;
                float4 _Tongue_Color;
                float4 _Teeth_Color;
                float4 _Teeth_Edge_Color;
                float _Teeth_Thickness;
                float _Mouth_Depth;
                float _Flip_X;
                UNITY_TEXTURE_STREAMING_DEBUG_VARS;
                CBUFFER_END
                
                
                // Object and Global properties
                SAMPLER(SamplerState_Linear_Repeat);
                TEXTURE2D(_Texture);
                SAMPLER(sampler_Texture);
            
            // Graph Includes
            // GraphIncludes: <None>
            
            // -- Property used by ScenePickingPass
            #ifdef SCENEPICKINGPASS
            float4 _SelectionID;
            #endif
            
            // -- Properties used by SceneSelectionPass
            #ifdef SCENESELECTIONPASS
            int _ObjectId;
            int _PassValue;
            #endif
            
            // Graph Functions
            
                void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
                {
                    Out = A * B;
                }
                
                void Unity_Branch_float(float Predicate, float True, float False, out float Out)
                {
                    Out = Predicate ? True : False;
                }
                
                void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
                {
                    RGBA = float4(R, G, B, A);
                    RGB = float3(R, G, B);
                    RG = float2(R, G);
                }
                
                void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
                {
                Out = A * B;
                }
                
                void Unity_Negate_float2(float2 In, out float2 Out)
                {
                    Out = -1 * In;
                }
                
                // unity-custom-func-begin
                void ParallaxUV_float(float2 UV, float3 ViewDirection, float2 Depth, out float2 OutUV){
                OutUV = saturate(
                  UV  + (ViewDirection.xz
                  * Depth)
                );
                }
                // unity-custom-func-end
                
                struct Bindings_ParallaxSampleTexture2D_ae45c5063979a430984f11ebf013c35c_float
                {
                float3 ObjectSpaceViewDirection;
                half4 uv0;
                };
                
                void SG_ParallaxSampleTexture2D_ae45c5063979a430984f11ebf013c35c_float(UnityTexture2D _Texture, float2 _Depth, float _Flip_X, Bindings_ParallaxSampleTexture2D_ae45c5063979a430984f11ebf013c35c_float IN, out float Out_R_2, out float Out_G_3, out float Out_B_1)
                {
                UnityTexture2D _Property_dad3badd99624fdbab11879bd30d376a_Out_0_Texture2D = _Texture;
                float4 _UV_4592539f7377446791ce24767a34c92a_Out_0_Vector4 = IN.uv0;
                float _Property_eb437bd110674c94aa025481bbeb9168_Out_0_Boolean = _Flip_X;
                float _Branch_2db3155f235b4e9aad311b592b7680c6_Out_3_Float;
                Unity_Branch_float(_Property_eb437bd110674c94aa025481bbeb9168_Out_0_Boolean, float(-1), float(1), _Branch_2db3155f235b4e9aad311b592b7680c6_Out_3_Float);
                float4 _Combine_e831df117fad41e7a7aca27e3cb0828c_RGBA_4_Vector4;
                float3 _Combine_e831df117fad41e7a7aca27e3cb0828c_RGB_5_Vector3;
                float2 _Combine_e831df117fad41e7a7aca27e3cb0828c_RG_6_Vector2;
                Unity_Combine_float(_Branch_2db3155f235b4e9aad311b592b7680c6_Out_3_Float, float(1), float(1), float(1), _Combine_e831df117fad41e7a7aca27e3cb0828c_RGBA_4_Vector4, _Combine_e831df117fad41e7a7aca27e3cb0828c_RGB_5_Vector3, _Combine_e831df117fad41e7a7aca27e3cb0828c_RG_6_Vector2);
                float3 _Multiply_5b8a4ed7c54545d98ac880a3d084e129_Out_2_Vector3;
                Unity_Multiply_float3_float3(IN.ObjectSpaceViewDirection, _Combine_e831df117fad41e7a7aca27e3cb0828c_RGB_5_Vector3, _Multiply_5b8a4ed7c54545d98ac880a3d084e129_Out_2_Vector3);
                float2 _Property_db40aa4398dc42d2a16ff4e25da19fb8_Out_0_Vector2 = _Depth;
                float2 _Negate_9ba8a9d8334f438fb84d14622429bed5_Out_1_Vector2;
                Unity_Negate_float2(_Property_db40aa4398dc42d2a16ff4e25da19fb8_Out_0_Vector2, _Negate_9ba8a9d8334f438fb84d14622429bed5_Out_1_Vector2);
                float2 _ParallaxUVCustomFunction_d355531d1569421bb20205b595d4c55c_OutUV_3_Vector2;
                ParallaxUV_float((_UV_4592539f7377446791ce24767a34c92a_Out_0_Vector4.xy), _Multiply_5b8a4ed7c54545d98ac880a3d084e129_Out_2_Vector3, _Negate_9ba8a9d8334f438fb84d14622429bed5_Out_1_Vector2, _ParallaxUVCustomFunction_d355531d1569421bb20205b595d4c55c_OutUV_3_Vector2);
                float4 _SampleTexture2D_f5331c8e752f4566b67010e5d21b5a3b_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_dad3badd99624fdbab11879bd30d376a_Out_0_Texture2D.tex, _Property_dad3badd99624fdbab11879bd30d376a_Out_0_Texture2D.samplerstate, _Property_dad3badd99624fdbab11879bd30d376a_Out_0_Texture2D.GetTransformedUV(_ParallaxUVCustomFunction_d355531d1569421bb20205b595d4c55c_OutUV_3_Vector2) );
                float _SampleTexture2D_f5331c8e752f4566b67010e5d21b5a3b_R_4_Float = _SampleTexture2D_f5331c8e752f4566b67010e5d21b5a3b_RGBA_0_Vector4.r;
                float _SampleTexture2D_f5331c8e752f4566b67010e5d21b5a3b_G_5_Float = _SampleTexture2D_f5331c8e752f4566b67010e5d21b5a3b_RGBA_0_Vector4.g;
                float _SampleTexture2D_f5331c8e752f4566b67010e5d21b5a3b_B_6_Float = _SampleTexture2D_f5331c8e752f4566b67010e5d21b5a3b_RGBA_0_Vector4.b;
                float _SampleTexture2D_f5331c8e752f4566b67010e5d21b5a3b_A_7_Float = _SampleTexture2D_f5331c8e752f4566b67010e5d21b5a3b_RGBA_0_Vector4.a;
                Out_R_2 = _SampleTexture2D_f5331c8e752f4566b67010e5d21b5a3b_R_4_Float;
                Out_G_3 = _SampleTexture2D_f5331c8e752f4566b67010e5d21b5a3b_G_5_Float;
                Out_B_1 = _SampleTexture2D_f5331c8e752f4566b67010e5d21b5a3b_B_6_Float;
                }
                
                void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
                {
                    Out = A * B;
                }
                
                void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
                {
                    Out = lerp(A, B, T);
                }
                
                void Unity_Add_float(float A, float B, out float Out)
                {
                    Out = A + B;
                }
                
                void Unity_Branch_float3(float Predicate, float3 True, float3 False, out float3 Out)
                {
                    Out = Predicate ? True : False;
                }
            
            // Custom interpolators pre vertex
            /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
            
            // Graph Vertex
            struct VertexDescription
                {
                    float3 Position;
                    float3 Normal;
                    float3 Tangent;
                };
                
                VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
                {
                    VertexDescription description = (VertexDescription)0;
                    description.Position = IN.ObjectSpacePosition;
                    description.Normal = IN.ObjectSpaceNormal;
                    description.Tangent = IN.ObjectSpaceTangent;
                    return description;
                }
            
            // Custom interpolators, pre surface
            #ifdef FEATURES_GRAPH_VERTEX
            Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
            {
            return output;
            }
            #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
            #endif
            
            // Graph Pixel
            struct SurfaceDescription
                {
                    float3 BaseColor;
                    float3 Emission;
                    float Alpha;
                };
                
                SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
                {
                    SurfaceDescription surface = (SurfaceDescription)0;
                    float4 _Property_c6e5cb5bdf714fe8a6ff8a4db984ddee_Out_0_Vector4 = _Mouth_Color;
                    float4 _Property_6a93ac1325de4cb79d98f6caea13fdd8_Out_0_Vector4 = _Mouth_Color;
                    UnityTexture2D _Property_4dc7fd4f69e743aab88f1c4097303f71_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_Texture);
                    float _Property_e7251618caa74325ad70e40dd4b915cf_Out_0_Float = _Tongue_Depth;
                    float2 _Vector2_2e29f538f8a44455a027aee5898dae58_Out_0_Vector2 = float2(float(1), float(1));
                    float2 _Multiply_b2d5eac04cba4b1f887426a6bfc69bb5_Out_2_Vector2;
                    Unity_Multiply_float2_float2((_Property_e7251618caa74325ad70e40dd4b915cf_Out_0_Float.xx), _Vector2_2e29f538f8a44455a027aee5898dae58_Out_0_Vector2, _Multiply_b2d5eac04cba4b1f887426a6bfc69bb5_Out_2_Vector2);
                    float _Property_6097c9c4024844b7a53ab897340bc2b0_Out_0_Boolean = _Flip_X;
                    Bindings_ParallaxSampleTexture2D_ae45c5063979a430984f11ebf013c35c_float _ParallaxSampleTexture2D_c29d966c4be34c4bbdd1fe8a01531312;
                    _ParallaxSampleTexture2D_c29d966c4be34c4bbdd1fe8a01531312.ObjectSpaceViewDirection = IN.ObjectSpaceViewDirection;
                    _ParallaxSampleTexture2D_c29d966c4be34c4bbdd1fe8a01531312.uv0 = IN.uv0;
                    float _ParallaxSampleTexture2D_c29d966c4be34c4bbdd1fe8a01531312_OutR_2_Float;
                    float _ParallaxSampleTexture2D_c29d966c4be34c4bbdd1fe8a01531312_OutG_3_Float;
                    float _ParallaxSampleTexture2D_c29d966c4be34c4bbdd1fe8a01531312_OutB_1_Float;
                    SG_ParallaxSampleTexture2D_ae45c5063979a430984f11ebf013c35c_float(_Property_4dc7fd4f69e743aab88f1c4097303f71_Out_0_Texture2D, _Multiply_b2d5eac04cba4b1f887426a6bfc69bb5_Out_2_Vector2, _Property_6097c9c4024844b7a53ab897340bc2b0_Out_0_Boolean, _ParallaxSampleTexture2D_c29d966c4be34c4bbdd1fe8a01531312, _ParallaxSampleTexture2D_c29d966c4be34c4bbdd1fe8a01531312_OutR_2_Float, _ParallaxSampleTexture2D_c29d966c4be34c4bbdd1fe8a01531312_OutG_3_Float, _ParallaxSampleTexture2D_c29d966c4be34c4bbdd1fe8a01531312_OutB_1_Float);
                    float4 _Property_af6a9a8c31b845a6af3143281d2bf53b_Out_0_Vector4 = _Tongue_Color;
                    float4 _Multiply_e4efce35b80a4b2cafb8b9adf899cba8_Out_2_Vector4;
                    Unity_Multiply_float4_float4((_ParallaxSampleTexture2D_c29d966c4be34c4bbdd1fe8a01531312_OutB_1_Float.xxxx), _Property_af6a9a8c31b845a6af3143281d2bf53b_Out_0_Vector4, _Multiply_e4efce35b80a4b2cafb8b9adf899cba8_Out_2_Vector4);
                    float4 _Lerp_c14b8c38c754445685b381d8451856d3_Out_3_Vector4;
                    Unity_Lerp_float4(_Property_6a93ac1325de4cb79d98f6caea13fdd8_Out_0_Vector4, _Multiply_e4efce35b80a4b2cafb8b9adf899cba8_Out_2_Vector4, (_ParallaxSampleTexture2D_c29d966c4be34c4bbdd1fe8a01531312_OutB_1_Float.xxxx), _Lerp_c14b8c38c754445685b381d8451856d3_Out_3_Vector4);
                    float4 _Property_361f5fa06ce3451f93bb5d7380c2da3e_Out_0_Vector4 = _Teeth_Edge_Color;
                    UnityTexture2D _Property_532fa8ab3762489185678100eb2df2b3_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_Texture);
                    float _Property_37191fa8d35f47f2b1e681595f7e55f4_Out_0_Float = _Teeth_Depth;
                    float _Property_b3e5f83350924b82878b321ad1c8327f_Out_0_Float = _Teeth_Thickness;
                    float _Add_562c66b73f9a4fc59ae477a4c3b081fc_Out_2_Float;
                    Unity_Add_float(_Property_37191fa8d35f47f2b1e681595f7e55f4_Out_0_Float, _Property_b3e5f83350924b82878b321ad1c8327f_Out_0_Float, _Add_562c66b73f9a4fc59ae477a4c3b081fc_Out_2_Float);
                    Bindings_ParallaxSampleTexture2D_ae45c5063979a430984f11ebf013c35c_float _ParallaxSampleTexture2D_e67be38c032a40e087adc2e477807e7d;
                    _ParallaxSampleTexture2D_e67be38c032a40e087adc2e477807e7d.ObjectSpaceViewDirection = IN.ObjectSpaceViewDirection;
                    _ParallaxSampleTexture2D_e67be38c032a40e087adc2e477807e7d.uv0 = IN.uv0;
                    float _ParallaxSampleTexture2D_e67be38c032a40e087adc2e477807e7d_OutR_2_Float;
                    float _ParallaxSampleTexture2D_e67be38c032a40e087adc2e477807e7d_OutG_3_Float;
                    float _ParallaxSampleTexture2D_e67be38c032a40e087adc2e477807e7d_OutB_1_Float;
                    SG_ParallaxSampleTexture2D_ae45c5063979a430984f11ebf013c35c_float(_Property_532fa8ab3762489185678100eb2df2b3_Out_0_Texture2D, (_Add_562c66b73f9a4fc59ae477a4c3b081fc_Out_2_Float.xx), 0, _ParallaxSampleTexture2D_e67be38c032a40e087adc2e477807e7d, _ParallaxSampleTexture2D_e67be38c032a40e087adc2e477807e7d_OutR_2_Float, _ParallaxSampleTexture2D_e67be38c032a40e087adc2e477807e7d_OutG_3_Float, _ParallaxSampleTexture2D_e67be38c032a40e087adc2e477807e7d_OutB_1_Float);
                    float4 _Lerp_5f6226b27af847188fbc7109af235632_Out_3_Vector4;
                    Unity_Lerp_float4(_Lerp_c14b8c38c754445685b381d8451856d3_Out_3_Vector4, _Property_361f5fa06ce3451f93bb5d7380c2da3e_Out_0_Vector4, (_ParallaxSampleTexture2D_e67be38c032a40e087adc2e477807e7d_OutG_3_Float.xxxx), _Lerp_5f6226b27af847188fbc7109af235632_Out_3_Vector4);
                    float4 _Property_695a3e46fe5548aa87913f3d4c0ecf05_Out_0_Vector4 = _Teeth_Color;
                    UnityTexture2D _Property_b738848bca344d979f28a5e8709ce50a_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_Texture);
                    float _Property_6497ac1f7d7f451f98435734295f617c_Out_0_Float = _Teeth_Depth;
                    float _Property_d9e84b7e76784f3e8aa00bd8bfe14599_Out_0_Boolean = _Flip_X;
                    Bindings_ParallaxSampleTexture2D_ae45c5063979a430984f11ebf013c35c_float _ParallaxSampleTexture2D_26eb78f900f5474b9ded770a5d15b9c7;
                    _ParallaxSampleTexture2D_26eb78f900f5474b9ded770a5d15b9c7.ObjectSpaceViewDirection = IN.ObjectSpaceViewDirection;
                    _ParallaxSampleTexture2D_26eb78f900f5474b9ded770a5d15b9c7.uv0 = IN.uv0;
                    float _ParallaxSampleTexture2D_26eb78f900f5474b9ded770a5d15b9c7_OutR_2_Float;
                    float _ParallaxSampleTexture2D_26eb78f900f5474b9ded770a5d15b9c7_OutG_3_Float;
                    float _ParallaxSampleTexture2D_26eb78f900f5474b9ded770a5d15b9c7_OutB_1_Float;
                    SG_ParallaxSampleTexture2D_ae45c5063979a430984f11ebf013c35c_float(_Property_b738848bca344d979f28a5e8709ce50a_Out_0_Texture2D, (_Property_6497ac1f7d7f451f98435734295f617c_Out_0_Float.xx), _Property_d9e84b7e76784f3e8aa00bd8bfe14599_Out_0_Boolean, _ParallaxSampleTexture2D_26eb78f900f5474b9ded770a5d15b9c7, _ParallaxSampleTexture2D_26eb78f900f5474b9ded770a5d15b9c7_OutR_2_Float, _ParallaxSampleTexture2D_26eb78f900f5474b9ded770a5d15b9c7_OutG_3_Float, _ParallaxSampleTexture2D_26eb78f900f5474b9ded770a5d15b9c7_OutB_1_Float);
                    float4 _Lerp_3951173a3ad147bda8daa33a464ba7e4_Out_3_Vector4;
                    Unity_Lerp_float4(_Lerp_5f6226b27af847188fbc7109af235632_Out_3_Vector4, _Property_695a3e46fe5548aa87913f3d4c0ecf05_Out_0_Vector4, (_ParallaxSampleTexture2D_26eb78f900f5474b9ded770a5d15b9c7_OutG_3_Float.xxxx), _Lerp_3951173a3ad147bda8daa33a464ba7e4_Out_3_Vector4);
                    UnityTexture2D _Property_a67db6e8d526453c89a445ef89b619e3_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_Texture);
                    float _Property_764a5b6e97fb4e6cb52af22e014d2cb7_Out_0_Float = _Mouth_Depth;
                    float2 _Vector2_1dc9ddcac0504b7daf87eeaf74d1347b_Out_0_Vector2 = float2(float(1), float(1));
                    float2 _Multiply_4a47a872e85a4a4c8c8494908dbe7767_Out_2_Vector2;
                    Unity_Multiply_float2_float2((_Property_764a5b6e97fb4e6cb52af22e014d2cb7_Out_0_Float.xx), _Vector2_1dc9ddcac0504b7daf87eeaf74d1347b_Out_0_Vector2, _Multiply_4a47a872e85a4a4c8c8494908dbe7767_Out_2_Vector2);
                    float _Property_40b6c2c1ebd649ee85152cbb137d4219_Out_0_Boolean = _Flip_X;
                    Bindings_ParallaxSampleTexture2D_ae45c5063979a430984f11ebf013c35c_float _ParallaxSampleTexture2D_bb9439882a6f45b096dccfb671f40ade;
                    _ParallaxSampleTexture2D_bb9439882a6f45b096dccfb671f40ade.ObjectSpaceViewDirection = IN.ObjectSpaceViewDirection;
                    _ParallaxSampleTexture2D_bb9439882a6f45b096dccfb671f40ade.uv0 = IN.uv0;
                    float _ParallaxSampleTexture2D_bb9439882a6f45b096dccfb671f40ade_OutR_2_Float;
                    float _ParallaxSampleTexture2D_bb9439882a6f45b096dccfb671f40ade_OutG_3_Float;
                    float _ParallaxSampleTexture2D_bb9439882a6f45b096dccfb671f40ade_OutB_1_Float;
                    SG_ParallaxSampleTexture2D_ae45c5063979a430984f11ebf013c35c_float(_Property_a67db6e8d526453c89a445ef89b619e3_Out_0_Texture2D, _Multiply_4a47a872e85a4a4c8c8494908dbe7767_Out_2_Vector2, _Property_40b6c2c1ebd649ee85152cbb137d4219_Out_0_Boolean, _ParallaxSampleTexture2D_bb9439882a6f45b096dccfb671f40ade, _ParallaxSampleTexture2D_bb9439882a6f45b096dccfb671f40ade_OutR_2_Float, _ParallaxSampleTexture2D_bb9439882a6f45b096dccfb671f40ade_OutG_3_Float, _ParallaxSampleTexture2D_bb9439882a6f45b096dccfb671f40ade_OutB_1_Float);
                    float4 _Lerp_9412d0275e2b47429cb89b3b89798977_Out_3_Vector4;
                    Unity_Lerp_float4(_Property_c6e5cb5bdf714fe8a6ff8a4db984ddee_Out_0_Vector4, _Lerp_3951173a3ad147bda8daa33a464ba7e4_Out_3_Vector4, (_ParallaxSampleTexture2D_bb9439882a6f45b096dccfb671f40ade_OutR_2_Float.xxxx), _Lerp_9412d0275e2b47429cb89b3b89798977_Out_3_Vector4);
                    float3 _Branch_48ee12d07e0447a8a1b11405ef31386c_Out_3_Vector3;
                    Unity_Branch_float3(0, IN.ObjectSpaceViewDirection, (_Lerp_9412d0275e2b47429cb89b3b89798977_Out_3_Vector4.xyz), _Branch_48ee12d07e0447a8a1b11405ef31386c_Out_3_Vector3);
                    UnityTexture2D _Property_fc4d5cb8867142b7af1000f12a0b5617_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_Texture);
                    float4 _SampleTexture2D_39637566a9c84bb8aa66f76590d11702_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_fc4d5cb8867142b7af1000f12a0b5617_Out_0_Texture2D.tex, _Property_fc4d5cb8867142b7af1000f12a0b5617_Out_0_Texture2D.samplerstate, _Property_fc4d5cb8867142b7af1000f12a0b5617_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
                    float _SampleTexture2D_39637566a9c84bb8aa66f76590d11702_R_4_Float = _SampleTexture2D_39637566a9c84bb8aa66f76590d11702_RGBA_0_Vector4.r;
                    float _SampleTexture2D_39637566a9c84bb8aa66f76590d11702_G_5_Float = _SampleTexture2D_39637566a9c84bb8aa66f76590d11702_RGBA_0_Vector4.g;
                    float _SampleTexture2D_39637566a9c84bb8aa66f76590d11702_B_6_Float = _SampleTexture2D_39637566a9c84bb8aa66f76590d11702_RGBA_0_Vector4.b;
                    float _SampleTexture2D_39637566a9c84bb8aa66f76590d11702_A_7_Float = _SampleTexture2D_39637566a9c84bb8aa66f76590d11702_RGBA_0_Vector4.a;
                    surface.BaseColor = _Branch_48ee12d07e0447a8a1b11405ef31386c_Out_3_Vector3;
                    surface.Emission = float3(0, 0, 0);
                    surface.Alpha = _SampleTexture2D_39637566a9c84bb8aa66f76590d11702_R_4_Float;
                    return surface;
                }
            
            // --------------------------------------------------
            // Build Graph Inputs
            #ifdef HAVE_VFX_MODIFICATION
            #define VFX_SRP_ATTRIBUTES Attributes
            #define VFX_SRP_VARYINGS Varyings
            #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
            #endif
            VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
                {
                    VertexDescriptionInputs output;
                    ZERO_INITIALIZE(VertexDescriptionInputs, output);
                
                    output.ObjectSpaceNormal =                          input.normalOS;
                    output.ObjectSpaceTangent =                         input.tangentOS.xyz;
                    output.ObjectSpacePosition =                        input.positionOS;
                #if UNITY_ANY_INSTANCING_ENABLED
                #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
                #endif
                
                    return output;
                }
                
            SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
                {
                    SurfaceDescriptionInputs output;
                    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
                
                #ifdef HAVE_VFX_MODIFICATION
                #if VFX_USE_GRAPH_VALUES
                    uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
                    /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
                #endif
                    /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
                
                #endif
                
                    
                
                
                
                
                
                    output.WorldSpaceViewDirection = GetWorldSpaceNormalizeViewDir(input.positionWS);
                    output.ObjectSpaceViewDirection = TransformWorldToObjectDir(output.WorldSpaceViewDirection);
                
                    #if UNITY_UV_STARTS_AT_TOP
                    #else
                    #endif
                
                
                    output.uv0 = input.texCoord0;
                #if UNITY_ANY_INSTANCING_ENABLED
                #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
                #endif
                #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
                #else
                #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
                #endif
                #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
                
                        return output;
                }
                
            
            // --------------------------------------------------
            // Main
            
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/LightingMetaPass.hlsl"
            
            // --------------------------------------------------
            // Visual Effect Vertex Invocations
            #ifdef HAVE_VFX_MODIFICATION
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
            #endif
            
            ENDHLSL
            }
            Pass
            {
                Name "SceneSelectionPass"
                Tags
                {
                    "LightMode" = "SceneSelectionPass"
                }
            
            // Render State
            Cull Off
            
            // Debug
            // <None>
            
            // --------------------------------------------------
            // Pass
            
            HLSLPROGRAM
            
            // Pragmas
            #pragma target 2.0
                #pragma vertex vert
                #pragma fragment frag
            
            // Keywords
            // PassKeywords: <None>
            // GraphKeywords: <None>
            
            // Defines
            
            #define _NORMALMAP 1
            #define _NORMAL_DROPOFF_TS 1
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define ATTRIBUTES_NEED_TEXCOORD0
            #define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
            #define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
            #define VARYINGS_NEED_TEXCOORD0
            #define FEATURES_GRAPH_VERTEX
            /* WARNING: $splice Could not find named fragment 'PassInstancing' */
            #define SHADERPASS SHADERPASS_DEPTHONLY
                #define SCENESELECTIONPASS 1
                #define ALPHA_CLIP_THRESHOLD 1
            
            
            // custom interpolator pre-include
            /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
            
            // Includes
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
            
            // --------------------------------------------------
            // Structs and Packing
            
            // custom interpolators pre packing
            /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
            
            struct Attributes
                {
                     float3 positionOS : POSITION;
                     float3 normalOS : NORMAL;
                     float4 tangentOS : TANGENT;
                     float4 uv0 : TEXCOORD0;
                    #if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
                     uint instanceID : INSTANCEID_SEMANTIC;
                    #endif
                };
                struct Varyings
                {
                     float4 positionCS : SV_POSITION;
                     float4 texCoord0;
                    #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
                     uint instanceID : CUSTOM_INSTANCE_ID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                     uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                     uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                     FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
                    #endif
                };
                struct SurfaceDescriptionInputs
                {
                     float4 uv0;
                };
                struct VertexDescriptionInputs
                {
                     float3 ObjectSpaceNormal;
                     float3 ObjectSpaceTangent;
                     float3 ObjectSpacePosition;
                };
                struct PackedVaryings
                {
                     float4 positionCS : SV_POSITION;
                     float4 texCoord0 : INTERP0;
                    #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
                     uint instanceID : CUSTOM_INSTANCE_ID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                     uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                     uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                     FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
                    #endif
                };
            
            PackedVaryings PackVaryings (Varyings input)
                {
                    PackedVaryings output;
                    ZERO_INITIALIZE(PackedVaryings, output);
                    output.positionCS = input.positionCS;
                    output.texCoord0.xyzw = input.texCoord0;
                    #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
                    output.instanceID = input.instanceID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                    output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                    output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                    output.cullFace = input.cullFace;
                    #endif
                    return output;
                }
                
                Varyings UnpackVaryings (PackedVaryings input)
                {
                    Varyings output;
                    output.positionCS = input.positionCS;
                    output.texCoord0 = input.texCoord0.xyzw;
                    #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
                    output.instanceID = input.instanceID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                    output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                    output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                    output.cullFace = input.cullFace;
                    #endif
                    return output;
                }
                
            
            // --------------------------------------------------
            // Graph
            
            // Graph Properties
            CBUFFER_START(UnityPerMaterial)
                float _Teeth_Depth;
                float4 _Texture_TexelSize;
                float _Tongue_Depth;
                float4 _Mouth_Color;
                float4 _Tongue_Color;
                float4 _Teeth_Color;
                float4 _Teeth_Edge_Color;
                float _Teeth_Thickness;
                float _Mouth_Depth;
                float _Flip_X;
                UNITY_TEXTURE_STREAMING_DEBUG_VARS;
                CBUFFER_END
                
                
                // Object and Global properties
                SAMPLER(SamplerState_Linear_Repeat);
                TEXTURE2D(_Texture);
                SAMPLER(sampler_Texture);
            
            // Graph Includes
            // GraphIncludes: <None>
            
            // -- Property used by ScenePickingPass
            #ifdef SCENEPICKINGPASS
            float4 _SelectionID;
            #endif
            
            // -- Properties used by SceneSelectionPass
            #ifdef SCENESELECTIONPASS
            int _ObjectId;
            int _PassValue;
            #endif
            
            // Graph Functions
            // GraphFunctions: <None>
            
            // Custom interpolators pre vertex
            /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
            
            // Graph Vertex
            struct VertexDescription
                {
                    float3 Position;
                    float3 Normal;
                    float3 Tangent;
                };
                
                VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
                {
                    VertexDescription description = (VertexDescription)0;
                    description.Position = IN.ObjectSpacePosition;
                    description.Normal = IN.ObjectSpaceNormal;
                    description.Tangent = IN.ObjectSpaceTangent;
                    return description;
                }
            
            // Custom interpolators, pre surface
            #ifdef FEATURES_GRAPH_VERTEX
            Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
            {
            return output;
            }
            #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
            #endif
            
            // Graph Pixel
            struct SurfaceDescription
                {
                    float Alpha;
                };
                
                SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
                {
                    SurfaceDescription surface = (SurfaceDescription)0;
                    UnityTexture2D _Property_fc4d5cb8867142b7af1000f12a0b5617_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_Texture);
                    float4 _SampleTexture2D_39637566a9c84bb8aa66f76590d11702_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_fc4d5cb8867142b7af1000f12a0b5617_Out_0_Texture2D.tex, _Property_fc4d5cb8867142b7af1000f12a0b5617_Out_0_Texture2D.samplerstate, _Property_fc4d5cb8867142b7af1000f12a0b5617_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
                    float _SampleTexture2D_39637566a9c84bb8aa66f76590d11702_R_4_Float = _SampleTexture2D_39637566a9c84bb8aa66f76590d11702_RGBA_0_Vector4.r;
                    float _SampleTexture2D_39637566a9c84bb8aa66f76590d11702_G_5_Float = _SampleTexture2D_39637566a9c84bb8aa66f76590d11702_RGBA_0_Vector4.g;
                    float _SampleTexture2D_39637566a9c84bb8aa66f76590d11702_B_6_Float = _SampleTexture2D_39637566a9c84bb8aa66f76590d11702_RGBA_0_Vector4.b;
                    float _SampleTexture2D_39637566a9c84bb8aa66f76590d11702_A_7_Float = _SampleTexture2D_39637566a9c84bb8aa66f76590d11702_RGBA_0_Vector4.a;
                    surface.Alpha = _SampleTexture2D_39637566a9c84bb8aa66f76590d11702_R_4_Float;
                    return surface;
                }
            
            // --------------------------------------------------
            // Build Graph Inputs
            #ifdef HAVE_VFX_MODIFICATION
            #define VFX_SRP_ATTRIBUTES Attributes
            #define VFX_SRP_VARYINGS Varyings
            #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
            #endif
            VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
                {
                    VertexDescriptionInputs output;
                    ZERO_INITIALIZE(VertexDescriptionInputs, output);
                
                    output.ObjectSpaceNormal =                          input.normalOS;
                    output.ObjectSpaceTangent =                         input.tangentOS.xyz;
                    output.ObjectSpacePosition =                        input.positionOS;
                #if UNITY_ANY_INSTANCING_ENABLED
                #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
                #endif
                
                    return output;
                }
                
            SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
                {
                    SurfaceDescriptionInputs output;
                    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
                
                #ifdef HAVE_VFX_MODIFICATION
                #if VFX_USE_GRAPH_VALUES
                    uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
                    /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
                #endif
                    /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
                
                #endif
                
                    
                
                
                
                
                
                
                    #if UNITY_UV_STARTS_AT_TOP
                    #else
                    #endif
                
                
                    output.uv0 = input.texCoord0;
                #if UNITY_ANY_INSTANCING_ENABLED
                #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
                #endif
                #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
                #else
                #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
                #endif
                #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
                
                        return output;
                }
                
            
            // --------------------------------------------------
            // Main
            
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/SelectionPickingPass.hlsl"
            
            // --------------------------------------------------
            // Visual Effect Vertex Invocations
            #ifdef HAVE_VFX_MODIFICATION
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
            #endif
            
            ENDHLSL
            }
            Pass
            {
                Name "ScenePickingPass"
                Tags
                {
                    "LightMode" = "Picking"
                }
            
            // Render State
            Cull Back
            
            // Debug
            // <None>
            
            // --------------------------------------------------
            // Pass
            
            HLSLPROGRAM
            
            // Pragmas
            #pragma target 2.0
                #pragma vertex vert
                #pragma fragment frag
            
            // Keywords
            // PassKeywords: <None>
            // GraphKeywords: <None>
            
            // Defines
            
            #define _NORMALMAP 1
            #define _NORMAL_DROPOFF_TS 1
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define ATTRIBUTES_NEED_TEXCOORD0
            #define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
            #define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
            #define VARYINGS_NEED_POSITION_WS
            #define VARYINGS_NEED_TEXCOORD0
            #define FEATURES_GRAPH_VERTEX
            /* WARNING: $splice Could not find named fragment 'PassInstancing' */
            #define SHADERPASS SHADERPASS_DEPTHONLY
                #define SCENEPICKINGPASS 1
                #define ALPHA_CLIP_THRESHOLD 1
            
            
            // custom interpolator pre-include
            /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
            
            // Includes
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
            
            // --------------------------------------------------
            // Structs and Packing
            
            // custom interpolators pre packing
            /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
            
            struct Attributes
                {
                     float3 positionOS : POSITION;
                     float3 normalOS : NORMAL;
                     float4 tangentOS : TANGENT;
                     float4 uv0 : TEXCOORD0;
                    #if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
                     uint instanceID : INSTANCEID_SEMANTIC;
                    #endif
                };
                struct Varyings
                {
                     float4 positionCS : SV_POSITION;
                     float3 positionWS;
                     float4 texCoord0;
                    #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
                     uint instanceID : CUSTOM_INSTANCE_ID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                     uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                     uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                     FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
                    #endif
                };
                struct SurfaceDescriptionInputs
                {
                     float3 ObjectSpaceViewDirection;
                     float3 WorldSpaceViewDirection;
                     float4 uv0;
                };
                struct VertexDescriptionInputs
                {
                     float3 ObjectSpaceNormal;
                     float3 ObjectSpaceTangent;
                     float3 ObjectSpacePosition;
                };
                struct PackedVaryings
                {
                     float4 positionCS : SV_POSITION;
                     float4 texCoord0 : INTERP0;
                     float3 positionWS : INTERP1;
                    #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
                     uint instanceID : CUSTOM_INSTANCE_ID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                     uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                     uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                     FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
                    #endif
                };
            
            PackedVaryings PackVaryings (Varyings input)
                {
                    PackedVaryings output;
                    ZERO_INITIALIZE(PackedVaryings, output);
                    output.positionCS = input.positionCS;
                    output.texCoord0.xyzw = input.texCoord0;
                    output.positionWS.xyz = input.positionWS;
                    #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
                    output.instanceID = input.instanceID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                    output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                    output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                    output.cullFace = input.cullFace;
                    #endif
                    return output;
                }
                
                Varyings UnpackVaryings (PackedVaryings input)
                {
                    Varyings output;
                    output.positionCS = input.positionCS;
                    output.texCoord0 = input.texCoord0.xyzw;
                    output.positionWS = input.positionWS.xyz;
                    #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
                    output.instanceID = input.instanceID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                    output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                    output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                    output.cullFace = input.cullFace;
                    #endif
                    return output;
                }
                
            
            // --------------------------------------------------
            // Graph
            
            // Graph Properties
            CBUFFER_START(UnityPerMaterial)
                float _Teeth_Depth;
                float4 _Texture_TexelSize;
                float _Tongue_Depth;
                float4 _Mouth_Color;
                float4 _Tongue_Color;
                float4 _Teeth_Color;
                float4 _Teeth_Edge_Color;
                float _Teeth_Thickness;
                float _Mouth_Depth;
                float _Flip_X;
                UNITY_TEXTURE_STREAMING_DEBUG_VARS;
                CBUFFER_END
                
                
                // Object and Global properties
                SAMPLER(SamplerState_Linear_Repeat);
                TEXTURE2D(_Texture);
                SAMPLER(sampler_Texture);
            
            // Graph Includes
            // GraphIncludes: <None>
            
            // -- Property used by ScenePickingPass
            #ifdef SCENEPICKINGPASS
            float4 _SelectionID;
            #endif
            
            // -- Properties used by SceneSelectionPass
            #ifdef SCENESELECTIONPASS
            int _ObjectId;
            int _PassValue;
            #endif
            
            // Graph Functions
            
                void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
                {
                    Out = A * B;
                }
                
                void Unity_Branch_float(float Predicate, float True, float False, out float Out)
                {
                    Out = Predicate ? True : False;
                }
                
                void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
                {
                    RGBA = float4(R, G, B, A);
                    RGB = float3(R, G, B);
                    RG = float2(R, G);
                }
                
                void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
                {
                Out = A * B;
                }
                
                void Unity_Negate_float2(float2 In, out float2 Out)
                {
                    Out = -1 * In;
                }
                
                // unity-custom-func-begin
                void ParallaxUV_float(float2 UV, float3 ViewDirection, float2 Depth, out float2 OutUV){
                OutUV = saturate(
                  UV  + (ViewDirection.xz
                  * Depth)
                );
                }
                // unity-custom-func-end
                
                struct Bindings_ParallaxSampleTexture2D_ae45c5063979a430984f11ebf013c35c_float
                {
                float3 ObjectSpaceViewDirection;
                half4 uv0;
                };
                
                void SG_ParallaxSampleTexture2D_ae45c5063979a430984f11ebf013c35c_float(UnityTexture2D _Texture, float2 _Depth, float _Flip_X, Bindings_ParallaxSampleTexture2D_ae45c5063979a430984f11ebf013c35c_float IN, out float Out_R_2, out float Out_G_3, out float Out_B_1)
                {
                UnityTexture2D _Property_dad3badd99624fdbab11879bd30d376a_Out_0_Texture2D = _Texture;
                float4 _UV_4592539f7377446791ce24767a34c92a_Out_0_Vector4 = IN.uv0;
                float _Property_eb437bd110674c94aa025481bbeb9168_Out_0_Boolean = _Flip_X;
                float _Branch_2db3155f235b4e9aad311b592b7680c6_Out_3_Float;
                Unity_Branch_float(_Property_eb437bd110674c94aa025481bbeb9168_Out_0_Boolean, float(-1), float(1), _Branch_2db3155f235b4e9aad311b592b7680c6_Out_3_Float);
                float4 _Combine_e831df117fad41e7a7aca27e3cb0828c_RGBA_4_Vector4;
                float3 _Combine_e831df117fad41e7a7aca27e3cb0828c_RGB_5_Vector3;
                float2 _Combine_e831df117fad41e7a7aca27e3cb0828c_RG_6_Vector2;
                Unity_Combine_float(_Branch_2db3155f235b4e9aad311b592b7680c6_Out_3_Float, float(1), float(1), float(1), _Combine_e831df117fad41e7a7aca27e3cb0828c_RGBA_4_Vector4, _Combine_e831df117fad41e7a7aca27e3cb0828c_RGB_5_Vector3, _Combine_e831df117fad41e7a7aca27e3cb0828c_RG_6_Vector2);
                float3 _Multiply_5b8a4ed7c54545d98ac880a3d084e129_Out_2_Vector3;
                Unity_Multiply_float3_float3(IN.ObjectSpaceViewDirection, _Combine_e831df117fad41e7a7aca27e3cb0828c_RGB_5_Vector3, _Multiply_5b8a4ed7c54545d98ac880a3d084e129_Out_2_Vector3);
                float2 _Property_db40aa4398dc42d2a16ff4e25da19fb8_Out_0_Vector2 = _Depth;
                float2 _Negate_9ba8a9d8334f438fb84d14622429bed5_Out_1_Vector2;
                Unity_Negate_float2(_Property_db40aa4398dc42d2a16ff4e25da19fb8_Out_0_Vector2, _Negate_9ba8a9d8334f438fb84d14622429bed5_Out_1_Vector2);
                float2 _ParallaxUVCustomFunction_d355531d1569421bb20205b595d4c55c_OutUV_3_Vector2;
                ParallaxUV_float((_UV_4592539f7377446791ce24767a34c92a_Out_0_Vector4.xy), _Multiply_5b8a4ed7c54545d98ac880a3d084e129_Out_2_Vector3, _Negate_9ba8a9d8334f438fb84d14622429bed5_Out_1_Vector2, _ParallaxUVCustomFunction_d355531d1569421bb20205b595d4c55c_OutUV_3_Vector2);
                float4 _SampleTexture2D_f5331c8e752f4566b67010e5d21b5a3b_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_dad3badd99624fdbab11879bd30d376a_Out_0_Texture2D.tex, _Property_dad3badd99624fdbab11879bd30d376a_Out_0_Texture2D.samplerstate, _Property_dad3badd99624fdbab11879bd30d376a_Out_0_Texture2D.GetTransformedUV(_ParallaxUVCustomFunction_d355531d1569421bb20205b595d4c55c_OutUV_3_Vector2) );
                float _SampleTexture2D_f5331c8e752f4566b67010e5d21b5a3b_R_4_Float = _SampleTexture2D_f5331c8e752f4566b67010e5d21b5a3b_RGBA_0_Vector4.r;
                float _SampleTexture2D_f5331c8e752f4566b67010e5d21b5a3b_G_5_Float = _SampleTexture2D_f5331c8e752f4566b67010e5d21b5a3b_RGBA_0_Vector4.g;
                float _SampleTexture2D_f5331c8e752f4566b67010e5d21b5a3b_B_6_Float = _SampleTexture2D_f5331c8e752f4566b67010e5d21b5a3b_RGBA_0_Vector4.b;
                float _SampleTexture2D_f5331c8e752f4566b67010e5d21b5a3b_A_7_Float = _SampleTexture2D_f5331c8e752f4566b67010e5d21b5a3b_RGBA_0_Vector4.a;
                Out_R_2 = _SampleTexture2D_f5331c8e752f4566b67010e5d21b5a3b_R_4_Float;
                Out_G_3 = _SampleTexture2D_f5331c8e752f4566b67010e5d21b5a3b_G_5_Float;
                Out_B_1 = _SampleTexture2D_f5331c8e752f4566b67010e5d21b5a3b_B_6_Float;
                }
                
                void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
                {
                    Out = A * B;
                }
                
                void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
                {
                    Out = lerp(A, B, T);
                }
                
                void Unity_Add_float(float A, float B, out float Out)
                {
                    Out = A + B;
                }
                
                void Unity_Branch_float3(float Predicate, float3 True, float3 False, out float3 Out)
                {
                    Out = Predicate ? True : False;
                }
            
            // Custom interpolators pre vertex
            /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
            
            // Graph Vertex
            struct VertexDescription
                {
                    float3 Position;
                    float3 Normal;
                    float3 Tangent;
                };
                
                VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
                {
                    VertexDescription description = (VertexDescription)0;
                    description.Position = IN.ObjectSpacePosition;
                    description.Normal = IN.ObjectSpaceNormal;
                    description.Tangent = IN.ObjectSpaceTangent;
                    return description;
                }
            
            // Custom interpolators, pre surface
            #ifdef FEATURES_GRAPH_VERTEX
            Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
            {
            return output;
            }
            #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
            #endif
            
            // Graph Pixel
            struct SurfaceDescription
                {
                    float3 BaseColor;
                    float Alpha;
                };
                
                SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
                {
                    SurfaceDescription surface = (SurfaceDescription)0;
                    float4 _Property_c6e5cb5bdf714fe8a6ff8a4db984ddee_Out_0_Vector4 = _Mouth_Color;
                    float4 _Property_6a93ac1325de4cb79d98f6caea13fdd8_Out_0_Vector4 = _Mouth_Color;
                    UnityTexture2D _Property_4dc7fd4f69e743aab88f1c4097303f71_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_Texture);
                    float _Property_e7251618caa74325ad70e40dd4b915cf_Out_0_Float = _Tongue_Depth;
                    float2 _Vector2_2e29f538f8a44455a027aee5898dae58_Out_0_Vector2 = float2(float(1), float(1));
                    float2 _Multiply_b2d5eac04cba4b1f887426a6bfc69bb5_Out_2_Vector2;
                    Unity_Multiply_float2_float2((_Property_e7251618caa74325ad70e40dd4b915cf_Out_0_Float.xx), _Vector2_2e29f538f8a44455a027aee5898dae58_Out_0_Vector2, _Multiply_b2d5eac04cba4b1f887426a6bfc69bb5_Out_2_Vector2);
                    float _Property_6097c9c4024844b7a53ab897340bc2b0_Out_0_Boolean = _Flip_X;
                    Bindings_ParallaxSampleTexture2D_ae45c5063979a430984f11ebf013c35c_float _ParallaxSampleTexture2D_c29d966c4be34c4bbdd1fe8a01531312;
                    _ParallaxSampleTexture2D_c29d966c4be34c4bbdd1fe8a01531312.ObjectSpaceViewDirection = IN.ObjectSpaceViewDirection;
                    _ParallaxSampleTexture2D_c29d966c4be34c4bbdd1fe8a01531312.uv0 = IN.uv0;
                    float _ParallaxSampleTexture2D_c29d966c4be34c4bbdd1fe8a01531312_OutR_2_Float;
                    float _ParallaxSampleTexture2D_c29d966c4be34c4bbdd1fe8a01531312_OutG_3_Float;
                    float _ParallaxSampleTexture2D_c29d966c4be34c4bbdd1fe8a01531312_OutB_1_Float;
                    SG_ParallaxSampleTexture2D_ae45c5063979a430984f11ebf013c35c_float(_Property_4dc7fd4f69e743aab88f1c4097303f71_Out_0_Texture2D, _Multiply_b2d5eac04cba4b1f887426a6bfc69bb5_Out_2_Vector2, _Property_6097c9c4024844b7a53ab897340bc2b0_Out_0_Boolean, _ParallaxSampleTexture2D_c29d966c4be34c4bbdd1fe8a01531312, _ParallaxSampleTexture2D_c29d966c4be34c4bbdd1fe8a01531312_OutR_2_Float, _ParallaxSampleTexture2D_c29d966c4be34c4bbdd1fe8a01531312_OutG_3_Float, _ParallaxSampleTexture2D_c29d966c4be34c4bbdd1fe8a01531312_OutB_1_Float);
                    float4 _Property_af6a9a8c31b845a6af3143281d2bf53b_Out_0_Vector4 = _Tongue_Color;
                    float4 _Multiply_e4efce35b80a4b2cafb8b9adf899cba8_Out_2_Vector4;
                    Unity_Multiply_float4_float4((_ParallaxSampleTexture2D_c29d966c4be34c4bbdd1fe8a01531312_OutB_1_Float.xxxx), _Property_af6a9a8c31b845a6af3143281d2bf53b_Out_0_Vector4, _Multiply_e4efce35b80a4b2cafb8b9adf899cba8_Out_2_Vector4);
                    float4 _Lerp_c14b8c38c754445685b381d8451856d3_Out_3_Vector4;
                    Unity_Lerp_float4(_Property_6a93ac1325de4cb79d98f6caea13fdd8_Out_0_Vector4, _Multiply_e4efce35b80a4b2cafb8b9adf899cba8_Out_2_Vector4, (_ParallaxSampleTexture2D_c29d966c4be34c4bbdd1fe8a01531312_OutB_1_Float.xxxx), _Lerp_c14b8c38c754445685b381d8451856d3_Out_3_Vector4);
                    float4 _Property_361f5fa06ce3451f93bb5d7380c2da3e_Out_0_Vector4 = _Teeth_Edge_Color;
                    UnityTexture2D _Property_532fa8ab3762489185678100eb2df2b3_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_Texture);
                    float _Property_37191fa8d35f47f2b1e681595f7e55f4_Out_0_Float = _Teeth_Depth;
                    float _Property_b3e5f83350924b82878b321ad1c8327f_Out_0_Float = _Teeth_Thickness;
                    float _Add_562c66b73f9a4fc59ae477a4c3b081fc_Out_2_Float;
                    Unity_Add_float(_Property_37191fa8d35f47f2b1e681595f7e55f4_Out_0_Float, _Property_b3e5f83350924b82878b321ad1c8327f_Out_0_Float, _Add_562c66b73f9a4fc59ae477a4c3b081fc_Out_2_Float);
                    Bindings_ParallaxSampleTexture2D_ae45c5063979a430984f11ebf013c35c_float _ParallaxSampleTexture2D_e67be38c032a40e087adc2e477807e7d;
                    _ParallaxSampleTexture2D_e67be38c032a40e087adc2e477807e7d.ObjectSpaceViewDirection = IN.ObjectSpaceViewDirection;
                    _ParallaxSampleTexture2D_e67be38c032a40e087adc2e477807e7d.uv0 = IN.uv0;
                    float _ParallaxSampleTexture2D_e67be38c032a40e087adc2e477807e7d_OutR_2_Float;
                    float _ParallaxSampleTexture2D_e67be38c032a40e087adc2e477807e7d_OutG_3_Float;
                    float _ParallaxSampleTexture2D_e67be38c032a40e087adc2e477807e7d_OutB_1_Float;
                    SG_ParallaxSampleTexture2D_ae45c5063979a430984f11ebf013c35c_float(_Property_532fa8ab3762489185678100eb2df2b3_Out_0_Texture2D, (_Add_562c66b73f9a4fc59ae477a4c3b081fc_Out_2_Float.xx), 0, _ParallaxSampleTexture2D_e67be38c032a40e087adc2e477807e7d, _ParallaxSampleTexture2D_e67be38c032a40e087adc2e477807e7d_OutR_2_Float, _ParallaxSampleTexture2D_e67be38c032a40e087adc2e477807e7d_OutG_3_Float, _ParallaxSampleTexture2D_e67be38c032a40e087adc2e477807e7d_OutB_1_Float);
                    float4 _Lerp_5f6226b27af847188fbc7109af235632_Out_3_Vector4;
                    Unity_Lerp_float4(_Lerp_c14b8c38c754445685b381d8451856d3_Out_3_Vector4, _Property_361f5fa06ce3451f93bb5d7380c2da3e_Out_0_Vector4, (_ParallaxSampleTexture2D_e67be38c032a40e087adc2e477807e7d_OutG_3_Float.xxxx), _Lerp_5f6226b27af847188fbc7109af235632_Out_3_Vector4);
                    float4 _Property_695a3e46fe5548aa87913f3d4c0ecf05_Out_0_Vector4 = _Teeth_Color;
                    UnityTexture2D _Property_b738848bca344d979f28a5e8709ce50a_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_Texture);
                    float _Property_6497ac1f7d7f451f98435734295f617c_Out_0_Float = _Teeth_Depth;
                    float _Property_d9e84b7e76784f3e8aa00bd8bfe14599_Out_0_Boolean = _Flip_X;
                    Bindings_ParallaxSampleTexture2D_ae45c5063979a430984f11ebf013c35c_float _ParallaxSampleTexture2D_26eb78f900f5474b9ded770a5d15b9c7;
                    _ParallaxSampleTexture2D_26eb78f900f5474b9ded770a5d15b9c7.ObjectSpaceViewDirection = IN.ObjectSpaceViewDirection;
                    _ParallaxSampleTexture2D_26eb78f900f5474b9ded770a5d15b9c7.uv0 = IN.uv0;
                    float _ParallaxSampleTexture2D_26eb78f900f5474b9ded770a5d15b9c7_OutR_2_Float;
                    float _ParallaxSampleTexture2D_26eb78f900f5474b9ded770a5d15b9c7_OutG_3_Float;
                    float _ParallaxSampleTexture2D_26eb78f900f5474b9ded770a5d15b9c7_OutB_1_Float;
                    SG_ParallaxSampleTexture2D_ae45c5063979a430984f11ebf013c35c_float(_Property_b738848bca344d979f28a5e8709ce50a_Out_0_Texture2D, (_Property_6497ac1f7d7f451f98435734295f617c_Out_0_Float.xx), _Property_d9e84b7e76784f3e8aa00bd8bfe14599_Out_0_Boolean, _ParallaxSampleTexture2D_26eb78f900f5474b9ded770a5d15b9c7, _ParallaxSampleTexture2D_26eb78f900f5474b9ded770a5d15b9c7_OutR_2_Float, _ParallaxSampleTexture2D_26eb78f900f5474b9ded770a5d15b9c7_OutG_3_Float, _ParallaxSampleTexture2D_26eb78f900f5474b9ded770a5d15b9c7_OutB_1_Float);
                    float4 _Lerp_3951173a3ad147bda8daa33a464ba7e4_Out_3_Vector4;
                    Unity_Lerp_float4(_Lerp_5f6226b27af847188fbc7109af235632_Out_3_Vector4, _Property_695a3e46fe5548aa87913f3d4c0ecf05_Out_0_Vector4, (_ParallaxSampleTexture2D_26eb78f900f5474b9ded770a5d15b9c7_OutG_3_Float.xxxx), _Lerp_3951173a3ad147bda8daa33a464ba7e4_Out_3_Vector4);
                    UnityTexture2D _Property_a67db6e8d526453c89a445ef89b619e3_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_Texture);
                    float _Property_764a5b6e97fb4e6cb52af22e014d2cb7_Out_0_Float = _Mouth_Depth;
                    float2 _Vector2_1dc9ddcac0504b7daf87eeaf74d1347b_Out_0_Vector2 = float2(float(1), float(1));
                    float2 _Multiply_4a47a872e85a4a4c8c8494908dbe7767_Out_2_Vector2;
                    Unity_Multiply_float2_float2((_Property_764a5b6e97fb4e6cb52af22e014d2cb7_Out_0_Float.xx), _Vector2_1dc9ddcac0504b7daf87eeaf74d1347b_Out_0_Vector2, _Multiply_4a47a872e85a4a4c8c8494908dbe7767_Out_2_Vector2);
                    float _Property_40b6c2c1ebd649ee85152cbb137d4219_Out_0_Boolean = _Flip_X;
                    Bindings_ParallaxSampleTexture2D_ae45c5063979a430984f11ebf013c35c_float _ParallaxSampleTexture2D_bb9439882a6f45b096dccfb671f40ade;
                    _ParallaxSampleTexture2D_bb9439882a6f45b096dccfb671f40ade.ObjectSpaceViewDirection = IN.ObjectSpaceViewDirection;
                    _ParallaxSampleTexture2D_bb9439882a6f45b096dccfb671f40ade.uv0 = IN.uv0;
                    float _ParallaxSampleTexture2D_bb9439882a6f45b096dccfb671f40ade_OutR_2_Float;
                    float _ParallaxSampleTexture2D_bb9439882a6f45b096dccfb671f40ade_OutG_3_Float;
                    float _ParallaxSampleTexture2D_bb9439882a6f45b096dccfb671f40ade_OutB_1_Float;
                    SG_ParallaxSampleTexture2D_ae45c5063979a430984f11ebf013c35c_float(_Property_a67db6e8d526453c89a445ef89b619e3_Out_0_Texture2D, _Multiply_4a47a872e85a4a4c8c8494908dbe7767_Out_2_Vector2, _Property_40b6c2c1ebd649ee85152cbb137d4219_Out_0_Boolean, _ParallaxSampleTexture2D_bb9439882a6f45b096dccfb671f40ade, _ParallaxSampleTexture2D_bb9439882a6f45b096dccfb671f40ade_OutR_2_Float, _ParallaxSampleTexture2D_bb9439882a6f45b096dccfb671f40ade_OutG_3_Float, _ParallaxSampleTexture2D_bb9439882a6f45b096dccfb671f40ade_OutB_1_Float);
                    float4 _Lerp_9412d0275e2b47429cb89b3b89798977_Out_3_Vector4;
                    Unity_Lerp_float4(_Property_c6e5cb5bdf714fe8a6ff8a4db984ddee_Out_0_Vector4, _Lerp_3951173a3ad147bda8daa33a464ba7e4_Out_3_Vector4, (_ParallaxSampleTexture2D_bb9439882a6f45b096dccfb671f40ade_OutR_2_Float.xxxx), _Lerp_9412d0275e2b47429cb89b3b89798977_Out_3_Vector4);
                    float3 _Branch_48ee12d07e0447a8a1b11405ef31386c_Out_3_Vector3;
                    Unity_Branch_float3(0, IN.ObjectSpaceViewDirection, (_Lerp_9412d0275e2b47429cb89b3b89798977_Out_3_Vector4.xyz), _Branch_48ee12d07e0447a8a1b11405ef31386c_Out_3_Vector3);
                    UnityTexture2D _Property_fc4d5cb8867142b7af1000f12a0b5617_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_Texture);
                    float4 _SampleTexture2D_39637566a9c84bb8aa66f76590d11702_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_fc4d5cb8867142b7af1000f12a0b5617_Out_0_Texture2D.tex, _Property_fc4d5cb8867142b7af1000f12a0b5617_Out_0_Texture2D.samplerstate, _Property_fc4d5cb8867142b7af1000f12a0b5617_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
                    float _SampleTexture2D_39637566a9c84bb8aa66f76590d11702_R_4_Float = _SampleTexture2D_39637566a9c84bb8aa66f76590d11702_RGBA_0_Vector4.r;
                    float _SampleTexture2D_39637566a9c84bb8aa66f76590d11702_G_5_Float = _SampleTexture2D_39637566a9c84bb8aa66f76590d11702_RGBA_0_Vector4.g;
                    float _SampleTexture2D_39637566a9c84bb8aa66f76590d11702_B_6_Float = _SampleTexture2D_39637566a9c84bb8aa66f76590d11702_RGBA_0_Vector4.b;
                    float _SampleTexture2D_39637566a9c84bb8aa66f76590d11702_A_7_Float = _SampleTexture2D_39637566a9c84bb8aa66f76590d11702_RGBA_0_Vector4.a;
                    surface.BaseColor = _Branch_48ee12d07e0447a8a1b11405ef31386c_Out_3_Vector3;
                    surface.Alpha = _SampleTexture2D_39637566a9c84bb8aa66f76590d11702_R_4_Float;
                    return surface;
                }
            
            // --------------------------------------------------
            // Build Graph Inputs
            #ifdef HAVE_VFX_MODIFICATION
            #define VFX_SRP_ATTRIBUTES Attributes
            #define VFX_SRP_VARYINGS Varyings
            #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
            #endif
            VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
                {
                    VertexDescriptionInputs output;
                    ZERO_INITIALIZE(VertexDescriptionInputs, output);
                
                    output.ObjectSpaceNormal =                          input.normalOS;
                    output.ObjectSpaceTangent =                         input.tangentOS.xyz;
                    output.ObjectSpacePosition =                        input.positionOS;
                #if UNITY_ANY_INSTANCING_ENABLED
                #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
                #endif
                
                    return output;
                }
                
            SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
                {
                    SurfaceDescriptionInputs output;
                    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
                
                #ifdef HAVE_VFX_MODIFICATION
                #if VFX_USE_GRAPH_VALUES
                    uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
                    /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
                #endif
                    /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
                
                #endif
                
                    
                
                
                
                
                
                    output.WorldSpaceViewDirection = GetWorldSpaceNormalizeViewDir(input.positionWS);
                    output.ObjectSpaceViewDirection = TransformWorldToObjectDir(output.WorldSpaceViewDirection);
                
                    #if UNITY_UV_STARTS_AT_TOP
                    #else
                    #endif
                
                
                    output.uv0 = input.texCoord0;
                #if UNITY_ANY_INSTANCING_ENABLED
                #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
                #endif
                #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
                #else
                #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
                #endif
                #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
                
                        return output;
                }
                
            
            // --------------------------------------------------
            // Main
            
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/SelectionPickingPass.hlsl"
            
            // --------------------------------------------------
            // Visual Effect Vertex Invocations
            #ifdef HAVE_VFX_MODIFICATION
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
            #endif
            
            ENDHLSL
            }
            Pass
            {
                Name "Universal 2D"
                Tags
                {
                    "LightMode" = "Universal2D"
                }
            
            // Render State
            Cull Back
                Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
                ZTest LEqual
                ZWrite Off
            
            // Debug
            // <None>
            
            // --------------------------------------------------
            // Pass
            
            HLSLPROGRAM
            
            // Pragmas
            #pragma target 2.0
                #pragma vertex vert
                #pragma fragment frag
            
            // Keywords
            // PassKeywords: <None>
            // GraphKeywords: <None>
            
            // Defines
            
            #define _NORMALMAP 1
            #define _NORMAL_DROPOFF_TS 1
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define ATTRIBUTES_NEED_TEXCOORD0
            #define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
            #define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
            #define VARYINGS_NEED_POSITION_WS
            #define VARYINGS_NEED_TEXCOORD0
            #define FEATURES_GRAPH_VERTEX
            /* WARNING: $splice Could not find named fragment 'PassInstancing' */
            #define SHADERPASS SHADERPASS_2D
            
            
            // custom interpolator pre-include
            /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
            
            // Includes
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
            
            // --------------------------------------------------
            // Structs and Packing
            
            // custom interpolators pre packing
            /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
            
            struct Attributes
                {
                     float3 positionOS : POSITION;
                     float3 normalOS : NORMAL;
                     float4 tangentOS : TANGENT;
                     float4 uv0 : TEXCOORD0;
                    #if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
                     uint instanceID : INSTANCEID_SEMANTIC;
                    #endif
                };
                struct Varyings
                {
                     float4 positionCS : SV_POSITION;
                     float3 positionWS;
                     float4 texCoord0;
                    #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
                     uint instanceID : CUSTOM_INSTANCE_ID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                     uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                     uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                     FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
                    #endif
                };
                struct SurfaceDescriptionInputs
                {
                     float3 ObjectSpaceViewDirection;
                     float3 WorldSpaceViewDirection;
                     float4 uv0;
                };
                struct VertexDescriptionInputs
                {
                     float3 ObjectSpaceNormal;
                     float3 ObjectSpaceTangent;
                     float3 ObjectSpacePosition;
                };
                struct PackedVaryings
                {
                     float4 positionCS : SV_POSITION;
                     float4 texCoord0 : INTERP0;
                     float3 positionWS : INTERP1;
                    #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
                     uint instanceID : CUSTOM_INSTANCE_ID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                     uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                     uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                     FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
                    #endif
                };
            
            PackedVaryings PackVaryings (Varyings input)
                {
                    PackedVaryings output;
                    ZERO_INITIALIZE(PackedVaryings, output);
                    output.positionCS = input.positionCS;
                    output.texCoord0.xyzw = input.texCoord0;
                    output.positionWS.xyz = input.positionWS;
                    #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
                    output.instanceID = input.instanceID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                    output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                    output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                    output.cullFace = input.cullFace;
                    #endif
                    return output;
                }
                
                Varyings UnpackVaryings (PackedVaryings input)
                {
                    Varyings output;
                    output.positionCS = input.positionCS;
                    output.texCoord0 = input.texCoord0.xyzw;
                    output.positionWS = input.positionWS.xyz;
                    #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
                    output.instanceID = input.instanceID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                    output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                    output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                    output.cullFace = input.cullFace;
                    #endif
                    return output;
                }
                
            
            // --------------------------------------------------
            // Graph
            
            // Graph Properties
            CBUFFER_START(UnityPerMaterial)
                float _Teeth_Depth;
                float4 _Texture_TexelSize;
                float _Tongue_Depth;
                float4 _Mouth_Color;
                float4 _Tongue_Color;
                float4 _Teeth_Color;
                float4 _Teeth_Edge_Color;
                float _Teeth_Thickness;
                float _Mouth_Depth;
                float _Flip_X;
                UNITY_TEXTURE_STREAMING_DEBUG_VARS;
                CBUFFER_END
                
                
                // Object and Global properties
                SAMPLER(SamplerState_Linear_Repeat);
                TEXTURE2D(_Texture);
                SAMPLER(sampler_Texture);
            
            // Graph Includes
            // GraphIncludes: <None>
            
            // -- Property used by ScenePickingPass
            #ifdef SCENEPICKINGPASS
            float4 _SelectionID;
            #endif
            
            // -- Properties used by SceneSelectionPass
            #ifdef SCENESELECTIONPASS
            int _ObjectId;
            int _PassValue;
            #endif
            
            // Graph Functions
            
                void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
                {
                    Out = A * B;
                }
                
                void Unity_Branch_float(float Predicate, float True, float False, out float Out)
                {
                    Out = Predicate ? True : False;
                }
                
                void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
                {
                    RGBA = float4(R, G, B, A);
                    RGB = float3(R, G, B);
                    RG = float2(R, G);
                }
                
                void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
                {
                Out = A * B;
                }
                
                void Unity_Negate_float2(float2 In, out float2 Out)
                {
                    Out = -1 * In;
                }
                
                // unity-custom-func-begin
                void ParallaxUV_float(float2 UV, float3 ViewDirection, float2 Depth, out float2 OutUV){
                OutUV = saturate(
                  UV  + (ViewDirection.xz
                  * Depth)
                );
                }
                // unity-custom-func-end
                
                struct Bindings_ParallaxSampleTexture2D_ae45c5063979a430984f11ebf013c35c_float
                {
                float3 ObjectSpaceViewDirection;
                half4 uv0;
                };
                
                void SG_ParallaxSampleTexture2D_ae45c5063979a430984f11ebf013c35c_float(UnityTexture2D _Texture, float2 _Depth, float _Flip_X, Bindings_ParallaxSampleTexture2D_ae45c5063979a430984f11ebf013c35c_float IN, out float Out_R_2, out float Out_G_3, out float Out_B_1)
                {
                UnityTexture2D _Property_dad3badd99624fdbab11879bd30d376a_Out_0_Texture2D = _Texture;
                float4 _UV_4592539f7377446791ce24767a34c92a_Out_0_Vector4 = IN.uv0;
                float _Property_eb437bd110674c94aa025481bbeb9168_Out_0_Boolean = _Flip_X;
                float _Branch_2db3155f235b4e9aad311b592b7680c6_Out_3_Float;
                Unity_Branch_float(_Property_eb437bd110674c94aa025481bbeb9168_Out_0_Boolean, float(-1), float(1), _Branch_2db3155f235b4e9aad311b592b7680c6_Out_3_Float);
                float4 _Combine_e831df117fad41e7a7aca27e3cb0828c_RGBA_4_Vector4;
                float3 _Combine_e831df117fad41e7a7aca27e3cb0828c_RGB_5_Vector3;
                float2 _Combine_e831df117fad41e7a7aca27e3cb0828c_RG_6_Vector2;
                Unity_Combine_float(_Branch_2db3155f235b4e9aad311b592b7680c6_Out_3_Float, float(1), float(1), float(1), _Combine_e831df117fad41e7a7aca27e3cb0828c_RGBA_4_Vector4, _Combine_e831df117fad41e7a7aca27e3cb0828c_RGB_5_Vector3, _Combine_e831df117fad41e7a7aca27e3cb0828c_RG_6_Vector2);
                float3 _Multiply_5b8a4ed7c54545d98ac880a3d084e129_Out_2_Vector3;
                Unity_Multiply_float3_float3(IN.ObjectSpaceViewDirection, _Combine_e831df117fad41e7a7aca27e3cb0828c_RGB_5_Vector3, _Multiply_5b8a4ed7c54545d98ac880a3d084e129_Out_2_Vector3);
                float2 _Property_db40aa4398dc42d2a16ff4e25da19fb8_Out_0_Vector2 = _Depth;
                float2 _Negate_9ba8a9d8334f438fb84d14622429bed5_Out_1_Vector2;
                Unity_Negate_float2(_Property_db40aa4398dc42d2a16ff4e25da19fb8_Out_0_Vector2, _Negate_9ba8a9d8334f438fb84d14622429bed5_Out_1_Vector2);
                float2 _ParallaxUVCustomFunction_d355531d1569421bb20205b595d4c55c_OutUV_3_Vector2;
                ParallaxUV_float((_UV_4592539f7377446791ce24767a34c92a_Out_0_Vector4.xy), _Multiply_5b8a4ed7c54545d98ac880a3d084e129_Out_2_Vector3, _Negate_9ba8a9d8334f438fb84d14622429bed5_Out_1_Vector2, _ParallaxUVCustomFunction_d355531d1569421bb20205b595d4c55c_OutUV_3_Vector2);
                float4 _SampleTexture2D_f5331c8e752f4566b67010e5d21b5a3b_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_dad3badd99624fdbab11879bd30d376a_Out_0_Texture2D.tex, _Property_dad3badd99624fdbab11879bd30d376a_Out_0_Texture2D.samplerstate, _Property_dad3badd99624fdbab11879bd30d376a_Out_0_Texture2D.GetTransformedUV(_ParallaxUVCustomFunction_d355531d1569421bb20205b595d4c55c_OutUV_3_Vector2) );
                float _SampleTexture2D_f5331c8e752f4566b67010e5d21b5a3b_R_4_Float = _SampleTexture2D_f5331c8e752f4566b67010e5d21b5a3b_RGBA_0_Vector4.r;
                float _SampleTexture2D_f5331c8e752f4566b67010e5d21b5a3b_G_5_Float = _SampleTexture2D_f5331c8e752f4566b67010e5d21b5a3b_RGBA_0_Vector4.g;
                float _SampleTexture2D_f5331c8e752f4566b67010e5d21b5a3b_B_6_Float = _SampleTexture2D_f5331c8e752f4566b67010e5d21b5a3b_RGBA_0_Vector4.b;
                float _SampleTexture2D_f5331c8e752f4566b67010e5d21b5a3b_A_7_Float = _SampleTexture2D_f5331c8e752f4566b67010e5d21b5a3b_RGBA_0_Vector4.a;
                Out_R_2 = _SampleTexture2D_f5331c8e752f4566b67010e5d21b5a3b_R_4_Float;
                Out_G_3 = _SampleTexture2D_f5331c8e752f4566b67010e5d21b5a3b_G_5_Float;
                Out_B_1 = _SampleTexture2D_f5331c8e752f4566b67010e5d21b5a3b_B_6_Float;
                }
                
                void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
                {
                    Out = A * B;
                }
                
                void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
                {
                    Out = lerp(A, B, T);
                }
                
                void Unity_Add_float(float A, float B, out float Out)
                {
                    Out = A + B;
                }
                
                void Unity_Branch_float3(float Predicate, float3 True, float3 False, out float3 Out)
                {
                    Out = Predicate ? True : False;
                }
            
            // Custom interpolators pre vertex
            /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
            
            // Graph Vertex
            struct VertexDescription
                {
                    float3 Position;
                    float3 Normal;
                    float3 Tangent;
                };
                
                VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
                {
                    VertexDescription description = (VertexDescription)0;
                    description.Position = IN.ObjectSpacePosition;
                    description.Normal = IN.ObjectSpaceNormal;
                    description.Tangent = IN.ObjectSpaceTangent;
                    return description;
                }
            
            // Custom interpolators, pre surface
            #ifdef FEATURES_GRAPH_VERTEX
            Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
            {
            return output;
            }
            #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
            #endif
            
            // Graph Pixel
            struct SurfaceDescription
                {
                    float3 BaseColor;
                    float Alpha;
                };
                
                SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
                {
                    SurfaceDescription surface = (SurfaceDescription)0;
                    float4 _Property_c6e5cb5bdf714fe8a6ff8a4db984ddee_Out_0_Vector4 = _Mouth_Color;
                    float4 _Property_6a93ac1325de4cb79d98f6caea13fdd8_Out_0_Vector4 = _Mouth_Color;
                    UnityTexture2D _Property_4dc7fd4f69e743aab88f1c4097303f71_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_Texture);
                    float _Property_e7251618caa74325ad70e40dd4b915cf_Out_0_Float = _Tongue_Depth;
                    float2 _Vector2_2e29f538f8a44455a027aee5898dae58_Out_0_Vector2 = float2(float(1), float(1));
                    float2 _Multiply_b2d5eac04cba4b1f887426a6bfc69bb5_Out_2_Vector2;
                    Unity_Multiply_float2_float2((_Property_e7251618caa74325ad70e40dd4b915cf_Out_0_Float.xx), _Vector2_2e29f538f8a44455a027aee5898dae58_Out_0_Vector2, _Multiply_b2d5eac04cba4b1f887426a6bfc69bb5_Out_2_Vector2);
                    float _Property_6097c9c4024844b7a53ab897340bc2b0_Out_0_Boolean = _Flip_X;
                    Bindings_ParallaxSampleTexture2D_ae45c5063979a430984f11ebf013c35c_float _ParallaxSampleTexture2D_c29d966c4be34c4bbdd1fe8a01531312;
                    _ParallaxSampleTexture2D_c29d966c4be34c4bbdd1fe8a01531312.ObjectSpaceViewDirection = IN.ObjectSpaceViewDirection;
                    _ParallaxSampleTexture2D_c29d966c4be34c4bbdd1fe8a01531312.uv0 = IN.uv0;
                    float _ParallaxSampleTexture2D_c29d966c4be34c4bbdd1fe8a01531312_OutR_2_Float;
                    float _ParallaxSampleTexture2D_c29d966c4be34c4bbdd1fe8a01531312_OutG_3_Float;
                    float _ParallaxSampleTexture2D_c29d966c4be34c4bbdd1fe8a01531312_OutB_1_Float;
                    SG_ParallaxSampleTexture2D_ae45c5063979a430984f11ebf013c35c_float(_Property_4dc7fd4f69e743aab88f1c4097303f71_Out_0_Texture2D, _Multiply_b2d5eac04cba4b1f887426a6bfc69bb5_Out_2_Vector2, _Property_6097c9c4024844b7a53ab897340bc2b0_Out_0_Boolean, _ParallaxSampleTexture2D_c29d966c4be34c4bbdd1fe8a01531312, _ParallaxSampleTexture2D_c29d966c4be34c4bbdd1fe8a01531312_OutR_2_Float, _ParallaxSampleTexture2D_c29d966c4be34c4bbdd1fe8a01531312_OutG_3_Float, _ParallaxSampleTexture2D_c29d966c4be34c4bbdd1fe8a01531312_OutB_1_Float);
                    float4 _Property_af6a9a8c31b845a6af3143281d2bf53b_Out_0_Vector4 = _Tongue_Color;
                    float4 _Multiply_e4efce35b80a4b2cafb8b9adf899cba8_Out_2_Vector4;
                    Unity_Multiply_float4_float4((_ParallaxSampleTexture2D_c29d966c4be34c4bbdd1fe8a01531312_OutB_1_Float.xxxx), _Property_af6a9a8c31b845a6af3143281d2bf53b_Out_0_Vector4, _Multiply_e4efce35b80a4b2cafb8b9adf899cba8_Out_2_Vector4);
                    float4 _Lerp_c14b8c38c754445685b381d8451856d3_Out_3_Vector4;
                    Unity_Lerp_float4(_Property_6a93ac1325de4cb79d98f6caea13fdd8_Out_0_Vector4, _Multiply_e4efce35b80a4b2cafb8b9adf899cba8_Out_2_Vector4, (_ParallaxSampleTexture2D_c29d966c4be34c4bbdd1fe8a01531312_OutB_1_Float.xxxx), _Lerp_c14b8c38c754445685b381d8451856d3_Out_3_Vector4);
                    float4 _Property_361f5fa06ce3451f93bb5d7380c2da3e_Out_0_Vector4 = _Teeth_Edge_Color;
                    UnityTexture2D _Property_532fa8ab3762489185678100eb2df2b3_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_Texture);
                    float _Property_37191fa8d35f47f2b1e681595f7e55f4_Out_0_Float = _Teeth_Depth;
                    float _Property_b3e5f83350924b82878b321ad1c8327f_Out_0_Float = _Teeth_Thickness;
                    float _Add_562c66b73f9a4fc59ae477a4c3b081fc_Out_2_Float;
                    Unity_Add_float(_Property_37191fa8d35f47f2b1e681595f7e55f4_Out_0_Float, _Property_b3e5f83350924b82878b321ad1c8327f_Out_0_Float, _Add_562c66b73f9a4fc59ae477a4c3b081fc_Out_2_Float);
                    Bindings_ParallaxSampleTexture2D_ae45c5063979a430984f11ebf013c35c_float _ParallaxSampleTexture2D_e67be38c032a40e087adc2e477807e7d;
                    _ParallaxSampleTexture2D_e67be38c032a40e087adc2e477807e7d.ObjectSpaceViewDirection = IN.ObjectSpaceViewDirection;
                    _ParallaxSampleTexture2D_e67be38c032a40e087adc2e477807e7d.uv0 = IN.uv0;
                    float _ParallaxSampleTexture2D_e67be38c032a40e087adc2e477807e7d_OutR_2_Float;
                    float _ParallaxSampleTexture2D_e67be38c032a40e087adc2e477807e7d_OutG_3_Float;
                    float _ParallaxSampleTexture2D_e67be38c032a40e087adc2e477807e7d_OutB_1_Float;
                    SG_ParallaxSampleTexture2D_ae45c5063979a430984f11ebf013c35c_float(_Property_532fa8ab3762489185678100eb2df2b3_Out_0_Texture2D, (_Add_562c66b73f9a4fc59ae477a4c3b081fc_Out_2_Float.xx), 0, _ParallaxSampleTexture2D_e67be38c032a40e087adc2e477807e7d, _ParallaxSampleTexture2D_e67be38c032a40e087adc2e477807e7d_OutR_2_Float, _ParallaxSampleTexture2D_e67be38c032a40e087adc2e477807e7d_OutG_3_Float, _ParallaxSampleTexture2D_e67be38c032a40e087adc2e477807e7d_OutB_1_Float);
                    float4 _Lerp_5f6226b27af847188fbc7109af235632_Out_3_Vector4;
                    Unity_Lerp_float4(_Lerp_c14b8c38c754445685b381d8451856d3_Out_3_Vector4, _Property_361f5fa06ce3451f93bb5d7380c2da3e_Out_0_Vector4, (_ParallaxSampleTexture2D_e67be38c032a40e087adc2e477807e7d_OutG_3_Float.xxxx), _Lerp_5f6226b27af847188fbc7109af235632_Out_3_Vector4);
                    float4 _Property_695a3e46fe5548aa87913f3d4c0ecf05_Out_0_Vector4 = _Teeth_Color;
                    UnityTexture2D _Property_b738848bca344d979f28a5e8709ce50a_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_Texture);
                    float _Property_6497ac1f7d7f451f98435734295f617c_Out_0_Float = _Teeth_Depth;
                    float _Property_d9e84b7e76784f3e8aa00bd8bfe14599_Out_0_Boolean = _Flip_X;
                    Bindings_ParallaxSampleTexture2D_ae45c5063979a430984f11ebf013c35c_float _ParallaxSampleTexture2D_26eb78f900f5474b9ded770a5d15b9c7;
                    _ParallaxSampleTexture2D_26eb78f900f5474b9ded770a5d15b9c7.ObjectSpaceViewDirection = IN.ObjectSpaceViewDirection;
                    _ParallaxSampleTexture2D_26eb78f900f5474b9ded770a5d15b9c7.uv0 = IN.uv0;
                    float _ParallaxSampleTexture2D_26eb78f900f5474b9ded770a5d15b9c7_OutR_2_Float;
                    float _ParallaxSampleTexture2D_26eb78f900f5474b9ded770a5d15b9c7_OutG_3_Float;
                    float _ParallaxSampleTexture2D_26eb78f900f5474b9ded770a5d15b9c7_OutB_1_Float;
                    SG_ParallaxSampleTexture2D_ae45c5063979a430984f11ebf013c35c_float(_Property_b738848bca344d979f28a5e8709ce50a_Out_0_Texture2D, (_Property_6497ac1f7d7f451f98435734295f617c_Out_0_Float.xx), _Property_d9e84b7e76784f3e8aa00bd8bfe14599_Out_0_Boolean, _ParallaxSampleTexture2D_26eb78f900f5474b9ded770a5d15b9c7, _ParallaxSampleTexture2D_26eb78f900f5474b9ded770a5d15b9c7_OutR_2_Float, _ParallaxSampleTexture2D_26eb78f900f5474b9ded770a5d15b9c7_OutG_3_Float, _ParallaxSampleTexture2D_26eb78f900f5474b9ded770a5d15b9c7_OutB_1_Float);
                    float4 _Lerp_3951173a3ad147bda8daa33a464ba7e4_Out_3_Vector4;
                    Unity_Lerp_float4(_Lerp_5f6226b27af847188fbc7109af235632_Out_3_Vector4, _Property_695a3e46fe5548aa87913f3d4c0ecf05_Out_0_Vector4, (_ParallaxSampleTexture2D_26eb78f900f5474b9ded770a5d15b9c7_OutG_3_Float.xxxx), _Lerp_3951173a3ad147bda8daa33a464ba7e4_Out_3_Vector4);
                    UnityTexture2D _Property_a67db6e8d526453c89a445ef89b619e3_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_Texture);
                    float _Property_764a5b6e97fb4e6cb52af22e014d2cb7_Out_0_Float = _Mouth_Depth;
                    float2 _Vector2_1dc9ddcac0504b7daf87eeaf74d1347b_Out_0_Vector2 = float2(float(1), float(1));
                    float2 _Multiply_4a47a872e85a4a4c8c8494908dbe7767_Out_2_Vector2;
                    Unity_Multiply_float2_float2((_Property_764a5b6e97fb4e6cb52af22e014d2cb7_Out_0_Float.xx), _Vector2_1dc9ddcac0504b7daf87eeaf74d1347b_Out_0_Vector2, _Multiply_4a47a872e85a4a4c8c8494908dbe7767_Out_2_Vector2);
                    float _Property_40b6c2c1ebd649ee85152cbb137d4219_Out_0_Boolean = _Flip_X;
                    Bindings_ParallaxSampleTexture2D_ae45c5063979a430984f11ebf013c35c_float _ParallaxSampleTexture2D_bb9439882a6f45b096dccfb671f40ade;
                    _ParallaxSampleTexture2D_bb9439882a6f45b096dccfb671f40ade.ObjectSpaceViewDirection = IN.ObjectSpaceViewDirection;
                    _ParallaxSampleTexture2D_bb9439882a6f45b096dccfb671f40ade.uv0 = IN.uv0;
                    float _ParallaxSampleTexture2D_bb9439882a6f45b096dccfb671f40ade_OutR_2_Float;
                    float _ParallaxSampleTexture2D_bb9439882a6f45b096dccfb671f40ade_OutG_3_Float;
                    float _ParallaxSampleTexture2D_bb9439882a6f45b096dccfb671f40ade_OutB_1_Float;
                    SG_ParallaxSampleTexture2D_ae45c5063979a430984f11ebf013c35c_float(_Property_a67db6e8d526453c89a445ef89b619e3_Out_0_Texture2D, _Multiply_4a47a872e85a4a4c8c8494908dbe7767_Out_2_Vector2, _Property_40b6c2c1ebd649ee85152cbb137d4219_Out_0_Boolean, _ParallaxSampleTexture2D_bb9439882a6f45b096dccfb671f40ade, _ParallaxSampleTexture2D_bb9439882a6f45b096dccfb671f40ade_OutR_2_Float, _ParallaxSampleTexture2D_bb9439882a6f45b096dccfb671f40ade_OutG_3_Float, _ParallaxSampleTexture2D_bb9439882a6f45b096dccfb671f40ade_OutB_1_Float);
                    float4 _Lerp_9412d0275e2b47429cb89b3b89798977_Out_3_Vector4;
                    Unity_Lerp_float4(_Property_c6e5cb5bdf714fe8a6ff8a4db984ddee_Out_0_Vector4, _Lerp_3951173a3ad147bda8daa33a464ba7e4_Out_3_Vector4, (_ParallaxSampleTexture2D_bb9439882a6f45b096dccfb671f40ade_OutR_2_Float.xxxx), _Lerp_9412d0275e2b47429cb89b3b89798977_Out_3_Vector4);
                    float3 _Branch_48ee12d07e0447a8a1b11405ef31386c_Out_3_Vector3;
                    Unity_Branch_float3(0, IN.ObjectSpaceViewDirection, (_Lerp_9412d0275e2b47429cb89b3b89798977_Out_3_Vector4.xyz), _Branch_48ee12d07e0447a8a1b11405ef31386c_Out_3_Vector3);
                    UnityTexture2D _Property_fc4d5cb8867142b7af1000f12a0b5617_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_Texture);
                    float4 _SampleTexture2D_39637566a9c84bb8aa66f76590d11702_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_fc4d5cb8867142b7af1000f12a0b5617_Out_0_Texture2D.tex, _Property_fc4d5cb8867142b7af1000f12a0b5617_Out_0_Texture2D.samplerstate, _Property_fc4d5cb8867142b7af1000f12a0b5617_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
                    float _SampleTexture2D_39637566a9c84bb8aa66f76590d11702_R_4_Float = _SampleTexture2D_39637566a9c84bb8aa66f76590d11702_RGBA_0_Vector4.r;
                    float _SampleTexture2D_39637566a9c84bb8aa66f76590d11702_G_5_Float = _SampleTexture2D_39637566a9c84bb8aa66f76590d11702_RGBA_0_Vector4.g;
                    float _SampleTexture2D_39637566a9c84bb8aa66f76590d11702_B_6_Float = _SampleTexture2D_39637566a9c84bb8aa66f76590d11702_RGBA_0_Vector4.b;
                    float _SampleTexture2D_39637566a9c84bb8aa66f76590d11702_A_7_Float = _SampleTexture2D_39637566a9c84bb8aa66f76590d11702_RGBA_0_Vector4.a;
                    surface.BaseColor = _Branch_48ee12d07e0447a8a1b11405ef31386c_Out_3_Vector3;
                    surface.Alpha = _SampleTexture2D_39637566a9c84bb8aa66f76590d11702_R_4_Float;
                    return surface;
                }
            
            // --------------------------------------------------
            // Build Graph Inputs
            #ifdef HAVE_VFX_MODIFICATION
            #define VFX_SRP_ATTRIBUTES Attributes
            #define VFX_SRP_VARYINGS Varyings
            #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
            #endif
            VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
                {
                    VertexDescriptionInputs output;
                    ZERO_INITIALIZE(VertexDescriptionInputs, output);
                
                    output.ObjectSpaceNormal =                          input.normalOS;
                    output.ObjectSpaceTangent =                         input.tangentOS.xyz;
                    output.ObjectSpacePosition =                        input.positionOS;
                #if UNITY_ANY_INSTANCING_ENABLED
                #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
                #endif
                
                    return output;
                }
                
            SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
                {
                    SurfaceDescriptionInputs output;
                    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
                
                #ifdef HAVE_VFX_MODIFICATION
                #if VFX_USE_GRAPH_VALUES
                    uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
                    /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
                #endif
                    /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
                
                #endif
                
                    
                
                
                
                
                
                    output.WorldSpaceViewDirection = GetWorldSpaceNormalizeViewDir(input.positionWS);
                    output.ObjectSpaceViewDirection = TransformWorldToObjectDir(output.WorldSpaceViewDirection);
                
                    #if UNITY_UV_STARTS_AT_TOP
                    #else
                    #endif
                
                
                    output.uv0 = input.texCoord0;
                #if UNITY_ANY_INSTANCING_ENABLED
                #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
                #endif
                #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
                #else
                #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
                #endif
                #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
                
                        return output;
                }
                
            
            // --------------------------------------------------
            // Main
            
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/PBR2DPass.hlsl"
            
            // --------------------------------------------------
            // Visual Effect Vertex Invocations
            #ifdef HAVE_VFX_MODIFICATION
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
            #endif
            
            ENDHLSL
            }
        }
        CustomEditor "UnityEditor.ShaderGraph.GenericShaderGraphMaterialGUI"
        CustomEditorForRenderPipeline "UnityEditor.ShaderGraphLitGUI" "UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset"
        FallBack "Hidden/Shader Graph/FallbackError"
    }