using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class StudentButton : MonoBehaviour
{
    private Student studentData;

    public void SetStudentData(Student student)
    {
        studentData = student;
        GetComponent<Button>().onClick.AddListener(OnStudentSelected);
    }

    private async void OnStudentSelected()
    {
        // ��������� ������ ������ �������� ����� ���������
        var dbManager = FindObjectOfType<FirebaseDBManager>();
        if (dbManager != null)
        {
            UserSession.SelectedStudent = await dbManager.GetStudentDetails(studentData.Id);
        }
        else
        {
            Debug.LogError("FirebaseDBManager �� ������!");
            UserSession.SelectedStudent = studentData; // ���������� ���� �� ������� ������
        }

        UserSession.SelectedStudent = studentData;
        SceneManager.LoadScene("PrSkillsStudent");
    }
}