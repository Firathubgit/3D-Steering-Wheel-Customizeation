using UnityEngine;

public class SteeringWheelPartClicker : MonoBehaviour
{
    public SteeringWheelCustomizer customizer;
    public SteeringWheelPartData partData;

    // Called when a child is clicked
    public void NotifyPartSelected()
    {
        if (customizer && partData != null)
        {
            customizer.SelectPart(partData);
        }
    }
}
