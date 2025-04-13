using UnityEngine;

public class PageAuth : MonoBehaviour
{
    // Панели (страницы)
    public GameObject mainPanel;
    public GameObject mainAuthPanel;

    // Метод для открытия Main
    public void ShowAuthPanel()
    {
        mainPanel.SetActive(false);
        mainAuthPanel.SetActive(true);
    }

}
