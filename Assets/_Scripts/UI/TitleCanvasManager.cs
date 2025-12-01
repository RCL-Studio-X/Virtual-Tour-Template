using UnityEngine;
using TMPro;

namespace StudioX.VirtualTour.UI
{
    /// <summary>
    /// Manages the title canvas during initialization and updates the displayed title text.
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public class TitleCanvasManager : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("Reference to the TextMeshProUGUI component used as the title header.")]
        public TextMeshProUGUI header;

        private Canvas _canvas;

        /// <summary>
        /// Initializes internal references and validates required components.
        /// </summary>
        private void Awake()
        {
            _canvas = GetComponent<Canvas>();

            if (!_canvas)
            {
                Debug.LogError(
                    $"[{nameof(TitleCanvasManager)}] Canvas component is missing on GameObject '{gameObject.name}'. This component requires a Canvas.");
                enabled = false;
            }

            if (!header)
            {
                Debug.LogWarning(
                    $"[{nameof(TitleCanvasManager)}] Header (TextMeshProUGUI) is not assigned in the inspector on GameObject '{gameObject.name}'.");
            }
        }

        /// <summary>
        /// Sets the title text and ensures the canvas is visible.
        /// </summary>
        /// <param name="title">The title text to display.</param>
        public void SetTitle(string title)
        {
            if (!_canvas)
            {
                Debug.LogError(
                    $"[{nameof(TitleCanvasManager)}] Cannot enable canvas: Canvas reference is missing.");
                return;
            }

            if (!header)
            {
                Debug.LogWarning(
                    $"[{nameof(TitleCanvasManager)}] Cannot set title: Header reference is missing.");
                return;
            }

            header.SetText(title);
            _canvas.enabled = true;
        }
    }
}
