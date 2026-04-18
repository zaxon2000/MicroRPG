using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public GameObject target;

    [Header("Zoom")]
    [Tooltip("Orthographic size when fully zoomed in.")]
    public float minZoom = 2f;
    [Tooltip("Orthographic size when fully zoomed out.")]
    public float maxZoom = 10f;
    [Tooltip("How many world-units of ortho size change per scroll notch.")]
    public float zoomSpeed = 1f;
    [Tooltip("How fast the camera smoothly reaches the target zoom level.")]
    public float zoomSmoothSpeed = 8f;

    private Camera _cam;
    private float _targetOrthoSize;

    void Awake()
    {
        _cam = GetComponent<Camera>();
        // Initialise target size from whatever the camera starts at in the scene.
        _targetOrthoSize = _cam != null ? _cam.orthographicSize : 5f;
    }

    void LateUpdate()
    {
        if (target != null)
            transform.position = new Vector3(
                target.transform.position.x,
                target.transform.position.y,
                -10f);

        HandleZoom();
    }

    private void HandleZoom()
    {
        if (_cam == null || !_cam.orthographic) return;

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0f)
        {
            // Scrolling up (positive) zooms in; scrolling down zooms out.
            _targetOrthoSize -= scroll * zoomSpeed;
            _targetOrthoSize = Mathf.Clamp(_targetOrthoSize, minZoom, maxZoom);
        }

        // Smooth damp toward the target size every frame.
        _cam.orthographicSize = Mathf.Lerp(
            _cam.orthographicSize,
            _targetOrthoSize,
            Time.deltaTime * zoomSmoothSpeed);
    }
}