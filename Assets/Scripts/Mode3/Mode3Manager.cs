using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Random = UnityEngine.Random; // Clarify System.Random vs UnityEngine.Random

public class Mode3Manager : MonoBehaviour
{
    // 🎨 Types of brushes available
    public enum BrushType
    {
        None,       // ❌ no drawing, no sound
        Solid,
        Crayon,
        Marker,
        Spray,
        Pencil
    }
    [Header("Sound")]
    public Mode3SoundManager soundManager;

    [Header("UI Elements")]
    public Slider brushSizeSlider; // Brush size slider from UI

    [Header("Brush Settings")]
    public Color brushColor = Color.black; // Current selected brush color
    public BrushType currentBrush = BrushType.Solid; // Default brush

    // =========================================================================
    // ✨ OPTIMIZATION & HIGHLIGHT FIELDS ADDED
    // =========================================================================

    // OPTIMIZATION: The critical CPU-side array to prevent lag
    private Color[] drawingPixels;
    // Flag to check if we need to call Apply()
    private bool hasDrawnThisFrame = false;

    [Header("UI – Brush Buttons")]
    public RectTransform[] brushButtons;
    [SerializeField] private float activeOffset = -20f; // Visual offset for active button

    // Store the original brush type for size preview restoration
    private BrushType savedBrushType;
    // =========================================================================

    // --- Private Drawing State ---
    private RawImage drawingArea; // UI element to draw on
    private Texture2D drawingTexture; // Actual texture we modify for drawing
    private RectTransform drawingRect; // Rect of drawing area

    private Vector2? lastMousePos = null; // Tracks last mouse position for smooth lines
    private Color savedColor; // Used when temporarily overriding brush color
    private bool isAdjustingBrush = false; // Flag for brush preview mode
    private readonly int textureScale = 1; // Can downscale texture if needed
    private bool isBrushing = false;

    // Helper method to get the index in the 1D color array
    private int GetIndex(int x, int y)
    {
        // Add a check to prevent IndexOutOfRangeException if coordinates somehow exceed bounds
        if (drawingTexture == null) return -1;
        if (x < 0 || x >= drawingTexture.width || y < 0 || y >= drawingTexture.height) return -1;

        return y * drawingTexture.width + x;
    }

    // =========================================================================
    // INITIALIZATION & SETUP
    // =========================================================================

    void OnEnable()
    {
        InitializeDrawing();
        HighlightActiveBrush((int)currentBrush);
    }

    /// <summary>
    /// Finds the correct drawing image, sets up a drawable texture, and initializes drawingPixels.
    /// </summary>
    void InitializeDrawing()
    {
        int selectedImageIndex = PlayerPrefs.GetInt("Mode3Image", 0);
        if (selectedImageIndex <= 0)
        {
            Debug.LogError("Mode3Manager: Invalid or unset Mode3Image PlayerPref.");
            return;
        }

        string objectName = GetObjectNameFromIndex(selectedImageIndex);
        GameObject activeObject = GameObject.Find(objectName);
        if (activeObject == null)
        {
            Debug.LogError("Mode3Manager: No GameObject found with name: " + objectName);
            return;
        }

        drawingArea = activeObject.GetComponentInChildren<RawImage>();
        drawingRect = drawingArea?.rectTransform;

        Texture2D sourceTexture = drawingArea.texture as Texture2D;
        if (sourceTexture == null)
        {
            Debug.LogError("Mode3Manager: RawImage texture is not a readable Texture2D. Creating blank fallback.");
            sourceTexture = new Texture2D(512, 512, TextureFormat.RGBA32, false);
            Color[] whitePixels = new Color[512 * 512];
            for (int i = 0; i < whitePixels.Length; i++) whitePixels[i] = Color.white;
            sourceTexture.SetPixels(whitePixels);
            sourceTexture.Apply();
        }

        // Create the actual drawable texture clone
        drawingTexture = new Texture2D(sourceTexture.width / textureScale, sourceTexture.height / textureScale, TextureFormat.RGBA32, false);

        // OPTIMIZATION: Initialize the CPU-side pixel array once
        drawingPixels = sourceTexture.GetPixels();

        drawingTexture.SetPixels(drawingPixels);
        drawingTexture.Apply();

        drawingArea.texture = drawingTexture;
    }

