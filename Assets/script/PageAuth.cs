using UnityEngine;

public class PageAuth : MonoBehaviour
{
    // ������ (��������)
    public GameObject mainPanel;
    public GameObject mainAuthPanel;

    // ����� ��� �������� Main
    public void ShowAuthPanel()
    {
        mainPanel.SetActive(false);
        mainAuthPanel.SetActive(true);
    }

}
