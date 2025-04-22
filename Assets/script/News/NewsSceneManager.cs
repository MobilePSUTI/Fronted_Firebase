using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;

public class NewsSceneManager : MonoBehaviour
{
    [SerializeField] private Transform newsContainer;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private GameObject loadingIndicator;

    private const string NewsItemWithPhotoPrefabPath = "Panel_news";
    private const string NewsItemWithoutPhotoPrefabPath = "Panel_news_netPhoto";
    private const string NewsItemPhotoOnlyPrefabPath = "Panel_newPhoto";
    private const int MaxImageLoadRetries = 3;
    private const float ImageRetryDelayBase = 1f;

    private DateTime lastUpdateTime;
    private bool isDragging;
    private bool isUpdating;
    private GameObject newsItemWithPhotoPrefab;
    private GameObject newsItemWithoutPhotoPrefab;
    private GameObject newsItemPhotoOnlyPrefab;
    private Queue<GameObject> newsItemPool = new Queue<GameObject>();
    private Vector2 touchPosition;

    private void Start()
    {
        if (!ValidateComponents()) return;

        lastUpdateTime = DateTime.Now;
        newsItemWithPhotoPrefab = Resources.Load<GameObject>(NewsItemWithPhotoPrefabPath);
        newsItemWithoutPhotoPrefab = Resources.Load<GameObject>(NewsItemWithoutPhotoPrefabPath);
        newsItemPhotoOnlyPrefab = Resources.Load<GameObject>(NewsItemPhotoOnlyPrefabPath);

        if (!ValidatePrefabs()) return;

        loadingIndicator.SetActive(true);
        if (NewsDataCache.CachedPosts.Count > 0 && NewsDataCache.CachedVKGroups.Count > 0)
            StartCoroutine(DisplayCachedNews());

        StartCoroutine(LoadNewsAndDisplay());
    }

    private bool ValidateComponents()
    {
        if (newsContainer == null || scrollRect == null || loadingIndicator == null)
        {
            Debug.LogError("[NewsScene] Missing UI components: newsContainer, scrollRect, or loadingIndicator");
            return false;
        }
        return true;
    }

    private bool ValidatePrefabs()
    {
        if (newsItemWithPhotoPrefab == null || newsItemWithoutPhotoPrefab == null || newsItemPhotoOnlyPrefab == null)
        {
            Debug.LogError("[NewsScene] News prefabs not found in Resources folder");
            return false;
        }
        return true;
    }

    private void Update()
    {
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            isDragging = true;
            touchPosition = Touchscreen.current.primaryTouch.position.ReadValue();
        }
        else
        {
            isDragging = false;
        }

