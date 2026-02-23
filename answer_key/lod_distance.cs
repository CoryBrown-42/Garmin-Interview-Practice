using UnityEngine;
using System.Collections.Generic;

public static class LODCalculator
{
    /// <summary>
    /// Calculates optimal LOD transition distances (or screen relative heights for LODGroup)
    /// based on an object's bounding box size and target pixel coverage thresholds.
    /// This helps prevent popping artifacts by ensuring transitions happen when objects
    /// occupy a consistent pixel height on screen.
    /// </summary>
    /// <param name="camera">The main camera viewing the scene. Required for FOV and screen height.</param>
    /// <param name="objectBounds">The world-space bounding box of the object.
    ///                        The largest dimension of its size will be used for calculation.</param>
    /// <param name="targetPixelThresholds">An array of desired pixel heights on screen for each LOD transition.
    ///                                      e.g., {100f, 30f} means transition from LOD0->LOD1 when object is 100px tall,
    ///                                      and from LOD1->LOD2 when object is 30px tall.</param>
    /// <returns>
    /// A tuple containing:
    /// 1. An array of world-space distances at which each LOD transition should occur.
    /// 2. An array of screenRelativeTransitionHeight values, suitable for Unity's LODGroup component.
    ///    The count matches targetPixelThresholds. The values are ordered from highest LOD to lowest (LOD0 -> LOD1, etc.).
    /// </returns>
    public static (float[] distances, float[] screenRelativeTransitionHeights) CalculateOptimalLODTransitions(
        Camera camera, Bounds objectBounds, float[] targetPixelThresholds)
    {
        if (camera == null)
        {
            Debug.LogError("LODCalculator: Camera is null. Cannot calculate LOD transitions.");
            return (new float[0], new float[0]);
        }
        if (targetPixelThresholds == null || targetPixelThresholds.Length == 0)
        {
            Debug.LogWarning("LODCalculator: No target pixel thresholds provided.");
            return (new float[0], new float[0]);
        }

        // Get camera properties
        float fovRadians = camera.fieldOfView * Mathf.Deg2Rad;
        float screenHeightPixels = Screen.height;

        // For orthographic cameras, distance doesn't affect screen size.
        // LOD transitions would typically be purely distance-based, not pixel-coverage based.
        // For this task, we assume perspective as FOV is given.
        if (camera.orthographic)
        {
            Debug.LogWarning("LODCalculator: Orthographic camera detected. Pixel coverage calculation is less meaningful for distance-based LODs. Returning 0 for distances and screenRelativeTransitionHeights.");
            // For a "sensible" orthographic LOD system, you'd likely define fixed distances
            // or use the 'screenRelativeTransitionHeight' as a scaling factor for the object's mesh size itself.
            // For now, returning empty to signal this isn't directly applicable for the distance calculation.
            return (new float[targetPixelThresholds.Length], new float[targetPixelThresholds.Length]);
        }
        
        // Calculate tangent of half FOV (used in perspective projection)
        float tanHalfFov = Mathf.Tan(fovRadians / 2f);
        
        // Determine the "significant" size of the object.
        // Using the largest dimension ensures that even thin-but-tall objects (like trees)
        // or wide-but-flat objects (like bunkers) are adequately considered.
        float objectVisualSize = Mathf.Max(objectBounds.size.x, objectBounds.size.y, objectBounds.size.z);

        // Avoid division by zero or near-zero if object is tiny
        if (objectVisualSize <= 0.0001f)
        {
            Debug.LogWarning($"LODCalculator: Object bounds size is too small ({objectVisualSize}). Returning very small distances.");
            var smallDistances = new float[targetPixelThresholds.Length];
            var smallHeights = new float[targetPixelThresholds.Length];
            for(int i = 0; i < targetPixelThresholds.Length; i++) {
                smallDistances[i] = 0.01f; // Arbitrary small distance
                smallHeights[i] = 1.0f; // Always visible
            }
            return (smallDistances, smallHeights);
        }

        // Pre-calculate common factors
        // 2 * tanHalfFov represents the world height of the frustum at 1 unit distance
        float frustumHeightFactor = 2f * tanHalfFov; 

        List<float> distances = new List<float>();
        List<float> screenRelativeHeights = new List<float>();

        foreach (float threshold in targetPixelThresholds)
        {
            if (threshold <= 0)
            {
                Debug.LogWarning($"LODCalculator: Target pixel threshold '{threshold}' is invalid. Skipping.");
                distances.Add(0f); // Or some default/error value
                screenRelativeHeights.Add(0f);
                continue;
            }

            // Calculate screenRelativeTransitionHeight for LODGroup
            // This is the desired percentage of screen height the object occupies at transition
            float screenRelativeHeight = threshold / screenHeightPixels;
            
            // Calculate the world-space distance for this screen relative height
            // D = objectSize / (frustumHeightFactor * screenRelativeHeight)
            float distance = objectVisualSize / (frustumHeightFactor * screenRelativeHeight);

            distances.Add(distance);
            screenRelativeHeights.Add(screenRelativeHeight);
        }

        return (distances.ToArray(), screenRelativeHeights.ToArray());
    }
}

