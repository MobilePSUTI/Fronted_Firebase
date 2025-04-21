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
            // Проверяем кеш
            if (UserSession.CachedGroups.Count > 0)
            {
                CreateButtons(UserSession.CachedGroups);
                return;
            }

            firebaseManager = gameObject.AddComponent<FirebaseDBManager>();
            await firebaseManager.Initialize();

            List<Group> groups = await firebaseManager.GetAllGroups();
            UserSession.CachedGroups = groups; // Сохраняем в кеш
            CreateButtons(groups);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Ошибка загрузки групп: {ex.Message}");
        }
        finally
        {
            if (loadingIndicator != null) loadingIndicator.SetActive(false);
        }
    }

    void CreateButtons(List<Group> groups)
    {
        // Очистка предыдущих кнопок
        foreach (Transform child in buttonParent)
        {
            Destroy(child.gameObject);
        }

        if (groups == null || groups.Count == 0)
        {
            Debug.LogWarning("Список групп пуст");
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