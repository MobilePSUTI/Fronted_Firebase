using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class LinuxGameOverController : MonoBehaviour
{
    public TextMeshProUGUI coinsText;

    private void Start()
    {
        coinsText.text = "Монет заработано: " + GameSession.SessionCoins;
    }

    public void ReturnToMainMenu()
    {
        SceneManager.LoadScene(6);
    }
}