using UnityEngine;
using TMPro;
using System.Threading.Tasks;
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
            if (UserSession.CurrentUser == null)
            {
                scoreText.text = "0";
                return;
            }

            if (_firebaseManager == null || _firebaseManager.DatabaseReference == null)
            {
                scoreText.text = "0";
                Debug.LogError("FirebaseManager не инициализирован");
                return;
            }

            if (!_firebaseManager.isInitialized)
            {
                await _firebaseManager.Initialize();
            }

            Query scoresQuery = _firebaseManager.DatabaseReference
                .Child("game_results")
                .OrderByChild("student_id")
                .EqualTo(UserSession.CurrentUser.Id);

            DataSnapshot snapshot = await scoresQuery.GetValueAsync();

            if (snapshot == null || !snapshot.Exists)
            {
                scoreText.text = "0";
                return;
            }

            int totalScore = 0;

            foreach (DataSnapshot gameSnapshot in snapshot.Children)
            {
                if (gameSnapshot == null || !gameSnapshot.HasChildren) continue;

                try
                {
                    int score = 0;
                    if (gameSnapshot.HasChild("score"))
                    {
                        string scoreStr = gameSnapshot.Child("score").Value?.ToString();
                        int.TryParse(scoreStr, out score);
                    }

                    totalScore += score;
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"Ошибка обработки результата: {ex.Message}");
                }
            }

            scoreText.text = totalScore.ToString();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Ошибка загрузки: {ex}");
            scoreText.text = "0";
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