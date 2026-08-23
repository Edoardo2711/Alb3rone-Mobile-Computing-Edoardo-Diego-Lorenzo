using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Schermata di game over. Va sul Canvas creato da
/// Tools > UI > Costruisci schermata Game Over.
///
/// Ascolta PlayerHealth.OnDeath, mostra il pannello dopo una breve pausa e, al tocco
/// di "Riprova", chiama PlayerRespawn.Respawn().
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

        showCo = null;
    }

    /// <summary>Collegato al pulsante "Riprova".</summary>
    public void Retry()
    {
        if (showCo != null) { StopCoroutine(showCo); showCo = null; }

        if (panel != null) panel.SetActive(false);
        Time.timeScale = 1f;

        if (playerRespawn != null)
            playerRespawn.Respawn();
        else
            Debug.LogWarning("[GameOverUI] PlayerRespawn non assegnato: impossibile far rinascere il player.");
    }
}
