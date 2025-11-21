using UnityEngine;
using UnityEngine.EventSystems;
using static System.Net.Mime.MediaTypeNames;

public class CameraLookDown : MonoBehaviour
{
    [Header("Target (used only for SnapToFit)")]
    public BoxCollider dungeonBounds;

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
    public float fastMoveMultiplier = 2.5f;   // Shift multiplier
    public float acceleration = 10f;          // how quickly camera reaches target speed
    public float damping = 10f;               // how quickly it slows when no input

    [Header("Edge Scrolling")]
    public bool useEdgeScroll = true;
    public bool requireEdgeScrollKey = false;
    public KeyCode edgeScrollKey = KeyCode.LeftAlt;
    public float edgeSize = 20f;              // pixels from screen edge
    public float edgeScrollSpeed = 1f;        // multiplier relative to moveSpeed

    [Header("Middle Mouse Drag Pan")]
    public bool useMiddleMouseDrag = true;
    public float dragPanSpeed = 0.5f;         // higher = faster drag

    [Header("Rotation (RMB)")]
    public bool allowRotation = true;
    public float rotateSpeed = 3f;
    public float minPitch = 20f;              // lowest angle (flatter)
    public float maxPitch = 89f;              // near top-down

    [Header("Double Click Focus")]
    public bool enableDoubleClickFocus = true;
    public float doubleClickTime = 0.25f;     // seconds

    [Header("Zoom Focus")]
    public bool zoomTowardsFocus = true;
    private Transform zoomFocusTarget;

    private Camera cam;
    private Vector3 moveVelocity = Vector3.zero;
    private bool isDraggingMiddle = false;
    private Vector3 lastMousePos;

    private float lastClickTime = -1f;
    private Transform lastClickTarget;

    void Awake()
    {
        cam = GetComponent<Camera>();
        // You choose Orthographic or Perspective on the Camera component.
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
        if (!UnityEngine.Application.isFocused)
            return;

        if (dungeonBounds == null)
            return;

        // --- Input actions ---
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            SnapToFit();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ClearZoomFocus();  // unfocus, but keep current view
        }

        if (enableDoubleClickFocus)
        {
            HandleDoubleClickFocus();
        }

