using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using Firebase.Database;
using System;
using System.Threading.Tasks;

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

    private async void Start()
    {
        await LoadAllSkills();
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
                // Load main skills
                DataSnapshot mainSkillsSnapshot = snapshot.Child("main_skills");
                if (mainSkillsSnapshot.Exists)
                {
                    foreach (DataSnapshot skillSnapshot in mainSkillsSnapshot.Children)
                    {
                        string skillId = skillSnapshot.Key;
                        if (int.TryParse(skillSnapshot.Value.ToString(), out int points))
                        {
                            studentMainSkills[skillId] = points;
                        }
                    }
                }

                // Load additional skills
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
        // Update main skills
        var mainSkills = allSkills.Values
            .Where(s => s.type == "main")
            .OrderBy(s => int.Parse(s.id))
            .Take(7)
            .ToList();

        for (int i = 0; i < mainSkills.Count && i < mainSkillBars.Count; i++)
        {
            string skillId = mainSkills[i].id;
            int points = studentMainSkills.ContainsKey(skillId) ? studentMainSkills[skillId] : 0;

            mainSkillBars[i].fillAmount = points / 100f;
            mainSkillTexts[i].text = points.ToString();
            mainSkillTitles[i].text = mainSkills[i].title;
        }

        // Update additional skills
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
