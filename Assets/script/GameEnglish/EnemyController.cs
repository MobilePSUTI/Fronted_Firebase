using UnityEngine;
using TMPro;

public class EnemyController : MonoBehaviour
{
    [Header("���������")]
    public float moveSpeed = 2f;
    public TextMeshProUGUI wordDisplay;
    public string targetWord;
    private string currentTyped = ""; 
    private int hiddenLetterIndex;
    private GameObject borderLine;

    private void Start()
    {
        targetWord = GameManager.Instance.GetRandomWord().ToLower();
        hiddenLetterIndex = Random.Range(1, targetWord.Length);
        UpdateWordDisplay();
        if (wordDisplay == null)
        {
            wordDisplay = GetComponentInChildren<TextMeshProUGUI>();
        }
        
        borderLine = GameObject.Find("BorderLine");
    }

    private void Update()
    {
        if (PauseManager.Instance.IsGamePaused()) return;
        transform.Translate(Vector2.down * moveSpeed * Time.deltaTime);

        if (transform.position.y < borderLine.transform.position.y + 0.8f)
        {
            GameManager.Instance.WordMissed();
            Destroy(gameObject);
        }
    }

    public void ProcessKeyPress(char key)
    {
        key = char.ToLower(key);

        if (targetWord[hiddenLetterIndex] == key)
        {
            currentTyped = targetWord;
            UpdateWordDisplay();
            WordCompleted();
        }
    }

    private void UpdateWordDisplay()
    {
        string display = "";
        for (int i = 0; i < targetWord.Length; i++)
        {
            if (i < currentTyped.Length || i != hiddenLetterIndex)
            {
                if (i < currentTyped.Length)
                    display += "<color=green>" + targetWord[i] + "</color>";
                else
                    display += targetWord[i];
            }
            else
            {
                display += "_";
            }
        }
        wordDisplay.text = display;
    }

    private void WordCompleted()
    {
        GameManager.Instance.AddCoins(GameManager.Instance.CalculateCoinReward());
        Destroy(gameObject);
    }
}