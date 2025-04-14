using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.Networking;

public class NewsSceneManager : MonoBehaviour
{
    public Transform newsContainer; // ��������� ��� ��������
    public ScrollRect scrollRect; // ScrollRect ��� ���������
    public GameObject loadingIndicator; // ��������� ��������

    private const string NewsItemWithPhotoPrefabPath = "Panel_news";
    private const string NewsItemWithoutPhotoPrefabPath = "Panel_news_netPhoto";

    private DateTime lastUpdateTime; // ����� ���������� ����������
    private const float updateInterval = 60f; // �������� �������� ���������� (� ��������)

    private bool isDragging = false; // ���� ��� ������������, ����� �� ������������ ������
    private bool isUpdating = false; // ���� ��� �������������� ������������� ����������

    private GameObject newsItemWithPhotoPrefab; // ������ ������� � ����
    private GameObject newsItemWithoutPhotoPrefab; // ������ ������� ��� ����

    private Queue<GameObject> newsItemPool = new Queue<GameObject>(); // ��� �������� ��� ��������
    private bool isInitialLoadComplete = false;

    void Start()
    {
        // ��������� �������
        newsItemWithPhotoPrefab = Resources.Load<GameObject>(NewsItemWithPhotoPrefabPath);
        newsItemWithoutPhotoPrefab = Resources.Load<GameObject>(NewsItemWithoutPhotoPrefabPath);

        if (newsItemWithPhotoPrefab == null || newsItemWithoutPhotoPrefab == null)
        {
            Debug.LogError("������� �� ������� � ����� Resources.");
            return;
        }

        // ���������� ��������� ��������
        if (loadingIndicator != null)
            loadingIndicator.SetActive(true);
        lastUpdateTime = DateTime.Now;
        // �������� �������� ��������
        StartCoroutine(LoadNewsAndDisplay());
    }
    IEnumerator LoadNewsAndDisplay()
    {
        // ��������� �������
        var vkNewsLoad = gameObject.AddComponent<VKNewsLoad>();
        yield return StartCoroutine(vkNewsLoad.GetNewsFromVK(0, 100));

        // ��������� ���������
        if (vkNewsLoad.allPosts != null && vkNewsLoad.groupDictionary != null)
        {
            // ��������� � ���
            NewsDataCache.CachedPosts = vkNewsLoad.allPosts;
            NewsDataCache.CachedVKGroups = vkNewsLoad.groupDictionary;

            // ���������� �������
            yield return StartCoroutine(DisplayNews(vkNewsLoad.allPosts, vkNewsLoad.groupDictionary));
        }
        else
        {
            Debug.LogError("�� ������� ��������� �������");
            // ����� �������� ��������� ������� �����
        }

        // �������� ��������� ��������
        if (loadingIndicator != null)
            loadingIndicator.SetActive(false);

        Destroy(vkNewsLoad);
        isInitialLoadComplete = true;
    }
    IEnumerator DisplayNews(List<Post> posts, Dictionary<long, VKGroup> groups)
    {
        // ������� ������ �������
        foreach (Transform child in newsContainer)
        {
            child.gameObject.SetActive(false);
        }

        // ���������� ����� �������
        foreach (var post in posts)
        {
            bool hasText = !string.IsNullOrEmpty(post.text);
            bool hasImage = post.attachments != null && post.attachments.Any(a => a.type == "photo");

            if (!hasText && !hasImage) continue;

            var group = groups.ContainsKey(-post.owner_id) ? groups[-post.owner_id] : null;
            if (group == null) continue;

            GameObject newsItem = GetNewsItemFromPool(hasImage);
            SetupNewsItem(newsItem, post, group, hasText, hasImage);

            yield return null;
        }
    }
    void Update()
    {
        // ���������, ����� �� ������������ ������
        if (scrollRect != null)
        {
            if (Input.GetMouseButton(0)) // ���������, ������ �� ����� ������ ����
            {
                isDragging = true;
            }
            else
            {
                isDragging = false;
            }

            // ���������, ������� �� ������������ �����
            if (isDragging && scrollRect.velocity.y > 0 && scrollRect.verticalNormalizedPosition >= 0.99f)
            {
                // ��������� ���������� ��������
                StartCoroutine(CheckAndUpdateNews());
            }
        }
    }

