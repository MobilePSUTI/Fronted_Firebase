using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class VKResponse
{
    public Response response;
}

[System.Serializable]
public class Response
{
    public List<Post> items;
    public List<VKGroup> groups;
}

[System.Serializable]
public class Post
{
    public string text;
    public long date;
    public List<Attachment> attachments;
    public long owner_id;
}

[System.Serializable]
public class Attachment
{
    public string type;
    public Photo photo;
}

[System.Serializable]
public class Photo
{
    public List<PhotoSize> sizes;
}

[System.Serializable]
public class PhotoSize
{
    public string type;
    public string url;
    public int width;
    public int height;
}

[System.Serializable]
public class VKGroup
{
    public long id;
    public string name;
    public string photo_200;
}

public class VKNewsLoad : MonoBehaviour
{
    // Embedded VK settings
    [SerializeField] private string accessToken = "2e1a194f2e1a194f2e1a194f0d2d3078ac22e1a2e1a194f49ac02415f6ad1570cce36f8"; // Set your VK API access token here
    [SerializeField] private List<int> groupIds = new List<int> { 78711199, 188328031, 17785357 };

    public List<Post> allPosts = new List<Post>();
    public Dictionary<long, VKGroup> groupDictionary = new Dictionary<long, VKGroup>();
    private const int MaxRetries = 3;
    private const float RetryDelayBase = 1f;

    private void OnValidate()
    {
        if (string.IsNullOrEmpty(accessToken))
            Debug.LogWarning("[VKNewsLoad] AccessToken is empty. Please set a valid VK API access token in the Inspector.");
        if (groupIds == null || groupIds.Count == 0)
            Debug.LogWarning("[VKNewsLoad] GroupIds list is empty. Please add valid VK group IDs in the Inspector.");
    }

    public IEnumerator GetNewsFromVK(int offset = 0, int count = 20)
    {
        if (string.IsNullOrEmpty(accessToken))
        {
            Debug.LogError("[VKNewsLoad] AccessToken is missing");
            yield break;
        }

        if (groupIds == null || groupIds.Count == 0)
        {
            Debug.LogError("[VKNewsLoad] No GroupIds specified");
            yield break;
        }

        allPosts.Clear();
        groupDictionary.Clear();

        foreach (int groupId in groupIds)
        {
            bool success = false;
            for (int attempt = 1; attempt <= MaxRetries; attempt++)
            {
                string url = $"https://api.vk.com/method/wall.get?owner_id=-{groupId}&access_token={accessToken}&v=5.199&count={count}&offset={offset}&extended=1";
                using UnityWebRequest request = UnityWebRequest.Get(url);
                request.timeout = 10;

                Debug.Log($"[VKNews] Fetching news for group {groupId}, attempt {attempt}");
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log($"[VKNews] Received response for group {groupId}");
                    ProcessNews(request.downloadHandler.text);
                    success = true;
                    break;
                }

                Debug.LogWarning($"[VKNews] Attempt {attempt} failed for group {groupId}: {request.error}");
                if (attempt < MaxRetries)
                    yield return new WaitForSeconds(RetryDelayBase * attempt);
            }

            if (!success)
                Debug.LogError($"[VKNews] Failed to fetch news for group {groupId} after {MaxRetries} attempts");
        }

        allPosts.RemoveAll(post => !IsValidDate(post.date));
        allPosts.Sort((a, b) => b.date.CompareTo(a.date));
        Debug.Log($"[VKNews] Loaded {allPosts.Count} posts from {groupDictionary.Count} groups");
    }

    private void ProcessNews(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            Debug.LogError("[VKNews] JSON response is empty");
            return;
        }

        try
        {
            var response = JsonUtility.FromJson<VKResponse>(json);
            if (response?.response?.items == null || response.response.groups == null)
            {
                Debug.LogError($"[VKNews] Invalid JSON response: {json}");
                return;
            }

            foreach (var post in response.response.items)
            {
                if (post == null || !IsValidDate(post.date)) continue;
                allPosts.Add(post);
            }

            foreach (var group in response.response.groups)
            {
                if (group != null && !groupDictionary.ContainsKey(group.id))
                    groupDictionary.Add(group.id, group);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[VKNews] Error processing news: {ex.Message}\nJSON: {json}");
        }
    }

    private bool IsValidDate(long unixSeconds)
    {
        try
        {
            DateTimeOffset.FromUnixTimeSeconds(unixSeconds);
            return true;
        }
        catch
        {
            return false;
        }
    }
}