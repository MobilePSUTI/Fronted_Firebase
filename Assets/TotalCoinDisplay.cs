using UnityEngine;
using TMPro;

public class TotalCoinsDisplay : MonoBehaviour
{
    public TextMeshProUGUI totalCoinsText;

    private void Start()
    {
        UpdateCoinsDisplay();
    }

    public void UpdateCoinsDisplay()
    {
        totalCoinsText.text = "" + GameSession.TotalCoins;
    }
}