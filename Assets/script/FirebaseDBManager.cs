using UnityEngine;
using Firebase;
using Firebase.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

public class FirebaseDBManager : MonoBehaviour
{
    private static FirebaseDBManager _instance;
    public static FirebaseDBManager Instance => _instance;

    private DatabaseReference databaseRef;
    public bool isInitialized = false;
    public DatabaseReference DatabaseReference 
    {
        get 
        {
            if (!isInitialized)
            {
                Debug.LogError("FirebaseDBManager is not initialized!");
                return null;
            }
            return databaseRef;
        }
    }
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this); // Уничтожаем только компонент, а не весь GameObject
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject); // Переносим только этот объект
    }

    public async Task Initialize()
    {
        if (isInitialized) return;

        try
        {
            var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();
            if (dependencyStatus != DependencyStatus.Available)
            {
                throw new Exception($"Could not resolve dependencies: {dependencyStatus}");
            }

            FirebaseApp app = FirebaseApp.DefaultInstance;
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
            Debug.Log($"[Auth] Starting authentication for: {email}");

            DataSnapshot dbSnapshot = await databaseRef.GetValueAsync();

            if (!dbSnapshot.Exists)
            {
                Debug.LogError("[Auth] Database is empty");
                return null;
            }

            // Find users table
            DataSnapshot usersTable = null;
            foreach (DataSnapshot node in dbSnapshot.Children)
            {
                if (node.HasChild("name") && node.Child("name").Value?.ToString() == "users")
                {
                    usersTable = node.Child("data");
                    break;
                }
            }

            if (usersTable == null)
            {
                Debug.LogError("[Auth] Users table not found");
                return null;
            }

            foreach (DataSnapshot userSnapshot in usersTable.Children)
            {
                string userEmail = userSnapshot.Child("email")?.Value?.ToString();
                Debug.Log($"[Auth] Checking user: {userEmail}");

                if (userEmail?.ToLower().Trim() == email.ToLower().Trim())
                {
                    string storedPass = userSnapshot.Child("password")?.Value?.ToString();
                    Debug.Log($"[Auth] Password check for: {userEmail}");

                    if (storedPass == password.Trim())
                    {
                        string userId = userSnapshot.Child("id")?.Value?.ToString() ?? userSnapshot.Key;
                        string role = await GetUserRole(userId);

                        Debug.Log($"[Auth] Authentication successful! Role: {role}");

                        return new Student
                        {
                            Id = userId,
                            Username = userSnapshot.Child("username")?.Value?.ToString() ?? "",
                            Email = email,
                            First = userSnapshot.Child("first_name")?.Value?.ToString() ?? "",
                            Last = userSnapshot.Child("last_name")?.Value?.ToString() ?? "",
                            Second = userSnapshot.Child("second_name")?.Value?.ToString() ?? "",
                            GroupName = await GetGroupName(userSnapshot.Child("group_id")?.Value?.ToString()),
                            Role = role,
                            AvatarPath = userSnapshot.Child("avatar_path")?.Value?.ToString() ?? ""
                        };
                    }
                    else
                    {
                        Debug.Log("[Auth] Password incorrect");
                        return null;
                    }
                }
            }

            Debug.LogError("[Auth] User not found in database");
            return null;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Auth] Critical error: {ex.Message}\n{ex.StackTrace}");
            return null;
        }
    }

    public async Task<string> GetGroupName(string groupId)
    {
        if (string.IsNullOrEmpty(groupId))
        {
            Debug.LogWarning("GroupId is null or empty!");
            return "Группа не указана";
        }

        try
        {
            // Correct path to groups table
            DataSnapshot groupsSnapshot = await databaseRef.Child("6").Child("data").GetValueAsync();

            if (groupsSnapshot.Exists)
            {
                foreach (DataSnapshot groupSnapshot in groupsSnapshot.Children)
                {
                    string currentGroupId = groupSnapshot.Child("id")?.Value?.ToString();
                    if (currentGroupId == groupId)
                    {
                        string groupName = groupSnapshot.Child("title")?.Value?.ToString();
                        if (!string.IsNullOrEmpty(groupName))
                        {
                            return groupName;
                        }
                    }
                }
            }

            Debug.LogWarning($"Группа с ID {groupId} не найдена в Firebase");
            return "Группа не найдена";
        }
        catch (Exception ex)
        {
            Debug.LogError($"Ошибка при загрузке группы: {ex.Message}");
            return "Ошибка загрузки";
        }
    }

    private async Task<string> GetUserRole(string userId)
    {
        try
        {
            DataSnapshot dbSnapshot = await databaseRef.GetValueAsync();

            // Find the users_roles table
            DataSnapshot rolesTable = null;
            foreach (DataSnapshot node in dbSnapshot.Children)
            {
                if (node.HasChild("name") && node.Child("name").Value?.ToString() == "users_roles")
                {
                    rolesTable = node.Child("data");
                    break;
                }
            }

            if (rolesTable != null)
            {
                foreach (DataSnapshot roleSnapshot in rolesTable.Children)
                {
                    string studentId = roleSnapshot.Child("student_id")?.Value?.ToString();
                    string roleId = roleSnapshot.Child("role_id")?.Value?.ToString();

                    if (studentId == userId)
                    {
                        return roleId == "2" ? "teacher" : "student";
                    }
                }
            }
            return "student"; // Default to student if role not found
        }
        catch
        {
            return "student"; // Default to student on error
        }
    }

    public async Task<bool> RegisterStudent(
        string email,
        string password,
        string firstName,
        string lastName,
        string secondName,
        string groupId,
        byte[] avatar)
    {
        if (!isInitialized)
        {
            Debug.LogError("Firebase not initialized");
            return false;
        }

        try
        {
            if (await CheckEmailExists(email))
            {
                Debug.Log("Email already exists");
                return false;
            }

            Debug.Log($"[RegisterStudent] Attempting to retrieve group with ID: {groupId}");
            DataSnapshot groupSnapshot = await databaseRef
                .Child("6")
                .Child("data")
                .GetValueAsync();

            if (!groupSnapshot.Exists || !groupSnapshot.HasChild(groupId))
            {
                Debug.LogError($"Group {groupId} not found in groups table");
                return false;
            }

            DataSnapshot targetGroup = groupSnapshot.Child(groupId);
            string groupTitle = targetGroup.Child("title").Value?.ToString() ?? "Unknown";
            string programId = targetGroup.Child("educational_program_id").Value?.ToString();

            if (string.IsNullOrEmpty(programId))
            {
                Debug.LogError($"Group {groupId} ({groupTitle}) has no educational program");
                return false;
            }

            Debug.Log($"[RegisterStudent] Group ID: {groupId}, Title: {groupTitle}, Program ID: {programId}");

            DataSnapshot programSnapshot = await GetEducationalProgram(programId);
            if (programSnapshot == null)
            {
                Debug.LogError($"Program {programId} not found");
                return false;
            }

            string programTitle = programSnapshot.Child("title").Value?.ToString();
            Debug.Log($"[RegisterStudent] Found program: {programTitle} (ID: {programId})");

            List<string> mainSkills = new List<string>();
            DataSnapshot skillsNode = programSnapshot.Child("main_skills");
            if (skillsNode.Exists)
            {
                foreach (var skill in skillsNode.Children)
                {
                    string skillId = skill.Value?.ToString();
                    if (!string.IsNullOrEmpty(skillId))
                    {
                        mainSkills.Add(skillId);
                    }
                }
            }

            List<string> additionalSkills = new List<string>();
            DataSnapshot allSkillsSnapshot = await databaseRef.Child("11").Child("data").GetValueAsync();
            if (allSkillsSnapshot.Exists)
            {
                foreach (DataSnapshot skillSnapshot in allSkillsSnapshot.Children)
                {
                    if (skillSnapshot.HasChild("type") && skillSnapshot.Child("type").Value?.ToString() == "additional")
                    {
                        string skillId = skillSnapshot.Child("id").Value?.ToString();
                        if (!string.IsNullOrEmpty(skillId))
                        {
                            additionalSkills.Add(skillId);
                        }
                    }
                }
            }

            string userId = databaseRef.Child("14").Child("data").Push().Key;

            // Преобразуем аватар в Base64, если он есть
            string avatarBase64 = "";
            if (avatar != null && avatar.Length > 0)
            {
                avatarBase64 = Convert.ToBase64String(avatar);
                Debug.Log($"[RegisterStudent] Avatar converted to Base64: {avatarBase64.Length} characters");

                // Проверка валидности Base64
                try
                {
                    Convert.FromBase64String(avatarBase64);
                    Debug.Log($"[RegisterStudent] Base64 string is valid for user {userId}");
                }
                catch (FormatException ex)
                {
                    Debug.LogError($"[RegisterStudent] Generated Base64 string is invalid for user {userId}: {ex.Message}");
                    avatarBase64 = ""; // Сбрасываем, чтобы не сохранять невалидные данные
                }
            }

            var userData = new Dictionary<string, object>
            {
                {"id", userId},
                {"email", email.ToLower().Trim()},
                {"password", password.Trim()},
                {"first_name", firstName.Trim()},
                {"last_name", lastName.Trim()},
                {"second_name", string.IsNullOrEmpty(secondName) ? "" : secondName.Trim()},
                {"group_id", groupId},
                {"created_at", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")},
                {"updated_at", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")},
                {"avatar_path", avatarBase64},
                {"username", $"{firstName} {lastName}"},
                {"skill_id", "0"},
                {"game_stats_ref", userId}
            };

            string roleId = databaseRef.Child("15").Child("data").Push().Key;
            var roleData = new Dictionary<string, object>
            {
                {"id", roleId},
                {"role_id", "1"},
                {"student_id", userId}
            };

            var skillsData = new Dictionary<string, object>
            {
                {"main_skills", new Dictionary<string, int>()},
                {"additional_skills", new Dictionary<string, int>()}
            };

            foreach (string skillId in mainSkills)
            {
                ((Dictionary<string, int>)skillsData["main_skills"]).Add(skillId, 0);
            }

            foreach (string skillId in additionalSkills)
            {
                ((Dictionary<string, int>)skillsData["additional_skills"]).Add(skillId, 0);
            }

            var updates = new Dictionary<string, object>
            {
                {$"14/data/{userId}", userData},
                {$"15/data/{roleId}", roleData},
                {$"16/data/{userId}", skillsData}
            };

            await databaseRef.UpdateChildrenAsync(updates);

            Debug.Log($"Student {email} registered successfully in program {programTitle}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Registration failed: {ex.Message}\n{ex.StackTrace}");
            return false;
        }
    }

    private async Task<DataSnapshot> GetEducationalProgram(string programId)
    {
        DataSnapshot programsSnapshot = await databaseRef
            .Child("4") // educational_programs table
            .Child("data")
            .GetValueAsync();

        if (!programsSnapshot.Exists) return null;

        foreach (DataSnapshot program in programsSnapshot.Children)
        {
            string currentId = program.Child("id").Value?.ToString();
            if (currentId == programId)
            {
                return program;
            }
        }

        return null;
    }

    private async Task<bool> CheckEmailExists(string email)
    {
        DataSnapshot usersSnapshot = await databaseRef
            .Child("14") // users table
            .Child("data")
            .GetValueAsync();

        if (!usersSnapshot.Exists) return false;

        foreach (DataSnapshot user in usersSnapshot.Children)
        {
            string userEmail = user.Child("email").Value?.ToString();
            if (userEmail?.ToLower() == email.ToLower())
            {
                return true;
            }
        }

        return false;
    }
    public async Task<List<string>> GetProgramSkills(string programId)
    {
        var skills = new List<string>();
        try
        {
            if (!isInitialized) await Initialize();

            // Получаем все программы
            DataSnapshot programsSnapshot = await databaseRef.Child("4").Child("data").GetValueAsync();

            if (programsSnapshot.Exists && programsSnapshot.HasChildren)
            {
                // Ищем программу с нужным ID в массиве
                foreach (DataSnapshot programSnapshot in programsSnapshot.Children)
                {
                    string currentProgramId = programSnapshot.Child("id")?.Value?.ToString();
                    if (currentProgramId == programId)
                    {
                        var skillsNode = programSnapshot.Child("main_skills");
                        if (skillsNode.Exists && skillsNode.ChildrenCount > 0)
                        {
                            foreach (var skill in skillsNode.Children)
                            {
                                if (!string.IsNullOrEmpty(skill.Value?.ToString()))
                                    skills.Add(skill.Value.ToString());
                            }
                        }
                        break; // Нашли нужную программу, выходим из цикла
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error getting program skills: {ex.Message}");
        }
        return skills;
    }
    public async Task<Group> GetGroupDetails(string groupId)
    {
        try
        {
            DataSnapshot snapshot = await databaseRef.Child("6").Child("data").Child(groupId).GetValueAsync();
            if (snapshot.Exists)
            {
                return new Group
                {
                    Id = groupId,
                    Title = snapshot.Child("title").Value?.ToString() ?? "",
                    Course = snapshot.Child("course").Value?.ToString() ?? "",
                    ProgramId = snapshot.Child("educational_program_id").Value?.ToString() ?? ""
                };
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error loading group details: {ex.Message}");
        }
        return null;
    }

    public async Task<byte[]> GetUserAvatar(string userId)
    {
        if (!isInitialized) await Initialize();

        try
        {
            // Путь к данным пользователя в таблице users (14)
            DataSnapshot snapshot = await databaseRef
                .Child("14") // Таблица users
                .Child("data")
                .Child(userId)
                .GetValueAsync();

            if (snapshot.Exists && snapshot.HasChild("avatar_path"))
            {
                string avatarBase64 = snapshot.Child("avatar_path").Value?.ToString();
                if (string.IsNullOrEmpty(avatarBase64))
                {
                    Debug.LogWarning($"[Avatar] Avatar path for user {userId} is empty");
                    return null;
                }

                try
                {
                    byte[] avatarData = Convert.FromBase64String(avatarBase64);
                    Debug.Log($"[Avatar] Successfully decoded Base64 avatar for user {userId}, size: {avatarData.Length} bytes");
                    return avatarData;
                }
                catch (FormatException ex)
                {
                    Debug.LogError($"[Avatar] Failed to decode Base64 string for user {userId}: {ex.Message}");
                    return null;
                }
            }

            Debug.LogWarning($"[Avatar] Avatar not found for user {userId}");
            return null;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Avatar] Error loading avatar: {ex.Message}");
            return null;
        }
    }

    public async Task<List<Group>> GetAllGroups()
    {
        List<Group> groups = new List<Group>();
        try
        {
            if (!isInitialized) await Initialize();

            Debug.Log("[Firebase] Loading groups...");

            // Получаем данные из узла 
            DataSnapshot groupsSnapshot = await databaseRef.Child("6").Child("data").GetValueAsync();

            Debug.Log($"Raw groups data: {groupsSnapshot.GetRawJsonValue()}");

            if (groupsSnapshot.Exists && groupsSnapshot.HasChildren)
            {
                foreach (DataSnapshot groupSnapshot in groupsSnapshot.Children)
                {
                    // Для структуры с таблицами и данными
                    var groupData = groupSnapshot.Child("data").Exists
                        ? groupSnapshot.Child("data")
                        : groupSnapshot;

                    groups.Add(new Group
                    {
                        Id = groupData.Child("id").Value?.ToString() ?? groupSnapshot.Key,
                        Title = groupData.Child("title").Value?.ToString() ?? "Название не указано",
                        Course = groupData.Child("course").Value?.ToString() ?? "",
                        ProgramId = groupData.Child("educational_program_id").Value?.ToString() ?? ""
                    });
                }
                Debug.Log($"[Firebase] Loaded {groups.Count} groups");
            }
            else
            {
                Debug.LogWarning("[Firebase] No groups found in database");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Firebase] Error loading groups: {ex.Message}");
        }

        return groups;
    }

    public async Task<List<Student>> GetStudentsByGroup(string groupId)
    {
        List<Student> students = new List<Student>();
        try
        {
            if (!isInitialized) await Initialize();

            // Получаем данные из таблицы users (у вас это узел 14)
            DataSnapshot usersSnapshot = await databaseRef.Child("14").Child("data").GetValueAsync();

            if (usersSnapshot.Exists)
            {
                foreach (DataSnapshot userSnapshot in usersSnapshot.Children)
                {
                    string userGroupId = userSnapshot.Child("group_id")?.Value?.ToString();

                    // Проверяем, что группа совпадает и поле group_id не пустое
                    if (!string.IsNullOrEmpty(userGroupId) && userGroupId == groupId)
                    {
                        var student = new Student
                        {
                            Id = userSnapshot.Key,
                            Username = userSnapshot.Child("username").Value?.ToString() ?? "",
                            First = userSnapshot.Child("first_name").Value?.ToString() ?? "",
                            Last = userSnapshot.Child("last_name").Value?.ToString() ?? "",
                            Second = userSnapshot.Child("second_name").Value?.ToString() ?? "",
                            GroupId = groupId,
                            GroupName = await GetGroupName(groupId),
                            Email = userSnapshot.Child("email").Value?.ToString() ?? ""
                        };
                        students.Add(student);
                    }
                }
            }

            Debug.Log($"Найдено студентов: {students.Count} для группы {groupId}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Ошибка загрузки студентов: {ex.Message}\n{ex.StackTrace}");
        }
        return students;
    }

    public async Task<Student> GetStudentDetails(string studentId)
    {
        try
        {
            DataSnapshot snapshot = await databaseRef.Child("14").Child("data").Child(studentId).GetValueAsync();

            if (snapshot.Exists)
            {
                return new Student
                {
                    Id = studentId,
                    First = snapshot.Child("first_name").Value?.ToString() ?? "",
                    Last = snapshot.Child("last_name").Value?.ToString() ?? "",
                    Second = snapshot.Child("second_name").Value?.ToString() ?? "",
                    GroupId = snapshot.Child("group_id").Value?.ToString() ?? "",
                    Email = snapshot.Child("email").Value?.ToString() ?? "",
                    AvatarPath = snapshot.Child("avatar_path").Value?.ToString() ?? "",
                    Username = snapshot.Child("username").Value?.ToString() ?? "",
                    CreatedAt = snapshot.Child("created_at").Value?.ToString() ?? "",
                    UpdatedAt = snapshot.Child("updated_at").Value?.ToString() ?? "",
                    GroupName = await GetGroupName(snapshot.Child("group_id").Value?.ToString())
                };
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error loading student data: {ex.Message}");
        }
        return null;
    }

    public async Task UpdateStudentStats(string studentId, int coinsToAdd, float timeToAdd)
    {
        try
        {
            // Получаем текущие данные студента
            DataSnapshot snapshot = await databaseRef.Child("14").Child("data").Child(studentId).GetValueAsync();

            if (snapshot.Exists)
            {
                // Парсим текущие значения
                int currentCoins = 0;
                float currentTime = 0f;

                if (snapshot.HasChild("total_coins"))
                    int.TryParse(snapshot.Child("total_coins").Value?.ToString(), out currentCoins);

                if (snapshot.HasChild("total_play_time"))
                    float.TryParse(snapshot.Child("total_play_time").Value?.ToString(), out currentTime);

                // Обновляем значения
                var updates = new Dictionary<string, object>
            {
                {"total_coins", currentCoins + coinsToAdd},
                {"total_play_time", currentTime + timeToAdd},
                {"updated_at", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")}
            };

                // Сохраняем обновления
                await databaseRef.Child("14").Child("data").Child(studentId).UpdateChildrenAsync(updates);

                Debug.Log($"Student stats updated: +{coinsToAdd} coins, +{timeToAdd} seconds");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error updating student stats: {ex.Message}");
        }
    }

    public async Task<Dictionary<string, int>> GetStudentGameScores(string studentId)
    {
        var scores = new Dictionary<string, int>();

        try
        {
            if (!isInitialized) await Initialize();
            if (string.IsNullOrEmpty(studentId)) return scores;

            DataSnapshot snapshot = await databaseRef.Child("game_results")
                .OrderByChild("student_id")
                .EqualTo(studentId)
                .GetValueAsync();

            if (snapshot.Exists)
            {
                foreach (DataSnapshot gameSnapshot in snapshot.Children)
                {
                    string gameName = gameSnapshot.Child("game_name")?.Value?.ToString() ?? "unknown";
                    int coins = 0;
                    if (gameSnapshot.Child("coins")?.Value != null)
                    {
                        int.TryParse(gameSnapshot.Child("coins").Value.ToString(), out coins);
                    }

                    if (scores.ContainsKey(gameName))
                    {
                        scores[gameName] += coins;
                    }
                    else
                    {
                        scores.Add(gameName, coins);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error getting student scores: {ex.Message}");
        }

        return scores;
    }
}