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
            // ���������� Orthographic Size
            mainCamera.orthographicSize *= targetAspect / currentAspect;
        }
        else
        {
            // ���������� Field of View ��� Perspective ������
            mainCamera.fieldOfView *= targetAspect / currentAspect;
        }
    }
}