using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneLoader : MonoBehaviour
{
    // ����� ��� �������� ����� � ������� ���������
    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}