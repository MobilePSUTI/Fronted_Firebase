using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using Firebase.Database;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

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
        // Create a persistent GameObject for FirebaseDBManager
        var firebaseManagerObject = new GameObject("FirebaseDBManager");
        firebaseManager = firebaseManagerObject.AddComponent<FirebaseDBManager>();
        DontDestroyOnLoad(firebaseManagerObject); // Ensure it persists across scenes

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
        var ratingPreloader = loaderObject.AddComponent<RatingPreloader>();

        // Загружаем данные
        yield return progressController.PreloadSkillsCoroutine();
        yield return ratingPreloader.PreloadRatingData();

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
                yield return StartCoroutine(LoadStudentAvatar(UserSession.CurrentUser.Id));
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

    private IEnumerator LoadStudentAvatar(string userId)
    {
        var task = firebaseManager.GetUserAvatar(userId);
        yield return new WaitUntil(() => task.IsCompleted);

        byte[] avatarData = task.Result;
        if (avatarData != null && avatarData.Length > 0)
        {
            Texture2D texture = new Texture2D(2, 2);
            texture.LoadImage(avatarData);
            UserSession.CachedAvatar = texture;
        }
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

// Helper class to preload rating data
public class RatingPreloader : MonoBehaviour
{
    private List<UserSession.UserRatingData> allUsers = new List<UserSession.UserRatingData>();

    public IEnumerator PreloadRatingData()
    {
        Debug.Log("[RatingPreloader] Starting preload of rating data...");

        var dbManager = FindObjectOfType<FirebaseDBManager>();
        if (dbManager == null)
        {
            Debug.LogError("[RatingPreloader] FirebaseDBManager not found!");
            yield break;
        }

        // Load all users
        Debug.Log("[RatingPreloader] Loading users from Firebase...");
        var usersTask = FirebaseDatabase.DefaultInstance
            .GetReference("14")
            .Child("data")
            .GetValueAsync();
        yield return new WaitUntil(() => usersTask.IsCompleted);

        DataSnapshot usersSnapshot = usersTask.Result;
        if (!usersSnapshot.Exists)
        {
            Debug.LogWarning("[RatingPreloader] No users found in database");
            yield break;
        }

        Debug.Log($"[RatingPreloader] Found {usersSnapshot.ChildrenCount} users in database");

        // Load all groups for group names
        Debug.Log("[RatingPreloader] Loading groups...");
        var groupsTask = dbManager.GetAllGroups();
        yield return new WaitUntil(() => groupsTask.IsCompleted);
        var groups = groupsTask.Result;
        var groupNames = new Dictionary<string, string>();

        foreach (var group in groups)
        {
            if (!groupNames.ContainsKey(group.Id))
            {
                groupNames.Add(group.Id, group.Title);
                Debug.Log($"[RatingPreloader] Added group: {group.Id} - {group.Title}");
            }
            else
            {
                Debug.Log($"[RatingPreloader] Duplicate group ID skipped: {group.Id}");
            }
        }

        // Load points for each user
        Debug.Log("[RatingPreloader] Calculating points for users...");
        foreach (DataSnapshot userSnapshot in usersSnapshot.Children)
        {
            string userId = userSnapshot.Key;
            Debug.Log($"[RatingPreloader] Processing user: {userId}");

            int points = 0;
            var pointsTask = CalculateTotalPoints(userId);
            yield return new WaitUntil(() => pointsTask.IsCompleted);
            points = pointsTask.Result;
            Debug.Log($"[RatingPreloader] User {userId} has {points} points");

            if (points > 0) // Only include users with points
            {
                string groupId = userSnapshot.Child("group_id")?.Value?.ToString();
                string groupName;

                if (string.IsNullOrEmpty(groupId))
                {
                    Debug.Log($"[RatingPreloader] User {userId} has no group_id or group_id is null");
                    groupName = "N/A";
                }
                else
                {
                    groupNames.TryGetValue(groupId, out groupName);
                    if (string.IsNullOrEmpty(groupName))
                    {
                        Debug.Log($"[RatingPreloader] Group ID {groupId} not found in groupNames for user {userId}");
                        groupName = "N/A";
                    }
                }

                var user = new User
                {
                    Id = userId,
                    First = userSnapshot.Child("first_name")?.Value?.ToString() ?? "Unknown",
                    Last = userSnapshot.Child("last_name")?.Value?.ToString() ?? "Unknown",
                    Email = userSnapshot.Child("email")?.Value?.ToString() ?? ""
                };

                allUsers.Add(new UserSession.UserRatingData
                {
                    User = user,
                    TotalPoints = points,
                    GroupName = groupName ?? "N/A"
                });

                Debug.Log($"[RatingPreloader] Added to rating: {user.Last} {user.First} - {points} points, Group: {groupName}");
            }
        }

        // Sort by points descending and store in cache
        allUsers = allUsers.OrderByDescending(u => u.TotalPoints).ToList();
        UserSession.CachedRatingData = allUsers;
        Debug.Log($"[RatingPreloader] Total users with points cached: {allUsers.Count}");
    }

    private async Task<int> CalculateTotalPoints(string userId)
    {
        int totalPoints = 0;
        Debug.Log($"[RatingPreloader] Calculating points for user: {userId}");

        try
        {
            DataSnapshot snapshot = await FirebaseDatabase.DefaultInstance
                .GetReference("16")
                .Child("data")
                .Child(userId)
                .GetValueAsync();

            if (snapshot.Exists)
            {
                var mainSkills = snapshot.Child("main_skills");
                if (mainSkills.Exists)
                {
                    foreach (var skill in mainSkills.Children)
                    {
                        if (int.TryParse(skill.Value?.ToString(), out int points))
                        {
                            totalPoints += points;
                            Debug.Log($"[RatingPreloader] Main skill {skill.Key}: +{points} (Total: {totalPoints})");
                        }
                    }
                }

                var additionalSkills = snapshot.Child("additional_skills");
                if (additionalSkills.Exists)
                {
                    foreach (var skill in additionalSkills.Children)
                    {
                        if (int.TryParse(skill.Value?.ToString(), out int points))
                        {
                            totalPoints += points;
                            Debug.Log($"[RatingPreloader] Additional skill {skill.Key}: +{points} (Total: {totalPoints})");
                        }
                    }
                }
            }
            else
            {
                Debug.Log($"[RatingPreloader] No skills data found for user {userId}");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[RatingPreloader] Error calculating points for user {userId}: {ex.Message}");
        }

        Debug.Log($"[RatingPreloader] Final points for {userId}: {totalPoints}");
        return totalPoints;
    }
}