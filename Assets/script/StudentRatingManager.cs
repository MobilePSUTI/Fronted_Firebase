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
            DebugLog("Rating load already in progress");
            return;
        }

        isLoading = true;
        DebugLog("Starting rating load...");

        try
        {
            await LoadAllUsersWithPoints();
            DisplayRating();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Rating error: {ex.Message}");
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

            if (points > 0) // Only include users with points
            {
                string groupId = userSnapshot.Child("group_id")?.Value?.ToString();
                groupNames.TryGetValue(groupId, out string groupName);

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

                DebugLog($"Added to rating: {user.Last} {user.First} - {points} points");
            }
        }

        // Sort by points descending
        allUsers = allUsers.OrderByDescending(u => u.TotalPoints).ToList();
        DebugLog($"Total users with points: {allUsers.Count}");
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
            Debug.LogError($"Error calculating points: {ex.Message}");
        }

        DebugLog($"Final points for {userId}: {totalPoints}");
        return totalPoints;
    }

    private void DisplayRating()
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

        // Find current user
        UserRatingData currentUserData = null;
        if (UserSession.CurrentUser != null)
        {
            currentUserData = allUsers.FirstOrDefault(u => u.User.Id == UserSession.CurrentUser.Id);
            DebugLog(currentUserData != null ?
                $"Current user found at position {allUsers.IndexOf(currentUserData) + 1}" :
                "Current user not found in rating");
        }

        // Determine who to show
        var usersToShow = new List<UserRatingData>();
        int displayCount = Mathf.Min(10, allUsers.Count);

        bool showCurrentUserSeparately = currentUserData != null &&
                                      allUsers.IndexOf(currentUserData) >= 10;

        int topUsersToShow = showCurrentUserSeparately ?
            Mathf.Min(9, allUsers.Count) :
            displayCount;

        // Add top users
        for (int i = 0; i < topUsersToShow; i++)
        {
            usersToShow.Add(allUsers[i]);
            DebugLog($"Added top user {i + 1}: {allUsers[i].User.Last} {allUsers[i].User.First}");
        }

        // Add current user if needed
        if (showCurrentUserSeparately)
        {
            usersToShow.Add(currentUserData);
            DebugLog($"Added current user separately at position {usersToShow.Count}");
        }

        // Display users
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

                // Highlight current user
                if (currentUserData != null && userData.User.Id == currentUserData.User.Id)
                {
                    HighlightPosition(ratingItems[i]);
                    DebugLog($"Highlighting current user at position {i}");
                }
            }
            else
            {
                DebugLog($"Not enough text components ({texts.Length}) for position {i}");
            }
        }

        // Current user special display
        if (currentUserRatingItem != null)
        {
            bool shouldShowCurrentUser = currentUserData != null &&
                                      (allUsers.IndexOf(currentUserData) >= 10 ||
                                       allUsers.IndexOf(currentUserData) == -1);

            if (shouldShowCurrentUser)
            {
                var texts = currentUserRatingItem.GetComponentsInChildren<Text>(true);
                if (texts.Length >= 3)
                {
                    int position = allUsers.IndexOf(currentUserData) + 1;
                    texts[0].text = position > 0 ? position.ToString() : "-";
                    texts[1].text = $"{currentUserData.User.Last} {currentUserData.User.First}";
                    texts[2].text = currentUserData.TotalPoints.ToString();
                    HighlightPosition(currentUserRatingItem);

                    DebugLog($"Displaying current user separately: " +
                             $"{position}. {currentUserData.User.Last} {currentUserData.User.First}");
                }
                currentUserRatingItem.SetActive(true);
            }
            else
            {
                currentUserRatingItem.SetActive(false);
                DebugLog("Current user is in top 10, hiding separate display");
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