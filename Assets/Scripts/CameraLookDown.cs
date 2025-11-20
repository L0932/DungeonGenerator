using UnityEngine;

public class CameraLookDown : MonoBehaviour
{
    [Header("Target")]
    public BoxCollider dungeonBounds;   // Used only for SnapToFit

    [Header("Framing")]
    public float padding = 1.2f;

    [Header("Ortho Settings")]
    public float orthographicHeight = 50f; // camera Y position in ortho
    public float minOrthoSize = 5f;
    public float maxOrthoSize = 80f;
    public float orthoZoomSpeed = 40f;

    [Header("Perspective Settings")]
    public float minHeight = 10f;   // min Y for perspective
    public float maxHeight = 150f;  // max Y for perspective
    public float perspectiveZoomSpeed = 40f;

    [Header("Movement")]
    public float moveSpeed = 30f;

    private Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();
        // IMPORTANT: we do NOT change cam.orthographic here.
        // Choose Orthographic or Perspective on the Camera component itself.
    }

    void Start()
    {
        if (dungeonBounds != null)
        {
            SnapToFit();
        }
    }

    void Update()
    {
        if (dungeonBounds == null)
            return;

        // Tab = zoom-to-fit overview
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            SnapToFit();
        }

        HandleMovement();
        HandleZoom();
    }

    // --- PUBLIC: called from DGenerator ---
    public void SetTarget(BoxCollider col)
    {
        dungeonBounds = col;

        if (dungeonBounds != null)
        {
            SnapToFit();
        }
    }

    // --- CORE: Zoom-to-fit ---
    void SnapToFit()
    {
        if (dungeonBounds == null)
            return;

        Bounds b = dungeonBounds.bounds;
        Vector3 center = b.center;

        float sizeX = b.size.x;
        float sizeZ = b.size.z;

        float halfWidth = sizeX * 0.5f;
        float halfHeight = sizeZ * 0.5f;

        float aspect = cam.aspect;
        float requiredHalfHeight =
            Mathf.Max(halfHeight, halfWidth / aspect) * padding;

        if (cam.orthographic)
        {
            float targetSize = Mathf.Clamp(requiredHalfHeight, minOrthoSize, maxOrthoSize);
            cam.orthographicSize = targetSize;

            transform.position = new Vector3(center.x, orthographicHeight, center.z);
            transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }
        else
        {
            float fovRad = cam.fieldOfView * Mathf.Deg2Rad;
            float height = requiredHalfHeight / Mathf.Tan(fovRad * 0.5f);
            height = Mathf.Clamp(height, minHeight, maxHeight);

            transform.position = new Vector3(center.x, height, center.z);
            transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }
    }

    // --- Movement: WASD ---
    void HandleMovement()
    {
        Vector3 dir = Vector3.zero;

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            dir += Vector3.forward;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            dir += Vector3.back;

        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            dir += Vector3.left;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            dir += Vector3.right;

        if (dir.sqrMagnitude < 0.0001f)
            return;

        dir.Normalize();
        transform.position += dir * moveSpeed * Time.deltaTime;
    }

    // --- Zoom: scroll wheel ---
    void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) < 0.0001f)
            return;

        if (cam.orthographic)
        {
            float size = cam.orthographicSize;
            size -= scroll * orthoZoomSpeed;  // no deltaTime; wheel is discrete
            cam.orthographicSize = Mathf.Clamp(size, minOrthoSize, maxOrthoSize);
        }
        else
        {
            Vector3 pos = transform.position;
            float height = pos.y;

            height -= scroll * perspectiveZoomSpeed;
            height = Mathf.Clamp(height, minHeight, maxHeight);

            transform.position = new Vector3(pos.x, height, pos.z);
        }
    }
}
