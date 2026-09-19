using UnityEngine;

/// <summary>
/// Decide l'ordine di disegno in base alla quota dei piedi: chi ha i piedi piu' in basso
/// e' piu' vicino alla camera e viene disegnato davanti.
/// Perche' funzioni, gli oggetti che devono confrontarsi fra loro (scenario, player, mob)
/// devono stare tutti sullo stesso sorting layer.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public class OrdinamentoY : MonoBehaviour
{
    [Tooltip("Ricalcola a ogni frame: serve a player e mob. Per gli oggetti fermi basta una volta.")]
    public bool ogniFrame = false;

    [Tooltip("Collider da cui leggere i piedi (bounds.min.y). Se vuoto usa il primo Collider2D che trova.")]
    public Collider2D riferimento;

    [Tooltip("Correzione manuale della quota dei piedi, in unita' di mondo.")]
    public float scostamentoPiedi = 0f;

    [Tooltip("Limita l'ordinamento a un'area: fuori si torna all'ordine che l'oggetto aveva in origine.")]
    public bool soloDentroArea = false;

    [Tooltip("Area in coordinate di mondo. Di default e' ZonaSpawnMob: x -199..-78, y -152..-11.")]
    public Rect area = new Rect(-199f, -152f, 121f, 141f);

    [Tooltip("Ordine da usare fuori dall'area. E' l'ordine che l'oggetto aveva prima: per il player 0.")]
    public int ordineFuoriArea = 0;

    // Un pixel vale 1/8 di unita', quindi 100 per unita' lascia margine di sovra-risoluzione
    // senza avvicinarsi al limite di sortingOrder (short).
    const float PerUnita = 100f;

    SpriteRenderer[] renderers;

    void OnEnable()
    {
        Raccogli();
        Aggiorna();
    }

    void LateUpdate()
    {
        if (ogniFrame) Aggiorna();
    }

    /// <summary>Rilegge renderer e collider. Da richiamare se la gerarchia cambia.</summary>
    public void Raccogli()
    {
        renderers = GetComponentsInChildren<SpriteRenderer>(true);

        if (riferimento == null) riferimento = GetComponent<Collider2D>();
        if (riferimento == null) riferimento = GetComponentInChildren<Collider2D>();
    }

    /// <summary>
    /// Quota dei piedi: il collider se c'e' (per player e oggetti e' la base, cioe' il punto giusto),
    /// altrimenti il bordo inferiore dello sprite. Il centro del transform non va bene: sugli NPC
    /// sta circa 1,85 unita' sopra i piedi.
    /// </summary>
    float QuotaPiedi()
    {
        if (riferimento != null) return riferimento.bounds.min.y;
        if (renderers != null && renderers.Length > 0 && renderers[0] != null) return renderers[0].bounds.min.y;
        return transform.position.y;
    }

    public void Aggiorna()
    {
        if (renderers == null || renderers.Length == 0) return;

        if (soloDentroArea && !area.Contains(transform.position))
        {
            for (int i = 0; i < renderers.Length; i++)
                if (renderers[i] != null) renderers[i].sortingOrder = ordineFuoriArea;
            return;
        }

        float piedi = QuotaPiedi() + scostamentoPiedi;
        int ordine = Mathf.Clamp(Mathf.RoundToInt(-piedi * PerUnita), short.MinValue, short.MaxValue);

        for (int i = 0; i < renderers.Length; i++)
            if (renderers[i] != null) renderers[i].sortingOrder = ordine;
    }
}
