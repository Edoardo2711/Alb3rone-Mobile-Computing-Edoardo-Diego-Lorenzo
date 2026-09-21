using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Il demone spettrale, boss del Dungeon.
///
/// Dorme al centro della sala finche' il player non entra nell'arena (o lo colpisce), poi lo
/// insegue fluttuando: e' un fantasma senza gambe, quindi passa sopra le tombe e non usa A*.
/// Un solo attacco, l'affondo in balzo: si raccoglie (preavviso, l'alone si accende) e si lancia
/// in linea retta; colpisce chi ha i piedi nella sua impronta a terra durante il balzo.
/// Sotto meta' vita (fase 2) carica piu' in fretta, balza 2-3 volte di fila e fra una raffica e
/// l'altra spara palle di fuoco magenta.
///
/// Anima gli sprite da se', in 8 direzioni: MobAnimatorController ne gestisce solo 4.
/// Alla morte si gira verso la camera (la dissolvenza esiste solo frontale), segna bossUcciso
/// e sparisce. Se il player muore, torna a dormire al suo posto con la vita piena.
/// </summary>
[RequireComponent(typeof(MobHealth))]
public class BossDemone : MonoBehaviour
{
    /// <summary>Ordine delle direzioni: e' l'ordine delle righe di demone.png.</summary>
    public static readonly string[] NomiDirezioni =
        { "su", "giu", "destra", "sinistra", "suDestra", "suSinistra", "giuDestra", "giuSinistra" };
    public const int FrameHover = 23;
    public const int FrameMuovi = 12;

    const int Su = 0, Giu = 1, Destra = 2, Sinistra = 3, SuDestra = 4, SuSinistra = 5, GiuDestra = 6, GiuSinistra = 7;

    [Header("Sprite (li riempie il builder)")]
    [Tooltip("8 direzioni x 23 frame: 0-16 fluttua, 17-19 si raccoglie, 20-21 balzo, 22 atterra.")]
    public Sprite[] hover;
    [Tooltip("8 direzioni x 12 frame: fluttua con le braccia basse, per spostarsi.")]
    public Sprite[] muovi;
    [Tooltip("Dissolvenza, solo frontale.")]
    public Sprite[] morte;
    public float fps = 12f;

    [Header("Riferimenti")]
    public SpriteRenderer corpo;
    [Tooltip("Alone magenta dietro al corpo: senza, sul pavimento scuro si vedrebbero solo gli occhi.")]
    public SpriteRenderer alone;
    [Tooltip("Impronta a terra: chi ci ha i piedi dentro durante il balzo viene colpito.")]
    public Collider2D zonaColpo;
    [Tooltip("Vuoto = la cerca in scena.")]
    public BarraBoss barra;
    public GameObject prefabProiettile;
    public string nomeBoss = "DEMONE SPETTRALE";

    [Header("Arena")]
    [Tooltip("Entrandoci il boss si sveglia. Coordinate di mondo.")]
    public Rect arena = new Rect(-268f, -6f, 26f, 20f);
    [Tooltip("Il boss non esce mai da qui, nemmeno col balzo: e' il pavimento della sala.")]
    public Rect limiti = new Rect(-295f, -18f, 80f, 37f);
    public float durataRisveglio = 1f;

    [Header("Movimento")]
    public float velocita = 3.5f;
    public float velocitaFase2 = 4.5f;
    [Tooltip("Sotto questa distanza dai piedi del player smette di avvicinarsi e balza.")]
    public float distanzaBalzo = 7f;

    [Header("Balzo")]
    public float lunghezzaBalzo = 8f;
    public float durataCarica = 0.7f;
    public float durataCaricaFase2 = 0.4f;
    public float durataBalzo = 0.35f;
    public float durataAtterraggio = 0.15f;
    public float durataRecupero = 1.2f;
    public float durataRecuperoFase2 = 0.5f;
    public float dannoBalzo = 20f;
    [Tooltip("Fase 2: balzi di fila, estremi compresi.")]
    public int balziMinFase2 = 2;
    public int balziMaxFase2 = 3;
    public float pausaFraBalzi = 0.25f;

    [Header("Fase 2")]
    [Range(0f, 1f)] public float sogliaFase2 = 0.5f;
    public int proiettiliPerRaffica = 3;
    [Tooltip("Gradi fra un proiettile e l'altro della raffica.")]
    public float apertura = 20f;
    public float velocitaProiettile = 8f;
    public float dannoProiettile = 10f;
    public float intervalloSparo = 3.5f;
    [Tooltip("Spara solo se il player e' almeno cosi' lontano: da vicino balza.")]
    public float distanzaMinimaSparo = 6f;
    [Tooltip("Da che altezza sopra i piedi partono i proiettili (il petto).")]
    public float altezzaSparo = 6f;

