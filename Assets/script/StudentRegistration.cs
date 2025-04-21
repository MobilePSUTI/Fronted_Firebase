using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine.EventSystems;

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

    [Header("UI Adjustment")]
    public RectTransform registrationContentPanel; // ������ � ������ �����
    public ScrollRect scrollRect; // �����������, ���� ������������ ScrollRect
    public float keyboardPadding = 10f; // ������ �� ����������

    [Header("Settings")]
    public int maxAvatarSize = 512;

    private int selectedAvatarIndex = -1;
    private Texture2D[] avatarTextures;
    private FirebaseDBManager firebaseManager;
    private List<Group> groups;
    private bool isKeyboardVisible = false;
    private Vector2 originalContentPosition;

    void Start()
    {
        // ������������� Firebase Manager
        firebaseManager = gameObject.AddComponent<FirebaseDBManager>();
        _ = firebaseManager.Initialize();

        // ��������� UI
        avatarSelectionPanel.SetActive(false);
        errorText.text = "";

        // ������������� ��� ������ � �����������
        if (registrationContentPanel == null && registrationPanel != null)
        {
            registrationContentPanel = registrationPanel.GetComponent<RectTransform>();
        }
        originalContentPosition = registrationContentPanel != null ?
            registrationContentPanel.anchoredPosition : Vector2.zero;

        // ��������� ����� �����
        SetupInputFields();

        // �������� ������
        StartCoroutine(LoadGroups());
        LoadAvatarOptions();
    }

    void Update()
    {
#if UNITY_ANDROID || UNITY_IOS
        // �������� ��������� ����������
        if (TouchScreenKeyboard.visible != isKeyboardVisible)
        {
            isKeyboardVisible = TouchScreenKeyboard.visible;
            if (!isKeyboardVisible)
            {
                ResetContentPosition();
            }
        }
#endif
    }

    void SetupInputFields()
    {
        // ��������� ������� ��� ���� ����� �����
        InputField[] allInputs = GetComponentsInChildren<InputField>(true);
        foreach (InputField input in allInputs)
        {
            // ��������� EventTrigger ���� ��� ���
            EventTrigger trigger = input.gameObject.GetComponent<EventTrigger>();
            if (trigger == null)
            {
                trigger = input.gameObject.AddComponent<EventTrigger>();
            }

            // ������� ������ ��� ������� ������
            var entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.Select;
            entry.callback.AddListener((data) => { OnInputSelected(input); });
            trigger.triggers.Add(entry);

            // ��������� onEndEdit ��� ����
            input.onEndEdit.AddListener((text) => OnInputDeselected(input));
            input.shouldHideMobileInput = false;
        }
    }

    void OnInputSelected(InputField input)
    {
        AdjustForKeyboard(input);
    }

    void OnInputDeselected(InputField input)
    {
        ResetContentPosition();
    }

    void AdjustForKeyboard(InputField input)
    {
        if (input == null || registrationContentPanel == null) return;

        Canvas.ForceUpdateCanvases();

        // �������� ������� ���� �����
        RectTransform inputRect = input.GetComponent<RectTransform>();
        Vector2 inputPosition = (Vector2)inputRect.transform.position;

        // ��������� ��������
        float keyboardHeight = GetKeyboardHeight();
        float canvasHeight = GetComponent<RectTransform>().rect.height;
        float inputBottom = inputPosition.y - inputRect.rect.height / 2;
        float visiblePosition = canvasHeight - keyboardHeight - keyboardPadding;

        if (inputBottom < visiblePosition)
        {
            float adjustment = visiblePosition - inputBottom;
            registrationContentPanel.anchoredPosition += new Vector2(0, adjustment);
        }
    }

    void ResetContentPosition()
    {
        if (registrationContentPanel != null)
        {
            registrationContentPanel.anchoredPosition = originalContentPosition;
        }
    }

    float GetKeyboardHeight()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        using (AndroidJavaClass UnityClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        {
            AndroidJavaObject View = UnityClass.GetStatic<AndroidJavaObject>("currentActivity")
                .Get<AndroidJavaObject>("mUnityPlayer").Call<AndroidJavaObject>("getView");
            using (AndroidJavaObject rect = new AndroidJavaObject("android.graphics.Rect"))
            {
                View.Call("getWindowVisibleDisplayFrame", rect);
                return Screen.height - rect.Call<int>("height");
            }
        }
#elif UNITY_IOS && !UNITY_EDITOR
        return TouchScreenKeyboard.area.height;
#else
        return 300; // ��������� ������ ��� ���������
#endif
    }

    void LoadAvatarOptions()
    {
        avatarTextures = Resources.LoadAll<Texture2D>("Avatars");

        if (avatarTextures == null || avatarTextures.Length == 0)
        {
            Debug.LogError("�� ������� ��������� �������� ��������");
            return;
        }

        for (int i = 0; i < avatarOptions.Length; i++)
        {
            if (i < avatarTextures.Length)
            {
                if (avatarTextures[i] == null || !avatarTextures[i].isReadable)
                {
                    Debug.LogError($"�������� ������� {i} �� �������� ��� ������");
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
            errorText.text = "������ �������� �����";
            Debug.LogError("Failed to load groups: " + task.Exception);
            yield break;
        }

        groups = task.Result;

        groupDropdown.options.Add(new Dropdown.OptionData("�������� ������"));
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
            errorText.text = "��� ��������� �����";
            Debug.LogWarning("No groups available in database");
        }

        groupDropdown.value = 0;
        groupDropdown.RefreshShownValue();

        if (loadingIndicator != null)
            loadingIndicator.SetActive(false);
    }

    public void OnRegisterButtonClick()
    {
        string error = ValidateFields();
        if (!string.IsNullOrEmpty(error))
        {
            errorText.text = error;
            return;
        }

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
            return "���������� ������� ������.";

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password) ||
            string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName))
            return "��� ������������ ���� ������ ���� ���������.";

        if (groupDropdown.value == 0)
            return "���������� ������� ������.";

        if (!IsValidEmail(email))
            return "������� ���������� email �����.";

        return null;
    }

    public void OnConfirmAvatarButtonClick()
    {
        if (selectedAvatarIndex == -1)
        {
            errorText.text = "����������, �������� ������.";
            return;
        }

        if (groups == null || groups.Count == 0 || groupDropdown.value <= 0)
        {
            errorText.text = "���������� ������� ������.";
            Debug.LogError("������: ������ �� ��������� ��� �� �������");
            return;
        }

        string email = emailInput.text.Trim();
        string password = passwordInput.text.Trim();
        string firstName = firstNameInput.text.Trim();
        string lastName = lastNameInput.text.Trim();
        string secondName = secondNameInput.text.Trim();

        int selectedGroupIndex = groupDropdown.value - 1;
        if (selectedGroupIndex < 0 || selectedGroupIndex >= groups.Count)
        {
            errorText.text = "������ ������ ������.";
            Debug.LogError($"�������� ������ ������: {selectedGroupIndex}, ����� �����: {groups.Count}");
            return;
        }

        string groupId = groups[selectedGroupIndex].Id;

        Texture2D selectedTexture = avatarTextures[selectedAvatarIndex];
        Texture2D resizedTexture = ResizeTexture(selectedTexture, maxAvatarSize, maxAvatarSize);
        byte[] avatarBytes = resizedTexture.EncodeToPNG();

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

        var registerTask = firebaseManager.RegisterStudent(
            email, password, firstName, lastName, secondName, groupId, avatarBytes);
        yield return new WaitUntil(() => registerTask.IsCompleted);

        if (registerTask.IsFaulted)
        {
            error = "������ ����������";
        }
        else
        {
            success = registerTask.Result;
            error = success ? "" : "������ �����������";
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
}