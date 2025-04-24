using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Database;
using System.Linq;
using UnityEngine.UIElements;

public class StudentRatingManager : MonoBehaviour
{
    [Header("UI Settings")]
    [SerializeField] private GameObject[] ratingItems = new GameObject[10];
    [SerializeField] private GameObject currentUserRatingItem;
    [SerializeField] private Color currentUserHighlightColor = new Color(0.8f, 0.9f, 1f);

    [Header("Debug")]
    [SerializeField] private bool debugMode = true;

    private List<UserSession.UserRatingData> allUsers = new List<UserSession.UserRatingData>();
    private bool isLoading;
    private FirebaseDBManager dbManager;

    private async void Start()
    {
        dbManager = FindObjectOfType<FirebaseDBManager>();
        if (dbManager == null)
        {
            Debug.LogError("[Rating] FirebaseDBManager not found");
            return;
        }
        await LoadAndDisplayRating();
    }

    public async Task LoadAndDisplayRating()
    {
        if (isLoading) return;
        isLoading = true;
        DebugLog("Starting rating load...");

        try
        {
            if (UserSession.CachedRatingData.Count > 0)
            {
                DebugLog("Using cached rating data");
                allUsers = UserSession.CachedRatingData;
                await DisplayRating();
            }

            await LoadAllUsersWithPoints();
            await DisplayRating();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[Rating] Error: {ex.Message}");
        }
        finally
        {
            isLoading = false;
            DebugLog("Rating load completed");
        }
    }

    private async Task LoadAllUsersWithPoints()
    {
        allUsers.Clear();
        DebugLog("Cleared existing user data");

        DataSnapshot usersSnapshot = await FirebaseDatabase.DefaultInstance
            .GetReference("14/data")
            .GetValueAsync();

        if (!usersSnapshot.Exists)
        {
            Debug.LogWarning("[Rating] No users found");
            return;
        }

        DebugLog($"Found {usersSnapshot.ChildrenCount} users");

        var groups = await dbManager.GetAllGroups();
        var groupInfo = groups.ToDictionary(g => g.Id, g => (g.Title, g.Course));

        foreach (DataSnapshot userSnapshot in usersSnapshot.Children)
        {
            string userId = userSnapshot.Key;
            int points = await CalculateTotalPoints(userId);
            if (points <= 0) continue;

            string groupId = userSnapshot.Child("group_id")?.Value?.ToString();
            string groupName = "N/A";
            string course = "N/A";

            if (!string.IsNullOrEmpty(groupId) && groupInfo.TryGetValue(groupId, out var info))
            {
                groupName = info.Title;
                course = info.Course;
            }

            allUsers.Add(new UserSession.UserRatingData
            {
                User = new User
                {
                    Id = userId,
                    First = userSnapshot.Child("first_name")?.Value?.ToString() ?? "Unknown",
                    Last = userSnapshot.Child("last_name")?.Value?.ToString() ?? "Unknown",
                    Email = userSnapshot.Child("email")?.Value?.ToString() ?? ""
                },
                TotalPoints = points,
                GroupName = groupName,
                Course = course
            });
        }

        allUsers = allUsers.OrderByDescending(u => u.TotalPoints).ToList();
        UserSession.CachedRatingData = allUsers;
        DebugLog($"Total users with points: {allUsers.Count}");
    }

    private async Task<int> CalculateTotalPoints(string userId)
    {
        int totalPoints = 0;
        try
        {
            DataSnapshot snapshot = await FirebaseDatabase.DefaultInstance
                .GetReference("16/data")
                .Child(userId)
                .GetValueAsync();

            if (snapshot.Exists)
            {
                foreach (var skill in snapshot.Child("main_skills")?.Children ?? new DataSnapshot[0])
                    if (int.TryParse(skill.Value?.ToString(), out int points))
                        totalPoints += points;

                foreach (var skill in snapshot.Child("additional_skills")?.Children ?? new DataSnapshot[0])
                    if (int.TryParse(skill.Value?.ToString(), out int points))
                        totalPoints += points;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[Rating] Error calculating points for {userId}: {ex.Message}");
        }
        return totalPoints;
    }

    private async Task DisplayRating()
    {
        if (ratingItems == null || ratingItems.Length == 0)
        {
            Debug.LogError("[Rating] Rating UI elements not assigned");
            return;
        }

        foreach (var item in ratingItems)
        {
            if (item != null)
            {
                ClearTextComponents(item);
                item.SetActive(false);
            }
        }

        var currentUserData = UserSession.CurrentUser != null
            ? allUsers.FirstOrDefault(u => u.User.Id == UserSession.CurrentUser.Id)
            : null;

        int topUsersToShow = Mathf.Min(10, allUsers.Count);
        for (int i = 0; i < topUsersToShow; i++)
        {
            if (i >= ratingItems.Length || ratingItems[i] == null) continue;

            var userData = allUsers[i];
            int position = i + 1;

            if (SetTextComponents(ratingItems[i], position.ToString(), $"{userData.User.First} {userData.User.Last[0] + "."}",
                userData.TotalPoints.ToString(), $"{userData.GroupName} ({userData.Course})"))
            {
                ratingItems[i].SetActive(true);
                if (currentUserData != null && userData.User.Id == currentUserData.User.Id)
                    HighlightPosition(ratingItems[i]);
            }
        }

        if (currentUserRatingItem != null && UserSession.CurrentUser != null)
        {
            int position = currentUserData != null ? allUsers.IndexOf(currentUserData) + 1 : -1;
            int points = currentUserData?.TotalPoints ?? await CalculateTotalPoints(UserSession.CurrentUser.Id);
            string groupName = currentUserData?.GroupName ?? "N/A";
            string course = currentUserData?.Course ?? "N/A";

            if (string.IsNullOrEmpty(groupName) || groupName == "N/A")
            {
                DataSnapshot userSnapshot = await FirebaseDatabase.DefaultInstance
                    .GetReference("14/data")
                    .Child(UserSession.CurrentUser.Id)
                    .GetValueAsync();

                if (userSnapshot.Exists)
                {
                    string groupId = userSnapshot.Child("group_id")?.Value?.ToString();
                    if (!string.IsNullOrEmpty(groupId))
                    {
                        var group = await dbManager.GetGroupDetails(groupId);
                        if (group != null)
                        {
                            groupName = group.Title;
                            course = group.Course;
                        }
                    }
                }
            }

            if (SetTextComponents(currentUserRatingItem, position > 0 ? position.ToString() : "-",
                $"{UserSession.CurrentUser.Last} {UserSession.CurrentUser.First}", points.ToString(),
                $"{groupName} ({course})"))
            {
                HighlightPosition(currentUserRatingItem);
                currentUserRatingItem.SetActive(true);
            }
        }
    }

    private void ClearTextComponents(GameObject item)
    {
        foreach (var text in item.GetComponentsInChildren<TMP_Text>(true))
            text.text = "";
    }

    private bool SetTextComponents(GameObject item, string position, string name, string points, string group)
    {
        var texts = item.GetComponentsInChildren<TMP_Text>(true);
        if (texts.Length < 4) return false;

        texts[0].text = position;
        texts[1].text = name;
        texts[2].text = points;
        texts[3].text = group;
        return true;
    }

    private void HighlightPosition(GameObject ratingItem)
    {
        var image = ratingItem.GetComponent<UnityEngine.UI.Image>();
        if (image != null)
            image.color = currentUserHighlightColor;
    }

    private void DebugLog(string message)
    {
        if (debugMode)
            Debug.Log($"[Rating] {message}");
    }
}