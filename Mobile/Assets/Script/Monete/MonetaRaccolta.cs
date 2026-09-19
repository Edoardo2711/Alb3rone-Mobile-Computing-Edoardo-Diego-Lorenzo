using UnityEngine;

/// <summary>
/// La moneta a terra. Come NpcInterazione non usa collider: misura la distanza dal player.
/// Serviva per gli NPC per non farsi attraversare il trigger da zombie e palle di fuoco,
/// e qui vale lo stesso, con in piu' il vantaggio di non dipendere dalla matrice delle
/// collisioni 2D (la moneta non deve entrare in nessun layer nuovo).
/// </summary>
[DisallowMultipleComponent]
public class MonetaRaccolta : MonoBehaviour
{
    [Header("Valore")]
    [Min(1)] public int valore = 1;

    [Header("Raccolta")]
    [Tooltip("Entro quante unita' dal player la moneta viene presa.")]
    [Min(0.1f)] public float raggioRaccolta = 1.4f;

    [Tooltip("Entro quante unita' comincia a rincorrere il player. 0 = sta ferma e aspetta.")]
    public float raggioAttrazione = 3f;

    [Tooltip("Quanto veloce si avvicina, in unita' al secondo.")]
    public float velocitaAttrazione = 7f;

    [Header("Movimento sul posto")]
    [Tooltip("Di quanto sale e scende, in unita'. 0 = immobile.")]
    public float ampiezzaSaltello = 0.18f;
    public float velocitaSaltello = 3f;

    [Header("Durata")]
    [Tooltip("Dopo quanti secondi svanisce. 0 = resta a terra per sempre.")]
    public float durataVita = 0f;

    [Tooltip("Quanto dura la dissolvenza finale, presa dagli ultimi secondi di durataVita.")]
    public float durataDissolvenza = 1.5f;

    public bool debugLog = false;

    // Il player e' lo stesso per tutte le monete: cercarlo una volta basta.
    private static Transform player;

    private Vector3 posizioneLogica;   // dove sta la moneta senza il saltello
    private float faseSaltello;        // sfasata per moneta, altrimenti saltellano all'unisono
    private float nascita;
    private bool presa;
    private SpriteRenderer[] renderers;

    void Awake()
    {
        posizioneLogica = transform.position;
        faseSaltello = Random.value * Mathf.PI * 2f;
        nascita = Time.time;
        renderers = GetComponentsInChildren<SpriteRenderer>(true);
    }

    /// <summary>La usa DropMonete: sposta la moneta senza lasciare indietro il punto del saltello.</summary>
    public void Posiziona(Vector3 posizione)
    {
        posizioneLogica = posizione;
        transform.position = posizione;
    }

    void Update()
    {
        if (presa) return;

        if (player == null)
        {
            GameObject go = GameObject.FindGameObjectWithTag("Player");
            if (go == null) return;
            player = go.transform;
        }

        float distanza = Vector2.Distance(player.position, posizioneLogica);

        if (distanza <= raggioRaccolta)
        {
            Raccogli();
            return;
        }

        if (raggioAttrazione > raggioRaccolta && distanza <= raggioAttrazione)
        {
            posizioneLogica = Vector3.MoveTowards(
                posizioneLogica, player.position, velocitaAttrazione * Time.deltaTime);
        }

        float salto = ampiezzaSaltello * Mathf.Abs(Mathf.Sin(Time.time * velocitaSaltello + faseSaltello));
        transform.position = posizioneLogica + Vector3.up * salto;

        if (durataVita > 0f)
        {
            float vissuto = Time.time - nascita;
            if (vissuto >= durataVita) { Destroy(gameObject); return; }

            float inizioDissolvenza = durataVita - Mathf.Max(0.01f, durataDissolvenza);
            if (vissuto > inizioDissolvenza)
                ImpostaTrasparenza(1f - (vissuto - inizioDissolvenza) / (durataVita - inizioDissolvenza));
        }
    }

    void Raccogli()
    {
        ProgressoGioco p = ProgressoGioco.Instance;
        if (p == null)
        {
            // Senza ProgressoGioco la moneta resta a terra: sparire senza accreditare
            // nulla sarebbe peggio, perche' non si capirebbe perche' il totale non sale.
            Debug.LogWarning("[MonetaRaccolta] ProgressoGioco non in scena: la moneta resta a terra.");
            return;
        }

        presa = true;
        p.AggiungiMonete(valore);
        if (debugLog) Debug.Log($"[MonetaRaccolta] Raccolte {valore} monete.");
        Destroy(gameObject);
    }

    void ImpostaTrasparenza(float alpha)
    {
        if (renderers == null) return;
        alpha = Mathf.Clamp01(alpha);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;
            Color c = renderers[i].color;
            c.a = alpha;
            renderers[i].color = c;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, raggioRaccolta);
        if (raggioAttrazione > raggioRaccolta)
        {
            Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, raggioAttrazione);
        }
    }
}
