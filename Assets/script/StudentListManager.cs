using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;

public class StudentListManager : MonoBehaviour
{
    public GameObject studentPrefab;
    public Transform studentParent;
    public GameObject loadingIndicator;

    async void Start()
    {
        if (loadingIndicator != null) loadingIndicator.SetActive(true);

        try
        {
            // ��������� ���
            if (UserSession.CachedStudents.TryGetValue(UserSession.SelectedGroupId, out var cachedStudents))
            {
                CreateStudentList(cachedStudents);
                return;
            }

            var dbManager = gameObject.AddComponent<FirebaseDBManager>();
            await dbManager.Initialize();

            var students = await dbManager.GetStudentsByGroup(UserSession.SelectedGroupId);

            if (students != null && students.Count > 0)
            {
                // ��������� � ���
                UserSession.CachedStudents[UserSession.SelectedGroupId] = students;
                CreateStudentList(students);
            }
            else
            {
                Debug.LogWarning("��� ��������� � ��������� ������");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"������ �������� ���������: {ex.Message}");
        }
        finally
        {
            if (loadingIndicator != null) loadingIndicator.SetActive(false);
        }
    }

    async void CreateStudentList(List<Student> students)
    {
        var dbManager = FindObjectOfType<FirebaseDBManager>();

        foreach (Student student in students)
        {
            student.GroupName = await dbManager.GetGroupName(student.GroupId);

            GameObject studentUI = Instantiate(studentPrefab, studentParent);
            Text studentText = studentUI.GetComponentInChildren<Text>();
            studentText.text = $"{student.Last} {student.First[0]}.{student.Second[0]}.";

            StudentButton studentButton = studentUI.GetComponent<StudentButton>();
            studentButton.SetStudentData(student);
        }
    }
}