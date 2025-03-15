using UnityEngine;
using TMPro;
using UnityEngine.UI; // Needed for Toggle
using System.Collections;
using System.Collections.Generic;

public class SteeringWheelCustomizer : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Dropdown materialsDropdown;
    public TMP_Text totalPriceText;
    public TMP_Text selectedPartText;
    public CanvasGroup textCanvasGroup;

    [Header("Camera & Fade Settings")]
    public Transform steeringWheelCenter;
    public float orbitSpeed = 5f;
    public float fadeDuration = 0.5f;

    [Header("Zoom Settings")]
    public float zoomSmoothSpeed = 10f;
    public float zoomAmount = 10f;
    public float minFOV = 20f;
    public float maxFOV = 80f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip selectPartClip;
    public AudioClip materialChangeClip;
    public AudioClip fadeOutClip;
    public AudioClip fadeInClip;

    [Header("Toggle Object 1 Settings")]
    [Tooltip("UI Toggle that controls turning on/off extra object 1")]
    public Toggle objectToggle1;
    [Tooltip("The object to be turned on/off (already in the scene) for toggle 1")]
    public GameObject toggleableObject1;
    [Tooltip("When toggle 1 is on, this price is added to the total.")]
    public float toggleObjectPrice1 = 100f;
    private float additionalTogglePrice1 = 0f;

    [Header("Toggle Object 2 Settings")]
    [Tooltip("UI Toggle that controls turning on/off extra object 2")]
    public Toggle objectToggle2;
    [Tooltip("The object to be turned on/off (already in the scene) for toggle 2")]
    public GameObject toggleableObject2;
    [Tooltip("When toggle 2 is on, this price is added to the total.")]
    public float toggleObjectPrice2 = 150f;
    private float additionalTogglePrice2 = 0f;

    private Camera mainCam;
    private SteeringWheelPartData currentPart;
    private float targetFOV;
    private Coroutine fadeRoutine;

    // Dictionary to store each part's chosen price
    private Dictionary<SteeringWheelPartData, float> partPriceMap = new Dictionary<SteeringWheelPartData, float>();

    void Start()
    {
        mainCam = Camera.main;
        if (!mainCam)
            Debug.LogWarning("No main camera found!");

        targetFOV = mainCam.fieldOfView;

        if (materialsDropdown)
            materialsDropdown.onValueChanged.AddListener(OnMaterialDropdownChanged);

        if (totalPriceText)
            totalPriceText.text = "Total: $0.00";
        if (selectedPartText)
            selectedPartText.text = "Selected: None";

        // Setup Toggle Object 1
        if (objectToggle1 != null)
            objectToggle1.onValueChanged.AddListener(OnToggle1Changed);
        if (toggleableObject1 != null)
            toggleableObject1.SetActive(false);

        // Setup Toggle Object 2
        if (objectToggle2 != null)
            objectToggle2.onValueChanged.AddListener(OnToggle2Changed);
        if (toggleableObject2 != null)
            toggleableObject2.SetActive(false);
    }

    void Update()
    {
        HandleMiddleClickOrbit();
        HandleSmoothZoom();
    }

    #region Part Selection

    // Called from SteeringWheelPartClicker when a part is selected
    public void SelectPart(SteeringWheelPartData part)
    {
        currentPart = part;
        if (!partPriceMap.ContainsKey(part))
            partPriceMap.Add(part, 0f);

        if (selectedPartText)
            selectedPartText.text = "Selected: " + part.partName;

        RebuildMaterialDropdown(part);

        if (audioSource && selectPartClip)
            audioSource.PlayOneShot(selectPartClip);
    }

    // Rebuild the TMP_Dropdown options (with preview images)
    void RebuildMaterialDropdown(SteeringWheelPartData part)
    {
        if (!materialsDropdown || part == null) return;
        materialsDropdown.ClearOptions();

        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();

        foreach (var opt in part.materialOptions)
        {
            string optionLabel = opt.material.name + " ($" + opt.price + ")";
            Sprite previewSprite = opt.previewSprite;
            // Supply Color.white as the default color
            var optionData = new TMP_Dropdown.OptionData(optionLabel, previewSprite, Color.white);
            options.Add(optionData);
        }

        materialsDropdown.AddOptions(options);
        materialsDropdown.value = 0;
        materialsDropdown.RefreshShownValue();
    }

    // Called when the user selects a material from the dropdown
    void OnMaterialDropdownChanged(int newIndex)
    {
        if (currentPart == null) return;
        if (newIndex < 0 || newIndex >= currentPart.materialOptions.Length) return;

        var chosenOption = currentPart.materialOptions[newIndex];
        ApplyMaterialToPart(currentPart, chosenOption.material);

        partPriceMap[currentPart] = chosenOption.price;
        RecalculateTotalPrice();

        if (audioSource && materialChangeClip)
            audioSource.PlayOneShot(materialChangeClip);
    }

    // Replace all materials on each Renderer under partRoot with the chosen material
    void ApplyMaterialToPart(SteeringWheelPartData part, Material mat)
    {
        if (part.partRoot)
        {
            Renderer[] childRenderers = part.partRoot.GetComponentsInChildren<Renderer>();
            foreach (Renderer r in childRenderers)
            {
                Material[] newMats = new Material[r.materials.Length];
                for (int i = 0; i < newMats.Length; i++)
                {
                    newMats[i] = mat;
                }
                r.materials = newMats;
            }
        }
    }

    // Sum up all part prices plus the additional prices from both toggles, then update the total price text
    void RecalculateTotalPrice()
    {
        float total = additionalTogglePrice1 + additionalTogglePrice2;
        foreach (var kvp in partPriceMap)
        {
            total += kvp.Value;
        }
        if (totalPriceText)
            totalPriceText.text = "Total: $" + total.ToString("F2");
    }

    #endregion

    #region Toggle Object Functionality

    // Called when Toggle 1 changes its value
    public void OnToggle1Changed(bool isOn)
    {
        if (toggleableObject1 != null)
            toggleableObject1.SetActive(isOn);

        additionalTogglePrice1 = isOn ? toggleObjectPrice1 : 0f;
        RecalculateTotalPrice();
    }

    // Called when Toggle 2 changes its value
    public void OnToggle2Changed(bool isOn)
    {
        if (toggleableObject2 != null)
            toggleableObject2.SetActive(isOn);

        additionalTogglePrice2 = isOn ? toggleObjectPrice2 : 0f;
        RecalculateTotalPrice();
    }

    #endregion

    #region Middle-Click Orbit & Fade

    void HandleMiddleClickOrbit()
    {
        if (Input.GetMouseButtonDown(2))
        {
            StartFade(0f);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            if (audioSource && fadeOutClip)
                audioSource.PlayOneShot(fadeOutClip);
        }

        if (Input.GetMouseButton(2))
        {
            OrbitCamera();
        }

        if (Input.GetMouseButtonUp(2))
        {
            StartFade(1f);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (audioSource && fadeInClip)
                audioSource.PlayOneShot(fadeInClip);
        }
    }

    void OrbitCamera()
    {
        if (!steeringWheelCenter || !mainCam) return;
        float x = Input.GetAxis("Mouse X") * orbitSpeed;
        float y = Input.GetAxis("Mouse Y") * orbitSpeed;
        mainCam.transform.RotateAround(steeringWheelCenter.position, Vector3.up, x);
        mainCam.transform.RotateAround(steeringWheelCenter.position, mainCam.transform.right, -y);
    }

    void StartFade(float targetAlpha)
    {
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeCanvasGroup(textCanvasGroup, textCanvasGroup.alpha, targetAlpha, fadeDuration));
    }

    IEnumerator FadeCanvasGroup(CanvasGroup cg, float startAlpha, float endAlpha, float dur)
    {
        float time = 0f;
        while (time < dur)
        {
            time += Time.deltaTime;
            float newAlpha = Mathf.Lerp(startAlpha, endAlpha, time / dur);
            cg.alpha = newAlpha;
            yield return null;
        }
        cg.alpha = endAlpha;
    }

    #endregion

    #region Smooth Zoom

    void HandleSmoothZoom()
    {
        if (!mainCam) return;
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.001f)
        {
            targetFOV -= scroll * zoomAmount;
            targetFOV = Mathf.Clamp(targetFOV, minFOV, maxFOV);
        }
        mainCam.fieldOfView = Mathf.Lerp(mainCam.fieldOfView, targetFOV, Time.deltaTime * zoomSmoothSpeed);
    }

    #endregion
}
