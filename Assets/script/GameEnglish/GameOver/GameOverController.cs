using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class GameOverController : MonoBehaviour
{
    public TextMeshProUGUI coinsText;
    public TextMeshProUGUI timeText; // Добавляем поле

    private void Start()
    {
        coinsText.text = "Монет заработано: " + GameSession.SessionCoins;

        int minutes = Mathf.FloorToInt(GameSession.SessionTime / 60f);
        int seconds = Mathf.FloorToInt(GameSession.SessionTime % 60f);
        timeText.text = $"Времени потрачено: {minutes:00}:{seconds:00}";
    }
    
    public void ReturnToMainMenu()
    {
        SceneManager.LoadScene(6);
    }
}