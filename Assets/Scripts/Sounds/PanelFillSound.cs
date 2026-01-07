using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PanelFillSound : MonoBehaviour
{
    public GameObject LoadingPanelPanel;
    public GameObject DrawingPanel;
    [Header("References")]
    public AudioSource audioSource;   // Assign the AudioSource on the panel
    public Image fillImage;           // Assign the child Image with Fill Method

    [Header("Settings")]
    public float fillDuration =.25f;   // Time (seconds) to fill image

    private bool hasPlayed = false;

    private void OnEnable()
    {
        // Reset fill and flag each time the panel becomes active
        if (fillImage != null) fillImage.fillAmount = 0f;
        hasPlayed = false;

        StartCoroutine(FillRoutine());
    }

    private IEnumerator FillRoutine()
    {
        // Play the sound only once
        if (!hasPlayed && audioSource != null)
        {
            audioSource.Play();
            hasPlayed = true;
        }

        float elapsed = 0f;

        while (elapsed < fillDuration)
        {
            elapsed += Time.deltaTime;
            if (fillImage != null)
            {
                fillImage.fillAmount = Mathf.Clamp01(elapsed / fillDuration);
            }
            yield return null;
        }

        Debug.Log("Image Loading Filled**");
        DrawingPanel.SetActive(true);
        LoadingPanelPanel.SetActive(false);
    }
}
