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

        // ������� ������ FirebaseDBManager � ��������� ��� ����� �������
        var firebaseManagerObject = new GameObject("FirebaseDBManager");
        firebaseManager = firebaseManagerObject.AddComponent<FirebaseDBManager>();
        DontDestroyOnLoad(firebaseManagerObject);

        await firebaseManager.Initialize();

        if (UserSession.CurrentUser != null && UserSession.CurrentUser.Role == "teacher")
        {
            Debug.Log($"[PrMenuModel] ������� ������������: {UserSession.CurrentUser.Username}");
            StartCoroutine(LoadNewsInBackground());
        }
    }

    private void OnDestroy()
    {
        isDestroyed = true; // ��������, ��� ������ ���������
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
                errorText.text = "����� � ������ �� ����� ���� �������.";
            if (!isDestroyed && loadingIndicator != null)
                loadingIndicator.SetActive(false);
            return;
        }

        StartCoroutine(LoginTeacherCoroutine(login, password));
    }

    private IEnumerator LoginTeacherCoroutine(string login, string password)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // ��������� ����������� ������ �����������
        var task = firebaseManager.AuthenticateUser(login, password);
        yield return new WaitUntil(() => task.IsCompleted);

        // ������ ������������ ��������� ��� ����� try-catch
        bool loginSuccess = false;
        string errorMessage = "";

        if (task.IsFaulted)
        {
            errorMessage = "������ ����������";
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
                errorMessage = "������ ������ ��� ��������������";
                UserSession.CurrentUser = null;
            }
        }
        else
        {
            errorMessage = "�������� ����� ��� ������";
        }

        // ������������� ����� ������, ���� ����
        if (!isDestroyed && errorText != null)
            errorText.text = errorMessage;

        // ���� ����������� �������, ����������
        if (loginSuccess)
        {
            StartCoroutine(LoadTeacherDataInBackground());
            yield return StartCoroutine(LoadTeacherSceneAsync());
        }

        // ���������
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
            Debug.Log("[PrMenuModel] ������� ������� ��������� � ����");
        }
        else
        {
            Debug.LogWarning("[PrMenuModel] �� ������� ��������� ������� � ����");
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
                errorText.text = "������� ������� � �������.";
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
            Debug.Log("[PrMenuModel] ������� ������� ���������");
        }
        else
        {
            if (!isDestroyed && errorText != null)
                errorText.text = "������ �������� ��������";
            Debug.LogError("[PrMenuModel] �� ������� ��������� �������");
        }

        Destroy(vkNewsLoad);
    }
    public void OnLogoutButtonClick()
    {
        UserSession.ClearSession();
        SceneManager.LoadScene("MainScene");
    }
}