using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System;

public class StudentRegistration : MonoBehaviour
{
    [Header("UI Elements")]
    public InputField emailInput;
    public InputField passwordInput;
    public InputField firstNameInput;
    public InputField lastNameInput;
    public InputField secondNameInput;
    public Dropdown groupDropdown;
    public Text errorText;
    public GameObject loadingIndicator;
    public GameObject registrationPanel;
    public GameObject avatarSelectionPanel;
    public Image[] avatarOptions;
    public Image selectedAvatarImage;

    [Header("Settings")]
    public int maxAvatarSize = 512; // Максимальный размер аватара в пикселях

    private int selectedAvatarIndex = -1;
    private Texture2D[] avatarTextures;
    private FirebaseDBManager firebaseManager;
    private List<Group> groups;

    void Start()
    {
        // Инициализация Firebase Manager
        firebaseManager = gameObject.AddComponent<FirebaseDBManager>();
        _ = firebaseManager.Initialize();

        // Загрузка списка групп и аватарок
        StartCoroutine(LoadGroups());
        LoadAvatarOptions();

        // Настройка UI
        avatarSelectionPanel.SetActive(false);
        errorText.text = "";
    }

    void LoadAvatarOptions()
    {
        avatarTextures = Resources.LoadAll<Texture2D>("Avatars");

        if (avatarTextures == null || avatarTextures.Length == 0)
        {
            Debug.LogError("Не удалось загрузить текстуры аватаров");
            return;
        }

        for (int i = 0; i < avatarOptions.Length; i++)
        {
            if (i < avatarTextures.Length)
            {
                if (avatarTextures[i] == null || !avatarTextures[i].isReadable)
                {
                    Debug.LogError($"Текстура аватара {i} не доступна для чтения");
                    continue;
                }

                int index = i;
                avatarOptions[i].sprite = Sprite.Create(
                    avatarTextures[i],
                    new Rect(0, 0, avatarTextures[i].width, avatarTextures[i].height),
                    new Vector2(0.5f, 0.5f)
                );

                Button btn = avatarOptions[i].GetComponent<Button>();
                btn.onClick.AddListener(() => SelectAvatar(index));
            }
            else
            {
                avatarOptions[i].gameObject.SetActive(false);
            }
        }
    }

    void SelectAvatar(int index)
    {
        selectedAvatarIndex = index;
        selectedAvatarImage.sprite = Sprite.Create(
            avatarTextures[index],
            new Rect(0, 0, avatarTextures[index].width, avatarTextures[index].height),
            new Vector2(0.5f, 0.5f)
        );
    }

    IEnumerator LoadGroups()
    {
        if (loadingIndicator != null)
            loadingIndicator.SetActive(true);

        errorText.text = "";
        groupDropdown.ClearOptions();
        groupDropdown.interactable = false;

        var task = firebaseManager.GetAllGroups();
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.IsFaulted)
        {
            errorText.text = "Ошибка загрузки групп";
            Debug.LogError("Failed to load groups: " + task.Exception);
            yield break;
        }

        groups = task.Result;

        // Добавляем заголовок в выпадающий список
        groupDropdown.options.Add(new Dropdown.OptionData("Выберите группу"));

        // Сортируем группы по названию
        groups.Sort((a, b) => a.Title.CompareTo(b.Title));

        if (groups.Count > 0)
        {
            foreach (var group in groups)
            {
                groupDropdown.options.Add(new Dropdown.OptionData(group.Title));
            }
            groupDropdown.interactable = true;
        }
        else
        {
            errorText.text = "Нет доступных групп";
            Debug.LogWarning("No groups available in database");
        }

        groupDropdown.value = 0;
        groupDropdown.RefreshShownValue();

