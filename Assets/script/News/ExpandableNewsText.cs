using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ExpandableNewsText : MonoBehaviour
{
    [SerializeField] public Text textShort;
    [SerializeField] public Text textFull;
    [SerializeField] public Button showMoreButton;
    [SerializeField] public int previewLength = 100;

    private string fullText;
    private bool isExpanded = false;
    private RectTransform imageRectTransform;
    private float originalHeight;
    private VerticalLayoutGroup layoutGroup;

    private void Awake()
    {
        // Проверяем, что все необходимые компоненты привязаны
        if (textShort == null || textFull == null || showMoreButton == null)
        {
            Debug.LogError("One or more required components (textShort, textFull, showMoreButton) are not assigned in the Inspector!", this);
            return;
        }

        // Находим RectTransform объекта Image (родителя textShort и textFull)
        imageRectTransform = textShort.transform.parent.GetComponent<RectTransform>();
        if (imageRectTransform == null)
        {
            Debug.LogError("Parent of textShort does not have a RectTransform component!", textShort);
            return;
        }

        layoutGroup = GetComponent<VerticalLayoutGroup>();
        if (layoutGroup == null)
        {
            Debug.LogError("VerticalLayoutGroup component is missing on this GameObject!", this);
            return;
        }

        // Сохраняем исходную высоту объекта Image
        originalHeight = imageRectTransform.sizeDelta.y;
    }

    private void Start()
    {
        // Тестовый вызов для проверки
        Initialize("Это тестовый текст, который должен быть достаточно длинным, чтобы показать кнопку 'Ещё'. Добавим побольше текста, чтобы точно превысить длину предпросмотра в 100 символов!");
    }

    public void Initialize(string text)
    {
        // Проверяем, что Awake завершился без ошибок
        if (textShort == null || textFull == null || showMoreButton == null || imageRectTransform == null)
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

        // Изменяем размер Image в зависимости от состояния
        if (isExpanded)
        {
            // Устанавливаем высоту Image на основе размера полного текста
            LayoutRebuilder.ForceRebuildLayoutImmediate(textFull.rectTransform);
            float fullTextHeight = textFull.preferredHeight;
            imageRectTransform.sizeDelta = new Vector2(imageRectTransform.sizeDelta.x, fullTextHeight);
        }
        else
        {
            // Возвращаем исходную высоту
            imageRectTransform.sizeDelta = new Vector2(imageRectTransform.sizeDelta.x, originalHeight);
        }

        // Обновляем макет
        LayoutRebuilder.ForceRebuildLayoutImmediate(imageRectTransform);
        LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
    }
}