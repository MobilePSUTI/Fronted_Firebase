using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;
using System;
using TMPro; // Для TextMeshPro, если используется

public class StudentListManager : MonoBehaviour
{
    public GameObject studentPrefab;
    public Transform studentParent;
    public GameObject loadingIndicator;
    public Text groupNameText; // Ссылка на компонент Text для отображения названия группы
    public TextMeshProUGUI groupNameTextTMP; // Ссылка на TextMeshProUGUI (если используется)

    private FirebaseDBManager _dbManager;

    private void Awake()
    {
        // Получаем или создаем экземпляр FirebaseDBManager
        if (FirebaseDBManager.Instance == null)
        {
            GameObject firebaseManager = new GameObject("FirebaseManager");
            firebaseManager.AddComponent<FirebaseDBManager>();
            DontDestroyOnLoad(firebaseManager);
        }
        _dbManager = FirebaseDBManager.Instance;
    }

    async void Start()
    {
        if (loadingIndicator != null)
            loadingIndicator.SetActive(true);

        try
        {
            // Убедимся, что Firebase инициализирован
            await _dbManager.Initialize();

            // Получаем название группы из базы данных
            string groupName = await _dbManager.GetGroupName(UserSession.SelectedGroupId);

            // Обновляем текстовый элемент с названием группы
            if (groupNameText != null)
            {
                groupNameText.text = string.IsNullOrEmpty(groupName) ? "Группа не найдена" : $"Группа: {groupName}";
            }
            else if (groupNameTextTMP != null)
            {
                groupNameTextTMP.text = string.IsNullOrEmpty(groupName) ? "Группа не найдена" : $"Группа: {groupName}";
            }
            else
            {
                Debug.LogWarning("Не указан компонент текста для отображения названия группы!");
            }

            // Проверяем кеш в UserSession
            if (UserSession.CachedStudents.TryGetValue(UserSession.SelectedGroupId, out var cachedStudents))
            {
                await CreateStudentList(cachedStudents);
                if (loadingIndicator != null)
                    loadingIndicator.SetActive(false);
                return;
            }

            // Загружаем студентов
            var students = await _dbManager.GetStudentsByGroup(UserSession.SelectedGroupId);

            if (students != null && students.Count > 0)
            {
                // Сохраняем в кеш UserSession
                UserSession.CachedStudents[UserSession.SelectedGroupId] = students;
                await CreateStudentList(students);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Ошибка при загрузке данных: {ex.Message}");
        }
        finally
        {
            if (loadingIndicator != null)
                loadingIndicator.SetActive(false);
        }
    }

    private async Task CreateStudentList(List<Student> students)
    {
        if (students == null)
        {
            Debug.LogError("Список студентов равен null");
            return;
        }

        // Очищаем предыдущий список
        foreach (Transform child in studentParent)
        {
            Destroy(child.gameObject);
        }

        foreach (Student student in students)
        {
            if (student == null) continue;

            try
            {
                // Получаем название группы для студента
                student.GroupName = await _dbManager.GetGroupName(student.GroupId);

                // Создаем UI элемент студента
                GameObject studentUI = Instantiate(studentPrefab, studentParent);
                Text studentText = studentUI.GetComponentInChildren<Text>();
                studentText.text = $"{student.Last} {student.First[0]}.{student.Second[0]}.";

                // Настраиваем кнопку
                StudentButton studentButton = studentUI.GetComponent<StudentButton>();
                if (studentButton != null)
                {
                    studentButton.SetStudentData(student);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка при создании элемента студента: {ex.Message}");
            }
        }
    }
}