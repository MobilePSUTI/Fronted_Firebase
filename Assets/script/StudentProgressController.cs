using UnityEngine;
using UnityEngine.UI;
using TMPro;
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
    public List<Image> mainSkillBars = new List<Image>();
    public List<TextMeshProUGUI> mainSkillTexts = new List<TextMeshProUGUI>();
    public List<TextMeshProUGUI> mainSkillTitles = new List<TextMeshProUGUI>();
    public List<Image> additionalSkillBars = new List<Image>();
    public List<TextMeshProUGUI> additionalSkillTexts = new List<TextMeshProUGUI>();
    public List<TextMeshProUGUI> additionalSkillTitles = new List<TextMeshProUGUI>();

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
        // Initialize all lists to prevent null references
        if (mainSkillBars == null) mainSkillBars = new List<Image>();
        if (mainSkillTexts == null) mainSkillTexts = new List<TextMeshProUGUI>();
        if (mainSkillTitles == null) mainSkillTitles = new List<TextMeshProUGUI>();
        if (additionalSkillBars == null) additionalSkillBars = new List<Image>();
        if (additionalSkillTexts == null) additionalSkillTexts = new List<TextMeshProUGUI>();
        if (additionalSkillTitles == null) additionalSkillTitles = new List<TextMeshProUGUI>();

        // Check for cached data
        if (UserSession.CachedSkills != null)
        {
            LoadFromCache(UserSession.CachedSkills);
            dataLoadedFromCache = true;
        }
    }

    private async void Start()
    {
        try
        {
            // If cache exists, show data immediately
            if (dataLoadedFromCache)
            {
                SafeUpdateSkillUI();
            }

            // Load fresh data
            await SafeLoadAllData();
            await SafeLoadStudentSkills();

            // Update UI and cache
            SafeUpdateSkillUI();
            SaveToCache();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error in Start: {ex.Message}");
        }
    }

    private async Task SafeLoadAllData()
    {
        try
        {
            await Task.WhenAll(
                SafeLoadAllSkills(),
                SafeLoadStudentData(),
                SafeLoadStudentSkills()
            );
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error loading data: {ex.Message}");
        }
    }

    public async Task SafeLoadAllSkills()
    {
        try
        {
            // Check Firebase initialization
            if (FirebaseDBManager.Instance == null || !FirebaseDBManager.Instance.isInitialized)
            {
                await FirebaseDBManager.Instance.Initialize();
            }

            // Load all skills from 11/data
            DataSnapshot skillsSnapshot = await FirebaseDBManager.Instance.DatabaseReference
                .Child("11/data")
                .GetValueAsync();

            if (!skillsSnapshot.Exists || !skillsSnapshot.HasChildren)
            {
                Debug.LogWarning("[StudentProgress] No skills data found in 11/data");
                return;
            }

            allSkills.Clear();
            foreach (DataSnapshot skillSnapshot in skillsSnapshot.Children)
            {
                if (skillSnapshot?.HasChild("id") != true) continue;
                var skill = new SkillData
                {
                    id = skillSnapshot.Child("id")?.Value?.ToString() ?? string.Empty,
                    title = skillSnapshot.Child("title")?.Value?.ToString() ?? "N/A",
                    type = skillSnapshot.Child("type")?.Value?.ToString() ?? "main",
                    points = 0 // Default points, updated in SafeLoadStudentSkills
                };
                if (!string.IsNullOrEmpty(skill.id))
                {
                    allSkills[skill.id] = skill;
                }
            }

            Debug.Log($"[StudentProgress] Loaded {allSkills.Count} skills: {string.Join(", ", allSkills.Keys)}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[StudentProgress] Error loading all skills: {ex.Message}");
        }
    }

    private async Task SafeLoadStudentData()
    {
        if (UserSession.CurrentUser == null) return;

        try
        {
            DataSnapshot studentSnapshot = await FirebaseDatabase.DefaultInstance
                .GetReference("14/data/" + UserSession.CurrentUser.Id)
                .GetValueAsync();

            if (studentSnapshot?.Exists != true) return;

            string groupId = studentSnapshot.Child("group_id")?.Value?.ToString();
            if (string.IsNullOrEmpty(groupId)) return;

            DataSnapshot groupSnapshot = await FirebaseDatabase.DefaultInstance
                .GetReference("6/data/" + groupId)
                .GetValueAsync();

            if (groupSnapshot?.Exists != true) return;

            string programId = groupSnapshot.Child("educational_program_id")?.Value?.ToString();
            if (string.IsNullOrEmpty(programId)) return;

            DataSnapshot programSnapshot = await FirebaseDatabase.DefaultInstance
                .GetReference("4/data/" + programId)
                .GetValueAsync();

            if (programSnapshot?.Exists != true) return;

            DataSnapshot mainSkillsSnapshot = programSnapshot.Child("main_skills");
            if (mainSkillsSnapshot?.Exists == true)
            {
                programMainSkills.Clear();
                foreach (DataSnapshot skillIdSnapshot in mainSkillsSnapshot.Children)
                {
                    if (skillIdSnapshot?.Value != null)
                    {
                        programMainSkills.Add(skillIdSnapshot.Value.ToString());
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error loading student data: {ex.Message}");
        }
    }

    private async Task SafeLoadStudentSkills()
    {
        if (UserSession.CurrentUser == null)
        {
            Debug.LogWarning("[StudentProgress] No current user, skipping student skills load");
            return;
        }

        try
        {
            DataSnapshot snapshot = await FirebaseDatabase.DefaultInstance
                .GetReference("16/data/" + UserSession.CurrentUser.Id)
                .GetValueAsync();

            studentMainSkills.Clear();
            studentAdditionalSkills.Clear();

            if (snapshot?.Exists != true)
            {
                Debug.LogWarning($"[StudentProgress] No skills data found for user {UserSession.CurrentUser.Id}");
                return;
            }

            // Main skills
            DataSnapshot mainSkillsSnapshot = snapshot.Child("main_skills");
            if (mainSkillsSnapshot?.Exists == true)
            {
                foreach (DataSnapshot skillSnapshot in mainSkillsSnapshot.Children)
                {
                    if (skillSnapshot?.Key != null && programMainSkills.Contains(skillSnapshot.Key))
                    {
                        if (int.TryParse(skillSnapshot.Value?.ToString(), out int points))
                        {
                            studentMainSkills[skillSnapshot.Key] = points;
                        }
                    }
                }
            }

            // Additional skills
            DataSnapshot additionalSkillsSnapshot = snapshot.Child("additional_skills");
            if (additionalSkillsSnapshot?.Exists == true)
            {
                foreach (DataSnapshot skillSnapshot in additionalSkillsSnapshot.Children)
                {
                    if (skillSnapshot?.Key != null)
                    {
                        if (int.TryParse(skillSnapshot.Value?.ToString(), out int points))
                        {
                            studentAdditionalSkills[skillSnapshot.Key] = points;
                        }
                    }
                }
            }

            Debug.Log($"[StudentProgress] Loaded {studentMainSkills.Count} main skills and {studentAdditionalSkills.Count} additional skills for user {UserSession.CurrentUser.Id}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error loading student skills: {ex.Message}");
        }
    }

    private void SafeUpdateSkillUI()
    {
        try
        {
            // Update main skills
            if (mainSkillBars != null && mainSkillTexts != null && mainSkillTitles != null)
            {
                // Get all main skills from programMainSkills
                var mainSkills = programMainSkills
                    .Where(id => !string.IsNullOrEmpty(id) && allSkills.ContainsKey(id))
                    .Select(id => allSkills[id])
                    .Take(mainSkillBars.Count)
                    .ToList();

                for (int i = 0; i < mainSkillBars.Count; i++)
                {
                    if (mainSkillBars[i] == null || mainSkillTexts[i] == null || mainSkillTitles[i] == null)
                    {
                        Debug.LogWarning($"Main skill UI elements at index {i} are not properly assigned");
                        continue;
                    }

                    mainSkillBars[i].gameObject.SetActive(true); // Always show the bar

                    if (i < mainSkills.Count && mainSkills[i] != null)
                    {
                        string skillId = mainSkills[i].id;
                        int points = studentMainSkills.ContainsKey(skillId) ? studentMainSkills[skillId] : 0;

                        mainSkillTitles[i].text = mainSkills[i].title ?? "N/A";
                        mainSkillTexts[i].text = points.ToString();
                        mainSkillBars[i].color = points > 0 ? Color.green : Color.gray;
                        StartCoroutine(SafeAnimateBarFill(mainSkillBars[i], points / 100f));
                    }
                    else
                    {
                        // Placeholder for unused slots
                        mainSkillTitles[i].text = "N/A";
                        mainSkillTexts[i].text = "0";
                        mainSkillBars[i].color = Color.gray;
                        StartCoroutine(SafeAnimateBarFill(mainSkillBars[i], 0f));
                    }
                }

                Debug.Log($"[StudentProgress] Main skills displayed: {mainSkills.Count} (IDs: {string.Join(", ", mainSkills.Select(s => s.id))})");
            }
            else
            {
                Debug.LogWarning("[StudentProgress] Main skill UI elements are not assigned");
            }

            // Update additional skills
            if (additionalSkillBars != null && additionalSkillTexts != null && additionalSkillTitles != null)
            {
                // Get all additional skills (IDs 101–107)
                var additionalSkills = allSkills.Values
                    .Where(s => s != null && s.type == "additional")
                    .OrderBy(s => int.TryParse(s.id, out int idNum) ? idNum : 0)
                    .Take(additionalSkillBars.Count)
                    .ToList();

                for (int i = 0; i < additionalSkillBars.Count; i++)
                {
                    if (additionalSkillBars[i] == null || additionalSkillTexts[i] == null || additionalSkillTitles[i] == null)
                    {
                        Debug.LogWarning($"Additional skill UI elements at index {i} are not properly assigned");
                        continue;
                    }

                    additionalSkillBars[i].gameObject.SetActive(true); // Always show the bar

                    if (i < additionalSkills.Count && additionalSkills[i] != null)
                    {
                        string skillId = additionalSkills[i].id;
                        int points = studentAdditionalSkills.ContainsKey(skillId) ? studentAdditionalSkills[skillId] : 0;

                        additionalSkillTitles[i].text = additionalSkills[i].title ?? "N/A";
                        additionalSkillTexts[i].text = points.ToString();
                        additionalSkillBars[i].color = points > 0 ? Color.green : Color.gray;
                        StartCoroutine(SafeAnimateBarFill(additionalSkillBars[i], points / 100f));
                    }
                    else
                    {
                        // Placeholder for unused slots
                        additionalSkillTitles[i].text = "N/A";
                        additionalSkillTexts[i].text = "0";
                        additionalSkillBars[i].color = Color.gray;
                        StartCoroutine(SafeAnimateBarFill(additionalSkillBars[i], 0f));
                    }
                }

                Debug.Log($"[StudentProgress] Additional skills displayed: {additionalSkills.Count} (IDs: {string.Join(", ", additionalSkills.Select(s => s.id))})");
            }
            else
            {
                Debug.LogWarning("[StudentProgress] Additional skill UI elements are not assigned");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[StudentProgress] Error updating skill UI: {ex.Message}");
        }
    }

    private IEnumerator SafeAnimateBarFill(Image bar, float targetFill)
    {
        if (bar == null) yield break;

        float startFill = bar.fillAmount;
        float elapsed = 0f;
        bool errorOccurred = false;

        while (elapsed < fillAnimationDuration && !errorOccurred)
        {
            // Проверяем наличие компонента перед каждым обновлением
            if (bar == null)
            {
                errorOccurred = true;
                continue;
            }

            try
            {
                bar.fillAmount = Mathf.Lerp(startFill, targetFill, elapsed / fillAnimationDuration);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error animating bar: {ex.Message}");
                errorOccurred = true;
                continue;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Завершаем анимацию, если не было ошибок
        if (!errorOccurred && bar != null)
        {
            try
            {
                bar.fillAmount = targetFill;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error setting final bar value: {ex.Message}");
            }
        }
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

    // Coroutine versions for preloading

    public IEnumerator PreloadSkillsCoroutine()
    {
        yield return SafeLoadAllSkillsCoroutine();
        yield return SafeLoadStudentDataCoroutine();
        yield return SafeLoadStudentSkillsCoroutine();
        SaveToCache();
    }

    private IEnumerator SafeLoadAllSkillsCoroutine()
    {
        Task<DataSnapshot> task = FirebaseDatabase.DefaultInstance.GetReference("11/data").GetValueAsync();
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.IsCompletedSuccessfully && task.Result?.Exists == true)
        {
            foreach (DataSnapshot skillSnapshot in task.Result.Children)
            {
                if (skillSnapshot?.HasChild("id") == true &&
                    skillSnapshot.HasChild("title") == true &&
                    skillSnapshot.HasChild("type") == true)
                {
                    SkillData skill = new SkillData
                    {
                        id = skillSnapshot.Child("id")?.Value?.ToString() ?? string.Empty,
                        title = skillSnapshot.Child("title")?.Value?.ToString() ?? string.Empty,
                        type = skillSnapshot.Child("type")?.Value?.ToString() ?? string.Empty
                    };
                    if (!string.IsNullOrEmpty(skill.id))
                    {
                        allSkills[skill.id] = skill;
                    }
                }
            }
        }
    }

    private IEnumerator SafeLoadStudentDataCoroutine()
    {
        if (UserSession.CurrentUser == null) yield break;

        Task<DataSnapshot> studentTask = FirebaseDatabase.DefaultInstance
            .GetReference("14/data/" + UserSession.CurrentUser.Id)
            .GetValueAsync();
        yield return new WaitUntil(() => studentTask.IsCompleted);

        if (!studentTask.IsCompletedSuccessfully || studentTask.Result?.Exists != true) yield break;

        string groupId = studentTask.Result.Child("group_id")?.Value?.ToString();
        if (string.IsNullOrEmpty(groupId)) yield break;

        Task<DataSnapshot> groupTask = FirebaseDatabase.DefaultInstance
            .GetReference("6/data/" + groupId)
            .GetValueAsync();
        yield return new WaitUntil(() => groupTask.IsCompleted);

        if (!groupTask.IsCompletedSuccessfully || groupTask.Result?.Exists != true) yield break;

        string programId = groupTask.Result.Child("educational_program_id")?.Value?.ToString();
        if (string.IsNullOrEmpty(programId)) yield break;

        Task<DataSnapshot> programTask = FirebaseDatabase.DefaultInstance
            .GetReference("4/data/" + programId)
            .GetValueAsync();
        yield return new WaitUntil(() => programTask.IsCompleted);

        if (programTask.IsCompletedSuccessfully && programTask.Result?.Exists == true)
        {
            DataSnapshot mainSkillsSnapshot = programTask.Result.Child("main_skills");
            if (mainSkillsSnapshot?.Exists == true)
            {
                programMainSkills.Clear();
                foreach (DataSnapshot skillIdSnapshot in mainSkillsSnapshot.Children)
                {
                    if (skillIdSnapshot?.Value != null)
                    {
                        programMainSkills.Add(skillIdSnapshot.Value.ToString());
                    }
                }
            }
        }
    }

    private IEnumerator SafeLoadStudentSkillsCoroutine()
    {
        if (UserSession.CurrentUser == null) yield break;

        Task<DataSnapshot> task = FirebaseDatabase.DefaultInstance
            .GetReference("16/data/" + UserSession.CurrentUser.Id)
            .GetValueAsync();
        yield return new WaitUntil(() => task.IsCompleted);

        if (!task.IsCompletedSuccessfully || task.Result?.Exists != true) yield break;

        // Main skills
        DataSnapshot mainSkillsSnapshot = task.Result.Child("main_skills");
        if (mainSkillsSnapshot?.Exists == true)
        {
            studentMainSkills.Clear();
            foreach (DataSnapshot skillSnapshot in mainSkillsSnapshot.Children)
            {
                if (skillSnapshot?.Key != null && programMainSkills.Contains(skillSnapshot.Key))
                {
                    if (int.TryParse(skillSnapshot.Value?.ToString(), out int points))
                    {
                        studentMainSkills[skillSnapshot.Key] = points;
                    }
                }
            }
        }

        // Additional skills
        DataSnapshot additionalSkillsSnapshot = task.Result.Child("additional_skills");
        if (additionalSkillsSnapshot?.Exists == true)
        {
            studentAdditionalSkills.Clear();
            foreach (DataSnapshot skillSnapshot in additionalSkillsSnapshot.Children)
            {
                if (skillSnapshot?.Key != null)
                {
                    if (int.TryParse(skillSnapshot.Value?.ToString(), out int points))
                    {
                        studentAdditionalSkills[skillSnapshot.Key] = points;
                    }
                }
            }
        }
    }

    public async Task PreloadSkillsAsync()
    {
        try
        {
            if (UserSession.CurrentUser == null || string.IsNullOrEmpty(UserSession.CurrentUser.Id))
            {
                Debug.LogWarning("[StudentProgress] No current user or user ID, skipping skill preload");
                return;
            }

            var firebaseManager = FirebaseDBManager.Instance;
            if (firebaseManager == null)
            {
                Debug.LogError("[StudentProgress] FirebaseDBManager instance is null");
                return;
            }

            // Ensure Firebase is initialized
            if (!firebaseManager.isInitialized)
            {
                await firebaseManager.Initialize();
                if (!firebaseManager.isInitialized || firebaseManager.DatabaseReference == null)
                {
                    Debug.LogError("[StudentProgress] Firebase initialization failed or DatabaseReference is null");
                    return;
                }
            }

            // Check GroupId
            string groupId = UserSession.SelectedStudent.GroupId;
            if (string.IsNullOrEmpty(groupId))
            {
                Debug.LogWarning($"[StudentProgress] GroupId is missing for user {UserSession.CurrentUser.Id}");
                return;
            }

            // Get group details to find program ID
            var groupDetails = await firebaseManager.GetGroupDetails(groupId);
            if (groupDetails == null || string.IsNullOrEmpty(groupDetails.ProgramId))
            {
                Debug.LogWarning($"[StudentProgress] No program ID found for GroupId {groupId}");
                return;
            }

            string programId = groupDetails.ProgramId;

            // Load program skills (main skills)
            List<string> mainSkills = await firebaseManager.GetProgramSkills(programId);
            if (mainSkills == null || mainSkills.Count == 0)
            {
                Debug.LogWarning($"[StudentProgress] No main skills found for program {programId}");
                return;
            }

            // Load all skills (main and additional)
            var skillsSnapshot = await firebaseManager.DatabaseReference
                .Child("11/ data")
                .GetValueAsync();
            if (!skillsSnapshot.Exists || !skillsSnapshot.HasChildren)
            {
                Debug.LogWarning("[StudentProgress] No skills data found in 11/data");
                return;
            }

            allSkills.Clear();
            foreach (DataSnapshot skillSnapshot in skillsSnapshot.Children)
            {
                if (skillSnapshot?.HasChild("id") != true) continue;
                var skill = new SkillData
                {
                    id = skillSnapshot.Child("id")?.Value?.ToString() ?? string.Empty,
                    title = skillSnapshot.Child("title")?.Value?.ToString() ?? "N/A",
                    type = skillSnapshot.Child("type")?.Value?.ToString() ?? "main",
                    points = 0
                };
                if (!string.IsNullOrEmpty(skill.id))
                {
                    allSkills[skill.id] = skill;
                }
            }

            // Store main skills for the program
            programMainSkills = mainSkills;

            // Log loaded skills
            Debug.Log($"[StudentProgress] Preloaded {mainSkills.Count} main skills: {string.Join(", ", mainSkills)}");
            Debug.Log($"[StudentProgress] Loaded {allSkills.Count} total skills: {string.Join(", ", allSkills.Keys)}");

            // Save to cache
            SaveToCache();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[StudentProgress] Failed to preload skills: {ex.Message}\n{ex.StackTrace}");
        }
    }
}