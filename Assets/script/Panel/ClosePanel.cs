using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClosePanel : MonoBehaviour
{
    public GameObject Panel;

    public void ClosePanelClick()
    {
        if (Panel.activeSelf)
        {
            Panel.SetActive(false);

        }
    }
}
