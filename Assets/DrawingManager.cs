using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class DrawingManager : MonoBehaviour
{
    public enum BrushType
    {
        Solid,
        Crayon,
        Marker,
        Spray,
        Pencil
    }

    [Header("UI Elements")]
    public Slider brushSizeSlider;

    [Header("Brush Settings")]
    public Color brushColor = Color.black;
    public BrushType currentBrush = BrushType.Solid;

    // --- OPTIMIZATION FIELDS ---
    private Color[] drawingPixels; // The critical CPU-side array to prevent lag
    private bool hasDrawnThisFrame = false; // Flag to check if we need to call Apply()
    // ---------------------------

    private RawImage drawingArea;
    private PolygonCollider2D drawingBoundaryCollider;
    private Texture2D drawingTexture;
    private RectTransform drawingRect;
    private Texture2D boundaryMask;

    private Vector2? lastDrawPos = null;
    private Color savedColor;
    private bool isAdjustingBrush = false;
    private int textureScale = 1;

    private float lastDrawTime = 0f;
    private float drawInterval = 0.016f;

    public DrawingPanelSoundManager soundManager;

    [Header("UI – Brush Buttons")]
    public RectTransform[] brushButtons;
    [SerializeField] private float activeOffset = -20f;

    void OnEnable()
    {
        InitializeDrawing();
    }

    // ----------------------------------------------------------------------
    // INITIALIZATION AND SETUP
    // ----------------------------------------------------------------------

    void InitializeDrawing()
    {
        // ... (standard setup for textureScale and slider)
        if (Application.isMobilePlatform)
        {
            textureScale = 2;
            brushSizeSlider.maxValue = 10f;
        }
        else
        {
            brushSizeSlider.maxValue = 20f;
        }

        int selectedImageIndex = PlayerPrefs.GetInt("DrawingImage", 0);
        if (selectedImageIndex <= 0)
        {
            Debug.LogError("DrawingManager: Invalid or unset DrawingImage PlayerPref.");
            return;
        }

        string objectName = GetObjectNameFromIndex(selectedImageIndex);
        GameObject activeFruit = GameObject.Find(objectName);
        if (activeFruit == null)
        {
            Debug.LogError("DrawingManager: No GameObject found with name: " + objectName);
            return;
        }

        drawingArea = activeFruit.GetComponentInChildren<RawImage>();
        drawingBoundaryCollider = activeFruit.GetComponentInChildren<PolygonCollider2D>();
        drawingRect = drawingArea?.rectTransform;

        if (drawingArea == null || drawingBoundaryCollider == null)
        {
            Debug.LogError("DrawingManager: RawImage or PolygonCollider2D missing on " + objectName);
            return;
        }

        Texture2D sourceTexture = drawingArea.texture as Texture2D;
        if (sourceTexture == null)
        {
            Debug.LogError("DrawingManager: Source texture is missing or invalid!");
            return;
        }

        drawingTexture = new Texture2D(sourceTexture.width / textureScale, sourceTexture.height / textureScale, TextureFormat.RGBA32, false);

        // OPTIMIZATION: Get the pixel data once and work with the array.
        drawingPixels = sourceTexture.GetPixels();

        drawingTexture.SetPixels(drawingPixels);
        drawingTexture.Apply();

        drawingArea.texture = drawingTexture;

        CreateBoundaryMask();
        LoadDrawingProgress();

        HighlightActiveBrush((int)currentBrush);
    }

    void CreateBoundaryMask()
    {
        // ... (standard CreateBoundaryMask logic)
        boundaryMask = new Texture2D(drawingTexture.width, drawingTexture.height, TextureFormat.R8, false);
        Color[] maskPixels = new Color[boundaryMask.width * boundaryMask.height];

        for (int x = 0; x < boundaryMask.width; x++)
        {
            for (int y = 0; y < boundaryMask.height; y++)
            {
                Vector2 worldPoint = PixelToWorld(x, y);
                maskPixels[y * boundaryMask.width + x] =
                    drawingBoundaryCollider.OverlapPoint(worldPoint) ? Color.white : Color.black;
            }
        }

        boundaryMask.SetPixels(maskPixels);
        boundaryMask.Apply();
    }

    string GetObjectNameFromIndex(int index)
    {
        string[] names = {
            "", "Apple","Banana","Cherry","Grappes","Mango","Orange","Pear","Gauva","Strawberry",
            "Pineapple","brocolli","Capsicum","Carrot","Chilli","Corn","EggPlant","Lemon","Onion","Pea","Tomato",
            "Crab","Fish","JellyFish","Octupus","Shark","SmallFIsh","StarFish","StingRay","Turtle","Whale",
            "Cat","Deer","Dog","Elephant","graph","Lion","Monkey","Rabbit","Sheep","Tiger"
        };
        return (index >= 1 && index < names.Length) ? names[index] : "";
    }

    // ----------------------------------------------------------------------
    // DRAWING LOOP
    // ----------------------------------------------------------------------

    void Update()
    {
        if (Time.time - lastDrawTime < drawInterval) return;
        lastDrawTime = Time.time;

        if (Input.GetMouseButton(0))
            HandleDrawing(Input.mousePosition);

        // ... (Touch input logic)
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary || touch.phase == TouchPhase.Began)
            {
                HandleDrawing(touch.position);
            }
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                lastDrawPos = null;
                SaveDrawingProgress();
                if (soundManager != null)
                    soundManager.StopBrushingSound();
            }
        }
    }

    void LateUpdate()
    {
        // OPTIMIZATION: Only apply changes to the GPU once per frame if drawing occurred.
        if (hasDrawnThisFrame)
        {
            drawingTexture.SetPixels(drawingPixels);
            drawingTexture.Apply(); // This is now called ONCE per frame max while drawing.
            hasDrawnThisFrame = false;
        }

        if (Input.GetMouseButtonUp(0) && lastDrawPos != null)
        {
            SaveDrawingProgress();
            lastDrawPos = null;

            if (soundManager != null)
                soundManager.StopBrushingSound();
        }
    }

    void HandleDrawing(Vector2 screenPos)
    {
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(drawingRect, screenPos, null, out Vector2 localPos))
        {
            if (drawingRect.rect.Contains(localPos))
            {
                hasDrawnThisFrame = true; // Set flag to trigger GPU update in LateUpdate

                if (soundManager != null)
                    soundManager.PlayBrushingSound();

                Vector2 curr = localPos;

                if (lastDrawPos == null)
                {
                    DrawCircleAt(curr);
                }
                else
                {
                    Vector2 prev = lastDrawPos.Value;
                    float distance = Vector2.Distance(prev, curr);
                    int steps = Mathf.Min(Mathf.CeilToInt(distance * 2), 10);

                    for (int i = 0; i <= steps; i++)
                    {
                        Vector2 lerped = Vector2.Lerp(prev, curr, i / (float)steps);
                        DrawCircleAt(lerped);
                    }
                }
                lastDrawPos = curr;
            }
            else
            {
                if (lastDrawPos != null)
                {
                    if (soundManager != null)
                        soundManager.StopBrushingSound();

                    lastDrawPos = null;
                    SaveDrawingProgress();
                }
            }
        }
    }

    void DrawCircleAt(Vector2 localPos)
    {
        float normalizedX = (localPos.x + drawingRect.rect.width / 2f) / drawingRect.rect.width;
        float normalizedY = (localPos.y + drawingRect.rect.height / 2f) / drawingRect.rect.height;

        int centerX = Mathf.RoundToInt(normalizedX * drawingTexture.width);
        int centerY = Mathf.RoundToInt(normalizedY * drawingTexture.height);
        int radius = Mathf.RoundToInt(brushSizeSlider.value * (Application.isMobilePlatform ? 0.5f : 1f));

        switch (currentBrush)
        {
            case BrushType.Solid:
                DrawSolidBrush(centerX, centerY, radius);
                return;
            case BrushType.Crayon:
                DrawCrayonBrush(centerX, centerY, radius);
                return;
            case BrushType.Marker:
                DrawMarkerBrush(centerX, centerY, radius);
                return;
            case BrushType.Pencil:
                DrawPencilBrush(centerX, centerY, radius);
                return;
            case BrushType.Spray:
                DrawSprayBrush(centerX, centerY, radius);
                return;
        }
    }

    // ----------------------------------------------------------------------
    // 🎨 OPTIMIZED & TEXTURED BRUSH IMPLEMENTATIONS 🎨
    // ----------------------------------------------------------------------

    int GetIndex(int x, int y)
    {
        return y * drawingTexture.width + x;
    }

    /// <summary>Draws a fully opaque, solid circle.</summary>
    void DrawSolidBrush(int centerX, int centerY, int radius)
    {
        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                if (x * x + y * y > radius * radius) continue;

                int px = Mathf.Clamp(centerX + x, 0, drawingTexture.width - 1);
                int py = Mathf.Clamp(centerY + y, 0, drawingTexture.height - 1);

                if (boundaryMask.GetPixel(px, py).r < 0.5f) continue;

                // OPTIMIZED: Set the color in the CPU array
                drawingPixels[GetIndex(px, py)] = brushColor;
            }
        }
    }

    /// <summary>Draws a thick, opaque-like brush with random texture/sparsity for a CRAYON effect.</summary>
    void DrawCrayonBrush(int centerX, int centerY, int radius)
    {
        float noiseFactor = 0.5f;
        float opacity = 0.35f;

        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                float distSq = x * x + y * y;
                if (distSq > radius * radius) continue;

                // Gritty texture: skip half the pixels randomly
                if (Random.value > noiseFactor) continue;

                int px = Mathf.Clamp(centerX + x, 0, drawingTexture.width - 1);
                int py = Mathf.Clamp(centerY + y, 0, drawingTexture.height - 1);
                if (boundaryMask.GetPixel(px, py).r < 0.5f) continue;

                int index = GetIndex(px, py);
                Color existing = drawingPixels[index];

                // Waxy blend with slight opacity variation
                float blendAlpha = opacity + Random.Range(-0.1f, 0.1f);
                Color finalColor = Color.Lerp(existing, brushColor, blendAlpha);

                // Slight value randomization for wax texture
                float h, s, v;
                Color.RGBToHSV(finalColor, out h, out s, out v);
                v = Mathf.Clamp01(v + Random.Range(-0.05f, 0.05f));
                finalColor = Color.HSVToRGB(h, s, v);

                // OPTIMIZED: Set the color in the CPU array
                drawingPixels[index] = finalColor;
            }
        }
    }

    /// <summary>Draws a semi-translucent, smooth brush for a MARKER effect.</summary>
    void DrawMarkerBrush(int centerX, int centerY, int radius)
    {
        float markerOpacity = 0.35f;
        float softEdgeAmount = 0.4f;

        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                float distSq = x * x + y * y;
                if (distSq > radius * radius) continue;

                int px = Mathf.Clamp(centerX + x, 0, drawingTexture.width - 1);
                int py = Mathf.Clamp(centerY + y, 0, drawingTexture.height - 1);

                if (boundaryMask.GetPixel(px, py).r < 0.5f) continue;

                int index = GetIndex(px, py);
                Color existing = drawingPixels[index];
                float blendFactor = markerOpacity;

                // Soft edge: reduce blend factor near the circle's edge
                float distance = Mathf.Sqrt(distSq);
                if (distance > radius * (1f - softEdgeAmount))
                {
                    blendFactor *= 1f - ((distance - radius * (1f - softEdgeAmount)) / (radius * softEdgeAmount));
                }
                blendFactor = Mathf.Clamp01(blendFactor);

                Color finalColor = Color.Lerp(existing, brushColor, blendFactor);

                // OPTIMIZED: Set the color in the CPU array
                drawingPixels[index] = finalColor;
            }
        }
    }

    /// <summary>Draws a very light, scratchy, and buildable PENCIL effect.</summary>
    void DrawPencilBrush(int centerX, int centerY, int radius)
    {
        float pencilOpacity = 0.08f;
        float scratchinessFactor = 0.6f;

        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                float distSq = x * x + y * y;
                // Irregular edge:
                if (distSq > radius * radius + Random.Range(-2, 2)) continue;

                // Scratchiness: skip a high percentage of pixels
                if (Random.value < scratchinessFactor) continue;

                int px = Mathf.Clamp(centerX + x, 0, drawingTexture.width - 1);
                int py = Mathf.Clamp(centerY + y, 0, drawingTexture.height - 1);
                if (boundaryMask.GetPixel(px, py).r < 0.5f) continue;

                int index = GetIndex(px, py);
                Color existing = drawingPixels[index];

                // Very low alpha blend for soft, layered graphite look
                Color finalColor = Color.Lerp(existing, brushColor, pencilOpacity);

                // OPTIMIZED: Set the color in the CPU array
                drawingPixels[index] = finalColor;
            }
        }
    }

    /// <summary>Draws a dispersed SPRAY PAINT effect.</summary>
    void DrawSprayBrush(int centerX, int centerY, int radius)
    {
        int sprayCount = radius * (Application.isMobilePlatform ? 2 : 5);
        float sprayOpacity = 0.15f;

        for (int i = 0; i < sprayCount; i++)
        {
            Vector2 offset = Random.insideUnitCircle * radius;
            int px = Mathf.Clamp(centerX + Mathf.RoundToInt(offset.x), 0, drawingTexture.width - 1);
            int py = Mathf.Clamp(centerY + Mathf.RoundToInt(offset.y), 0, drawingTexture.height - 1);
            if (boundaryMask.GetPixel(px, py).r < 0.5f) continue;

            int index = GetIndex(px, py);
            Color existing = drawingPixels[index];

            Color blended = Color.Lerp(existing, brushColor, sprayOpacity + Random.Range(-0.05f, 0.05f));

            // OPTIMIZED: Set the color in the CPU array
            drawingPixels[index] = blended;
        }
    }

    // ----------------------------------------------------------------------
    // UTILITY METHODS (No changes here, but now use drawingPixels for Clear)
    // ----------------------------------------------------------------------

    Vector2 PixelToWorld(int px, int py)
    {
        float uiX = ((float)px / drawingTexture.width) * drawingRect.rect.width - drawingRect.rect.width / 2f;
        float uiY = ((float)py / drawingTexture.height) * drawingRect.rect.height - drawingRect.rect.height / 2f;
        return drawingRect.TransformPoint(new Vector2(uiX, uiY));
    }

    public void SetBrushColor(Color color) => brushColor = color;
    public void UseEraser() => brushColor = Color.white;

    public void ClearCanvas()
    {
        if (drawingArea == null) return;

        // Reset the CPU array to white
        for (int i = 0; i < drawingPixels.Length; i++)
            drawingPixels[i] = Color.white;

        // Apply changes once
        drawingTexture.SetPixels(drawingPixels);
        drawingTexture.Apply();

        Debug.Log("✅ Canvas cleared");
    }

    // ----------------------------------------------------------------------
    // SAVE/LOAD METHODS (Use drawingPixels for consistent state)
    // ----------------------------------------------------------------------

    public void SaveDrawingProgress()
    {
        if (drawingTexture == null) return;

        // Ensure the latest changes are reflected in the texture before encoding
        drawingTexture.SetPixels(drawingPixels);
        drawingTexture.Apply();

        StartCoroutine(SaveDrawingAsync());
    }

    private IEnumerator SaveDrawingAsync()
    {
        yield return null;
        byte[] bytes = drawingTexture.EncodeToPNG();
        string filePath = GetDrawingSavePath();
        File.WriteAllBytes(filePath, bytes);
        Debug.Log("✅ Drawing saved at: " + filePath);
    }

    void LoadDrawingProgress()
    {
        string filePath = GetDrawingSavePath();

        if (File.Exists(filePath))
        {
            byte[] bytes = File.ReadAllBytes(filePath);
            Texture2D loadedTex = new Texture2D(drawingTexture.width, drawingTexture.height, TextureFormat.RGBA32, false);

            if (loadedTex.LoadImage(bytes))
            {
                // Update both the texture and the CPU-side array
                drawingTexture = loadedTex;
                drawingPixels = drawingTexture.GetPixels();
                drawingArea.texture = drawingTexture;
                Debug.Log("✅ Drawing progress loaded");
            }
            else
            {
                Debug.LogWarning("⚠ Failed to load saved texture, starting fresh.");
            }
        }
        else
        {
            Debug.Log("ℹ️ No saved drawing found.");
        }
    }

    string GetDrawingSavePath()
    {
        int selectedImageIndex = PlayerPrefs.GetInt("DrawingImage", 0);
        string fileName = $"FruitDrawing_{selectedImageIndex}.png";
        return Path.Combine(Application.persistentDataPath, fileName);
    }

    // ... (Brush selector and HighlightActiveBrush methods remain the same)

    public void OnBrushSizeDragStart()
    {
        if (!isAdjustingBrush)
        {
            savedColor = brushColor;
            brushColor = Color.white; // Temporary color for preview
            isAdjustingBrush = true;
        }
    }

    public void OnBrushSizeDragEnd()
    {
        if (isAdjustingBrush) StartCoroutine(RestoreBrushColorAfterDelay(0.5f));
    }

    private IEnumerator RestoreBrushColorAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        brushColor = savedColor;
        isAdjustingBrush = false;
    }

    public void SaveFullScreenScreenshot()
    {
        StartCoroutine(CaptureFullScreenScreenshot());
    }

    private IEnumerator CaptureFullScreenScreenshot()
    {
        // ... (Screenshot logic)
        yield return new WaitForEndOfFrame();

        Texture2D screenTex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        screenTex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        screenTex.Apply();

        byte[] bytes = screenTex.EncodeToPNG();
        Destroy(screenTex);

        string filename = "FullScreenshot_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png";

#if UNITY_EDITOR || UNITY_STANDALONE
        string desktopPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop);
        string filePath = Path.Combine(desktopPath, filename);
        File.WriteAllBytes(filePath, bytes);
        Debug.Log("✅ Screenshot saved: " + filePath);

