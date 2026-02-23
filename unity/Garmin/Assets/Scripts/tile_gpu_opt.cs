using System.Collections.Generic;
using UnityEngine;

public static class MaterialAnalyzer
{
    /// <summary>
    /// Analyzes a list of materials and returns those that would cause overdraw issues
    /// on tile-based mobile GPUs.
    ///
    /// Overdraw issues are typically caused by transparent materials (performing alpha blending)
    /// that are rendered late in the frame (high render queue), especially when they cover
    /// large screen areas or overlap significantly.
    /// </summary>
    /// <param name="materials">The list of materials to analyze.</param>
    /// <returns>A list of materials identified as potential overdraw culprits.</returns>
    public static List<Material> FindOverdrawCulprits(List<Material> materials)
    {
        List<Material> overdrawCulprits = new List<Material>();

        if (materials == null || materials.Count == 0)
        {
            return overdrawCulprits;
        }

        foreach (Material material in materials)
        {
            if (material == null)
            {
                continue;
            }

            // Built-in Render Pipeline standard render queues:
            // Geometry = 2000 (default for opaque objects)
            // AlphaTest = 2450 (for materials with alpha cutoff, rendered with opaque)
            // Transparent = 3000 (default for blended transparent objects)
            // Overlay = 4000 (for UI elements, gizmos, post-fx overlays)
            //
            // We are looking for materials that are explicitly marked as transparent
            // and are rendered after the main opaque geometry.
            if (material.renderQueue >= (int)UnityEngine.Rendering.RenderQueue.Transparent)
            {
                // The RenderType tag is a robust way to determine a shader's intended rendering behavior.
                // "Transparent" and "Fade" indicate alpha blending, which causes overdraw.
                // "Opaque" and "Cutout" do not cause blending overdraw in the same problematic way.
                // We use 'false' for checkFallbacks for performance and to get the direct tag.
                string renderType = material.GetTag("RenderType", false);

                if (renderType == "Transparent" || renderType == "Fade")
                {
                    // This material performs alpha blending and is scheduled to render after opaque geometry.
                    // This configuration is a prime candidate for generating overdraw on tile-based GPUs.
                    overdrawCulprits.Add(material);
                }
            }
            // Note: Materials with RenderQueue.AlphaTest (2450) are technically opaque
            // but can also contribute to GPU load if the alpha test discards many fragments
            // after expensive fragment shader execution. However, they are not "transparent"
            // and do not cause blending overdraw in the same manner requested by the prompt.
        }

        return overdrawCulprits;
    }
}