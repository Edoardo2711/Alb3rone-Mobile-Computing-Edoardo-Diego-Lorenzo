using UnityEngine;
using TMPro;

/// <summary>
/// Il contatore delle monete nell'HUD. Si aggancia a ProgressoGioco.OnMoneteCambiate,
/// cosi' non deve controllare il totale a ogni frame.
/// </summary>
public class ContatoreMonete : MonoBehaviour
{
    [Header("Dove scrivere")]
    public TMP_Text testo;

    [Tooltip("{0} e' il numero di monete.")]
    public string formato = "{0}";

    public bool debugLog = false;

    private ProgressoGioco agganciato;

    void OnEnable() => Aggancia();

    void OnDisable()
    {
        if (agganciato != null)
        {
            agganciato.OnMoneteCambiate -= Mostra;
            agganciato = null;
        }
    }

    void Update()
    {
        // ProgressoGioco assegna Instance in Awake, ma se questo HUD si accende prima
        // (ordine di Awake non garantito) il primo tentativo va a vuoto: si riprova
        // finche' non lo si trova, poi Update non serve piu' a niente.
        if (agganciato == null) Aggancia();
    }

    void Aggancia()
    {
        ProgressoGioco p = ProgressoGioco.Instance;
        if (p == null) return;

        agganciato = p;
        agganciato.OnMoneteCambiate += Mostra;
        Mostra(agganciato.monete);
        if (debugLog) Debug.Log("[ContatoreMonete] Agganciato a ProgressoGioco.");
    }

    void Mostra(int monete)
    {
        if (testo != null) testo.text = string.Format(formato, monete);
    }
}
