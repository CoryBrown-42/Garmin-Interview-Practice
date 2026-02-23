using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class PBRMaterialValidator : EditorWindow
{
    private Vector2 scrollPosition;
    private List<ValidationResult> results = new List<ValidationResult>();
    private bool showPassed = false;

    // Configuration thresholds (persisted with EditorPrefs)
    private float albedoMinBrightness = 0.1f;
    private float albedoMaxBrightness = 0.8f;
    private float metallicBinaryThreshold = 0.05f; // % deviation from 0 or 1
    private float metallicMaxNonBinaryPercentage = 5.0f; // Max % of pixels allowed to be non-binary

    private const string ALBEDO_MIN_KEY = "PBRValidator_AlbedoMin";
    private const string ALBEDO_MAX_KEY = "PBRValidator_AlbedoMax";
    private const string METALLIC_BINARY_THRESHOLD_KEY = "PBRValidator_MetallicBinaryThreshold";
    private const string METALLIC_MAX_NON_BINARY_KEY = "PBRValidator_MetallicMaxNonBinary";

    [MenuItem("Tools/Garmin/PBR Material Validator")]
    public static void ShowWindow()
    {
        GetWindow<PBRMaterialValidator>("PBR Material Validator").LoadSettings();
    }

    private void OnEnable()
    {
        LoadSettings();
    }

    private void LoadSettings()
    {
        albedoMinBrightness = EditorPrefs.GetFloat(ALBEDO_MIN_KEY, 0.1f);
        albedoMaxBrightness = EditorPrefs.GetFloat(ALBEDO_MAX_KEY, 0.8f);
        metallicBinaryThreshold = EditorPrefs.GetFloat(METALLIC_BINARY_THRESHOLD_KEY, 0.05f);
        metallicMaxNonBinaryPercentage = EditorPrefs.GetFloat(METALLIC_MAX_NON_BINARY_KEY, 5.0f);
    }

    private void SaveSettings()
    {
        EditorPrefs.SetFloat(ALBEDO_MIN_KEY, albedoMinBrightness);
        EditorPrefs.SetFloat(ALBEDO_MAX_KEY, albedoMaxBrightness);
        EditorPrefs.SetFloat(METALLIC_BINARY_THRESHOLD_KEY, metallicBinaryThreshold);
        EditorPrefs.SetFloat(METALLIC_MAX_NON_BINARY_KEY, metallicMaxNonBinaryPercentage);
    }

    void OnGUI()
    {
        GUILayout.Label("PBR Material Validation Settings", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        albedoMinBrightness = EditorGUILayout.Slider("Albedo Min Brightness", albedoMinBrightness, 0.0f, 1.0f);
        albedoMaxBrightness = EditorGUILayout.Slider("Albedo Max Brightness", albedoMaxBrightness, 0.0f, 1.0f);
        EditorGUILayout.MinMaxSlider("Albedo Brightness Range", ref albedoMinBrightness, ref albedoMaxBrightness, 0.0f, 1.0f);

        EditorGUILayout.Space();
        metallicBinaryThreshold = EditorGUILayout.Slider("Metallic Binary Epsilon", metallicBinaryThreshold, 0.001f, 0.2f);
        metallicMaxNonBinaryPercentage = EditorGUILayout.Slider("Metallic Max Non-Binary %", metallicMaxNonBinaryPercentage, 0.0f, 20.0f);
        if (EditorGUI.EndChangeCheck())
        {
            SaveSettings();
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Validate All Materials in Scene"))
        {
            ValidateMaterials();
        }

        EditorGUILayout.Space();

        showPassed = EditorGUILayout.Toggle("Show Passed Materials", showPassed);

        GUILayout.Label($"Validation Results ({results.Count} issues found)", EditorStyles.boldLabel);
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        foreach (var result in results)
        {
            if (result.Type == ResultType.Passed && !showPassed)
                continue;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.ObjectField("GameObject:", result.GameObject, typeof(GameObject), true);
            EditorGUILayout.ObjectField("Material:", result.Material, typeof(Material), true);
            EditorGUILayout.LabelField("Type:", result.Type.ToString());
            EditorGUILayout.HelpBox(result.Message, GetMessageType(result.Type));

            if (result.FixAction != null)
            {
                if (GUILayout.Button("Fix This Issue"))
                {
                    result.FixAction();
                    // Re-validate to update the UI
                    ValidateMaterials();
                    break; // Break to avoid modifying the list while iterating
                }
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

        EditorGUILayout.EndScrollView();
    }

    private MessageType GetMessageType(ResultType type)
    {
        switch (type)
        {
            case ResultType.Error: return MessageType.Error;
            case ResultType.Warning: return MessageType.Warning;
            case ResultType.Info: return MessageType.Info;
            case ResultType.Passed: return MessageType.None; // No specific icon for passed
            default: return MessageType.None;
        }
    }

    private void ValidateMaterials()
    {
        results.Clear();
        int validatedCount = 0;
        Renderer[] renderers = FindObjectsOfType<Renderer>();

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null) continue;

            foreach (Material material in renderer.sharedMaterials)
            {
                if (material == null) continue;

                EditorUtility.DisplayProgressBar("Validating Materials", $"Processing {renderer.name} - {material.name}", (float)validatedCount / renderers.Length);

                // Check if it's a PBR-like shader (Built-in Standard or similar)
                // We're looking for common properties used in PBR materials
                bool isPBRShader = material.HasProperty("_MainTex") && material.HasProperty("_MetallicGlossMap");

                if (!isPBRShader)
                {
                    results.Add(new ValidationResult(renderer.gameObject, material, ResultType.Info,
                        $"Material uses a non-PBR or unknown shader '{material.shader.name}'. Skipping detailed PBR checks."));
                    continue;
                }

                // Albedo Brightness Check
                ValidateAlbedoBrightness(renderer.gameObject, material);

                // Metallic Map Check
                ValidateMetallicMap(renderer.gameObject, material);

                // Normal Map Type Check
                ValidateNormalMapType(renderer.gameObject, material);

                validatedCount++;
            }
        }

        EditorUtility.ClearProgressBar();
        Debug.Log($"PBR Material Validation Complete. Found {results.Count(r => r.Type != ResultType.Passed)} issues.");
    }

    private void ValidateAlbedoBrightness(GameObject go, Material material)
    {
        Texture2D albedoTex = material.mainTexture as Texture2D; // _MainTex is default albedo

        if (albedoTex == null)
        {
            results.Add(new ValidationResult(go, material, ResultType.Warning,
                "No Albedo (Main) texture assigned. Cannot validate brightness."));
            return;
        }

        if (!albedoTex.isReadable)
        {
            results.Add(new ValidationResult(go, material, ResultType.Warning,
                $"Albedo texture '{albedoTex.name}' is not readable. Cannot validate brightness. " +
                "Enable 'Read/Write Enabled' in texture import settings if you wish to validate."));
            return;
        }

        float averageBrightness = GetAverageBrightness(albedoTex);
        if (averageBrightness < albedoMinBrightness || averageBrightness > albedoMaxBrightness)
        {
            results.Add(new ValidationResult(go, material, ResultType.Error,
                $"Albedo texture '{albedoTex.name}' has an average brightness ({averageBrightness:F2}) outside the optimal range [{albedoMinBrightness:F2}-{albedoMaxBrightness:F2}]. " +
                "This might violate energy conservation or lead to incorrect lighting."));
        }
        else
        {
            results.Add(new ValidationResult(go, material, ResultType.Passed,
                $"Albedo texture '{albedoTex.name}' average brightness ({averageBrightness:F2}) is within range."));
        }
    }

    private float GetAverageBrightness(Texture2D texture)
    {
        Color[] pixels = texture.GetPixels(); // Editor only: GC is acceptable
        float totalBrightness = 0f;
        foreach (Color pixel in pixels)
        {
            // Luminance calculation: 0.2126*R + 0.7152*G + 0.0722*B
            totalBrightness += (pixel.r * 0.2126f + pixel.g * 0.7152f + pixel.b * 0.0722f);
        }
        return totalBrightness / pixels.Length;
    }

    private void ValidateMetallicMap(GameObject go, Material material)
    {
        // For Standard shader, metallic is often in the R channel of _MetallicGlossMap
        // or directly in _Metallic if it's a float/slider.
        Texture2D metallicTex = material.GetTexture("_MetallicGlossMap") as Texture2D;
        if (metallicTex == null)
        {
            // If no texture, check metallic slider value
            if (material.HasProperty("_Metallic"))
            {
                float metallicValue = material.GetFloat("_Metallic");
                if (Mathf.Abs(metallicValue - 0f) > metallicBinaryThreshold && Mathf.Abs(metallicValue - 1f) > metallicBinaryThreshold)
                {
                    results.Add(new ValidationResult(go, material, ResultType.Warning,
                        $"Metallic slider value ({metallicValue:F2}) is not binary-ish (0 or 1). " +
                        "Consider using true 0 for dielectrics and 1 for metals for physical accuracy."));
                }
                else
                {
                    results.Add(new ValidationResult(go, material, ResultType.Passed,
                        $"Metallic slider value ({metallicValue:F2}) is binary-ish."));
                }
            }
            else
            {
                results.Add(new ValidationResult(go, material, ResultType.Info,
                    "No Metallic texture or slider found. Skipping metallic validation."));
            }
            return;
        }

        if (!metallicTex.isReadable)
        {
            results.Add(new ValidationResult(go, material, ResultType.Warning,
                $"Metallic texture '{metallicTex.name}' is not readable. Cannot validate values. " +
                "Enable 'Read/Write Enabled' in texture import settings if you wish to validate."));
            return;
        }

        Color[] pixels = metallicTex.GetPixels(); // Editor only: GC is acceptable
        int nonBinaryPixels = 0;
        foreach (Color pixel in pixels)
        {
            // Metallic value is typically in the Red channel of _MetallicGlossMap
            float metallicValue = pixel.r;
            if (Mathf.Abs(metallicValue - 0f) > metallicBinaryThreshold && Mathf.Abs(metallicValue - 1f) > metallicBinaryThreshold)
            {
                nonBinaryPixels++;
            }
        }

        float nonBinaryPercentage = (float)nonBinaryPixels / pixels.Length * 100f;

        if (nonBinaryPercentage > metallicMaxNonBinaryPercentage)
        {
            results.Add(new ValidationResult(go, material, ResultType.Warning,
                $"Metallic texture '{metallicTex.name}' has {nonBinaryPercentage:F2}% pixels that are not binary-ish (0 or 1). " +
                "PBR metallic maps should ideally be purely 0 (dielectric) or 1 (metal). This can lead to unphysical materials."));
        }
        else
        {
            results.Add(new ValidationResult(go, material, ResultType.Passed,
                $"Metallic texture '{metallicTex.name}' has {nonBinaryPercentage:F2}% non-binary pixels (within tolerance)."));
        }
    }

    private void ValidateNormalMapType(GameObject go, Material material)
    {
        Texture2D normalMapTex = material.GetTexture("_BumpMap") as Texture2D; // _BumpMap is default normal map
        if (normalMapTex == null)
        {
            results.Add(new ValidationResult(go, material, ResultType.Info,
                "No Normal Map (BumpMap) assigned. Skipping normal map type validation."));
            return;
        }

        string assetPath = AssetDatabase.GetAssetPath(normalMapTex);
        if (string.IsNullOrEmpty(assetPath))
        {
            results.Add(new ValidationResult(go, material, ResultType.Warning,
                $"Normal Map texture '{normalMapTex.name}' is not an asset (e.g., procedural). Cannot validate import type."));
            return;
        }

        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            results.Add(new ValidationResult(go, material, ResultType.Error,
                $"Could not get TextureImporter for Normal Map '{normalMapTex.name}'."));
            return;
        }

        if (importer.textureType != TextureImporterType.NormalMap)
        {
            results.Add(new ValidationResult(go, material, ResultType.Error,
                $"Normal Map texture '{normalMapTex.name}' is incorrectly set to '{importer.textureType}'. It should be 'Normal Map' type for correct PBR shading.",
                () => FixNormalMapType(importer)));
        }
        else
        {
            results.Add(new ValidationResult(go, material, ResultType.Passed,
                $"Normal Map texture '{normalMapTex.name}' has the correct 'Normal Map' type."));
        }
    }

    private void FixNormalMapType(TextureImporter importer)
    {
        importer.textureType = TextureImporterType.NormalMap;
        importer.SaveAndReimport();
        Debug.Log($"Fixed normal map type for '{importer.assetPath}' to Normal Map.");
    }

    public enum ResultType
    {
        Passed,
        Info,
        Warning,
        Error
    }

    public class ValidationResult
    {
        public GameObject GameObject;
        public Material Material;
        public ResultType Type;
        public string Message;
        public System.Action FixAction;

        public ValidationResult(GameObject go, Material mat, ResultType type, string message, System.Action fixAction = null)
        {
            GameObject = go;
            Material = mat;
            Type = type;
            Message = message;
            FixAction = fixAction;
        }
    }
}