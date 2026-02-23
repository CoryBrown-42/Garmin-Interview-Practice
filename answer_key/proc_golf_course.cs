using UnityEngine;
using System.Collections.Generic;

public class FairwayMeshGenerator : MonoBehaviour
{
    [Tooltip("Material to apply to the generated fairway mesh.")]
    public Material fairwayMaterial;

    [Tooltip("Controls the density of the texture along the fairway. 1 means texture repeats once per world unit.")]
    [Range(0.1f, 10f)]
    public float textureRepeatRate = 1f;

    // Internal arrays for mesh data, pre-allocated and reused to avoid GC.
    // Cleared and resized as needed, but not re-created.
    private List<Vector3> vertices = new List<Vector3>();
    private List<Vector3> normals = new List<Vector3>();
    private List<Vector2> uvs = new List<Vector2>();
    private List<int> triangles = new List<int>();

    // For calculating UV 'V' coordinate based on distance
    private List<float> cumulativeDistances = new List<float>();

    /// <summary>
    /// Generates or updates a mesh for a fairway strip given center points and a width.
    /// This method is optimized for performance and GC avoidance on mobile.
    /// </summary>
    /// <param name="centerPoints">A list of Vector3 points defining the center line of the fairway.</param>
    /// <param name="width">The total width of the fairway.</param>
    /// <param name="meshToPopulate">The Mesh object to populate. If null, a new Mesh will be created.
    /// Recommended to pass an existing Mesh for GC avoidance when updating.</param>
    /// <returns>The generated/updated Mesh object.</returns>
    public Mesh GenerateFairwayMesh(List<Vector3> centerPoints, float width, Mesh meshToPopulate = null)
    {
        if (centerPoints == null || centerPoints.Count < 2)
        {
            Debug.LogWarning("Fairway generation requires at least 2 center points.");
            return null;
        }

        // --- 1. Initialize Mesh and Clear Data ---
        Mesh mesh = meshToPopulate;
        if (mesh == null)
        {
            mesh = new Mesh();
            mesh.name = "FairwayMesh";
            // For meshes with > 65535 vertices (unlikely for a single fairway strip, but good practice)
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; 
        }
        else
        {
            mesh.Clear(); // Clear existing mesh data
        }

        // Clear internal lists without reallocating their capacity
        vertices.Clear();
        normals.Clear();
        uvs.Clear();
        triangles.Clear();
        cumulativeDistances.Clear();

        // --- 2. Calculate Cumulative Distances for UVs ---
        float totalLength = 0f;
        cumulativeDistances.Add(0f); // First point starts at distance 0

        for (int i = 0; i < centerPoints.Count - 1; i++)
        {
            float segmentLength = Vector3.Distance(centerPoints[i], centerPoints[i + 1]);
            totalLength += segmentLength;
            cumulativeDistances.Add(totalLength);
        }

        // --- 3. Generate Vertices, Normals, and UVs ---
        int numPoints = centerPoints.Count;
        int vertexIndex = 0;

        for (int i = 0; i < numPoints; i++)
        {
            Vector3 currentPoint = centerPoints[i];
            Vector3 segmentForward;

            // Calculate forward direction for the current point (mitered joints for smoother turns)
            if (i == 0)
            {
                segmentForward = (centerPoints[i + 1] - currentPoint).normalized;
            }
            else if (i == numPoints - 1)
            {
                segmentForward = (currentPoint - centerPoints[i - 1]).normalized;
            }
            else
            {
                Vector3 prevSegment = (currentPoint - centerPoints[i - 1]).normalized;
                Vector3 nextSegment = (centerPoints[i + 1] - currentPoint).normalized;
                segmentForward = (prevSegment + nextSegment).normalized;
            }

            // Calculate the perpendicular direction (flat on the XY plane, assuming Y is Up)
            // Cross product with Vector3.up gives a vector perpendicular to both, lying on the XZ plane.
            Vector3 perpendicular = Vector3.Cross(segmentForward, Vector3.up).normalized;

            // Calculate the left and right vertices
            Vector3 leftVertex = currentPoint - perpendicular * (width / 2f);
            Vector3 rightVertex = currentPoint + perpendicular * (width / 2f);

            vertices.Add(leftVertex);
            vertices.Add(rightVertex);

            // Normals typically point up for a flat fairway surface
            normals.Add(Vector3.up);
            normals.Add(Vector3.up);

            // UVs: U = 0 for left, U = 1 for right (across width)
            // V = cumulative distance along path, scaled by textureRepeatRate for consistent tiling
            float vCoord = cumulativeDistances[i] * textureRepeatRate;
            uvs.Add(new Vector2(0f, vCoord)); // Left vertex UV
            uvs.Add(new Vector2(1f, vCoord)); // Right vertex UV

            // --- 4. Generate Triangles (for all segments except the last point) ---
            if (i < numPoints - 1)
            {
                // Current segment vertices:
                // v0 = left of current point
                // v1 = right of current point
                // Next segment vertices:
                // v2 = left of next point
                // v3 = right of next point

                int v0 = vertexIndex;     // Current Left
                int v1 = vertexIndex + 1; // Current Right
                int v2 = vertexIndex + 2; // Next Left
                int v3 = vertexIndex + 3; // Next Right

                // First triangle of the quad:
                triangles.Add(v0);
                triangles.Add(v2);
                triangles.Add(v1);

                // Second triangle of the quad:
                triangles.Add(v2);
                triangles.Add(v3);
                triangles.Add(v1);
            }

            vertexIndex += 2; // Move to the next pair of vertices
        }

        // --- 5. Assign Data to Mesh ---
        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs); // Use UV channel 0
        mesh.SetTriangles(triangles, 0); // Assign to submesh 0

