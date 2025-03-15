using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ChildClickForwarder : MonoBehaviour
{
    private void OnMouseDown()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // Move up until we find a parent tagged "PartParent"
            Transform t = transform;
            while (t != null)
            {
                if (t.CompareTag("PartParent"))
                {
                    var clicker = t.GetComponent<SteeringWheelPartClicker>();
                    if (clicker != null)
                    {
                        clicker.NotifyPartSelected();
                    }
                    break;
                }
                t = t.parent;
            }
        }
    }
}
