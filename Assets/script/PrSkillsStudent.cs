using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;

public class PrSkillsStudent : MonoBehaviour
{
    public Text studentNameText;
    public Text studentGroupText;
    public PrSkillsManager skillsManager;

    async void Start()
    {
        if (UserSession.SelectedStudent == null)
        {
            Debug.LogError("Студент не выбран!");
            SceneManager.LoadScene("PrListStudents");
            return;
        }

        // Initialize FirebaseDBManager if needed
        var dbManager = FindObjectOfType<FirebaseDBManager>();
        if (dbManager == null)
        {
            GameObject firebaseObj = new GameObject("FirebaseDBManager");
            dbManager = firebaseObj.AddComponent<FirebaseDBManager>();
            await dbManager.Initialize();
        }

        // Load group name if needed
        if (string.IsNullOrEmpty(UserSession.SelectedStudent.GroupName))
        {
            UserSession.SelectedStudent.GroupName =
                await dbManager.GetGroupName(UserSession.SelectedStudent.GroupId);
        }

        DisplayStudentInfo(UserSession.SelectedStudent);

        // Initialize skills manager
        await skillsManager.Initialize();
    }

    private void DisplayStudentInfo(Student student)
    {
        studentNameText.text = $"{student.Last} {student.First} {student.Second}";
        studentGroupText.text = !string.IsNullOrEmpty(student.GroupName)
            ? student.GroupName
            : "Группа не указана";
    }
}