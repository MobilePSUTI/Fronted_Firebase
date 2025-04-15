using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using Firebase.Database;
using System.Threading.Tasks;
using System;

public class PrMenuModel : MonoBehaviour
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
       
        if (UserSession.CurrentUser != null && UserSession.CurrentUser.Role == "teacher")
        {
            Debug.Log($"������� ������������: {UserSession.CurrentUser.Username}");
            // ������� �������� �������� ����� ����� ����� � ���������� ���� ��� ��������
            StartCoroutine(LoadNewsInBackground());
        }
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

    public void OnTeacherLoginButtonClick()
    {
        string login = loginInput.text;
        string password = passwordInput.text;

        if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
        {
            errorText.text = "����� � ������ �� ����� ���� �������.";
            return;
        }

        StartCoroutine(LoginTeacherCoroutine(login, password));
    }

    private IEnumerator LoginTeacherCoroutine(string login, string password)
    {
        if (loadingIndicator != null)
            loadingIndicator.SetActive(true);

        var task = firebaseManager.AuthenticateUser(login, password);
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.IsFaulted)
        {
            errorText.text = "������ ����������";
        }
        else if (task.Result != null)
        {
            UserSession.CurrentUser = task.Result;

            if (UserSession.CurrentUser.Role == "teacher")
            {
                errorText.text = "";
                // ��������� ������������ �������� ������
                StartCoroutine(LoadTeacherDataInBackground());
                // ��������� �� ����� �������������
                yield return StartCoroutine(LoadTeacherSceneAsync());
            }
            else
            {
                errorText.text = "������ ������ ��� ��������������";
                UserSession.CurrentUser = null;
            }
        }
        else
        {
            errorText.text = "�������� ����� ��� ������";
        }

        if (loadingIndicator != null)
            loadingIndicator.SetActive(false);
    }

    private IEnumerator LoadTeacherDataInBackground()
    {
        // ������������ ����� � ���������
        var groupsTask = firebaseManager.GetAllGroups();
        yield return new WaitUntil(() => groupsTask.IsCompleted);

        if (groupsTask.IsCompletedSuccessfully)
        {
            // ����� ��������� ������ � ���
            UserSession.CachedGroups = groupsTask.Result;

            // ������������ ��������� ��� ������ ������
            foreach (var group in groupsTask.Result)
            {
                var studentsTask = firebaseManager.GetStudentsByGroup(group.Id);
                yield return new WaitUntil(() => studentsTask.IsCompleted);

                // ��������� � ���
                if (studentsTask.IsCompletedSuccessfully)
                {
                    UserSession.CachedStudents[group.Id] = studentsTask.Result;
                }
            }
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
            Debug.Log("������� ������� ��������� � ����");
        }
        else
        {
            Debug.LogWarning("�� ������� ��������� ������� � ����");
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

        if (loadingIndicator != null)
            loadingIndicator.SetActive(true);

        yield return StartCoroutine(GetNewsFromVK());

        yield return StartCoroutine(LoadTeacherSceneAsync());

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
            Debug.Log("������� ������� ���������");
        }
        else
        {
            Debug.LogError("�� ������� ��������� �������");
            errorText.text = "������ �������� ��������";
        }

        Destroy(vkNewsLoad);
    }
}