using UnityEngine;

public class FillModeSoundManager : MonoBehaviour
{
    [Header("Assign the AudioSource that holds your sound clip")]
    public AudioSource RefreshSound;
    public AudioSource FilledSound;
    public AudioSource CompletedSound;

    /// <summary>
    /// Plays the sound once when called (e.g., from a UI Button).
    /// </summary>
    public void PlayRefreshSound()
    {
        if (RefreshSound == null)
        {
            Debug.LogWarning("[FillModeSoundManager] No AudioSource assigned.");
            return;
        }

        if (!RefreshSound.isPlaying)
        {
            RefreshSound.Play();
            Debug.Log("[FillModeSoundManager] Panel sound played once.");
        }
    }

    public void PlayFilledSound()
    {
        if (FilledSound == null)
        {
            Debug.LogWarning("[FillModeSoundManager] No AudioSource assigned.");
            return;
        }

        if (!FilledSound.isPlaying)
        {
            FilledSound.Play();
            Debug.Log("[FillModeSoundManager] Panel sound played once.");
        }
    }

    public void PlayCompletedSound()
    {
        if (CompletedSound == null)
        {
            Debug.LogWarning("[FillModeSoundManager] No AudioSource assigned.");
            return;
        }

        if (!CompletedSound.isPlaying)
        {
            CompletedSound.Play();
            Debug.Log("[FillModeSoundManager] Panel sound played once.");
        }
    }
}
