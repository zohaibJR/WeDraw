using UnityEngine;
using UnityEngine.UI;

public class ColorButton : MonoBehaviour
{
    public DrawingManager drawingManager;

    void Start()
    {
        GetComponent<Button>().onClick.AddListener(() =>
        {
            drawingManager.SetBrushColor(GetComponent<Image>().color);
        });
    }
}
