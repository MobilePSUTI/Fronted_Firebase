using UnityEngine;
using System.Collections.Generic;

public static class NewsDataCache
{
    private static List<Post> _cachedPosts;
    private static Dictionary<long, VKGroup> _cachedVKGroups;

    public static List<Post> CachedPosts
    {
        get
        {
            if (_cachedPosts == null)
            {
                if (PlayerPrefs.HasKey("CachedPosts"))
                {
                    string json = PlayerPrefs.GetString("CachedPosts");
                    _cachedPosts = JsonUtility.FromJson<SerializableList<Post>>(json).Items;
                }
                else
                {
                    _cachedPosts = new List<Post>();
                }
            }
            return _cachedPosts;
        }
        set
        {
            _cachedPosts = value;
        }
    }

    public static Dictionary<long, VKGroup> CachedVKGroups
    {
        get
        {
            if (_cachedVKGroups == null)
            {
                if (PlayerPrefs.HasKey("CachedVKGroups"))
                {
                    string json = PlayerPrefs.GetString("CachedVKGroups");
                    _cachedVKGroups = JsonUtility.FromJson<SerializableDictionary<long, VKGroup>>(json).ToDictionary();
                }
                else
                {
                    _cachedVKGroups = new Dictionary<long, VKGroup>();
                }
            }
            return _cachedVKGroups;
        }
        set
        {
            _cachedVKGroups = value;
        }
    }

    public static void SaveCacheToPersistentStorage()
    {
        if (_cachedPosts != null)
        {
            string postsJson = JsonUtility.ToJson(new SerializableList<Post>(_cachedPosts));
            PlayerPrefs.SetString("CachedPosts", postsJson);
        }

        if (_cachedVKGroups != null)
        {
            string groupsJson = JsonUtility.ToJson(new SerializableDictionary<long, VKGroup>(_cachedVKGroups));
            PlayerPrefs.SetString("CachedVKGroups", groupsJson);
        }

        PlayerPrefs.Save();
    }

    public static void ClearCache()
    {
        _cachedPosts = null;
        _cachedVKGroups = null;
        PlayerPrefs.DeleteKey("CachedPosts");
        PlayerPrefs.DeleteKey("CachedVKGroups");
    }
}

[System.Serializable]
public class SerializableList<T>
{
    public List<T> Items;

    public SerializableList(List<T> items)
    {
        Items = items;
    }
}

[System.Serializable]
public class SerializableDictionary<TKey, TValue>
{
    public List<TKey> Keys;
    public List<TValue> Values;

    public SerializableDictionary(Dictionary<TKey, TValue> dictionary)
    {
        Keys = new List<TKey>(dictionary.Keys);
        Values = new List<TValue>(dictionary.Values);
    }

    public Dictionary<TKey, TValue> ToDictionary()
    {
        var dictionary = new Dictionary<TKey, TValue>();
        for (int i = 0; i < Keys.Count; i++)
        {
            dictionary.Add(Keys[i], Values[i]);
        }
        return dictionary;
    }
}