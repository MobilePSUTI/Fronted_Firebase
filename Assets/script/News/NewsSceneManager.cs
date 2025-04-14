using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.Networking;

public class NewsSceneManager : MonoBehaviour
{
    public Transform newsContainer; // Контейнер для новостей
    public ScrollRect scrollRect; // ScrollRect для прокрутки
    public GameObject loadingIndicator; // Индикатор загрузки

    private const string NewsItemWithPhotoPrefabPath = "Panel_news";
    private const string NewsItemWithoutPhotoPrefabPath = "Panel_news_netPhoto";

    private DateTime lastUpdateTime; // Время последнего обновления
    private const float updateInterval = 60f; // Интервал проверки обновлений (в секундах)

    private bool isDragging = false; // Флаг для отслеживания, тянет ли пользователь список
    private bool isUpdating = false; // Флаг для предотвращения множественных обновлений

    private GameObject newsItemWithPhotoPrefab; // Префаб новости с фото
    private GameObject newsItemWithoutPhotoPrefab; // Префаб новости без фото

    private Queue<GameObject> newsItemPool = new Queue<GameObject>(); // Пул объектов для новостей
    private bool isInitialLoadComplete = false;

    void Start()
    {
        // Загружаем префабы
        newsItemWithPhotoPrefab = Resources.Load<GameObject>(NewsItemWithPhotoPrefabPath);
        newsItemWithoutPhotoPrefab = Resources.Load<GameObject>(NewsItemWithoutPhotoPrefabPath);

        if (newsItemWithPhotoPrefab == null || newsItemWithoutPhotoPrefab == null)
        {
            Debug.LogError("Префабы не найдены в папке Resources.");
            return;
        }

        // Показываем индикатор загрузки
        if (loadingIndicator != null)
            loadingIndicator.SetActive(true);
        lastUpdateTime = DateTime.Now;
        // Начинаем загрузку новостей
        StartCoroutine(LoadNewsAndDisplay());
    }
    IEnumerator LoadNewsAndDisplay()
    {
        // Загружаем новости
        var vkNewsLoad = gameObject.AddComponent<VKNewsLoad>();
        yield return StartCoroutine(vkNewsLoad.GetNewsFromVK(0, 100));

        // Проверяем результат
        if (vkNewsLoad.allPosts != null && vkNewsLoad.groupDictionary != null)
        {
            // Сохраняем в кэш
            NewsDataCache.CachedPosts = vkNewsLoad.allPosts;
            NewsDataCache.CachedVKGroups = vkNewsLoad.groupDictionary;

            // Отображаем новости
            yield return StartCoroutine(DisplayNews(vkNewsLoad.allPosts, vkNewsLoad.groupDictionary));
        }
        else
        {
            Debug.LogError("Не удалось загрузить новости");
            // Можно добавить повторную попытку здесь
        }

        // Скрываем индикатор загрузки
        if (loadingIndicator != null)
            loadingIndicator.SetActive(false);

        Destroy(vkNewsLoad);
        isInitialLoadComplete = true;
    }
    IEnumerator DisplayNews(List<Post> posts, Dictionary<long, VKGroup> groups)
    {
        // Очищаем старые новости
        foreach (Transform child in newsContainer)
        {
            child.gameObject.SetActive(false);
        }

        // Отображаем новые новости
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
        // Проверяем, тянет ли пользователь список
        if (scrollRect != null)
        {
            if (Input.GetMouseButton(0)) // Проверяем, нажата ли левая кнопка мыши
            {
                isDragging = true;
            }
            else
            {
                isDragging = false;
            }

            // Проверяем, листает ли пользователь вверх
            if (isDragging && scrollRect.velocity.y > 0 && scrollRect.verticalNormalizedPosition >= 0.99f)
            {
                // Запускаем обновление новостей
                StartCoroutine(CheckAndUpdateNews());
            }
        }
    }

    IEnumerator CheckForUpdates()
    {
        while (true)
        {
            yield return new WaitForSeconds(updateInterval); // Ждем заданный интервал

            // Проверяем наличие новых новостей
            yield return StartCoroutine(CheckAndUpdateNews());
        }
    }

