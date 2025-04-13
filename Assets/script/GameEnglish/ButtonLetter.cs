using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ButtonLetter : MonoBehaviour
{
    public char letter;
    private Button button;
    private Image buttonImage;
    public Sprite pressedSprite;   // Изображение при нажатии
    private Sprite normalSprite;   // Исходное изображение кнопки
    public float changeDuration = 0.3f; // Длительность смены спрайта

    private void Awake()
    {
        button = GetComponent<Button>();
        buttonImage = GetComponent<Image>();
        normalSprite = buttonImage.sprite; // Сохраняем исходный спрайт

        button.onClick.AddListener(OnButtonClick);

        // Устанавливаем текст кнопки
        Text buttonText = GetComponentInChildren<Text>();
        if (buttonText != null)
        {
            buttonText.text = letter.ToString();
        }
    }

    private void OnButtonClick()
    {
        StartCoroutine(ChangeButtonSprite());
        PlayerController.Instance.OnKeyboardKeyPressed(letter);
    }

    private IEnumerator ChangeButtonSprite()
    {
        // Меняем спрайт на нажатый
        buttonImage.sprite = pressedSprite;

        // Ждем указанное время
        yield return new WaitForSeconds(changeDuration);

        // Возвращаем исходный спрайт
        buttonImage.sprite = normalSprite;
    }
}