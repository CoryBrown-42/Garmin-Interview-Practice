using UnityEngine;
using System.Collections.Generic;
using System.Linq; // For ordering and LINQ methods

#if UNITY_EDITOR
using UnityEditor; // Required for MenuItem
#endif

/// <summary>
/// A utility class to analyze a Unity scene's renderers and report potential draw call savings
/// through static batching, dynamic batching, and GPU instancing.
/// </summary>
public class SceneBatchingAnalyzer
{
    /// <summary>
    /// Represents the batching analysis report for a single material.
    /// </summary>
    public struct MaterialBatchingReport
    {
        public Material Material;
        public int TotalRendererCount;
        public int StaticBatchingPotentialCount; // Renderers marked 'Static'
        public int DynamicBatchingPotentialCount; // Renderers NOT static, and small enough (MeshRenderer only)
        public int InstancingPotentialCount; // Renderers using a material with 'Enable Instancing' checked

        /// <summary>
        /// The number of draw calls if no batching/instancing occurs for this material group.
        /// </summary>
        public int DrawCallsWithoutBatching => TotalRendererCount;

        /// <summary>
        /// The number of draw calls if all renderers for this material could be ideally batched/instanced into one.
        /// </summary>
        public int DrawCallsWithIdealBatching => TotalRendererCount > 0 ? 1 : 0;

        /// <summary>
        /// The potential number of draw calls saved for this material group if ideal batching/instancing is achieved.
        /// </summary>
        public int PotentialDrawCallSavings => TotalRendererCount > 1 ? TotalRendererCount - 1 : 0;

        public override string ToString()
        {
            return $"  - Material: {Material.name}\n" +
                   $"    Total Renderers: {TotalRendererCount}\n" +
                   $"    Potential Static Batching Candidates: {StaticBatchingPotentialCount}\n" +
                   $"    Potential Dynamic Batching Candidates (Small Meshes): {DynamicBatchingPotentialCount}\n" +
                   $"    Potential GPU Instancing Candidates (Material Enabled): {InstancingPotentialCount}\n" +
                   $"    Potential Draw Call Savings for this Material: {PotentialDrawCallSavings}\n";
        }
    }

    // Dynamic batching vertex limit for Built-in RP (approximate)
    private const int DynamicBatchingVertexLimit = 300;

    /// <summary>
    /// Analyzes all active renderers in the current scene and groups them by shared material.
    /// Reports how many draw calls could be saved through static, dynamic batching, and GPU instancing.
    /// </summary>
    /// <returns>A dictionary where keys are materials and values are their batching reports.</returns>
    public static Dictionary<Material, MaterialBatchingReport> AnalyzeSceneBatching()
    {
        // Use FindObjectsOfTypeAll for editor analysis, otherwise FindObjectsOfType for runtime.
        // For an analysis tool, FindObjectsOfType is acceptable, as it's not run every frame.
        Renderer[] allRenderers = GameObject.FindObjectsOfType<Renderer>();

        Debug.Log($"<color=cyan>--- Starting Scene Batching Analysis for {allRenderers.Length} Renderers ---</color>");

        // Group renderers by their shared material
        Dictionary<Material, List<Renderer>> groupedRenderers = new Dictionary<Material, List<Renderer>>();
        foreach (Renderer r in allRenderers)
        {
            // Skip disabled renderers, renderers without a material, or SkinnedMeshRenderers for this specific batching analysis
            // SkinnedMeshRenderers cannot be static batched and typically have their own rendering pipeline/animation costs.
            if (!r.enabled || r.sharedMaterial == null || r is SkinnedMeshRenderer)
            {
                continue;
            }

            // Always use sharedMaterial to avoid creating new material instances
            Material mat = r.sharedMaterial;

            if (!groupedRenderers.ContainsKey(mat))
            {
                groupedRenderers.Add(mat, new List<Renderer>());
            }
            groupedRenderers[mat].Add(r);
        }

        // Generate the detailed reports for each material group
        /*
        A dictionary is used instead of a list because each material is a unique key, and you want to group renderers and reports by material. 
        This allows fast lookup, grouping, and retrieval of batching reports for each specific material, rather than searching through a list for matches.
        It makes grouping and accessing data by material efficient and clear.
        */
        Dictionary<Material, MaterialBatchingReport> reports = new Dictionary<Material, MaterialBatchingReport>();
        int totalPotentialSavings = 0;
        int currentDrawCalls = 0;

        foreach (var entry in groupedRenderers)
        {
            Material mat = entry.Key;
            List<Renderer> renderers = entry.Value;

            MaterialBatchingReport report = new MaterialBatchingReport
            {
                Material = mat,
                TotalRendererCount = renderers.Count
            };

            currentDrawCalls++; // Each unique material typically means at least one draw call

            foreach (Renderer r in renderers)
            {
                // Check for Static Batching potential (requires GameObject to be marked static)
                if (r.gameObject.isStatic)
                {
                    // MeshRenderers are the primary candidates for static batching
                    if (r is MeshRenderer)
                    {
                        report.StaticBatchingPotentialCount++;
                    }
                }
                // Check for Dynamic Batching potential (if not static, and is a small MeshRenderer)
                // Note: Dynamic batching has other strict rules (no lightmap UVs, no real-time shadows, etc.)
                // This count is an optimistic estimate based on material and size.
                else if (r is MeshRenderer meshRenderer)
                {
                    MeshFilter meshFilter = meshRenderer.GetComponent<MeshFilter>();
                    if (meshFilter != null && meshFilter.sharedMesh != null && meshFilter.sharedMesh.vertexCount <= DynamicBatchingVertexLimit)
                    {
                        report.DynamicBatchingPotentialCount++;
                    }
                }

                // Check for GPU Instancing potential (if the material has it enabled)
                // Note: This only checks the material flag, not if the shader supports it or if Unity will actually instance.
                if (mat.enableInstancing)
                {
                    report.InstancingPotentialCount++;
                }
            }
            reports.Add(mat, report);
            totalPotentialSavings += report.PotentialDrawCallSavings;
        }

        // Print the full report to the console
        PrintBatchingReport(reports, allRenderers.Length, currentDrawCalls, totalPotentialSavings);

        return reports;
    }