    [Header("Feedback")]
    public Color coloreColpo = new Color(1f, 0.45f, 0.75f, 1f);
    public float durataColpo = 0.1f;
    [Range(0f, 1f)] public float alfaAlone = 0.35f;
    [Range(0f, 1f)] public float alfaAloneCarica = 0.85f;

    [Header("Morte")]
    public float fpsMorte = 10f;
    [Tooltip("Compare quando muore. Vuoto = niente.")]
    public string messaggioMorte = "Il demone si dissolve. La porta in fondo alla sala e' aperta.";

    public bool debugLog = false;

    private MobHealth vita;
    private PlayerHealth player;
    private Collider2D piediPlayer;
    private Collider2D[] colliders;
    private Vector3 posizioneIniziale;
    private int direzione = Giu;
    private bool sveglio;
    private bool fase2;
    private bool morto;
    private Coroutine lampeggio;
    private ContactFilter2D filtro;
    private readonly List<Collider2D> trovati = new List<Collider2D>();

    void Awake()
    {
        vita = GetComponent<MobHealth>();
        colliders = GetComponents<Collider2D>();
        posizioneIniziale = transform.position;
        filtro = new ContactFilter2D();
        filtro.useTriggers = false;

        // Come gli altri mob: la difficolta' scala i danni (la vita la scala gia' MobHealth).
        dannoBalzo *= Difficulty.Multiplier;
        dannoProiettile *= Difficulty.Multiplier;
    }

    void OnEnable()
    {
        vita.OnHit += Colpito;
        vita.OnDeath += Muori;
    }

    void OnDisable()
    {
        vita.OnHit -= Colpito;
        vita.OnDeath -= Muori;
        if (player != null) player.OnDeath -= PlayerMorto;
        if (ProgressoGioco.Instance != null) ProgressoGioco.Instance.OnProgressoCambiato -= ControllaProgressoEvento;
    }

    void Start()
    {
        if (barra == null) barra = FindFirstObjectByType<BarraBoss>(FindObjectsInactive.Include);
        player = FindFirstObjectByType<PlayerHealth>();
        if (player != null)
        {
            player.OnDeath += PlayerMorto;
            foreach (var c in player.GetComponents<Collider2D>())
                if (!c.isTrigger) { piediPlayer = c; break; }
        }
        if (ProgressoGioco.Instance != null) ProgressoGioco.Instance.OnProgressoCambiato += ControllaProgressoEvento;

        // Caricando una partita in cui il boss e' gia' morto non deve ricomparire.
        if (ControllaProgresso()) return;

        Ricomincia();
    }

    // ---------------------------------------------------------------- ciclo principale

    IEnumerator Vita()
    {
        // Dorme al centro, fluttuando verso la camera.
        direzione = Giu;
        float t = 0f;
        while (!sveglio)
        {
            if (player != null && !player.IsDead && arena.Contains(PiediPlayer())) sveglio = true;
            MostraHover(direzione, (int)(t * fps) % 17);
            t += Time.deltaTime;
            yield return null;
        }

        if (debugLog) Debug.Log("[BossDemone] sveglio");
        if (barra != null) barra.Mostra(nomeBoss, vita.GetHealthPercent());
        yield return Fluttua(durataRisveglio, true);

        float prossimoSparo = Time.time + intervalloSparo;
        while (true)
        {
            // Si avvicina finche' non e' a tiro di balzo; in fase 2, da lontano, spara.
            t = 0f;
            while (true)
            {
                Vector2 verso = PiediPlayer() - (Vector2)transform.position;
                if (verso.magnitude <= distanzaBalzo) break;

                if (fase2 && Time.time >= prossimoSparo && verso.magnitude >= distanzaMinimaSparo)
                {
                    yield return Spara();
                    prossimoSparo = Time.time + intervalloSparo;
                    continue;
                }

                direzione = DirezioneDi(verso);
                Sposta(transform.position + (Vector3)(verso.normalized * (fase2 ? velocitaFase2 : velocita) * Time.deltaTime));
                MostraMuovi(direzione, (int)(t * fps) % FrameMuovi);
                t += Time.deltaTime;
                yield return null;
            }

            int balzi = fase2 ? Random.Range(balziMinFase2, balziMaxFase2 + 1) : 1;
            for (int i = 0; i < balzi; i++)
            {
                yield return Balza();
                if (i < balzi - 1) yield return Fluttua(pausaFraBalzi, false);
            }

            yield return Fluttua(fase2 ? durataRecuperoFase2 : durataRecupero, true);
        }
    }

