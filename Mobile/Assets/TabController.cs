using UnityEngine;
using UnityEngine.UI;

public class TabController : MonoBehaviour
{
    public Image[] tabImages;
    public GameObject[] pages;

    // Rendiamo i colori pubblici così appaiono nell'Inspector
    // Ho messo dei colori di base, ma li modificherai tu da Unity!
    public Color tabActiveColor = Color.yellow;
    public Color tabInactiveColor = Color.gray;

    void Start()
    {
        // Attiva la prima tab (indice 0) all'avvio
        ActivateTab(0);
    }

    public void ActivateTab(int tabNo)
    {
        for (int i = 0; i < pages.Length; i++)
        {
            pages[i].SetActive(false);
            // Usa il colore disattivato che hai scelto nell'Inspector
            tabImages[i].color = tabInactiveColor;
        }

        pages[tabNo].SetActive(true);
        // Usa il colore attivato che hai scelto nell'Inspector
        tabImages[tabNo].color = tabActiveColor;
    }
}