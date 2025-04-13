using UnityEngine;
using UnityEngine.UI;

public class ButtonTwoManager : MonoBehaviour
{
    public Button[] buttons; // Массив кнопок
    public GameObject[] contentPanels; // Массив панелей контента, соответствующих кнопкам

    private int activeButtonIndex = -1; // Индекс активной кнопки

    void Start()
    {
        // Назначаем обработчики для кнопок
        for (int i = 0; i < buttons.Length; i++)
        {
            int index = i;
            buttons[i].onClick.AddListener(() => OnButtonClick(index));
        }

        // Инициализация: отключаем все панели контента
        UpdateContentPanels();
    }

    void OnButtonClick(int index)
    {
        // Если нажата уже активная кнопка, ничего не делаем
        if (activeButtonIndex == index)
            return;

        activeButtonIndex = index;

        // Обновляем панели контента
        UpdateContentPanels();
    }

    void UpdateContentPanels()
    {
        // Отключаем все панели контента
        foreach (var panel in contentPanels)
        {
            panel.SetActive(false);
        }

        // Активируем только одну панель контента
        if (activeButtonIndex >= 0 && activeButtonIndex < contentPanels.Length)
        {
            contentPanels[activeButtonIndex].SetActive(true);
        }
    }
}
