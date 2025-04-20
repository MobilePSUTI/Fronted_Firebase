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
        // ���������, ��� ��� ����������� ���������� ���������
        if (textShort == null || textFull == null || showMoreButton == null)
        {
            Debug.LogError("One or more required components (textShort, textFull, showMoreButton) are not assigned in the Inspector!", this);
            return;
        }

        // ������� RectTransform ������� Image (�������� textShort � textFull)
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

        // ��������� �������� ������ ������� Image
        originalHeight = imageRectTransform.sizeDelta.y;
    }

    private void Start()
    {
        // �������� ����� ��� ��������
        Initialize("��� �������� �����, ������� ������ ���� ���������� �������, ����� �������� ������ '���'. ������� �������� ������, ����� ����� ��������� ����� ������������� � 100 ��������!");
    }

    public void Initialize(string text)
    {
        // ���������, ��� Awake ���������� ��� ������
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

        // ������� ���������� ���������, ����� �������� ������������
        showMoreButton.onClick.RemoveAllListeners();
        showMoreButton.onClick.AddListener(ToggleText);
    }

    private void ToggleText()
    {
        isExpanded = !isExpanded;

        textShort.gameObject.SetActive(!isExpanded);
        textFull.gameObject.SetActive(isExpanded);

        // ��������� ����� ������ (��� TextMeshProUGUI)
        var buttonText = showMoreButton.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
        {
            buttonText.text = isExpanded ? "������" : "���";
        }
        else
        {
            Debug.LogWarning("Button text (TextMeshProUGUI) component not found! Make sure the button has a TextMeshProUGUI child.", showMoreButton);
        }

        // �������� ������ Image � ����������� �� ���������
        if (isExpanded)
        {
            // ������������� ������ Image �� ������ ������� ������� ������
            LayoutRebuilder.ForceRebuildLayoutImmediate(textFull.rectTransform);
            float fullTextHeight = textFull.preferredHeight;
            imageRectTransform.sizeDelta = new Vector2(imageRectTransform.sizeDelta.x, fullTextHeight);
        }
        else
        {
            // ���������� �������� ������
            imageRectTransform.sizeDelta = new Vector2(imageRectTransform.sizeDelta.x, originalHeight);
        }

        // ��������� �����
        LayoutRebuilder.ForceRebuildLayoutImmediate(imageRectTransform);
        LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
    }
}