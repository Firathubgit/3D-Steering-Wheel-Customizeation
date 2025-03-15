using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChanger : MonoBehaviour
{
    [Tooltip("Name of the scene to load when R is pressed")]
    public string sceneName;

    void Update()
    {
        // Check if the R key was pressed in this frame.
        if (Input.GetKeyDown(KeyCode.R))
        {
            // Make sure the scene name is not empty.
            if (!string.IsNullOrEmpty(sceneName))
            {
                SceneManager.LoadScene(sceneName);
            }
            else
            {
                Debug.LogWarning("SceneChanger: Scene name is not set in the Inspector.");
            }
        }
    }
}
