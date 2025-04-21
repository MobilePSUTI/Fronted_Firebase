using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Collections;
using TMPro;

public class FacultySelector : MonoBehaviour
{
    [System.Serializable]
    public class FacultyGame
    {
        public string facultyName;
        [TextArea(3, 5)]
        public string gameDescription;
        public string gameSceneName;
        public Sprite sceneBackground;
        public Color playButtonColor = Color.black;
        public Sprite playButtonBackground;
        public Color facultyNameColor = Color.black;
    }

    // Основные UI элементы
    [SerializeField] private TMP_Text facultyNameDisplay; // Изменено с Image на Text
    [SerializeField] private Text descriptionText;
    [SerializeField] private Button leftButton;
    [SerializeField] private Button rightButton;
    [SerializeField] private Button playButton;
    [SerializeField] private Image sceneBackground;
    [SerializeField] private Image playButtonBackground;

    // Окно подтверждения
    [SerializeField] private GameObject confirmationWindow;
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;

    [Header("Animation Settings")]
    [SerializeField] private float switchDuration = 0.3f;
    [SerializeField] private AnimationCurve fadeCurve;

    public List<FacultyGame> facultyGames;
    private int currentIndex = 0;
    private bool isSwitching = false;

    private void Awake()
    {
        // Инициализация всех компонентов
        if (facultyNameDisplay == null) facultyNameDisplay = transform.Find("FacultyNameText").GetComponent<TMP_Text>();
        if (descriptionText == null) descriptionText = transform.Find("DescriptionText").GetComponent<Text>();
        // ... аналогично для других компонентов
    }

    private void Start()
    {
        leftButton.onClick.AddListener(PreviousGame);
        rightButton.onClick.AddListener(NextGame);
        playButton.onClick.AddListener(ShowConfirmation);
        yesButton.onClick.AddListener(StartGame);
        noButton.onClick.AddListener(HideConfirmation);

        confirmationWindow.SetActive(false);
        UpdateDisplay();
    }

    public void ShowConfirmation()
    {
        confirmationWindow.SetActive(true);

        // Блокируем другие кнопки при открытом окне
        leftButton.interactable = false;
        rightButton.interactable = false;
        playButton.interactable = false;
    }

    public void HideConfirmation()
    {
        confirmationWindow.SetActive(false);

        // Разблокируем кнопки
        leftButton.interactable = true;
        rightButton.interactable = true;
        playButton.interactable = true;
    }

    public void StartGame()
    {
        HideConfirmation();
        if (!string.IsNullOrEmpty(facultyGames[currentIndex].gameSceneName))
        {
            SceneManager.LoadScene(facultyGames[currentIndex].gameSceneName);
        }
    }

    public void NextGame()
    {
        if (isSwitching || facultyGames.Count <= 1) return;
        StartCoroutine(SwitchAnimation(1));
    }

    public void PreviousGame()
    {
        if (isSwitching || facultyGames.Count <= 1) return;
        StartCoroutine(SwitchAnimation(-1));
    }

    private IEnumerator SwitchAnimation(int direction)
    {
        isSwitching = true;

        // Фаза исчезновения
        float timer = 0;
        while (timer < switchDuration / 2)
        {
            timer += Time.unscaledDeltaTime;
            float alpha = fadeCurve.Evaluate(timer / (switchDuration / 2));
            SetElementsAlpha(1 - alpha);
            yield return null;
        }

        // Смена контента
        currentIndex = (currentIndex + direction + facultyGames.Count) % facultyGames.Count;
        UpdateContent();

        // Фаза появления
        timer = 0;
        while (timer < switchDuration / 2)
        {
            timer += Time.unscaledDeltaTime;
            float alpha = fadeCurve.Evaluate(timer / (switchDuration / 2));
            SetElementsAlpha(alpha);
            yield return null;
        }

        SetElementsAlpha(1);
        isSwitching = false;
    }

    private void SetElementsAlpha(float alpha)
    {
        // Тексты с прозрачностью
        facultyNameDisplay.alpha = alpha;
        descriptionText.color = new Color(0, 0, 0, alpha); // Черный цвет с изменяемой прозрачностью

        // Фон и кнопки с прозрачностью
        sceneBackground.color = new Color(1, 1, 1, alpha);
        playButtonBackground.color = new Color(1, 1, 1, alpha);
    }

    private void UpdateContent()
    {
        FacultyGame current = facultyGames[currentIndex];

        facultyNameDisplay.text = current.facultyName;
        facultyNameDisplay.color = current.facultyNameColor;// Устанавливаем текст вместо изображения
        descriptionText.text = current.gameDescription;
        sceneBackground.sprite = current.sceneBackground;
        playButtonBackground.sprite = current.playButtonBackground;
        playButtonBackground.color = current.playButtonColor;

        // Убедимся, что текст черный
        descriptionText.color = Color.black;
    }

    private void UpdateDisplay()
    {
        UpdateContent();

        leftButton.gameObject.SetActive(facultyGames.Count > 1);
        rightButton.gameObject.SetActive(facultyGames.Count > 1);
    }
}