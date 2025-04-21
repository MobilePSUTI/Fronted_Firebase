using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.IO;
using System.Linq; // Для использования Distinct()

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Word Files")]
    public TextAsset easyWordsFile;
    public TextAsset mediumWordsFile;
    public TextAsset hardWordsFile;

    [Header("Game Settings")]
    public float initialTimePerWord = 30f;
    public float difficultyIncreaseInterval = 60f;
    public int maxWordsToLose = 3;
    public const int MAX_COINS = 50;

    [Header("UI References")]
    public TextMeshProUGUI coinsText;
    public TextMeshProUGUI timerText;

    // Списки слов
    private List<string> easyWords = new List<string>();
    private List<string> mediumWords = new List<string>();
    private List<string> hardWords = new List<string>();

    // Резервные списки слов
    private readonly List<string> defaultEasyWords = new List<string> { "cat", "dog", "sun", "hat", "pen" };
    private readonly List<string> defaultMediumWords = new List<string> { "apple", "house", "water", "light", "music" };
    private readonly List<string> defaultHardWords = new List<string> { "elephant", "computer", "keyboard", "adventure", "mountain" };

    private int coinsCollected = 0;
    public float gameTimer = 0f;
    public int wordsMissed = 0;
    public int currentDifficulty = 0;
    public bool gameOver = false;

    public int CoinsCollected
    {
        get => coinsCollected;
        set
        {
            coinsCollected = Mathf.Min(value, MAX_COINS);
            coinsText.text = ""+coinsCollected;

            if (coinsCollected >= MAX_COINS && !gameOver)
            {
                GameOver(true);
            }
        }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            LoadWordsFromFiles();
            CheckWordListsNotEmpty();
            GameSession.ResetSession(); // <--- Важно
        }
        else
        {
            Destroy(gameObject);
        }
    }


    private void LoadWordsFromFiles()
    {
        easyWords = LoadWordsFromFile(easyWordsFile, defaultEasyWords);
        mediumWords = LoadWordsFromFile(mediumWordsFile, defaultMediumWords);
        hardWords = LoadWordsFromFile(hardWordsFile, defaultHardWords);
    }

    private List<string> LoadWordsFromFile(TextAsset file, List<string> defaultWords)
    {
        if (file == null)
        {
            Debug.LogWarning("Word file not assigned, using default words");
            return new List<string>(defaultWords);
        }

        // Разделяем текст файла по строкам и удаляем пустые записи
        var words = file.text.Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries)
                            .Select(w => w.Trim()) // Удаляем пробелы по краям
                            .Where(w => !string.IsNullOrWhiteSpace(w)) // Игнорируем пустые строки
                            .Distinct() // Удаляем дубликаты
                            .ToList();

        if (words.Count == 0)
        {
            Debug.LogWarning("Word file is empty, using default words");
            return new List<string>(defaultWords);
        }

        return words;
    }

    private void CheckWordListsNotEmpty()
    {
        if (easyWords.Count == 0)
        {
            Debug.LogError("Easy words list is empty after loading!");
            easyWords = new List<string>(defaultEasyWords);
        }

        if (mediumWords.Count == 0)
        {
            Debug.LogError("Medium words list is empty after loading!");
            mediumWords = new List<string>(defaultMediumWords);
        }

        if (hardWords.Count == 0)
        {
            Debug.LogError("Hard words list is empty after loading!");
            hardWords = new List<string>(defaultHardWords);
        }
    }

    private void Update()
    {
        if (!gameOver && !PauseManager.Instance.IsGamePaused())
        {
            gameTimer += Time.deltaTime;
            UpdateTimerUI();

            if (gameTimer >= difficultyIncreaseInterval && currentDifficulty < 2)
            {
                currentDifficulty++;
                difficultyIncreaseInterval *= 2;
            }
        }
    }

    public string GetRandomWord()
    {
        List<string> selectedList = easyWords;

        if (currentDifficulty == 1) selectedList = mediumWords;
        else if (currentDifficulty == 2)
        {
            int r = Random.Range(0, 3);
            selectedList = r == 0 ? easyWords : r == 1 ? mediumWords : hardWords;
        }

        return selectedList[Random.Range(0, selectedList.Count)];
    }

    public void AddCoins(int amount)
    {
        CoinsCollected += amount;
    }

    public void WordMissed()
    {
        wordsMissed++;
        if (wordsMissed >= maxWordsToLose) GameOver(false);
    }

    private void GameOver(bool isWin)
    {
        gameOver = true;
        Debug.Log(isWin ? "You Win!" : "Game Over! Coins: " + coinsCollected);

        GameSession.AddCoins(coinsCollected);
        GameSession.SetTime(gameTimer);
        GameOverController.SetGameResults(coinsCollected, gameTimer, isWin);
        SceneManager.LoadScene("GameOverEnglish");
    }



    private void UpdateTimerUI()
    {
        int minutes = Mathf.FloorToInt(gameTimer / 60f);
        int seconds = Mathf.FloorToInt(gameTimer % 60f);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    public int CalculateCoinReward()
    {
        return Random.Range(2, 5);
    }
}