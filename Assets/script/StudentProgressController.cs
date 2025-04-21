using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Firebase.Database;
using System;
using System.Collections;

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

    [Header("UI Elements")]
    public List<Image> mainSkillBars;
    public List<Text> mainSkillTexts;
    public List<Text> mainSkillTitles;
    public List<Image> additionalSkillBars;
    public List<Text> additionalSkillTexts;
    public List<Text> additionalSkillTitles;

    [Header("Settings")]
    public float fillAnimationDuration = 0.5f;

    private Dictionary<string, SkillData> allSkills = new Dictionary<string, SkillData>();
    private Dictionary<string, int> studentMainSkills = new Dictionary<string, int>();
    private Dictionary<string, int> studentAdditionalSkills = new Dictionary<string, int>();
    private List<string> programMainSkills = new List<string>();
    private bool dataLoadedFromCache = false;

    [System.Serializable]
    public class SkillsCache
    {
        public Dictionary<string, SkillData> allSkills;
        public Dictionary<string, int> studentMainSkills;
        public Dictionary<string, int> studentAdditionalSkills;
        public List<string> programMainSkills;
    }

    private void Awake()
    {
        // Check for cached data
        if (UserSession.CachedSkills != null)
        {
            LoadFromCache(UserSession.CachedSkills);
            dataLoadedFromCache = true;
        }
    }

    private async void Start()
    {
        // If cache exists, show data immediately
        if (dataLoadedFromCache)
        {
            UpdateSkillUI();
        }

        // Load fresh data
        await LoadAllData();

        // Update UI and cache
        UpdateSkillUI();
        SaveToCache();
    }

    private async Task LoadAllData()
    {
        try
        {
            await Task.WhenAll(
                LoadAllSkills(),
                LoadStudentData(),
                LoadStudentSkills()
            );
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error loading data: {ex.Message}");
        }
    }

    public async Task LoadAllSkills()
    {
        try
        {
            // Check Firebase initialization
            if (!FirebaseDBManager.Instance.isInitialized)
            {
                await FirebaseDBManager.Instance.Initialize();
            }

            // Get student ID
            string studentId = UserSession.CurrentUser?.Id;
            if (string.IsNullOrEmpty(studentId))
            {
                Debug.LogError("Student ID is null or empty");
                return;
            }

            // Get student skills data
            DataSnapshot skillsSnapshot = await FirebaseDBManager.Instance.DatabaseReference
                .Child("16") // user_skills table
                .Child("data")
                .Child(studentId)
                .GetValueAsync();

            if (!skillsSnapshot.Exists)
            {
                Debug.LogWarning("No skills data found for student");
                return;
            }

            // Get main and additional skills
            DataSnapshot mainSkillsNode = skillsSnapshot.Child("main_skills");
            DataSnapshot additionalSkillsNode = skillsSnapshot.Child("additional_skills");

            // Initialize dictionaries
            studentMainSkills = new Dictionary<string, int>();
            studentAdditionalSkills = new Dictionary<string, int>();

            // Load main skills
            if (mainSkillsNode.Exists && mainSkillsNode.HasChildren)
            {
                foreach (DataSnapshot skill in mainSkillsNode.Children)
                {
                    string skillId = skill.Key;
                    int skillValue = Convert.ToInt32(skill.Value);
                    studentMainSkills[skillId] = skillValue;
                }
            }

            // Load additional skills
            if (additionalSkillsNode.Exists && additionalSkillsNode.HasChildren)
            {
                foreach (DataSnapshot skill in additionalSkillsNode.Children)
                {
                    string skillId = skill.Key;
                    int skillValue = Convert.ToInt32(skill.Value);
                    studentAdditionalSkills[skillId] = skillValue;
                }
            }

            Debug.Log($"Successfully loaded {studentMainSkills.Count} main skills and {studentAdditionalSkills.Count} additional skills");
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
            DataSnapshot studentSnapshot = await FirebaseDatabase.DefaultInstance
                .GetReference("14/data/" + UserSession.CurrentUser.Id)
                .GetValueAsync();

            if (studentSnapshot.Exists)
            {
                string groupId = studentSnapshot.Child("group_id").Value.ToString();

                DataSnapshot groupSnapshot = await FirebaseDatabase.DefaultInstance
                    .GetReference("6/data/" + groupId)
                    .GetValueAsync();

                if (groupSnapshot.Exists)
                {
                    string programId = groupSnapshot.Child("educational_program_id").Value.ToString();

                    DataSnapshot programSnapshot = await FirebaseDatabase.DefaultInstance
                        .GetReference("4/data/" + programId)
                        .GetValueAsync();

                    if (programSnapshot.Exists)
                    {
                        DataSnapshot mainSkillsSnapshot = programSnapshot.Child("main_skills");
                        if (mainSkillsSnapshot.Exists)
                        {
                            programMainSkills.Clear();
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
                // Main skills
                DataSnapshot mainSkillsSnapshot = snapshot.Child("main_skills");
                if (mainSkillsSnapshot.Exists)
                {
                    studentMainSkills.Clear();
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

                // Additional skills
                DataSnapshot additionalSkillsSnapshot = snapshot.Child("additional_skills");
                if (additionalSkillsSnapshot.Exists)
                {
                    studentAdditionalSkills.Clear();
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
        // Main skills
        var filteredMainSkills = programMainSkills
            .Select(id => allSkills.ContainsKey(id) ? allSkills[id] : null)
            .Where(skill => skill != null)
            .Take(mainSkillBars.Count)
            .ToList();

        for (int i = 0; i < mainSkillBars.Count; i++)
        {
            if (i < filteredMainSkills.Count)
            {
                string skillId = filteredMainSkills[i].id;
                int points = studentMainSkills.ContainsKey(skillId) ? studentMainSkills[skillId] : 0;

                mainSkillTitles[i].text = filteredMainSkills[i].title;
                mainSkillTexts[i].text = points.ToString();
                StartCoroutine(AnimateBarFill(mainSkillBars[i], points / 100f));
            }
            else
            {
                mainSkillBars[i].gameObject.SetActive(false);
            }
        }

        // Additional skills
        var additionalSkills = allSkills.Values
            .Where(s => s.type == "additional")
            .OrderBy(s => int.Parse(s.id))
            .Take(additionalSkillBars.Count)
            .ToList();

        for (int i = 0; i < additionalSkillBars.Count; i++)
        {
            if (i < additionalSkills.Count)
            {
                string skillId = additionalSkills[i].id;
                int points = studentAdditionalSkills.ContainsKey(skillId) ? studentAdditionalSkills[skillId] : 0;

                additionalSkillTitles[i].text = additionalSkills[i].title;
                additionalSkillTexts[i].text = points.ToString();
                StartCoroutine(AnimateBarFill(additionalSkillBars[i], points / 100f));
            }
            else
            {
                additionalSkillBars[i].gameObject.SetActive(false);
            }
        }
    }

    private IEnumerator AnimateBarFill(Image bar, float targetFill)
    {
        float startFill = bar.fillAmount;
        float elapsed = 0f;

        while (elapsed < fillAnimationDuration)
        {
            bar.fillAmount = Mathf.Lerp(startFill, targetFill, elapsed / fillAnimationDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        bar.fillAmount = targetFill;
    }

    public void LoadFromCache(SkillsCache cache)
    {
        if (cache == null) return;

        allSkills = cache.allSkills ?? new Dictionary<string, SkillData>();
        studentMainSkills = cache.studentMainSkills ?? new Dictionary<string, int>();
        studentAdditionalSkills = cache.studentAdditionalSkills ?? new Dictionary<string, int>();
        programMainSkills = cache.programMainSkills ?? new List<string>();
    }

    public void SaveToCache()
    {
        UserSession.CachedSkills = new SkillsCache
        {
            allSkills = allSkills,
            studentMainSkills = studentMainSkills,
            studentAdditionalSkills = studentAdditionalSkills,
            programMainSkills = programMainSkills
        };
    }

    public IEnumerator PreloadSkillsCoroutine()
    {
        yield return LoadAllSkillsCoroutine();
        yield return LoadStudentDataCoroutine();
        yield return LoadStudentSkillsCoroutine();
        SaveToCache();
    }

    private IEnumerator LoadAllSkillsCoroutine()
    {
        Task<DataSnapshot> task = FirebaseDatabase.DefaultInstance.GetReference("11/data").GetValueAsync();
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.IsCompletedSuccessfully && task.Result.Exists)
        {
            foreach (DataSnapshot skillSnapshot in task.Result.Children)
            {
                // Check if the snapshot contains the required fields
                if (skillSnapshot.HasChild("id") && skillSnapshot.HasChild("title") && skillSnapshot.HasChild("type"))
                {
                    SkillData skill = new SkillData
                    {
                        id = skillSnapshot.Child("id").Value.ToString(),
                        title = skillSnapshot.Child("title").Value.ToString(),
                        type = skillSnapshot.Child("type").Value.ToString()
                    };
                    allSkills[skill.id] = skill;
                }
                else
                {
                    Debug.LogWarning($"Skipping invalid skill entry in snapshot: {skillSnapshot.Key}");
                }
            }
        }
        else
        {
            Debug.LogWarning("No skills data found or task failed.");
        }
    }

    private IEnumerator LoadStudentDataCoroutine()
    {
        if (UserSession.CurrentUser == null) yield break;

        Task<DataSnapshot> studentTask = FirebaseDatabase.DefaultInstance
            .GetReference("14/data/" + UserSession.CurrentUser.Id)
            .GetValueAsync();
        yield return new WaitUntil(() => studentTask.IsCompleted);

        if (!studentTask.IsCompletedSuccessfully || !studentTask.Result.Exists) yield break;

        string groupId = studentTask.Result.Child("group_id").Value.ToString();

        Task<DataSnapshot> groupTask = FirebaseDatabase.DefaultInstance
            .GetReference("6/data/" + groupId)
            .GetValueAsync();
        yield return new WaitUntil(() => groupTask.IsCompleted);

        if (!groupTask.IsCompletedSuccessfully || !groupTask.Result.Exists) yield break;

        string programId = groupTask.Result.Child("educational_program_id").Value.ToString();

        Task<DataSnapshot> programTask = FirebaseDatabase.DefaultInstance
            .GetReference("4/data/" + programId)
            .GetValueAsync();
        yield return new WaitUntil(() => programTask.IsCompleted);

        if (programTask.IsCompletedSuccessfully && programTask.Result.Exists)
        {
            DataSnapshot mainSkillsSnapshot = programTask.Result.Child("main_skills");
            if (mainSkillsSnapshot.Exists)
            {
                programMainSkills.Clear();
                foreach (DataSnapshot skillIdSnapshot in mainSkillsSnapshot.Children)
                {
                    programMainSkills.Add(skillIdSnapshot.Value.ToString());
                }
            }
        }
    }

    private IEnumerator LoadStudentSkillsCoroutine()
    {
        if (UserSession.CurrentUser == null) yield break;

        Task<DataSnapshot> task = FirebaseDatabase.DefaultInstance
            .GetReference("16/data/" + UserSession.CurrentUser.Id)
            .GetValueAsync();
        yield return new WaitUntil(() => task.IsCompleted);

        if (!task.IsCompletedSuccessfully || !task.Result.Exists) yield break;

        // Main skills
        DataSnapshot mainSkillsSnapshot = task.Result.Child("main_skills");
        if (mainSkillsSnapshot.Exists)
        {
            studentMainSkills.Clear();
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

        // Additional skills
        DataSnapshot additionalSkillsSnapshot = task.Result.Child("additional_skills");
        if (additionalSkillsSnapshot.Exists)
        {
            studentAdditionalSkills.Clear();
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