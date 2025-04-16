using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;
using System.Collections.Generic;
using Firebase.Database;

public class PrSkillsManager : MonoBehaviour
{
    public Button mainSkillsButton;
    public Button additionalSkillsButton;
    public GameObject mainSkillsPanel;
    public GameObject additionalSkillsPanel;
    public Text[] mainSkillTexts; // 7 main skills
    public Text[] additionalSkillTexts; // 7 additional skills
    public Button[] mainSkillButtons; // + main skills
    public Button[] additionalSkillButtons; // + additional skills

    private Student currentStudent;
    private Dictionary<string, int> mainSkills = new Dictionary<string, int>();
    private Dictionary<string, int> additionalSkills = new Dictionary<string, int>();
    private List<string> currentMainSkillIds = new List<string>();

    public async Task Initialize()
    {
        if (UserSession.SelectedStudent == null)
        {
            Debug.LogError("No student selected!");
            return;
        }

        currentStudent = UserSession.SelectedStudent;

        // Verify student data
        if (string.IsNullOrEmpty(currentStudent.GroupId))
        {
            Debug.LogError("Selected student has no group assigned!");
            return;
        }

        // Initialize Firebase if needed
        var dbManager = FindObjectOfType<FirebaseDBManager>();
        if (dbManager == null)
        {
            GameObject firebaseObj = new GameObject("FirebaseDBManager");
            dbManager = firebaseObj.AddComponent<FirebaseDBManager>();
            await dbManager.Initialize();
        }

        // Load group name if missing
        if (string.IsNullOrEmpty(currentStudent.GroupName))
        {
            currentStudent.GroupName = await dbManager.GetGroupName(currentStudent.GroupId);
        }

        // Get main skill IDs for student's educational program
        await LoadEducationalProgramSkills();

        // Load student skills
        await LoadStudentSkills();

        // Setup button listeners
        mainSkillsButton.onClick.AddListener(() => ShowSkillsPanel(true));
        additionalSkillsButton.onClick.AddListener(() => ShowSkillsPanel(false));

        // Setup + buttons for main skills
        for (int i = 0; i < mainSkillButtons.Length; i++)
        {
            int index = i;
            mainSkillButtons[i].onClick.AddListener(() => OnSkillButtonClicked(index, true));
        }

        // Setup + buttons for additional skills
        for (int i = 0; i < additionalSkillButtons.Length; i++)
        {
            int index = i;
            additionalSkillButtons[i].onClick.AddListener(() => OnSkillButtonClicked(index, false));
        }

        // Show main skills by default
        ShowSkillsPanel(true);
    }

