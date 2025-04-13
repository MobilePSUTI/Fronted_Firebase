using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ButtonLetter : MonoBehaviour
{
    public char letter;
    private Button button;
    private Image buttonImage;
    public Sprite pressedSprite;   // ����������� ��� �������
    private Sprite normalSprite;   // �������� ����������� ������
    public float changeDuration = 0.3f; // ������������ ����� �������

    private void Awake()
    {
        button = GetComponent<Button>();
        buttonImage = GetComponent<Image>();
        normalSprite = buttonImage.sprite; // ��������� �������� ������

        button.onClick.AddListener(OnButtonClick);

        // ������������� ����� ������
        Text buttonText = GetComponentInChildren<Text>();
        if (buttonText != null)
        {
            buttonText.text = letter.ToString();
        }
    }

    private void OnButtonClick()
    {
        StartCoroutine(ChangeButtonSprite());
        PlayerController.Instance.OnKeyboardKeyPressed(letter);
    }

    private IEnumerator ChangeButtonSprite()
    {
        // ������ ������ �� �������
        buttonImage.sprite = pressedSprite;

        // ���� ��������� �����
        yield return new WaitForSeconds(changeDuration);

        // ���������� �������� ������
        buttonImage.sprite = normalSprite;
    }
}