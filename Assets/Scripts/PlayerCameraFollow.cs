using UnityEngine;

/// <summary>
/// Smoothly moves the scene's main camera to follow this object (the player).
/// Lives on the Player prefab so no scene changes are needed. Keeps the camera's own Z.
/// </summary>
[DisallowMultipleComponent]
public class PlayerCameraFollow : MonoBehaviour
{
    [Tooltip("Camera to move. Uses Camera.main if empty.")]
    [SerializeField] private Camera targetCamera;

    [Tooltip("Approximate time in seconds the camera takes to catch up. Lower = snappier.")]
    [SerializeField, Min(0.01f)] private float smoothTime = 0.15f;

    [Tooltip("Offset from the player, in world units.")]
    [SerializeField] private Vector2 offset;

    private Transform cameraTransform;
    private Vector3 velocity;
    private bool placed;

    private void Start()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (targetCamera == null)
        {
            Debug.LogError("PlayerCameraFollow: no camera found. Tag a camera MainCamera or assign one.", this);
            enabled = false;
            return;
        }

        cameraTransform = targetCamera.transform;
    }

    // LateUpdate so the camera moves after the player has finished moving this frame.
    private void LateUpdate()
    {
        // First frame: jump straight onto the player. By now every Start() has run, so the player has
        // already snapped to its grid cell and the camera does not drift in from its scene position.
        if (!placed)
        {
            cameraTransform.position = Goal();
            velocity = Vector3.zero;
            placed = true;
            return;
        }

        cameraTransform.position = Vector3.SmoothDamp(cameraTransform.position, Goal(), ref velocity, smoothTime);
    }

    private Vector3 Goal()
    {
        Vector3 p = transform.position;
        return new Vector3(p.x + offset.x, p.y + offset.y, cameraTransform.position.z);
    }
}