public class GolfCourseObjectLODSetter : MonoBehaviour
{
    [Tooltip("The camera used for calculating LOD distances.")]
    public Camera mainCamera;

    [Tooltip("The LODGroup component on this object.")]
    public LODGroup lodGroup;

    [Tooltip("Desired pixel height for LOD0 -> LOD1 transition.")]
    [Range(10, 500)] public float lod0_1_PixelThreshold = 150f; 

    [Tooltip("Desired pixel height for LOD1 -> LOD2 transition.")]
    [Range(5, 200)] public float lod1_2_PixelThreshold = 50f;

    [Tooltip("Desired pixel height for LOD2 -> LOD3 transition (if applicable).")]
    [Range(1, 100)] public float lod2_3_PixelThreshold = 10f;

    [Header("Debug Info (Read Only)")]
    public float[] calculatedDistances;
    public float[] calculatedScreenRelativeHeights;


    void Start()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("No main camera found! Please assign one to GolfCourseObjectLODSetter.");
                enabled = false;
                return;
            }
        }

        if (lodGroup == null)
        {
            lodGroup = GetComponent<LODGroup>();
            if (lodGroup == null)
            {
                Debug.LogError("No LODGroup found on this GameObject! Please add one or assign.");
                enabled = false;
                return;
            }
        }

        // Get the object's bounds. This works even if the object is complex with multiple meshes
        // as Renderer.bounds combines them. If the object itself has no Renderer,
        // you might need to iterate through child renderers or manually define bounds.
        Renderer[] childRenderers = GetComponentsInChildren<Renderer>();
        if (childRenderers.Length == 0)
        {
            Debug.LogWarning($"No renderers found for {gameObject.name}. Cannot calculate bounds for LOD.");
            enabled = false;
            return;
        }

        Bounds combinedBounds = new Bounds(childRenderers[0].bounds.center, childRenderers[0].bounds.size);
        for (int i = 1; i < childRenderers.Length; i++)
        {
            combinedBounds.Encapsulate(childRenderers[i].bounds);
        }

        // Define target pixel thresholds for each transition
        // LODGroup works with LODs indexed 0, 1, 2...
        // A transition threshold is for LOD_N -> LOD_N+1
        // So if you have LOD0, LOD1, LOD2, you need two thresholds.
        // The last LOD (Culled) doesn't need a threshold, it's just what happens when all others are too small.
        List<float> thresholds = new List<float>();
        if (lodGroup.lodCount > 1) thresholds.Add(lod0_1_PixelThreshold);
        if (lodGroup.lodCount > 2) thresholds.Add(lod1_2_PixelThreshold);
        if (lodGroup.lodCount > 3) thresholds.Add(lod2_3_PixelThreshold);
        // Add more if you have more LOD levels

        var result = LODCalculator.CalculateOptimalLODTransitions(
            mainCamera, combinedBounds, thresholds.ToArray());

        calculatedDistances = result.distances;
        calculatedScreenRelativeHeights = result.screenRelativeTransitionHeights;

        // Apply calculated screenRelativeTransitionHeights to the LODGroup
        LOD[] lods = lodGroup.GetLODs();
        for (int i = 0; i < calculatedScreenRelativeHeights.Length && i < lods.Length; i++)
        {
            // Note: LODGroup's LOD array is ordered from LOD0 down to the smallest visible LOD.
            // The calculatedScreenRelativeHeights array should correspond to these transitions.
            // i.e., calculatedScreenRelativeHeights[0] is for LOD0->LOD1 transition.
            lods[i].screenRelativeTransitionHeight = calculatedScreenRelativeHeights[i];
        }

        // For the last LOD (often Culled or a very low-res impostor), its transition height
        // would be the last calculated value.
        // It's good practice to ensure transition heights are decreasing.
        // This recalculates the internal LOD distances based on the new screenRelativeTransitionHeight values.
        lodGroup.SetLODs(lods);
        lodGroup.RecalculateBounds(); // Important to ensure the LODGroup's bounds are correct after changes.

        Debug.Log($"Configured LODs for {gameObject.name}:");
        for (int i = 0; i < lods.Length; i++)
        {
            Debug.Log($"  LOD{i}: Screen Relative Height = {lods[i].screenRelativeTransitionHeight:F4}");
            if (i < calculatedDistances.Length)
            {
                 Debug.Log($"           (Approx. Distance = {calculatedDistances[i]:F2}m)");
            }
        }
    }
}