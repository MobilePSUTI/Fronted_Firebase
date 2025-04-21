using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class AvatarLoader : MonoBehaviour
{
    public Image avatarImage;
    private FirebaseDBManager firebaseManager;

    // �������� ���� ��� ������� �� ���������
    public Sprite defaultAvatarSprite; // ��������� � ���������� Unity

    void Awake()
    {
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
            DisplayDefaultAvatar();
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

    private void DisplayDefaultAvatar()
    {
        if (avatarImage == null)
        {
            Debug.LogError("avatarImage �� �������� � ����������!");
            return;
        }

        if (defaultAvatarSprite != null)
        {
            avatarImage.sprite = defaultAvatarSprite;
            avatarImage.gameObject.SetActive(true);
            Debug.Log("�������� ������ �� ���������");
        }
        else
        {
            Debug.LogWarning("������ �� ��������� �� �������� � ����������");
            avatarImage.gameObject.SetActive(false);
        }
    }

    public async void LoadAvatar(string userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            Debug.LogWarning("userId ������ ��� null");
            DisplayDefaultAvatar();
            return;
        }

        if (firebaseManager == null)
        {
            Debug.LogError("firebaseManager �� ���������������!");
            DisplayDefaultAvatar();
            return;
        }

        avatarImage.gameObject.SetActive(false);

        try
        {
            byte[] avatarData = await firebaseManager.GetUserAvatar(userId);

            if (avatarData == null || avatarData.Length == 0)
            {
                Debug.LogWarning("������ �� ������ ��� ������");
                DisplayDefaultAvatar();
                return;
            }

            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(avatarData))
            {
                Debug.LogError("�� ������� ��������� �������� �� ������");
                DisplayDefaultAvatar();
                return;
            }

            // �������� ������
            UserSession.CachedAvatar = texture;

            DisplayAvatar(texture);
            Debug.Log("������ ������� �������� � ���������");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"������ ��� �������� �������: {ex.Message}");
            DisplayDefaultAvatar();
        }
    }
}