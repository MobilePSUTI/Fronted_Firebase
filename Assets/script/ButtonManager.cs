using UnityEngine;
using UnityEngine.UI;

public class ButtonManager : MonoBehaviour
{
    public Button[] leftButtons; // Левые кнопки
    public Button[] rightButtons; // Правые кнопки
    public GameObject[] leftContentPanels; // Панели контента для левых кнопок
    public GameObject[] rightContentPanels; // Панели контента для правых кнопок

    public Sprite leftNormalSprite; // Обычное состояние левых кнопок
    public Sprite leftActiveSprite; // Активное состояние левых кнопок

    public Sprite rightNormalSprite; // Обычное состояние правых кнопок
    public Sprite rightActiveSprite; // Активное состояние правых кнопок

    private int activeButtonIndex = -1; // Индекс активной кнопки
    private bool isLeftGroupActive = false; // Активна ли группа левых кнопок

    void Start()
    {
        // Назначаем обработчики для левых кнопок
        for (int i = 0; i < leftButtons.Length; i++)
        {
            int index = i;
            leftButtons[i].onClick.AddListener(() => OnButtonClick(index, true));
        }

        // Назначаем обработчики для правых кнопок
        for (int i = 0; i < rightButtons.Length; i++)
        {
            int index = i;
            rightButtons[i].onClick.AddListener(() => OnButtonClick(index, false));
        }

        // Инициализация: отключаем все панели контента
        UpdateContentPanels();
        UpdateButtonSprites();
    }

    void OnButtonClick(int index, bool isLeft)
    {
        // Если нажата уже активная кнопка, ничего не делаем
        if (isLeftGroupActive == isLeft && activeButtonIndex == index)
            return;

        activeButtonIndex = index;
        isLeftGroupActive = isLeft;

        // Обновляем изображения кнопок
        UpdateButtonSprites();

        // Обновляем панели контента
        UpdateContentPanels();
    }

    void UpdateButtonSprites()
    {
        // Обновляем изображения левых кнопок
        for (int i = 0; i < leftButtons.Length; i++)
        {
            leftButtons[i].image.sprite = (isLeftGroupActive && i == activeButtonIndex) ? leftActiveSprite : leftNormalSprite;
        }

        // Обновляем изображения правых кнопок
        for (int i = 0; i < rightButtons.Length; i++)
        {
            rightButtons[i].image.sprite = (!isLeftGroupActive && i == activeButtonIndex) ? rightActiveSprite : rightNormalSprite;
        }
    }

    void UpdateContentPanels()
    {
        // Отключаем все панели контента
        foreach (var panel in leftContentPanels)
        {
            panel.SetActive(false);
        }
        foreach (var panel in rightContentPanels)
        {
            panel.SetActive(false);
        }

        // Активируем только одну панель контента
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