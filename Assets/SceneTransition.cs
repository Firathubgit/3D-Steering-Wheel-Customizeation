using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneTransitionWithSound : MonoBehaviour
{
    [Header("Scene Loading")]
    public string sceneToLoad;

    [Header("Fade Settings")]
    public Image fadeImage;
    public float fadeDuration = 1f;

    [Header("Audio")]
    public AudioSource buttonAudioSource;
    public AudioClip buttonSoundEffect;

    private void Start()
    {
        // Make sure the image doesn't block clicks
        if (fadeImage != null)
        {
            fadeImage.raycastTarget = false; // Important!
            // (Ensures UI button behind it is still clickable)

            // Optionally start faded from black if needed
            Color startColor = fadeImage.color;
            startColor.a = 1f;
            fadeImage.color = startColor;

            // Start fade-in
            StartCoroutine(FadeIn());
        }
    }

    // Hook this to the button OnClick event
    public void OnButtonPressed()
    {
        // Play button sound
        if (buttonAudioSource && buttonSoundEffect)
        {
            buttonAudioSource.PlayOneShot(buttonSoundEffect);
        }

        // Begin fade-out and scene load
        StartCoroutine(FadeOutAndLoadScene());
    }

    private IEnumerator FadeIn()
    {
        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = 1f - (elapsedTime / fadeDuration);

            Color c = fadeImage.color;
            c.a = alpha;
            fadeImage.color = c;

            yield return null;
        }

        // Fully transparent at the end
        Color finalColor = fadeImage.color;
        finalColor.a = 0f;
        fadeImage.color = finalColor;
    }

    private IEnumerator FadeOutAndLoadScene()
    {
        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = (elapsedTime / fadeDuration);

            Color c = fadeImage.color;
            c.a = alpha;
            fadeImage.color = c;

            yield return null;
        }

        SceneManager.LoadScene(sceneToLoad);
    }
}
