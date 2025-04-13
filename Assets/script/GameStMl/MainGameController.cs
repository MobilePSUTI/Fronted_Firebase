using UnityEngine;
using UnityEngine.SceneManagement;

public class MainGameController : MonoBehaviour
{
    public void PlayGame()
    {
        // ��������� ��������� �����
        SceneManager.LoadScene("GameEnglish");
    }

    public void ExitGame()
    {
        SceneManager.LoadScene("GameStMl");
    }
}