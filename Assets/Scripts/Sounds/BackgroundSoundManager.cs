using UnityEngine;

public class BackgroundSoundManager : MonoBehaviour
{
    public AudioSource bgAudioSource;
    public AudioSource drawingModeAudioSource;
    public AudioSource imageFillModeAudioSource;

    void Awake()
    {
        if (bgAudioSource == null)
            bgAudioSource = GetComponent<AudioSource>();
    }

    // ✅ Called every time this GameObject is enabled/activated
    void OnEnable()
    {
        StartBGSound();
    }

    /* =========================
       BACKGROUND SOUND FUNCTIONS
       ========================= */

    public void StartBGSound()
    {
        if (bgAudioSource != null && !bgAudioSource.isPlaying)
        {
            bgAudioSource.Play();
            Debug.Log("[BG Sound] Background sound started.");
        }
    }

    public void StopBGSound()
    {
        if (bgAudioSource != null && bgAudioSource.isPlaying)
        {
            bgAudioSource.Stop();
            Debug.Log("[BG Sound] Background sound stopped.");
        }
    }

    /* =========================
       DRAWING MODE SOUND FUNCTIONS
       ========================= */

    public void StartDrawingModeSound()
    {
        if (drawingModeAudioSource != null && !drawingModeAudioSource.isPlaying)
        {
            drawingModeAudioSource.Play();
            Debug.Log("[Drawing Mode Sound] Started.");
        }
    }

    public void StopDrawingModeSound()
    {
        if (drawingModeAudioSource != null && drawingModeAudioSource.isPlaying)
        {
            drawingModeAudioSource.Stop();
            Debug.Log("[Drawing Mode Sound] Stopped.");
        }
    }

    /* =========================
       IMAGE FILL MODE SOUND FUNCTIONS
       ========================= */

    public void StartImageFillModeSound()
    {
        if (imageFillModeAudioSource != null && !imageFillModeAudioSource.isPlaying)
        {
            imageFillModeAudioSource.Play();
            Debug.Log("[Image Fill Mode Sound] Started.");
        }
    }

    public void StopImageFillModeSound()
    {
        if (imageFillModeAudioSource != null && imageFillModeAudioSource.isPlaying)
        {
            imageFillModeAudioSource.Stop();
            Debug.Log("[Image Fill Mode Sound] Stopped.");
        }
    }
}
