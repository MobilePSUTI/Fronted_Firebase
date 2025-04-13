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

    void Start()
    {
        // Инициализируем lastUpdateTime текущим временем
        lastUpdateTime = DateTime.Now;

        // Проверяем, что контейнер для новостей назначен
        if (newsContainer == null)
        {
            Debug.LogError("Контейнер для новостей (newsContainer) не назначен.");
            return;
        }

        // Загружаем префабы один раз при старте
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

        // Загружаем новости из кэша
        StartCoroutine(DisplayCachedNews());

        // Запускаем корутину для периодической проверки обновлений
        StartCoroutine(CheckForUpdates());
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
        // Если обновление уже выполняется, выходим
        if (isUpdating)
        {
            yield break;
        }

        isUpdating = true; // Устанавливаем флаг обновления

        // Проверяем, что lastUpdateTime находится в допустимом диапазоне
        if (lastUpdateTime.Year < 1 || lastUpdateTime.Year > 9999)
        {
            Debug.LogError("Некорректное значение lastUpdateTime.");
            isUpdating = false;
            yield break;
        }

        // Загружаем новости из ВКонтакте
        var vkNewsLoad = gameObject.AddComponent<VKNewsLoad>();
        yield return StartCoroutine(vkNewsLoad.GetNewsFromVK());

        // Преобразуем lastUpdateTime в Unix timestamp для сравнения
        long lastUpdateTimestamp = ((DateTimeOffset)lastUpdateTime).ToUnixTimeSeconds();

        // Проверяем, есть ли новые новости
        var newPosts = vkNewsLoad.allPosts
            .Where(p => p.date > lastUpdateTimestamp) // Сравниваем Unix timestamp
            .ToList();

        if (newPosts.Count > 0)
        {
            // Обновляем время последнего обновления
            lastUpdateTime = DateTime.Now;

            // Добавляем новые новости в начало списка
            NewsDataCache.CachedPosts.InsertRange(0, newPosts);

            // Обновляем UI
            yield return StartCoroutine(AddNewPostsToUI(newPosts));
        }

        // Уничтожаем временный компонент
        Destroy(vkNewsLoad);

        isUpdating = false; // Сбрасываем флаг обновления
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
        var nameGroup = newsItem.transform.Find("Name_group")?.GetComponent<Text>();
        var dateTimeText = newsItem.transform.Find("DateTimeText")?.GetComponent<Text>();
        var newsText = newsItem.transform.Find("Text (Legacy)")?.GetComponent<Text>();
        var fotoGroup = newsItem.transform.Find("foto_group")?.GetComponent<RawImage>();
        var foto = newsItem.transform.Find("Foto")?.GetComponent<RawImage>();

        if (nameGroup == null || dateTimeText == null || newsText == null)
        {
            Debug.LogError("Не удалось найти необходимые компоненты в префабе.");
            return;
        }

        nameGroup.text = group.name;
        StartCoroutine(LoadGroupImage(group.photo_200, fotoGroup));

        newsText.text = hasText ? post.text : "";

        System.DateTime dateTime = new System.DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
        dateTime = dateTime.AddSeconds(post.date).ToLocalTime();
        dateTimeText.text = dateTime.ToString("dd.MM.yyyy HH:mm");

        if (hasImage)
        {
            var photoSize = post.attachments.First(a => a.type == "photo").photo.sizes.FirstOrDefault(s => s.type == "x");
            if (photoSize != null && !string.IsNullOrEmpty(photoSize.url))
            {
                StartCoroutine(LoadImage(photoSize.url, foto));
            }
        }
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