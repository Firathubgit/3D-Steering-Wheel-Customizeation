using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ToggleCameraTransition : MonoBehaviour
{
    [Header("Camera Transition Settings")]
    public Camera targetCamera;               // The camera to control
    public Transform targetTransform;         // The target (spawn) location for state B
    public float transitionDuration = 2f;     // Duration for the camera transition
    public float targetFOVValue = 70f;        // The FOV when transitioning to state B

    [Header("Zoom Settings (Using FOV)")]
    public float zoomSpeed = 10f;             // How fast the FOV adjusts
    public float minFOV = 30f;                // Minimum FOV (zoomed in)
    public float maxFOV = 90f;                // Maximum FOV (zoomed out)
    public float fovLerpSpeed = 5f;           // Smoothing factor for FOV changes
    private float currentFOV;               // Our target FOV value (modified via scroll)

    [Header("Light Settings")]
    public Light[] lightsToTurnOff;           // Lights that are on in state A (fade to 0 in state B)
    public Light[] lightsToTurnOn;            // Lights that are off in state A (fade to desired intensity in state B)
    public float lightTransitionDuration = 1f; // Duration for light fade transitions
    private float[] lightsToTurnOffOriginalIntensities;
    private float[] lightsToTurnOnDesiredIntensities;

    [Header("Object Toggle")]
    [Tooltip("An object already in the scene that is deactivated in state A and activated in state B")]
    public GameObject objectToToggle;

    [Header("UI Button Image Settings")]
    public Image buttonImage;                 // The UI Button’s Image component
    public Sprite defaultButtonSprite;        // Sprite for state A (default)
    public Sprite toggledButtonSprite;        // Sprite for state B

    [Header("UI Fade Settings")]
    public CanvasGroup[] uiToFade;            // UI elements that fade during transitions
    public float uiFadeDuration = 1f;         // Duration for UI fade

    [Header("Scripts to Disable")]
    [Tooltip("Scripts that will be disabled when toggling to state B and re‑enabled on state A.")]
    public MonoBehaviour[] scriptsToDisable;

    [Header("Sound Effects")]
    public AudioSource buttonAudioSource;
    public AudioClip buttonPressSound;
    public AudioSource middleMouseAudioSource;
    public AudioClip middleMousePressSound;
    public AudioClip middleMouseReleaseSound;

    [Header("Panning Settings (in State B)")]
    [Tooltip("The object that the camera will always look at (e.g. LOGO)")]
    public Transform logoTransform;
    [Tooltip("Maximum horizontal (left/right) pan offset in world units")]
    public float maxPanHorizontal = 5f;
    [Tooltip("Maximum vertical (up/down) pan offset in world units")]
    public float maxPanVertical = 2f;
    [Tooltip("Sensitivity factor to convert mouse movement to world space panning")]
    public float panSensitivity = 0.01f;

    // Private fields to store original camera state (state A)
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private float originalFOV;

    // Transition control flags
    private bool toggled = false;             // false = state A, true = state B
    private bool isTransitioning = false;     // true while a transition is underway

    // Panning variables (active in state B)
    private Vector3 baseCameraPosition;       // Camera position at the end of transition to state B
    private Vector3 panOffset = Vector3.zero; // The current panning offset (relative to baseCameraPosition)
    private Vector3 panInitialMousePosition = Vector3.zero; // Mouse position when middle mouse is pressed
    private Vector3 panInitialOffset = Vector3.zero;          // panOffset when middle mouse is pressed

    void Start()
    {
        if (targetCamera == null)
        {
            Debug.LogError("Target Camera is not assigned.");
            enabled = false;
            return;
        }

        // Record original camera state (state A)
        originalPosition = targetCamera.transform.position;
        originalRotation = targetCamera.transform.rotation;
        originalFOV = targetCamera.fieldOfView;
        currentFOV = originalFOV;

        // Set UI initial state and button sprite
        if (uiToFade != null)
        {
            foreach (CanvasGroup cg in uiToFade)
            {
                if (cg != null)
                    cg.alpha = 1f;
            }
        }
        if (buttonImage != null && defaultButtonSprite != null)
            buttonImage.sprite = defaultButtonSprite;

        // Store initial intensities for lightsToTurnOff
        if (lightsToTurnOff != null && lightsToTurnOff.Length > 0)
        {
            lightsToTurnOffOriginalIntensities = new float[lightsToTurnOff.Length];
            for (int i = 0; i < lightsToTurnOff.Length; i++)
            {
                if (lightsToTurnOff[i] != null)
                    lightsToTurnOffOriginalIntensities[i] = lightsToTurnOff[i].intensity;
            }
        }
        // For lightsToTurnOn, record desired intensities and set them to 0
        if (lightsToTurnOn != null && lightsToTurnOn.Length > 0)
        {
            lightsToTurnOnDesiredIntensities = new float[lightsToTurnOn.Length];
            for (int i = 0; i < lightsToTurnOn.Length; i++)
            {
                if (lightsToTurnOn[i] != null)
                {
                    lightsToTurnOnDesiredIntensities[i] = lightsToTurnOn[i].intensity;
                    lightsToTurnOn[i].intensity = 0f;
                }
            }
        }

        // Ensure the toggle object is deactivated in state A
        if (objectToToggle != null)
            objectToToggle.SetActive(false);

        // If no logoTransform is provided, default to targetTransform
        if (logoTransform == null)
            logoTransform = targetTransform;
    }

    /// <summary>
    /// Called by the UI Button’s OnClick() event.
    /// Plays a sound and toggles between state A and state B.
    /// </summary>
    public void OnButtonPressed()
    {
        if (isTransitioning)
            return;

        if (buttonAudioSource != null && buttonPressSound != null)
            buttonAudioSource.PlayOneShot(buttonPressSound);

        if (!toggled)
            StartCoroutine(TransitionToStateB());
        else
            StartCoroutine(TransitionToStateA());
    }

    /// <summary>
    /// Transitions the camera from state A to state B.
    /// In state B, the camera moves to the target location, rotates so its Y-angle becomes -50° (using the target’s X and Z),
    /// and its FOV is set to 70. Also, UI fades out, lights change, the designated object is activated, and specified scripts are disabled.
    /// </summary>
    private IEnumerator TransitionToStateB()
    {
        isTransitioning = true;

        // Disable specified scripts
        if (scriptsToDisable != null)
        {
            foreach (MonoBehaviour script in scriptsToDisable)
            {
                if (script != null)
                    script.enabled = false;
            }
        }

        // Fade out UI elements
        if (uiToFade != null && uiToFade.Length > 0)
            yield return StartCoroutine(FadeUIAlpha(0f, uiFadeDuration));

        // Light fade transitions
        if (lightsToTurnOff != null)
        {
            for (int i = 0; i < lightsToTurnOff.Length; i++)
            {
                if (lightsToTurnOff[i] != null)
                {
                    StartCoroutine(FadeLightIntensity(lightsToTurnOff[i],
                        lightsToTurnOffOriginalIntensities[i], 0f, lightTransitionDuration));
                }
            }
        }
        if (lightsToTurnOn != null)
        {
            for (int i = 0; i < lightsToTurnOn.Length; i++)
            {
                if (lightsToTurnOn[i] != null)
                {
                    StartCoroutine(FadeLightIntensity(lightsToTurnOn[i],
                        0f, lightsToTurnOnDesiredIntensities[i], lightTransitionDuration));
                }
            }
        }

        // Smoothly transition the camera's position, rotation, and FOV.
        float elapsed = 0f;
        Vector3 startPos = targetCamera.transform.position;
        Quaternion startRot = targetCamera.transform.rotation;
        float startFOV = targetCamera.fieldOfView;
        // Compute final rotation: use targetTransform's X and Z but force Y to -50°.
        Quaternion finalRot = Quaternion.Euler(targetTransform.eulerAngles.x, -50f, targetTransform.eulerAngles.z);

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / transitionDuration);
            float smoothT = t * t * (3f - 2f * t);

            targetCamera.transform.position = Vector3.Lerp(startPos, targetTransform.position, smoothT);
            targetCamera.transform.rotation = Quaternion.Slerp(startRot, finalRot, smoothT);
            targetCamera.fieldOfView = Mathf.Lerp(startFOV, targetFOVValue, smoothT);

            yield return null;
        }
        // Ensure final state B settings.
        targetCamera.transform.position = targetTransform.position;
        targetCamera.transform.rotation = finalRot;
        targetCamera.fieldOfView = targetFOVValue;
        currentFOV = targetCamera.fieldOfView;

        // Set the base position for panning.
        baseCameraPosition = targetCamera.transform.position;
        panOffset = Vector3.zero;

        // Activate the designated object.
        if (objectToToggle != null)
            objectToToggle.SetActive(true);

        // Change the UI button image to indicate state B.
        if (buttonImage != null && toggledButtonSprite != null)
            buttonImage.sprite = toggledButtonSprite;

        toggled = true;
        isTransitioning = false;
    }

    /// <summary>
    /// Transitions the camera from state B back to state A (original state).
    /// UI fades in, lights reverse their fade, the toggle object is deactivated,
    /// and disabled scripts are re‑enabled. The camera returns to its original state.
    /// </summary>
    private IEnumerator TransitionToStateA()
    {
        isTransitioning = true;

        // Fade in UI elements.
        if (uiToFade != null && uiToFade.Length > 0)
            yield return StartCoroutine(FadeUIAlpha(1f, uiFadeDuration));

        // Reverse light transitions.
        if (lightsToTurnOff != null)
        {
            for (int i = 0; i < lightsToTurnOff.Length; i++)
            {
                if (lightsToTurnOff[i] != null)
                {
                    StartCoroutine(FadeLightIntensity(lightsToTurnOff[i],
                        0f, lightsToTurnOffOriginalIntensities[i], lightTransitionDuration));
                }
            }
        }
        if (lightsToTurnOn != null)
        {
            for (int i = 0; i < lightsToTurnOn.Length; i++)
            {
                if (lightsToTurnOn[i] != null)
                {
                    StartCoroutine(FadeLightIntensity(lightsToTurnOn[i],
                        lightsToTurnOnDesiredIntensities[i], 0f, lightTransitionDuration));
                }
            }
        }

        // Smoothly transition the camera back to its original state.
        float elapsed = 0f;
        Vector3 startPos = targetCamera.transform.position;
        Quaternion startRot = targetCamera.transform.rotation;
        float startFOV = targetCamera.fieldOfView;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / transitionDuration);
            float smoothT = t * t * (3f - 2f * t);

            targetCamera.transform.position = Vector3.Lerp(startPos, originalPosition, smoothT);
            targetCamera.transform.rotation = Quaternion.Slerp(startRot, originalRotation, smoothT);
            targetCamera.fieldOfView = Mathf.Lerp(startFOV, originalFOV, smoothT);

            yield return null;
        }
        // Ensure final state A settings.
        targetCamera.transform.position = originalPosition;
        targetCamera.transform.rotation = originalRotation;
        targetCamera.fieldOfView = originalFOV;

        // Deactivate the toggle object.
        if (objectToToggle != null)
            objectToToggle.SetActive(false);

        // Change the UI button image back to default.
        if (buttonImage != null && defaultButtonSprite != null)
            buttonImage.sprite = defaultButtonSprite;

        // Re-enable previously disabled scripts.
        if (scriptsToDisable != null)
        {
            foreach (MonoBehaviour script in scriptsToDisable)
            {
                if (script != null)
                    script.enabled = true;
            }
        }

        toggled = false;
        isTransitioning = false;
    }

    /// <summary>
    /// Smoothly fades all assigned CanvasGroup elements to the specified target alpha over the given duration.
    /// </summary>
    private IEnumerator FadeUIAlpha(float targetAlpha, float duration)
    {
        if (uiToFade == null || uiToFade.Length == 0)
            yield break;

        float elapsed = 0f;
        float[] initialAlphas = new float[uiToFade.Length];
        for (int i = 0; i < uiToFade.Length; i++)
        {
            if (uiToFade[i] != null)
                initialAlphas[i] = uiToFade[i].alpha;
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            for (int i = 0; i < uiToFade.Length; i++)
            {
                if (uiToFade[i] != null)
                    uiToFade[i].alpha = Mathf.Lerp(initialAlphas[i], targetAlpha, t);
            }
            yield return null;
        }
        for (int i = 0; i < uiToFade.Length; i++)
        {
            if (uiToFade[i] != null)
                uiToFade[i].alpha = targetAlpha;
        }
    }

    /// <summary>
    /// Smoothly fades a light’s intensity from startIntensity to endIntensity over the given duration.
    /// </summary>
    private IEnumerator FadeLightIntensity(Light light, float startIntensity, float endIntensity, float duration)
    {
        if (light == null)
            yield break;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            light.intensity = Mathf.Lerp(startIntensity, endIntensity, t);
            yield return null;
        }
        light.intensity = endIntensity;
    }

    void Update()
    {
        // When in state B and not transitioning, allow panning and zooming.
        if (toggled && !isTransitioning)
        {
            // ----- Panning Controls -----
            // When middle mouse is pressed, record the initial mouse position and current pan offset.
            if (Input.GetMouseButtonDown(2))
            {
                panInitialMousePosition = Input.mousePosition;
                panInitialOffset = panOffset;
                if (middleMouseAudioSource != null && middleMousePressSound != null)
                    middleMouseAudioSource.PlayOneShot(middleMousePressSound);
            }
            // While middle mouse is held, compute the delta and update panOffset.
            if (Input.GetMouseButton(2))
            {
                Vector3 delta = Input.mousePosition - panInitialMousePosition;
                // Invert the vertical component: when the mouse moves up, delta.y is positive, but we subtract it so the camera pans downward.
                Vector3 newOffset = panInitialOffset + new Vector3(delta.x, -delta.y, 0) * panSensitivity;
                newOffset.x = Mathf.Clamp(newOffset.x, -maxPanHorizontal, maxPanHorizontal);
                newOffset.y = Mathf.Clamp(newOffset.y, -maxPanVertical, maxPanVertical);
                newOffset.z = 0;
                panOffset = newOffset;
                // Update the camera's position relative to the base position.
                targetCamera.transform.position = Vector3.Lerp(targetCamera.transform.position, baseCameraPosition + panOffset, Time.deltaTime * 10f);
                // Always look at the LOGO.
                if (logoTransform != null)
                    targetCamera.transform.LookAt(logoTransform.position);
            }
            if (Input.GetMouseButtonUp(2))
            {
                if (middleMouseAudioSource != null && middleMouseReleaseSound != null)
                    middleMouseAudioSource.PlayOneShot(middleMouseReleaseSound);
            }

            // ----- Zoom Controls (via FOV) -----
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.001f)
            {
                currentFOV = Mathf.Clamp(currentFOV - scroll * zoomSpeed, minFOV, maxFOV);
            }
            targetCamera.fieldOfView = Mathf.Lerp(targetCamera.fieldOfView, currentFOV, Time.deltaTime * fovLerpSpeed);
        }
    }
}
