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
            Debug.LogWarning("������� ������������ �� ���������");
        }
    }
    private void DisplayAvatar(Texture2D texture)
    {
        avatarImage.sprite = Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f)
        );
        avatarImage.gameObject.SetActive(true);

        if (loadingIndicator != null)
            loadingIndicator.SetActive(false);
    }

    public void LoadAvatar(string userId) // �������� �� string
    {
        StartCoroutine(LoadAvatarCoroutine(userId));
    }

    IEnumerator LoadAvatarCoroutine(string userId) // �������� �� string
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
                Debug.LogWarning("������ �� ������ ��� ������");
                yield break;
            }

            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(avatarData))
            {
                Debug.LogError("�� ������� ��������� �������� �� ������");
                yield break;
            }

            avatarImage.sprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f)
            );

            avatarImage.gameObject.SetActive(true);
            Debug.Log("������ ������� �������� � ���������");
        }
        finally
        {
            if (loadingIndicator != null)
                loadingIndicator.SetActive(false);
        }
    }
}