    private async Task LoadEducationalProgramSkills()
    {
        try
        {
            // Verify student has a group ID
            if (string.IsNullOrEmpty(currentStudent.GroupId))
            {
                Debug.LogError("Student has no group ID assigned!");
                currentMainSkillIds = GetDefaultSkillIds();
                return;
            }

            // Load group data - using correct path "6/data"
            DataSnapshot groupSnapshot = await FirebaseDatabase.DefaultInstance
                .GetReference("6")  // Groups table is node "6"
                .Child("data")     // Then "data"
                .Child(currentStudent.GroupId)
                .GetValueAsync();

            if (!groupSnapshot.Exists)
            {
                Debug.LogError($"Group {currentStudent.GroupId} not found in database!");
                currentMainSkillIds = GetDefaultSkillIds();
                return;
            }

            // Get educational program ID
            string programId = groupSnapshot.Child("educational_program_id").Value?.ToString();
            if (string.IsNullOrEmpty(programId))
            {
                Debug.LogError("No educational program ID found for group!");
                currentMainSkillIds = GetDefaultSkillIds();
                return;
            }

            // Load educational program - using correct path "4/data"
            DataSnapshot programSnapshot = await FirebaseDatabase.DefaultInstance
                .GetReference("4")  // Educational programs table is node "4"
                .Child("data")      // Then "data"
                .Child(programId)
                .GetValueAsync();

            if (!programSnapshot.Exists)
            {
                Debug.LogError($"Educational program {programId} not found!");
                currentMainSkillIds = GetDefaultSkillIds();
                return;
            }

            // Load main skills
            currentMainSkillIds = new List<string>();
            var mainSkillIds = programSnapshot.Child("main_skills").Value as List<object>;

            if (mainSkillIds != null && mainSkillIds.Count > 0)
            {
                foreach (var skillId in mainSkillIds)
                {
                    currentMainSkillIds.Add(skillId.ToString());
                }
            }
            else
            {
                Debug.LogWarning("No main skills defined in educational program");
            }

            // Ensure we have exactly 7 skills
            while (currentMainSkillIds.Count < 7)
            {
                currentMainSkillIds.Add((currentMainSkillIds.Count + 1).ToString());
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error loading educational program skills: {ex.Message}");
            currentMainSkillIds = GetDefaultSkillIds();
        }
    }

    private List<string> GetDefaultSkillIds()
    {
        return new List<string> { "1", "2", "3", "4", "5", "6", "7" };
    }

    private async Task LoadStudentSkills()
    {
        try
        {
            // Initialize all skills with default values first
            for (int i = 0; i < 7; i++)
            {
                string skillId = i < currentMainSkillIds.Count ? currentMainSkillIds[i] : (i + 1).ToString();
                mainSkills[$"skill{i + 1}"] = 0;
                string skillName = await GetSkillName(skillId);
                mainSkillTexts[i].text = $"{skillName}: 0";
            }

            for (int i = 1; i <= 7; i++)
            {
                additionalSkills[$"add_skill{i}"] = 0;
                string addSkillName = await GetAdditionalSkillName(i);
                additionalSkillTexts[i - 1].text = $"{addSkillName}: 0";
            }

            // Get student's skills from Firebase with correct path
            DataSnapshot snapshot = await FirebaseDatabase.DefaultInstance
                .GetReference("16")  // Start with node "16"
                .Child("data")       // Then "data"
                .Child(currentStudent.Id)  // Then student ID
                .GetValueAsync();

            if (snapshot.Exists)
            {
                // Load main skills
                var mainSkillsData = snapshot.Child("main_skills").Value as Dictionary<string, object>;
                if (mainSkillsData != null)
                {
                    for (int i = 0; i < 7; i++)
                    {
                        string skillId = i < currentMainSkillIds.Count ? currentMainSkillIds[i] : (i + 1).ToString();
                        if (mainSkillsData.ContainsKey(skillId))
                        {
                            mainSkills[$"skill{i + 1}"] = int.Parse(mainSkillsData[skillId].ToString());
                            string skillName = await GetSkillName(skillId);
                            mainSkillTexts[i].text = $"{skillName}: {mainSkills[$"skill{i + 1}"]}";
                        }
                    }
                }

                // Load additional skills
                var additionalSkillsData = snapshot.Child("additional_skills").Value as Dictionary<string, object>;
                if (additionalSkillsData != null)
                {
                    for (int i = 1; i <= 7; i++)
                    {
                        string skillKey = (100 + i).ToString();
                        if (additionalSkillsData.ContainsKey(skillKey))
                        {
                            additionalSkills[$"add_skill{i}"] = int.Parse(additionalSkillsData[skillKey].ToString());
                            string skillName = await GetAdditionalSkillName(i);
                            additionalSkillTexts[i - 1].text = $"{skillName}: {additionalSkills[$"add_skill{i}"]}";
                        }
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error loading skills: {ex.Message}");
        }
    }

    private async Task<string> GetSkillName(string skillId)
    {
        try
        {
            DataSnapshot snapshot = await FirebaseDatabase.DefaultInstance
                .GetReference("skills")
                .Child(skillId)
                .Child("title")
                .GetValueAsync();

            return snapshot?.Value?.ToString() ?? $"Skill {skillId}";
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error loading skill name: {ex.Message}");
            return $"Skill {skillId}";
        }
    }

    private async Task<string> GetAdditionalSkillName(int index)
    {
        try
        {
            int skillId = 100 + index;
            DataSnapshot snapshot = await FirebaseDatabase.DefaultInstance
                .GetReference("skills")
                .Child(skillId.ToString())
                .Child("title")
                .GetValueAsync();

            return snapshot?.Value?.ToString() ?? $"Additional Skill {index}";
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error loading additional skill name: {ex.Message}");
            return $"Additional Skill {index}";
        }
    }

    private void ShowSkillsPanel(bool showMain)
    {
        mainSkillsPanel.SetActive(showMain);
        additionalSkillsPanel.SetActive(!showMain);
    }

    private async void OnSkillButtonClicked(int skillIndex, bool isMainSkill)
    {
        try
        {
            string skillPath;
            string skillKey;
            int newValue;

            if (isMainSkill)
            {
                if (skillIndex >= currentMainSkillIds.Count)
                {
                    Debug.LogError($"Main skill index out of range: {skillIndex}");
                    return;
                }

                string skillId = currentMainSkillIds[skillIndex];
                skillKey = $"main_skills/{skillId}";
                newValue = mainSkills[$"skill{skillIndex + 1}"] + 1;
                mainSkills[$"skill{skillIndex + 1}"] = newValue;
                string skillName = await GetSkillName(skillId);
                mainSkillTexts[skillIndex].text = $"{skillName}: {newValue}";
            }
            else
            {
                skillKey = $"additional_skills/{(101 + skillIndex)}";
                newValue = additionalSkills[$"add_skill{skillIndex + 1}"] + 1;
                additionalSkills[$"add_skill{skillIndex + 1}"] = newValue;
                string skillName = await GetAdditionalSkillName(skillIndex + 1);
                additionalSkillTexts[skillIndex].text = $"{skillName}: {newValue}";
            }

            // Update in Firebase with correct path
            await FirebaseDatabase.DefaultInstance
                .GetReference("16")  // Start with node "16"
                .Child("data")       // Then "data"
                .Child(currentStudent.Id)  // Then student ID
                .Child(skillKey)    // Then skill path
                .SetValueAsync(newValue);

            Debug.Log("Skill updated successfully");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error updating skill: {ex.Message}");
        }
    }
}