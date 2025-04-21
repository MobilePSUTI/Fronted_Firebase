using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Linq;

public class LinuxGameManager : MonoBehaviour
{
    public static LinuxGameManager Instance;

    [Header("Questions")] 
    public TextAsset easyQuestionsFile;
    public TextAsset mediumQuestionsFile;
    public TextAsset hardQuestionsFile;

    [Header("UI")] 
    public TextMeshProUGUI questionText;
    public List<TextMeshProUGUI> answerTexts;
    public TextMeshProUGUI errorsText;
    public TextMeshProUGUI scoreText;
    public GameObject errorImage;

    [Header("Buttons")]
    public List<Button> answerButtons; // Привязать 4 кнопки в инспекторе

    private List<Question> easyQuestions = new List<Question>();
    private List<Question> mediumQuestions = new List<Question>();
    private List<Question> hardQuestions = new List<Question>();

    private int score = 0;
    private int errors = 0;
    private int questionsAnswered = 0;
    private int difficulty = 0; // 0 - easy, 1 - medium, 2 - hard
    private Question currentQuestion;
    private bool gameOver = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            AssignButtonListeners(); // Подключаем кнопки
            LoadQuestions();
            ShowNextQuestion();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Присваиваем обработчики кнопкам
    private void AssignButtonListeners()
    {
        for (int i = 0; i < answerButtons.Count; i++)
        {
            int index = i; // Локальная переменная для замыкания
            answerButtons[i].onClick.AddListener(() => AnswerClicked(index));
        }
    }

    private void LoadQuestions()
    {
        easyQuestions = ParseQuestions(easyQuestionsFile);
        mediumQuestions = ParseQuestions(mediumQuestionsFile);
        hardQuestions = ParseQuestions(hardQuestionsFile);
    }

    private List<Question> ParseQuestions(TextAsset file)
    {
        var questions = new List<Question>();
        if (file == null) return questions;

        string[] lines = file.text.Split('\n');
        foreach (string line in lines)
        {
            string[] parts = line.Split('|');
            if (parts.Length >= 2)
            {
                var q = new Question();
                q.Text = parts[0];
                q.Answers = parts.Skip(1).OrderBy(x => Random.value).ToList();
                q.CorrectAnswer = parts[1];
                questions.Add(q);
            }
        }
        return questions;
    }

    private void ShowNextQuestion()
    {
        if (gameOver) return;

        questionsAnswered++;
        if (questionsAnswered % 3 == 0 && difficulty < 2) difficulty++;

        List<Question> sourceList = difficulty == 0 ? easyQuestions : difficulty == 1 ? mediumQuestions : hardQuestions;
        if (sourceList.Count == 0)
        {
            GameOver();
            return;
        }

        int index = Random.Range(0, sourceList.Count);
        currentQuestion = sourceList[index];
        sourceList.RemoveAt(index);

        questionText.text = currentQuestion.Text;

        var randomizedAnswers = currentQuestion.Answers.OrderBy(x => Random.value).ToList();
        for (int i = 0; i < answerTexts.Count; i++)
        {
            answerTexts[i].text = randomizedAnswers.Count > i ? randomizedAnswers[i] : "";
            answerButtons[i].interactable = true;
        }
    }

    public void AnswerClicked(int index)
    {
        if (gameOver) return;

        string chosenAnswer = answerTexts[index].text;
        if (chosenAnswer == currentQuestion.CorrectAnswer)
        {
            score += 5;
            Debug.Log($"Correct answer! New score: {score}");
        }
        else
        {
            StartCoroutine(ErrorAlert());
            errors++;
            Debug.Log($"Wrong answer! Errors: {errors}/3");

            if (errors >= 3)
            {
                GameOver();
                return;
            }
        }

        UpdateUI();
        ShowNextQuestion();
    }

    IEnumerator ErrorAlert()
    {
        errorImage.gameObject.SetActive(true);
        yield return new WaitForSeconds(0.5f);
        errorImage.gameObject.SetActive(false);
    }

    private void UpdateUI()
    {
        scoreText.text = "Заработано очков: " + score;
        errorsText.text = "Кол-во ошибок: " + errors + "/3";
    }

    private void GameOver()
    {
        PlayerPrefs.SetInt("FinalScore", score);
        PlayerPrefs.SetInt("FinalErrors", errors);
        PlayerPrefs.SetFloat("FinalTime", Time.timeSinceLevelLoad);
        PlayerPrefs.Save();

        SceneManager.LoadScene("GameOverLinux");
    }



    [System.Serializable]
    public class Question
    {
        public string Text;
        public List<string> Answers;
        public string CorrectAnswer;
    }
}
