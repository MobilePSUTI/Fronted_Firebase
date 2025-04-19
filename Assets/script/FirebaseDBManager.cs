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
    private bool isInitialized = false;
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
                            // написать загрузку аватара при входе студента
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
    
    public async Task<bool> RegisterNewStudent(string email, string password, string firstName,
    string lastName, string secondName, string groupId, byte[] avatar)
    {
        if (!isInitialized) await Initialize();

        try
        {
            Debug.Log($"[Register] Attempting to register: {email}");

            // Проверка существования email
            if (await CheckEmailExists(email))
            {
                Debug.Log($"[Register] Email already exists: {email}");
                return false;
            }

            // Создаем ID для пользователя
            string userId = databaseRef.Child("users").Push().Key;

            // Подготовка данных пользователя для таблицы users
            var userData = new Dictionary<string, object>
        {
            {"id", userId},
            {"email", email.ToLower().Trim()},
            {"password", password.Trim()},
            {"first_name", firstName.Trim()},
            {"last_name", lastName.Trim()},
            {"second_name", string.IsNullOrWhiteSpace(secondName) ? "" : secondName.Trim()},
            {"group_id", groupId},
            {"created_at", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")}, // MySQL datetime format
            {"updated_at", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")},
            {"avatar_path", avatar != null ? $"avatars/{userId}.png" : ""},
            {"username", $"{firstName} {lastName}"},
            {"skill_id", "0"} // Добавляем skill_id по умолчанию
        };

            // Подготовка данных для таблицы users_roles
            var roleData = new Dictionary<string, object>
        {
            {"id", databaseRef.Child("users_roles").Push().Key},
            {"role_id", "1"}, // 1 = student
            {"student_id", userId},
        };

            // Создаем атомарный набор обновлений
            var updates = new Dictionary<string, object>
        {
            {$"14/data/{userId}", userData},
            {$"15/data/{roleData["id"]}", roleData}
        };

            // Выполняем атомарную запись
            await databaseRef.UpdateChildrenAsync(updates);

            Debug.Log($"[Register] Successfully registered user {userId}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Register] Error: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> CheckEmailExists(string email)
    {
        if (!isInitialized) await Initialize();

        try
        {
            var snapshot = await databaseRef.Child("users")
                .OrderByChild("email")
                .EqualTo(email.ToLower().Trim())
                .GetValueAsync();

            return snapshot.Exists;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[EmailCheck] Error: {ex.Message}");
            return false;
        }
    }

    public async Task<byte[]> GetUserAvatar(string userId)
    {
        if (!isInitialized) await Initialize();

        try
        {
            DataSnapshot snapshot = await databaseRef.Child("users")
                .Child(userId)
                .Child("avatar")
                .GetValueAsync();

            if (snapshot.Exists && snapshot.Value != null)
            {
                string base64Avatar = snapshot.Value.ToString();
                return Convert.FromBase64String(base64Avatar);
            }
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
}