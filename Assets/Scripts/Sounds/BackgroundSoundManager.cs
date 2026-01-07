using UnityEngine;
using UnityEngine.UI; // Required for UI Slider

public class BackgroundSoundManager : MonoBehaviour
{
    public AudioSource bgAudioSource;
    public AudioSource drawingModeAudioSource;
    public AudioSource imageFillModeAudioSource;
    public AudioSource paintModeAudioSource;

    [Header("Volume Control")]
    public Slider volumeSlider; // Assign this in Inspector

    void Awake()
    {
        if (bgAudioSource == null)
            bgAudioSource = GetComponent<AudioSource>();

        // Optional: Set initial volume from slider
        if (volumeSlider != null)
            SetAllVolumes(volumeSlider.value);
    }

    void OnEnable()
    {
        StartBGSound();
    }

    void Start()
    {
        // Listen to slider value changes
        if (volumeSlider != null)
            volumeSlider.onValueChanged.AddListener(SetAllVolumes);
    }

    /* =========================
       VOLUME CONTROL FUNCTION
       ========================= */
    public void SetAllVolumes(float volume)
    {
        if (bgAudioSource != null)
            bgAudioSource.volume = volume;

        if (drawingModeAudioSource != null)
            drawingModeAudioSource.volume = volume;

        if (imageFillModeAudioSource != null)
            imageFillModeAudioSource.volume = volume;

        if (paintModeAudioSource != null)
            paintModeAudioSource.volume = volume;

        Debug.Log("[Volume] Set to: " + volume);
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

    /* =========================
       PAINT MODE SOUND FUNCTIONS
       ========================= */
    public void StartPaintModeSound()
    {
        if (paintModeAudioSource != null && !paintModeAudioSource.isPlaying)
        {
            paintModeAudioSource.Play();
            Debug.Log("[Paint Mode Sound] Started.");
        }
    }

    public void StopPaintModeSound()
    {
        if (paintModeAudioSource != null && paintModeAudioSource.isPlaying)
        {
            paintModeAudioSource.Stop();
            Debug.Log("[Paint Mode Sound] Stopped.");
        }
    }
}
