using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Collections;

public class FacultySelector : MonoBehaviour
{
    [System.Serializable]
    public class FacultyGame
    {
        public string facultyName;
        public Sprite facultyImage;
        [TextArea(3, 5)]
        public string gameDescription;
        public string gameSceneName;
        public Sprite sceneBackground;
        public Color playButtonColor = Color.black;
        public Sprite playButtonBackground;
    }

    // Основные UI элементы
    [SerializeField] private Image facultyImageDisplay;
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
    [SerializeField] private float switchDuration = 0.3f; // Уменьшено время анимации
    [SerializeField] private AnimationCurve fadeCurve;

    public List<FacultyGame> facultyGames;
    private int currentIndex = 0;
    private bool isSwitching = false;

    private void Awake()
    {
        // Инициализация всех компонентов
        if (facultyImageDisplay == null) facultyImageDisplay = transform.Find("FacultyImage").GetComponent<Image>();
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
            // Можно добавить эффект загрузки
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

        // Фаза исчезновения (упрощена)
        float timer = 0;
        while (timer < switchDuration / 2)
        {
            timer += Time.unscaledDeltaTime; // Используем unscaledDeltaTime для независимости от Time.timeScale
            float alpha = fadeCurve.Evaluate(timer / (switchDuration / 2));
            SetElementsAlpha(1 - alpha);
            yield return null;
        }

        // Смена контента (без задержки)
        currentIndex = (currentIndex + direction + facultyGames.Count) % facultyGames.Count;
        UpdateContent();

        // Фаза появления (упрощена)
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
        // Изображения и фон с прозрачностью
        facultyImageDisplay.color = new Color(1, 1, 1, alpha);
        sceneBackground.color = new Color(1, 1, 1, alpha);
        playButtonBackground.color = new Color(1, 1, 1, alpha);

        // Текст всегда черный, только меняем прозрачность
        descriptionText.color = new Color(0, 0, 0, alpha); // Черный цвет с изменяемой прозрачностью
    }

    private void UpdateContent()
    {
        FacultyGame current = facultyGames[currentIndex];

        facultyImageDisplay.sprite = current.facultyImage;
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