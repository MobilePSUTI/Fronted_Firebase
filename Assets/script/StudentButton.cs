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
        // «агружаем полные данные студента перед переходом
        var dbManager = FindObjectOfType<FirebaseDBManager>();
        if (dbManager != null)
        {
            UserSession.SelectedStudent = await dbManager.GetStudentDetails(studentData.Id);
        }
        else
        {
            Debug.LogError("FirebaseDBManager не найден!");
            UserSession.SelectedStudent = studentData; // »спользуем хот€ бы базовые данные
        }

        UserSession.SelectedStudent = studentData;
        SceneManager.LoadScene("PrSkillsStudent");
    }
}