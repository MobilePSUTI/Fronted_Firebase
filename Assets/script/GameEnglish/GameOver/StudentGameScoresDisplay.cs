using UnityEngine;
using TMPro;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using Firebase.Database;

public class StudentGameScoresDisplay : MonoBehaviour
{
    public TextMeshProUGUI scoreText;
    public GameObject loadingIndicator;

    private FirebaseDBManager _firebaseManager;
    private bool _isLoading;

    private void Awake()
    {
        _firebaseManager = FirebaseDBManager.Instance;
    }

    private async void Start()
    {
        await LoadAndDisplayScores();
    }

    public async Task LoadAndDisplayScores()
    {
        if (_isLoading) return;
        _isLoading = true;

        ShowLoading(true);
        scoreText.text = "";

        try
        {
            // 1. Проверка авторизации
            if (UserSession.CurrentUser == null)
            {
                scoreText.text = "Требуется авторизация";
                return;
            }

            // 2. Проверка Firebase
            if (_firebaseManager == null || _firebaseManager.DatabaseReference == null)
            {
                scoreText.text = "Ошибка подключения";
                Debug.LogError("FirebaseManager не инициализирован");
                return;
            }

            if (!_firebaseManager.isInitialized)
            {
                await _firebaseManager.Initialize();
            }

            // 3. Запрос данных
            Query scoresQuery = _firebaseManager.DatabaseReference
                .Child("game_results")
                .OrderByChild("student_id")
                .EqualTo(UserSession.CurrentUser.Id);

            DataSnapshot snapshot = await scoresQuery.GetValueAsync();

            if (snapshot == null || !snapshot.Exists)
            {
                scoreText.text = "Нет данных о результатах";
                return;
            }

            // 4. Обработка результатов
            var gameResults = new Dictionary<string, int>();
            int totalScore = 0;
            int gameCount = 0;

            foreach (DataSnapshot gameSnapshot in snapshot.Children)
            {
                if (gameSnapshot == null || !gameSnapshot.HasChildren) continue;

                try
                {
                    // 4.1 Получаем название игры
                    string gameName = gameSnapshot.Child("game_name")?.Value?.ToString();
                    if (string.IsNullOrEmpty(gameName))
                    {
                        Debug.LogWarning("Не найдено название игры в записи");
                        continue;
                    }

                    // 4.2 Получаем очки (пробуем score, затем coins)
                    int score = 0;
                    if (gameSnapshot.HasChild("score"))
                    {
                        string scoreStr = gameSnapshot.Child("score").Value?.ToString();
                        int.TryParse(scoreStr, out score);
                    }

                    // 4.3 Форматируем название игры
                    string displayName = gameName switch
                    {
                        "LinuxQuiz" => "Linux Викторина",
                        "EnglishMiniGame" => "Английский",
                        _ => gameName
                    };

                    // 4.4 Добавляем в результаты
                    if (gameResults.ContainsKey(displayName))
                    {
                        gameResults[displayName] += score;
                    }
                    else
                    {
                        gameResults.Add(displayName, score);
                    }

                    totalScore += score;
                    gameCount++;
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"Ошибка обработки результата: {ex.Message}");
                }
            }

            // 5. Отображение результатов
            if (gameCount == 0)
            {
                scoreText.text = "Нет данных о результатах";
                return;
            }

            string resultText = "<b>Ваши результаты:</b>\n\n";
            foreach (var result in gameResults.OrderByDescending(r => r.Value))
            {
                resultText += $"• {result.Key}: <color=#FFD700>{result.Value}</color> очков\n";
            }

            resultText += $"\n<b>Общий счет:</b> <color=#00FF00>{totalScore}</color> очков";
            scoreText.text = resultText;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Ошибка загрузки: {ex}");
            scoreText.text = "Ошибка загрузки данных";
        }
        finally
        {
            ShowLoading(false);
            _isLoading = false;
        }
    }

    private void ShowLoading(bool show)
    {
        if (loadingIndicator != null)
            loadingIndicator.SetActive(show);

        if (scoreText != null)
            scoreText.gameObject.SetActive(!show);
    }

    public async void OnRefreshClicked()
    {
        await LoadAndDisplayScores();
    }
}