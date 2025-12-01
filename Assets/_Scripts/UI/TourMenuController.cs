using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using StudioX.VirtualTour.Core;
using TMPro;

namespace StudioX.VirtualTour.UI
{
    /// <summary>
    /// Controls population and interaction behavior for the tour selection menu.
    /// </summary>
    public class TourMenuController : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("Parent transform containing dynamically created menu slots.")]
        public Transform scrollViewContent;

        [Tooltip("Prefab for each selectable menu slot.")]
        public GameObject slotPrefab;

        [Tooltip("Panel containing the tour menu UI.")]
        public GameObject menuPanel;

        [Header("Layout Settings")]
        [Tooltip("Vertical spacing between each menu slot.")]
        public float slotSpacing = 3f;

        [Tooltip("Padding at the bottom of the scroll view content.")]
        public float bottomPadding = 20f;

        private List<string> _videoDisplayNames;
        private TourManager _tourManager;

        private readonly List<GameObject> _createdSlots = new();

        /// <summary>
        /// Initializes manager references and populates the menu on startup.
        /// </summary>
        private void Start()
        {
            if (!_tourManager)
                _tourManager = FindFirstObjectByType<TourManager>();

            if (_tourManager)
            {
                _videoDisplayNames = _tourManager.videoDisplayNames;
                _tourManager.OnVideoChanged += OnVideoChanged;
            }

            PopulateTourMenu();

            if (menuPanel)
                menuPanel.SetActive(false);
        }

        /// <summary>
        /// Rebuilds the list of video entries displayed within the menu.
        /// </summary>
        public void PopulateTourMenu()
        {
            foreach (Transform child in scrollViewContent)
                Destroy(child.gameObject);

            _createdSlots.Clear();

            int count = _videoDisplayNames?.Count ?? 0;
            int currentIndex = _tourManager ? _tourManager.GetCurrentIndex() : -1;

            var layoutGroup = scrollViewContent.GetComponent<VerticalLayoutGroup>();
            if (layoutGroup)
                layoutGroup.spacing = slotSpacing;

            for (int i = 0; i < count; i++)
            {
                var slot = Instantiate(slotPrefab, scrollViewContent);
                _createdSlots.Add(slot);

                var button = slot.GetComponent<Button>();
                var tmpText = slot.GetComponentInChildren<TMP_Text>();
                var legacyText = slot.GetComponentInChildren<Text>();

                // --- Rewritten switch → if/else ---

                if (button == null)
                {
                    // Do nothing
                }
                else if (i == currentIndex)   // currently selected
                {
                    button.interactable = false;
                    var colors = button.colors;
                    colors.disabledColor = new Color(0.7f, 0.7f, 0.7f);
                    button.colors = colors;
                }
                else  // clickable entries
                {
                    int targetIndex = i;
                    button.onClick.AddListener(() =>
                    {
                        _tourManager.PlayVideoAtIndex(targetIndex);
                        RefreshMenu();
                    });
                }

                // Assign text
                if (_videoDisplayNames != null)
                {
                    string label = _videoDisplayNames[i];
                    if (tmpText)
                        tmpText.text = label;
                    else if (legacyText)
                        legacyText.text = label;
                }
            }

            if (!layoutGroup)
                ManuallyPositionSlots(_createdSlots);

            AdjustContentHeight(_createdSlots);
        }


        /// <summary>
        /// Positions slots manually when no layout group is present.
        /// </summary>
        private void ManuallyPositionSlots(List<GameObject> slots)
        {
            if (slots.Count == 0)
                return;

            float slotHeight = slots[0].GetComponent<RectTransform>().rect.height;

            for (int i = 0; i < slots.Count; i++)
            {
                var rect = slots[i].GetComponent<RectTransform>();
                rect.anchoredPosition = new Vector2(
                    rect.anchoredPosition.x,
                    -(i * (slotHeight + slotSpacing))
                );
            }
        }

        /// <summary>
        /// Adjusts the scroll view content height to fit all created menu slots.
        /// </summary>
        private void AdjustContentHeight(List<GameObject> slots)
        {
            if (slots.Count == 0)
                return;

            float slotHeight = slots[0].GetComponent<RectTransform>().rect.height;
            float totalHeight = (slotHeight * slots.Count) +
                                (slotSpacing * (slots.Count - 1)) +
                                bottomPadding;

            var layoutGroup = scrollViewContent.GetComponent<VerticalLayoutGroup>();
            if (layoutGroup)
                totalHeight += layoutGroup.padding.top + layoutGroup.padding.bottom;

            var contentRect = scrollViewContent.GetComponent<RectTransform>();
            contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, totalHeight);
        }

        /// <summary>
        /// Toggles the visibility of the tour menu panel.
        /// </summary>
        public void ToggleMenuVisibility()
        {
            if (menuPanel)
                menuPanel.SetActive(!menuPanel.activeSelf);
        }

        /// <summary>
        /// Refreshes the menu, rebuilding all visible slots.
        /// </summary>
        public void RefreshMenu() =>
            PopulateTourMenu();

        /// <summary>
        /// Called when the current video changes, ensuring the menu updates to reflect the active selection.
        /// </summary>
        private void OnVideoChanged(int newIndex) =>
            RefreshMenu();

        /// <summary>
        /// Unsubscribes from tour events when this controller is destroyed.
        /// </summary>
        private void OnDestroy()
        {
            if (_tourManager)
                _tourManager.OnVideoChanged -= OnVideoChanged;
        }
    }
}
