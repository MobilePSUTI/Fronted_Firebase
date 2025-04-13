using UnityEngine;

public class ScrollingBackground : MonoBehaviour
{
    [Header("Настройки прокрутки")]
    [Tooltip("Скорость прокрутки фона")]
    public float scrollSpeed = 0.5f;
    [Tooltip("Направление прокрутки (по умолчанию вниз)")]
    public Vector2 direction = new Vector2(0, -1);
    [Tooltip("Использовать параллакс-эффект?")]
    public bool useParallax = true;
    [Tooltip("Множитель параллакса (чем больше, тем сильнее эффект)")]
    [Range(0f, 1f)] public float parallaxEffect = 0.5f;

    private Material backgroundMaterial;
    private Vector2 savedOffset;
    private Transform cameraTransform;
    private Vector3 lastCameraPosition;

    private void Start()
    {
        // Получаем материал из Renderer компонента
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            backgroundMaterial = renderer.material;
            savedOffset = backgroundMaterial.mainTextureOffset;
        }

        // Настройка параллакса
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
            // Параллакс-эффект на основе движения камеры
            Vector3 deltaMovement = cameraTransform.position - lastCameraPosition;
            transform.position += new Vector3(deltaMovement.x * parallaxEffect, deltaMovement.y * parallaxEffect, 0);
            lastCameraPosition = cameraTransform.position;
        }

        // Прокрутка текстуры
        if (backgroundMaterial != null)
        {
            Vector2 offset = direction * (scrollSpeed * Time.time);
            backgroundMaterial.mainTextureOffset = offset;
        }
    }

    private void OnDisable()
    {
        // Восстанавливаем оригинальные настройки текстуры
        if (backgroundMaterial != null)
        {
            backgroundMaterial.mainTextureOffset = savedOffset;
        }
    }
}