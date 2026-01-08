using UnityEngine;

public class DrawingPanelSoundManager : MonoBehaviour
{
    [Header("Assign the AudioSource that holds your refresh sound")]
    public AudioSource RefreshSound;

    [Header("Assign the AudioSource that holds your continuous brushing sound")]
    public AudioSource BrushingSound;

    [Header("Brushing Sound Control")]
    public bool allowBrushingSound = true;   // ✅ true / false switch

    /// <summary>
    /// Plays the refresh sound once.
    /// </summary>
    public void PlayRefreshSound()
    {
        if (RefreshSound == null)
        {
            Debug.LogWarning("[DrawingPanelSoundManager] No RefreshSound assigned.");
            return;
        }

        if (!RefreshSound.isPlaying)
            RefreshSound.Play();
    }

    /// <summary>
    /// Starts the continuous brushing sound if allowed.
    /// </summary>
    public void PlayBrushingSound()
    {
        // ❌ Block brushing sound when not allowed
        if (!allowBrushingSound)
            return;

        if (BrushingSound == null)
        {
            Debug.LogWarning("[DrawingPanelSoundManager] No BrushingSound assigned.");
            return;
        }

        if (!BrushingSound.isPlaying)
        {
            BrushingSound.loop = true;
            BrushingSound.Play();
        }
    }

    /// <summary>
    /// Stops the brushing sound.
    /// </summary>
    public void StopBrushingSound()
    {
        if (BrushingSound != null && BrushingSound.isPlaying)
            BrushingSound.Stop();
    }
}
