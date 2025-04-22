using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private GameObject loadingIndicator;
    [SerializeField] private TMP_InputField loginInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private TMP_Text errorText;

    private FirebaseDBManager firebaseManager;
    private bool isNewsLoading;
    private bool isDestroyed; // Track if the GameObject is destroyed

    private async void Start()
    {
        if (!ValidateUIComponents()) return;

        var firebaseManagerObject = new GameObject("FirebaseDBManager");
        firebaseManager = firebaseManagerObject.AddComponent<FirebaseDBManager>();
        DontDestroyOnLoad(firebaseManagerObject);

        await firebaseManager.Initialize();

        if (UserSession.CurrentUser != null && UserSession.CurrentUser.Role == "student")
        {
            Debug.Log($"[MainMenu] Current user: {UserSession.CurrentUser.Username}");
            await PreloadStudentDataAsync();
        }
    }

    private void OnDestroy()
    {
        isDestroyed = true; // Mark as destroyed to prevent accessing invalid references
    }

    private bool ValidateUIComponents()
    {
        if (loginInput == null || passwordInput == null || errorText == null)
        {
            Debug.LogError("[MainMenu] UI components missing");
            return false;
        }
        return true;
    }

    private async Task PreloadStudentDataAsync()
    {
        var loaderObject = new GameObject("StudentDataPreloader");
        var progressController = loaderObject.AddComponent<StudentProgressController>();
        var ratingPreloader = loaderObject.AddComponent<RatingPreloader>();

        try
        {
            await Task.WhenAll(
                progressController.PreloadSkillsAsync(),
                ratingPreloader.PreloadRatingDataAsync()
            );
            Debug.Log("[MainMenu] Student data preloaded");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[MainMenu] Preload failed: {ex.Message}");
        }
        finally
        {
            if (loaderObject != null)
                Destroy(loaderObject);
        }
    }

    public async void OnLoginButtonClick()
    {
        if (!ValidateUIComponents()) return;

        // Set loading indicator only if not destroyed
        if (!isDestroyed && loadingIndicator != null)
            loadingIndicator.SetActive(true);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var user = await firebaseManager.AuthenticateUser(loginInput.text, passwordInput.text);
            if (user == null)
            {
                if (!isDestroyed && errorText != null)
                    errorText.text = "Invalid login or password";
                return;
            }

            UserSession.CurrentUser = user;
            if (user.Role != "student")
            {
                if (!isDestroyed && errorText != null)
                    errorText.text = "Access restricted to students";
                UserSession.CurrentUser = null;
                return;
            }

            if (!isDestroyed && errorText != null)
                errorText.text = "";
            await Task.WhenAll(
                LoadStudentAvatarAsync(user.Id),
                PreloadStudentDataAsync()
            );

            LoadNewsInBackground();
            await LoadStudentsSceneAsync();
        }
        catch (Exception ex)
        {
            if (!isDestroyed && errorText != null)
                errorText.text = "Connection error";
            Debug.LogError($"[MainMenu] Login failed: {ex.Message}");
        }
        finally
        {
            // Only access loadingIndicator if not destroyed
            if (!isDestroyed && loadingIndicator != null)
                loadingIndicator.SetActive(false);
            stopwatch.Stop();
            Debug.Log($"[MainMenu] Login completed in {stopwatch.ElapsedMilliseconds} ms");
        }
    }

    private async Task LoadStudentAvatarAsync(string userId)
    {
        if (UserSession.CachedAvatar != null)
        {
            Debug.Log("[MainMenu] Using cached avatar");
            return;
        }

        byte[] avatarData = await firebaseManager.GetUserAvatar(userId);
        if (avatarData != null && avatarData.Length > 0)
        {
            Texture2D texture = new Texture2D(2, 2);
            if (texture.LoadImage(avatarData))
                UserSession.CachedAvatar = texture;
            else
                Debug.LogWarning("[MainMenu] Failed to load avatar image");
        }
    }

    private void LoadNewsInBackground()
    {
        if (isNewsLoading) return;
        isNewsLoading = true;

        var vkNewsLoad = gameObject.AddComponent<VKNewsLoad>();
        StartCoroutine(LoadNewsCoroutine(vkNewsLoad));
    }

    private IEnumerator LoadNewsCoroutine(VKNewsLoad vkNewsLoad)
    {
        yield return vkNewsLoad.GetNewsFromVK(0, 20);

        if (vkNewsLoad.allPosts != null && vkNewsLoad.groupDictionary != null)
        {
            NewsDataCache.CachedPosts = vkNewsLoad.allPosts;
            NewsDataCache.CachedVKGroups = vkNewsLoad.groupDictionary;
            NewsDataCache.SaveCacheToPersistentStorage();
            Debug.Log("[MainMenu] News loaded in background");
        }
        else
        {
            Debug.LogWarning("[MainMenu] Failed to load news");
        }

        if (vkNewsLoad != null)
            Destroy(vkNewsLoad);
        isNewsLoading = false;
    }

    private async Task LoadStudentsSceneAsync()
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("StudentsScene");
        if (asyncLoad == null)
        {
            Debug.LogError("[MainMenu] Failed to load StudentsScene");
            return;
        }

        asyncLoad.allowSceneActivation = false;
        while (!asyncLoad.isDone)
        {
            if (asyncLoad.progress >= 0.9f)
                asyncLoad.allowSceneActivation = true;
            await Task.Yield();
        }
    }

    public void OnNewsButtonClick()
    {
        if (UserSession.CurrentUser == null)
        {
            if (!isDestroyed && errorText != null)
                errorText.text = "Please log in first.";
            return;
        }

        if (!isNewsLoading)
            StartCoroutine(LoadNewsBeforeTransition());
    }

    private IEnumerator LoadNewsBeforeTransition()
    {
        isNewsLoading = true;
        if (!isDestroyed && loadingIndicator != null)
            loadingIndicator.SetActive(true);

        var vkNewsLoad = gameObject.AddComponent<VKNewsLoad>();
        // No need to assign vkSettings; configure accessToken and groupIds in Inspector
        yield return vkNewsLoad.GetNewsFromVK(0, 20);

        if (vkNewsLoad.allPosts != null && vkNewsLoad.groupDictionary != null)
        {
            NewsDataCache.CachedPosts = vkNewsLoad.allPosts;
            NewsDataCache.CachedVKGroups = vkNewsLoad.groupDictionary;
            NewsDataCache.SaveCacheToPersistentStorage();
            Debug.Log("[MainMenu] News loaded successfully");
        }
        else
        {
            if (!isDestroyed && errorText != null)
                errorText.text = "News loading error";
            Debug.LogError("[MainMenu] Failed to load news");
        }

        yield return StartCoroutine(LoadStudentsSceneCoroutine());

        if (!isDestroyed && loadingIndicator != null)
            loadingIndicator.SetActive(false);
        isNewsLoading = false;
        if (vkNewsLoad != null)
            Destroy(vkNewsLoad);
    }

    private IEnumerator LoadStudentsSceneCoroutine()
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("StudentsScene");
        if (asyncLoad == null)
        {
            Debug.LogError("[MainMenu] Failed to load StudentsScene");
            yield break;
        }

        asyncLoad.allowSceneActivation = false;
        while (!asyncLoad.isDone)
        {
            if (asyncLoad.progress >= 0.9f)
                asyncLoad.allowSceneActivation = true;
            yield return null;
        }
    }
    public void OnLogoutButtonClick()
    {
        UserSession.ClearSession();
        SceneManager.LoadScene("MainScene");
    }
}