using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using Firebase.Database;
using System.Threading.Tasks;
using System.Collections.Generic;

public class GameOverController : MonoBehaviour
{
    public TextMeshProUGUI scoreText; // Переименовали coinsText в scoreText
    public TextMeshProUGUI timeText;
    private FirebaseDBManager firebaseManager;

    private void Start()
    {
        firebaseManager = FirebaseDBManager.Instance;

        // Получаем сохраненные результаты из GameManager
        int score = GameManager.Instance != null ? GameManager.Instance.CoinsCollected : PlayerPrefs.GetInt("FinalCoins", 0);
        float time = GameManager.Instance != null ? GameManager.Instance.gameTimer : PlayerPrefs.GetFloat("FinalTime", 0f);
        bool isWin = GameManager.Instance != null ? (GameManager.Instance.CoinsCollected >= GameManager.MAX_COINS) : (PlayerPrefs.GetInt("IsWin", 0) == 1);

        // Отображаем результаты
        scoreText.text = $"Score: {score} ({(isWin ? "WIN" : "LOSE")})";

        int minutes = Mathf.FloorToInt(time / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);
        timeText.text = string.Format("Time: {0:00}:{1:00}", minutes, seconds);

        // Сохраняем результаты в Firebase
        if (UserSession.CurrentUser != null)
        {
            SaveGameResults(UserSession.CurrentUser.Id, score, time, isWin);
        }
    }

    public void ReturnToMainMenu()
    {
        SceneManager.LoadScene("GameStMl");
    }

    private async void SaveGameResults(string studentId, int score, float time, bool isWin)
    {
        if (firebaseManager == null || string.IsNullOrEmpty(studentId))
        {
            Debug.LogError("FirebaseManager not initialized or studentId is empty");
            return;
        }

        try
        {
            string gameName = "EnglishMiniGame"; // Фиксированное имя игры
            string timestamp = System.DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

            var gameResult = new Dictionary<string, object>
            {
                {"student_id", studentId},
                {"game_name", gameName},
                {"score", score},
                {"time", time},
                {"is_win", isWin},
                {"timestamp", timestamp},
                {"game_version", Application.version}
            };

            DatabaseReference gameResultsRef = firebaseManager.DatabaseReference.Child("game_results").Push();
            await gameResultsRef.SetValueAsync(gameResult);

            Debug.Log("English game results saved successfully for student: " + studentId);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Failed to save English game results: " + ex.Message);
        }
    }
}