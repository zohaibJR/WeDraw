using UnityEngine;

public class DrawingButtonManager : MonoBehaviour
{
    // Attach this to each button in the Inspector with a different index (1 to 40)
    public void OnDrawingButtonClicked(int imageIndex)
    {
        PlayerPrefs.SetInt("DrawingImage", imageIndex);
        PlayerPrefs.Save();

        Debug.Log($"[DrawingButtonManager] Button clicked, DrawingImage set to index: {imageIndex}");
        UpdateReferenceImage();
    }

    private void UpdateReferenceImage()
    {
        ReferenceImageManager manager = FindObjectOfType<ReferenceImageManager>();
        if (manager != null)
        {
            manager.UpdateImageFromPrefs();
        }
        else
        {
            Debug.LogWarning("[DrawingButtonManager] ReferenceImageManager not found in scene.");
        }
    }

    void Start()
    {
        int selectedImage = PlayerPrefs.GetInt("DrawingImage", -1);
        Debug.Log($"[DrawingButtonManager] Start: Current DrawingImage = {selectedImage}");
        Debug.Log("..............");
    }
}
