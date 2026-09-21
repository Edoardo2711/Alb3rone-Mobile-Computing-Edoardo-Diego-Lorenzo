using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Trappola a fasi: riposo -> preavviso -> salita -> attiva -> discesa -> ricarica.
/// Fa danno solo nella fase attiva, a chi ha i piedi dentro la zona di danno.
/// Anima gli sprite da se': i tempi di ogni fase si tarano da qui, senza Animator.
///
/// Due modi di scattare:
///  - Prossimita': resta a riposo finche' il player non entra nella zona (le spine);
///  - Ciclo: si accende e si spegne a ritmo fisso, sempre (il fuoco).
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class Trappola : MonoBehaviour
{
    public enum Innesco { Prossimita, Ciclo }

    [Header("Innesco")]
    public Innesco innesco = Innesco.Prossimita;
    [Tooltip("Solo per Ciclo: secondi prima del primo scatto. Serve a sfasare trappole vicine.")]
    public float ritardoIniziale = 0f;

    [Header("Frame")]
    public Sprite[] frameRiposo;
    [Tooltip("Ripetuti per tutta la durata del preavviso.")]
    public Sprite[] framePreavviso;
    public Sprite[] frameSalita;
    [Tooltip("Ripetuti per tutta la durata della fase attiva.")]
    public Sprite[] frameAttiva;
    public Sprite[] frameDiscesa;
    [Tooltip("Frame al secondo di tutte le fasi.")]
    public float fps = 12f;

    [Header("Tempi (secondi)")]
    public float durataPreavviso = 0.5f;
    public float durataAttiva = 0.8f;
    [Tooltip("Pausa a riposo dopo la discesa. Nel Ciclo e' il tempo in cui la trappola sta spenta.")]
    public float ricarica = 1f;

    [Header("Danno")]
    public float danno = 10f;
    [Tooltip("Ogni quanto ripete il danno se si resta dentro. PlayerHealth ha gia' 0,3s di invulnerabilita'.")]
    public float intervalloDanno = 0.5f;
    [Tooltip("Zona in cui si prende danno (e, con Prossimita', in cui si fa scattare). Vuoto = il Collider2D di questo oggetto.")]
    public Collider2D zonaDanno;

    [Header("Disegno")]
    [Tooltip("A riposo la trappola e' piatta: sta sotto il player.")]
    public string layerATerra = "CamminaDavanti";
    [Tooltip("Alzata copre chi le sta dietro: si ordina per Y insieme a player, mob e oggetti.")]
    public string layerAlzata = "Player";

    public bool debugLog = false;

    private SpriteRenderer sr;
    private ContactFilter2D filtro;
    private readonly List<Collider2D> trovati = new List<Collider2D>();
    private float prossimoDanno;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (zonaDanno == null) zonaDanno = GetComponent<Collider2D>();

        // Solo collider "veri": i piedi del player non sono un trigger, le zone di spawn e i waypoint si'.
        filtro = new ContactFilter2D();
        filtro.useTriggers = false;
    }

    void OnEnable()
    {
        MettiATerra();
        Mostra(frameRiposo, 0);
        StartCoroutine(Ciclo());
    }

    IEnumerator Ciclo()
    {
        if (innesco == Innesco.Ciclo && ritardoIniziale > 0f)
            yield return Riposo(ritardoIniziale);

        while (true)
        {
            if (innesco == Innesco.Prossimita)
            {
                Mostra(frameRiposo, 0);
                while (TrovaPlayer() == null) yield return null;
            }

            if (debugLog) Debug.Log($"[Trappola] {name}: scatta");

            yield return Suona(framePreavviso, durataPreavviso, false);

            Alza();
            yield return Suona(frameSalita, -1f, false);

            prossimoDanno = 0f;
            yield return Suona(frameAttiva, durataAttiva, true);

            yield return Suona(frameDiscesa, -1f, false);
            MettiATerra();

            yield return Riposo(ricarica);
        }
    }

    IEnumerator Riposo(float secondi)
    {
        Mostra(frameRiposo, 0);
        yield return Suona(frameRiposo, secondi, false);
    }

    /// <summary>
    /// Scorre i frame. Con durata &lt; 0 li mostra una volta sola; altrimenti li ripete fino
    /// a riempire la durata. Con 'colpisce' controlla il danno a ogni frame di gioco.
    /// </summary>
    IEnumerator Suona(Sprite[] frames, float durata, bool colpisce)
    {
        bool vuoto = frames == null || frames.Length == 0;
        float passo = 1f / Mathf.Max(1f, fps);
        if (durata < 0f) durata = vuoto ? 0f : frames.Length * passo;

        float t = 0f;
        while (t < durata)
        {
            if (!vuoto) Mostra(frames, (int)(t / passo) % frames.Length);
            if (colpisce) Colpisci();
            yield return null;
            t += Time.deltaTime;
        }
    }

    void Colpisci()
    {
        if (Time.time < prossimoDanno) return;

        var player = TrovaPlayer();
        if (player == null || player.IsDead) return;

        prossimoDanno = Time.time + intervalloDanno;
        player.TakeDamage(danno);
    }

    PlayerHealth TrovaPlayer()
    {
        if (zonaDanno == null) return null;

        trovati.Clear();
        zonaDanno.Overlap(filtro, trovati);
        for (int i = 0; i < trovati.Count; i++)
        {
            var ph = trovati[i].GetComponentInParent<PlayerHealth>();
            if (ph != null) return ph;
        }
        return null;
    }

    void Mostra(Sprite[] frames, int i)
    {
        if (frames != null && frames.Length > 0) sr.sprite = frames[i];
    }

    void MettiATerra()
    {
        sr.sortingLayerName = layerATerra;
        sr.sortingOrder = 0;
    }

    /// <summary>Stessa regola di OrdinamentoY, coi piedi alla base della trappola (il pivot).</summary>
    void Alza()
    {
        sr.sortingLayerName = layerAlzata;
        sr.sortingOrder = Mathf.Clamp(Mathf.RoundToInt(-transform.position.y * 100f), short.MinValue, short.MaxValue);
    }

    void OnDrawGizmosSelected()
    {
        var z = zonaDanno != null ? zonaDanno : GetComponent<Collider2D>();
        if (z == null) return;
        Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.8f);
        Gizmos.DrawWireCube(z.bounds.center, z.bounds.size);
    }
}
