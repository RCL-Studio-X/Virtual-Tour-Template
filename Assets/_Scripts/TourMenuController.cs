using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class TourMenuController : MonoBehaviour
{
    [Header("UI References")]
    public Transform scrollViewContent;
    public GameObject slotPrefab;
    public GameObject menuPanel;

    [Header("Layout Settings")]
    public float slotSpacing = 3f;
    public float bottomPadding = 20f;

    [Header("Configuration")]
    [Tooltip("Optional - custom display names for tour videos")]
    public List<string> videoDisplayNames = new List<string>();

    [Tooltip("Reference to the TourManager")]
    public TourManager tourManager;

    private List<GameObject> createdSlots = new List<GameObject>();

    private void Start()
    {
        if (tourManager != null)
        {
            tourManager.OnVideoChanged += OnVideoChanged;
        }

        PopulateTourMenu();

        if (menuPanel != null)
            menuPanel.SetActive(false);
    }

    public void PopulateTourMenu()
    {
        // Clear existing
        foreach (Transform child in scrollViewContent)
            Destroy(child.gameObject);
        createdSlots.Clear();

        int count = videoDisplayNames.Count;
        int currentIndex = tourManager != null ? tourManager.GetCurrentIndex() : -1;

        VerticalLayoutGroup layoutGroup = scrollViewContent.GetComponent<VerticalLayoutGroup>();
        if (layoutGroup != null)
            layoutGroup.spacing = slotSpacing;

        for (int i = 0; i < count; i++)
        {
            GameObject slot = Instantiate(slotPrefab, scrollViewContent);
            createdSlots.Add(slot);

            Button button = slot.GetComponent<Button>();
            TMP_Text tmpText = slot.GetComponentInChildren<TMP_Text>();
            Text legacyText = slot.GetComponentInChildren<Text>();

            if (button != null)
            {
                if (i == currentIndex)
                {
                    button.interactable = false;
                    ColorBlock colors = button.colors;
                    colors.disabledColor = new Color(0.7f, 0.7f, 0.7f);
                    button.colors = colors;
                }
                else
                {
                    int targetIndex = i;
                    button.onClick.AddListener(() =>
                    {
                        tourManager.PlayVideoAtIndex(targetIndex);
                        RefreshMenu(); // update UI
                    });
                }
            }

            string label = videoDisplayNames[i];
            if (tmpText != null)
                tmpText.text = label;
            else if (legacyText != null)
                legacyText.text = label;
        }

        if (layoutGroup == null)
            ManuallyPositionSlots(createdSlots);

        AdjustContentHeight(createdSlots);
    }

    private void ManuallyPositionSlots(List<GameObject> slots)
    {
        if (slots.Count == 0) return;

        float slotHeight = slots[0].GetComponent<RectTransform>().rect.height;

        for (int i = 0; i < slots.Count; i++)
        {
            RectTransform rect = slots[i].GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(
                rect.anchoredPosition.x,
                -i * (slotHeight + slotSpacing)
            );
        }
    }

    private void AdjustContentHeight(List<GameObject> slots)
    {
        if (slots.Count == 0) return;

        float slotHeight = slots[0].GetComponent<RectTransform>().rect.height;
        float totalHeight = (slotHeight * slots.Count) +
                            (slotSpacing * (slots.Count - 1)) +
                            bottomPadding;

        VerticalLayoutGroup layoutGroup = scrollViewContent.GetComponent<VerticalLayoutGroup>();
        if (layoutGroup != null)
            totalHeight += layoutGroup.padding.top + layoutGroup.padding.bottom;

        RectTransform contentRect = scrollViewContent.GetComponent<RectTransform>();
        contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, totalHeight);
    }

    public void ToggleMenuVisibility()
    {
        if (menuPanel != null)
            menuPanel.SetActive(!menuPanel.activeSelf);
    }

    public void RefreshMenu()
    {
        PopulateTourMenu();
    }

    private void OnVideoChanged(int newIndex)
    {
        RefreshMenu();
    }

    private void OnDestroy()
    {
        if (tourManager != null)
        {
            tourManager.OnVideoChanged -= OnVideoChanged;
        }
    }
}
