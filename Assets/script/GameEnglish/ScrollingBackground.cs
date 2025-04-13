using UnityEngine;

public class ScrollingBackground : MonoBehaviour
{
    [Header("��������� ���������")]
    [Tooltip("�������� ��������� ����")]
    public float scrollSpeed = 0.5f;
    [Tooltip("����������� ��������� (�� ��������� ����)")]
    public Vector2 direction = new Vector2(0, -1);
    [Tooltip("������������ ���������-������?")]
    public bool useParallax = true;
    [Tooltip("��������� ���������� (��� ������, ��� ������� ������)")]
    [Range(0f, 1f)] public float parallaxEffect = 0.5f;

    private Material backgroundMaterial;
    private Vector2 savedOffset;
    private Transform cameraTransform;
    private Vector3 lastCameraPosition;

    private void Start()
    {
        // �������� �������� �� Renderer ����������
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            backgroundMaterial = renderer.material;
            savedOffset = backgroundMaterial.mainTextureOffset;
        }

        // ��������� ����������
        if (useParallax && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
            lastCameraPosition = cameraTransform.position;
        }
    }

    private void Update()
    {
        if (useParallax && cameraTransform != null)
        {
            // ���������-������ �� ������ �������� ������
            Vector3 deltaMovement = cameraTransform.position - lastCameraPosition;
            transform.position += new Vector3(deltaMovement.x * parallaxEffect, deltaMovement.y * parallaxEffect, 0);
            lastCameraPosition = cameraTransform.position;
        }

        // ��������� ��������
        if (backgroundMaterial != null)
        {
            Vector2 offset = direction * (scrollSpeed * Time.time);
            backgroundMaterial.mainTextureOffset = offset;
        }
    }

    private void OnDisable()
    {
        // ��������������� ������������ ��������� ��������
        if (backgroundMaterial != null)
        {
            backgroundMaterial.mainTextureOffset = savedOffset;
        }
    }
}