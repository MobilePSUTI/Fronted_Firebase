using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections;
using UnityEngine.SceneManagement;

public class StudentListManager : MonoBehaviour
{
    public GameObject studentPrefab;
    public Transform studentParent;

    async void Start()
    {
        var dbManager = gameObject.AddComponent<FirebaseDBManager>();
        await dbManager.Initialize();

        var students = await dbManager.GetStudentsByGroup(UserSession.SelectedGroupId);

        if (students != null && students.Count > 0)
        {
            CreateStudentList(students);
        }
        else
        {
            Debug.LogWarning("Нет студентов в выбранной группе");
        }
    }

    public async void CreateStudentList(List<Student> students)
    {
        var dbManager = FindObjectOfType<FirebaseDBManager>();

        foreach (Student student in students)
        {
            // Заранее загружаем GroupName
            student.GroupName = await dbManager.GetGroupName(student.GroupId);

            GameObject studentUI = Instantiate(studentPrefab, studentParent);
            Text studentText = studentUI.GetComponentInChildren<Text>();
            studentText.text = $"{student.Last} {student.First[0]}.{student.Second[0]}.";

            StudentButton studentButton = studentUI.GetComponent<StudentButton>();
            studentButton.SetStudentData(student);
        }
    }
}