using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class GameOverController : MonoBehaviour
{
    public TextMeshProUGUI coinsText;
    public TextMeshProUGUI timeText;

    private void Start()
    {
        // �������� ����������� ����������
        int coins = PlayerPrefs.GetInt("FinalCoins", 0);
        float time = PlayerPrefs.GetFloat("FinalTime", 0f);

        // ���������� ����������
        coinsText.text = "Coins: " + coins;

        int minutes = Mathf.FloorToInt(time / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);
        timeText.text = string.Format("Time: {0:00}:{1:00}", minutes, seconds);
    }

    public void ReturnToMainMenu()
    {
        SceneManager.LoadScene(6); // ��������� ������� �����
    }
}
