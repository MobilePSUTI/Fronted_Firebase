using UnityEngine;
using UnityEngine.UI;

public class LoadingText : MonoBehaviour
{
    public Text loadingText;

    public void ShowLoadingText()
    {
        loadingText.text = "Загрузка...";
    }

    public void HideLoadingText()
    {
        loadingText.text = "";
    }
}