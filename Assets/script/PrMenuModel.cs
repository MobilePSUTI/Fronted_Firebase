using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class PrMenuModel : MonoBehaviour
{
    public GameObject loadingIndicator;
    public InputField loginInput;
    public InputField passwordInput;
    public Text errorText;

    private FirebaseDBManager firebaseManager;

    void Start()
    {
        firebaseManager = gameObject.AddComponent<FirebaseDBManager>();
        _ = firebaseManager.Initialize();
    }

    public void OnTeacherLoginButtonClick()
    {
        string login = loginInput.text;
        string password = passwordInput.text;

        if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
        {
            errorText.text = "Ћогин и пароль не могут быть пустыми.";
            return;
        }

        StartCoroutine(LoginTeacher(login, password));
    }

    IEnumerator LoginTeacher(string login, string password)
    {
        if (loadingIndicator != null)
            loadingIndicator.SetActive(true);

        var task = firebaseManager.AuthenticateUser(login, password);
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.Result == null || task.Result.Role != "teacher")
        {
            errorText.text = "Ќеверные данные или доступ только дл€ преподавателей";
            if (loadingIndicator != null)
                loadingIndicator.SetActive(false);
            yield break;
        }

        UserSession.CurrentUser = task.Result;
        Debug.Log($"ѕреподаватель {task.Result.Username} успешно авторизован.");
        errorText.text = "";

        yield return StartCoroutine(GetNewsFromVK());
        yield return StartCoroutine(LoadTeacherSceneAsync());

        if (loadingIndicator != null)
            loadingIndicator.SetActive(false);
    }

    IEnumerator LoadTeacherSceneAsync()
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("PrepodModel");
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
    }

    IEnumerator GetNewsFromVK()
    {
        var vkNewsLoad = gameObject.AddComponent<VKNewsLoad>();
        yield return StartCoroutine(vkNewsLoad.GetNewsFromVK(0, 100));

        NewsDataCache.CachedPosts = vkNewsLoad.allPosts;
        NewsDataCache.CachedVKGroups = vkNewsLoad.groupDictionary;

        Destroy(vkNewsLoad);
    }
}