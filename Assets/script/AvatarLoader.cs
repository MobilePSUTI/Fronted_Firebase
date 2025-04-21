using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class AvatarLoader : MonoBehaviour
{
    public Image avatarImage;
    private FirebaseDBManager firebaseManager;

    // Добавьте поле для аватара по умолчанию
    public Sprite defaultAvatarSprite; // Назначьте в инспекторе Unity

    void Awake()
    {
        firebaseManager = FirebaseDBManager.Instance;
        if (firebaseManager == null)
        {
            Debug.LogError("FirebaseDBManager не найден! Убедитесь, что он инициализирован в сцене.");
        }
    }

    void Start()
    {
        if (UserSession.CurrentUser != null)
        {
            if (UserSession.CachedAvatar != null)
            {
                DisplayAvatar(UserSession.CachedAvatar);
            }
            else
            {
                LoadAvatar(UserSession.CurrentUser.Id);
            }
        }
        else
        {
            Debug.LogWarning("Текущий пользователь не определен");
            DisplayDefaultAvatar();
        }
    }

    private void DisplayAvatar(Texture2D texture)
    {
        if (avatarImage == null)
        {
            Debug.LogError("avatarImage не назначен в инспекторе!");
            return;
        }

        avatarImage.sprite = Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f)
        );
        avatarImage.gameObject.SetActive(true);
    }

    private void DisplayDefaultAvatar()
    {
        if (avatarImage == null)
        {
            Debug.LogError("avatarImage не назначен в инспекторе!");
            return;
        }

        if (defaultAvatarSprite != null)
        {
            avatarImage.sprite = defaultAvatarSprite;
            avatarImage.gameObject.SetActive(true);
            Debug.Log("Отображён аватар по умолчанию");
        }
        else
        {
            Debug.LogWarning("Аватар по умолчанию не назначен в инспекторе");
            avatarImage.gameObject.SetActive(false);
        }
    }

    public async void LoadAvatar(string userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            Debug.LogWarning("userId пустой или null");
            DisplayDefaultAvatar();
            return;
        }

        if (firebaseManager == null)
        {
            Debug.LogError("firebaseManager не инициализирован!");
            DisplayDefaultAvatar();
            return;
        }

        avatarImage.gameObject.SetActive(false);

        try
        {
            byte[] avatarData = await firebaseManager.GetUserAvatar(userId);

            if (avatarData == null || avatarData.Length == 0)
            {
                Debug.LogWarning("Аватар не найден или пустой");
                DisplayDefaultAvatar();
                return;
            }

            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(avatarData))
            {
                Debug.LogError("Не удалось загрузить текстуру из байтов");
                DisplayDefaultAvatar();
                return;
            }

            // Кэшируем аватар
            UserSession.CachedAvatar = texture;

            DisplayAvatar(texture);
            Debug.Log("Аватар успешно загружен и отображен");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Ошибка при загрузке аватара: {ex.Message}");
            DisplayDefaultAvatar();
        }
    }
}