    IEnumerator Balza()
    {
        // La direzione si decide all'inizio della carica e non cambia piu': e' il preavviso che
        // permette di scansarsi.
        Vector2 verso = PiediPlayer() - (Vector2)transform.position;
        if (verso.sqrMagnitude < 0.01f) verso = Vector2.down;
        direzione = DirezioneDi(verso);
        Vector3 partenza = transform.position;
        Vector3 arrivo = Limita(partenza + (Vector3)(verso.normalized * lunghezzaBalzo));

        // Carica: si raccoglie (frame 17-19) e l'alone si accende.
        float carica = fase2 ? durataCaricaFase2 : durataCarica;
        for (float t = 0f; t < carica; t += Time.deltaTime)
        {
            float k = t / carica;
            MostraHover(direzione, 17 + Mathf.Min(2, (int)(k * 3f)));
            ImpostaAlone(Mathf.Lerp(alfaAlone, alfaAloneCarica, k));
            yield return null;
        }

        // Balzo: frame 20-21, si sposta davvero e colpisce una volta sola.
        bool colpito = false;
        for (float t = 0f; t < durataBalzo; t += Time.deltaTime)
        {
            float k = t / durataBalzo;
            MostraHover(direzione, 20 + Mathf.Min(1, (int)(k * 2f)));
            Sposta(Vector3.Lerp(partenza, arrivo, k));
            if (!colpito && PlayerNellImpronta())
            {
                colpito = true;
                player.TakeDamage(dannoBalzo);
            }
            yield return null;
        }
        Sposta(arrivo);
        if (!colpito && PlayerNellImpronta()) player.TakeDamage(dannoBalzo);

        MostraHover(direzione, 22);
        ImpostaAlone(alfaAlone);
        yield return new WaitForSeconds(durataAtterraggio);
    }

    IEnumerator Spara()
    {
        Vector2 verso = PiediPlayer() - (Vector2)transform.position;
        direzione = DirezioneDi(verso);
        yield return Fluttua(0.4f, false);

        if (prefabProiettile != null && player != null)
        {
            Vector3 bocca = transform.position + Vector3.up * altezzaSparo;
            Vector2 mira = BersaglioPlayer() - (Vector2)bocca;
            float base0 = Mathf.Atan2(mira.y, mira.x) * Mathf.Rad2Deg;
            for (int i = 0; i < proiettiliPerRaffica; i++)
            {
                float ang = base0 + (i - (proiettiliPerRaffica - 1) * 0.5f) * apertura;
                Vector2 dir = new Vector2(Mathf.Cos(ang * Mathf.Deg2Rad), Mathf.Sin(ang * Mathf.Deg2Rad));
                var go = Instantiate(prefabProiettile, bocca, Quaternion.identity);
                var fb = go.GetComponent<Fireball>();
                if (fb != null) fb.Launch(dir, velocitaProiettile, dannoProiettile);
            }
        }
        yield return Fluttua(0.3f, false);
    }

    /// <summary>Resta sul posto fluttuando; con 'guarda' continua a girarsi verso il player.</summary>
    IEnumerator Fluttua(float secondi, bool guarda)
    {
        for (float t = 0f; t < secondi; t += Time.deltaTime)
        {
            if (guarda && player != null) direzione = DirezioneDi(PiediPlayer() - (Vector2)transform.position);
            MostraHover(direzione, (int)(t * fps) % 17);
            yield return null;
        }
    }

    // ---------------------------------------------------------------- eventi

    void Colpito()
    {
        if (morto) return;
        if (!sveglio) sveglio = true;

        float p = vita.GetHealthPercent();
        if (barra != null) barra.Imposta(p);
        if (!fase2 && p <= sogliaFase2)
        {
            fase2 = true;
            if (debugLog) Debug.Log("[BossDemone] fase 2");
        }

        if (lampeggio != null) StopCoroutine(lampeggio);
        lampeggio = StartCoroutine(Lampeggia());
    }

    IEnumerator Lampeggia()
    {
        corpo.color = coloreColpo;
        yield return new WaitForSeconds(durataColpo);
        corpo.color = Color.white;
        lampeggio = null;
    }

