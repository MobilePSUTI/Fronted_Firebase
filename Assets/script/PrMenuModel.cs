using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections;

public class PrMenuModel : MonoBehaviour
{
    [SerializeField] private GameObject loadingIndicator;
    [SerializeField] private TMP_InputField loginInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private TMP_Text errorText;

    private FirebaseDBManager firebaseManager;
    private bool isNewsLoading;
    private bool isDestroyed;

    async void Start()
    {
        if (!ValidateUIComponents()) return;

        // Создаем объект FirebaseDBManager и сохраняем его между сценами
        var firebaseManagerObject = new GameObject("FirebaseDBManager");
        firebaseManager = firebaseManagerObject.AddComponent<FirebaseDBManager>();
        DontDestroyOnLoad(firebaseManagerObject);

        await firebaseManager.Initialize();

        if (UserSession.CurrentUser != null && UserSession.CurrentUser.Role == "teacher")
        {
            Debug.Log($"[PrMenuModel] Текущий пользователь: {UserSession.CurrentUser.Username}");
            StartCoroutine(LoadNewsInBackground());
        }
    }

    private void OnDestroy()
    {
        isDestroyed = true; // Отмечаем, что объект уничтожен
    }

    private bool ValidateUIComponents()
    {
        if (loginInput == null || passwordInput == null || errorText == null)
        {
            Debug.LogError("[PrMenuModel] UI components missing");
            return false;
        }
        return true;
    }

    public void OnTeacherLoginButtonClick()
    {
        if (!ValidateUIComponents()) return;

        if (!isDestroyed && loadingIndicator != null)
            loadingIndicator.SetActive(true);

        string login = loginInput.text;
        string password = passwordInput.text;

        if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
        {
            if (!isDestroyed && errorText != null)
                errorText.text = "Логин и пароль не могут быть пустыми.";
            if (!isDestroyed && loadingIndicator != null)
                loadingIndicator.SetActive(false);
            return;
        }

        StartCoroutine(LoginTeacherCoroutine(login, password));
    }

    private IEnumerator LoginTeacherCoroutine(string login, string password)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Выполняем асинхронную задачу авторизации
        var task = firebaseManager.AuthenticateUser(login, password);
        yield return new WaitUntil(() => task.IsCompleted);

        // Теперь обрабатываем результат вне блока try-catch
        bool loginSuccess = false;
        string errorMessage = "";

        if (task.IsFaulted)
        {
            errorMessage = "Ошибка соединения";
            Debug.LogError($"[PrMenuModel] Authentication failed: {task.Exception?.Message}");
        }
        else if (task.Result != null)
        {
            UserSession.CurrentUser = task.Result;

            if (UserSession.CurrentUser.Role == "teacher")
            {
                errorMessage = "";
                loginSuccess = true;
            }
            else
            {
                errorMessage = "Доступ только для преподавателей";
                UserSession.CurrentUser = null;
            }
        }
        else
        {
            errorMessage = "Неверный логин или пароль";
        }

        // Устанавливаем текст ошибки, если есть
        if (!isDestroyed && errorText != null)
            errorText.text = errorMessage;

        // Если авторизация успешна, продолжаем
        if (loginSuccess)
        {
            StartCoroutine(LoadTeacherDataInBackground());
            yield return StartCoroutine(LoadTeacherSceneAsync());
        }

        // Завершаем
        if (!isDestroyed && loadingIndicator != null)
            loadingIndicator.SetActive(false);

        stopwatch.Stop();
        Debug.Log($"[PrMenuModel] Login completed in {stopwatch.ElapsedMilliseconds} ms");
    }

    private IEnumerator LoadTeacherDataInBackground()
    {
        var groupsTask = firebaseManager.GetAllGroups();
        yield return new WaitUntil(() => groupsTask.IsCompleted);

        if (groupsTask.IsCompletedSuccessfully)
        {
            UserSession.CachedGroups = groupsTask.Result;

            foreach (var group in groupsTask.Result)
            {
                var studentsTask = firebaseManager.GetStudentsByGroup(group.Id);
                yield return new WaitUntil(() => studentsTask.IsCompleted);

                if (studentsTask.IsCompletedSuccessfully)
                {
                    UserSession.CachedStudents[group.Id] = studentsTask.Result;
                }
            }
            Debug.Log("[PrMenuModel] Teacher data preloaded");
        }
        else
        {
            Debug.LogWarning("[PrMenuModel] Failed to preload teacher data");
        }
    }

    IEnumerator LoadNewsInBackground()
    {
        if (isNewsLoading) yield break;

        isNewsLoading = true;
        var vkNewsLoad = gameObject.AddComponent<VKNewsLoad>();
        yield return StartCoroutine(vkNewsLoad.GetNewsFromVK(0, 100));

        if (vkNewsLoad.allPosts != null && vkNewsLoad.groupDictionary != null)
        {
            NewsDataCache.CachedPosts = vkNewsLoad.allPosts;
            NewsDataCache.CachedVKGroups = vkNewsLoad.groupDictionary;
            Debug.Log("[PrMenuModel] Новости успешно загружены в фоне");
        }
        else
        {
            Debug.LogWarning("[PrMenuModel] Не удалось загрузить новости в фоне");
        }

        Destroy(vkNewsLoad);
        isNewsLoading = false;
    }

    IEnumerator LoadTeacherSceneAsync()
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("PrepodModel");
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
            if (!isDestroyed && errorText != null)
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

        if (!isDestroyed && loadingIndicator != null)
            loadingIndicator.SetActive(true);

        yield return StartCoroutine(GetNewsFromVK());

        yield return StartCoroutine(LoadTeacherSceneAsync());

        if (!isDestroyed && loadingIndicator != null)
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
            Debug.Log("[PrMenuModel] Новости успешно загружены");
        }
        else
        {
            if (!isDestroyed && errorText != null)
                errorText.text = "Ошибка загрузки новостей";
            Debug.LogError("[PrMenuModel] Не удалось загрузить новости");
        }

        Destroy(vkNewsLoad);
    }
    public void OnLogoutButtonClick()
    {
        UserSession.ClearSession();
        SceneManager.LoadScene("MainScene");
    }
}