        if (loadingIndicator != null)
            loadingIndicator.SetActive(false);
    }

    public void OnRegisterButtonClick()
    {
        // Валидация полей
        string error = ValidateFields();
        if (!string.IsNullOrEmpty(error))
        {
            errorText.text = error;
            return;
        }

        // Переход к выбору аватара
        registrationPanel.SetActive(false);
        avatarSelectionPanel.SetActive(true);
        errorText.text = "";
    }

    private string ValidateFields()
    {
        string email = emailInput.text.Trim();
        string password = passwordInput.text.Trim();
        string firstName = firstNameInput.text.Trim();
        string lastName = lastNameInput.text.Trim();

        if (groupDropdown.options.Count == 0 || groupDropdown.value < 0)
            return "Необходимо выбрать группу.";

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password) ||
            string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName))
            return "Все обязательные поля должны быть заполнены.";

        if (groupDropdown.value == 0)
            return "Необходимо выбрать группу.";

        if (!IsValidEmail(email))
            return "Введите корректный email адрес.";

        return null;
    }

    public void OnConfirmAvatarButtonClick()
    {
        if (selectedAvatarIndex == -1)
        {
            errorText.text = "Пожалуйста, выберите аватар.";
            return;
        }

        if (groups == null || groups.Count == 0 || groupDropdown.value <= 0)
        {
            errorText.text = "Необходимо выбрать группу.";
            Debug.LogError("Ошибка: группы не загружены или не выбрана");
            return;
        }

        // Получаем данные из полей
        string email = emailInput.text.Trim();
        string password = passwordInput.text.Trim();
        string firstName = firstNameInput.text.Trim();
        string lastName = lastNameInput.text.Trim();
        string secondName = secondNameInput.text.Trim();

        // Получаем ID выбранной группы (теперь string)
        int selectedGroupIndex = groupDropdown.value - 1;
        if (selectedGroupIndex < 0 || selectedGroupIndex >= groups.Count)
        {
            errorText.text = "Ошибка выбора группы.";
            Debug.LogError($"Неверный индекс группы: {selectedGroupIndex}, всего групп: {groups.Count}");
            return;
        }

        string groupId = groups[selectedGroupIndex].Id; // Теперь string

        // Подготавливаем аватар
        Texture2D selectedTexture = avatarTextures[selectedAvatarIndex];
        Texture2D resizedTexture = ResizeTexture(selectedTexture, maxAvatarSize, maxAvatarSize);
        byte[] avatarBytes = resizedTexture.EncodeToPNG();

        // Начинаем процесс регистрации
        StartCoroutine(RegisterStudent(email, password, firstName, lastName, secondName, groupId, avatarBytes));
    }

    private Texture2D ResizeTexture(Texture2D source, int newWidth, int newHeight)
    {
        source.filterMode = FilterMode.Bilinear;
        RenderTexture rt = RenderTexture.GetTemporary(newWidth, newHeight);
        rt.filterMode = FilterMode.Bilinear;
        RenderTexture.active = rt;
        Graphics.Blit(source, rt);

        Texture2D result = new Texture2D(newWidth, newHeight);
        result.ReadPixels(new Rect(0, 0, newWidth, newHeight), 0, 0);
        result.Apply();

        RenderTexture.ReleaseTemporary(rt);
        return result;
    }

    IEnumerator RegisterStudent(string email, string password, string firstName,
     string lastName, string secondName, string groupId, byte[] avatarBytes)
    {
        if (loadingIndicator != null)
            loadingIndicator.SetActive(true);

        bool success = false;
        string error = "";

        var registerTask = firebaseManager.RegisterNewStudent(
            email, password, firstName, lastName, secondName, groupId, avatarBytes);
        yield return new WaitUntil(() => registerTask.IsCompleted);

        if (registerTask.IsFaulted)
        {
            error = "Ошибка соединения";
        }
        else
        {
            success = registerTask.Result;
            error = success ? "" : "Ошибка регистрации";
        }

        if (!string.IsNullOrEmpty(error))
            errorText.text = error;
        else if (success)
            SceneManager.LoadScene("MainScene");

        if (loadingIndicator != null)
            loadingIndicator.SetActive(false);
    }


    public void OnBackButtonClick()
    {
        if (avatarSelectionPanel.activeSelf)
        {
            avatarSelectionPanel.SetActive(false);
            registrationPanel.SetActive(true);
        }
        else
        {
            SceneManager.LoadScene("MainScene");
        }
    }

    private bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    private void ShowError(string message)
    {
        errorText.text = message;
    }
}