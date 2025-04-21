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
        var dbManager = FindObjectOfType<FirebaseDBManager>();
        if (dbManager == null)
        {
            Debug.LogError("FirebaseDBManager not found!");
            return;
        }

        // Load complete student data
        UserSession.SelectedStudent = await dbManager.GetStudentDetails(studentData.Id);

        // Verify critical data exists
        if (string.IsNullOrEmpty(UserSession.SelectedStudent.GroupId))
        {
            Debug.LogError("Student has no group assigned!");
            return;
        }

        // Load group name if missing
        if (string.IsNullOrEmpty(UserSession.SelectedStudent.GroupName))
        {
            UserSession.SelectedStudent.GroupName =
                await dbManager.GetGroupName(UserSession.SelectedStudent.GroupId);
        }

        SceneManager.LoadScene("PrSkillsStudent");
    }
}