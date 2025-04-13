using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;

public class PrSkillsStudent : MonoBehaviour
{
    public Text studentNameText;
    public Text studentGroupText;

    async void Start()
    {
        if (UserSession.SelectedStudent == null)
        {
            Debug.LogError("������� �� ������!");
            SceneManager.LoadScene("PrListStudents");
            return;
        }

        // ���� FirebaseDBManager � �����
        var dbManager = FindObjectOfType<FirebaseDBManager>();

        // ���� �� ������, ������ �����
        if (dbManager == null)
        {
            GameObject firebaseObj = new GameObject("FirebaseDBManager");
            dbManager = firebaseObj.AddComponent<FirebaseDBManager>();
            await dbManager.Initialize(); // ��������������
        }

        // ��������� GroupName, ���� ��� ���
        if (string.IsNullOrEmpty(UserSession.SelectedStudent.GroupName))
        {
            UserSession.SelectedStudent.GroupName =
                await dbManager.GetGroupName(UserSession.SelectedStudent.GroupId);
        }

        DisplayStudentInfo(UserSession.SelectedStudent);
    }

    private void DisplayStudentInfo(Student student)
    {
        studentNameText.text = $"{student.Last} {student.First} {student.Second}";
        studentGroupText.text = !string.IsNullOrEmpty(student.GroupName)
            ? student.GroupName
            : "������ �� �������";
    }
}