using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class AvatarLoader : MonoBehaviour
{
    public Image avatarImage;
    private FirebaseDBManager firebaseManager;

    void Awake()
    {
        // �������������� firebaseManager ����� Singleton
        firebaseManager = FirebaseDBManager.Instance;
        if (firebaseManager == null)
        {
            Debug.LogError("FirebaseDBManager �� ������! ���������, ��� �� ��������������� � �����.");
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
            Debug.LogWarning("������� ������������ �� ���������");
        }
    }

    private void DisplayAvatar(Texture2D texture)
    {
        if (avatarImage == null)
        {
            Debug.LogError("avatarImage �� �������� � ����������!");
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
            Debug.LogWarning("userId ������ ��� null");
            return;
        }

        if (firebaseManager == null)
        {
            Debug.LogError("firebaseManager �� ���������������!");
            return;
        }

        avatarImage.gameObject.SetActive(false);

        try
        {
            byte[] avatarData = await firebaseManager.GetUserAvatar(userId);

            if (avatarData == null || avatarData.Length == 0)
            {
                Debug.LogWarning("������ �� ������ ��� ������");
                return;
            }

            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(avatarData))
            {
                Debug.LogError("�� ������� ��������� �������� �� ������");
                return;
            }

            // �������� ������
            UserSession.CachedAvatar = texture;

            avatarImage.sprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f)
            );

            avatarImage.gameObject.SetActive(true);
            Debug.Log("������ ������� �������� � ���������");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"������ ��� �������� �������: {ex.Message}");
        }
    }
}