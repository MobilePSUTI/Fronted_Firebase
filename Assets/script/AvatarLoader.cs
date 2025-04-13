using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class AvatarLoader : MonoBehaviour
{
    public Image avatarImage;
    public GameObject loadingIndicator;
    private FirebaseDBManager firebaseManager;

    void Start()
    {
        firebaseManager = gameObject.AddComponent<FirebaseDBManager>();
        _ = firebaseManager.Initialize();

        if (UserSession.CurrentUser != null)
        {
            LoadAvatar(UserSession.CurrentUser.Id); // Теперь Id - string
        }
        else
        {
            Debug.LogWarning("Текущий пользователь не определен");
        }
    }

    public void LoadAvatar(string userId) // Изменено на string
    {
        StartCoroutine(LoadAvatarCoroutine(userId));
    }

    IEnumerator LoadAvatarCoroutine(string userId) // Изменено на string
    {
        if (loadingIndicator != null)
            loadingIndicator.SetActive(true);

        avatarImage.gameObject.SetActive(false);

        try
        {
            var task = firebaseManager.GetUserAvatar(userId);
            yield return new WaitUntil(() => task.IsCompleted);

            byte[] avatarData = task.Result;

            if (avatarData == null || avatarData.Length == 0)
            {
                Debug.LogWarning("Аватар не найден или пустой");
                yield break;
            }

            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(avatarData))
            {
                Debug.LogError("Не удалось загрузить текстуру из байтов");
                yield break;
            }

            avatarImage.sprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f)
            );

            avatarImage.gameObject.SetActive(true);
            Debug.Log("Аватар успешно загружен и отображен");
        }
        finally
        {
            if (loadingIndicator != null)
                loadingIndicator.SetActive(false);
        }
    }
}