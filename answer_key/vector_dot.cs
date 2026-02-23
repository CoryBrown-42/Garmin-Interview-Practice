using UnityEngine;

public static class GolfBallSimulatorUtils
{
    // Cache the main camera reference for performance.
    // Accessing Camera.main repeatedly can cause a slight overhead due to scene searching.
    // Initialize in Awake/Start of a manager script or on first access if needed.
    private static Camera _mainCamera;
    private static Camera MainCamera
    {
        get
        {
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
                if (_mainCamera == null)
                {
                    Debug.LogError("GolfBallSimulatorUtils: Main Camera not found! Ensure a camera is tagged 'MainCamera'.");
                }
            }
            return _mainCamera;
        }
    }

    // Pre-calculate the cosine squared of the 30-degree threshold angle.
    // This avoids recalculating it every time the function is called.
    private const float CONE_ANGLE_DEGREES = 30.0f;
    private static readonly float COS_CONE_ANGLE = Mathf.Cos(CONE_ANGLE_DEGREES * Mathf.Deg2Rad);
    private static readonly float COS_SQ_CONE_ANGLE = COS_CONE_ANGLE * COS_CONE_ANGLE;

    /// <summary>
    /// Determines if a ball's velocity vector is within a predefined 30-degree cone
    /// aligned with the screen's normal (camera's forward vector).
    /// </summary>
    /// <param name="ballVelocity">The current velocity vector of the golf ball.</param>
    /// <returns>True if the velocity is within the cone, false otherwise.</returns>
    public static bool IsBallVelocityWithinScreenCone(Vector3 ballVelocity)
    {
        // 1. Get the screen's normal vector.
        // This is the camera's forward vector, which points out of the screen.
        // MainCamera is a cached property to avoid repeated Camera.main lookups.
        Vector3 screenNormal = MainCamera.transform.forward;

        // 2. Handle zero velocity edge case.
        // A ball with negligible velocity cannot be considered 'within' a directional cone.
        // Using sqrMagnitude and Epsilon avoids an unnecessary sqrt and handles floating point inaccuracies.
        float velocitySqrMagnitude = ballVelocity.sqrMagnitude;
        if (velocitySqrMagnitude <= float.Epsilon) // float.Epsilon is the smallest positive float value.
        {
            return false;
        }

        // 3. Calculate the dot product between the ball velocity and the screen normal.
        // The dot product result ranges from -1 (opposite) to 1 (same direction) for unit vectors.
        // For non-unit vectors, it's |A||B|cos(theta).
        float dotProduct = Vector3.Dot(ballVelocity, screenNormal);

        // 4. Perform the optimized angle comparison.
        // We want to check if the angle theta between ballVelocity (V) and screenNormal (N) is less than 30 degrees.
        // This means cos(theta) >= cos(30 degrees).
        //
        // Original comparison: (V . N) / (|V| * |N|) >= cos(threshold)
        // Since |N| is 1 (camera.forward is unit vector): (V . N) / |V| >= cos(threshold)
        // Multiply by |V| (which is positive): (V . N) >= |V| * cos(threshold)
        // Square both sides to eliminate the square root from |V|:
        // (V . N)^2 >= |V|^2 * cos(threshold)^2
        //
        // This transformation avoids two expensive square root operations:
        // - One in `ballVelocity.normalized` (if we were to normalize `ballVelocity`).
        // - One in `ballVelocity.magnitude` (if we were to calculate |V| directly).
        //
        // dotProduct is (V . N)
        // velocitySqrMagnitude is |V|^2
        // COS_SQ_CONE_ANGLE is cos(threshold)^2
        return (dotProduct * dotProduct) >= (velocitySqrMagnitude * COS_SQ_CONE_ANGLE);
    }
}