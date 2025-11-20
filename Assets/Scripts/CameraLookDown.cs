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
    public float defaultPerspectivePitch = 60f; // angle for SnapToFit in perspective

    [Header("Movement")]
    public float moveSpeed = 30f;
    public float fastMoveMultiplier = 2.5f;   // Shift multiplier

    [Header("Rotation (RMB)")]
    public bool allowRotation = true;
    public float rotateSpeed = 3f;
    public float minPitch = 20f;   // lowest angle (flatter)
    public float maxPitch = 89f;   // near top-down
    public float defaultOrthoPitch = 90f; // for SnapToFit in ortho

    private Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();
        // IMPORTANT: we do NOT touch cam.orthographic here.
        // Set Orthographic/Perspective on the Camera component itself.
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
        HandleRotation();
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

        // We’ll always reset to strict top-down
        const float TOPDOWN_PITCH = 90f;
        const float YAW = 0f; // or use transform.eulerAngles.y if you want to keep yaw

        if (cam.orthographic)
        {
            float targetSize = Mathf.Clamp(requiredHalfHeight, minOrthoSize, maxOrthoSize);
            cam.orthographicSize = targetSize;

            // Y position fixed, looking straight down
            transform.position = new Vector3(center.x, orthographicHeight, center.z);
            transform.rotation = Quaternion.Euler(TOPDOWN_PITCH, YAW, 0f);
        }
        else
        {
            float fovRad = cam.fieldOfView * Mathf.Deg2Rad;
            float height = requiredHalfHeight / Mathf.Tan(fovRad * 0.5f);
            height = Mathf.Clamp(height, minHeight, maxHeight);

            transform.position = new Vector3(center.x, height, center.z);
            transform.rotation = Quaternion.Euler(TOPDOWN_PITCH, YAW, 0f);
        }
    }


    // --- Movement: WASD, camera-relative on ground plane ---
    void HandleMovement()
    {
        Vector3 input = Vector3.zero;

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            input += Vector3.forward;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            input += Vector3.back;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            input += Vector3.left;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            input += Vector3.right;

        if (input.sqrMagnitude < 0.0001f)
            return;

        // -------------------------
        // SHIFT SPEED MULTIPLIER
        // -------------------------
        float speed = moveSpeed;
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            speed *= fastMoveMultiplier;

        // Total War–style: movement based on camera yaw, projected to XZ
        float yaw = transform.eulerAngles.y;
        Quaternion yawRot = Quaternion.Euler(0f, yaw, 0f);

        Vector3 forward = yawRot * Vector3.forward;  // W/S
        Vector3 right = yawRot * Vector3.right;    // A/D

        Vector3 moveDir =
            forward * input.z +   // W/S
            right * input.x;    // A/D

        moveDir.y = 0f;
        moveDir.Normalize();

        transform.position += moveDir * speed * Time.deltaTime;
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
            size -= scroll * orthoZoomSpeed;  // wheel is discrete; no deltaTime
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

    // --- RMB Rotation: yaw + pitch ---
    void HandleRotation()
    {
        if (!allowRotation)
            return;

        if (!Input.GetMouseButton(1)) // RMB not held
            return;

        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        // Yaw (rotate around world up)
        transform.Rotate(Vector3.up, mouseX * rotateSpeed, Space.World);

        // Pitch (tilt up/down)
        Vector3 e = transform.eulerAngles;

        // Unity uses 0..360, convert to -180..180 range for clamping
        float pitch = e.x;
        if (pitch > 180f) pitch -= 360f;

        pitch -= mouseY * rotateSpeed; // invert if it feels wrong
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        transform.eulerAngles = new Vector3(pitch, transform.eulerAngles.y, 0f);
    }
}