    IEnumerator CheckForUpdates()
    {
        while (true)
        {
            yield return new WaitForSeconds(updateInterval); // ���� �������� ��������

            // ��������� ������� ����� ��������
            yield return StartCoroutine(CheckAndUpdateNews());
        }
    }

    IEnumerator CheckAndUpdateNews()
    {
        if (isUpdating) yield break;
        isUpdating = true;

        // ������ �� ������������ ����
        if (lastUpdateTime.Year < 1 || lastUpdateTime.Year > 9999)
        {
            lastUpdateTime = DateTime.Now;
        }

        try
        {
            long lastUpdateTimestamp = new DateTimeOffset(lastUpdateTime).ToUnixTimeSeconds();

            var vkNewsLoad = gameObject.AddComponent<VKNewsLoad>();
            yield return StartCoroutine(vkNewsLoad.GetNewsFromVK());

            var newPosts = vkNewsLoad.allPosts
                .Where(p => p.date > lastUpdateTimestamp)
                .ToList();

            if (newPosts.Count > 0)
            {
                lastUpdateTime = DateTime.Now;
                NewsDataCache.CachedPosts.InsertRange(0, newPosts);
                yield return StartCoroutine(AddNewPostsToUI(newPosts));
            }

            Destroy(vkNewsLoad);
        }
        finally
        {
            isUpdating = false;
        }
    }

