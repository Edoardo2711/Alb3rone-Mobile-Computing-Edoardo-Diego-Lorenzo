using UnityEngine;
using UnityEngine.SceneManagement; // Fondamentale per cambiare le scene!
using UnityEngine.UI; // Serve per poter controllare i bottoni

public class MainMenuManager : MonoBehaviour
{
    [Header("Pannelli dell'Interfaccia")]
    public GameObject paginaMenu;
    public GameObject paginaSettings;

    [Header("Bottoni")]
    public Button bottoneContinua; // Trascina qui il tuo bottone Continua

    [Header("Impostazioni Gioco")]
    public string nomeScenaGioco = "SampleScene"; // Scrivi qui il nome esatto della tua scena

    void Start()
    {
        // 1. Assicura che i pannelli siano corretti all'avvio
        if (paginaMenu != null && paginaSettings != null)
        {
            paginaMenu.SetActive(true);
            paginaSettings.SetActive(false);
        }

        // 2. "Continua" e' cliccabile solo se esiste davvero un file di salvataggio
        if (bottoneContinua != null)
        {
            bottoneContinua.interactable = SaveController.HasSave;
        }
    }

    // --- SEZIONE: IMPOSTAZIONI ---

    public void OpenSettings()
    {
        paginaMenu.SetActive(false);
        paginaSettings.SetActive(true);
    }

    public void CloseSettings()
    {
        paginaSettings.SetActive(false);
        paginaMenu.SetActive(true);
    }

    // --- SEZIONE: GESTIONE PARTITA E USCITA ---

    public void NewGame()
    {
        Debug.Log("Iniziando una Nuova Partita...");

        // La scena parte da zero. Il vecchio salvataggio resta su disco finche'
        // il giocatore non salva di nuovo, e le impostazioni (difficolta', volume)
        // non vengono toccate.
        SaveController.LoadOnNextStart = false;

        SceneManager.LoadScene(nomeScenaGioco);
    }

    public void ContinueGame()
    {
        Debug.Log("Caricamento partita salvata...");

        if (SaveController.HasSave)
        {
            // Sara' SaveController, all'avvio della scena, a ricaricare la partita
            SaveController.LoadOnNextStart = true;
            SceneManager.LoadScene(nomeScenaGioco);
        }
    }

    public void QuitGame()
    {
        Debug.Log("Il gioco si sta chiudendo...");

        // Questo comando chiude il gioco vero e proprio (quando lo esporti/buildi)
        Application.Quit();
    }
}
