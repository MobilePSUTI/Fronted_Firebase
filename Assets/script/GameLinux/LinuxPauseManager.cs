using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LinuxPauseManager : MonoBehaviour
{
    public static LinuxPauseManager Instance;

    [Header("UI")]
    public GameObject pausePanel;
    public Button pauseButton;
    public Button continueButton;
    public Button exitButton;

    [SerializeField] private bool isPaused = false;

    private void Awake()
    {
        // Singleton
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
        // Убедимся, что пауза отключена при старте
        Time.timeScale = 1f;
        isPaused = false;

        if (pausePanel != null)
            pausePanel.SetActive(false);

        // Навешиваем кнопки
        if (pauseButton != null)
            pauseButton.onClick.AddListener(TogglePause);

        if (continueButton != null)
            continueButton.onClick.AddListener(TogglePause);

        if (exitButton != null)
            exitButton.onClick.AddListener(ExitToMenu);
    }

    public void TogglePause()
    {
        isPaused = !isPaused;

        if (isPaused)
        {
            Time.timeScale = 0f;
            if (pausePanel != null)
                pausePanel.SetActive(true);
            Debug.Log("Game paused");
        }
        else
        {
            Time.timeScale = 1f;
            if (pausePanel != null)
                pausePanel.SetActive(false);
            Debug.Log("Game resumed");
        }
    }

    public void ExitToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("GameStMl"); // ← Замени на имя своей сцены
    }

    public bool IsGamePaused()
    {
        return isPaused;
    }
}