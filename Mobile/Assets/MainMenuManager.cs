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

        // 2. Controlla se esiste un salvataggio per attivare/disattivare "Continua"
        if (bottoneContinua != null)
        {
            // Usiamo una chiave finta chiamata "Salvataggio" per capire se c'è una partita
            if (PlayerPrefs.HasKey("Salvataggio"))
            {
                bottoneContinua.interactable = true;  // C'è un salvataggio, bottone cliccabile
            }
            else
            {
                bottoneContinua.interactable = false; // Nessun salvataggio, bottone grigio
            }
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

        // Se inizi una nuova partita, cancelli i vecchi salvataggi!
        // (Rimuovi PlayerPrefs.DeleteAll() se vuoi gestire i salvataggi in modo diverso)
        PlayerPrefs.DeleteAll();

        // Crea una chiave per dire al gioco: "Ehi, ora esiste una partita!"
        PlayerPrefs.SetInt("Salvataggio", 1);
        PlayerPrefs.Save();

        // Carica la scena
        SceneManager.LoadScene(nomeScenaGioco);
    }

    public void ContinueGame()
    {
        Debug.Log("Caricamento partita salvata...");

        if (PlayerPrefs.HasKey("Salvataggio"))
        {
            // Carica la scena. Sarà poi il Player (nella nuova scena) a ricaricare la sua posizione
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