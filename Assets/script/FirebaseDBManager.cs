using UnityEngine;
using Firebase;
using Firebase.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class FirebaseDBManager : MonoBehaviour
{
    public static FirebaseDBManager _instance;
    public static FirebaseDBManager Instance => _instance;

    public DatabaseReference databaseRef;
    public bool isInitialized;
    private const int MaxRetries = 3;
    private const float RetryDelayBase = 1f;

    public DatabaseReference DatabaseReference
    {
        get
        {
            if (!isInitialized)
            {
                Debug.LogError("[Firebase] Not initialized");
                return null;
            }
            return databaseRef;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public async Task Initialize()
    {
        if (isInitialized) return;

        try
        {
            var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();
            if (dependencyStatus != DependencyStatus.Available)
                throw new Exception($"[Firebase] Could not resolve dependencies: {dependencyStatus}");

            FirebaseApp app = FirebaseApp.DefaultInstance;
            FirebaseDatabase.DefaultInstance.SetPersistenceEnabled(true);
            databaseRef = FirebaseDatabase.GetInstance(app).RootReference;
            isInitialized = true;
            Debug.Log("[Firebase] Initialized successfully");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Firebase] Initialization failed: {ex.Message}");
            throw;
        }
    }

    public async Task<User> AuthenticateUser(string email, string password)
    {
        if (!isInitialized) await Initialize();

        try
        {
            Debug.Log($"[Auth] Authenticating: {email}");
            DataSnapshot snapshot = await RetryQuery(() => databaseRef
                .Child("14/data")
                .OrderByChild("email")
                .EqualTo(email.ToLower().Trim())
                .GetValueAsync());

            if (!snapshot.Exists || !snapshot.HasChildren)
            {
                Debug.LogError("[Auth] User not found");
                return null;
            }

            var userData = snapshot.Children.First();
            if (userData.Child("email")?.Value?.ToString()?.ToLower().Trim() != email.ToLower().Trim())
            {
                Debug.LogError("[Auth] Email mismatch");
                return null;
            }

            if (userData.Child("password")?.Value?.ToString() != password.Trim())
            {
                Debug.LogError("[Auth] Password incorrect");
                return null;
            }

            string userId = userData.Key;
            string role = await GetUserRole(userId);

            // Создаем объект в зависимости от роли
            if (role == "teacher")
            {
                Debug.Log($"[Auth] Success! Role: {role}");
                return new Teacher
                {
                    Id = userId,
                    Username = userData.Child("username")?.Value?.ToString() ?? "",
                    Email = email,
                    First = userData.Child("first_name")?.Value?.ToString() ?? "",
                    Last = userData.Child("last_name")?.Value?.ToString() ?? "",
                    Second = userData.Child("second_name")?.Value?.ToString() ?? "",
                    Role = role,
                    AvatarPath = userData.Child("avatar_path")?.Value?.ToString() ?? "",
                    CreatedAt = userData.Child("created_at")?.Value?.ToString() ?? "",
                    UpdatedAt = userData.Child("updated_at")?.Value?.ToString() ?? ""
                };
            }
            else
            {
                string groupId = userData.Child("group_id")?.Value?.ToString();
                string groupName = await GetGroupName(groupId);

                Debug.Log($"[Auth] Success! Role: {role}");
                return new Student
                {
                    Id = userId,
                    Username = userData.Child("username")?.Value?.ToString() ?? "",
                    Email = email,
                    First = userData.Child("first_name")?.Value?.ToString() ?? "",
                    Last = userData.Child("last_name")?.Value?.ToString() ?? "",
                    Second = userData.Child("second_name")?.Value?.ToString() ?? "",
                    GroupId = groupId,
                    GroupName = groupName,
                    Role = role,
                    AvatarPath = userData.Child("avatar_path")?.Value?.ToString() ?? "",
                    CreatedAt = userData.Child("created_at")?.Value?.ToString() ?? "",
                    UpdatedAt = userData.Child("updated_at")?.Value?.ToString() ?? ""
                };
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Auth] Error: {ex.Message}");
            return null;
        }
    }

    public async Task<string> GetGroupName(string groupId)
    {
        if (string.IsNullOrEmpty(groupId))
            return "Группа не указана";

        var cachedGroup = UserSession.CachedGroups.Find(g => g.Id == groupId);
        if (cachedGroup != null)
            return cachedGroup.Title;

        try
        {
            DataSnapshot snapshot = await RetryQuery(() => databaseRef
                .Child("6/data")
                .Child(groupId)
                .GetValueAsync());

            if (snapshot.Exists)
            {
                string groupName = snapshot.Child("title")?.Value?.ToString() ?? "Группа не найдена";
                UserSession.CachedGroups.Add(new Group
                {
                    Id = groupId,
                    Title = groupName,
                    Course = snapshot.Child("course")?.Value?.ToString() ?? "",
                    ProgramId = snapshot.Child("educational_program_id")?.Value?.ToString() ?? ""
                });
                UserSession.SaveSession();
                return groupName;
            }

            Debug.LogWarning($"[Group] Group {groupId} not found");
            return "Группа не найдена";
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Group] Error: {ex.Message}");
            return "Ошибка загрузки";
        }
    }

    private async Task<string> GetUserRole(string userId)
    {
        try
        {
            DataSnapshot snapshot = await RetryQuery(() => databaseRef
                .Child("14/data")
                .Child(userId)
                .GetValueAsync());

            if (snapshot.Exists && snapshot.HasChild("role"))
            {
                return snapshot.Child("role").Value.ToString();
            }

            snapshot = await RetryQuery(() => databaseRef
                .Child("15/data")
                .OrderByChild("student_id")
                .EqualTo(userId)
                .GetValueAsync());

            if (snapshot.Exists && snapshot.HasChildren)
                return snapshot.Children.First().Child("role_id")?.Value?.ToString() == "2" ? "teacher" : "student";
            return "student";
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Role] Error: {ex.Message}");
            return "student";
        }
    }

    public async Task<bool> RegisterStudent(string email, string password, string firstName, string lastName, string secondName, string groupId, byte[] avatar)
    {
        if (!isInitialized) await Initialize();

        try
        {
            if (await CheckEmailExists(email))
            {
                Debug.Log("[Register] Email already exists");
                return false;
            }

            DataSnapshot groupSnapshot = await RetryQuery(() => databaseRef.Child("6/data").Child(groupId).GetValueAsync());
            if (!groupSnapshot.Exists)
            {
                Debug.LogError($"[Register] Group {groupId} not found");
                return false;
            }

            string groupTitle = groupSnapshot.Child("title")?.Value?.ToString() ?? "Unknown";
            string programId = groupSnapshot.Child("educational_program_id")?.Value?.ToString();
            if (string.IsNullOrEmpty(programId))
            {
                Debug.LogError($"[Register] Group {groupId} has no program");
                return false;
            }

            DataSnapshot programSnapshot = await GetEducationalProgram(programId);
            if (programSnapshot == null)
            {
                Debug.LogError($"[Register] Program {programId} not found");
                return false;
            }

            List<string> mainSkills = programSnapshot.Child("main_skills")?.Children
                .Select(s => s.Value?.ToString())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList() ?? new List<string>();

            List<string> additionalSkills = (await databaseRef.Child("11/data").GetValueAsync())
                .Children
                .Where(s => s.Child("type")?.Value?.ToString() == "additional")
                .Select(s => s.Child("id")?.Value?.ToString())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();

            string userId = databaseRef.Child("14/data").Push().Key;
            string avatarBase64 = avatar != null && avatar.Length > 0 ? Convert.ToBase64String(avatar) : "";

            var userData = new Dictionary<string, object>
            {
                {"id", userId},
                {"email", email.ToLower().Trim()},
                {"password", password.Trim()},
                {"first_name", firstName.Trim()},
                {"last_name", lastName.Trim()},
                {"second_name", secondName?.Trim() ?? ""},
                {"group_id", groupId},
                {"created_at", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")},
                {"updated_at", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")},
                {"avatar_path", avatarBase64},
                {"username", $"{firstName} {lastName}"},
                {"skill_id", "0"},
                {"game_stats_ref", userId}
            };

            string roleId = databaseRef.Child("15/data").Push().Key;
            var roleData = new Dictionary<string, object>
            {
                {"id", roleId},
                {"role_id", "1"},
                {"student_id", userId}
            };

            var skillsData = new Dictionary<string, object>
            {
                {"main_skills", mainSkills.ToDictionary(s => s, _ => 0)},
                {"additional_skills", additionalSkills.ToDictionary(s => s, _ => 0)}
            };

            var updates = new Dictionary<string, object>
            {
                {$"14/data/{userId}", userData},
                {$"15/data/{roleId}", roleData},
                {$"16/data/{userId}", skillsData}
            };

            await databaseRef.UpdateChildrenAsync(updates);
            Debug.Log($"[Register] Student {email} registered");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Register] Error: {ex.Message}");
            return false;
        }
    }

    private async Task<DataSnapshot> GetEducationalProgram(string programId)
    {
        DataSnapshot snapshot = await RetryQuery(() => databaseRef.Child("4/data").GetValueAsync());
        return snapshot.Children.FirstOrDefault(p => p.Child("id")?.Value?.ToString() == programId);
    }

    private async Task<bool> CheckEmailExists(string email)
    {
        DataSnapshot snapshot = await RetryQuery(() => databaseRef.Child("14/data").GetValueAsync());
        return snapshot.Children.Any(u => u.Child("email")?.Value?.ToString()?.ToLower() == email.ToLower());
    }

    public async Task<List<string>> GetProgramSkills(string programId)
    {
        try
        {
            if (!isInitialized) await Initialize();
            DataSnapshot snapshot = await RetryQuery(() => databaseRef.Child("4/data").GetValueAsync());
            var program = snapshot.Children.FirstOrDefault(p => p.Child("id")?.Value?.ToString() == programId);
            return program?.Child("main_skills")?.Children
                .Select(s => s.Value?.ToString())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList() ?? new List<string>();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Skills] Error: {ex.Message}");
            return new List<string>();
        }
    }

    public async Task<Group> GetGroupDetails(string groupId)
    {
        try
        {
            DataSnapshot snapshot = await RetryQuery(() => databaseRef.Child("6/data").Child(groupId).GetValueAsync());
            if (snapshot.Exists)
                return new Group
                {
                    Id = groupId,
                    Title = snapshot.Child("title")?.Value?.ToString() ?? "",
                    Course = snapshot.Child("course")?.Value?.ToString() ?? "",
                    ProgramId = snapshot.Child("educational_program_id")?.Value?.ToString() ?? ""
                };
            return null;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Group] Error: {ex.Message}");
            return null;
        }
    }

    public async Task<byte[]> GetUserAvatar(string userId)
    {
        try
        {
            if (!isInitialized) await Initialize();
            DataSnapshot snapshot = await RetryQuery(() => databaseRef.Child("14/data").Child(userId).GetValueAsync());
            string avatarBase64 = snapshot.Child("avatar_path")?.Value?.ToString();
            if (string.IsNullOrEmpty(avatarBase64))
                return null;

            return Convert.FromBase64String(avatarBase64);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Avatar] Error: {ex.Message}");
            return null;
        }
    }

    public async Task<List<Group>> GetAllGroups()
    {
        try
        {
            if (!isInitialized) await Initialize();
            DataSnapshot snapshot = await RetryQuery(() => databaseRef.Child("6/data").GetValueAsync());
            return snapshot.Children.Select(g => new Group
            {
                Id = g.Key,
                Title = g.Child("title")?.Value?.ToString() ?? "Название не указано",
                Course = g.Child("course")?.Value?.ToString() ?? "",
                ProgramId = g.Child("educational_program_id")?.Value?.ToString() ?? ""
            }).ToList();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Groups] Error: {ex.Message}");
            return new List<Group>();
        }
    }

    public async Task<List<Student>> GetStudentsByGroup(string groupId)
    {
        try
        {
            if (!isInitialized) await Initialize();
            DataSnapshot snapshot = await RetryQuery(() => databaseRef.Child("14/data").GetValueAsync());
            return (await Task.WhenAll(snapshot.Children
                .Where(u => u.Child("group_id")?.Value?.ToString() == groupId)
                .Select(async u => new Student
                {
                    Id = u.Key,
                    Username = u.Child("username")?.Value?.ToString() ?? "",
                    First = u.Child("first_name")?.Value?.ToString() ?? "",
                    Last = u.Child("last_name")?.Value?.ToString() ?? "",
                    Second = u.Child("second_name")?.Value?.ToString() ?? "",
                    GroupId = groupId,
                    GroupName = await GetGroupName(groupId),
                    Email = u.Child("email")?.Value?.ToString() ?? ""
                }))).ToList();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Students] Error: {ex.Message}");
            return new List<Student>();
        }
    }

    public async Task<Student> GetStudentDetails(string studentId)
    {
        try
        {
            DataSnapshot snapshot = await RetryQuery(() => databaseRef.Child("14/data").Child(studentId).GetValueAsync());
            if (snapshot.Exists)
            {
                string groupId = snapshot.Child("group_id")?.Value?.ToString();
                return new Student
                {
                    Id = studentId,
                    First = snapshot.Child("first_name")?.Value?.ToString() ?? "",
                    Last = snapshot.Child("last_name")?.Value?.ToString() ?? "",
                    Second = snapshot.Child("second_name")?.Value?.ToString() ?? "",
                    GroupId = groupId,
                    Email = snapshot.Child("email")?.Value?.ToString() ?? "",
                    AvatarPath = snapshot.Child("avatar_path")?.Value?.ToString() ?? "",
                    Username = snapshot.Child("username")?.Value?.ToString() ?? "",
                    CreatedAt = snapshot.Child("created_at")?.Value?.ToString() ?? "",
                    UpdatedAt = snapshot.Child("updated_at")?.Value?.ToString() ?? "",
                    GroupName = await GetGroupName(groupId)
                };
            }
            return null;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Student] Error: {ex.Message}");
            return null;
        }
    }

    public async Task UpdateStudentStats(string studentId, int coinsToAdd, float timeToAdd)
    {
        try
        {
            DataSnapshot snapshot = await RetryQuery(() => databaseRef.Child("14/data").Child(studentId).GetValueAsync());
            if (snapshot.Exists)
            {
                int currentCoins = int.TryParse(snapshot.Child("total_coins")?.Value?.ToString(), out var coins) ? coins : 0;
                float currentTime = float.TryParse(snapshot.Child("total_play_time")?.Value?.ToString(), out var time) ? time : 0f;

                var updates = new Dictionary<string, object>
                {
                    {"total_coins", currentCoins + coinsToAdd},
                    {"total_play_time", currentTime + timeToAdd},
                    {"updated_at", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")}
                };

                await databaseRef.Child("14/data").Child(studentId).UpdateChildrenAsync(updates);
                Debug.Log($"[Stats] Updated: +{coinsToAdd} coins, +{timeToAdd} seconds");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Stats] Error: {ex.Message}");
        }
    }

    public async Task<Dictionary<string, int>> GetStudentGameScores(string studentId)
    {
        var scores = new Dictionary<string, int>();
        try
        {
            if (!isInitialized) await Initialize();
            DataSnapshot snapshot = await RetryQuery(() => databaseRef
                .Child("game_results")
                .OrderByChild("student_id")
                .EqualTo(studentId)
                .GetValueAsync());

            foreach (DataSnapshot game in snapshot.Children)
            {
                string gameName = game.Child("game_name")?.Value?.ToString() ?? "unknown";
                int coins = int.TryParse(game.Child("coins")?.Value?.ToString(), out var c) ? c : 0;
                scores[gameName] = scores.GetValueOrDefault(gameName, 0) + coins;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Scores] Error: {ex.Message}");
        }
        return scores;
    }

    private async Task<DataSnapshot> RetryQuery(Func<Task<DataSnapshot>> query)
    {
        for (int attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                return await query();
            }
            catch (Exception ex)
            {
                if (attempt == MaxRetries)
                {
                    Debug.LogError($"[Query] Failed after {MaxRetries} attempts: {ex.Message}");
                    throw;
                }
                Debug.LogWarning($"[Query] Attempt {attempt} failed: {ex.Message}");
                await Task.Delay((int)(RetryDelayBase * 1000 * attempt));
            }
        }
        return null;
    }
}