    // =========================================================================
    // DRAWING LOOP & OPTIMIZED APPLY
    // =========================================================================
    void Update()
    {
        bool isMouseDown = Input.GetMouseButton(0);

        if (isMouseDown && drawingRect != null)
        {
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                drawingRect,
                Input.mousePosition,
                null,
                out Vector2 localMousePos))
            {
                if (drawingRect.rect.Contains(localMousePos))
                {
                    // 🔊 START BRUSH SOUND (only once)
                    if (!isBrushing)
                    {
                        isBrushing = true;
                        soundManager?.PlayBrushingSound();
                    }

                    hasDrawnThisFrame = true;

                    Vector2 curr = localMousePos;

                    if (lastMousePos == null)
                    {
                        DrawStrokeAt(curr);
                    }
                    else
                    {
                        Vector2 prev = lastMousePos.Value;
                        float distance = Vector2.Distance(prev, curr);
                        int steps = Mathf.CeilToInt(distance * 2f);

                        for (int i = 0; i <= steps; i++)
                        {
                            Vector2 lerped = Vector2.Lerp(prev, curr, i / (float)steps);
                            DrawStrokeAt(lerped);
                        }
                    }

                    lastMousePos = curr;
                    return; // ⛔ IMPORTANT: exit early so stop logic doesn't trigger
                }
            }
        }

        // 🛑 STOP BRUSHING (mouse released or left drawing area)
        if (isBrushing)
        {
            isBrushing = false;
            soundManager?.StopBrushingSound();
        }

        lastMousePos = null;
    }

    void OnDisable()
    {
        isBrushing = false;
        soundManager?.StopBrushingSound();
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
    }

    // =========================================================================
    // CORE DRAWING LOGIC (Rewritten for Optimization and Texture)
    // =========================================================================

    /// <summary>
    /// Converts local UI position to texture pixel coordinates and calls the appropriate brush function.
    /// </summary>
    void DrawStrokeAt(Vector2 localPos)
    {
        // Convert local UI position to normalized (0 to 1) coordinates
        float normalizedX = (localPos.x + drawingRect.rect.width / 2f) / drawingRect.rect.width;
        float normalizedY = (localPos.y + drawingRect.rect.height / 2f) / drawingRect.rect.height;

        // Convert normalized to pixel coordinates
        int centerX = Mathf.RoundToInt(normalizedX * drawingTexture.width);
        int centerY = Mathf.RoundToInt(normalizedY * drawingTexture.height);
        int radius = Mathf.RoundToInt(brushSizeSlider.value);

        switch (currentBrush)
        {
            case BrushType.Solid:
                DrawSolidBrush(centerX, centerY, radius);
                break;

            case BrushType.Crayon:
                DrawCrayonBrush(centerX, centerY, radius);
                break;

            case BrushType.Marker:
                DrawMarkerBrush(centerX, centerY, radius);
                break;

            case BrushType.Pencil:
                DrawPencilBrush(centerX, centerY, radius);
                break;

            case BrushType.Spray:
                DrawSprayBrush(centerX, centerY, radius);
                break;

            case BrushType.None:
            default:
                // Do nothing
                break;
        }

    }

    /// <summary>Draws a fully opaque, solid circle. (OPTIMIZED)</summary>
    void DrawSolidBrush(int centerX, int centerY, int radius)
    {
        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                if (x * x + y * y > radius * radius) continue;

                int px = Mathf.Clamp(centerX + x, 0, drawingTexture.width - 1);
                int py = Mathf.Clamp(centerY + y, 0, drawingTexture.height - 1);

                int index = GetIndex(px, py);
                if (index != -1) drawingPixels[index] = brushColor;
            }
        }
    }

    /// <summary>Draws a thick, opaque-like brush with random texture/sparsity for a CRAYON effect. (OPTIMIZED)</summary>
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

                int index = GetIndex(px, py);
                if (index == -1) continue;

                Color existing = drawingPixels[index];

                // Waxy blend with slight opacity variation
                float blendAlpha = opacity + Random.Range(-0.1f, 0.1f);
                Color finalColor = Color.Lerp(existing, brushColor, blendAlpha);

                // Slight value randomization for wax texture
                float h, s, v;
                Color.RGBToHSV(finalColor, out h, out s, out v);
                v = Mathf.Clamp01(v + Random.Range(-0.05f, 0.05f));
                finalColor = Color.HSVToRGB(h, s, v);

                drawingPixels[index] = finalColor;
            }
        }
    }

    /// <summary>Draws a semi-translucent, smooth brush for a MARKER effect. (OPTIMIZED)</summary>
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

                int index = GetIndex(px, py);
                if (index == -1) continue;

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

                drawingPixels[index] = finalColor;
            }
        }
    }

    /// <summary>Draws a very light, scratchy, and buildable PENCIL effect. (OPTIMIZED)</summary>
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

                int index = GetIndex(px, py);
                if (index == -1) continue;

                Color existing = drawingPixels[index];

                // Very low alpha blend for soft, layered graphite look
                Color finalColor = Color.Lerp(existing, brushColor, pencilOpacity);

                drawingPixels[index] = finalColor;
            }
        }
    }

    /// <summary>Draws a dispersed SPRAY PAINT effect. (OPTIMIZED)</summary>
    void DrawSprayBrush(int centerX, int centerY, int radius)
    {
        int sprayCount = radius * 5; // Increased spray count
        float sprayOpacity = 0.15f;

        for (int i = 0; i < sprayCount; i++)
        {
            Vector2 offset = Random.insideUnitCircle * radius;
            int px = Mathf.Clamp(centerX + Mathf.RoundToInt(offset.x), 0, drawingTexture.width - 1);
            int py = Mathf.Clamp(centerY + Mathf.RoundToInt(offset.y), 0, drawingTexture.height - 1);

            int index = GetIndex(px, py);
            if (index == -1) continue;

            Color existing = drawingPixels[index];

            Color blended = Color.Lerp(existing, brushColor, sprayOpacity + Random.Range(-0.05f, 0.05f));

            drawingPixels[index] = blended;
        }
    }

    // =========================================================================
    // UTILITY & UI METHODS (Updated to use drawingPixels)
    // =========================================================================

    string GetObjectNameFromIndex(int index)
    {
        // ... (Logic remains the same)
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

    public void SetBrushColor(Color color) => brushColor = color;

    public void ClearCanvas()
    {
        if (drawingTexture == null || drawingArea == null) return;

        // Reset the CPU array to white
        for (int i = 0; i < drawingPixels.Length; i++)
            drawingPixels[i] = Color.white;

        // Apply changes once
        drawingTexture.SetPixels(drawingPixels);
        drawingTexture.Apply();

        Debug.Log("✅ Canvas cleadred");
    }

    // ----------------- Eraser -----------------
    public void UseEraser()
    {
        // Eraser is Solid brush painting with white
        currentBrush = BrushType.Solid;
        brushColor = Color.white;
        HighlightActiveBrush(0); // Assuming Solid is button 0
    }

    // ----------------- Brush Size Preview -----------------
    public void OnBrushSizeDragStart()
    {
        if (!isAdjustingBrush)
        {
            savedColor = brushColor;
            // FIX: Save the original brush type
            savedBrushType = currentBrush;

            // Temporarily set brush to white and solid to simulate a simple preview circle
            brushColor = Color.white;
            currentBrush = BrushType.Solid;
            isAdjustingBrush = true;
        }
    }

    public void OnBrushSizeDragEnd()
    {
        if (isAdjustingBrush)
        {
            StartCoroutine(RestoreBrushColorAndTypeAfterDelay(0.1f));
        }
    }

    private IEnumerator RestoreBrushColorAndTypeAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        // FIX: Restore both color AND brush type
        brushColor = savedColor;
        currentBrush = savedBrushType;
        isAdjustingBrush = false;

        // Re-highlight the correct button after restoring the type
        HighlightActiveBrush((int)currentBrush);
    }

    // ----------------- Brush Switch Buttons -----------------

    public void SetBrushToNone()
    {
        Debug.Log("None Brush Selected");
        currentBrush = BrushType.None;

        if (soundManager != null)
        {
            soundManager.allowBrushingSound = false; // ❌ disable sound
            soundManager.StopBrushingSound();        // stop immediately
        }

        HighlightActiveBrush(-1); // no button highlighted
    }

    public void SetBrushToSolid()
    {
        Debug.Log("Solid Brush Selected, Red");
        brushColor = Color.red;
        currentBrush = BrushType.Solid;

        if (soundManager != null)
            soundManager.allowBrushingSound = true;

        HighlightActiveBrush(0);
    }

    public void SetBrushToCrayon()
    {
        Debug.Log("Crayon Brush Selected, Aqua");
        brushColor = Color.cyan;
        currentBrush = BrushType.Crayon;

        if (soundManager != null)
            soundManager.allowBrushingSound = true;

        HighlightActiveBrush(1);
    }

    public void SetBrushToMarker()
    {
        Debug.Log("Marker Brush Selected, Pink");
        brushColor = new Color(1f, 0.4f, 0.7f);
        currentBrush = BrushType.Marker;

        if (soundManager != null)
            soundManager.allowBrushingSound = true;

        HighlightActiveBrush(2);
    }

    public void SetBrushToSpray()
    {
        Debug.Log("Spray Brush Selected, Green");
        brushColor = Color.green;
        currentBrush = BrushType.Spray;

        if (soundManager != null)
            soundManager.allowBrushingSound = true;

        HighlightActiveBrush(3);
    }

    public void SetBrushToPencil()
    {
        Debug.Log("Pencil Brush Selected, Yellow");
        brushColor = Color.yellow;
        currentBrush = BrushType.Pencil;

        if (soundManager != null)
            soundManager.allowBrushingSound = true;

        HighlightActiveBrush(4);
    }

    // ----------------- UI Highlight Logic (Restored) -----------------

    /// <summary>Moves the active brush button left, others back to zero.</summary>
    private void HighlightActiveBrush(int activeIndex)
    {
        for (int i = 0; i < brushButtons.Length; i++)
        {
            if (brushButtons[i] == null) continue;

            float targetX = (i == activeIndex) ? activeOffset : 0f;

            // Stop any previous slide coroutine on this button before starting a new one
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
            // Use Lerp for smooth movement
            btn.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
            yield return null;
        }

        btn.anchoredPosition = endPos;
    }
}