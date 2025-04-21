using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Database;
using System.Linq;

public class StudentRatingManager : MonoBehaviour
{
    [Header("UI Settings")]
    public GameObject[] ratingItems = new GameObject[10];
    public GameObject currentUserRatingItem;
    public Color currentUserHighlightColor = new Color(0.8f, 0.9f, 1f);

    [Header("Debug")]
    public bool debugMode = true;

    private List<UserRatingData> allUsers = new List<UserRatingData>();
    private bool isLoading = false;

    private class UserRatingData
    {
        public User User { get; set; }
        public int TotalPoints { get; set; }
        public string GroupName { get; set; }
    }

    private async void Start()
    {
        await LoadAndDisplayRating();
    }

    public async Task LoadAndDisplayRating()
    {
        if (isLoading)
        {
            DebugLog("Ratingkeeper load already in progress");
            return;
        }

        isLoading = true;
        DebugLog("Starting rating load...");

        try
        {
            // First, display cached data if available
            if (UserSession.CachedRatingData != null && UserSession.CachedRatingData.Count > 0)
            {
                DebugLog("Using cached rating data for initial display...");
                allUsers = UserSession.CachedRatingData.Select(data => new UserRatingData
                {
                    User = data.User,
                    TotalPoints = data.TotalPoints,
                    GroupName = data.GroupName
                }).ToList();
                await DisplayRating();
            }

            // Then, load fresh data in the background and update the UI
            await LoadAllUsersWithPoints();
            await DisplayRating();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Rating error: {ex.Message}\n{ex.StackTrace}");
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

        var dbManager = FindObjectOfType<FirebaseDBManager>();
        if (dbManager == null)
        {
            Debug.LogError("FirebaseDBManager not found!");
            return;
        }

        // Load all users
        DebugLog("Loading users from Firebase...");
        DataSnapshot usersSnapshot = await FirebaseDatabase.DefaultInstance
            .GetReference("14") // users table
            .Child("data")
            .GetValueAsync();

        if (!usersSnapshot.Exists)
        {
            Debug.LogWarning("No users found in database");
            return;
        }

        DebugLog($"Found {usersSnapshot.ChildrenCount} users in database");

        // Load all groups for group names
        DebugLog("Loading groups...");
        var groups = await dbManager.GetAllGroups();
        var groupNames = new Dictionary<string, string>();

        foreach (var group in groups)
        {
            if (!groupNames.ContainsKey(group.Id))
            {
                groupNames.Add(group.Id, group.Title);
                DebugLog($"Added group: {group.Id} - {group.Title}");
            }
            else
            {
                DebugLog($"Duplicate group ID skipped: {group.Id}");
            }
        }

        // Load points for each user
        DebugLog("Calculating points for users...");
        foreach (DataSnapshot userSnapshot in usersSnapshot.Children)
        {
            string userId = userSnapshot.Key;
            DebugLog($"Processing user: {userId}");

            int points = await CalculateTotalPoints(userId);
            DebugLog($"User {userId} has {points} points");

            if (points > 0) // Only include users with points in the main ranking list
            {
                string groupId = userSnapshot.Child("group_id")?.Value?.ToString();
                string groupName;

                // Check if groupId is null before calling TryGetValue
                if (string.IsNullOrEmpty(groupId))
                {
                    DebugLog($"User {userId} has no group_id or group_id is null");
                    groupName = "N/A";
                }
                else
                {
                    groupNames.TryGetValue(groupId, out groupName);
                    if (string.IsNullOrEmpty(groupName))
                    {
                        DebugLog($"Group ID {groupId} not found in groupNames for user {userId}");
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

                allUsers.Add(new UserRatingData
                {
                    User = user,
                    TotalPoints = points,
                    GroupName = groupName ?? "N/A"
                });

                DebugLog($"Added to rating: {user.Last} {user.First} - {points} points, Group: {groupName}");
            }
        }

        // Sort by points descending
        allUsers = allUsers.OrderByDescending(u => u.TotalPoints).ToList();
        DebugLog($"Total users with points: {allUsers.Count}");

        // Update the cache with the fresh data
        UserSession.CachedRatingData = allUsers.Select(data => new UserSession.UserRatingData
        {
            User = data.User,
            TotalPoints = data.TotalPoints,
            GroupName = data.GroupName
        }).ToList();
        DebugLog("Updated cached rating data with fresh data");
    }

    private async Task<int> CalculateTotalPoints(string userId)
    {
        int totalPoints = 0;
        DebugLog($"Calculating points for user: {userId}");

        try
        {
            DataSnapshot snapshot = await FirebaseDatabase.DefaultInstance
                .GetReference("16")
                .Child("data")
                .Child(userId)
                .GetValueAsync();

            if (snapshot.Exists)
            {
                // Sum main skills
                var mainSkills = snapshot.Child("main_skills");
                if (mainSkills.Exists)
                {
                    foreach (var skill in mainSkills.Children)
                    {
                        if (int.TryParse(skill.Value?.ToString(), out int points))
                        {
                            totalPoints += points;
                            DebugLog($"Main skill {skill.Key}: +{points} (Total: {totalPoints})");
                        }
                    }
                }

                // Sum additional skills
                var additionalSkills = snapshot.Child("additional_skills");
                if (additionalSkills.Exists)
                {
                    foreach (var skill in additionalSkills.Children)
                    {
                        if (int.TryParse(skill.Value?.ToString(), out int points))
                        {
                            totalPoints += points;
                            DebugLog($"Additional skill {skill.Key}: +{points} (Total: {totalPoints})");
                        }
                    }
                }
            }
            else
            {
                DebugLog($"No skills data found for user {userId}");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error calculating points for user {userId}: {ex.Message}");
        }

        DebugLog($"Final points for {userId}: {totalPoints}");
        return totalPoints;
    }

    private async Task DisplayRating()
    {
        if (ratingItems == null || ratingItems.Length == 0)
        {
            Debug.LogError("Rating UI elements not assigned!");
            return;
        }

        DebugLog($"Displaying rating for {allUsers.Count} users");

        // Clear UI
        for (int i = 0; i < ratingItems.Length; i++)
        {
            if (ratingItems[i] == null)
            {
                DebugLog($"Rating item {i} is null!");
                continue;
            }

            var texts = ratingItems[i].GetComponentsInChildren<Text>(true);
            foreach (var text in texts)
            {
                if (text != null)
                {
                    text.text = "";
                    DebugLog($"Cleared text in position {i}");
                }
            }
            ratingItems[i].SetActive(false);
        }

        // Find current user in the ranking list (if they have points > 0)
        UserRatingData currentUserData = null;
        if (UserSession.CurrentUser != null)
        {
            currentUserData = allUsers.FirstOrDefault(u => u.User.Id == UserSession.CurrentUser.Id);
            DebugLog(currentUserData != null ?
                $"Current user found at position {allUsers.IndexOf(currentUserData) + 1} in ranking" :
                "Current user not found in ranking (likely has 0 points)");
        }

        // Display top 10 users in the main list
        var usersToShow = new List<UserRatingData>();
        int topUsersToShow = Mathf.Min(10, allUsers.Count);

        for (int i = 0; i < topUsersToShow; i++)
        {
            usersToShow.Add(allUsers[i]);
            DebugLog($"Added top user {i + 1}: {allUsers[i].User.Last} {allUsers[i].User.First}");
        }

        // Display users in the main list
        for (int i = 0; i < usersToShow.Count; i++)
        {
            if (i >= ratingItems.Length || ratingItems[i] == null)
            {
                DebugLog($"Skipping display for position {i} - no UI element");
                continue;
            }

            var userData = usersToShow[i];
            var texts = ratingItems[i].GetComponentsInChildren<Text>(true);
            DebugLog($"Found {texts.Length} text components for position {i}");

            if (texts.Length >= 3)
            {
                int actualPosition = allUsers.IndexOf(userData) + 1;
                texts[0].text = actualPosition.ToString();
                texts[1].text = $"{userData.User.Last} {userData.User.First}";
                texts[2].text = userData.TotalPoints.ToString();
                ratingItems[i].SetActive(true);

                DebugLog($"Displaying at position {i}: " +
                         $"{actualPosition}. {userData.User.Last} {userData.User.First} - {userData.TotalPoints} pts");

                // Highlight current user if they are in the top 10
                if (currentUserData != null && userData.User.Id == currentUserData.User.Id)
                {
                    HighlightPosition(ratingItems[i]);
                    DebugLog($"Highlighting current user at position {i} in main list");
                }
            }
            else
            {
                DebugLog($"Not enough text components ({texts.Length}) for position {i}");
            }
        }

        // Always display the current user in the "Current User Rating" section
        if (currentUserRatingItem != null && UserSession.CurrentUser != null)
        {
            var texts = currentUserRatingItem.GetComponentsInChildren<Text>(true);
            if (texts.Length >= 3)
            {
                int position = -1;
                int currentUserPoints = 0;
                string groupName = "N/A";

                // If the current user is in the ranking list, use their data
                if (currentUserData != null)
                {
                    position = allUsers.IndexOf(currentUserData) + 1;
                    currentUserPoints = currentUserData.TotalPoints;
                    groupName = currentUserData.GroupName;
                }
                else
                {
                    // Otherwise, calculate their points and fetch their group name
                    currentUserPoints = await CalculateTotalPoints(UserSession.CurrentUser.Id);
                    DebugLog($"Current user points (calculated separately): {currentUserPoints}");

                    // Fetch the current user's group name
                    DataSnapshot userSnapshot = await FirebaseDatabase.DefaultInstance
                        .GetReference("14")
                        .Child("data")
                        .Child(UserSession.CurrentUser.Id)
                        .GetValueAsync();

                    if (userSnapshot.Exists)
                    {
                        string groupId = userSnapshot.Child("group_id")?.Value?.ToString();
                        if (!string.IsNullOrEmpty(groupId))
                        {
                            var dbManager = FindObjectOfType<FirebaseDBManager>();
                            if (dbManager != null)
                            {
                                groupName = await dbManager.GetGroupName(groupId);
                                if (string.IsNullOrEmpty(groupName))
                                {
                                    DebugLog($"Group ID {groupId} not found for current user");
                                    groupName = "N/A";
                                }
                            }
                        }
                    }
                }

                texts[0].text = position > 0 ? position.ToString() : "-";
                texts[1].text = $"{UserSession.CurrentUser.Last} {UserSession.CurrentUser.First}";
                texts[2].text = currentUserPoints.ToString();
                HighlightPosition(currentUserRatingItem);

                DebugLog($"Displaying current user in Current User Rating section: " +
                         $"{(position > 0 ? position.ToString() : "-")}. " +
                         $"{UserSession.CurrentUser.Last} {UserSession.CurrentUser.First} - {currentUserPoints} pts, Group: {groupName}");
            }
            currentUserRatingItem.SetActive(true);
        }
        else
        {
            if (currentUserRatingItem != null)
            {
                currentUserRatingItem.SetActive(false);
                DebugLog("No current user logged in or currentUserRatingItem is null, hiding Current User Rating section");
            }
        }
    }

    private void HighlightPosition(GameObject ratingItem)
    {
        var image = ratingItem.GetComponent<Image>();
        if (image != null)
        {
            image.color = currentUserHighlightColor;
        }
    }

    private void DebugLog(string message)
    {
        if (debugMode)
        {
            Debug.Log($"[RatingDebug] {message}");
        }
    }
}