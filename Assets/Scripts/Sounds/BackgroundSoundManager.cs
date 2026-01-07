using UnityEngine;

public class BackgroundSoundManager : MonoBehaviour
{
    public AudioSource bgAudioSource;
    public AudioSource drawingModeAudioSource;

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

    /// <summary>
    /// ✅ Starts playing the background sound if it’s not already playing.
    /// </summary>
    public void StartBGSound()
    {
        if (bgAudioSource != null && !bgAudioSource.isPlaying)
        {
            bgAudioSource.Play();
            Debug.Log("[BG Sound] Background sound started.");
        }
    }

    /// <summary>
    /// ✅ Stops the background sound if it’s currently playing.
    /// </summary>
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

    /// <summary>
    /// ✅ Starts playing the drawing mode sound if it’s not already playing.
    /// </summary>
    public void StartDrawingModeSound()
    {
        if (drawingModeAudioSource != null && !drawingModeAudioSource.isPlaying)
        {
            drawingModeAudioSource.Play();
            Debug.Log("[Drawing Mode Sound] Started.");
        }
    }

    /// <summary>
    /// ✅ Stops the drawing mode sound if it’s currently playing.
    /// </summary>
    public void StopDrawingModeSound()
    {
        if (drawingModeAudioSource != null && drawingModeAudioSource.isPlaying)
        {
            drawingModeAudioSource.Stop();
            Debug.Log("[Drawing Mode Sound] Stopped.");
        }
    }
}
