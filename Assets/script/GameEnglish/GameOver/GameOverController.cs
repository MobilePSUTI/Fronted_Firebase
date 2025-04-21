using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using Firebase.Database;
using System.Threading.Tasks;
using System.Collections.Generic;

public class GameOverController : MonoBehaviour
{
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI timeText;
    private FirebaseDBManager firebaseManager;

    // ����������� ���� ��� �������� ������ ����� �������
    public static int LastScore { get; private set; }
    public static float LastTime { get; private set; }
    public static bool LastIsWin { get; private set; }

    public static void SetGameResults(int score, float time, bool isWin)
    {
        LastScore = score;
        LastTime = time;
        LastIsWin = isWin;
    }

    private void Start()
    {
        firebaseManager = FirebaseDBManager.Instance;

        // �������� ����������� ����������
        int score = LastScore;
        float time = LastTime;
        bool isWin = LastIsWin;

        // ���������� ����������
        scoreText.text = $"Score: {score} ({(isWin ? "WIN" : "LOSE")})";

        int minutes = Mathf.FloorToInt(time / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);
        timeText.text = string.Format("Time: {0:00}:{1:00}", minutes, seconds);

        // ��������� � Firebase
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
            var gameResult = new Dictionary<string, object>
            {
                {"student_id", studentId},
                {"game_name", "EnglishMiniGame"},
                {"score", score},
                {"time", time},
                {"timestamp", System.DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")},
                {"game_version", Application.version}
            };

            DatabaseReference gameResultsRef = firebaseManager.DatabaseReference.Child("game_results").Push();
            await gameResultsRef.SetValueAsync(gameResult);

            Debug.Log("Game results saved successfully");
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Failed to save game results: " + ex.Message);
        }
    }
}