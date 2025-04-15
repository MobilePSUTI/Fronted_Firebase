using UnityEngine;

public class AppManager : MonoBehaviour
{
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);

        // Инициализируем сессию при запуске приложения
        if (!PlayerPrefs.HasKey("SessionInitialized"))
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.SetInt("SessionInitialized", 1);
            PlayerPrefs.Save();
        }
    }

    private void OnApplicationQuit()
    {
        // Очищаем кэш при выходе, если нужно
        NewsDataCache.ClearCache();
        UserSession.ClearSession();
    }
}