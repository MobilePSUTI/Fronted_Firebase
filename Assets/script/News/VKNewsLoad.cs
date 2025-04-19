using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System;

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
    public string screen_name;
    public int is_closed;
    public string type;
    public string photo_50;
    public string photo_100;
    public string photo_200;
}

public class VKNewsLoad : MonoBehaviour
{
    public string accessToken = "2e1a194f2e1a194f2e1a194f0d2d3078ac22e1a2e1a194f49ac02415f6ad1570cce36f8";
    public List<int> groupIds = new List<int> { 78711199, 188328031, 17785357 };

    public List<Post> allPosts = new List<Post>();
    public Dictionary<long, VKGroup> groupDictionary = new Dictionary<long, VKGroup>();

    public IEnumerator GetNewsFromVK(int offset = 0, int count = 100)
    {
        allPosts.Clear();
        groupDictionary.Clear();

        foreach (var groupId in groupIds)
        {
            string url = $"https://api.vk.com/method/wall.get?owner_id=-{groupId}&access_token={accessToken}&v=5.199&count={count}&offset={offset}&extended=1";
            UnityWebRequest request = UnityWebRequest.Get(url);
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Ошибка: " + request.error);
            }
            else
            {
                ProcessNews(request.downloadHandler.text);
            }
        }

        // Сортируем только посты с валидными датами
        allPosts.RemoveAll(post =>
        {
            try
            {
                new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(post.date);
                return false;
            }
            catch
            {
                return true;
            }
        });

        allPosts.Sort((post1, post2) => post2.date.CompareTo(post1.date));
    }

    public void ProcessNews(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            Debug.LogError("JSON ответ пуст или равен null.");
            return;
        }

        try
        {
            var response = JsonUtility.FromJson<VKResponse>(json);

            if (response?.response?.items == null || response.response.groups == null)
            {
                Debug.LogError("Неверный JSON ответ или пустые данные.");
                return;
            }

            // Добавляем только посты с валидными датами
            foreach (var post in response.response.items)
            {
                try
                {
                    // Проверяем валидность даты
                    var testDate = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)
                        .AddSeconds(post.date);
                    allPosts.Add(post);
                }
                catch (ArgumentOutOfRangeException)
                {
                    Debug.LogWarning($"Пост с невалидной датой пропущен: {post.date}");
                }
            }

            foreach (var group in response.response.groups)
            {
                if (!groupDictionary.ContainsKey(group.id))
                {
                    groupDictionary.Add(group.id, group);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Ошибка обработки новостей: {e}");
        }
    }
}