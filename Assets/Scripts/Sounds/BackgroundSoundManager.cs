using UnityEngine;

public class BackgroundSoundManager : MonoBehaviour
{
    public AudioSource bgAudioSource;

    void Awake()
    {
        if (bgAudioSource == null)
            bgAudioSource = GetComponent<AudioSource>();
    }

    // ✅ Called every time this GameObject is enabled/activated
    void OnEnable()
    {
        StartBGSound();   // Use the new method for consistency
    }

    /// <summary>
    /// ✅ Starts playing the background sound if it’s not already playing.
    /// </summary>
    public void StartBGSound()
    {
        if (bgAudioSource != null && !bgAudioSource.isPlaying)
        {
            bgAudioSource.Play();
            Debug.Log("[BG Sound] Background dsound started.");
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
}
