using UnityEngine;
using UnityEngine.UI;

public class ButtonTwoManager : MonoBehaviour
{
    public Button[] buttons; // ������ ������
    public GameObject[] contentPanels; // ������ ������� ��������, ��������������� �������

    private int activeButtonIndex = -1; // ������ �������� ������

    void Start()
    {
        // ��������� ����������� ��� ������
        for (int i = 0; i < buttons.Length; i++)
        {
            int index = i;
            buttons[i].onClick.AddListener(() => OnButtonClick(index));
        }

        // �������������: ��������� ��� ������ ��������
        UpdateContentPanels();
    }

    void OnButtonClick(int index)
    {
        // ���� ������ ��� �������� ������, ������ �� ������
        if (activeButtonIndex == index)
            return;

        activeButtonIndex = index;

        // ��������� ������ ��������
        UpdateContentPanels();
    }

    void UpdateContentPanels()
    {
        // ��������� ��� ������ ��������
        foreach (var panel in contentPanels)
        {
            panel.SetActive(false);
        }

        // ���������� ������ ���� ������ ��������
        if (activeButtonIndex >= 0 && activeButtonIndex < contentPanels.Length)
        {
            contentPanels[activeButtonIndex].SetActive(true);
        }
    }
}
