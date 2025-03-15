using System.Collections;
using UnityEngine;

public class CameraHoverManager : MonoBehaviour
{
    [Header("Camera References")]
    public Camera mainCamera;                // Main Camera
    public Transform defaultCameraTransform; // Default position/rotation pivot
    public float defaultFOV = 60f;           // Default field of view (no hover)

    [Header("Transition Settings")]
    public float cameraTransitionDuration = 2f; // Time (s) to move from one position to another
    public float orbitDistance = 2f;           // How far from pivot when orbiting
    public float orbitSpeed = 100f;            // How fast to orbit with middle-click

    // Internal state
    private Coroutine cameraMoveCoroutine;
    private bool isHovering;
    private bool isTransitioning;
    private Transform currentPivot;
    private float yaw, pitch;

    /// <summary>
    /// Called by a button when hovered: move camera to pivot, set up orbit.
    /// </summary>
    public void HoverOver(Transform pivot, float targetFOV)
    {
        isHovering = true;
        currentPivot = pivot; // We'll orbit around this pivot if the user holds middle-click

        // Move camera smoothly to that pivot
        MoveCamera(pivot, targetFOV);
    }

    /// <summary>
    /// Called by a button when hover ends: move camera back to default.
    /// </summary>
    public void ExitHover()
    {
        isHovering = false;
        currentPivot = defaultCameraTransform;
        MoveCamera(defaultCameraTransform, defaultFOV);
    }

    /// <summary>
    /// Core function to smoothly move the camera to a new pivot + FOV.
    /// </summary>
    private void MoveCamera(Transform targetTransform, float targetFOV)
    {
        // Stop any previous camera movement
        if (cameraMoveCoroutine != null)
        {
            StopCoroutine(cameraMoveCoroutine);
        }
        cameraMoveCoroutine = StartCoroutine(SmoothMoveCamera(targetTransform, targetFOV));
    }

    private IEnumerator SmoothMoveCamera(Transform target, float targetFOV)
    {
        isTransitioning = true;

        // Store starting position/rotation/FOV
        Vector3 startPos = mainCamera.transform.position;
        Quaternion startRot = mainCamera.transform.rotation;
        float startFOV = mainCamera.fieldOfView;

        // Target position/rotation/FOV
        Vector3 endPos = target.position;
        Quaternion endRot = target.rotation;
        float endFOV = targetFOV;

        float elapsedTime = 0f;
        while (elapsedTime < cameraTransitionDuration)
        {
            float t = elapsedTime / cameraTransitionDuration;
            mainCamera.transform.position = Vector3.Lerp(startPos, endPos, t);
            mainCamera.transform.rotation = Quaternion.Slerp(startRot, endRot, t);
            mainCamera.fieldOfView = Mathf.Lerp(startFOV, endFOV, t);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Snap to final position/rotation/FOV
        mainCamera.transform.position = endPos;
        mainCamera.transform.rotation = endRot;
        mainCamera.fieldOfView = endFOV;

        isTransitioning = false;
    }

    private void Update()
    {
        // Only orbit if we're hovering over a button AND not still transitioning
        if (!isTransitioning && isHovering && Input.GetMouseButton(2) && currentPivot != null)
        {
            // Get mouse movement
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            // Update yaw and pitch
            yaw += mouseX * orbitSpeed * Time.deltaTime;
            pitch -= mouseY * orbitSpeed * Time.deltaTime;
            pitch = Mathf.Clamp(pitch, -80f, 80f); // Limit vertical orbit

            // Build rotation from yaw/pitch
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);

            // Position camera in orbit around pivot
            Vector3 offset = new Vector3(0, 0, -orbitDistance);
            mainCamera.transform.position = currentPivot.position + rotation * offset;
            mainCamera.transform.LookAt(currentPivot.position);
        }
    }
}
