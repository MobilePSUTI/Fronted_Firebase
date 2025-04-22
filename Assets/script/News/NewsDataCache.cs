using UnityEngine;
using System.Collections.Generic;
using System;

public static class NewsDataCache
{
    private static List<Post> _cachedPosts = new List<Post>();
    private static Dictionary<long, VKGroup> _cachedVKGroups = new Dictionary<long, VKGroup>();

    public static List<Post> CachedPosts
    {
        get => _cachedPosts;
        set => _cachedPosts = value ?? new List<Post>();
    }

    public static Dictionary<long, VKGroup> CachedVKGroups
    {
        get => _cachedVKGroups;
        set => _cachedVKGroups = value ?? new Dictionary<long, VKGroup>();
    }

    public static void SaveCacheToPersistentStorage()
    {
        try
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
            Debug.Log("[NewsDataCache] Cache saved");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[NewsDataCache] Save failed: {ex.Message}");
        }
    }

    public static void ClearCache()
    {
        _cachedPosts.Clear();
        _cachedVKGroups.Clear();
        PlayerPrefs.DeleteKey("CachedPosts");
        PlayerPrefs.DeleteKey("CachedVKGroups");
        PlayerPrefs.Save();
        Debug.Log("[NewsDataCache] Cache cleared");
    }

    [Serializable]
    public class SerializableList<T>
    {
        public List<T> Items;
        public SerializableList(List<T> items) => Items = items;
    }

    [Serializable]
    public class SerializableDictionary<TKey, TValue>
    {
        public List<TKey> Keys;
        public List<TValue> Values;

        public SerializableDictionary(Dictionary<TKey, TValue> dictionary)
        {
            Keys = new List<TKey>();
            Values = new List<TValue>();
            foreach (var kvp in dictionary)
            {
                Keys.Add(kvp.Key);
                Values.Add(kvp.Value);
            }
        }

        public Dictionary<TKey, TValue> ToDictionary()
        {
            var dictionary = new Dictionary<TKey, TValue>();
            for (int i = 0; i < Mathf.Min(Keys.Count, Values.Count); i++)
                dictionary[Keys[i]] = Values[i];
            return dictionary;
        }
    }
}