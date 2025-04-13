using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;

public class GroupButtonCreator : MonoBehaviour
{
    public GameObject buttonPrefab;
    public Transform buttonParent;
    private FirebaseDBManager firebaseManager;

    async void Start()
    {
        firebaseManager = gameObject.AddComponent<FirebaseDBManager>();
        await firebaseManager.Initialize();

        var task = firebaseManager.GetAllGroups();
        await task;

        if (task.IsCompletedSuccessfully)
        {
            CreateButtons(task.Result);
        }
        else
        {
            Debug.LogError("������ ��� �������� �����");
        }
    }

    public void CreateButtons(List<Group> groups)
    {
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
                OnGroupButtonClick(currentGroup.Title, currentGroup.Id); // Id ������ string
            });
        }
    }

    void OnGroupButtonClick(string groupName, string groupId) // �������� �� string
    {
        Debug.Log($"������� ������: {groupName} (ID: {groupId})");

        UserSession.SelectedGroupId = groupId;
        UserSession.SelectedGroupName = groupName;

        SceneManager.LoadScene("PrListStudents");
    }

}