using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Database;
using System.Linq;
using System;

public class RatingPreloader : MonoBehaviour
{
    public async Task PreloadRatingDataAsync()
    {
        Debug.Log("[RatingPreloader] Starting preload...");

        var dbManager = FindObjectOfType<FirebaseDBManager>();
        if (dbManager == null)
        {
            Debug.LogError("[RatingPreloader] FirebaseDBManager not found");
            return;
        }

        try
        {
            var allUsers = new List<UserSession.UserRatingData>();
            DataSnapshot usersSnapshot = await FirebaseDatabase.DefaultInstance
                .GetReference("14/data")
                .GetValueAsync();

            if (!usersSnapshot.Exists)
            {
                Debug.LogWarning("[RatingPreloader] No users found");
                return;
            }

            var groups = UserSession.CachedGroups.Count > 0
                ? UserSession.CachedGroups
                : await dbManager.GetAllGroups();
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
            Debug.Log($"[RatingPreloader] Cached {allUsers.Count} users");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[RatingPreloader] Error: {ex.Message}");
        }
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
        catch (Exception ex)
        {
            Debug.LogError($"[RatingPreloader] Error calculating points for {userId}: {ex.Message}");
        }
        return totalPoints;
    }
}