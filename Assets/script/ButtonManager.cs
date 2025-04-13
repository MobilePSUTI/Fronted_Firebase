using UnityEngine;
using UnityEngine.UI;

public class ButtonManager : MonoBehaviour
{
    public Button[] leftButtons; // ����� ������
    public Button[] rightButtons; // ������ ������
    public GameObject[] leftContentPanels; // ������ �������� ��� ����� ������
    public GameObject[] rightContentPanels; // ������ �������� ��� ������ ������

    public Sprite leftNormalSprite; // ������� ��������� ����� ������
    public Sprite leftActiveSprite; // �������� ��������� ����� ������

    public Sprite rightNormalSprite; // ������� ��������� ������ ������
    public Sprite rightActiveSprite; // �������� ��������� ������ ������

    private int activeButtonIndex = -1; // ������ �������� ������
    private bool isLeftGroupActive = false; // ������� �� ������ ����� ������

    void Start()
    {
        // ��������� ����������� ��� ����� ������
        for (int i = 0; i < leftButtons.Length; i++)
        {
            int index = i;
            leftButtons[i].onClick.AddListener(() => OnButtonClick(index, true));
        }

        // ��������� ����������� ��� ������ ������
        for (int i = 0; i < rightButtons.Length; i++)
        {
            int index = i;
            rightButtons[i].onClick.AddListener(() => OnButtonClick(index, false));
        }

        // �������������: ��������� ��� ������ ��������
        UpdateContentPanels();
        UpdateButtonSprites();
    }

    void OnButtonClick(int index, bool isLeft)
    {
        // ���� ������ ��� �������� ������, ������ �� ������
        if (isLeftGroupActive == isLeft && activeButtonIndex == index)
            return;

        activeButtonIndex = index;
        isLeftGroupActive = isLeft;

        // ��������� ����������� ������
        UpdateButtonSprites();

        // ��������� ������ ��������
        UpdateContentPanels();
    }

    void UpdateButtonSprites()
    {
        // ��������� ����������� ����� ������
        for (int i = 0; i < leftButtons.Length; i++)
        {
            leftButtons[i].image.sprite = (isLeftGroupActive && i == activeButtonIndex) ? leftActiveSprite : leftNormalSprite;
        }

        // ��������� ����������� ������ ������
        for (int i = 0; i < rightButtons.Length; i++)
        {
            rightButtons[i].image.sprite = (!isLeftGroupActive && i == activeButtonIndex) ? rightActiveSprite : rightNormalSprite;
        }
    }

    void UpdateContentPanels()
    {
        // ��������� ��� ������ ��������
        foreach (var panel in leftContentPanels)
        {
            panel.SetActive(false);
        }
        foreach (var panel in rightContentPanels)
        {
            panel.SetActive(false);
        }

        // ���������� ������ ���� ������ ��������
        if (isLeftGroupActive)
        {
            if (activeButtonIndex >= 0 && activeButtonIndex < leftContentPanels.Length)
            {
                leftContentPanels[activeButtonIndex].SetActive(true);
            }
        }
        else
        {
            if (activeButtonIndex >= 0 && activeButtonIndex < rightContentPanels.Length)
            {
                rightContentPanels[activeButtonIndex].SetActive(true);
            }
        }
    }
}