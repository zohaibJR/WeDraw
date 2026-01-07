using UnityEngine;

public class Mode3SoundManager : MonoBehaviour
{
    [Header("Assign the AudioSource that holds your refresh sound")]
    public AudioSource RefreshSound;

    [Header("Assign the AudioSource that holds your continuous brushing sound")]
    public AudioSource BrushingSound;

    /// <summary>
    /// Plays the refresh sound once when called (e.g., from a UI Button).
    /// </summary>
    public void PlayRefreshSound()
    {
        if (RefreshSound == null)
        {
            Debug.LogWarning("[Mode3SoundManager] No RefreshSound assigned.");
            return;
        }

        if (!RefreshSound.isPlaying)
        {
            RefreshSound.Play();
            Debug.Log("[Mode3SoundManager] Refresh sound played once.");
        }
    }

    /// <summary>
    /// Starts the continuous brushing sound if not already playing.
    /// </summary>
    public void PlayBrushingSound()
    {
        if (BrushingSound == null)
        {
            Debug.LogWarning("[Mode3SoundManager] No BrushingSound assigned.");
            return;
        }

        if (!BrushingSound.isPlaying)
        {
            BrushingSound.loop = true;   // ensure looping
            BrushingSound.Play();
            Debug.Log("[Mode3SoundManager] Brushing sound started.");
        }
    }

    /// <summary>
    /// Stops the brushing sound if it’s currently playing.
    /// </summary>
    public void StopBrushingSound()
    {
        if (BrushingSound != null && BrushingSound.isPlaying)
        {
            BrushingSound.Stop();
            Debug.Log("[Mode3SoundManager] Brushing sound stopped.");
        }
    }
}
