using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using Firebase.Database;
using System.Threading.Tasks;
using System.Collections.Generic;

public class GameOverLinux : MonoBehaviour
{
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI timeText;
    private FirebaseDBManager firebaseManager;

    private void Start()
    {
        firebaseManager = FirebaseDBManager.Instance;

        // �������� ����������� ����������
        int score = PlayerPrefs.GetInt("FinalScore", 0);
        int errors = PlayerPrefs.GetInt("FinalErrors", 0);
        float time = PlayerPrefs.GetFloat("FinalTime", 0f);

        // ���������� ����������
        scoreText.text = "���������� �����: " + score;
        timeText.text = FormatTime(time);

        // ��������� ����������
        if (UserSession.CurrentUser != null)
        {
            SaveGameResults(score, time);
        }
    }

    private string FormatTime(float seconds)
    {
        int minutes = Mathf.FloorToInt(seconds / 60f);
        int secs = Mathf.FloorToInt(seconds % 60f);
        return string.Format("{0:00}:{1:00}", minutes, secs);
    }

    public void ReturnToMainMenu()
    {
        SceneManager.LoadScene("GameStMl");
    }

    private async Task SaveGameResults(int score, float time)
    {
        try
        {
            string studentId = UserSession.CurrentUser.Id;
            string gameName = "LinuxQuiz";
            string timestamp = System.DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

            // ��������� ��������� ����
            var gameResult = new Dictionary<string, object>
            {
                {"student_id", studentId},
                {"game_name", gameName},
                {"score", score},
                {"time", time},
                {"timestamp", timestamp},
                {"game_version", Application.version}
            };

            // ��������� � ����� ���������
            await firebaseManager.DatabaseReference
                .Child("game_results")
                .Push()
                .SetValueAsync(gameResult);

            // ��������� ����� ���� ��������
            await UpdateStudentTotalScore(studentId, score);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Save error: " + ex.Message);
        }
    }

    private async Task UpdateStudentTotalScore(string studentId, int scoreToAdd)
    {
        var studentRef = firebaseManager.DatabaseReference
            .Child("students")
            .Child(studentId);

        // �������� ������� ����
        DataSnapshot snapshot = await studentRef.GetValueAsync();
        int currentScore = 0;

        if (snapshot.Exists && snapshot.HasChild("total_score"))
        {
            currentScore = int.Parse(snapshot.Child("total_score").Value.ToString());
        }

        // ��������� ����
        await studentRef.Child("total_score").SetValueAsync(currentScore + scoreToAdd);
    }
}