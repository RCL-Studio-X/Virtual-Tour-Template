using UnityEngine;
using TMPro;

/// <summary>
/// Manages a title canvas by updating its header text and ensuring the canvas is enabled
/// when a title is set.
/// </summary>
[RequireComponent(typeof(Canvas))]
public class TitleCanvasManager : MonoBehaviour
{
    /// <summary>
    /// The TextMeshProUGUI component used as the visual title header.
    /// </summary>
    [Header("UI References")]
    [Tooltip("Reference to the TextMeshProUGUI component used as the title header.")]
    public TextMeshProUGUI header;

    private Canvas _canvas;

    /// <summary>
    /// Initializes component references and validates required components.
    /// </summary>
    private void Awake()
    {
        _canvas = GetComponent<Canvas>();

        if (!_canvas)
        {
            Debug.LogError(
                $"[{nameof(TitleCanvasManager)}] Canvas component is missing on GameObject '{gameObject.name}'."
            );
            enabled = false;
        }

        if (!header)
        {
            Debug.LogWarning(
                $"[{nameof(TitleCanvasManager)}] Header TextMeshProUGUI is not assigned on '{gameObject.name}'."
            );
        }
    }

    /// <summary>
    /// Sets the displayed title text and enables the canvas.
    /// </summary>
    /// <param name="title">The text to display as the title.</param>
    public void SetTitle(string title)
    {
        if (!header)
        {
            Debug.LogWarning(
                $"[{nameof(TitleCanvasManager)}] Cannot set title: Header reference is missing."
            );
            return;
        }

        if (!_canvas)
        {
            Debug.LogError(
                $"[{nameof(TitleCanvasManager)}] Cannot enable canvas: Canvas reference is missing."
            );
            return;
        }

        header.SetText(title);
        _canvas.enabled = true;
    }
}