        HandleMovement();
        HandleZoom();
        HandleRotation();
        HandleMiddleMouseDrag();
    }

    // -------------------------------------------------------------
    // Public API (called from DGenerator)
    // -------------------------------------------------------------
    public void SetTarget(BoxCollider col)
    {
        dungeonBounds = col;

        if (dungeonBounds != null)
        {
            // Ensure cam is initialized even if Awake hasn't run yet
            if (cam == null)
                cam = GetComponent<Camera>();

            SnapToFit();
        }
    }

    // Center & frame the whole dungeon from a strict top-down view
    void SnapToFit()
    {
        ClearZoomFocus();

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

        const float TOPDOWN_PITCH = 90f;
        const float YAW = 0f;

        if (cam.orthographic)
        {
            float targetSize = Mathf.Clamp(requiredHalfHeight, minOrthoSize, maxOrthoSize);
            cam.orthographicSize = targetSize;

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

        moveVelocity = Vector3.zero;
    }

    // -------------------------------------------------------------
    // Movement (WASD + Edge + Shift speed + smoothing)
    // -------------------------------------------------------------
    void HandleMovement()
    {
        Vector2 input = Vector2.zero;

        // WASD / arrows
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            input.y += 1f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            input.y -= 1f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            input.x -= 1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            input.x += 1f;

        // Edge scrolling
        if (useEdgeScroll)
        {
            // If configured, require a key to be held (like LeftAlt)
            if (requireEdgeScrollKey && !Input.GetKey(edgeScrollKey))
            {
                // key not held -> no edge scroll
            }
            else if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                // mouse over UI -> no edge scroll
            }
            else
            {
                Vector3 mouse = Input.mousePosition;
                float w = Screen.width;
                float h = Screen.height;

                if (mouse.x <= edgeSize) input.x -= edgeScrollSpeed;
                else if (mouse.x >= w - edgeSize) input.x += edgeScrollSpeed;

                if (mouse.y <= edgeSize) input.y -= edgeScrollSpeed;
                else if (mouse.y >= h - edgeSize) input.y += edgeScrollSpeed;
            }
        }

        if (input.sqrMagnitude > 1f)
            input.Normalize();

        // Shift speed
        float speed = moveSpeed;
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            speed *= fastMoveMultiplier;

        // Camera-relative movement on XZ (Total War–style)
        float yaw = transform.eulerAngles.y;
        Quaternion yawRot = Quaternion.Euler(0f, yaw, 0f);

        Vector3 forward = yawRot * Vector3.forward;
        Vector3 right = yawRot * Vector3.right;

        Vector3 targetVelocity =
            (forward * input.y + right * input.x) * speed;

        targetVelocity.y = 0f;

        float dt = Time.deltaTime;

        if (targetVelocity.sqrMagnitude > 0.0001f)
        {
            moveVelocity = Vector3.Lerp(
                moveVelocity,
                targetVelocity,
                1f - Mathf.Exp(-acceleration * dt)
            );
        }
        else
        {
            moveVelocity = Vector3.Lerp(
                moveVelocity,
                Vector3.zero,
                1f - Mathf.Exp(-damping * dt)
            );
        }

        transform.position += moveVelocity * dt;
    }

    // -------------------------------------------------------------
    // Zoom (scroll) with "zoom to cursor" behavior
    // -------------------------------------------------------------
    void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) < 0.0001f)
            return;

        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        Ray rayBefore = cam.ScreenPointToRay(Input.mousePosition);

        if (!groundPlane.Raycast(rayBefore, out float enterBefore))
        {
            ApplyZoom(scroll);
            return;
        }

        Vector3 worldBefore = rayBefore.GetPoint(enterBefore);

        ApplyZoom(scroll);

        Ray rayAfter = cam.ScreenPointToRay(Input.mousePosition);
        if (!groundPlane.Raycast(rayAfter, out float enterAfter))
            return;

        Vector3 worldAfter = rayAfter.GetPoint(enterAfter);
        Vector3 diff = worldBefore - worldAfter;

        transform.position += diff;
    }

    void ApplyZoom(float scroll)
    {
        // If there is a focus target and feature is enabled, zoom around it
        if (zoomTowardsFocus && zoomFocusTarget != null)
        {
            Vector3 center = GetBoundsCenter(zoomFocusTarget);

            if (cam.orthographic)
            {
                float size = cam.orthographicSize;
                size -= scroll * orthoZoomSpeed;
                cam.orthographicSize = Mathf.Max(0.1f, size);

                // Always center on the object when zoom-focused
                Vector3 pos = transform.position;
                pos.x = center.x;
                pos.z = center.z;
                pos.y = orthographicHeight;
                transform.position = pos;
            }
            else
            {
                Vector3 camPos = transform.position;
                Vector3 dir = (camPos - center).normalized;

                if (dir.sqrMagnitude < 0.0001f)
                    dir = -transform.forward;

                // Adjust distance
                float distance = Vector3.Distance(camPos, center);
                distance -= scroll * perspectiveZoomSpeed;

                float minDist = 0.5f;
                distance = Mathf.Max(minDist, distance);

                Vector3 newPos = center + dir * distance;

                transform.position = newPos;
            }

            return;
        }

        // ----------- No focus target: normal zoom behavior -----------
        if (cam.orthographic)
        {
            float size = cam.orthographicSize;
            size -= scroll * orthoZoomSpeed;
            cam.orthographicSize = Mathf.Max(0.1f, size);
        }
        else
        {
            Vector3 pos = transform.position;
            pos.y -= scroll * perspectiveZoomSpeed;
            transform.position = pos;
        }
    }


    void ClearZoomFocus()
    {
        zoomFocusTarget = null;
        lastClickTime = -1f;
        lastClickTarget = null;
    }

    // -------------------------------------------------------------
    // RMB Rotation (yaw + pitch)
    // -------------------------------------------------------------
    void HandleRotation()
    {
        if (!allowRotation)
            return;

        if (!Input.GetMouseButton(1)) // RMB held
            return;

        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        // If we have a zoom focus target, orbit around it
        if (zoomTowardsFocus && zoomFocusTarget != null)
        {
            Vector3 pivot = GetBoundsCenter(zoomFocusTarget);
            Vector3 offset = transform.position - pivot;

            if (offset.sqrMagnitude < 0.0001f)
                offset = -transform.forward; // fallback

            // --- YAW: rotate around world up axis around pivot ---
            Quaternion yawRot = Quaternion.AngleAxis(mouseX * rotateSpeed, Vector3.up);
            offset = yawRot * offset;

            // --- PITCH: rotate around camera-local right axis through pivot ---
            Vector3 right = Vector3.Cross(Vector3.up, offset).normalized;
            if (right.sqrMagnitude > 0.0001f)
            {
                Quaternion pitchRot = Quaternion.AngleAxis(-mouseY * rotateSpeed, right);
                Vector3 candidateOffset = pitchRot * offset;

                // Clamp pitch by measuring angle from straight-down
                // 0°  = perfectly top-down
                // 90° = horizontal
                float pitchDown = Vector3.Angle(candidateOffset.normalized, Vector3.down);

                if (pitchDown >= minPitch && pitchDown <= maxPitch)
                {
                    offset = candidateOffset;
                }
            }

            transform.position = pivot + offset;
            transform.LookAt(pivot);
        }
        else
        {
            // --- Fallback: old behavior (rotate camera around itself) ---

            // Yaw
            transform.Rotate(Vector3.up, mouseX * rotateSpeed, Space.World);

            // Pitch
            Vector3 e = transform.eulerAngles;
            float pitch = e.x;
            if (pitch > 180f) pitch -= 360f; // convert to -180..180

            pitch -= mouseY * rotateSpeed;   // invert if you prefer opposite
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

            transform.eulerAngles = new Vector3(pitch, transform.eulerAngles.y, 0f);
        }
    }

    // -------------------------------------------------------------
    // Middle Mouse Drag Pan
    // -------------------------------------------------------------
    void HandleMiddleMouseDrag()
    {
        if (!useMiddleMouseDrag)
            return;

        if (Input.GetMouseButtonDown(2))
        {
            isDraggingMiddle = true;
            lastMousePos = Input.mousePosition;
        }
        else if (Input.GetMouseButtonUp(2))
        {
            isDraggingMiddle = false;
        }

        if (!isDraggingMiddle)
            return;

        Vector3 mouse = Input.mousePosition;
        Vector3 delta = mouse - lastMousePos;
        lastMousePos = mouse;

        float yaw = transform.eulerAngles.y;
        Quaternion yawRot = Quaternion.Euler(0f, yaw, 0f);

        Vector3 right = yawRot * Vector3.right;
        Vector3 forward = yawRot * Vector3.forward;

        // Drag screen direction
        Vector3 dragMove =
            (-right * delta.x + -forward * delta.y) * dragPanSpeed * Time.deltaTime;

        dragMove.y = 0f;

        transform.position += dragMove;
    }

    // -------------------------------------------------------------
    // Double-click focus
    // -------------------------------------------------------------
    void HandleDoubleClickFocus()
    {
        if (!enableDoubleClickFocus)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            float t = Time.time;

            // Raycast under mouse
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            Transform hitTransform = null;

            // Ignore the dungeon bounds layer
            int mask = ~LayerMask.GetMask("DungeonBounds");

            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, mask))            
            {
                hitTransform = hit.transform;
            }

            // If we clicked "empty" space, clear focus immediately
            if (hitTransform == null)
            {
                ClearZoomFocus();
                lastClickTime = -1f;
                lastClickTarget = null;
                return;
            }

            // We clicked on something:
            if (lastClickTime > 0f &&
                (t - lastClickTime) <= doubleClickTime &&
                lastClickTarget == hitTransform)
            {
                // Double-click detected on the same object
                FocusOn(hitTransform);
                lastClickTime = -1f;
                lastClickTarget = null;
            }
            else
            {
                // First click (or different target) – start tracking
                lastClickTime = t;
                lastClickTarget = hitTransform;
            }
        }
    }

    public void FocusOn(Transform target)
    {
        if (target == null) return;

        zoomFocusTarget = target;  // keep using this as zoom + orbit pivot

        Vector3 center = GetBoundsCenter(target);

        if (cam.orthographic)
        {
            Vector3 pos = transform.position;
            pos.x = center.x;
            pos.z = center.z;
            pos.y = orthographicHeight;
            transform.position = pos;
            // optional: keep rotation as-is for ortho
        }
        else
        {
            Vector3 camPos = transform.position;
            float distance = Vector3.Distance(camPos, center);

            Vector3 dir = (camPos - center).normalized;
            if (dir.sqrMagnitude < 0.0001f)
                dir = -transform.forward;

            float minDist = 0.5f;
            distance = Mathf.Max(distance, minDist);

            Vector3 newPos = center + dir * distance;
            newPos.y = Mathf.Clamp(newPos.y, minHeight, maxHeight);

            transform.position = newPos;
            transform.LookAt(center);
        }
    }

    Vector3 GetBoundsCenter(Transform t)
    {
        Renderer[] rs = t.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0)
            return t.position;

        Bounds b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++)
            b.Encapsulate(rs[i].bounds);

        return b.center;
    }
}
