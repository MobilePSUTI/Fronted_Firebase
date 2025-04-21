using UnityEngine;

public class AppManager : MonoBehaviour
{
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);

        // �������������� ������ ��� ������� ����������
        if (!PlayerPrefs.HasKey("SessionInitialized"))
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.SetInt("SessionInitialized", 1);
            PlayerPrefs.Save();
        }
    }

    private void OnApplicationQuit()
    {
        // ������� ��� ��� ������, ���� �����
        NewsDataCache.ClearCache();
        UserSession.ClearSession();
    }
}