    IEnumerator CheckAndUpdateNews()
    {
        if (isUpdating) yield break;
        isUpdating = true;

        // Защита от некорректной даты
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
        // Добавляем новые новости в начало контейнера
        foreach (var post in newPosts)
        {
            // Проверяем, есть ли текст или изображение
            bool hasText = !string.IsNullOrEmpty(post.text);
            bool hasImage = post.attachments != null && post.attachments.Any(a => a.type == "photo");

            // Если новость пустая (нет текста и изображений), пропускаем её
            if (!hasText && !hasImage)
            {
                Debug.LogWarning($"Пост с ID = {post.owner_id} пропущен: нет текста и изображения.");
                continue;
            }

            var group = NewsDataCache.CachedVKGroups.ContainsKey(-post.owner_id) ? NewsDataCache.CachedVKGroups[-post.owner_id] : null;
            if (group == null)
            {
                Debug.LogWarning($"Сообщество с ID = {Math.Abs(post.owner_id)} не найдено.");
                continue;
            }

            // Используем объект из пула или создаем новый
            GameObject newsItem = GetNewsItemFromPool(hasImage);
            newsItem.transform.SetAsFirstSibling();

            SetupNewsItem(newsItem, post, group, hasText, hasImage);

            // Делаем паузу, чтобы не перегружать UI
            yield return null;
        }

        // Прокручиваем список вверх
        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 1; // Прокрутка вверх
        }
    }

    IEnumerator DisplayCachedNews()
    {
        // Проверяем, есть ли данные в кэше
        if (NewsDataCache.CachedPosts == null || NewsDataCache.CachedVKGroups == null)
        {
            Debug.LogError("Нет данных в кэше.");
            yield break;
        }

        // Отображаем новости
        foreach (var post in NewsDataCache.CachedPosts)
        {
            // Проверяем, есть ли текст или изображение
            bool hasText = !string.IsNullOrEmpty(post.text);
            bool hasImage = post.attachments != null && post.attachments.Any(a => a.type == "photo");

            // Если новость пустая (нет текста и изображений), пропускаем её
            if (!hasText && !hasImage)
            {
                Debug.LogWarning($"Пост с ID = {post.owner_id} пропущен: нет текста и изображения.");
                continue;
            }

            var group = NewsDataCache.CachedVKGroups.ContainsKey(-post.owner_id) ? NewsDataCache.CachedVKGroups[-post.owner_id] : null;
            if (group == null)
            {
                Debug.LogWarning($"Сообщество с ID = {Math.Abs(post.owner_id)} не найдено.");
                continue;
            }

            // Используем объект из пула или создаем новый
            GameObject newsItem = GetNewsItemFromPool(hasImage);

            SetupNewsItem(newsItem, post, group, hasText, hasImage);

            // Делаем паузу, чтобы не перегружать UI
            yield return null;
        }

        // Скрываем индикатор загрузки
        if (loadingIndicator != null)
            loadingIndicator.SetActive(false);
    }

    private GameObject GetNewsItemFromPool(bool hasImage)
    {
        GameObject newsItemPrefab = hasImage ? newsItemWithPhotoPrefab : newsItemWithoutPhotoPrefab;

        // Ищем объект в пуле
        foreach (var item in newsItemPool)
        {
            if (item.activeInHierarchy == false && item.name == newsItemPrefab.name)
            {
                // Сбрасываем трансформацию изображения
                var foto = item.transform.Find("Foto")?.GetComponent<RawImage>();
                if (foto != null)
                {
                    foto.GetComponent<RectTransform>().sizeDelta = new Vector2(newsItemPrefab.GetComponent<RectTransform>().rect.width, 0);
                }

                item.SetActive(true);
                return item;
            }
        }

        // Если подходящего объекта в пуле нет, создаем новый
        GameObject newsItem = Instantiate(newsItemPrefab, newsContainer);
        newsItemPool.Enqueue(newsItem);
        return newsItem;
    }

    void SetupNewsItem(GameObject newsItem, Post post, VKGroup group, bool hasText, bool hasImage)
    {
        // Существующие компоненты
        var nameGroup = newsItem.transform.Find("Name_group")?.GetComponent<Text>();
        var dateTimeText = newsItem.transform.Find("DateTimeText")?.GetComponent<Text>();
        var fotoGroup = newsItem.transform.Find("foto_group")?.GetComponent<RawImage>();
        var foto = newsItem.transform.Find("Foto")?.GetComponent<RawImage>();

        // Инициализация стандартных полей
        nameGroup.text = group.name;
        StartCoroutine(LoadGroupImage(group.photo_200, fotoGroup));

        System.DateTime dateTime = new System.DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
        dateTime = dateTime.AddSeconds(post.date).ToLocalTime();
        dateTimeText.text = dateTime.ToString("dd.MM.yyyy HH:mm");

        // Настройка изображения
        if (hasImage && foto != null)
        {
            var photoSize = post.attachments.First(a => a.type == "photo").photo.sizes.FirstOrDefault(s => s.type == "x");
            if (photoSize != null && !string.IsNullOrEmpty(photoSize.url))
            {
                StartCoroutine(LoadAndScaleImage(photoSize.url, foto, photoSize.width, photoSize.height));
            }
        }

        // Инициализация расширяемого текста
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
            Debug.LogError("Ошибка загрузки: " + request.error);
            yield break;
        }

        // Получаем текстуру
        Texture2D texture = ((DownloadHandlerTexture)request.downloadHandler).texture;
        targetImage.texture = texture;

        // Настройка размеров
        RectTransform imageRect = targetImage.GetComponent<RectTransform>();
        float containerWidth = imageRect.parent.GetComponent<RectTransform>().rect.width;
        float aspectRatio = (float)originalWidth / originalHeight;

        // Рассчитываем высоту с учетом пропорций
        float targetHeight = containerWidth / aspectRatio;

        // Ограничиваем максимальную высоту (например, 600px или высоту контейнера)
        float maxHeight = 850f; // Замените на нужное значение
        targetHeight = Mathf.Min(targetHeight, maxHeight);

        // Устанавливаем размер
        imageRect.sizeDelta = new Vector2(containerWidth, targetHeight);

        // Принудительно обновляем макет
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
            Debug.LogError("Ошибка: " + request.error);
        }
        else
        {
            targetImage.texture = ((DownloadHandlerTexture)request.downloadHandler).texture;
        }
    }
}