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
            // 1. �������� �����������
            if (UserSession.CurrentUser == null)
            {
                scoreText.text = "��������� �����������";
                return;
            }

            // 2. �������� Firebase
            if (_firebaseManager == null || _firebaseManager.DatabaseReference == null)
            {
                scoreText.text = "������ �����������";
                Debug.LogError("FirebaseManager �� ���������������");
                return;
            }

            if (!_firebaseManager.isInitialized)
            {
                await _firebaseManager.Initialize();
            }

            // 3. ������ ������
            Query scoresQuery = _firebaseManager.DatabaseReference
                .Child("game_results")
                .OrderByChild("student_id")
                .EqualTo(UserSession.CurrentUser.Id);

            DataSnapshot snapshot = await scoresQuery.GetValueAsync();

            if (snapshot == null || !snapshot.Exists)
            {
                scoreText.text = "��� ������ � �����������";
                return;
            }

            // 4. ��������� �����������
            var gameResults = new Dictionary<string, int>();
            int totalScore = 0;
            int gameCount = 0;

            foreach (DataSnapshot gameSnapshot in snapshot.Children)
            {
                if (gameSnapshot == null || !gameSnapshot.HasChildren) continue;

                try
                {
                    // 4.1 �������� �������� ����
                    string gameName = gameSnapshot.Child("game_name")?.Value?.ToString();
                    if (string.IsNullOrEmpty(gameName))
                    {
                        Debug.LogWarning("�� ������� �������� ���� � ������");
                        continue;
                    }

                    // 4.2 �������� ���� (������� score, ����� coins)
                    int score = 0;
                    if (gameSnapshot.HasChild("score"))
                    {
                        string scoreStr = gameSnapshot.Child("score").Value?.ToString();
                        int.TryParse(scoreStr, out score);
                    }

                    // 4.3 ����������� �������� ����
                    string displayName = gameName switch
                    {
                        "LinuxQuiz" => "Linux ���������",
                        "EnglishMiniGame" => "����������",
                        _ => gameName
                    };

                    // 4.4 ��������� � ����������
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
                    Debug.LogWarning($"������ ��������� ����������: {ex.Message}");
                }
            }

            // 5. ����������� �����������
            if (gameCount == 0)
            {
                scoreText.text = "��� ������ � �����������";
                return;
            }

            string resultText = "<b>���� ����������:</b>\n\n";
            foreach (var result in gameResults.OrderByDescending(r => r.Value))
            {
                resultText += $"� {result.Key}: <color=#FFD700>{result.Value}</color> �����\n";
            }

            resultText += $"\n<b>����� ����:</b> <color=#00FF00>{totalScore}</color> �����";
            scoreText.text = resultText;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"������ ��������: {ex}");
            scoreText.text = "������ �������� ������";
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