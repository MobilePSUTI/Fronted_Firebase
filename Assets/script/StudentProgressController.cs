using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Firebase.Database;
using System;

public class StudentProgressController : MonoBehaviour
{
    [System.Serializable]
    public class SkillData
    {
        public string id;
        public string title;
        public int points;
        public string type; // "main" or "additional"
    }

    [Header("Main Skills")]
    public List<Image> mainSkillBars;
    public List<Text> mainSkillTexts;
    public List<Text> mainSkillTitles;

    [Header("Additional Skills")]
    public List<Image> additionalSkillBars;
    public List<Text> additionalSkillTexts;
    public List<Text> additionalSkillTitles;

    private Dictionary<string, SkillData> allSkills = new Dictionary<string, SkillData>();
    private Dictionary<string, int> studentMainSkills = new Dictionary<string, int>();
    private Dictionary<string, int> studentAdditionalSkills = new Dictionary<string, int>();
    private List<string> programMainSkills = new List<string>(); // Основные скиллы программы

    private async void Start()
    {
        await LoadAllSkills();
        await LoadStudentData();
        await LoadStudentSkills();
        UpdateSkillUI();
    }

    private async Task LoadAllSkills()
    {
        try
        {
            DataSnapshot snapshot = await FirebaseDatabase.DefaultInstance.GetReference("11/data").GetValueAsync();

            if (snapshot.Exists)
            {
                foreach (DataSnapshot skillSnapshot in snapshot.Children)
                {
                    SkillData skill = new SkillData
                    {
                        id = skillSnapshot.Child("id").Value.ToString(),
                        title = skillSnapshot.Child("title").Value.ToString(),
                        type = skillSnapshot.Child("type").Value.ToString()
                    };

                    allSkills[skill.id] = skill;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error loading skills: {ex.Message}");
        }
    }

    private async Task LoadStudentData()
    {
        if (UserSession.CurrentUser == null) return;

        try
        {
            // 1. Получаем group_id студента
            DataSnapshot studentSnapshot = await FirebaseDatabase.DefaultInstance
                .GetReference("14/data/" + UserSession.CurrentUser.Id)
                .GetValueAsync();

            if (studentSnapshot.Exists)
            {
                string groupId = studentSnapshot.Child("group_id").Value.ToString();

                // 2. Получаем educational_program_id группы
                DataSnapshot groupSnapshot = await FirebaseDatabase.DefaultInstance
                    .GetReference("6/data/" + groupId)
                    .GetValueAsync();

                if (groupSnapshot.Exists)
                {
                    string programId = groupSnapshot.Child("educational_program_id").Value.ToString();

                    // 3. Получаем main_skills программы
                    DataSnapshot programSnapshot = await FirebaseDatabase.DefaultInstance
                        .GetReference("4/data/" + programId)
                        .GetValueAsync();

                    if (programSnapshot.Exists)
                    {
                        // Получаем список ID основных скиллов для этой программы
                        DataSnapshot mainSkillsSnapshot = programSnapshot.Child("main_skills");
                        if (mainSkillsSnapshot.Exists)
                        {
                            foreach (DataSnapshot skillIdSnapshot in mainSkillsSnapshot.Children)
                            {
                                programMainSkills.Add(skillIdSnapshot.Value.ToString());
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error loading student data: {ex.Message}");
        }
    }

    private async Task LoadStudentSkills()
    {
        if (UserSession.CurrentUser == null) return;

        try
        {
            DataSnapshot snapshot = await FirebaseDatabase.DefaultInstance
                .GetReference("16/data/" + UserSession.CurrentUser.Id)
                .GetValueAsync();

            if (snapshot.Exists)
            {
                // Load main skills (only those that belong to student's program)
                DataSnapshot mainSkillsSnapshot = snapshot.Child("main_skills");
                if (mainSkillsSnapshot.Exists)
                {
                    foreach (DataSnapshot skillSnapshot in mainSkillsSnapshot.Children)
                    {
                        string skillId = skillSnapshot.Key;
                        if (programMainSkills.Contains(skillId) &&
                            int.TryParse(skillSnapshot.Value.ToString(), out int points))
                        {
                            studentMainSkills[skillId] = points;
                        }
                    }
                }

                // Load additional skills (all)
                DataSnapshot additionalSkillsSnapshot = snapshot.Child("additional_skills");
                if (additionalSkillsSnapshot.Exists)
                {
                    foreach (DataSnapshot skillSnapshot in additionalSkillsSnapshot.Children)
                    {
                        string skillId = skillSnapshot.Key;
                        if (int.TryParse(skillSnapshot.Value.ToString(), out int points))
                        {
                            studentAdditionalSkills[skillId] = points;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error loading student skills: {ex.Message}");
        }
    }

    private void UpdateSkillUI()
    {
        // Update main skills (only those from student's program)
        var filteredMainSkills = programMainSkills
            .Select(id => allSkills.ContainsKey(id) ? allSkills[id] : null)
            .Where(skill => skill != null)
            .Take(7)
            .ToList();

        for (int i = 0; i < filteredMainSkills.Count && i < mainSkillBars.Count; i++)
        {
            string skillId = filteredMainSkills[i].id;
            int points = studentMainSkills.ContainsKey(skillId) ? studentMainSkills[skillId] : 0;

            mainSkillBars[i].fillAmount = points / 100f;
            mainSkillTexts[i].text = points.ToString();
            mainSkillTitles[i].text = filteredMainSkills[i].title;
        }

        // Update additional skills (all)
        var additionalSkills = allSkills.Values
            .Where(s => s.type == "additional")
            .OrderBy(s => int.Parse(s.id))
            .Take(7)
            .ToList();

        for (int i = 0; i < additionalSkills.Count && i < additionalSkillBars.Count; i++)
        {
            string skillId = additionalSkills[i].id;
            int points = studentAdditionalSkills.ContainsKey(skillId) ? studentAdditionalSkills[skillId] : 0;

            additionalSkillBars[i].fillAmount = points / 100f;
            additionalSkillTexts[i].text = points.ToString();
            additionalSkillTitles[i].text = additionalSkills[i].title;
        }
    }
}