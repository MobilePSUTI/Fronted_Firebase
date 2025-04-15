using UnityEngine;
using System.Collections.Generic;

public static class UserSession
{
    private static User _currentUser;
    private static Student _selectedStudent;
    private static string _selectedGroupId;
    private static string _selectedGroupName;
    private static List<Group> _cachedGroups = new List<Group>();
    private static Dictionary<string, List<Student>> _cachedStudents = new Dictionary<string, List<Student>>();

    public static User CurrentUser
    {
        get => _currentUser;
        set
        {
            _currentUser = value;
            SaveSession();
        }
    }

    public static Student SelectedStudent
    {
        get => _selectedStudent;
        set
        {
            _selectedStudent = value;
            SaveSession();
        }
    }

    public static string SelectedGroupId
    {
        get => _selectedGroupId;
        set
        {
            _selectedGroupId = value;
            SaveSession();
        }
    }

    public static string SelectedGroupName
    {
        get => _selectedGroupName;
        set
        {
            _selectedGroupName = value;
            SaveSession();
        }
    }

    public static List<Group> CachedGroups
    {
        get => _cachedGroups ?? (_cachedGroups = new List<Group>());
        set
        {
            _cachedGroups = value ?? new List<Group>();
            SaveSession();
        }
    }

    public static Dictionary<string, List<Student>> CachedStudents
    {
        get => _cachedStudents ?? (_cachedStudents = new Dictionary<string, List<Student>>());
        set
        {
            _cachedStudents = value ?? new Dictionary<string, List<Student>>();
            SaveSession();
        }
    }

    static UserSession()
    {
        LoadSession();
    }

    private static void SaveSession()
    {
        PlayerPrefs.SetString("UserSession_SelectedGroupId", _selectedGroupId ?? "");
        PlayerPrefs.SetString("UserSession_SelectedGroupName", _selectedGroupName ?? "");

        if (_currentUser != null)
        {
            PlayerPrefs.SetString("UserSession_CurrentUser", JsonUtility.ToJson(_currentUser));
        }

        if (_selectedStudent != null)
        {
            PlayerPrefs.SetString("UserSession_SelectedStudent", JsonUtility.ToJson(_selectedStudent));
        }

        PlayerPrefs.Save();
    }

    private static void LoadSession()
    {
        _selectedGroupId = PlayerPrefs.GetString("UserSession_SelectedGroupId");
        if (string.IsNullOrEmpty(_selectedGroupId)) _selectedGroupId = null;

        _selectedGroupName = PlayerPrefs.GetString("UserSession_SelectedGroupName");
        if (string.IsNullOrEmpty(_selectedGroupName)) _selectedGroupName = null;

        if (PlayerPrefs.HasKey("UserSession_CurrentUser"))
        {
            _currentUser = JsonUtility.FromJson<User>(PlayerPrefs.GetString("UserSession_CurrentUser"));
        }

        if (PlayerPrefs.HasKey("UserSession_SelectedStudent"))
        {
            _selectedStudent = JsonUtility.FromJson<Student>(PlayerPrefs.GetString("UserSession_SelectedStudent"));
        }
    }

    public static void ClearCache()
    {
        _cachedGroups.Clear();
        _cachedStudents.Clear();

        // Очищаем только кэш, не трогая текущие выбранные элементы
        SaveSession();
    }

    public static void ClearSession()
    {
        _currentUser = null;
        _selectedStudent = null;
        _selectedGroupId = null;
        _selectedGroupName = null;
        _cachedGroups.Clear();
        _cachedStudents.Clear();

        PlayerPrefs.DeleteKey("UserSession_CurrentUser");
        PlayerPrefs.DeleteKey("UserSession_SelectedStudent");
        PlayerPrefs.DeleteKey("UserSession_SelectedGroupId");
        PlayerPrefs.DeleteKey("UserSession_SelectedGroupName");
        PlayerPrefs.Save();
    }
}