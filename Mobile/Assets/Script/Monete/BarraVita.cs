using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// La barra rossa della vita nell'HUD. Si aggancia a PlayerHealth.OnVitaCambiata,
/// cosi' non deve guardare la vita a ogni frame.
/// </summary>
public class BarraVita : MonoBehaviour
{
    [Header("Riferimenti")]
    [Tooltip("L'immagine che si accorcia. Deve avere Image Type = Filled.")]
    public Image riempimento;

    [Tooltip("Vuoto = si cerca il Player per tag.")]
    public PlayerHealth vita;

    [Header("Aspetto")]
    [Tooltip("Quanto ci mette la barra a raggiungere il valore nuovo. 0 = di scatto.")]
    public float morbidezza = 0.25f;

    [Tooltip("Colore sopra la soglia.")]
    public Color colorePieno = new Color(0.78f, 0.13f, 0.13f, 1f);

    [Tooltip("Colore quando la vita scende sotto la soglia.")]
    public Color coloreCritico = new Color(1f, 0.45f, 0.1f, 1f);

    [Range(0f, 1f)] public float sogliaCritica = 0.25f;

    private float obiettivo = 1f;

    void OnEnable()
    {
        Aggancia();
    }

    void OnDisable()
    {
        if (vita != null) vita.OnVitaCambiata -= Leggi;
    }

    void Update()
    {
        // Se il player non c'era ancora (ordine di Awake, o scena appena caricata) si riprova.
        if (vita == null) { Aggancia(); return; }

        if (riempimento == null) return;

        if (morbidezza <= 0f) riempimento.fillAmount = obiettivo;
        else riempimento.fillAmount = Mathf.MoveTowards(riempimento.fillAmount, obiettivo, Time.unscaledDeltaTime / morbidezza);
    }

    void Aggancia()
    {
        if (vita == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p == null) return;
            vita = p.GetComponent<PlayerHealth>();
            if (vita == null) return;
        }
        vita.OnVitaCambiata -= Leggi;   // niente doppie iscrizioni se OnEnable rigira
        vita.OnVitaCambiata += Leggi;
        Leggi();
    }

    void Leggi()
    {
        if (vita == null) return;
        obiettivo = vita.GetHealthPercent();
        if (riempimento != null)
            riempimento.color = obiettivo <= sogliaCritica ? coloreCritico : colorePieno;
    }
}
