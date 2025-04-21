using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;
using System;
using TMPro; // ��� TextMeshPro, ���� ������������

public class StudentListManager : MonoBehaviour
{
    public GameObject studentPrefab;
    public Transform studentParent;
    public GameObject loadingIndicator;
    public Text groupNameText; // ������ �� ��������� Text ��� ����������� �������� ������
    public TextMeshProUGUI groupNameTextTMP; // ������ �� TextMeshProUGUI (���� ������������)

    private FirebaseDBManager _dbManager;

    private void Awake()
    {
        // �������� ��� ������� ��������� FirebaseDBManager
        if (FirebaseDBManager.Instance == null)
        {
            GameObject firebaseManager = new GameObject("FirebaseManager");
            firebaseManager.AddComponent<FirebaseDBManager>();
            DontDestroyOnLoad(firebaseManager);
        }
        _dbManager = FirebaseDBManager.Instance;
    }

    async void Start()
    {
        if (loadingIndicator != null)
            loadingIndicator.SetActive(true);

        try
        {
            // ��������, ��� Firebase ���������������
            await _dbManager.Initialize();

            // �������� �������� ������ �� ���� ������
            string groupName = await _dbManager.GetGroupName(UserSession.SelectedGroupId);

            // ��������� ��������� ������� � ��������� ������
            if (groupNameText != null)
            {
                groupNameText.text = string.IsNullOrEmpty(groupName) ? "������ �� �������" : $"������: {groupName}";
            }
            else if (groupNameTextTMP != null)
            {
                groupNameTextTMP.text = string.IsNullOrEmpty(groupName) ? "������ �� �������" : $"������: {groupName}";
            }
            else
            {
                Debug.LogWarning("�� ������ ��������� ������ ��� ����������� �������� ������!");
            }

            // ��������� ��� � UserSession
            if (UserSession.CachedStudents.TryGetValue(UserSession.SelectedGroupId, out var cachedStudents))
            {
                await CreateStudentList(cachedStudents);
                if (loadingIndicator != null)
                    loadingIndicator.SetActive(false);
                return;
            }

            // ��������� ���������
            var students = await _dbManager.GetStudentsByGroup(UserSession.SelectedGroupId);

            if (students != null && students.Count > 0)
            {
                // ��������� � ��� UserSession
                UserSession.CachedStudents[UserSession.SelectedGroupId] = students;
                await CreateStudentList(students);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"������ ��� �������� ������: {ex.Message}");
        }
        finally
        {
            if (loadingIndicator != null)
                loadingIndicator.SetActive(false);
        }
    }

    private async Task CreateStudentList(List<Student> students)
    {
        if (students == null)
        {
            Debug.LogError("������ ��������� ����� null");
            return;
        }

        // ������� ���������� ������
        foreach (Transform child in studentParent)
        {
            Destroy(child.gameObject);
        }

        foreach (Student student in students)
        {
            if (student == null) continue;

            try
            {
                // �������� �������� ������ ��� ��������
                student.GroupName = await _dbManager.GetGroupName(student.GroupId);

                // ������� UI ������� ��������
                GameObject studentUI = Instantiate(studentPrefab, studentParent);
                Text studentText = studentUI.GetComponentInChildren<Text>();
                studentText.text = $"{student.Last} {student.First[0]}.{student.Second[0]}.";

                // ����������� ������
                StudentButton studentButton = studentUI.GetComponent<StudentButton>();
                if (studentButton != null)
                {
                    studentButton.SetStudentData(student);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"������ ��� �������� �������� ��������: {ex.Message}");
            }
        }
    }
}