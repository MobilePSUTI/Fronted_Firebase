using UnityEngine;

public class AppManager : MonoBehaviour
{
    private void Awake()
    {
        DontDestroyOnLoad(gameObject); // Объект не будет уничтожаться
    }
}