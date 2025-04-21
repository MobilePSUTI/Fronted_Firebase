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

        // ��������� ����������� ������
        pauseButton.onClick.AddListener(TogglePause);
        continueButton.onClick.AddListener(ContinueGame);
        exitButton.onClick.AddListener(ExitToMenu);
    }

    public void TogglePause()
    {
        isPaused = !isPaused;

        pausePanel.SetActive(isPaused);
        Time.timeScale = isPaused ? 0f : 1f;

        // ���������/�������� ������ UI �������� ��� �����
    }

    private void ContinueGame()
    {
        TogglePause();
    }

    private void ExitToMenu()
    {
        Time.timeScale = 1f; // ��������������� �����
        SceneManager.LoadScene(6); // �������� �� ���� ����� ����
    }

    public bool IsGamePaused()
    {
        return isPaused;
    }
}