        if (isDragging && scrollRect.velocity.y > 0 && scrollRect.verticalNormalizedPosition >= 0.99f)
            StartCoroutine(CheckAndUpdateNews());
    }

    private IEnumerator LoadNewsAndDisplay()
    {
        var vkNewsLoad = gameObject.AddComponent<VKNewsLoad>();
        // No need to assign vkSettings; configure accessToken and groupIds in Inspector
        yield return vkNewsLoad.GetNewsFromVK(0, 20);

        if (vkNewsLoad.allPosts != null && vkNewsLoad.groupDictionary != null)
        {
            NewsDataCache.CachedPosts = vkNewsLoad.allPosts;
            NewsDataCache.CachedVKGroups = vkNewsLoad.groupDictionary;
            NewsDataCache.SaveCacheToPersistentStorage();
            yield return StartCoroutine(DisplayNews(vkNewsLoad.allPosts, vkNewsLoad.groupDictionary));
        }
        else
        {
            Debug.LogError("[NewsScene] Failed to load news from VK");
        }

        loadingIndicator.SetActive(false);
        if (vkNewsLoad != null)
            Destroy(vkNewsLoad);
    }

    private IEnumerator DisplayCachedNews()
    {
        yield return StartCoroutine(DisplayNews(NewsDataCache.CachedPosts, NewsDataCache.CachedVKGroups));
        loadingIndicator.SetActive(false);
    }

    private IEnumerator DisplayNews(List<Post> posts, Dictionary<long, VKGroup> groups)
    {
        if (posts == null || groups == null)
        {
            Debug.LogError("[NewsScene] Posts or groups dictionary is null");
            yield break;
        }

        foreach (Transform child in newsContainer)
        {
            child.gameObject.SetActive(false);
            newsItemPool.Enqueue(child.gameObject);
        }

        foreach (var post in posts)
        {
            if (post == null)
            {
                Debug.LogWarning("[NewsScene] Skipping null post");
                continue;
            }

            bool hasText = !string.IsNullOrEmpty(post.text);
            bool hasImage = post.attachments != null && post.attachments.Exists(a => a?.type == "photo");

            if (!hasText && !hasImage)
            {
                Debug.LogWarning($"[NewsScene] Skipping post with owner_id {post.owner_id}: no text or image");
                continue;
            }

            var group = groups.ContainsKey(-post.owner_id) ? groups[-post.owner_id] : null;
            if (group == null)
            {
                Debug.LogWarning($"[NewsScene] Group for post with owner_id {post.owner_id} not found");
                continue;
            }

            GameObject newsItemPrefab = hasText && hasImage ? newsItemWithPhotoPrefab :
                                       hasText ? newsItemWithoutPhotoPrefab :
                                       newsItemPhotoOnlyPrefab;

            GameObject newsItem = GetNewsItemFromPool(newsItemPrefab);
            yield return StartCoroutine(SetupNewsItemAsync(newsItem, post, group, hasText, hasImage));
        }
    }

    private IEnumerator CheckAndUpdateNews()
    {
        if (isUpdating) yield break;
        isUpdating = true;

        var vkNewsLoad = gameObject.AddComponent<VKNewsLoad>();
        // No need to assign vkSettings; configure accessToken and groupIds in Inspector
        yield return vkNewsLoad.GetNewsFromVK(0, 20);

        if (vkNewsLoad.allPosts != null && vkNewsLoad.groupDictionary != null)
        {
            lastUpdateTime = DateTime.Now;
            NewsDataCache.CachedPosts = vkNewsLoad.allPosts;
            NewsDataCache.CachedVKGroups = vkNewsLoad.groupDictionary;
            NewsDataCache.SaveCacheToPersistentStorage();
            yield return StartCoroutine(DisplayNews(vkNewsLoad.allPosts, vkNewsLoad.groupDictionary));
        }
        else
        {
            Debug.LogError("[NewsScene] Failed to update news from VK");
        }

        if (vkNewsLoad != null)
            Destroy(vkNewsLoad);
        isUpdating = false;
    }

    private IEnumerator SetupNewsItemAsync(GameObject newsItem, Post post, VKGroup group, bool hasText, bool hasImage)
    {
        if (newsItem == null || post == null || group == null)
        {
            Debug.LogError("[NewsScene] Invalid news item, post, or group");
            yield break;
        }

        // Initialize components
        var nameGroup = newsItem.transform.Find("foto_group/Name_group")?.GetComponent<TMP_Text>();
        var dateTimeText = newsItem.transform.Find("DateTimeText")?.GetComponent<TMP_Text>();
        var fotoGroup = newsItem.transform.Find("foto_group")?.GetComponent<RawImage>();
        var foto = newsItem.transform.Find("Foto")?.GetComponent<RawImage>();

        // Set group name
        if (nameGroup != null)
        {
            nameGroup.text = group.name ?? "Unknown Group";
            nameGroup.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogWarning($"[NewsScene] Name_group TMP_Text not found in news item for post with owner_id {post.owner_id}");
        }

        // Load group photo
        if (fotoGroup != null && !string.IsNullOrEmpty(group.photo_200))
        {
            yield return StartCoroutine(LoadImageWithRetry(group.photo_200, fotoGroup));
        }

        // Handle date
        string dateString = "Unknown Date";
        if (IsValidUnixTimestamp(post.date))
        {
            try
            {
                DateTime dateTime = DateTimeOffset.FromUnixTimeSeconds(post.date).LocalDateTime;
                dateString = dateTime.ToString("dd.MM.yyyy HH:mm");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[NewsScene] Failed to parse date {post.date} for post with owner_id {post.owner_id}: {ex.Message}");
            }
        }
        else
        {
            Debug.LogWarning($"[NewsScene] Invalid Unix timestamp {post.date} for post with owner_id {post.owner_id}");
        }

        if (dateTimeText != null)
        {
            dateTimeText.text = dateString;
            dateTimeText.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogWarning($"[NewsScene] DateTimeText TMP_Text not found in news item for post with owner_id {post.owner_id}");
        }

        // Load post image
        if (hasImage && foto != null)
        {
            string photoUrl = GetPhotoUrl(post);
            if (!string.IsNullOrEmpty(photoUrl))
            {
                yield return StartCoroutine(LoadImageWithRetry(photoUrl, foto));
            }
            else
            {
                Debug.LogWarning($"[NewsScene] No valid photo URL found for post with owner_id {post.owner_id}");
            }
        }

        // Set post text
        if (hasText)
        {
            var expandableText = newsItem.GetComponent<ExpandableNewsText>();
            if (expandableText == null)
            {
                expandableText = newsItem.AddComponent<ExpandableNewsText>();
                expandableText.textShort = newsItem.transform.Find("Image/Text_Short")?.GetComponent<TMP_Text>()
                                         ?? newsItem.transform.Find("back/Text_Short")?.GetComponent<TMP_Text>();
                expandableText.textFull = newsItem.transform.Find("Image/Text_Full")?.GetComponent<TMP_Text>()
                                        ?? newsItem.transform.Find("back/Text_Full")?.GetComponent<TMP_Text>();
                expandableText.showMoreButton = newsItem.transform.Find("Image/Button_ShowMore")?.GetComponent<Button>()
                                              ?? newsItem.transform.Find("back/Button_ShowMore")?.GetComponent<Button>();
            }

            if (expandableText.textShort != null && expandableText.textFull != null && expandableText.showMoreButton != null)
            {
                expandableText.Initialize(post.text);
            }
            else
            {
                Debug.LogWarning($"[NewsScene] ExpandableNewsText components missing for post with owner_id {post.owner_id}");
            }
        }
    }

    private bool IsValidUnixTimestamp(long unixSeconds)
    {
        return unixSeconds > 0 && unixSeconds < 253402300800; // Valid range: 1970 to 9999
    }

    private string GetPhotoUrl(Post post)
    {
        if (post.attachments == null || post.attachments.Count == 0)
            return null;

        var photoAttachment = post.attachments.Find(a => a?.type == "photo");
        if (photoAttachment?.photo == null || photoAttachment.photo.sizes == null)
            return null;

        var photoSize = photoAttachment.photo.sizes.Find(s => s?.type == "x");
        return photoSize?.url;
    }

    private IEnumerator LoadImageWithRetry(string url, RawImage targetImage)
    {
        if (string.IsNullOrEmpty(url) || targetImage == null)
        {
            Debug.LogWarning($"[NewsScene] Invalid URL or targetImage for image load: {url}");
            yield break;
        }

        for (int attempt = 1; attempt <= MaxImageLoadRetries; attempt++)
        {
            using UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
            request.timeout = 10;
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                targetImage.texture = ((DownloadHandlerTexture)request.downloadHandler).texture;
                targetImage.gameObject.SetActive(true);
                yield break;
            }

            Debug.LogWarning($"[NewsScene] Image load attempt {attempt} failed for {url}: {request.error}");
            if (attempt < MaxImageLoadRetries)
                yield return new WaitForSeconds(ImageRetryDelayBase * attempt);
        }
        Debug.LogError($"[NewsScene] Failed to load image after {MaxImageLoadRetries} attempts: {url}");
    }

    private GameObject GetNewsItemFromPool(GameObject prefab)
    {
        foreach (var item in newsItemPool)
        {
            if (!item.activeInHierarchy && item.name == prefab.name + "(Clone)")
            {
                item.SetActive(true);
                return item;
            }
        }

        GameObject newsItem = Instantiate(prefab, newsContainer);
        newsItemPool.Enqueue(newsItem);
        return newsItem;
    }
}