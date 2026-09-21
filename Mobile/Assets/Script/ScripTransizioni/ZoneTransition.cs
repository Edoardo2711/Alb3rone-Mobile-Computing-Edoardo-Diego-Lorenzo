using UnityEngine;
using Cinemachine;

/// <summary>
/// Script unificato per tutte le transizioni di zona.
/// Sostituisce: TransizioneCaF, TransizioneCaQ, TransizioneDaF,
///              TransizioneDaO, TransizioneFaC, TransizioneFaD,
///              TransizioneOaD, TransizioneQaC
///
/// Come usarlo:
/// 1. Aggiungi questo script al GameObject trigger di ogni waypoint.
/// 2. Compila i campi nel Inspector:
///    - Map Boundary   → il PolygonCollider2D della zona di destinazione
///    - Offset         → lo spostamento XY da applicare al player
///    - Change Camera Size → spunta solo se la transizione deve cambiare la ortho size
///    - New Camera Size    → valore della nuova ortho size (usato solo se Change Camera Size = true)
///    - Vcam           → trascina la CinemachineVirtualCamera (usato solo se Change Camera Size = true)
/// </summary>
public class ZoneTransition : MonoBehaviour
{
    [Header("Zona di destinazione")]
    [SerializeField] private PolygonCollider2D mapBoundary;

    [Header("Spostamento del player")]
    [SerializeField] private Vector2 offset = Vector2.zero;

    [Header("Serve la spada del fabbro?")]
    [Tooltip("Acceso: senza la spada comprata il passaggio non si apre e compare un messaggio. " +
             "Va acceso solo sul waypoint che porta al Dungeon.")]
    [SerializeField] private bool richiedeSpada = false;

    [TextArea(1, 3)]
    [SerializeField] private string messaggioSenzaSpada = "Non posso avventurarmi con una spada non affilata.";

    [Header("Serve aver ucciso il boss?")]
    [Tooltip("Acceso: finche' il demone e' vivo il passaggio non si apre. Va acceso solo sulla porta del Dungeon verso la zona Oggetto.")]
    [SerializeField] private bool richiedeBoss = false;

    [TextArea(1, 3)]
    [SerializeField] private string messaggioBossVivo = "La porta non si apre finche' il demone e' in vita.";

    [Header("Salvataggio automatico")]
    [Tooltip("Acceso: dopo il passaggio la partita si salva da sola, ma solo se il boss e' ancora vivo. " +
             "Va acceso solo sull'ingresso del Dungeon: morendo contro il boss, 'Continua' riparte da li'.")]
    [SerializeField] private bool salvaSeBossVivo = false;

    [SerializeField] private string messaggioSalvataggio = "Partita salvata.";

    [Header("Camera (opzionale)")]
    [SerializeField] private bool changeCameraSize = false;
    [SerializeField] private float newCameraSize = 5f;
    [SerializeField] private CinemachineVirtualCamera vcam;

    // Flag statico condiviso tra tutte le istanze per evitare il double-trigger:
    // quando un waypoint teletrasporta il player, gli altri trigger ignorano
    // l'evento OnTriggerEnter2D per un frame.
    private static bool isTeleporting = false;

    private CinemachineConfiner confiner;

    private void Awake()
    {
        confiner = FindFirstObjectByType<CinemachineConfiner>();

        if (changeCameraSize && vcam == null)
            vcam = FindFirstObjectByType<CinemachineVirtualCamera>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        // Protezione double-trigger: se stiamo già teletrasportando, esci subito
        if (isTeleporting) return;

        // Il controllo della spada sta QUI dentro e non in uno script a parte sullo stesso
        // trigger: fra due OnTriggerEnter2D l'ordine di esecuzione non e' garantito, e il
        // teletrasporto partirebbe lo stesso prima che l'altro faccia in tempo a fermarlo.
        if (richiedeSpada && !HaLaSpada())
        {
            MessaggioSchermo.Mostra(messaggioSenzaSpada);
            return;
        }

        if (richiedeBoss && !BossUcciso())
        {
            MessaggioSchermo.Mostra(messaggioBossVivo);
            return;
        }

        isTeleporting = true;

        // 1. Aggiorna il confiner della camera
        if (confiner != null && mapBoundary != null)
        {
            confiner.m_BoundingShape2D = mapBoundary;
            confiner.InvalidatePathCache();
        }

        // 2. Cambia ortho size se richiesto
        if (changeCameraSize && vcam != null)
            vcam.m_Lens.OrthographicSize = newCameraSize;

        // 3. Teletrasporta il player: PRIMA transform (immediato), POI rigidbody (sync fisica)
        Vector3 newPos = collision.transform.position;
        newPos.x += offset.x;
        newPos.y += offset.y;

        collision.transform.position = newPos;
        Rigidbody2D rb = collision.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.position = newPos;
            rb.linearVelocity = Vector2.zero;
        }

        // 4. Salvataggio automatico davanti al boss: dopo confiner, zoom e posizione, cosi' il
        //    salvataggio registra la zona e il punto giusti.
        if (salvaSeBossVivo && !BossUcciso())
        {
            SaveController salvataggio = FindFirstObjectByType<SaveController>();
            if (salvataggio != null)
            {
                salvataggio.SaveGame();
                if (!string.IsNullOrEmpty(messaggioSalvataggio)) MessaggioSchermo.Mostra(messaggioSalvataggio);
            }
            else
            {
                Debug.LogWarning("[ZoneTransition] SaveController non in scena: salvataggio automatico saltato.");
            }
        }

        // 5. Resetta il flag al frame successivo
        // (garantisce che i trigger della zona di destinazione non sparino subito)
        StartCoroutine(ResetTeleportFlag());
    }

    /// <summary>
    /// Senza ProgressoGioco in scena si resta chiusi fuori: e' la scelta prudente, perche'
    /// aprire il passaggio sarebbe un salto diretto al boss senza l'arma giusta.
    /// </summary>
    private bool HaLaSpada()
    {
        ProgressoGioco p = ProgressoGioco.Instance;
        if (p == null)
        {
            Debug.LogWarning("[ZoneTransition] ProgressoGioco non in scena: il passaggio resta chiuso.");
            return false;
        }
        return p.spadaComprata;
    }

    /// <summary>Come per la spada: senza ProgressoGioco in scena la porta resta chiusa.</summary>
    private bool BossUcciso()
    {
        ProgressoGioco p = ProgressoGioco.Instance;
        if (p == null)
        {
            Debug.LogWarning("[ZoneTransition] ProgressoGioco non in scena: il passaggio resta chiuso.");
            return false;
        }
        return p.bossUcciso;
    }

    private System.Collections.IEnumerator ResetTeleportFlag()
    {
        // Aspetta la fine del frame corrente + 1 frame fisico
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        isTeleporting = false;
    }
}
