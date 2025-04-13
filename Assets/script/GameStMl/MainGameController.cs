using UnityEngine;
using UnityEngine.SceneManagement;

public class MainGameController : MonoBehaviour
{
    public void PlayGame()
    {
        // Загружаем следующую сцену
        SceneManager.LoadScene("GameEnglish");
    }

    public void ExitGame()
    {
        SceneManager.LoadScene("GameStMl");
    }
}