using UnityEngine;
using TMPro;

/// <summary>
/// Traduce "spadaComprata" in qualcosa che si sente in mano: alza il danno dell'attacco
/// e aggiorna la scritta dell'arma nella scheda Player.
///
/// Sta sul Player e non dentro ProgressoGioco o nel dialogo, perche' cosi' nessuno dei due
/// deve sapere che esistono PlayerAttacco e la UI. Riapplica tutto anche allo Start, quindi
/// un salvataggio caricato riparte con la spada giusta senza bisogno di riparlare col fabbro.
/// </summary>
[DisallowMultipleComponent]
public class ArmaEquipaggiata : MonoBehaviour
{
    [Header("Riferimenti")]
    [Tooltip("Vuoto = si cerca su questo stesso oggetto.")]
    public PlayerAttacco attacco;

    [Tooltip("La scritta WEAPON nella scheda Player. Vuoto = si cerca per nome in scena.")]
    public TMP_Text testoArma;

    [Tooltip("Nome dell'oggetto da cercare se testoArma e' vuoto.")]
    public string nomeTestoArma = "WeaponText";

    [Header("Danno")]
    public float dannoBase = 25f;
    public float dannoSpada = 60f;

    [Header("Scritta")]
    public string formato = "WEAPON: {0}";
    public string nomeArmaBase = "SWORD";
    public string nomeArmaSpada = "GREAT SWORD";

    public bool debugLog = true;

    private ProgressoGioco agganciato;

    void Start()
    {
        if (attacco == null) attacco = GetComponent<PlayerAttacco>();
        if (attacco == null)
            Debug.LogWarning("[ArmaEquipaggiata] PlayerAttacco non trovato: il danno non verra' cambiato.");

        if (testoArma == null) testoArma = CercaTestoArma();

        Aggancia();
        Applica();
    }

    void OnDestroy()
    {
        if (agganciato != null)
        {
            agganciato.OnProgressoCambiato -= Applica;
            agganciato = null;
        }
    }

    void Update()
    {
        // Come per il contatore: se ProgressoGioco non c'era ancora allo Start si riprova.
        if (agganciato == null && Aggancia()) Applica();
    }

    bool Aggancia()
    {
        ProgressoGioco p = ProgressoGioco.Instance;
        if (p == null) return false;
        agganciato = p;
        agganciato.OnProgressoCambiato += Applica;
        return true;
    }

    void Applica()
    {
        bool spada = agganciato != null && agganciato.spadaComprata;

        if (attacco != null) attacco.attackDamage = spada ? dannoSpada : dannoBase;

        if (testoArma != null)
            testoArma.text = string.Format(formato, spada ? nomeArmaSpada : nomeArmaBase);

        if (debugLog)
            Debug.Log($"[ArmaEquipaggiata] spada={spada}, danno={(attacco != null ? attacco.attackDamage : 0f)}");
    }

    /// <summary>
    /// WeaponText sta dentro la scheda Player del menu a tab, che all'avvio e' spenta:
    /// GameObject.Find non vede gli oggetti disattivati, quindi serve la ricerca fra tutti
    /// i TMP_Text inattivi compresi.
    /// </summary>
    TMP_Text CercaTestoArma()
    {
        if (string.IsNullOrEmpty(nomeTestoArma)) return null;

        // ⚠ In scena c'e' un secondo Canvas "UI" spento, copia identica del prefab: di
        // WeaponText ce ne sono due. Vince quello acceso, altrimenti si scriverebbe
        // nella copia che nessuno vede.
        TMP_Text[] tutti = FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        TMP_Text ripiego = null;
        foreach (TMP_Text t in tutti)
        {
            if (t == null || t.gameObject.name != nomeTestoArma) continue;
            Canvas radice = t.GetComponentInParent<Canvas>(true);
            if (radice != null && radice.gameObject.activeInHierarchy) return t;
            if (ripiego == null) ripiego = t;
        }
        if (ripiego != null) return ripiego;

        Debug.LogWarning($"[ArmaEquipaggiata] Nessun TMP_Text chiamato \"{nomeTestoArma}\" in scena.");
        return null;
    }
}
