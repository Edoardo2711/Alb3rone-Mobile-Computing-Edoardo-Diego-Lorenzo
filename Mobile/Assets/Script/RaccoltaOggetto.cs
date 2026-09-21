using UnityEngine;

/// <summary>
/// L'oggetto finale sul piedistallo della zona Oggetto. Avvicinandosi al punto di raccolta
/// (davanti al piedistallo, che e' solido) finisce nell'inventario e si segna oggettoPreso.
/// Se il salvataggio dice che e' gia' stato preso, sparisce da solo.
/// Lo sprite e il prefab d'inventario sono segnaposto finche' non si decide cos'e'.
/// </summary>
public class RaccoltaOggetto : MonoBehaviour
{
    [Tooltip("Il prefab UI che va nell'inventario (con Item e l'ID giusto nell'ItemDictionary).")]
    public GameObject prefabInventario;

    [Tooltip("Da dove si raccoglie. Vuoto = la posizione di questo oggetto.")]
    public Transform puntoRaccolta;
    public float raggio = 2f;

    [Tooltip("Compare quando lo si prende. Vuoto = niente.")]
    public string messaggio = "Hai trovato qualcosa che qualcuno ha lasciato quaggiu' tanto tempo fa.";

    [Header("Fluttua sul piedistallo")]
    public float ampiezza = 0.12f;
    public float velocita = 2f;

    private Transform player;
    private Collider2D piediPlayer;
    private Vector3 posizioneBase;

    void Start()
    {
        posizioneBase = transform.position;
        var ph = FindFirstObjectByType<PlayerHealth>();
        if (ph != null)
        {
            player = ph.transform;
            foreach (var c in ph.GetComponents<Collider2D>())
                if (!c.isTrigger) { piediPlayer = c; break; }
        }
        if (ProgressoGioco.Instance != null) ProgressoGioco.Instance.OnProgressoCambiato += Controlla;
        Controlla();
    }

    void OnDestroy()
    {
        if (ProgressoGioco.Instance != null) ProgressoGioco.Instance.OnProgressoCambiato -= Controlla;
    }

    /// <summary>Gia' preso (anche in un salvataggio caricato): non deve stare sul piedistallo.</summary>
    void Controlla()
    {
        if (ProgressoGioco.Instance != null && ProgressoGioco.Instance.oggettoPreso)
            gameObject.SetActive(false);
    }

    void Update()
    {
        transform.position = posizioneBase + Vector3.up * Mathf.Sin(Time.time * velocita) * ampiezza;

        if (player == null) return;
        Vector2 piedi = piediPlayer != null ? (Vector2)piediPlayer.bounds.center : (Vector2)player.position;
        Vector2 punto = puntoRaccolta != null ? (Vector2)puntoRaccolta.position : (Vector2)posizioneBase;
        if (Vector2.Distance(piedi, punto) > raggio) return;

        var inv = FindFirstObjectByType<InventoryController>();
        if (inv == null || !inv.AggiungiOggetto(prefabInventario))
        {
            MessaggioSchermo.Mostra("Non ho spazio per portarlo con me.");
            return;
        }
        if (ProgressoGioco.Instance != null) ProgressoGioco.Instance.SegnaOggettoPreso();
        if (!string.IsNullOrEmpty(messaggio)) MessaggioSchermo.Mostra(messaggio);
        gameObject.SetActive(false);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.8f);
        Vector3 p = puntoRaccolta != null ? puntoRaccolta.position : transform.position;
        Gizmos.DrawWireSphere(p, raggio);
    }
}
