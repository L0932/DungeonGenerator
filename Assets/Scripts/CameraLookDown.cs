using UnityEngine;

public class CameraLookDown : MonoBehaviour
{
    [Header("Target")]
    public BoxCollider dungeonBounds;    // Assign at runtime or in inspector

    [Header("Framing")]
    public float padding = 1.2f;         // Extra zoom-out margin
    public bool useOrthographic = true;  // Or perspective

    [Header("Height (Top-Down)")]
    public float orthographicHeight = 50f;   // Y position for ortho camera
    public float minHeight = 10f;            // Clamp for perspective (optional)
    public float maxHeight = 150f;

    [Header("Smoothing")]
    public bool smooth = true;
    public float smoothTime = 0.25f;

    private Camera cam;
    private Vector3 moveVelocity;
    private float sizeVelocity;
    private float heightVelocity;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void LateUpdate()
    {
        if (dungeonBounds == null)
            return;

        Bounds b = dungeonBounds.bounds;
        Vector3 center = b.center;

        // X = width, Z = depth on the ground plane
        float sizeX = b.size.x;
        float sizeZ = b.size.z;

        // Half extents we need to fit
        float halfWidth = sizeX * 0.5f;
        float halfHeight = sizeZ * 0.5f;

        // Account for screen aspect (wider screens see more horizontally)
        float aspect = cam.aspect;
        float requiredHalfHeight =
            Mathf.Max(halfHeight, halfWidth / aspect) * padding;

        if (useOrthographic)
        {
            cam.orthographic = true;

            float targetSize = requiredHalfHeight;
            float targetY = orthographicHeight;

            // Position above the center, looking straight down
            Vector3 targetPos = new Vector3(center.x, targetY, center.z);
            Quaternion targetRot = Quaternion.Euler(90f, 0f, 0f);

            if (smooth)
            {
                transform.position = Vector3.SmoothDamp(
                    transform.position,
                    targetPos,
                    ref moveVelocity,
                    smoothTime
                );

                cam.orthographicSize = Mathf.SmoothDamp(
                    cam.orthographicSize,
                    targetSize,
                    ref sizeVelocity,
                    smoothTime
                );
            }
            else
            {
                transform.position = targetPos;
                cam.orthographicSize = targetSize;
            }

            transform.rotation = targetRot;
        }
        else
        {
            cam.orthographic = false;

            float fovRad = cam.fieldOfView * Mathf.Deg2Rad;

            // With a top-down cam, half-height on ground = height * tan(FOV/2)
            float targetHeight = requiredHalfHeight / Mathf.Tan(fovRad * 0.5f);
            targetHeight = Mathf.Clamp(targetHeight, minHeight, maxHeight);

            Vector3 targetPos = new Vector3(center.x, targetHeight, center.z);
            Quaternion targetRot = Quaternion.Euler(90f, 0f, 0f);

            if (smooth)
            {
                float newY = Mathf.SmoothDamp(
                    transform.position.y,
                    targetHeight,
                    ref heightVelocity,
                    smoothTime
                );

                Vector3 pos = new Vector3(center.x, newY, center.z);
                transform.position = Vector3.SmoothDamp(
                    transform.position,
                    pos,
                    ref moveVelocity,
                    smoothTime
                );
            }
            else
            {
                transform.position = targetPos;
            }

            transform.rotation = targetRot;
        }
    }

    // Call this from your generator when dungeon is ready
    public void SetTarget(BoxCollider col)
    {
        dungeonBounds = col;

        // Optional: instant snap once when assigned
        if (!smooth && dungeonBounds != null)
        {
            LateUpdate();
        }
    }
}
