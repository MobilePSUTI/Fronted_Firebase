using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Threading.Tasks;
using System.Collections.Generic;
using Firebase.Database;
using System;

public class PrSkillsManager : MonoBehaviour
{
    public Button mainSkillsButton;
    public Button additionalSkillsButton;
    public GameObject mainSkillsPanel;
    public GameObject additionalSkillsPanel;
    public TextMeshProUGUI[] mainSkillTexts;
    public TextMeshProUGUI[] additionalSkillTexts;
    public Button[] mainSkillButtons;
    public Button[] additionalSkillButtons;

    private Student currentStudent; 
    private string currentTeacherId; 
    private Dictionary<string, int> mainSkills = new Dictionary<string, int>();
    private Dictionary<string, int> additionalSkills = new Dictionary<string, int>();
    private List<string> currentMainSkillIds = new List<string>();
    private Dictionary<string, string> skillNames = new Dictionary<string, string>();

    private DateTime lastClickTime;
    private int teacherClickCount;
    private const int MAX_POINTS_PER_DAY = 3;

    public async Task Initialize()
    {
        if (UserSession.CurrentUser == null || UserSession.CurrentUser.Role != "teacher")
        {
            Debug.LogError("No teacher logged in!");
            return;
        }
        if (UserSession.SelectedStudent == null)
        {
            Debug.LogError("No student selected!");
            return;
        }

        currentTeacherId = UserSession.CurrentUser.Id;
        currentStudent = UserSession.SelectedStudent;

        if (string.IsNullOrEmpty(currentStudent.GroupId))
        {
            Debug.LogError("Selected student has no group assigned!");
            return;
        }

        var dbManager = FindObjectOfType<FirebaseDBManager>();
        if (dbManager == null)
        {
            GameObject firebaseObj = new GameObject("FirebaseDBManager");
            dbManager = firebaseObj.AddComponent<FirebaseDBManager>();
            await dbManager.Initialize();
        }

        if (string.IsNullOrEmpty(currentStudent.GroupName))
        {
            currentStudent.GroupName = await dbManager.GetGroupName(currentStudent.GroupId);
        }

        await LoadEducationalProgramSkills();
        await LoadStudentSkills();
        await LoadTeacherClickCount();

        mainSkillsButton.onClick.AddListener(() => ShowSkillsPanel(true));
        additionalSkillsButton.onClick.AddListener(() => ShowSkillsPanel(false));

        for (int i = 0; i < mainSkillButtons.Length; i++)
        {
            int index = i;
            mainSkillButtons[i].onClick.AddListener(() => OnSkillButtonClicked(index, true));
        }

        for (int i = 0; i < additionalSkillButtons.Length; i++)
        {
            int index = i;
            additionalSkillButtons[i].onClick.AddListener(() => OnSkillButtonClicked(index, false));
        }

        ShowSkillsPanel(true);
        UpdateAllButtonsState();
    }

    private void UpdateAllButtonsState()
    {
        DateTime today = DateTime.Now.Date;
        bool limitReached = teacherClickCount >= MAX_POINTS_PER_DAY && lastClickTime.Date == today;

        for (int i = 0; i < mainSkillButtons.Length; i++)
        {
            mainSkillButtons[i].interactable = !limitReached;
            if (limitReached)
            {
                mainSkillTexts[i].text += " (Лимит учителя)";
            }
        }

        for (int i = 0; i < additionalSkillButtons.Length; i++)
        {
            additionalSkillButtons[i].interactable = !limitReached;
            if (limitReached)
            {
                additionalSkillTexts[i].text += " (Лимит учителя)";
            }
        }
    }

