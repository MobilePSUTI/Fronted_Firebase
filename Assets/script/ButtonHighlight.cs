using UnityEngine;
using UnityEngine.UI;

public class ButtonHighlight : MonoBehaviour
{
    public Color startColor = Color.white; // ��������� ����
    public Color endColor = Color.yellow; // ���� ���������
    public float speed = 1f; // �������� ��������� �����

    private Image buttonImage;
    private float time;

    void Start()
    {
        buttonImage = GetComponent<Image>();
        buttonImage.color = startColor;
    }

    void Update()
    {
        // ������� ��������� ����� � �������������� Mathf.PingPong
        time += Time.deltaTime * speed;
        float t = Mathf.PingPong(time, 1); // ���������� �������� �� 0 �� 1
        buttonImage.color = Color.Lerp(startColor, endColor, t);
    }
}