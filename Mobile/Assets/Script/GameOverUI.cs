using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Schermata di game over. Va sul Canvas creato da
/// Tools > UI > Costruisci schermata Game Over.
///
/// Ascolta PlayerHealth.OnDeath e mostra il pannello dopo una breve pausa. Con tornaAlMenu
/// (default) dopo qualche secondo, o al tocco del pulsante, si torna al menu principale;
/// senza, il pulsante chiama PlayerRespawn.Respawn().
/// </summary>
public class GameOverUI : MonoBehaviour
{
    [Header("Riferimenti UI")]
    [Tooltip("Il pannello da mostrare alla morte. Deve partire disattivato.")]
    [SerializeField] private GameObject panel;
    [SerializeField] private Button retryButton;

    [Header("Riferimenti Player")]
    [Tooltip("Lasciare vuoti per cercarli automaticamente in scena.")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerRespawn playerRespawn;

    [Header("Comportamento")]
    [Tooltip("Secondi di attesa tra la morte e la comparsa del pannello.")]
    [SerializeField] private float showDelay = 1f;
    [Tooltip("Se true, congela il gioco (timeScale = 0) mentre il pannello e' visibile.")]
    [SerializeField] private bool pauseGame = true;

    [Header("Ritorno al menu")]
    [Tooltip("Acceso: alla morte si torna al menu principale (da li' Continua o Nuova Partita). " +
             "Spento: il pulsante fa rinascere il player allo start, come prima.")]
    [SerializeField] private bool tornaAlMenu = true;
    [SerializeField] private string scenaMenu = "MainMenu";
    [Tooltip("Secondi (tempo reale) in cui 'SEI MORTO' resta a schermo prima di tornare al menu da solo.")]
    [SerializeField] private float attesaMenu = 2.5f;

    private Coroutine showCo;

    private void Awake()
    {
        if (playerHealth == null)  playerHealth  = FindFirstObjectByType<PlayerHealth>();
        if (playerRespawn == null && playerHealth != null)
            playerRespawn = playerHealth.GetComponent<PlayerRespawn>();

        if (panel != null) panel.SetActive(false);
    }

    private void OnEnable()
    {
        if (playerHealth != null) playerHealth.OnDeath += HandleDeath;
        if (retryButton != null)  retryButton.onClick.AddListener(Retry);
    }

    private void OnDisable()
    {
        if (playerHealth != null) playerHealth.OnDeath -= HandleDeath;
        if (retryButton != null)  retryButton.onClick.RemoveListener(Retry);

        // Se veniamo disabilitati con il gioco in pausa, non lasciamolo congelato.
        if (pauseGame) Time.timeScale = 1f;
    }

    private void HandleDeath()
    {
        if (showCo != null) StopCoroutine(showCo);
        showCo = StartCoroutine(ShowAfterDelay());
    }

    private IEnumerator ShowAfterDelay()
    {
        // Realtime: con pauseGame attivo il tempo di gioco e' fermo, ma la pausa qui
        // avviene comunque prima del congelamento; usare il tempo reale rende
        // l'attesa indipendente da qualunque timeScale gia' impostato altrove.
        if (showDelay > 0f) yield return new WaitForSecondsRealtime(showDelay);

        if (panel != null) panel.SetActive(true);
        if (pauseGame) Time.timeScale = 0f;

        if (tornaAlMenu)
        {
            if (attesaMenu > 0f) yield return new WaitForSecondsRealtime(attesaMenu);
            VaiAlMenu();
        }

        showCo = null;
    }

    /// <summary>Carica il menu principale. Il timeScale va rimesso a 1 prima: e' statico e sopravvive al cambio di scena.</summary>
    public void VaiAlMenu()
    {
        if (showCo != null) { StopCoroutine(showCo); showCo = null; }
        Time.timeScale = 1f;
        SceneManager.LoadScene(scenaMenu);
    }

    /// <summary>Collegato al pulsante: con tornaAlMenu porta al menu subito, altrimenti fa rinascere.</summary>
    public void Retry()
    {
        if (tornaAlMenu) { VaiAlMenu(); return; }

        if (showCo != null) { StopCoroutine(showCo); showCo = null; }

        if (panel != null) panel.SetActive(false);
        Time.timeScale = 1f;

        if (playerRespawn != null)
            playerRespawn.Respawn();
        else
            Debug.LogWarning("[GameOverUI] PlayerRespawn non assegnato: impossibile far rinascere il player.");
    }
}
