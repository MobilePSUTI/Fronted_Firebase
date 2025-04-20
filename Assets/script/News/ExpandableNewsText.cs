using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ExpandableNewsText : MonoBehaviour
{
    public Text textShort; // Используем Text
    public Text textFull;  // Используем Text
    public Button showMoreButton;
    [SerializeField] private int previewLength = 100;

    private string fullText;
    private bool isExpanded = false;
    private RectTransform imageRectTransform; // Контейнер (Image или back)
    private VerticalLayoutGroup containerLayoutGroup; // VerticalLayoutGroup на Image/back

    private void Awake()
    {
        // Проверяем, что все необходимые компоненты привязаны
        if (textShort == null)
        {
            Debug.LogError("textShort is not assigned in the Inspector!", this);
        }
        if (textFull == null)
        {
            Debug.LogError("textFull is not assigned in the Inspector!", this);
        }
        if (showMoreButton == null)
        {
            Debug.LogError("showMoreButton is not assigned in the Inspector!", this);
        }
        if (textShort == null || textFull == null || showMoreButton == null)
        {
            return;
        }

        // Находим RectTransform объекта Image или back (родителя textShort и textFull)
        imageRectTransform = textShort.transform.parent.GetComponent<RectTransform>();
        if (imageRectTransform == null)
        {
            Debug.LogError("Parent of textShort does not have a RectTransform component!", textShort);
            return;
        }

        // Находим VerticalLayoutGroup на контейнере (Image или back)
        containerLayoutGroup = imageRectTransform.GetComponent<VerticalLayoutGroup>();
        if (containerLayoutGroup == null)
        {
            Debug.LogError("VerticalLayoutGroup component is missing on the parent of textShort (Image or back)!", imageRectTransform);
            return;
        }
    }

    public void Initialize(string text)
    {
        // Проверяем, что Awake завершился без ошибок
        if (textShort == null || textFull == null || showMoreButton == null || imageRectTransform == null || containerLayoutGroup == null)
        {
            Debug.LogError("Cannot initialize due to missing components. Check the Awake method errors.", this);
            return;
        }

        fullText = text;
        textFull.text = fullText;

        if (fullText.Length > previewLength)
        {
            textShort.text = fullText.Substring(0, previewLength) + "...";
            showMoreButton.gameObject.SetActive(true);
        }
        else
        {
            textShort.text = fullText;
            showMoreButton.gameObject.SetActive(false);
        }

        textShort.gameObject.SetActive(true);
        textFull.gameObject.SetActive(false);

        // Удаляем предыдущие слушатели, чтобы избежать дублирования
        showMoreButton.onClick.RemoveAllListeners();
        showMoreButton.onClick.AddListener(ToggleText);

        // Обновляем макет после инициализации
        UpdateLayout();
    }

    private void ToggleText()
    {
        isExpanded = !isExpanded;

        textShort.gameObject.SetActive(!isExpanded);
        textFull.gameObject.SetActive(isExpanded);

        // Обновляем текст кнопки (для TextMeshProUGUI)
        var buttonText = showMoreButton.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
        {
            buttonText.text = isExpanded ? "Скрыть" : "Ещё";
        }
        else
        {
            Debug.LogWarning("Button text (TextMeshProUGUI) component not found! Make sure the button has a TextMeshProUGUI child.", showMoreButton);
        }

        // Обновляем макет
        UpdateLayout();
    }

    private void UpdateLayout()
    {
        // Принудительно обновляем макет для активного текста
        if (isExpanded)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(textFull.rectTransform);
        }
        else
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(textShort.rectTransform);
        }

        // Обновляем макет контейнера (Image или back), чтобы он подстроился под содержимое
        LayoutRebuilder.ForceRebuildLayoutImmediate(imageRectTransform);

        // Обновляем макет корневого объекта (Panel_news или Panel_news_netPhoto)
        LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
    }
}