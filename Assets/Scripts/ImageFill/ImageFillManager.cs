using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class ImageFillManager : MonoBehaviour
{
    public enum MarkerType { Solid, Crayon, Marker, Spray, Pencil, Eraser }

    private HashSet<PolygonCollider2D> filledColliders = new HashSet<PolygonCollider2D>();

    [Header("Brush Settings")]
    public Color brushColor = Color.black;
    public MarkerType currentMarkerType = MarkerType.Solid;

    // --- OPTIMIZATION FIELD ---
    // The critical CPU-side array to prevent lag during pixel manipulation.
    private Color[] drawingPixels;
    // --------------------------

    [Header("UI References")]
    public RectTransform[] brushButtons;
    public float activeOffset = 50f; // Offset for the highlighted brush button

    private RawImage drawingArea;
    private Texture2D drawingTexture;
    private RectTransform drawingRect;
    private PolygonCollider2D[] regionColliders;

    public FillModeSoundManager soundManager;

    void OnEnable()
    {
        InitializeDrawing();
        HighlightActiveBrush((int)currentMarkerType);
    }

    /// <summary>
    /// Public method called when a new image is selected to clear state and reinitialize.
    /// </summary>
    public void LoadNewImage()
    {
        // Essential: Clear the tracking data for the previous image.
        filledColliders.Clear();
        Debug.Log("[ImageFillManager] Switched image. Filled regions cleared.");

        // Re-run initialization to grab new texture and colliders from the newly active image GameObject.
        InitializeDrawing();
    }

    void InitializeDrawing()
    {
        int selectedImageIndex = PlayerPrefs.GetInt("FillImage", -1);
        if (selectedImageIndex < 1)
        {
            Debug.LogError("DrawingManager: 'FillImage' PlayerPref is invalid or not set.");
            drawingArea = null;
            return;
        }

        string objectName = GetObjectNameFromIndex(selectedImageIndex);
        GameObject activeObject = GameObject.Find(objectName);
        if (!activeObject)
        {
            Debug.LogError("DrawingManager: GameObject not found: " + objectName);
            drawingArea = null;
            return;
        }

        DrawingTargetFillMOde target = activeObject.GetComponent<DrawingTargetFillMOde>();
        if (!target)
        {
            Debug.LogError("DrawingManager: Missing DrawingTargetFillMOde on " + objectName);
            drawingArea = null;
            return;
        }

        // --- Update References ---
        drawingArea = target.GetDrawingArea();
        regionColliders = target.GetAllColliders();

        if (!drawingArea || regionColliders == null || regionColliders.Length == 0)
        {
            Debug.LogError("DrawingManager: DrawingArdea or Colliders are not properly set.");
            drawingArea = null;
            return;
        }
        drawingRect = drawingArea.rectTransform;

        Texture2D sourceTexture = (Texture2D)drawingArea.texture;
        if (sourceTexture == null)
        {
            Debug.LogError("DrawingManager: Source texture is null.");
            drawingArea = null;
            return;
        }

        // --- Initialize CPU-side array and texture ---
        filledColliders.Clear();

        // Create new editable texture
        drawingTexture = new Texture2D(sourceTexture.width, sourceTexture.height, TextureFormat.RGBA32, false);

        // OPTIMIZATION: Get the pixel data once and work with the array.
        drawingPixels = sourceTexture.GetPixels();

        drawingTexture.SetPixels(drawingPixels);
        drawingTexture.Apply();

        drawingArea.texture = drawingTexture;

        Debug.Log($"[ImageFillManager] Initialized for: {objectName} with {regionColliders.Length} regions.");
    }


    void Update()
    {
        // Check if drawingArea is initialized and the mouse button is pressed
        if (Input.GetMouseButtonDown(0) && drawingArea != null)
        {
            // Convert screen point to local point within the RawImage RectTransform
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(drawingRect, Input.mousePosition, null, out Vector2 localMousePos))
            {
                // Transform the local point into a world point for the 2D collider check
                Vector2 worldPoint = drawingRect.TransformPoint(localMousePos);

                foreach (PolygonCollider2D collider in regionColliders)
                {
                    // Check if the world point is inside the collider
                    if (collider && collider.OverlapPoint(worldPoint))
                    {
                        FillRegion(collider);
                        break; // Stop checking after the first overlap is found
                    }
                }
            }
        }
    }

    /// <summary>
    /// Fills a region defined by a PolygonCollider2D using the selected brush style.
    /// Uses a CPU-side array for lag-free pixel updates.
    /// </summary>
    private void FillRegion(PolygonCollider2D regionCollider)
    {
        if (regionCollider == null || drawingTexture == null || drawingPixels == null) return;

        bool isErasing = (currentMarkerType == MarkerType.Eraser);
        Rect rect = drawingRect.rect;
        int texWidth = drawingTexture.width;
        int texHeight = drawingTexture.height;

        // --- Core Pixel Looping and Coloring ---
        // Iterate over all pixels in the texture
        for (int x = 0; x < texWidth; x++)
        {
            for (int y = 0; y < texHeight; y++)
            {
                // Convert texture pixel (0 to width/height) to local-UI coords 
                float uiX = ((float)x / texWidth) * rect.width - rect.width * 0.5f;
                float uiY = ((float)y / texHeight) * rect.height - rect.height * 0.5f;

                // Transform local-UI point to world point
                Vector2 worldPoint = drawingRect.TransformPoint(new Vector2(uiX, uiY));

                // Check if the world point is within the current region collider
                if (!regionCollider.OverlapPoint(worldPoint))
                    continue; // Skip this pixel if it's outside the region

                int index = y * texWidth + x; // Calculate the 1D index

                // --- Dispatch to dedicated fill functions ---
                if (isErasing)
                {
                    EraseFill(index);
                }
                else
                {
                    // Dispatch is still necessary here to choose the right texture logic (Solid, Spray, etc.)
                    switch (currentMarkerType)
                    {
                        case MarkerType.Solid:
                            SolidFill(index);
                            break;
                        case MarkerType.Crayon:
                            CrayonFill(x, y, index);
                            break;
                        case MarkerType.Marker:
                            MarkerFill(index);
                            break;
                        case MarkerType.Spray:
                            SprayFill(index);
                            break;
                        case MarkerType.Pencil:
                            PencilFill(index);
                            break;
                    }
                }
            }
        }

        // BATCHED UPDATE: Send the updated CPU array back to the GPU once (for performance)
        drawingTexture.SetPixels(drawingPixels);
        drawingTexture.Apply();
        // --- End of Core Pixel Looping and Coloring ---

        // Update completion tracking and play sounds
        UpdateFillState(regionCollider, isErasing);
    }

    // --- Dedicated Fill/Erase Functions (The texture effects) ---

    private void SolidFill(int index)
    {
        // Applies a uniform, solid color.
        drawingPixels[index] = brushColor;
    }

    private void CrayonFill(int x, int y, int index)
    {
        // Sparse fill for a simple crayon/rough texture effect.
        if ((x + y) % 5 >= 2) return;
        drawingPixels[index] = brushColor;
    }

    private void MarkerFill(int index)
    {
        // Blends the new color with the existing one for a translucent marker effect.
        Color existingColor = drawingPixels[index];
        drawingPixels[index] = Color.Lerp(existingColor, brushColor, 0.6f);
    }

    private void SprayFill(int index)
    {
        // Randomly skips pixels for a speckled spray-paint effect.
        if (Random.value <= 0.9f) return;
        drawingPixels[index] = brushColor;
    }

    private void PencilFill(int index)
    {
        // Converts the brush color to grayscale for a pencil-like tint effect.
        float g = (brushColor.r + brushColor.g + brushColor.b) / 3f;
        drawingPixels[index] = new Color(g, g, g, 1f);
    }

    private void EraseFill(int index)
    {
        // Erases the color by setting the pixel back to white.
        drawingPixels[index] = Color.white;
    }

    // --- Dedicated State Management ---

    private void UpdateFillState(PolygonCollider2D regionCollider, bool isErasing)
    {
        if (isErasing)
        {
            filledColliders.Remove(regionCollider);
        }
        else
        {
            bool wasFilled = filledColliders.Contains(regionCollider);
            filledColliders.Add(regionCollider);

            if (!wasFilled)
            {
                soundManager?.PlayFilledSound();
            }
        }

        // Check if all regions are now colored
        bool allFilled = (regionColliders != null) && (filledColliders.Count == regionColliders.Length);

        if (allFilled)
        {
            soundManager?.PlayCompletedSound();
            Debug.Log("All regions filled! 🎉");
        }
    }
    // --- End of Dedicated Functions ---

    public void SetBrushColor(Color color)
    {
        brushColor = color;
    }

    /// <summary>
    /// Helper to set the marker type, color, and update the UI highlight.
    /// This replaces the central switch statement for selection.
    /// </summary>
    private void SetMarkerAndColor(MarkerType type, Color color, int index)
    {
        currentMarkerType = type;
        brushColor = color;
        Debug.Log(type.ToString() + " selected (Index: " + index + ")");
        HighlightActiveBrush(index);
    }

    // -----------------------------------------------------------------
    // New Public Functions for Marker Selection (Replacing the Switch)
    // -----------------------------------------------------------------

    public void SelectSolid()
    {
        Debug.Log("Solid selected");
        SetMarkerAndColor(MarkerType.Solid, Color.red, 0);
    }

    public void SelectCrayon()
    {
        Debug.Log("Crayon selected");
        SetMarkerAndColor(MarkerType.Crayon, Color.blue, 1);
    }

    public void SelectMarker()
    {
        Debug.Log("Marker selected");
        // Pink color
        SetMarkerAndColor(MarkerType.Marker, new Color(1f, 0.4f, 0.7f), 2);
    }

    public void SelectSpray()
    {
        Debug.Log("Spray selected");
        SetMarkerAndColor(MarkerType.Spray, Color.green, 3);
    }

    public void SelectPencil()
    {
        Debug.Log("Pencil selected");
        // yellow color here
        SetMarkerAndColor(MarkerType.Pencil, Color.yellow, 4);
    }

    public void SelectEraser()
    {
        Debug.Log("Eraser selected");
        // Eraser doesn't need a specific brush color, but we pass black as default
        SetMarkerAndColor(MarkerType.Eraser, Color.black, 5);
    }

    // -----------------------------------------------------------------

    private void HighlightActiveBrush(int activeIndex)
    {
        if (brushButtons == null || brushButtons.Length == 0) return;

        for (int i = 0; i < brushButtons.Length; i++)
        {
            if (brushButtons[i] == null) continue;

            // Use X axis for offset
            float targetX = (i == activeIndex) ? activeOffset : 0f;
            StartCoroutine(SlideButton(brushButtons[i], targetX));
        }
    }

    private IEnumerator SlideButton(RectTransform btn, float targetX)
    {
        Vector2 startPos = btn.anchoredPosition;
        // The Y position remains constant
        Vector2 endPos = new Vector2(targetX, startPos.y);
        float duration = 0.2f;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            btn.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
            yield return null;
        }

        btn.anchoredPosition = endPos;
    }

    public void ClearCanvas()
    {
        if (!drawingTexture || drawingPixels == null) return;

        // Clear the drawingTexture to white by modifying the CPU array
        for (int i = 0; i < drawingPixels.Length; i++)
        {
            drawingPixels[i] = Color.white;
        }

        // Upload changes to GPU once
        drawingTexture.SetPixels(drawingPixels);
        drawingTexture.Apply();

        // Clear the filled colliders set
        filledColliders.Clear();
        Debug.Log("Canvas cleared and fill regions reset.");
    }

    public void SaveScreenshotToDesktop()
    {
        if (drawingTexture == null)
        {
            Debug.LogWarning("SaveScreenshotToDesktop: No drawing texture to save.");
            return;
        }

        byte[] bytes = drawingTexture.EncodeToPNG();
        string desktopPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop);
        string fileName = "DrawingScreenshot_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png";
        string filePath = Path.Combine(desktopPath, fileName);

        try
        {
            File.WriteAllBytes(filePath, bytes);
            Debug.Log("Screenshot saved to Desktop: " + filePath);

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            System.Diagnostics.Process.Start(filePath);
#endif
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Failed to save screenshot: " + ex.Message);
        }
    }


    string GetObjectNameFromIndex(int index)
    {
        switch (index)
        {
            case 1: return "Apple";
            case 2: return "Banana";
            case 3: return "Cherry";
            case 4: return "Grappes";
            case 5: return "Mango";
            case 6: return "Orange";
            case 7: return "Pear";
            case 8: return "Gauva";
            case 9: return "Strawberry";
            case 10: return "Pineapple";
            case 11: return "brocolli";
            case 12: return "Capsicum";
            case 13: return "Carrot";
            case 14: return "Chilli";
            case 15: return "Corn";
            case 16: return "EggPlant";
            case 17: return "Lemon";
            case 18: return "Onion";
            case 19: return "Pea";
            case 20: return "Tomato";
            case 21: return "Crab";
            case 22: return "Fish";
            case 23: return "JellyFish";
            case 24: return "Octupus";
            case 25: return "Shark";
            case 26: return "SmallFIsh";
            case 27: return "StarFish";
            case 28: return "StingRay";
            case 29: return "Turtle";
            case 30: return "Whale";
            case 31: return "Cat";
            case 32: return "Deer";
            case 33: return "Dog";
            case 34: return "Elephant";
            case 35: return "graph";
            case 36: return "Lion";
            case 37: return "Monkey";
            case 38: return "Rabbit";
            case 39: return "Sheep";
            case 40: return "Tiger";
            default: return "";
        }
    }
}
