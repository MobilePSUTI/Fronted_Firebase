using UnityEngine;

public class CameraAspectRatio : MonoBehaviour
{
    public float baseWidth = 9f;
    public float baseHeight = 16f;

    private Camera mainCamera;

    void Start()
    {
        mainCamera = GetComponent<Camera>();
        AdjustCameraSize();
    }

    void AdjustCameraSize()
    {
        float targetAspect = baseWidth / baseHeight;
        float currentAspect = (float)Screen.width / Screen.height;

        if (mainCamera.orthographic)
        {
            // Подстройка Orthographic Size
            mainCamera.orthographicSize *= targetAspect / currentAspect;
        }
        else
        {
            // Подстройка Field of View для Perspective камеры
            mainCamera.fieldOfView *= targetAspect / currentAspect;
        }
    }
}