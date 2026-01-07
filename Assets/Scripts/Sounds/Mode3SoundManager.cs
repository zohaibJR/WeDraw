using UnityEngine;

public class Mode3SoundManager : MonoBehaviour
{

    [Header("Assign the AudioSource that holds your sound clip")]
    public AudioSource RefreshSound;

    /// <summary>
    /// Plays the sound once when called (e.g., from a UI Button).
    /// </summary>
    public void PlayRefreshSound()
    {
        if (RefreshSound == null)
        {
            Debug.LogWarning("[Mode3SoundManager] No AudioSource assigned.");
            return;
        }

        if (!RefreshSound.isPlaying)
        {
            RefreshSound.Play();
            Debug.Log("[Mode3SoundManager] Panel sound played once.");
        }
    }


}
