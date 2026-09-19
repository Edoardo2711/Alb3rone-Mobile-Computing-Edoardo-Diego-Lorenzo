using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Un messaggio che compare in basso, resta qualche secondo e svanisce.
/// Si chiama da qualunque punto con MessaggioSchermo.Mostra("..."), cosi' chi lo usa
/// (la transizione bloccata, le trappole, il boss) non deve sapere dov'e' la UI.
/// </summary>
public class MessaggioSchermo : MonoBehaviour
{
    public static MessaggioSchermo Istanza { get; private set; }

    [Header("Riferimenti")]
    [Tooltip("L'oggetto da accendere e spegnere. Vuoto = questo stesso.")]
    public GameObject pannello;
    public TMP_Text testo;

    [Tooltip("Serve per la dissolvenza. Se manca, il messaggio sparisce di colpo.")]
    public CanvasGroup gruppo;

    [Header("Tempi")]
    [Tooltip("Quanti secondi resta a schermo, dissolvenza esclusa.")]
    public float durata = 3f;
    public float dissolvenza = 0.4f;

    public bool debugLog = false;

    private Coroutine corrente;
    private string messaggioInCorso;

    void Awake()
    {
        if (Istanza != null && Istanza != this)
        {
            Debug.LogWarning("[MessaggioSchermo] Ce n'e' gia' uno in scena: questo si disattiva.");
            gameObject.SetActive(false);
            return;
        }
        Istanza = this;
        Nascondi();
    }

    void OnDestroy()
    {
        if (Istanza == this) Istanza = null;
    }

    /// <summary>Mostra il messaggio. Senza la UI in scena finisce nel log e basta.</summary>
    public static void Mostra(string messaggio)
    {
        if (string.IsNullOrEmpty(messaggio)) return;

        if (Istanza == null)
        {
            Debug.LogWarning("[MessaggioSchermo] Nessuno in scena, il messaggio non si vede: " + messaggio);
            return;
        }
        Istanza.MostraOra(messaggio);
    }

    void MostraOra(string messaggio)
    {
        // Se e' gia' a schermo lo stesso messaggio, non si ricomincia da capo: si fa
        // solo ripartire il tempo. Altrimenti, camminando avanti e indietro sul trigger,
        // il messaggio lampeggerebbe a ogni passo.
        bool stessoDiPrima = corrente != null && messaggioInCorso == messaggio;

        messaggioInCorso = messaggio;
        if (testo != null) testo.text = messaggio;
        if (debugLog) Debug.Log("[MessaggioSchermo] " + messaggio);

        if (corrente != null) StopCoroutine(corrente);
        corrente = StartCoroutine(Ciclo(stessoDiPrima));
    }

    IEnumerator Ciclo(bool giaVisibile)
    {
        GameObject p = pannello != null ? pannello : gameObject;
        p.SetActive(true);
        if (gruppo != null && !giaVisibile) gruppo.alpha = 1f;

        // Tempo non scalato: il messaggio deve vedersi anche a gioco fermo
        // (GameOverUI mette timeScale a 0 e ci resta).
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, durata));

        if (gruppo != null && dissolvenza > 0f)
        {
            float t = 0f;
            while (t < dissolvenza)
            {
                t += Time.unscaledDeltaTime;
                gruppo.alpha = 1f - Mathf.Clamp01(t / dissolvenza);
                yield return null;
            }
        }

        Nascondi();
        corrente = null;
        messaggioInCorso = null;
    }

    void Nascondi()
    {
        GameObject p = pannello != null ? pannello : gameObject;
        if (p != null && p != gameObject) p.SetActive(false);
        else if (p == gameObject && testo != null) testo.text = "";
        if (gruppo != null) gruppo.alpha = 1f;
    }
}
