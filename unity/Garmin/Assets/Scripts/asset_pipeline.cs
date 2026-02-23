
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;

public static class GarminTextureAuditor
{
    private const int MaxAllowedTextureSize = 512; // This is a common guideline for mobile performance, but you can adjust it as needed.

    [MenuItem("Tools/Garmin/Texture Auditor - Scan Folder")]
    public static void RunTextureAuditor()
    {
        string selectedPath = EditorUtility.OpenFolderPanel("Select Texture Folder", "Assets", "");

        if (string.IsNullOrEmpty(selectedPath))
        {
            Debug.LogWarning("Garmin Texture Auditor: No folder selected. Operation cancelled.");
            return;
        }

        // Ensure the path is relative to the project for AssetDatabase
        if (!selectedPath.StartsWith(Application.dataPath))
        {
            Debug.LogError("Garmin Texture Auditor: Please select a folder within your Unity project (e.g., inside 'Assets/').");
            return;
        }

        string relativePath = "Assets" + selectedPath.Substring(Application.dataPath.Length);

        Debug.Log($"Garmin Texture Auditor: Starting scan in folder: <color=yellow>{relativePath}</color>");

        List<string> oversizedTextures = new List<string>();
        List<string> mipmapEnabledTextures = new List<string>();
        int totalTexturesScanned = 0;

        // Find all texture assets in the selected folder and its subfolders
        // "t:Texture" is the asset type filter for textures
        string[] guids = AssetDatabase.FindAssets("t:Texture", new[] { relativePath });

        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);

            // We need the TextureImporter to access import settings like maxTextureSize and mipmapEnabled
            TextureImporter textureImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;

            if (textureImporter == null)
            {
                // This might happen if a non-texture asset was somehow included in the GUID list
                // or if the asset is not a standard TextureImporter type.
                continue;
            }

            totalTexturesScanned++;

            bool changed = false;

            // Check for oversized textures
            if (textureImporter.maxTextureSize > MaxAllowedTextureSize)
            {
                oversizedTextures.Add($"{assetPath} (Current: {textureImporter.maxTextureSize}px)");
                Debug.LogWarning($"<color=red>[OVERSIZED TEXTURE]</color> {assetPath} has maxTextureSize of {textureImporter.maxTextureSize}px (allowed: {MaxAllowedTextureSize}px).");
                textureImporter.maxTextureSize = MaxAllowedTextureSize;
                changed = true;
            }

            // Check for mipmaps enabled
            if (textureImporter.mipmapEnabled)
            {
                mipmapEnabledTextures.Add(assetPath);
                Debug.LogWarning($"<color=orange>[MIPMAPS ENABLED]</color> {assetPath} has mipmaps enabled.");
                //textureImporter.mipmapEnabled = false;
                //changed = true;
            }

            // Apply changes if any
            if (changed)
            {
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                Debug.Log($"<color=cyan>Updated import settings for:</color> {assetPath}");
            }
        }

        Debug.Log("\n--- Garmin Texture Auditor Summary ---");
        Debug.Log($"Total textures scanned: {totalTexturesScanned}");

        if (oversizedTextures.Count > 0)
        {
            Debug.LogWarning($"<color=red>Found {oversizedTextures.Count} oversized textures (maxTextureSize > {MaxAllowedTextureSize}px):</color>");
            foreach (string textureInfo in oversizedTextures)
            {
                Debug.LogWarning($"- {textureInfo}");
            }
        }
        else
        {
            Debug.Log($"<color=green>No oversized textures found (all maxTextureSize <= {MaxAllowedTextureSize}px).</color>");
        }

        if (mipmapEnabledTextures.Count > 0)
        {
            Debug.LogWarning($"<color=orange>Found {mipmapEnabledTextures.Count} textures with mipmaps enabled:</color>");
            foreach (string texturePath in mipmapEnabledTextures)
            {
                Debug.LogWarning($"- {texturePath}");
            }
        }
        else
        {
            Debug.Log($"<color=green>No textures found with mipmaps enabled.</color>");
        }

        if (oversizedTextures.Count == 0 && mipmapEnabledTextures.Count == 0)
        {
            Debug.Log("<color=green>Garmin Texture Auditor: All textures in the selected folder conform to performance guidelines!</color>");
        }
        else
        {
            Debug.Log("<color=yellow>Garmin Texture Auditor: Please address the warnings above for optimal mobile performance.</color>");
        }

        Debug.Log("--- End of Garmin Texture Auditor Summary ---");
    }
}