    /// <summary>
    /// Prints a comprehensive batching report to the Unity console.
    /// </summary>
    private static void PrintBatchingReport(
        Dictionary<Material, MaterialBatchingReport> reports,
        int totalRenderersScanned,
        int initialDrawCallsEstimate,
        int totalPotentialSavings)
    {
        Debug.Log($"<color=cyan>--- Batching Analysis Summary ---</color>");
        Debug.Log($"Total Renderers Scanned: {totalRenderersScanned}");
        Debug.Log($"Estimated Draw Calls without Batching: {initialDrawCallsEstimate} (based on unique materials)");
        Debug.Log($"Total Potential Draw Calls Savings (Ideal Batching): <color=green>{totalPotentialSavings}</color>");
        Debug.Log($"Estimated Draw Calls with Ideal Batching: <color=green>{initialDrawCallsEstimate - totalPotentialSavings}</color>");
        Debug.Log("\n<color=orange>Detailed Report by Material:</color>");

        // Order by potential savings descending for easier identification of high-impact materials
        foreach (var entry in reports.OrderByDescending(r => r.Value.PotentialDrawCallSavings))
        {
            Debug.Log(entry.Value.ToString());
        }

        Debug.Log("<color=cyan>--- Analysis Complete ---</color>");
        Debug.Log("<b>Recommendations:</b>\n" +
                  "1. Prioritize materials with high 'Potential Draw Call Savings'.\n" +
                  "2. For 'Potential Static Batching Candidates', ensure GameObjects are marked 'Static' in the Inspector.\n" +
                  "3. For 'Potential GPU Instancing Candidates', ensure the material has 'Enable Instancing' checked and the shader supports it.\n" +
                  "4. For 'Potential Dynamic Batching Candidates', be aware of its CPU overhead. GPU Instancing is often preferred for many small objects.\n" +
                  "5. After making changes, use the Unity Profiler (Rendering -> Batches) to verify draw call reduction.\n" +
                  "6. Use platform-specific profilers (Snapdragon Profiler, Xcode Instruments) to monitor GPU performance, VRAM, and CPU usage on target devices.");
    }

#if UNITY_EDITOR
    /// <summary>
    /// Editor Menu Item to run the analysis.
    /// </summary>
    [MenuItem("Garmin/Home Tee Hero/Analyze Scene Batching")]
    public static void RunSceneBatchingAnalysisMenuItem()
    {
        AnalyzeSceneBatching();
    }
#endif
}