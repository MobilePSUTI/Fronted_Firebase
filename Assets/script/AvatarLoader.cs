using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class AvatarLoader : MonoBehaviour
{
    public Image avatarImage;
    private FirebaseDBManager firebaseManager;

    void Awake()
    {
        // Инициализируем firebaseManager через Singleton
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

    public async void LoadAvatar(string userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            Debug.LogWarning("userId пустой или null");
            return;
        }

        if (firebaseManager == null)
        {
            Debug.LogError("firebaseManager не инициализирован!");
            return;
        }

        avatarImage.gameObject.SetActive(false);

        try
        {
            byte[] avatarData = await firebaseManager.GetUserAvatar(userId);

            if (avatarData == null || avatarData.Length == 0)
            {
                Debug.LogWarning("Аватар не найден или пустой");
                return;
            }

            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(avatarData))
            {
                Debug.LogError("Не удалось загрузить текстуру из байтов");
                return;
            }

            // Кэшируем аватар
            UserSession.CachedAvatar = texture;

            avatarImage.sprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f)
            );

            avatarImage.gameObject.SetActive(true);
            Debug.Log("Аватар успешно загружен и отображен");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Ошибка при загрузке аватара: {ex.Message}");
        }
    }
}