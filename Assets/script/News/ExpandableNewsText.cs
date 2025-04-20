using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ExpandableNewsText : MonoBehaviour
{
    public Text textShort; // ���������� Text
    public Text textFull;  // ���������� Text
    public Button showMoreButton;
    [SerializeField] private int previewLength = 100;

    private string fullText;
    private bool isExpanded = false;
    private RectTransform imageRectTransform; // ��������� (Image ��� back)
    private VerticalLayoutGroup containerLayoutGroup; // VerticalLayoutGroup �� Image/back

    private void Awake()
    {
        // ���������, ��� ��� ����������� ���������� ���������
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

        // ������� RectTransform ������� Image ��� back (�������� textShort � textFull)
        imageRectTransform = textShort.transform.parent.GetComponent<RectTransform>();
        if (imageRectTransform == null)
        {
            Debug.LogError("Parent of textShort does not have a RectTransform component!", textShort);
            return;
        }

        // ������� VerticalLayoutGroup �� ���������� (Image ��� back)
        containerLayoutGroup = imageRectTransform.GetComponent<VerticalLayoutGroup>();
        if (containerLayoutGroup == null)
        {
            Debug.LogError("VerticalLayoutGroup component is missing on the parent of textShort (Image or back)!", imageRectTransform);
            return;
        }
    }

    public void Initialize(string text)
    {
        // ���������, ��� Awake ���������� ��� ������
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

        // ������� ���������� ���������, ����� �������� ������������
        showMoreButton.onClick.RemoveAllListeners();
        showMoreButton.onClick.AddListener(ToggleText);

        // ��������� ����� ����� �������������
        UpdateLayout();
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

        // ��������� �����
        UpdateLayout();
    }

    private void UpdateLayout()
    {
        // ������������� ��������� ����� ��� ��������� ������
        if (isExpanded)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(textFull.rectTransform);
        }
        else
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(textShort.rectTransform);
        }

        // ��������� ����� ���������� (Image ��� back), ����� �� ����������� ��� ����������
        LayoutRebuilder.ForceRebuildLayoutImmediate(imageRectTransform);

        // ��������� ����� ��������� ������� (Panel_news ��� Panel_news_netPhoto)
        LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
    }
}