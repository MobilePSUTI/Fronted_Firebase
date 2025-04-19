using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using Firebase.Database;
using System;
using System.Threading.Tasks;

public class MainMenu : MonoBehaviour
{
    public GameObject loadingIndicator;
    public InputField loginInput;
    public InputField passwordInput;
    public Text errorText;

    private FirebaseDBManager firebaseManager;
    private bool isNewsLoading = false;

    async void Start()
    {
        firebaseManager = gameObject.AddComponent<FirebaseDBManager>();
        await firebaseManager.Initialize();

        await DebugCheckDatabaseStructure();

        if (UserSession.CurrentUser != null && UserSession.CurrentUser.Role == "student")
        {
            Debug.Log($"Текущий пользователь: {UserSession.CurrentUser.Username}");
            // Начинаем фоновую загрузку данных при старте, если пользователь уже авторизован
            StartCoroutine(PreloadStudentData());
        }
    }

    private IEnumerator PreloadStudentData()
    {
        // Создаем временный объект для предзагрузки
        var loaderObject = new GameObject("StudentDataPreloader");
        var progressController = loaderObject.AddComponent<StudentProgressController>();

        // Загружаем данные
        yield return progressController.PreloadSkillsCoroutine();

        // После загрузки уничтожаем временный объект
        Destroy(loaderObject);
    }

    private async Task DebugCheckDatabaseStructure()
    {
        try
        {
            DataSnapshot snapshot = await FirebaseDatabase.DefaultInstance.GetReference("").GetValueAsync();
            Debug.Log("Full DB data: " + snapshot.GetRawJsonValue());
        }
        catch (Exception ex)
        {
            Debug.LogError($"Database check failed: {ex.Message}");
        }
    }

    public void OnLoginButtonClick()
    {
        StartCoroutine(LoginStudentCoroutine(loginInput.text, passwordInput.text));
    }

    private IEnumerator LoginStudentCoroutine(string login, string password)
    {
        if (loadingIndicator != null)
            loadingIndicator.SetActive(true);

        var task = firebaseManager.AuthenticateUser(login, password);
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.IsFaulted)
        {
            errorText.text = "Ошибка соединения";
        }
        else if (task.Result != null)
        {
            UserSession.CurrentUser = task.Result;

            if (UserSession.CurrentUser.Role == "student")
            {
                errorText.text = "";
                // Запускаем предзагрузку данных студента
                yield return StartCoroutine(PreloadStudentData());
                // Запускаем загрузку новостей в фоне
                StartCoroutine(LoadNewsInBackground());
                // Переходим на сцену студентов
                yield return StartCoroutine(LoadStudentsSceneAsync());
            }
            else
            {
                errorText.text = "Доступ только для студентов";
                UserSession.CurrentUser = null;
            }
        }
        else
        {
            errorText.text = "Неверный логин или пароль";
        }

        if (loadingIndicator != null)
            loadingIndicator.SetActive(false);
    }

    IEnumerator LoadNewsInBackground()
    {
        isNewsLoading = true;
        var vkNewsLoad = gameObject.AddComponent<VKNewsLoad>();
        yield return StartCoroutine(vkNewsLoad.GetNewsFromVK(0, 100));

        if (vkNewsLoad.allPosts != null && vkNewsLoad.groupDictionary != null)
        {
            NewsDataCache.CachedPosts = vkNewsLoad.allPosts;
            NewsDataCache.CachedVKGroups = vkNewsLoad.groupDictionary;
            Debug.Log("Новости успешно загружены в фоне");
        }
        else
        {
            Debug.LogWarning("Не удалось загрузить новости в фоне");
        }

        Destroy(vkNewsLoad);
        isNewsLoading = false;
    }

    IEnumerator LoadStudentsSceneAsync()
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("StudentsScene");
        asyncLoad.allowSceneActivation = false;

        while (!asyncLoad.isDone)
        {
            if (asyncLoad.progress >= 0.9f)
            {
                asyncLoad.allowSceneActivation = true;
            }
            yield return null;
        }
    }

    public void OnNewsButtonClick()
    {
        if (UserSession.CurrentUser == null)
        {
            errorText.text = "Сначала войдите в систему.";
            return;
        }

        if (!isNewsLoading)
        {
            StartCoroutine(LoadNewsBeforeTransition());
        }
    }

    IEnumerator LoadNewsBeforeTransition()
    {
        isNewsLoading = true;

        if (loadingIndicator != null)
            loadingIndicator.SetActive(true);

        yield return StartCoroutine(GetNewsFromVK());

        yield return StartCoroutine(LoadStudentsSceneAsync());

        if (loadingIndicator != null)
            loadingIndicator.SetActive(false);

        isNewsLoading = false;
    }

    IEnumerator GetNewsFromVK()
    {
        var vkNewsLoad = gameObject.AddComponent<VKNewsLoad>();
        yield return StartCoroutine(vkNewsLoad.GetNewsFromVK(0, 100));

        if (vkNewsLoad.allPosts != null && vkNewsLoad.groupDictionary != null)
        {
            NewsDataCache.CachedPosts = vkNewsLoad.allPosts;
            NewsDataCache.CachedVKGroups = vkNewsLoad.groupDictionary;
            Debug.Log("Новости успешно загружены");
        }
        else
        {
            Debug.LogError("Не удалось загрузить новости");
            errorText.text = "Ошибка загрузки новостей";
        }

        Destroy(vkNewsLoad);
    }
}