    IEnumerator AddNewPostsToUI(List<Post> newPosts)
    {
        // ��������� ����� ������� � ������ ����������
        foreach (var post in newPosts)
        {
            // ���������, ���� �� ����� ��� �����������
            bool hasText = !string.IsNullOrEmpty(post.text);
            bool hasImage = post.attachments != null && post.attachments.Any(a => a.type == "photo");

            // ���� ������� ������ (��� ������ � �����������), ���������� �
            if (!hasText && !hasImage)
            {
                Debug.LogWarning($"���� � ID = {post.owner_id} ��������: ��� ������ � �����������.");
                continue;
            }

            var group = NewsDataCache.CachedVKGroups.ContainsKey(-post.owner_id) ? NewsDataCache.CachedVKGroups[-post.owner_id] : null;
            if (group == null)
            {
                Debug.LogWarning($"���������� � ID = {Math.Abs(post.owner_id)} �� �������.");
                continue;
            }

            // ���������� ������ �� ���� ��� ������� �����
            GameObject newsItem = GetNewsItemFromPool(hasImage);
            newsItem.transform.SetAsFirstSibling();

            SetupNewsItem(newsItem, post, group, hasText, hasImage);

            // ������ �����, ����� �� ����������� UI
            yield return null;
        }

        // ������������ ������ �����
        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 1; // ��������� �����
        }
    }

    IEnumerator DisplayCachedNews()
    {
        // ���������, ���� �� ������ � ����
        if (NewsDataCache.CachedPosts == null || NewsDataCache.CachedVKGroups == null)
        {
            Debug.LogError("��� ������ � ����.");
            yield break;
        }

        // ���������� �������
        foreach (var post in NewsDataCache.CachedPosts)
        {
            // ���������, ���� �� ����� ��� �����������
            bool hasText = !string.IsNullOrEmpty(post.text);
            bool hasImage = post.attachments != null && post.attachments.Any(a => a.type == "photo");

            // ���� ������� ������ (��� ������ � �����������), ���������� �
            if (!hasText && !hasImage)
            {
                Debug.LogWarning($"���� � ID = {post.owner_id} ��������: ��� ������ � �����������.");
                continue;
            }

            var group = NewsDataCache.CachedVKGroups.ContainsKey(-post.owner_id) ? NewsDataCache.CachedVKGroups[-post.owner_id] : null;
            if (group == null)
            {
                Debug.LogWarning($"���������� � ID = {Math.Abs(post.owner_id)} �� �������.");
                continue;
            }

            // ���������� ������ �� ���� ��� ������� �����
            GameObject newsItem = GetNewsItemFromPool(hasImage);

            SetupNewsItem(newsItem, post, group, hasText, hasImage);

            // ������ �����, ����� �� ����������� UI
            yield return null;
        }

        // �������� ��������� ��������
        if (loadingIndicator != null)
            loadingIndicator.SetActive(false);
    }

    private GameObject GetNewsItemFromPool(bool hasImage)
    {
        GameObject newsItemPrefab = hasImage ? newsItemWithPhotoPrefab : newsItemWithoutPhotoPrefab;

        // ���� ������ � ����
        foreach (var item in newsItemPool)
        {
            if (item.activeInHierarchy == false && item.name == newsItemPrefab.name)
            {
                // ���������� ������������� �����������
                var foto = item.transform.Find("Foto")?.GetComponent<RawImage>();
                if (foto != null)
                {
                    foto.GetComponent<RectTransform>().sizeDelta = new Vector2(newsItemPrefab.GetComponent<RectTransform>().rect.width, 0);
                }

                item.SetActive(true);
                return item;
            }
        }

        // ���� ����������� ������� � ���� ���, ������� �����
        GameObject newsItem = Instantiate(newsItemPrefab, newsContainer);
        newsItemPool.Enqueue(newsItem);
        return newsItem;
    }

    void SetupNewsItem(GameObject newsItem, Post post, VKGroup group, bool hasText, bool hasImage)
    {
        // ������������ ����������
        var nameGroup = newsItem.transform.Find("Name_group")?.GetComponent<Text>();
        var dateTimeText = newsItem.transform.Find("DateTimeText")?.GetComponent<Text>();
        var fotoGroup = newsItem.transform.Find("foto_group")?.GetComponent<RawImage>();
        var foto = newsItem.transform.Find("Foto")?.GetComponent<RawImage>();

        // ������������� ����������� �����
        nameGroup.text = group.name;
        StartCoroutine(LoadGroupImage(group.photo_200, fotoGroup));

        System.DateTime dateTime = new System.DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
        dateTime = dateTime.AddSeconds(post.date).ToLocalTime();
        dateTimeText.text = dateTime.ToString("dd.MM.yyyy HH:mm");

        // ��������� �����������
        if (hasImage && foto != null)
        {
            var photoSize = post.attachments.First(a => a.type == "photo").photo.sizes.FirstOrDefault(s => s.type == "x");
            if (photoSize != null && !string.IsNullOrEmpty(photoSize.url))
            {
                StartCoroutine(LoadAndScaleImage(photoSize.url, foto, photoSize.width, photoSize.height));
            }
        }

        // ������������� ������������ ������
        var expandableText = newsItem.GetComponent<ExpandableNewsText>();
        if (expandableText == null)
        {
            expandableText = newsItem.AddComponent<ExpandableNewsText>();
            expandableText.textShort = newsItem.transform.Find("Text_Short")?.GetComponent<Text>();
            expandableText.textFull = newsItem.transform.Find("Text_Full")?.GetComponent<Text>();
            expandableText.showMoreButton = newsItem.transform.Find("Button_ShowMore")?.GetComponent<Button>();
        }

        expandableText.Initialize(hasText ? post.text : "");
    }

    IEnumerator LoadAndScaleImage(string url, RawImage targetImage, int originalWidth, int originalHeight)
    {
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("������ ��������: " + request.error);
            yield break;
        }

        // �������� ��������
        Texture2D texture = ((DownloadHandlerTexture)request.downloadHandler).texture;
        targetImage.texture = texture;

        // ��������� ��������
        RectTransform imageRect = targetImage.GetComponent<RectTransform>();
        float containerWidth = imageRect.parent.GetComponent<RectTransform>().rect.width;
        float aspectRatio = (float)originalWidth / originalHeight;

        // ������������ ������ � ������ ���������
        float targetHeight = containerWidth / aspectRatio;

        // ������������ ������������ ������ (��������, 600px ��� ������ ����������)
        float maxHeight = 850f; // �������� �� ������ ��������
        targetHeight = Mathf.Min(targetHeight, maxHeight);

        // ������������� ������
        imageRect.sizeDelta = new Vector2(containerWidth, targetHeight);

        // ������������� ��������� �����
        LayoutRebuilder.ForceRebuildLayoutImmediate(imageRect.parent.GetComponent<RectTransform>());
    }

    IEnumerator LoadGroupImage(string url, RawImage targetImage)
    {
        yield return LoadImage(url, targetImage);
    }

    IEnumerator LoadImage(string url, RawImage targetImage)
    {
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("������: " + request.error);
        }
        else
        {
            targetImage.texture = ((DownloadHandlerTexture)request.downloadHandler).texture;
        }
    }
}