    void Muori()
    {
        if (morto) return;
        morto = true;
        StopAllCoroutines();
        lampeggio = null;
        corpo.color = Color.white;
        foreach (var c in colliders) c.enabled = false;
        if (barra != null) barra.Imposta(0f);
        StartCoroutine(Dissolvi());
    }

    IEnumerator Dissolvi()
    {
        float durata = morte.Length / Mathf.Max(1f, fpsMorte);
        for (float t = 0f; t < durata; t += Time.deltaTime)
        {
            int i = Mathf.Min(morte.Length - 1, (int)(t * fpsMorte));
            if (morte.Length > 0) corpo.sprite = morte[i];
            ImpostaAlone(alfaAlone * (1f - t / durata));
            yield return null;
        }

        if (barra != null) barra.Nascondi();
        if (ProgressoGioco.Instance != null) ProgressoGioco.Instance.SegnaBossUcciso();
        if (!string.IsNullOrEmpty(messaggioMorte)) MessaggioSchermo.Mostra(messaggioMorte);
        if (debugLog) Debug.Log("[BossDemone] morto");
        gameObject.SetActive(false);
    }

    /// <summary>Il player e' morto: si torna a dormire al proprio posto, vita piena.</summary>
    void PlayerMorto()
    {
        if (morto || !isActiveAndEnabled) return;
        Ricomincia();
    }

    void Ricomincia()
    {
        StopAllCoroutines();
        lampeggio = null;
        sveglio = false;
        fase2 = false;
        vita.Ripristina();
        transform.position = posizioneIniziale;
        corpo.color = Color.white;
        ImpostaAlone(alfaAlone);
        if (barra != null) barra.Nascondi();
        StartCoroutine(Vita());
    }

    /// <summary>Torna true (e spegne il boss) se il salvataggio dice che e' gia' stato ucciso.</summary>
    bool ControllaProgresso()
    {
        var p = ProgressoGioco.Instance;
        if (p == null || !p.bossUcciso || morto) return false;
        morto = true;
        if (barra != null) barra.Nascondi();
        gameObject.SetActive(false);
        return true;
    }

    void ControllaProgressoEvento() => ControllaProgresso();

    // ---------------------------------------------------------------- utilita'

    Vector2 PiediPlayer()
    {
        if (piediPlayer != null) return piediPlayer.bounds.center;
        return player != null ? (Vector2)player.transform.position : (Vector2)transform.position;
    }

    /// <summary>Dove mirare coi proiettili: il centro del collider del player, come MobRangedAttack.</summary>
    Vector2 BersaglioPlayer() => PiediPlayer();

    bool PlayerNellImpronta()
    {
        if (player == null || player.IsDead || zonaColpo == null) return false;
        trovati.Clear();
        zonaColpo.Overlap(filtro, trovati);
        for (int i = 0; i < trovati.Count; i++)
            if (trovati[i].GetComponentInParent<PlayerHealth>() == player) return true;
        return false;
    }

    void Sposta(Vector3 p) => transform.position = Limita(p);

    Vector3 Limita(Vector3 p)
    {
        p.x = Mathf.Clamp(p.x, limiti.xMin, limiti.xMax);
        p.y = Mathf.Clamp(p.y, limiti.yMin, limiti.yMax);
        return p;
    }

    static int DirezioneDi(Vector2 v)
    {
        if (v.sqrMagnitude < 0.0001f) return Giu;
        int settore = Mathf.RoundToInt(Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg / 45f);
        switch ((settore + 8) % 8)
        {
            case 0: return Destra;
            case 1: return SuDestra;
            case 2: return Su;
            case 3: return SuSinistra;
            case 4: return Sinistra;
            case 5: return GiuSinistra;
            case 6: return Giu;
            default: return GiuDestra;
        }
    }

    void MostraHover(int dir, int frame)
    {
        int i = dir * FrameHover + frame;
        if (hover != null && i < hover.Length) corpo.sprite = hover[i];
    }

    void MostraMuovi(int dir, int frame)
    {
        int i = dir * FrameMuovi + frame;
        if (muovi != null && i < muovi.Length) corpo.sprite = muovi[i];
    }

    void ImpostaAlone(float a)
    {
        if (alone == null) return;
        var c = alone.color;
        c.a = a;
        alone.color = c;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.2f, 0.8f, 0.8f);
        Gizmos.DrawWireCube(arena.center, arena.size);
        Gizmos.color = new Color(1f, 1f, 1f, 0.4f);
        Gizmos.DrawWireCube(limiti.center, limiti.size);
    }
}
