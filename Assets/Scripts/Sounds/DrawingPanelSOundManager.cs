using UnityEngine;

public class DrawingPanelSoundManager : MonoBehaviour
{
    [Header("Assign the AudioSource that holds your refresh sound")]
    public AudioSource RefreshSound;

    [Header("Assign the AudioSource that holds your continuous brushing sound")]
    public AudioSource BrushingSound;   // 🎨 add this in the Inspector

    /// <summary>
    /// Plays the refresh sound once when called (e.g., from a UI Button).
    /// </summary>
    public void PlayRefreshSound()
    {
        if (RefreshSound == null)
        {
            Debug.LogWarning("[DrawingPanelSoundManager] No RefreshSound assigned.");
            return;
        }

        if (!RefreshSound.isPlaying)
        {
            RefreshSound.Play();
            Debug.Log("[DrawingPanelSoundManager] Panel sound played once.");
        }
    }

    /// <summary>
    /// Starts the continuous brushing sound if not already playing.
    /// </summary>
    public void PlayBrushingSound()
    {
        if (BrushingSound == null)
        {
            Debug.LogWarning("[DrawingPanelSoundManager] No BrushingSound assigned.");
            return;
        }

        if (!BrushingSound.isPlaying)
        {
            BrushingSound.loop = true;          // make sure it loops
            BrushingSound.Play();
            Debug.Log("[DrawingPanelSoundManager] Brushing sound started.");
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
            Debug.Log("[DrawingPanelSoundManager] Brushing sound stopped.");
        }
    }
}
