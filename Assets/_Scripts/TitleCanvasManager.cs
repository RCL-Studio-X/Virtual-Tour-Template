using UnityEngine;
using TMPro;

[RequireComponent(typeof(Canvas))]
public class TitleCanvasManager : MonoBehaviour
{
    [Tooltip("Reference to the TextMeshProUGUI component used as the title header.")]
    public TextMeshProUGUI header;

    private Canvas canvas;

    void Awake()
    {
        canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError($"[{nameof(TitleCanvasManager)}] Canvas component is missing on GameObject '{gameObject.name}'. This component requires a Canvas.");
            enabled = false; // Disable script to prevent further issues
        }

        if (header == null)
        {
            Debug.LogWarning($"[{nameof(TitleCanvasManager)}] Header (TextMeshProUGUI) is not assigned in the inspector on GameObject '{gameObject.name}'.");
        }
    }

    public void SetTitle(string title)
    {
        if (header == null)
        {
            Debug.LogWarning($"[{nameof(TitleCanvasManager)}] Cannot set title: Header reference is null.");
            return;
        }

        if (canvas == null)
        {
            Debug.LogError($"[{nameof(TitleCanvasManager)}] Cannot enable canvas: Canvas reference is null.");
            return;
        }

        header.SetText(title);
        canvas.enabled = true;
    }
}
