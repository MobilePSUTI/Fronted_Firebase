using UnityEngine;
using UnityEngine.UI;
using TMPro; // Добавляем пространство имен для TextMeshPro

public class ExpandableNewsText : MonoBehaviour
{
    [Header("UI Elements")]
    public Text textShort;
    public Text textFull;
    public Button showMoreButton;

    [Header("Settings")]
    public int previewLength = 150;

    private bool isExpanded = false;
    private string fullText = "";

    void Start()
    {
        if (showMoreButton != null)
        {
            showMoreButton.onClick.AddListener(ToggleText);
        }
    }

    public void Initialize(string text)
    {
        if (textShort == null || textFull == null || showMoreButton == null)
        {
            Debug.LogError("UI элементы не назначены в префабе!");
            return;
        }

        fullText = text;

        int length = Mathf.Min(previewLength, text.Length);
        string previewText = text.Substring(0, length);
        if (text.Length > length) previewText += "...";

        textShort.text = previewText;
        textFull.text = fullText;

        showMoreButton.gameObject.SetActive(text.Length > previewLength);

        textShort.gameObject.SetActive(true);
        textFull.gameObject.SetActive(false);
        UpdateButtonText();
    }

    private void ToggleText()
    {
        isExpanded = !isExpanded;
        textShort.gameObject.SetActive(!isExpanded);
        textFull.gameObject.SetActive(isExpanded);
        UpdateButtonText();

        LayoutRebuilder.ForceRebuildLayoutImmediate(transform as RectTransform);
    }

    private void UpdateButtonText()
    {
        if (showMoreButton == null) return;

        // Изменяем на поиск компонента TextMeshProUGUI
        TextMeshProUGUI buttonText = showMoreButton.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
        {
            buttonText.text = isExpanded ? "Скрыть" : "Ещё";
        }
        else
        {
            Debug.LogWarning("Не найден компонент TextMeshProUGUI на кнопке");
        }
    }
}