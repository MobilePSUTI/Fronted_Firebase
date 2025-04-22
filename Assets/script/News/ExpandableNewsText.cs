using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ExpandableNewsText : MonoBehaviour
{
    public TMP_Text textShort;
    public TMP_Text textFull;
    public Button showMoreButton;

    private bool isFullTextShown; // Tracks whether full text is currently displayed

    public void Initialize(string text)
    {
        if (textShort == null || textFull == null || showMoreButton == null)
        {
            Debug.LogWarning("[ExpandableNewsText] Components missing: textShort, textFull, or showMoreButton");
            return;
        }

        // Set initial text content
        textShort.text = text.Length > 100 ? text.Substring(0, 100) + "..." : text;
        textFull.text = text;

        // Set initial state
        textShort.gameObject.SetActive(true);
        textFull.gameObject.SetActive(false);
        isFullTextShown = false;

        // Set up button listener
        showMoreButton.onClick.RemoveAllListeners();
        showMoreButton.onClick.AddListener(ToggleText);
    }

    private void ToggleText()
    {
        if (isFullTextShown)
        {
            // Hide full text, show short text
            textFull.gameObject.SetActive(false);
            textShort.gameObject.SetActive(true);
            isFullTextShown = false;
            showMoreButton.GetComponentInChildren<TMP_Text>().text = "Åù¸";
        }
        else
        {
            // Show full text, hide short text
            textShort.gameObject.SetActive(false);
            textFull.gameObject.SetActive(true);
            isFullTextShown = true;
            showMoreButton.GetComponentInChildren<TMP_Text>().text = "Ñêðûòü";
        }

        // Rebuild layout to reflect changes
        LayoutRebuilder.ForceRebuildLayoutImmediate(transform as RectTransform);
    }
}