        // --- 6. Final Mesh Optimizations ---
        mesh.RecalculateBounds(); // Crucial for frustum culling
        mesh.Optimize(); // Attempts to optimize vertex and triangle order for GPU cache

        return mesh;
    }

    /// <summary>
    /// Example usage of the generator. Creates a simple curved fairway.
    /// </summary>
    [ContextMenu("Generate Example Fairway")]
    public void GenerateExampleFairway()
    {
        // Example center points for a curved path
        List<Vector3> pathPoints = new List<Vector3>();
        pathPoints.Add(new Vector3(0, 0, 0));
        pathPoints.Add(new Vector3(5, 0, 10));
        pathPoints.Add(new Vector3(10, 0, 20));
        pathPoints.Add(new Vector3(12, 0, 30));
        pathPoints.Add(new Vector3(10, 0, 40));
        pathPoints.Add(new Vector3(5, 0, 50));
        pathPoints.Add(new Vector3(0, 0, 60));
        pathPoints.Add(new Vector3(-5, 0, 70));
        pathPoints.Add(new Vector3(-10, 0, 80));

        float fairwayWidth = 10f;

        // Ensure there's a MeshFilter and MeshRenderer on this GameObject
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null) meshFilter = gameObject.AddComponent<MeshFilter>();

        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer == null) meshRenderer = gameObject.AddComponent<MeshRenderer>();

        // Generate the mesh
        Mesh fairwayMesh = GenerateFairwayMesh(pathPoints, fairwayWidth, meshFilter.sharedMesh);
        meshFilter.sharedMesh = fairwayMesh;

        // Apply material
        if (fairwayMaterial != null)
        {
            meshRenderer.sharedMaterial = fairwayMaterial;
        }
        else
        {
            Debug.LogWarning("No material assigned to FairwayMeshGenerator. Assign one in the inspector.");
        }

        // IMPORTANT for static batching: Mark the GameObject as static.
        // This *must* be done if you want static batching.
        gameObject.isStatic = true;
    }
}