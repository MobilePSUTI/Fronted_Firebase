using UnityEngine;
using TMPro;

public class EnemyController : MonoBehaviour
{
    [Header("Настройки")]
    public float moveSpeed = 2f;
    public TextMeshProUGUI wordDisplay;
    public string targetWord;
    private string currentTyped = "";
    private int hiddenLetterIndex; // Индекс скрытой буквы
    // позиция y = -343480

    private void Start()
    {
        // Получаем случайное слово
        targetWord = GameManager.Instance.GetRandomWord().ToLower();

        // Выбираем случайную букву для скрытия (кроме первой)
        hiddenLetterIndex = Random.Range(1, targetWord.Length);

        // Инициализируем отображение слова
        UpdateWordDisplay();

        // Настраиваем TextMeshPro
        if (wordDisplay == null)
        {
            wordDisplay = GetComponentInChildren<TextMeshProUGUI>();
        }
    }

    private void Update()
    {
        if (PauseManager.Instance.IsGamePaused()) return;
        // Движение вниз
        transform.Translate(Vector2.down * moveSpeed * Time.deltaTime);

        // Если враг ушел за экран
        if (transform.position.y < -5f)
        {
            GameManager.Instance.WordMissed();
            Destroy(gameObject);
        }
    }

    public void ProcessKeyPress(char key)
    {
        key = char.ToLower(key);

        // Проверяем, соответствует ли нажатая клавиша скрытой букве
        if (targetWord[hiddenLetterIndex] == key)
        {
            currentTyped = targetWord; // Считаем слово угаданным
            UpdateWordDisplay();
            WordCompleted();
        }
    }

    private void UpdateWordDisplay()
    {
        string display = "";
        for (int i = 0; i < targetWord.Length; i++)
        {
            // Показываем букву, если она уже введена или это не скрытая буква
            if (i < currentTyped.Length || i != hiddenLetterIndex)
            {
                // Подсвечиваем уже введенные буквы зеленым
                if (i < currentTyped.Length)
                    display += "<color=green>" + targetWord[i] + "</color>";
                else
                    display += targetWord[i];
            }
            else
            {
                display += "_"; // Скрытая буква
            }
        }
        wordDisplay.text = display;
    }

    private void WordCompleted()
    {
        GameManager.Instance.AddCoins(GameManager.Instance.CalculateCoinReward());
        Destroy(gameObject);
    }
}