using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;

public class GroupButtonCreator : MonoBehaviour
{
    public GameObject buttonPrefab;
    public Transform buttonParent;
    public GameObject loadingIndicator;
    private FirebaseDBManager firebaseManager;

    async void Start()
    {
        if (loadingIndicator != null) loadingIndicator.SetActive(true);

        try
        {
            // ��������� ���
            if (UserSession.CachedGroups.Count > 0)
            {
                CreateButtons(UserSession.CachedGroups);
                return;
            }

            firebaseManager = gameObject.AddComponent<FirebaseDBManager>();
            await firebaseManager.Initialize();

            List<Group> groups = await firebaseManager.GetAllGroups();
            UserSession.CachedGroups = groups; // ��������� � ���
            CreateButtons(groups);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"������ �������� �����: {ex.Message}");
        }
        finally
        {
            if (loadingIndicator != null) loadingIndicator.SetActive(false);
        }
    }

    void CreateButtons(List<Group> groups)
    {
        // ������� ���������� ������
        foreach (Transform child in buttonParent)
        {
            Destroy(child.gameObject);
        }

        if (groups == null || groups.Count == 0)
        {
            Debug.LogWarning("������ ����� ����");
            return;
        }

        foreach (Group group in groups)
        {
            GameObject button = Instantiate(buttonPrefab, buttonParent);
            button.GetComponentInChildren<Text>().text = group.Title;

            Group currentGroup = group;
            button.GetComponent<Button>().onClick.AddListener(() =>
            {
                OnGroupButtonClick(currentGroup.Title, currentGroup.Id);
            });
        }
    }

    void OnGroupButtonClick(string groupName, string groupId)
    {
        UserSession.SelectedGroupId = groupId;
        UserSession.SelectedGroupName = groupName;
        SceneManager.LoadScene("PrListStudents");
    }
}