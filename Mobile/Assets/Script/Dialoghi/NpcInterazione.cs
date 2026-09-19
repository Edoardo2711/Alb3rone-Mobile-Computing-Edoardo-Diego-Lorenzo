using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Rende un NPC interrogabile col tasto E. Non usa collider: misura la distanza fra il
/// player e un figlio "PuntoDialogo", che si trascina in Scene View dove il player ci
/// arriva davvero (per il fabbro, dalla parte opposta alla fucina).
/// Cosi' non tocca layer fisici, sorting layer ne' i collider gia' presenti.
/// </summary>
public class NpcInterazione : MonoBehaviour
{
    [Header("Chi e'")]
    [Tooltip("Nome mostrato nel pannello se il nodo di dialogo non ne specifica uno.")]
    public string nome;

    [Tooltip("Prima battuta. Senza questo l'NPC non e' interrogabile.")]
    public DialogoNodo dialogoIniziale;

    [Header("Dove si parla")]
    [Tooltip("Figlio vuoto che segna il punto in cui il player deve trovarsi. Vuoto = si usa l'NPC stesso.")]
    public Transform puntoDialogo;

    [Tooltip("Entro quante unita' dal punto compare la scritta.")]
    [Min(0.1f)]
    public float raggio = 2.5f;

    public bool debugLog = false;

    // Tutti gli NPC vivi. Serve a far vincere il piu' vicino quando due si sovrappongono.
    private static readonly List<NpcInterazione> tutti = new List<NpcInterazione>();
    private static Transform player;
    private static int frameElaborato = -1;

    /// <summary>L'NPC con cui si puo' parlare adesso, o null.</summary>
    public static NpcInterazione Candidato { get; private set; }

    /// <summary>Lo alza la UI del dialogo: mentre e' aperto non si cambia interlocutore.</summary>
    public static bool DialogoInCorso { get; set; }

    /// <summary>Cambia l'NPC a tiro (o non ce n'e' piu' nessuno): la UI mostra o nasconde "Premi E".</summary>
    public static event System.Action<NpcInterazione> OnCandidatoCambiato;

    /// <summary>E' stato premuto E davanti a un NPC: la UI apre il dialogo.</summary>
    public static event System.Action<NpcInterazione> OnRichiestaDialogo;

    public Vector3 PuntoInterazione => puntoDialogo != null ? puntoDialogo.position : transform.position;

    void OnEnable()
    {
        tutti.Add(this);
        if (string.IsNullOrEmpty(nome)) nome = gameObject.name;
    }

    void OnDisable()
    {
        tutti.Remove(this);
        if (Candidato == this) ImpostaCandidato(null);
    }

    void Update()
    {
        // Il lavoro e' uguale per tutti: lo fa il primo che passa e gli altri saltano.
        if (frameElaborato == Time.frameCount) return;
        frameElaborato = Time.frameCount;

        if (DialogoInCorso) return;

        if (player == null)
        {
            GameObject go = GameObject.FindGameObjectWithTag("Player");
            if (go == null) return;
            player = go.transform;
        }

        // Il piu' vicino fra quelli a tiro, cosi' con due NPC accostati non
        // compaiono due scritte e non si parla con quello sbagliato.
        NpcInterazione migliore = null;
        float minima = float.MaxValue;
        Vector2 pos = player.position;

        for (int i = 0; i < tutti.Count; i++)
        {
            NpcInterazione npc = tutti[i];
            if (npc == null || npc.dialogoIniziale == null) continue;

            float d = Vector2.Distance(pos, npc.PuntoInterazione);
            if (d <= npc.raggio && d < minima)
            {
                minima = d;
                migliore = npc;
            }
        }

        if (migliore != Candidato) ImpostaCandidato(migliore);

        if (Candidato != null && TastoPremuto())
        {
            if (Candidato.debugLog) Debug.Log("[NpcInterazione] Dialogo con " + Candidato.nome);
            if (OnRichiestaDialogo != null) OnRichiestaDialogo(Candidato);
        }
    }

    private static bool TastoPremuto()
    {
        // Keyboard.current e' null se non c'e' tastiera (telefono): li' servira' un bottone.
        return Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
    }

    private static void ImpostaCandidato(NpcInterazione npc)
    {
        Candidato = npc;
        if (OnCandidatoCambiato != null) OnCandidatoCambiato(npc);
    }

    // Il punto va piazzato a occhio in Scene View: questi disegnano dove cade e quanto e' largo.
    void OnDrawGizmos()
    {
        Vector3 p = PuntoInterazione;
        Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.9f);
        Gizmos.DrawWireSphere(p, 0.25f);
        if (puntoDialogo != null) Gizmos.DrawLine(transform.position, p);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.35f);
        Gizmos.DrawWireSphere(PuntoInterazione, raggio);
    }
}
