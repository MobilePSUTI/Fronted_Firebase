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
    private const string NewsItemPhotoOnlyPrefabPath = "Panel_newPhoto";

    private DateTime lastUpdateTime; // ����� ���������� ����������
    private const float updateInterval = 60f; // �������� �������� ���������� (� ��������)

    private bool isDragging = false; // ���� ��� ������������, ����� �� ������������ ������
    private bool isUpdating = false; // ���� ��� �������������� ������������� ����������

    private GameObject newsItemWithPhotoPrefab; // ������ ������� � ���� � �������
    private GameObject newsItemWithoutPhotoPrefab; // ������ ������� ������ � �������
    private GameObject newsItemPhotoOnlyPrefab; // ������ ������� ������ � ����

    private Queue<GameObject> newsItemPool = new Queue<GameObject>(); // ��� �������� ��� ��������
    private bool isInitialLoadComplete = false;

    void Start()
    {
        lastUpdateTime = DateTime.Now;
        newsItemWithPhotoPrefab = Resources.Load<GameObject>(NewsItemWithPhotoPrefabPath);
        newsItemWithoutPhotoPrefab = Resources.Load<GameObject>(NewsItemWithoutPhotoPrefabPath);
        newsItemPhotoOnlyPrefab = Resources.Load<GameObject>(NewsItemPhotoOnlyPrefabPath);

        if (loadingIndicator != null)
            loadingIndicator.SetActive(true);

        // ������ ������� ���������� ������������ ������, ���� ��� ����
        if (NewsDataCache.CachedPosts != null && NewsDataCache.CachedPosts.Count > 0 &&
        NewsDataCache.CachedVKGroups != null && NewsDataCache.CachedVKGroups.Count > 0)
        {
            StartCoroutine(DisplayCachedNews());
        }

        // ����� ��������� ������ ������
        StartCoroutine(LoadNewsAndDisplay());
    }

    IEnumerator LoadNewsAndDisplay()
    {
        var vkNewsLoad = gameObject.AddComponent<VKNewsLoad>();
        yield return StartCoroutine(vkNewsLoad.GetNewsFromVK(0, 100));

        if (vkNewsLoad.allPosts != null && vkNewsLoad.groupDictionary != null)
        {
            NewsDataCache.CachedPosts = vkNewsLoad.allPosts;
            NewsDataCache.CachedVKGroups = vkNewsLoad.groupDictionary;
            NewsDataCache.SaveCacheToPersistentStorage();

            yield return StartCoroutine(DisplayNews(vkNewsLoad.allPosts, vkNewsLoad.groupDictionary));
        }
        else
        {
            Debug.LogError("�� ������� ��������� �������: allPosts ��� groupDictionary ����� null.");
        }

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
            if (group == null)
            {
                Debug.LogWarning($"������ ��� ����� � owner_id = {post.owner_id} �� �������.");
                continue;
            }

            // ���������� ����� ������ ������������
            GameObject newsItemPrefab;
            if (hasText && hasImage)
            {
                newsItemPrefab = newsItemWithPhotoPrefab;
            }
            else if (hasText)
            {
                newsItemPrefab = newsItemWithoutPhotoPrefab;
            }
            else // ������ ����
            {
                newsItemPrefab = newsItemPhotoOnlyPrefab;
            }

            GameObject newsItem = GetNewsItemFromPool(newsItemPrefab);
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

        try
        {
            // ������������� ������� ���������� ����������
            if (lastUpdateTime.Year < 1970 || lastUpdateTime.Year > 9999)
            {
                lastUpdateTime = DateTime.Now;
            }

            long lastUpdateTimestamp;
            try
            {
                lastUpdateTimestamp = new DateTimeOffset(lastUpdateTime).ToUnixTimeSeconds();
            }
            catch (ArgumentOutOfRangeException)
            {
                // ���� ����� ���������, ���������� ������� �����
                lastUpdateTime = DateTime.Now;
                lastUpdateTimestamp = new DateTimeOffset(lastUpdateTime).ToUnixTimeSeconds();
            }

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

    IEnumerator LoadNewsCoroutine(VKNewsLoad vkNewsLoad, System.Action<bool, List<Post>> callback)
    {
        bool success = false;
        List<Post> filteredPosts = null;
        Exception error = null;

        // ��������� ��������
        IEnumerator internalLoad = vkNewsLoad.GetNewsFromVK();
        while (internalLoad.MoveNext())
        {
            yield return internalLoad.Current;
        }

        // ��������� ���������
        if (vkNewsLoad.allPosts != null)
        {
            try
            {
                // ��������� �����
                long lastUpdateTimestamp = new DateTimeOffset(lastUpdateTime).ToUnixTimeSeconds();
                filteredPosts = vkNewsLoad.allPosts
                .Where(p => p.date > lastUpdateTimestamp)
                .ToList();
                success = true;
            }
            catch (Exception ex)
            {
                error = ex;
                Debug.LogError($"������ ���������� ������: {ex.Message}");
            }
        }
        else
        {
            Debug.LogError("�� ������� ��������� �������");
        }

        callback?.Invoke(success, filteredPosts);
    }

    IEnumerator AddNewPostsToUICoroutine(List<Post> newPosts)
    {
        foreach (var post in newPosts)
        {
            bool hasText = !string.IsNullOrEmpty(post.text);
            bool hasImage = post.attachments != null && post.attachments.Any(a => a.type == "photo");

            if (!hasText && !hasImage) continue;

            var group = NewsDataCache.CachedVKGroups.ContainsKey(-post.owner_id) ?
            NewsDataCache.CachedVKGroups[-post.owner_id] : null;
            if (group == null) continue;

            GameObject newsItemPrefab = hasText && hasImage ? newsItemWithPhotoPrefab :
            hasText ? newsItemWithoutPhotoPrefab :
            newsItemPhotoOnlyPrefab;

            GameObject newsItem = GetNewsItemFromPool(newsItemPrefab);
            newsItem.transform.SetAsFirstSibling();

            // ��������� �������� � ���������� ���������� ������
            SetupNewsItemSafe(newsItem, post, group, hasText, hasImage);

            yield return null;
        }

        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 1;
        }
    }

    void SetupNewsItemSafe(GameObject newsItem, Post post, VKGroup group, bool hasText, bool hasImage)
    {
        try
        {
            SetupNewsItem(newsItem, post, group, hasText, hasImage);
        }
        catch (Exception ex)
        {
            Debug.LogError($"������ ��������� ��������: {ex.Message}");
        }
    }

    IEnumerator AddNewPostsToUI(List<Post> newPosts)
    {
        foreach (var post in newPosts)
        {
            bool hasText = !string.IsNullOrEmpty(post.text);
            bool hasImage = post.attachments != null && post.attachments.Any(a => a.type == "photo");

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

            GameObject newsItemPrefab;
            if (hasText && hasImage)
            {
                newsItemPrefab = newsItemWithPhotoPrefab;
            }
            else if (hasText)
            {
                newsItemPrefab = newsItemWithoutPhotoPrefab;
            }
            else
            {
                newsItemPrefab = newsItemPhotoOnlyPrefab;
            }

            GameObject newsItem = GetNewsItemFromPool(newsItemPrefab);
            newsItem.transform.SetAsFirstSibling();

            // ������� ��������� �������� ������� � ��������� yield-����
            yield return StartCoroutine(SetupNewsItemCoroutine(newsItem, post, group, hasText, hasImage));
        }

        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 1;
        }
    }

    IEnumerator SetupNewsItemCoroutine(GameObject newsItem, Post post, VKGroup group, bool hasText, bool hasImage)
    {
        try
        {
            SetupNewsItem(newsItem, post, group, hasText, hasImage);
        }
        catch (Exception ex)
        {
            Debug.LogError($"������ ��� ��������� �������� �������: {ex.Message}");
        }
        yield return null;
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

            // ���������� ����� ������ ������������
            GameObject newsItemPrefab;
            if (hasText && hasImage)
            {
                newsItemPrefab = newsItemWithPhotoPrefab;
            }
            else if (hasText)
            {
                newsItemPrefab = newsItemWithoutPhotoPrefab;
            }
            else // ������ ����
            {
                newsItemPrefab = newsItemPhotoOnlyPrefab;
            }

            // ���������� ������ �� ���� ��� ������� �����
            GameObject newsItem = GetNewsItemFromPool(newsItemPrefab);

            SetupNewsItem(newsItem, post, group, hasText, hasImage);

            // ������ �����, ����� �� ����������� UI
            yield return null;
        }

        // �������� ��������� ��������
        if (loadingIndicator != null)
            loadingIndicator.SetActive(false);
    }

    private GameObject GetNewsItemFromPool(GameObject prefab)
    {
        // ���� ������ � ����
        foreach (var item in newsItemPool)
        {
            if (item.activeInHierarchy == false && item.name == prefab.name + "(Clone)")
            {
                // ���������� ������������� �����������
                var foto = item.transform.Find("Foto")?.GetComponent<RawImage>();
                if (foto != null)
                {
                    foto.GetComponent<RectTransform>().sizeDelta = new Vector2(prefab.GetComponent<RectTransform>().rect.width, 0);
                }

                item.SetActive(true);
                return item;
            }
        }

        // ���� ����������� ������� � ���� ���, ������� �����
        GameObject newsItem = Instantiate(prefab, newsContainer);
        newsItemPool.Enqueue(newsItem);
        return newsItem;
    }

    void SetupNewsItem(GameObject newsItem, Post post, VKGroup group, bool hasText, bool hasImage)
    {
        // ���� Name_group ������ foto_group
        var nameGroup = newsItem.transform.Find("foto_group/Name_group")?.GetComponent<Text>();
        var dateTimeText = newsItem.transform.Find("DateTimeText")?.GetComponent<Text>();
        var fotoGroup = newsItem.transform.Find("foto_group")?.GetComponent<RawImage>();
        var foto = newsItem.transform.Find("Foto")?.GetComponent<RawImage>();

        // �������: ��������� ������ ������
        Debug.Log($"��������� �������: group.name = {group.name}, group.photo_200 = {group.photo_200}");

        if (nameGroup != null)
        {
            nameGroup.text = string.IsNullOrEmpty(group.name) ? "����������� ������" : group.name;
            nameGroup.gameObject.SetActive(true); // ��������, ��� ������ �������
            LayoutRebuilder.ForceRebuildLayoutImmediate(nameGroup.GetComponent<RectTransform>());
        }
        else
        {
            Debug.LogWarning($"������ Name_group �� ������ � {newsItem.name}/foto_group");
        }

        if (fotoGroup != null)
        {
            // ��������, ��� RawImage ������� � ����� ��������� �������
            fotoGroup.gameObject.SetActive(true);
            RectTransform fotoRect = fotoGroup.GetComponent<RectTransform>();
            if (fotoRect.sizeDelta.x <= 0 || fotoRect.sizeDelta.y <= 0)
            {
                Debug.LogWarning($"������� foto_group � {newsItem.name} ����� ����. ������������� ����������� �������.");
                fotoRect.sizeDelta = new Vector2(50, 50); // ���������� ���������� �������
            }

            if (!string.IsNullOrEmpty(group.photo_200))
            {
                StartCoroutine(LoadGroupImage(group.photo_200, fotoGroup));
            }
            else
            {
                Debug.LogWarning($"URL ����������� ������ ������ ��� {group.name}");
            }
        }
        else
        {
            Debug.LogWarning($"������ foto_group �� ������ � {newsItem.name}");
        }

        System.DateTime dateTime;
        try
        {
            // ������������ Unix-����� � DateTime � ���������� ������
            dateTime = new System.DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc)
            .AddSeconds(post.date)
            .ToLocalTime();
        }
        catch (ArgumentOutOfRangeException)
        {
            // ���� ���� ���������, ���������� ������� �����
            dateTime = DateTime.Now;
            Debug.LogWarning($"���������� ���� � �����: {post.date}. ������������ ������� ����.");
        }

        if (dateTimeText != null)
        {
            dateTimeText.text = dateTime.ToString("dd.MM.yyyy HH:mm");
            dateTimeText.gameObject.SetActive(true);
            LayoutRebuilder.ForceRebuildLayoutImmediate(dateTimeText.GetComponent<RectTransform>());
        }

        // ��������� �����������
        if (hasImage && foto != null)
        {
            var photoSize = post.attachments.First(a => a.type == "photo").photo.sizes.FirstOrDefault(s => s.type == "x");
            if (photoSize != null && !string.IsNullOrEmpty(photoSize.url))
            {
                StartCoroutine(LoadAndScaleImage(photoSize.url, foto, photoSize.width, photoSize.height));
            }
        }

        // ������������� ������������ ������ (������ ���� ���� ����� � ������ ������������ �����)
        if (hasText && newsItem.name.StartsWith("Panel_news"))
        {
            var expandableText = newsItem.GetComponent<ExpandableNewsText>();
            if (expandableText == null)
            {
                expandableText = newsItem.AddComponent<ExpandableNewsText>();
                expandableText.textShort = newsItem.transform.Find("Image/Text_Short")?.GetComponent<Text>();
                expandableText.textFull = newsItem.transform.Find("Image/Text_Full")?.GetComponent<Text>();
                expandableText.showMoreButton = newsItem.transform.Find("Image/Button_ShowMore")?.GetComponent<Button>();
            }

            if (expandableText.textShort != null && expandableText.textFull != null && expandableText.showMoreButton != null)
            {
                expandableText.Initialize(post.text);
            }
            else
            {
                Debug.LogWarning($"�� ������� ���������������� ExpandableNewsText ��� {newsItem.name}: ���������� �� �������.");
            }
        }
        else if (hasText && newsItem.name.StartsWith("Panel_news_netPhoto"))
        {
            var expandableText = newsItem.GetComponent<ExpandableNewsText>();
            if (expandableText == null)
            {
                expandableText = newsItem.AddComponent<ExpandableNewsText>();
                expandableText.textShort = newsItem.transform.Find("back/Text_Short")?.GetComponent<Text>();
                expandableText.textFull = newsItem.transform.Find("back/Text_Full")?.GetComponent<Text>();
                expandableText.showMoreButton = newsItem.transform.Find("back/Button_ShowMore")?.GetComponent<Button>();
            }

            if (expandableText.textShort != null && expandableText.textFull != null && expandableText.showMoreButton != null)
            {
                expandableText.Initialize(post.text);
            }
            else
            {
                Debug.LogWarning($"�� ������� ���������������� ExpandableNewsText ��� {newsItem.name}: ���������� �� �������.");
            }
        }
    }

    IEnumerator LoadAndScaleImage(string url, RawImage targetImage, int originalWidth, int originalHeight)
    {
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"������ �������� ����������� �����: {request.error}, URL: {url}");
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
        if (string.IsNullOrEmpty(url))
        {
            Debug.LogWarning("URL ��� ����������� ������.");
            yield break;
        }

        UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"������ �������� ����������� ������: {request.error}, URL: {url}");
        }
        else
        {
            targetImage.texture = ((DownloadHandlerTexture)request.downloadHandler).texture;
            targetImage.gameObject.SetActive(true); // ��������, ��� RawImage �������
            LayoutRebuilder.ForceRebuildLayoutImmediate(targetImage.GetComponent<RectTransform>());
        }
    }
}