using UnityEngine;
using UnityEngine.UI;
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
    public Text[] mainSkillTexts; // 7 main skills
    public Text[] additionalSkillTexts; // 7 additional skills
    public Button[] mainSkillButtons; // + main skills
    public Button[] additionalSkillButtons; // + additional skills

    private Student currentStudent;
    private Dictionary<string, int> mainSkills = new Dictionary<string, int>();
    private Dictionary<string, int> additionalSkills = new Dictionary<string, int>();
    private List<string> currentMainSkillIds = new List<string>();

    // Система ограничения нажатий
    private Dictionary<string, DateTime> lastClickTimes = new Dictionary<string, DateTime>();
    private Dictionary<string, int> clickCounts = new Dictionary<string, int>();
    private const int MAX_CLICKS_PER_DAY = 5;
    private string currentSkillKey = "";

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

        // Load click counts from Firebase
        await LoadClickCounts();

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

        // Обновляем состояние всех кнопок
        UpdateAllButtonsState();
    }

    private void UpdateAllButtonsState()
    {
        DateTime today = DateTime.Now.Date;

        // Обновляем состояние кнопок основных навыков
        for (int i = 0; i < mainSkillButtons.Length; i++)
        {
            string skillId = i < currentMainSkillIds.Count ? currentMainSkillIds[i] : (i + 1).ToString();
            string clickKey = $"{currentStudent.Id}_{skillId}";

            if (clickCounts.ContainsKey(clickKey) &&
                lastClickTimes[clickKey].Date == today &&
                clickCounts[clickKey] >= MAX_CLICKS_PER_DAY)
            {
                mainSkillButtons[i].interactable = false;
                mainSkillTexts[i].text += " (Лимит)";
            }
        }

        // Обновляем состояние кнопок дополнительных навыков
        for (int i = 0; i < additionalSkillButtons.Length; i++)
        {
            string skillId = (101 + i).ToString();
            string clickKey = $"{currentStudent.Id}_{skillId}";

            if (clickCounts.ContainsKey(clickKey) &&
                lastClickTimes[clickKey].Date == today &&
                clickCounts[clickKey] >= MAX_CLICKS_PER_DAY)
            {
                additionalSkillButtons[i].interactable = false;
                additionalSkillTexts[i].text += " (Лимит)";
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
                .GetReference("6")
                .Child("data")
                .Child(currentStudent.GroupId)
                .GetValueAsync();

            if (!groupSnapshot.Exists)
            {
                Debug.LogError($"Group {currentStudent.GroupId} not found in database!");
                currentMainSkillIds = GetDefaultSkillIds();
                return;
            }

            string programId = groupSnapshot.Child("educational_program_id").Value?.ToString();
            if (string.IsNullOrEmpty(programId))
            {
                Debug.LogError("No educational program ID found for group!");
                currentMainSkillIds = GetDefaultSkillIds();
                return;
            }

            DataSnapshot programSnapshot = await FirebaseDatabase.DefaultInstance
                .GetReference("4")
                .Child("data")
                .Child(programId)
                .GetValueAsync();

            if (!programSnapshot.Exists)
            {
                Debug.LogError($"Educational program {programId} not found!");
                currentMainSkillIds = GetDefaultSkillIds();
                return;
            }

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

            while (currentMainSkillIds.Count < 7)
            {
                currentMainSkillIds.Add((currentMainSkillIds.Count + 1).ToString());
            }
        }
        catch (Exception ex)
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

            DataSnapshot snapshot = await FirebaseDatabase.DefaultInstance
                .GetReference("16")
                .Child("data")
                .Child(currentStudent.Id)
                .GetValueAsync();

            if (snapshot.Exists)
            {
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
                        }
                    }
                }
            }
        }
        catch (Exception ex)
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
        catch (Exception ex)
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
        catch (Exception ex)
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
            string skillId;

            if (isMainSkill)
            {
                if (skillIndex >= currentMainSkillIds.Count)
                {
                    Debug.LogError($"Main skill index out of range: {skillIndex}");
                    return;
                }

                skillId = currentMainSkillIds[skillIndex];
                skillKey = $"main_skills/{skillId}";
                newValue = mainSkills[$"skill{skillIndex + 1}"] + 1;
                mainSkills[$"skill{skillIndex + 1}"] = newValue;
                string skillName = await GetSkillName(skillId);
                mainSkillTexts[skillIndex].text = $"{skillName}: {newValue}";
            }
            else
            {
                skillId = (101 + skillIndex).ToString();
                skillKey = $"additional_skills/{skillId}";
                newValue = additionalSkills[$"add_skill{skillIndex + 1}"] + 1;
                additionalSkills[$"add_skill{skillIndex + 1}"] = newValue;
                string skillName = await GetAdditionalSkillName(skillIndex + 1);
                additionalSkillTexts[skillIndex].text = $"{skillName}: {newValue}";
            }

            // Проверяем ограничение нажатий
            currentSkillKey = $"{currentStudent.Id}_{skillId}";
            DateTime now = DateTime.Now;
            DateTime today = now.Date;

            if (!lastClickTimes.ContainsKey(currentSkillKey) || lastClickTimes[currentSkillKey].Date < today)
            {
                clickCounts[currentSkillKey] = 1;
                lastClickTimes[currentSkillKey] = now;
            }
            else
            {
                if (clickCounts.ContainsKey(currentSkillKey) && clickCounts[currentSkillKey] >= MAX_CLICKS_PER_DAY)
                {
                    Debug.LogWarning($"Daily click limit reached (5/day) for skill {skillId}");
                    ShowLimitExceededMessage(isMainSkill, skillIndex);
                    return;
                }

                clickCounts[currentSkillKey] = clickCounts.ContainsKey(currentSkillKey) ?
                    clickCounts[currentSkillKey] + 1 : 1;
                lastClickTimes[currentSkillKey] = now;
            }

            // Обновляем состояние кнопки
            if (isMainSkill)
            {
                if (clickCounts[currentSkillKey] >= MAX_CLICKS_PER_DAY)
                {
                    mainSkillButtons[skillIndex].interactable = false;
                    mainSkillTexts[skillIndex].text += " (Лимит)";
                }
            }
            else
            {
                if (clickCounts[currentSkillKey] >= MAX_CLICKS_PER_DAY)
                {
                    additionalSkillButtons[skillIndex].interactable = false;
                    additionalSkillTexts[skillIndex].text += " (Лимит)";
                }
            }

            // Обновляем в Firebase
            await FirebaseDatabase.DefaultInstance
                .GetReference("16")
                .Child("data")
                .Child(currentStudent.Id)
                .Child(skillKey)
                .SetValueAsync(newValue);

            // Сохраняем счетчик нажатий
            await SaveClickCount(currentSkillKey, clickCounts[currentSkillKey]);

            Debug.Log($"Skill updated. Clicks today: {clickCounts[currentSkillKey]}/{MAX_CLICKS_PER_DAY}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error updating skill: {ex.Message}");
        }
    }

    private void ShowLimitExceededMessage(bool isMainSkill, int skillIndex)
    {
        if (isMainSkill)
        {
            mainSkillButtons[skillIndex].interactable = false;
            mainSkillTexts[skillIndex].text += " (Лимит)";
        }
        else
        {
            additionalSkillButtons[skillIndex].interactable = false;
            additionalSkillTexts[skillIndex].text += " (Лимит)";
        }
    }

    private async Task LoadClickCounts()
    {
        try
        {
            string today = DateTime.Now.ToString("yyyy-MM-dd");
            DataSnapshot snapshot = await FirebaseDatabase.DefaultInstance
                .GetReference("skill_clicks")
                .Child(today)
                .Child(currentStudent.Id)
                .GetValueAsync();

            if (snapshot.Exists)
            {
                foreach (DataSnapshot skillSnapshot in snapshot.Children)
                {
                    string skillId = skillSnapshot.Key;
                    string clickKey = $"{currentStudent.Id}_{skillId}";
                    clickCounts[clickKey] = int.Parse(skillSnapshot.Value.ToString());
                    lastClickTimes[clickKey] = DateTime.Now;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error loading click counts: {ex.Message}");
        }
    }

    private async Task SaveClickCount(string clickKey, int count)
    {
        try
        {
            string[] parts = clickKey.Split('_');
            if (parts.Length == 2)
            {
                string today = DateTime.Now.ToString("yyyy-MM-dd");
                string studentId = parts[0];
                string skillId = parts[1];

                await FirebaseDatabase.DefaultInstance
                    .GetReference("skill_clicks")
                    .Child(today)
                    .Child(studentId)
                    .Child(skillId)
                    .SetValueAsync(count);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error saving click count: {ex.Message}");
        }
    }

    private async void OnDestroy()
    {
        try
        {
            if (!string.IsNullOrEmpty(currentSkillKey) && clickCounts.ContainsKey(currentSkillKey))
            {
                await SaveClickCount(currentSkillKey, clickCounts[currentSkillKey]);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error in OnDestroy: {ex.Message}");
        }
    }
}