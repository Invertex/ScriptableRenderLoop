using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    class LitGUI : BaseLitGUI
    {
        protected static class Styles
        {
            public static string InputsText = "Inputs";

            public static GUIContent baseColorText = new GUIContent("Base Color + Opacity", "Albedo (RGB) and Opacity (A)");
            public static GUIContent baseColorSmoothnessText = new GUIContent("Base Color + Smoothness", "Albedo (RGB) and Smoothness (A)");

            public static GUIContent smoothnessMapChannelText = new GUIContent("Smoothness Source", "Smoothness texture and channel");
            public static GUIContent metallicText = new GUIContent("Metallic", "Metallic scale factor");
            public static GUIContent smoothnessText = new GUIContent("Smoothness", "Smoothness scale factor");
            public static GUIContent smoothnessRemappingText = new GUIContent("Smoothness Remapping", "Smoothness remapping");
            public static GUIContent maskMapSText = new GUIContent("Mask Map - M(R), AO(G), D(B), S(A)", "Mask map");

            public static GUIContent normalMapSpaceText = new GUIContent("Normal Map space", "");
            public static GUIContent normalMapText = new GUIContent("Normal Map", "Normal Map (BC7/BC5/DXT5(nm))");
            public static GUIContent normalMapOSText = new GUIContent("Normal Map OS", "Normal Map (BC7/DXT1/RGB)");
            public static GUIContent bentNormalMapText = new GUIContent("Bent normal map", "Use only with indirect diffuse lighting (Lightmap/lightprobe) - Cosine weighted Bent Normal Map (average unoccluded direction) (BC7/BC5/DXT5(nm))");
            public static GUIContent bentNormalMapOSText = new GUIContent("Bent normal map OS", "Use only with indirect diffuse lighting (Lightmap/lightprobe) - Bent Normal Map (BC7/DXT1/RGB)");

            public static GUIContent heightMapText = new GUIContent("Height Map (R)", "Height Map.\nFor floating point textures, min, max and base value should be 0, 1 and 0.");
            public static GUIContent heightMapCenterText = new GUIContent("Height Map Base", "Base of the heightmap in the texture (between 0 and 1)");
            public static GUIContent heightMapMinText = new GUIContent("Height Min (cm)", "Minimum value in the heightmap (in centimeters)");
            public static GUIContent heightMapMaxText = new GUIContent("Height Max (cm)", "Maximum value in the heightmap (in centimeters)");

            public static GUIContent tangentMapText = new GUIContent("Tangent Map", "Tangent Map (BC7/BC5/DXT5(nm))");
            public static GUIContent tangentMapOSText = new GUIContent("Tangent Map OS", "Tangent Map (BC7/DXT1/RGB)");
            public static GUIContent anisotropyText = new GUIContent("Anisotropy", "Anisotropy scale factor");
            public static GUIContent anisotropyMapText = new GUIContent("Anisotropy Map (R)", "Anisotropy");

            public static GUIContent UVBaseMappingText = new GUIContent("Base UV mapping", "");
            public static GUIContent texWorldScaleText = new GUIContent("World scale", "Tiling factor applied to Planar/Trilinear mapping");

            // Details
            public static string detailText = "Detail Inputs";
            public static GUIContent UVDetailMappingText = new GUIContent("Detail UV mapping", "");
            public static GUIContent detailMapNormalText = new GUIContent("Detail Map A(R) Ny(G) S(B) Nx(A)", "Detail Map");
            public static GUIContent detailAlbedoScaleText = new GUIContent("Detail AlbedoScale", "Detail Albedo Scale factor");
            public static GUIContent detailNormalScaleText = new GUIContent("Detail NormalScale", "Normal Scale factor");
            public static GUIContent detailSmoothnessScaleText = new GUIContent("Detail SmoothnessScale", "Smoothness Scale factor");

            // Subsurface
            public static GUIContent subsurfaceProfileText = new GUIContent("Subsurface profile", "A profile determines the shape of the blur filter.");
            public static GUIContent subsurfaceRadiusText = new GUIContent("Subsurface radius", "Determines the range of the blur.");
            public static GUIContent subsurfaceRadiusMapText = new GUIContent("Subsurface radius map (R)", "Determines the range of the blur.");
            public static GUIContent thicknessText = new GUIContent("Thickness", "If subsurface scattering is enabled, low values allow some light to be transmitted through the object.");
            public static GUIContent thicknessMapText = new GUIContent("Thickness map (R)", "If subsurface scattering is enabled, low values allow some light to be transmitted through the object.");

            // Clear Coat
            public static GUIContent coatCoverageText = new GUIContent("Coat Coverage", "Percentage of clear coat coverage");
            public static GUIContent coatIORText = new GUIContent("Coat IOR", "IOR of clear coat, value is [0..1] + 1.0. i.e 0.5 is IOR 1.5");

            // Specular color
            public static GUIContent specularColorText = new GUIContent("Specular Color", "Specular color (RGB)");

            // Specular occlusion
            public static GUIContent enableSpecularOcclusionText = new GUIContent("Enable Specular Occlusion from Bent normal", "Require cosine weighted bent normal and cosine weighted ambient occlusion. Specular occlusion for reflection probe");
            public static GUIContent specularOcclusionWarning = new GUIContent("Require a cosine weighted bent normal and ambient occlusion maps");

            // Emissive
            public static string lightingText = "Lighting Inputs";
            public static GUIContent emissiveText = new GUIContent("Emissive Color", "Emissive");
            public static GUIContent emissiveIntensityText = new GUIContent("Emissive Intensity", "Emissive");
            public static GUIContent albedoAffectEmissiveText = new GUIContent("Albedo Affect Emissive", "Specifies whether or not the emissive color is multiplied by the albedo.");

            public static GUIContent normalMapSpaceWarning = new GUIContent("Object space normal can't be use with triplanar mapping.");
        }

        // Lit shader is not layered but some layered materials inherit from it. In order to share code we need LitUI to account for this.
        protected const int kMaxLayerCount = 4;

        protected int       m_LayerCount = 1;
        protected string[]  m_PropertySuffixes = { "", "", "", "" };

        public enum UVBaseMapping
        {
            UV0,
            UV1,
            UV2,
            UV3,
            Planar,
            Triplanar
        }

        public enum NormalMapSpace
        {
            TangentSpace,
            ObjectSpace,
        }

        public enum HeightmapMode
        {
            Parallax,
            Displacement,
        }

        public enum UVDetailMapping
        {
            UV0,
            UV1,
            UV2,
            UV3
        }

        protected MaterialProperty[] UVBase = new MaterialProperty[kMaxLayerCount];
        protected const string kUVBase = "_UVBase";
        protected MaterialProperty[] TexWorldScale = new MaterialProperty[kMaxLayerCount];
        protected const string kTexWorldScale = "_TexWorldScale";
        protected MaterialProperty[] UVMappingMask = new MaterialProperty[kMaxLayerCount];
        protected const string kUVMappingMask = "_UVMappingMask";

        protected MaterialProperty[] baseColor = new MaterialProperty[kMaxLayerCount];
        protected const string kBaseColor = "_BaseColor";
        protected MaterialProperty[] baseColorMap = new MaterialProperty[kMaxLayerCount];
        protected const string kBaseColorMap = "_BaseColorMap";
        protected MaterialProperty[] metallic = new MaterialProperty[kMaxLayerCount];
        protected const string kMetallic = "_Metallic";
        protected MaterialProperty[] smoothness = new MaterialProperty[kMaxLayerCount];
        protected const string kSmoothness = "_Smoothness";
        protected MaterialProperty[] smoothnessRemapMin = new MaterialProperty[kMaxLayerCount];
        protected const string kSmoothnessRemapMin = "_SmoothnessRemapMin";
        protected MaterialProperty[] smoothnessRemapMax = new MaterialProperty[kMaxLayerCount];
        protected const string kSmoothnessRemapMax = "_SmoothnessRemapMax";
        protected MaterialProperty[] maskMap = new MaterialProperty[kMaxLayerCount];
        protected const string kMaskMap = "_MaskMap";
        protected MaterialProperty[] normalScale = new MaterialProperty[kMaxLayerCount];
        protected const string kNormalScale = "_NormalScale";
        protected MaterialProperty[] normalMap = new MaterialProperty[kMaxLayerCount];
        protected const string kNormalMap = "_NormalMap";
        protected MaterialProperty[] normalMapOS = new MaterialProperty[kMaxLayerCount];
        protected const string kNormalMapOS = "_NormalMapOS";
        protected MaterialProperty[] bentNormalMap = new MaterialProperty[kMaxLayerCount];
        protected const string kBentNormalMap = "_BentNormalMap";
        protected MaterialProperty[] bentNormalMapOS = new MaterialProperty[kMaxLayerCount];
        protected const string kBentNormalMapOS = "_BentNormalMapOS";
        protected MaterialProperty[] normalMapSpace = new MaterialProperty[kMaxLayerCount];
        protected const string kNormalMapSpace = "_NormalMapSpace";
        protected MaterialProperty[] heightMap = new MaterialProperty[kMaxLayerCount];
        protected const string kHeightMap = "_HeightMap";
        protected MaterialProperty[] heightAmplitude = new MaterialProperty[kMaxLayerCount];
        protected const string kHeightAmplitude = "_HeightAmplitude";
        protected MaterialProperty[] heightCenter = new MaterialProperty[kMaxLayerCount];
        protected const string kHeightCenter = "_HeightCenter";
        protected MaterialProperty[] heightMin = new MaterialProperty[kMaxLayerCount];
        protected const string kHeightMin = "_HeightMin";
        protected MaterialProperty[] heightMax = new MaterialProperty[kMaxLayerCount];
        protected const string kHeightMax = "_HeightMax";

        protected MaterialProperty[] UVDetail = new MaterialProperty[kMaxLayerCount];
        protected const string kUVDetail = "_UVDetail";
        protected MaterialProperty[] UVDetailsMappingMask = new MaterialProperty[kMaxLayerCount];
        protected const string kUVDetailsMappingMask = "_UVDetailsMappingMask";
        protected MaterialProperty[] detailMap = new MaterialProperty[kMaxLayerCount];
        protected const string kDetailMap = "_DetailMap";
        protected MaterialProperty[] detailAlbedoScale = new MaterialProperty[kMaxLayerCount];
        protected const string kDetailAlbedoScale = "_DetailAlbedoScale";
        protected MaterialProperty[] detailNormalScale = new MaterialProperty[kMaxLayerCount];
        protected const string kDetailNormalScale = "_DetailNormalScale";
        protected MaterialProperty[] detailSmoothnessScale = new MaterialProperty[kMaxLayerCount];
        protected const string kDetailSmoothnessScale = "_DetailSmoothnessScale";

        protected MaterialProperty specularColor = null;
        protected const string kSpecularColor = "_SpecularColor";
        protected MaterialProperty specularColorMap = null;
        protected const string kSpecularColorMap = "_SpecularColorMap";

        protected MaterialProperty tangentMap = null;
        protected const string kTangentMap = "_TangentMap";
        protected MaterialProperty tangentMapOS = null;
        protected const string kTangentMapOS = "_TangentMapOS";
        protected MaterialProperty anisotropy = null;
        protected const string kAnisotropy = "_Anisotropy";
        protected MaterialProperty anisotropyMap = null;
        protected const string kAnisotropyMap = "_AnisotropyMap";

        protected SubsurfaceScatteringProfile subsurfaceProfile = null;
        protected MaterialProperty subsurfaceProfileID  = null;
        protected const string     kSubsurfaceProfileID = "_SubsurfaceProfile";
        protected MaterialProperty subsurfaceRadius     = null;
        protected const string     kSubsurfaceRadius    = "_SubsurfaceRadius";
        protected MaterialProperty subsurfaceRadiusMap  = null;
        protected const string     kSubsurfaceRadiusMap = "_SubsurfaceRadiusMap";
        protected MaterialProperty thickness            = null;
        protected const string     kThickness           = "_Thickness";
        protected MaterialProperty thicknessMap         = null;
        protected const string     kThicknessMap        = "_ThicknessMap";

        protected MaterialProperty coatCoverage = null;
        protected const string kCoatCoverage = "_CoatCoverage";
        protected MaterialProperty coatIOR = null;
        protected const string kCoatIOR = "_CoatIOR";

        protected MaterialProperty emissiveColorMode = null;
        protected const string kEmissiveColorMode = "_EmissiveColorMode";
        protected MaterialProperty emissiveColor = null;
        protected const string kEmissiveColor = "_EmissiveColor";
        protected MaterialProperty emissiveColorMap = null;
        protected const string kEmissiveColorMap = "_EmissiveColorMap";
        protected MaterialProperty emissiveIntensity = null;
        protected const string kEmissiveIntensity = "_EmissiveIntensity";
        protected MaterialProperty albedoAffectEmissive = null;
        protected const string kAlbedoAffectEmissive = "_AlbedoAffectEmissive";
        protected MaterialProperty enableSpecularOcclusion = null;
        protected const string kEnableSpecularOcclusion = "_EnableSpecularOcclusion";

        protected void FindMaterialLayerProperties(MaterialProperty[] props)
        {
            for (int i = 0; i < m_LayerCount; ++i)
            {
                UVBase[i] = FindProperty(string.Format("{0}{1}", kUVBase, m_PropertySuffixes[i]), props);
                TexWorldScale[i] = FindProperty(string.Format("{0}{1}", kTexWorldScale, m_PropertySuffixes[i]), props);
                UVMappingMask[i] = FindProperty(string.Format("{0}{1}", kUVMappingMask, m_PropertySuffixes[i]), props);

                baseColor[i] = FindProperty(string.Format("{0}{1}", kBaseColor, m_PropertySuffixes[i]), props);
                baseColorMap[i] = FindProperty(string.Format("{0}{1}", kBaseColorMap, m_PropertySuffixes[i]), props);
                metallic[i] = FindProperty(string.Format("{0}{1}", kMetallic, m_PropertySuffixes[i]), props);
                smoothness[i] = FindProperty(string.Format("{0}{1}", kSmoothness, m_PropertySuffixes[i]), props);
                smoothnessRemapMin[i] = FindProperty(string.Format("{0}{1}", kSmoothnessRemapMin, m_PropertySuffixes[i]), props);
                smoothnessRemapMax[i] = FindProperty(string.Format("{0}{1}", kSmoothnessRemapMax, m_PropertySuffixes[i]), props);
                maskMap[i] = FindProperty(string.Format("{0}{1}", kMaskMap, m_PropertySuffixes[i]), props);
                normalMap[i] = FindProperty(string.Format("{0}{1}", kNormalMap, m_PropertySuffixes[i]), props);
                normalMapOS[i] = FindProperty(string.Format("{0}{1}", kNormalMapOS, m_PropertySuffixes[i]), props);
                normalScale[i] = FindProperty(string.Format("{0}{1}", kNormalScale, m_PropertySuffixes[i]), props);
                bentNormalMap[i] = FindProperty(string.Format("{0}{1}", kBentNormalMap, m_PropertySuffixes[i]), props);
                bentNormalMapOS[i] = FindProperty(string.Format("{0}{1}", kBentNormalMapOS, m_PropertySuffixes[i]), props);
                normalMapSpace[i] = FindProperty(string.Format("{0}{1}", kNormalMapSpace, m_PropertySuffixes[i]), props);
                heightMap[i] = FindProperty(string.Format("{0}{1}", kHeightMap, m_PropertySuffixes[i]), props);
                heightAmplitude[i] = FindProperty(string.Format("{0}{1}", kHeightAmplitude, m_PropertySuffixes[i]), props);
                heightMin[i] = FindProperty(string.Format("{0}{1}", kHeightMin, m_PropertySuffixes[i]), props);
                heightMax[i] = FindProperty(string.Format("{0}{1}", kHeightMax, m_PropertySuffixes[i]), props);
                heightCenter[i] = FindProperty(string.Format("{0}{1}", kHeightCenter, m_PropertySuffixes[i]), props);

                // Details
                UVDetail[i] = FindProperty(string.Format("{0}{1}", kUVDetail, m_PropertySuffixes[i]), props);
                UVDetailsMappingMask[i] = FindProperty(string.Format("{0}{1}", kUVDetailsMappingMask, m_PropertySuffixes[i]), props);
                detailMap[i] = FindProperty(string.Format("{0}{1}", kDetailMap, m_PropertySuffixes[i]), props);
                detailAlbedoScale[i] = FindProperty(string.Format("{0}{1}", kDetailAlbedoScale, m_PropertySuffixes[i]), props);
                detailNormalScale[i] = FindProperty(string.Format("{0}{1}", kDetailNormalScale, m_PropertySuffixes[i]), props);
                detailSmoothnessScale[i] = FindProperty(string.Format("{0}{1}", kDetailSmoothnessScale, m_PropertySuffixes[i]), props);
            }
        }

        protected void FindMaterialEmissiveProperties(MaterialProperty[] props)
        {
            emissiveColorMode = FindProperty(kEmissiveColorMode, props);
            emissiveColor = FindProperty(kEmissiveColor, props);
            emissiveColorMap = FindProperty(kEmissiveColorMap, props);
            emissiveIntensity = FindProperty(kEmissiveIntensity, props);
            albedoAffectEmissive = FindProperty(kAlbedoAffectEmissive, props);
            enableSpecularOcclusion = FindProperty(kEnableSpecularOcclusion, props);
        }

        protected override void FindMaterialProperties(MaterialProperty[] props)
        {
            FindMaterialLayerProperties(props);
            FindMaterialEmissiveProperties(props);

            // The next properties are only supported for regular Lit shader (not layered ones) because it's complicated to blend those parameters if they are different on a per layer basis.

            // Specular Color
            specularColor = FindProperty(kSpecularColor, props);
            specularColorMap = FindProperty(kSpecularColorMap, props);

            // Anisotropy
            tangentMap = FindProperty(kTangentMap, props);
            tangentMapOS = FindProperty(kTangentMapOS, props);
            anisotropy = FindProperty(kAnisotropy, props);
            anisotropyMap = FindProperty(kAnisotropyMap, props);

            // Sub surface
            subsurfaceProfileID = FindProperty(kSubsurfaceProfileID, props);
            subsurfaceRadius = FindProperty(kSubsurfaceRadius, props);
            subsurfaceRadiusMap = FindProperty(kSubsurfaceRadiusMap, props);
            thickness = FindProperty(kThickness, props);
            thicknessMap = FindProperty(kThicknessMap, props);

            // clear coat
            coatCoverage = FindProperty(kCoatCoverage, props);
            coatIOR = FindProperty(kCoatIOR, props);
        }

        protected void ShaderSSSInputGUI(Material material)
        {
            HDRenderPipeline hdPipeline = RenderPipelineManager.currentPipeline as HDRenderPipeline;

            if (subsurfaceProfile == null)
            {
                // Attempt to load the profile from the SSS Settings.
                int profileID = (int)subsurfaceProfileID.floatValue;

                if (0 <= profileID && profileID < hdPipeline.sssSettings.profiles.Length &&
                    hdPipeline.sssSettings.profiles[profileID] != null)
                {
                    // This is a valid profile ID.
                    subsurfaceProfile = hdPipeline.sssSettings.profiles[profileID];

                    // Refresh the ID of the profile.
                    hdPipeline.sssSettings.OnValidate();
                }
            }

            subsurfaceProfile = EditorGUILayout.ObjectField(Styles.subsurfaceProfileText, subsurfaceProfile, typeof(SubsurfaceScatteringProfile), false) as SubsurfaceScatteringProfile;

            bool validProfile = false;

            // Set the profile ID.
            if (subsurfaceProfile != null)
            {
                // Load the profile from the GUI field.
                int profileID = subsurfaceProfile.settingsIndex;

                if (0 <= profileID && profileID < hdPipeline.sssSettings.profiles.Length &&
                    hdPipeline.sssSettings.profiles[profileID] != null &&
                    hdPipeline.sssSettings.profiles[profileID] == subsurfaceProfile)
                {
                    validProfile = true;
                    material.SetInt("_SubsurfaceProfile", profileID);
                }
                else
                {
                    subsurfaceProfile = null;
                    Debug.LogError("The SSS Profile assigned to the material has an invalid index. First, add the Profile to the SSS Settings, and then reassign it to the material.");
                }
            }

            if (!validProfile)
            {
                // Disable SSS for this object.
                material.SetInt("_SubsurfaceProfile", SssConstants.SSS_NEUTRAL_PROFILE_ID);
            }

            m_MaterialEditor.ShaderProperty(subsurfaceRadius, Styles.subsurfaceRadiusText);
            m_MaterialEditor.TexturePropertySingleLine(Styles.subsurfaceRadiusMapText, subsurfaceRadiusMap);
            m_MaterialEditor.ShaderProperty(thickness, Styles.thicknessText);
            m_MaterialEditor.TexturePropertySingleLine(Styles.thicknessMapText, thicknessMap);
        }

        protected void ShaderClearCoatInputGUI()
        {
            m_MaterialEditor.ShaderProperty(coatCoverage, Styles.coatCoverageText);
            m_MaterialEditor.ShaderProperty(coatIOR, Styles.coatIORText);
        }

        protected void ShaderAnisoInputGUI()
        {
            if ((NormalMapSpace)normalMapSpace[0].floatValue == NormalMapSpace.TangentSpace)
            {
                m_MaterialEditor.TexturePropertySingleLine(Styles.tangentMapText, tangentMap);
            }
            else
            {
                m_MaterialEditor.TexturePropertySingleLine(Styles.tangentMapOSText, tangentMapOS);
            }
            m_MaterialEditor.ShaderProperty(anisotropy, Styles.anisotropyText);
            m_MaterialEditor.TexturePropertySingleLine(Styles.anisotropyMapText, anisotropyMap);
        }

        protected void DoLayerGUI(Material material, int layerIndex)
        {
            EditorGUILayout.LabelField(Styles.InputsText, EditorStyles.boldLabel);

            EditorGUI.indentLevel++;

            m_MaterialEditor.TexturePropertySingleLine(Styles.baseColorText, baseColorMap[layerIndex], baseColor[layerIndex]);

            if ( materialID == null || // Will be the case for Layered materials where we only support standard and the parameter does not exist
                (Lit.MaterialId)materialID.floatValue == Lit.MaterialId.LitStandard || (Lit.MaterialId)materialID.floatValue == Lit.MaterialId.LitAniso)
            {
                m_MaterialEditor.ShaderProperty(metallic[layerIndex], Styles.metallicText);
            }

            if(maskMap[layerIndex].textureValue == null)
            {
                m_MaterialEditor.ShaderProperty(smoothness[layerIndex], Styles.smoothnessText);

            }
            else
            {
                float remapMin = smoothnessRemapMin[layerIndex].floatValue;
                float remapMax = smoothnessRemapMax[layerIndex].floatValue;
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.MinMaxSlider(Styles.smoothnessRemappingText, ref remapMin, ref remapMax, 0.0f, 1.0f);
                if (EditorGUI.EndChangeCheck())
                {
                    smoothnessRemapMin[layerIndex].floatValue = remapMin;
                    smoothnessRemapMax[layerIndex].floatValue = remapMax;
                }
            }

            m_MaterialEditor.TexturePropertySingleLine(Styles.maskMapSText, maskMap[layerIndex]);

            m_MaterialEditor.ShaderProperty(normalMapSpace[layerIndex], Styles.normalMapSpaceText);

            // Triplanar only work with tangent space normal
            if ((NormalMapSpace)normalMapSpace[layerIndex].floatValue == NormalMapSpace.ObjectSpace && ((UVBaseMapping)UVBase[layerIndex].floatValue == UVBaseMapping.Triplanar))
            {
                EditorGUILayout.HelpBox(Styles.normalMapSpaceWarning.text, MessageType.Error);
            }

            // We have two different property for object space and tangent space normal map to allow
            // 1. to go back and forth
            // 2. to avoid the warning that ask to fix the object normal map texture (normalOS are just linear RGB texture
            if ((NormalMapSpace)normalMapSpace[layerIndex].floatValue == NormalMapSpace.TangentSpace)
            {
                m_MaterialEditor.TexturePropertySingleLine(Styles.normalMapText, normalMap[layerIndex], normalScale[layerIndex]);
                m_MaterialEditor.TexturePropertySingleLine(Styles.bentNormalMapText, bentNormalMap[layerIndex]);
            }
            else
            {
                // No scaling in object space
                m_MaterialEditor.TexturePropertySingleLine(Styles.normalMapOSText, normalMapOS[layerIndex]);
                m_MaterialEditor.TexturePropertySingleLine(Styles.bentNormalMapOSText, bentNormalMapOS[layerIndex]);
            }

            m_MaterialEditor.TexturePropertySingleLine(Styles.heightMapText, heightMap[layerIndex]);
            if (!heightMap[layerIndex].hasMixedValue && heightMap[layerIndex].textureValue != null)
            {
                EditorGUI.indentLevel++;
                m_MaterialEditor.ShaderProperty(heightMin[layerIndex], Styles.heightMapMinText);
                m_MaterialEditor.ShaderProperty(heightMax[layerIndex], Styles.heightMapMaxText);
                heightAmplitude[layerIndex].floatValue = (heightMax[layerIndex].floatValue - heightMin[layerIndex].floatValue) * 0.01f; // Conversion centimeters to meters.
                m_MaterialEditor.ShaderProperty(heightCenter[layerIndex], Styles.heightMapCenterText);
                EditorGUI.showMixedValue = false;
                EditorGUI.indentLevel--;
            }

            if(materialID != null)
            {
            switch ((Lit.MaterialId)materialID.floatValue)
            {
                case Lit.MaterialId.LitSSS:
                    ShaderSSSInputGUI(material);
                    break;
                case Lit.MaterialId.LitStandard:
                    // Nothing
                    break;
                case Lit.MaterialId.LitAniso:
                    ShaderAnisoInputGUI();
                    break;
                case Lit.MaterialId.LitSpecular:
                    m_MaterialEditor.TexturePropertySingleLine(Styles.specularColorText, specularColorMap, specularColor);
                    break;
                case Lit.MaterialId.LitClearCoat:
                    ShaderClearCoatInputGUI();
                    break;
                default:
                    Debug.Assert(false, "Encountered an unsupported MaterialID.");
                    break;
            }
            }

            EditorGUILayout.Space();
            m_MaterialEditor.ShaderProperty(UVBase[layerIndex], Styles.UVBaseMappingText);

            UVBaseMapping uvBaseMapping = (UVBaseMapping)UVBase[layerIndex].floatValue;

            float X, Y, Z, W;
            X = (uvBaseMapping == UVBaseMapping.UV0) ? 1.0f : 0.0f;
            Y = (uvBaseMapping == UVBaseMapping.UV1) ? 1.0f : 0.0f;
            Z = (uvBaseMapping == UVBaseMapping.UV2) ? 1.0f : 0.0f;
            W = (uvBaseMapping == UVBaseMapping.UV3) ? 1.0f : 0.0f;

            UVMappingMask[layerIndex].colorValue = new Color(X, Y, Z, W);
            if ((uvBaseMapping == UVBaseMapping.Planar) || (uvBaseMapping == UVBaseMapping.Triplanar))
            {
                m_MaterialEditor.ShaderProperty(TexWorldScale[layerIndex], Styles.texWorldScaleText);
            }
            m_MaterialEditor.TextureScaleOffsetProperty(baseColorMap[layerIndex]);

            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(Styles.detailText, EditorStyles.boldLabel);

            EditorGUI.indentLevel++;
            m_MaterialEditor.TexturePropertySingleLine(Styles.detailMapNormalText, detailMap[layerIndex]);

            // When Planar or Triplanar is enable the UVDetail use the same mode, so we disable the choice on UVDetail
            if (uvBaseMapping == UVBaseMapping.Planar)
            {
                EditorGUILayout.LabelField(Styles.UVDetailMappingText.text + ": Planar");
            }
            else if (uvBaseMapping == UVBaseMapping.Triplanar)
            {
                EditorGUILayout.LabelField(Styles.UVDetailMappingText.text + ": Triplanar");
            }
            else
            {
                m_MaterialEditor.ShaderProperty(UVDetail[layerIndex], Styles.UVDetailMappingText);
            }

            // Setup the UVSet for detail, if planar/triplanar is use for base, it will override the mapping of detail (See shader code)
            X = ((UVDetailMapping)UVDetail[layerIndex].floatValue == UVDetailMapping.UV0) ? 1.0f : 0.0f;
            Y = ((UVDetailMapping)UVDetail[layerIndex].floatValue == UVDetailMapping.UV1) ? 1.0f : 0.0f;
            Z = ((UVDetailMapping)UVDetail[layerIndex].floatValue == UVDetailMapping.UV2) ? 1.0f : 0.0f;
            W = ((UVDetailMapping)UVDetail[layerIndex].floatValue == UVDetailMapping.UV3) ? 1.0f : 0.0f;
            UVDetailsMappingMask[layerIndex].colorValue = new Color(X, Y, Z, W);

            m_MaterialEditor.TextureScaleOffsetProperty(detailMap[layerIndex]);
            m_MaterialEditor.ShaderProperty(detailAlbedoScale[layerIndex], Styles.detailAlbedoScaleText);
            m_MaterialEditor.ShaderProperty(detailNormalScale[layerIndex], Styles.detailNormalScaleText);
            m_MaterialEditor.ShaderProperty(detailSmoothnessScale[layerIndex], Styles.detailSmoothnessScaleText);

            EditorGUI.indentLevel--;
        }

        private void DoEmissiveGUI(Material material)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(Styles.lightingText, EditorStyles.boldLabel);
            m_MaterialEditor.ShaderProperty(enableSpecularOcclusion, Styles.enableSpecularOcclusionText);
            // TODO: display warning if we don't have bent normal (either OS or TS) and ambient occlusion
            //if (enableSpecularOcclusion.floatValue > 0.0f)
            {
                //EditorGUILayout.HelpBox(Styles.specularOcclusionWarning.text, MessageType.Error);                
            }
            EditorGUI.indentLevel++;            
            m_MaterialEditor.TexturePropertySingleLine(Styles.emissiveText, emissiveColorMap, emissiveColor);
            m_MaterialEditor.ShaderProperty(emissiveIntensity, Styles.emissiveIntensityText);
            m_MaterialEditor.ShaderProperty(albedoAffectEmissive, Styles.albedoAffectEmissiveText);
            EditorGUI.indentLevel--;
        }

        protected override void MaterialPropertiesGUI(Material material)
        {
            DoLayerGUI(material, 0);
            DoEmissiveGUI(material);
            // The parent Base.ShaderPropertiesGUI will call DoEmissionArea
        }

        protected override bool ShouldEmissionBeEnabled(Material mat)
        {
            return mat.GetFloat(kEmissiveIntensity) > 0.0f;
        }

        protected override void SetupMaterialKeywordsAndPassInternal(Material material)
        {
            SetupMaterialKeywordsAndPass(material);
        }

        // All Setup Keyword functions must be static. It allow to create script to automatically update the shaders with a script if code change
        static public void SetupMaterialKeywordsAndPass(Material material)
        {
            SetupBaseLitKeywords(material);
            SetupBaseLitMaterialPass(material);

            NormalMapSpace normalMapSpace = (NormalMapSpace)material.GetFloat(kNormalMapSpace);

            // Note: keywords must be based on Material value not on MaterialProperty due to multi-edit & material animation
            // (MaterialProperty value might come from renderer material property block)
            SetKeyword(material, "_MAPPING_PLANAR", ((UVBaseMapping)material.GetFloat(kUVBase)) == UVBaseMapping.Planar);
            SetKeyword(material, "_MAPPING_TRIPLANAR", ((UVBaseMapping)material.GetFloat(kUVBase)) == UVBaseMapping.Triplanar);
            SetKeyword(material, "_NORMALMAP_TANGENT_SPACE", (normalMapSpace == NormalMapSpace.TangentSpace));

            if (normalMapSpace == NormalMapSpace.TangentSpace)
            {
                // With details map, we always use a normal map and Unity provide a default (0, 0, 1) normal map for it
                SetKeyword(material, "_NORMALMAP", material.GetTexture(kNormalMap) || material.GetTexture(kDetailMap));
                SetKeyword(material, "_TANGENTMAP", material.GetTexture(kTangentMap));
                SetKeyword(material, "_BENTNORMALMAP", material.GetTexture(kBentNormalMap));
            }
            else // Object space
            {
                // With details map, we always use a normal map but in case of objects space there is no good default, so the result will be weird until users fix it
                SetKeyword(material, "_NORMALMAP", material.GetTexture(kNormalMapOS) || material.GetTexture(kDetailMap));
                SetKeyword(material, "_TANGENTMAP", material.GetTexture(kTangentMapOS));
                SetKeyword(material, "_BENTNORMALMAP", material.GetTexture(kBentNormalMapOS));
            }
            SetKeyword(material, "_MASKMAP", material.GetTexture(kMaskMap));
            SetKeyword(material, "_EMISSIVE_COLOR_MAP", material.GetTexture(kEmissiveColorMap));
            SetKeyword(material, "_ENABLESPECULAROCCLUSION", material.GetFloat(kEnableSpecularOcclusion) > 0.0f);
            SetKeyword(material, "_HEIGHTMAP", material.GetTexture(kHeightMap));
            SetKeyword(material, "_ANISOTROPYMAP", material.GetTexture(kAnisotropyMap));
            SetKeyword(material, "_DETAIL_MAP", material.GetTexture(kDetailMap));
            SetKeyword(material, "_SUBSURFACE_RADIUS_MAP", material.GetTexture(kSubsurfaceRadiusMap));
            SetKeyword(material, "_THICKNESSMAP", material.GetTexture(kThicknessMap));
            SetKeyword(material, "_SPECULARCOLORMAP", material.GetTexture(kSpecularColorMap));

            bool needUV2 = (UVDetailMapping)material.GetFloat(kUVDetail) == UVDetailMapping.UV2 && (UVBaseMapping)material.GetFloat(kUVBase) == UVBaseMapping.UV0;
            bool needUV3 = (UVDetailMapping)material.GetFloat(kUVDetail) == UVDetailMapping.UV3 && (UVBaseMapping)material.GetFloat(kUVBase) == UVBaseMapping.UV0;

            if (needUV3)
            {
                material.DisableKeyword("_REQUIRE_UV2");
                material.EnableKeyword("_REQUIRE_UV3");
            }
            else if (needUV2)
            {
                material.EnableKeyword("_REQUIRE_UV2");
                material.DisableKeyword("_REQUIRE_UV3");
            }
            else
            {
                material.DisableKeyword("_REQUIRE_UV2");
                material.DisableKeyword("_REQUIRE_UV3");
            }

            Lit.MaterialId materialId = (Lit.MaterialId)material.GetFloat(kMaterialID);

            SetKeyword(material, "_MATID_SSS", materialId == Lit.MaterialId.LitSSS);
            //SetKeyword(material, "_MATID_STANDARD", materialId == Lit.MaterialId.LitStandard); // See comment in Lit.shader, it is the default, we don't define it
            SetKeyword(material, "_MATID_ANISO", materialId == Lit.MaterialId.LitAniso);
            SetKeyword(material, "_MATID_SPECULAR", materialId == Lit.MaterialId.LitSpecular);
            SetKeyword(material, "_MATID_CLEARCOAT", materialId == Lit.MaterialId.LitClearCoat);
        }
    }
} // namespace UnityEditor
