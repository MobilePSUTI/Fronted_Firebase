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
    private const string NewsItemPhotoOnlyPrefabPath = "Panel_newPhoto";

    private DateTime lastUpdateTime; // Время последнего обновления
    private const float updateInterval = 60f; // Интервал проверки обновлений (в секундах)

    private bool isDragging = false; // Флаг для отслеживания, тянет ли пользователь список
    private bool isUpdating = false; // Флаг для предотвращения множественных обновлений

    private GameObject newsItemWithPhotoPrefab; // Префаб новости с фото и текстом
    private GameObject newsItemWithoutPhotoPrefab; // Префаб новости только с текстом
    private GameObject newsItemPhotoOnlyPrefab; // Префаб новости только с фото

    private Queue<GameObject> newsItemPool = new Queue<GameObject>(); // Пул объектов для новостей
    private bool isInitialLoadComplete = false;

    void Start()
    {
        lastUpdateTime = DateTime.Now;
        newsItemWithPhotoPrefab = Resources.Load<GameObject>(NewsItemWithPhotoPrefabPath);
        newsItemWithoutPhotoPrefab = Resources.Load<GameObject>(NewsItemWithoutPhotoPrefabPath);
        newsItemPhotoOnlyPrefab = Resources.Load<GameObject>(NewsItemPhotoOnlyPrefabPath);

        if (loadingIndicator != null)
            loadingIndicator.SetActive(true);

        // Всегда сначала показываем кэшированные данные, если они есть
        if (NewsDataCache.CachedPosts != null && NewsDataCache.CachedPosts.Count > 0 &&
        NewsDataCache.CachedVKGroups != null && NewsDataCache.CachedVKGroups.Count > 0)
        {
            StartCoroutine(DisplayCachedNews());
        }

        // Затем загружаем свежие данные
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
            Debug.LogError("Не удалось загрузить новости: allPosts или groupDictionary равны null.");
        }

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
            if (group == null)
            {
                Debug.LogWarning($"Группа для поста с owner_id = {post.owner_id} не найдена.");
                continue;
            }

            // Определяем какой префаб использовать
            GameObject newsItemPrefab;
            if (hasText && hasImage)
            {
                newsItemPrefab = newsItemWithPhotoPrefab;
            }
            else if (hasText)
            {
                newsItemPrefab = newsItemWithoutPhotoPrefab;
            }
            else // только фото
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

        try
        {
            // Инициализация времени последнего обновления
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
                // Если время невалидно, используем текущее время
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

        // Запускаем загрузку
        IEnumerator internalLoad = vkNewsLoad.GetNewsFromVK();
        while (internalLoad.MoveNext())
        {
            yield return internalLoad.Current;
        }

        // Проверяем результат
        if (vkNewsLoad.allPosts != null)
        {
            try
            {
                // Фильтруем посты
                long lastUpdateTimestamp = new DateTimeOffset(lastUpdateTime).ToUnixTimeSeconds();
                filteredPosts = vkNewsLoad.allPosts
                .Where(p => p.date > lastUpdateTimestamp)
                .ToList();
                success = true;
            }
            catch (Exception ex)
            {
                error = ex;
                Debug.LogError($"Ошибка фильтрации постов: {ex.Message}");
            }
        }
        else
        {
            Debug.LogError("Не удалось загрузить новости");
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

            // Настройка элемента с безопасной обработкой ошибок
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
            Debug.LogError($"Ошибка настройки элемента: {ex.Message}");
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
                Debug.LogWarning($"Пост с ID = {post.owner_id} пропущен: нет текста и изображения.");
                continue;
            }

            var group = NewsDataCache.CachedVKGroups.ContainsKey(-post.owner_id) ? NewsDataCache.CachedVKGroups[-post.owner_id] : null;
            if (group == null)
            {
                Debug.LogWarning($"Сообщество с ID = {Math.Abs(post.owner_id)} не найдено.");
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

            // Выносим настройку элемента новости в отдельный yield-блок
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
            Debug.LogError($"Ошибка при настройке элемента новости: {ex.Message}");
        }
        yield return null;
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

            // Определяем какой префаб использовать
            GameObject newsItemPrefab;
            if (hasText && hasImage)
            {
                newsItemPrefab = newsItemWithPhotoPrefab;
            }
            else if (hasText)
            {
                newsItemPrefab = newsItemWithoutPhotoPrefab;
            }
            else // только фото
            {
                newsItemPrefab = newsItemPhotoOnlyPrefab;
            }

            // Используем объект из пула или создаем новый
            GameObject newsItem = GetNewsItemFromPool(newsItemPrefab);

            SetupNewsItem(newsItem, post, group, hasText, hasImage);

            // Делаем паузу, чтобы не перегружать UI
            yield return null;
        }

        // Скрываем индикатор загрузки
        if (loadingIndicator != null)
            loadingIndicator.SetActive(false);
    }

    private GameObject GetNewsItemFromPool(GameObject prefab)
    {
        // Ищем объект в пуле
        foreach (var item in newsItemPool)
        {
            if (item.activeInHierarchy == false && item.name == prefab.name + "(Clone)")
            {
                // Сбрасываем трансформацию изображения
                var foto = item.transform.Find("Foto")?.GetComponent<RawImage>();
                if (foto != null)
                {
                    foto.GetComponent<RectTransform>().sizeDelta = new Vector2(prefab.GetComponent<RectTransform>().rect.width, 0);
                }

                item.SetActive(true);
                return item;
            }
        }

        // Если подходящего объекта в пуле нет, создаем новый
        GameObject newsItem = Instantiate(prefab, newsContainer);
        newsItemPool.Enqueue(newsItem);
        return newsItem;
    }

    void SetupNewsItem(GameObject newsItem, Post post, VKGroup group, bool hasText, bool hasImage)
    {
        // Ищем Name_group внутри foto_group
        var nameGroup = newsItem.transform.Find("foto_group/Name_group")?.GetComponent<Text>();
        var dateTimeText = newsItem.transform.Find("DateTimeText")?.GetComponent<Text>();
        var fotoGroup = newsItem.transform.Find("foto_group")?.GetComponent<RawImage>();
        var foto = newsItem.transform.Find("Foto")?.GetComponent<RawImage>();

        // Отладка: проверяем данные группы
        Debug.Log($"Настройка новости: group.name = {group.name}, group.photo_200 = {group.photo_200}");

        if (nameGroup != null)
        {
            nameGroup.text = string.IsNullOrEmpty(group.name) ? "Неизвестная группа" : group.name;
            nameGroup.gameObject.SetActive(true); // Убедимся, что объект активен
            LayoutRebuilder.ForceRebuildLayoutImmediate(nameGroup.GetComponent<RectTransform>());
        }
        else
        {
            Debug.LogWarning($"Объект Name_group не найден в {newsItem.name}/foto_group");
        }

        if (fotoGroup != null)
        {
            // Убедимся, что RawImage активен и имеет ненулевые размеры
            fotoGroup.gameObject.SetActive(true);
            RectTransform fotoRect = fotoGroup.GetComponent<RectTransform>();
            if (fotoRect.sizeDelta.x <= 0 || fotoRect.sizeDelta.y <= 0)
            {
                Debug.LogWarning($"Размеры foto_group в {newsItem.name} равны нулю. Устанавливаем стандартные размеры.");
                fotoRect.sizeDelta = new Vector2(50, 50); // Установите подходящие размеры
            }

            if (!string.IsNullOrEmpty(group.photo_200))
            {
                StartCoroutine(LoadGroupImage(group.photo_200, fotoGroup));
            }
            else
            {
                Debug.LogWarning($"URL изображения группы пустой для {group.name}");
            }
        }
        else
        {
            Debug.LogWarning($"Объект foto_group не найден в {newsItem.name}");
        }

        System.DateTime dateTime;
        try
        {
            // Конвертируем Unix-время в DateTime с обработкой ошибок
            dateTime = new System.DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc)
            .AddSeconds(post.date)
            .ToLocalTime();
        }
        catch (ArgumentOutOfRangeException)
        {
            // Если дата невалидна, используем текущее время
            dateTime = DateTime.Now;
            Debug.LogWarning($"Невалидная дата в посте: {post.date}. Использована текущая дата.");
        }

        if (dateTimeText != null)
        {
            dateTimeText.text = dateTime.ToString("dd.MM.yyyy HH:mm");
            dateTimeText.gameObject.SetActive(true);
            LayoutRebuilder.ForceRebuildLayoutImmediate(dateTimeText.GetComponent<RectTransform>());
        }

        // Настройка изображения
        if (hasImage && foto != null)
        {
            var photoSize = post.attachments.First(a => a.type == "photo").photo.sizes.FirstOrDefault(s => s.type == "x");
            if (photoSize != null && !string.IsNullOrEmpty(photoSize.url))
            {
                StartCoroutine(LoadAndScaleImage(photoSize.url, foto, photoSize.width, photoSize.height));
            }
        }

        // Инициализация расширяемого текста (только если есть текст и префаб поддерживает текст)
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
                Debug.LogWarning($"Не удалось инициализировать ExpandableNewsText для {newsItem.name}: компоненты не найдены.");
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
                Debug.LogWarning($"Не удалось инициализировать ExpandableNewsText для {newsItem.name}: компоненты не найдены.");
            }
        }
    }

    IEnumerator LoadAndScaleImage(string url, RawImage targetImage, int originalWidth, int originalHeight)
    {
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Ошибка загрузки изображения поста: {request.error}, URL: {url}");
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
        if (string.IsNullOrEmpty(url))
        {
            Debug.LogWarning("URL для изображения пустой.");
            yield break;
        }

        UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Ошибка загрузки изображения группы: {request.error}, URL: {url}");
        }
        else
        {
            targetImage.texture = ((DownloadHandlerTexture)request.downloadHandler).texture;
            targetImage.gameObject.SetActive(true); // Убедимся, что RawImage активен
            LayoutRebuilder.ForceRebuildLayoutImmediate(targetImage.GetComponent<RectTransform>());
        }
    }
}