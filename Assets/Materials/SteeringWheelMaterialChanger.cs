using UnityEngine;
using UnityEngine.UI;

public class SteeringWheelMaterialChanger : MonoBehaviour
{
    [Header("Template Materials")]
    public Material alcantaraMaterial;
    public Material carbonFiberMaterial;
    public Material leatherMaterial;

    [Header("Current Material")]
    // This is the material that will actually show on the steering wheel.
    public Material currentMaterial;

    [Header("UI Toggles")]
    public Toggle alcantaraToggle;
    public Toggle carbonFiberToggle;
    public Toggle leatherToggle;

    private void Start()
    {
        // Initialize events
        alcantaraToggle.onValueChanged.AddListener((isOn) => { if (isOn) ApplyAlcantara(); });
        carbonFiberToggle.onValueChanged.AddListener((isOn) => { if (isOn) ApplyCarbonFiber(); });
        leatherToggle.onValueChanged.AddListener((isOn) => { if (isOn) ApplyLeather(); });

        // Optionally, set a default material at start if needed
        if (alcantaraToggle.isOn) ApplyAlcantara();
        else if (carbonFiberToggle.isOn) ApplyCarbonFiber();
        else if (leatherToggle.isOn) ApplyLeather();
    }

    private void ApplyAlcantara()
    {
        if (alcantaraMaterial && currentMaterial)
        {
            currentMaterial.CopyPropertiesFromMaterial(alcantaraMaterial);
        }
    }

    private void ApplyCarbonFiber()
    {
        if (carbonFiberMaterial && currentMaterial)
        {
            currentMaterial.CopyPropertiesFromMaterial(carbonFiberMaterial);
        }
    }

    private void ApplyLeather()
    {
        if (leatherMaterial && currentMaterial)
        {
            currentMaterial.CopyPropertiesFromMaterial(leatherMaterial);
        }
    }
}
