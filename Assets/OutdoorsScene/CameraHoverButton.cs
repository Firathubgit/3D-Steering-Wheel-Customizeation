using UnityEngine;
using UnityEngine.EventSystems;

public class CameraHoverButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Hover Settings")]
    public Transform closeUpCameraTransform; // Pivot for close-up
    public float closeUpFOV = 40f;           // FOV when hovered

    private CameraHoverManager cameraHoverManager;

    private void Awake()
    {
        cameraHoverManager = FindObjectOfType<CameraHoverManager>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (cameraHoverManager != null && closeUpCameraTransform != null)
        {
            cameraHoverManager.HoverOver(closeUpCameraTransform, closeUpFOV);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (cameraHoverManager != null)
        {
            cameraHoverManager.ExitHover();
        }
    }
}
