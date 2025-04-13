using UnityEngine;
using UnityEngine.UI;

public class ButtonHighlight : MonoBehaviour
{
    public Color startColor = Color.white; // Начальный цвет
    public Color endColor = Color.yellow; // Цвет подсветки
    public float speed = 1f; // Скорость изменения цвета

    private Image buttonImage;
    private float time;

    void Start()
    {
        buttonImage = GetComponent<Image>();
        buttonImage.color = startColor;
    }

    void Update()
    {
        // Плавное изменение цвета с использованием Mathf.PingPong
        time += Time.deltaTime * speed;
        float t = Mathf.PingPong(time, 1); // Возвращает значение от 0 до 1
        buttonImage.color = Color.Lerp(startColor, endColor, t);
    }
}