using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance;

    [Header("UI References")]
    public GameObject pausePanel;
    public Button pauseButton;
    public Button continueButton;
    public Button exitButton;

    private bool isPaused = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        pausePanel.SetActive(false);

        // Назначаем обработчики кнопок
        pauseButton.onClick.AddListener(TogglePause);
        continueButton.onClick.AddListener(ContinueGame);
        exitButton.onClick.AddListener(ExitToMenu);
    }

    public void TogglePause()
    {
        isPaused = !isPaused;

        pausePanel.SetActive(isPaused);
        Time.timeScale = isPaused ? 0f : 1f;

        // Отключаем/включаем другие UI элементы при паузе
        GameManager.Instance.gameOver = isPaused;
    }

    private void ContinueGame()
    {
        TogglePause();
    }

    private void ExitToMenu()
    {
        Time.timeScale = 1f; // Восстанавливаем время
        SceneManager.LoadScene(6); // Замените на вашу сцену меню
    }

    public bool IsGamePaused()
    {
        return isPaused;
    }
}