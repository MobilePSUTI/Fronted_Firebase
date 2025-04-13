using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneLoader : MonoBehaviour
{
    // Метод для загрузки сцены с плавным переходом
    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}