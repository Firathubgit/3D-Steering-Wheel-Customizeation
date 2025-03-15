using UnityEngine;
using UnityEngine.UI;

public class TogglePanelController : MonoBehaviour
{
    [Header("Toggle and Panel Mapping")]
    public Toggle toggle;        // Assign the toggle in the Inspector
    public GameObject panel;     // Assign the associated panel in the Inspector

    private void Start()
    {
        // Initialize panel state based on the toggle's current value
        UpdatePanel(toggle.isOn);

        // Listen for toggle changes
        toggle.onValueChanged.AddListener(UpdatePanel);
    }

    private void UpdatePanel(bool isOn)
    {
        if (panel != null)
        {
            panel.SetActive(isOn);
        }
    }

    private void OnDestroy()
    {
        // Remove the listener when the object is destroyed
        toggle.onValueChanged.RemoveListener(UpdatePanel);
    }
}
