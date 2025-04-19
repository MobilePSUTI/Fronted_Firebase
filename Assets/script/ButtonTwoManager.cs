using UnityEngine;
using UnityEngine.UI;

public class ButtonTwoManager : MonoBehaviour
{
    public Button[] buttons; // Main and Additional skills buttons
    public GameObject[] contentPanels; // Main and Additional skills panels

    private int activeButtonIndex = -1;

    void Start()
    {
        // Assign click handlers
        for (int i = 0; i < buttons.Length; i++)
        {
            int index = i;
            buttons[i].onClick.AddListener(() => OnButtonClick(index));
        }

        // Show main skills by default
        activeButtonIndex = 0;
        UpdateContentPanels();
    }

    void OnButtonClick(int index)
    {
        if (activeButtonIndex == index) return;

        activeButtonIndex = index;
        UpdateContentPanels();
    }

    void UpdateContentPanels()
    {
        for (int i = 0; i < contentPanels.Length; i++)
        {
            contentPanels[i].SetActive(i == activeButtonIndex);
        }
    }
}