    private async Task LoadEducationalProgramSkills()
    {
        try
        {
            if (string.IsNullOrEmpty(currentStudent.GroupId))
            {
                Debug.LogError("Student has no group ID assigned!");
                currentMainSkillIds = GetDefaultSkillIds();
                return;
            }

            DataSnapshot groupSnapshot = await FirebaseDatabase.DefaultInstance
            .GetReference("6/data")
            .Child(currentStudent.GroupId)
            .GetValueAsync();

            if (!groupSnapshot.Exists) { Debug.LogError($"Group {currentStudent.GroupId} not found in database!"); currentMainSkillIds = GetDefaultSkillIds(); return; }

            string programId = groupSnapshot.Child("educational_program_id").Value?.ToString(); if (string.IsNullOrEmpty(programId)) { Debug.LogError("No educational program ID found for group!"); currentMainSkillIds = GetDefaultSkillIds(); return; }

            DataSnapshot programSnapshot = await FirebaseDatabase.DefaultInstance.GetReference("4/data").Child(programId).GetValueAsync();

            if (!programSnapshot.Exists) { Debug.LogError($"Educational program {programId} not found!"); currentMainSkillIds = GetDefaultSkillIds(); return; }

            currentMainSkillIds = new List<string>(); var mainSkillIds = programSnapshot.Child("main_skills").Value as List<object>;

            if (mainSkillIds != null && mainSkillIds.Count > 0) { foreach (var skillId in mainSkillIds) { currentMainSkillIds.Add(skillId.ToString()); } } else { Debug.LogWarning("No main skills defined in educational program"); }

            while (currentMainSkillIds.Count < 7)
            {
                currentMainSkillIds.Add((currentMainSkillIds.Count + 1).ToString());
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error loading educational program skills: {ex.Message}\n{ex.StackTrace}");
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
            // Initialize main skills
            for (int i = 0; i < 7; i++)
            {
                string skillId = i < currentMainSkillIds.Count ? currentMainSkillIds[i] : (i + 1).ToString();
                mainSkills[$"skill{i + 1}"] = 0;
                string skillName = await GetSkillName(skillId);
                skillNames[skillId] = skillName;
                mainSkillTexts[i].text = $"{skillName}: 0";
            }

            // Initialize additional skills
            for (int i = 0; i < 7; i++)
            {
                string skillId = (101 + i).ToString();
                additionalSkills[$"add_skill{i + 1}"] = 0;
                string addSkillName = await GetSkillName(skillId);
                skillNames[skillId] = addSkillName;
                additionalSkillTexts[i].text = $"{addSkillName}: 0";
            }

            // Load from Firebase
            DataSnapshot snapshot = await FirebaseDatabase.DefaultInstance
            .GetReference("16/data")
            .Child(currentStudent.Id)
            .GetValueAsync();

            if (snapshot.Exists)
            {
                // Main skills
                var mainSkillsData = snapshot.Child("main_skills").Value as Dictionary<string, object>;
                if (mainSkillsData != null)
                {
                    for (int i = 0; i < 7; i++)
                    {
                        string skillId = i < currentMainSkillIds.Count ? currentMainSkillIds[i] : (i + 1).ToString();
                        if (mainSkillsData.ContainsKey(skillId))
                        {
                            mainSkills[$"skill{i + 1}"] = int.Parse(mainSkillsData[skillId].ToString());
                            string skillName = skillNames[skillId];
                            mainSkillTexts[i].text = $"{skillName}: {mainSkills[$"skill{i + 1}"]}";
                        }
                    }
                }

                // Additional skills
                var additionalSkillsData = snapshot.Child("additional_skills").Value as Dictionary<string, object>;
                if (additionalSkillsData != null)
                {
                    for (int i = 0; i < 7; i++)
                    {
                        string skillId = (101 + i).ToString();
                        if (additionalSkillsData.ContainsKey(skillId))
                        {
                            additionalSkills[$"add_skill{i + 1}"] = int.Parse(additionalSkillsData[skillId].ToString());
                            string skillName = skillNames[skillId];
                            additionalSkillTexts[i].text = $"{skillName}: {additionalSkills[$"add_skill{i + 1}"]}";
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error loading skills: {ex.Message}\n{ex.StackTrace}");
        }
    }

    private async Task<string> GetSkillName(string skillId)
    {
        try
        {
            if (skillNames.ContainsKey(skillId))
            {
                return skillNames[skillId];
            }

            DataSnapshot snapshot = await FirebaseDatabase.DefaultInstance
            .GetReference("11/data")
            .GetValueAsync();

            if (snapshot == null || !snapshot.Exists)
            {
                skillNames[skillId] = $"Skill {skillId}";
                return $"Skill {skillId}";
            }

            foreach (DataSnapshot skillSnapshot in snapshot.Children)
            {
                string currentSkillId = skillSnapshot.Child("id").Value?.ToString();
                if (currentSkillId == skillId)
                {
                    string skillName = skillSnapshot.Child("title").Value?.ToString();
                    if (!string.IsNullOrEmpty(skillName))
                    {
                        skillNames[skillId] = skillName;
                        return skillName;
                    }
                }
            }

            skillNames[skillId] = $"Skill {skillId}";
            return $"Skill {skillId}";
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error loading skill name for ID {skillId}: {ex.Message}\n{ex.StackTrace}");
            return $"Skill {skillId}";
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
            // Check teacher point limit
            DateTime now = DateTime.Now;
            DateTime today = now.Date;
            if (teacherClickCount >= MAX_POINTS_PER_DAY && lastClickTime.Date == today)
            {
                ShowLimitExceededMessage(isMainSkill, skillIndex);
                return;
            }

            string skillPath;
            string skillKey;
            int newValue;
            string skillId;

            if (isMainSkill)
            {
                skillId = currentMainSkillIds[skillIndex];
                skillKey = $"main_skills/{skillId}";
                newValue = mainSkills[$"skill{skillIndex + 1}"] + 1;
                mainSkills[$"skill{skillIndex + 1}"] = newValue;
                string skillName = skillNames[skillId];
                mainSkillTexts[skillIndex].text = $"{skillName}: {newValue}";
            }
            else
            {
                skillId = (101 + skillIndex).ToString();
                skillKey = $"additional_skills/{skillId}";
                newValue = additionalSkills[$"add_skill{skillIndex + 1}"] + 1;
                additionalSkills[$"add_skill{skillIndex + 1}"] = newValue;
                string skillName = skillNames[skillId];
                additionalSkillTexts[skillIndex].text = $"{skillName}: {newValue}";
            }

            // Update teacher click count
            teacherClickCount++;
            lastClickTime = now;

            // Update Firebase: student skills
            await FirebaseDatabase.DefaultInstance
            .GetReference("16/data")
            .Child(currentStudent.Id)
            .Child(skillKey)
            .SetValueAsync(newValue);

            // Save teacher click count
            await SaveTeacherClickCount();

            // Check if limit reached
            if (teacherClickCount >= MAX_POINTS_PER_DAY)
            {
                UpdateAllButtonsState();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error updating skill: {ex.Message}\n{ex.StackTrace}");
        }
    }

    private void ShowLimitExceededMessage(bool isMainSkill, int skillIndex)
    {
        if (isMainSkill)
        {
            mainSkillTexts[skillIndex].text += " (Лимит учителя)";
        }
        else
        {
            additionalSkillTexts[skillIndex].text += " (Лимит учителя)";
        }
        UpdateAllButtonsState();
    }

    private async Task LoadTeacherClickCount()
    {
        try
        {
            string today = DateTime.Now.ToString("yyyy-MM-dd");
            DataSnapshot snapshot = await FirebaseDatabase.DefaultInstance
            .GetReference("skill_clicks")
            .Child(today)
            .Child(currentStudent.Id)
            .Child(currentTeacherId)
            .GetValueAsync();

            if (snapshot.Exists)
            {
                teacherClickCount = int.Parse(snapshot.Value.ToString());
                lastClickTime = DateTime.Now;
            }
            else
            {
                teacherClickCount = 0;
                lastClickTime = DateTime.Now;
            }

            Debug.Log($"[PrSkillsManager] Loaded teacher click count: {teacherClickCount} for teacher {currentTeacherId}, student {currentStudent.Id}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error loading teacher click count: {ex.Message}\n{ex.StackTrace}");
            teacherClickCount = 0;
            lastClickTime = DateTime.Now;
        }
    }

    private async Task SaveTeacherClickCount()
    {
        try
        {
            string today = DateTime.Now.ToString("yyyy-MM-dd");
            await FirebaseDatabase.DefaultInstance
            .GetReference("skill_clicks")
            .Child(today)
            .Child(currentStudent.Id)
            .Child(currentTeacherId)
            .SetValueAsync(teacherClickCount);

            Debug.Log($"[PrSkillsManager] Saved teacher click count: {teacherClickCount} for teacher {currentTeacherId}, student {currentStudent.Id}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error saving teacher click count: {ex.Message}\n{ex.StackTrace}");
        }
    }

    private async void OnDestroy()
    {
        try
        {
            await SaveTeacherClickCount();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error in OnDestroy: {ex.Message}\n{ex.StackTrace}");
        }
    }
}