#elif UNITY_ANDROID
        string path = Path.Combine(Application.persistentDataPath, filename);
        File.WriteAllBytes(path, bytes);

        using (AndroidJavaClass mediaScanner = new AndroidJavaClass("android.media.MediaScannerConnection"))
        using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        {
            AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            mediaScanner.CallStatic("scanFile", context, new string[] { path }, null, null);
        }

        Debug.Log("✅ Screenshot saved to Gallery: " + path);

#elif UNITY_IOS
        string path = Path.Combine(Application.persistentDataPath, filename);
        File.WriteAllBytes(path, bytes);
        Debug.Log("✅ Screenshot saved: " + path);
#endif
    }

    // ---------- Brush Selectors with Highlight and Auto-Color Assignment ----------

    /// <summary>Selects the Solid brush and sets the color to Red.</summary>
    public void SetBrushToSolid()
    {
        Debug.Log("Solid selected");
        brushColor = Color.red; // Automatically Red Color Selected
        currentBrush = BrushType.Solid;
        HighlightActiveBrush(0);
    }

    /// <summary>Selects the Crayon brush and sets the color to Aqua (Cyan).</summary>
    public void SetBrushToCrayon()
    {
        Debug.Log("Crayon selected");
        Debug.Log("Aqua (Cyan) selected");
        brushColor = Color.cyan; // Automatically Aqua/Cyan Color Selected
        currentBrush = BrushType.Crayon;
        HighlightActiveBrush(1);
    }

    /// <summary>Selects the Marker brush and sets the color to Pink.</summary>
    public void SetBrushToMarker()
    {
        Debug.Log("Marker selected");
        brushColor = new Color(1f, 0.4f, 0.7f); // Pink Color Selected (R=1, G=0.4, B=0.7)
        currentBrush = BrushType.Marker;
        HighlightActiveBrush(2);
    }

    /// <summary>Selects the Spray brush and sets the color to Green.</summary>
    public void SetBrushToSpray()
    {
        Debug.Log("Spray selected");
        brushColor = Color.green; // Automatically Green Color Selected
        currentBrush = BrushType.Spray;
        HighlightActiveBrush(3);
    }

    /// <summary>Selects the Pencil brush and sets the color to Yellow.</summary>
    public void SetBrushToPencil()
    {
        Debug.Log("Pencil selected");
        brushColor = Color.yellow; // Automatically Yellow Color Selected
        currentBrush = BrushType.Pencil;
        HighlightActiveBrush(4);
    }

    // Moves the active brush button left, others back to zero
    private void HighlightActiveBrush(int activeIndex)
    {
        for (int i = 0; i < brushButtons.Length; i++)
        {
            if (brushButtons[i] == null) continue;

            float targetX = (i == activeIndex) ? activeOffset : 0f;

            // Stop any previous slide coroutine on this button
            StopCoroutine("SlideButton");

            // Start a new smooth slide
            StartCoroutine(SlideButton(brushButtons[i], targetX));
        }
    }

    private IEnumerator SlideButton(RectTransform btn, float targetX)
    {
        Vector2 startPos = btn.anchoredPosition;
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


    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus) SaveDrawingProgress();
    }

    void OnApplicationQuit()
    {
        SaveDrawingProgress();
    }
}