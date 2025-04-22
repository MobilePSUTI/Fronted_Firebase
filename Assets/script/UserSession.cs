using UnityEngine;
using System.Collections.Generic;
using System;
using System.IO;

public static class UserSession
{
    private static User _currentUser;
    private static Student _selectedStudent;
    private static string _selectedGroupId;
    private static string _selectedGroupName;
    private static List<Group> _cachedGroups = new List<Group>();
    private static Dictionary<string, List<Student>> _cachedStudents = new Dictionary<string, List<Student>>();
    private static StudentProgressController.SkillsCache _cachedSkills;
    private static Texture2D _cachedAvatar;
    private static List<UserRatingData> _cachedRatingData = new List<UserRatingData>();

    [Serializable]
    public class UserRatingData
    {
        public User User { get; set; }
        public int TotalPoints { get; set; }
        public string GroupName { get; set; }
        public string Course { get; set; }
    }

    public static User CurrentUser
    {
        get => _currentUser;
        set { _currentUser = value; SaveSession(); }
    }

    public static Student SelectedStudent
    {
        get => _selectedStudent;
        set { _selectedStudent = value; SaveSession(); }
    }

    public static string SelectedGroupId
    {
        get => _selectedGroupId;
        set { _selectedGroupId = value; SaveSession(); }
    }

    public static string SelectedGroupName
    {
        get => _selectedGroupName;
        set { _selectedGroupName = value; SaveSession(); }
    }

    public static Texture2D CachedAvatar
    {
        get => _cachedAvatar;
        set { _cachedAvatar = value; SaveSession(); }
    }

    public static List<Group> CachedGroups
    {
        get => _cachedGroups;
        set { _cachedGroups = value ?? new List<Group>(); SaveSession(); }
    }

    public static Dictionary<string, List<Student>> CachedStudents
    {
        get => _cachedStudents;
        set { _cachedStudents = value ?? new Dictionary<string, List<Student>>(); SaveSession(); }
    }

    public static StudentProgressController.SkillsCache CachedSkills
    {
        get => _cachedSkills;
        set { _cachedSkills = value; SaveSession(); }
    }

    public static List<UserRatingData> CachedRatingData
    {
        get => _cachedRatingData;
        set { _cachedRatingData = value ?? new List<UserRatingData>(); SaveSession(); }
    }

    static UserSession()
    {
        LoadSession();
    }

    public static void SaveSession()
    {
        try
        {
            PlayerPrefs.SetString("UserSession_SelectedGroupId", _selectedGroupId ?? "");
            PlayerPrefs.SetString("UserSession_SelectedGroupName", _selectedGroupName ?? "");

            if (_currentUser != null)
                PlayerPrefs.SetString("UserSession_CurrentUser", JsonUtility.ToJson(_currentUser));

            if (_selectedStudent != null)
                PlayerPrefs.SetString("UserSession_SelectedStudent", JsonUtility.ToJson(_selectedStudent));

            if (_cachedSkills != null)
                PlayerPrefs.SetString("UserSession_CachedSkills", JsonUtility.ToJson(_cachedSkills));

            if (_cachedRatingData != null)
                PlayerPrefs.SetString("UserSession_CachedRatingData", JsonUtility.ToJson(new SerializableList<UserRatingData> { Items = _cachedRatingData }));

            if (_cachedAvatar != null)
            {
                string path = Path.Combine(Application.persistentDataPath, "avatar.png");
                File.WriteAllBytes(path, _cachedAvatar.EncodeToPNG());
                PlayerPrefs.SetString("UserSession_CachedAvatarPath", path);
            }

            PlayerPrefs.Save();
            Debug.Log("[UserSession] Session saved");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[UserSession] Save failed: {ex.Message}");
        }
    }

    private static void LoadSession()
    {
        try
        {
            _selectedGroupId = PlayerPrefs.GetString("UserSession_SelectedGroupId", null);
            _selectedGroupName = PlayerPrefs.GetString("UserSession_SelectedGroupName", null);

            if (PlayerPrefs.HasKey("UserSession_CurrentUser"))
                _currentUser = JsonUtility.FromJson<User>(PlayerPrefs.GetString("UserSession_CurrentUser"));

            if (PlayerPrefs.HasKey("UserSession_SelectedStudent"))
                _selectedStudent = JsonUtility.FromJson<Student>(PlayerPrefs.GetString("UserSession_SelectedStudent"));

            if (PlayerPrefs.HasKey("UserSession_CachedSkills"))
                _cachedSkills = JsonUtility.FromJson<StudentProgressController.SkillsCache>(PlayerPrefs.GetString("UserSession_CachedSkills"));

            if (PlayerPrefs.HasKey("UserSession_CachedRatingData"))
            {
                var serializableList = JsonUtility.FromJson<SerializableList<UserRatingData>>(PlayerPrefs.GetString("UserSession_CachedRatingData"));
                _cachedRatingData = serializableList?.Items ?? new List<UserRatingData>();
            }

            if (PlayerPrefs.HasKey("UserSession_CachedAvatarPath"))
            {
                string path = PlayerPrefs.GetString("UserSession_CachedAvatarPath");
                if (File.Exists(path))
                {
                    byte[] avatarData = File.ReadAllBytes(path);
                    Texture2D texture = new Texture2D(2, 2);
                    if (texture.LoadImage(avatarData))
                        _cachedAvatar = texture;
                }
            }

            Debug.Log("[UserSession] Session loaded");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[UserSession] Load failed: {ex.Message}");
        }
    }

    public static void ClearCache()
    {
        _cachedGroups.Clear();
        _cachedStudents.Clear();
        _cachedRatingData.Clear();
        SaveSession();
    }

    public static void ClearSession()
    {
        _currentUser = null;
        _selectedStudent = null;
        _selectedGroupId = null;
        _selectedGroupName = null;
        _cachedSkills = null;
        _cachedAvatar = null;
        _cachedGroups.Clear();
        _cachedStudents.Clear();
        _cachedRatingData.Clear();

        PlayerPrefs.DeleteAll();
        string avatarPath = Path.Combine(Application.persistentDataPath, "avatar.png");
        if (File.Exists(avatarPath))
            File.Delete(avatarPath);

        PlayerPrefs.Save();
        Debug.Log("[UserSession] Session cleared");
    }

    [Serializable]
    private class SerializableList<T>
    {
        public List<T> Items;
    }
}