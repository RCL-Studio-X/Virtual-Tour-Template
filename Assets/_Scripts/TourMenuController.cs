using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// Generates and manages a list of selectable tour video slots inside a scroll view.
/// </summary>
public class TourMenuController : MonoBehaviour
{
    /// <summary>
    /// The content container inside the ScrollView where video selection slots will be added.
    /// </summary>
    [Header("UI References")]
    [Tooltip("Content container inside the ScrollView where video selection slots will be added.")]
    public Transform scrollViewContent;

    /// <summary>
    /// Prefab used to generate individual video selection slots.
    /// </summary>
    [Tooltip("Prefab used to generate individual video selection slots.")]
    public GameObject slotPrefab;

    /// <summary>
    /// The main menu panel that can be shown or hidden.
    /// </summary>
    [Tooltip("The main menu panel that can be shown or hidden.")]
    public GameObject menuPanel;

    /// <summary>
    /// Vertical spacing between generated slots.
    /// </summary>
    [Header("Layout Settings")]
    [Tooltip("Vertical spacing between generated slots.")]
    public float slotSpacing = 3f;

    /// <summary>
    /// Extra padding added below the last element.
    /// </summary>
    [Tooltip("Extra padding added below the last element.")]
    public float bottomPadding = 20f;

    private List<string> _videoDisplayNames;
    private TourManager _tourManager;
    private readonly List<GameObject> _createdSlots = new();

    /// <summary>
    /// Initializes menu setup and registers event listeners.
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
    /// Creates and arranges all menu slots according to the tour video list.
    /// </summary>
    public void PopulateTourMenu()
    {
        foreach (Transform child in scrollViewContent)
            Destroy(child.gameObject);

        _createdSlots.Clear();

        int count = _videoDisplayNames.Count;
        int currentIndex = _tourManager ? _tourManager.CurrentVideoIndex : -1;

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

            if (button)
            {
                if (i == currentIndex)
                {
                    button.interactable = false;
                    var colors = button.colors;
                    colors.disabledColor = new Color(0.7f, 0.7f, 0.7f);
                    button.colors = colors;
                }
                else
                {
                    int targetIndex = i;
                    button.onClick.AddListener(() =>
                    {
                        _tourManager.PlayVideoAtIndex(targetIndex);
                        RefreshMenu();
                    });
                }
            }

            string label = _videoDisplayNames[i];
            if (tmpText)
                tmpText.text = label;
            else if (legacyText)
                legacyText.text = label;
        }

        if (!layoutGroup)
            ManuallyPositionSlots(_createdSlots);

        AdjustContentHeight(_createdSlots);
    }

    /// <summary>
    /// Manually positions slots vertically when no layout group is present.
    /// </summary>
    /// <param name="slots">List of slot objects to position.</param>
    private void ManuallyPositionSlots(List<GameObject> slots)
    {
        if (slots.Count == 0)
            return;

        float slotHeight = slots[0].GetComponent<RectTransform>().rect.height;

        for (int i = 0; i < slots.Count; i++)
        {
            RectTransform rect = slots[i].GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(
                rect.anchoredPosition.x,
                -(i * (slotHeight + slotSpacing))
            );
        }
    }

    /// <summary>
    /// Adjusts the total height of the ScrollView content to fit all generated slots.
    /// </summary>
    /// <param name="slots">List of generated slot objects.</param>
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

        RectTransform contentRect = scrollViewContent.GetComponent<RectTransform>();
        contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, totalHeight);
    }

    /// <summary>
    /// Shows or hides the menu panel.
    /// </summary>
    public void ToggleMenuVisibility()
    {
        if (!menuPanel)
            return;

        menuPanel.SetActive(!menuPanel.activeSelf);
    }

    /// <summary>
    /// Rebuilds the menu UI to reflect current selection or updated data.
    /// </summary>
    public void RefreshMenu()
    {
        PopulateTourMenu();
    }

    /// <summary>
    /// Called when the TourManager reports that the active video has changed.
    /// </summary>
    /// <param name="newIndex">The index of the newly activated video.</param>
    private void OnVideoChanged(int newIndex)
    {
        RefreshMenu();
    }

    /// <summary>
    /// Unregisters event listeners upon destruction.
    /// </summary>
    private void OnDestroy()
    {
        if (_tourManager)
            _tourManager.OnVideoChanged -= OnVideoChanged;
    }
}
