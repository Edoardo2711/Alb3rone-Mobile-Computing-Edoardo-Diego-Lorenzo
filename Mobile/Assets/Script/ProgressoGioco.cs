using UnityEngine;

/// <summary>
/// Tiene lo stato della quest nascosta: monete raccolte e tappe raggiunte.
/// Va messo sul GameObject "GameManager" della scena.
/// </summary>
public class ProgressoGioco : MonoBehaviour
{
    // Ci si arriva da dovunque senza doverlo trascinare in Inspector ogni volta
    // (il negozio del fabbro, il drop dei mob, la transizione al Dungeon).
    public static ProgressoGioco Instance { get; private set; }

    [Header("Stato")]
    public int monete = 0;
    public int pozioni = 0;
    public bool spadaComprata = false;
    public bool bossUcciso = false;
    public bool oggettoPreso = false;
    [Tooltip("L'oggetto finale e' stato riportato a Ilde (e tolto dall'inventario).")]
    public bool oggettoConsegnato = false;

    [Header("Debug")]
    public bool debugLog = true;

    /// <summary>Scatta a ogni variazione delle monete, col totale nuovo. Lo usa il contatore nell'UI.</summary>
    public event System.Action<int> OnMoneteCambiate;

    /// <summary>Scatta a ogni variazione delle pozioni, col totale nuovo. La usa il contatore nell'HUD.</summary>
    public event System.Action<int> OnPozioniCambiate;

    /// <summary>Scatta quando cambia una delle tappe (spada, boss, oggetto) o dopo un caricamento.</summary>
    public event System.Action OnProgressoCambiato;

    void Awake()
    {
        // Una sola copia: se la scena ne contiene due (merge sbagliato, prefab duplicato)
        // la seconda si toglie di mezzo invece di sovrascrivere i dati della prima.
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[ProgressoGioco] Ce n'e' gia' uno in scena: questo si disattiva.");
            enabled = false;
            return;
        }
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void AggiungiMonete(int quante)
    {
        if (quante <= 0) return;
        monete += quante;
        if (debugLog) Debug.Log($"[ProgressoGioco] +{quante} monete, totale {monete}");
        OnMoneteCambiate?.Invoke(monete);
    }

    public bool HaMonete(int quante) => monete >= quante;

    public void AggiungiPozioni(int quante)
    {
        if (quante <= 0) return;
        pozioni += quante;
        if (debugLog) Debug.Log($"[ProgressoGioco] +{quante} pozioni, totale {pozioni}");
        OnPozioniCambiate?.Invoke(pozioni);
    }

    /// <summary>Ne consuma una. Torna false (e non toglie niente) se non ce ne sono.</summary>
    public bool UsaPozione()
    {
        if (pozioni <= 0) return false;
        pozioni--;
        if (debugLog) Debug.Log($"[ProgressoGioco] Pozione bevuta, ne restano {pozioni}");
        OnPozioniCambiate?.Invoke(pozioni);
        return true;
    }

    /// <summary>Paga se ci sono abbastanza monete. Torna false (e non toglie niente) se non bastano.</summary>
    public bool PagaMonete(int quante)
    {
        if (quante < 0 || !HaMonete(quante)) return false;
        monete -= quante;
        if (debugLog) Debug.Log($"[ProgressoGioco] -{quante} monete, restano {monete}");
        OnMoneteCambiate?.Invoke(monete);
        return true;
    }

    public void SegnaSpadaComprata()
    {
        if (spadaComprata) return;
        spadaComprata = true;
        if (debugLog) Debug.Log("[ProgressoGioco] Spada comprata: il Dungeon e' accessibile.");
        OnProgressoCambiato?.Invoke();
    }

    public void SegnaBossUcciso()
    {
        if (bossUcciso) return;
        bossUcciso = true;
        if (debugLog) Debug.Log("[ProgressoGioco] Boss ucciso.");
        OnProgressoCambiato?.Invoke();
    }

    public void SegnaOggettoPreso()
    {
        if (oggettoPreso) return;
        oggettoPreso = true;
        if (debugLog) Debug.Log("[ProgressoGioco] Oggetto finale raccolto.");
        OnProgressoCambiato?.Invoke();
    }

    public void SegnaOggettoConsegnato()
    {
        if (oggettoConsegnato) return;
        oggettoConsegnato = true;
        if (debugLog) Debug.Log("[ProgressoGioco] Oggetto finale consegnato a Ilde.");
        OnProgressoCambiato?.Invoke();
    }

    /// <summary>Riporta tutto a zero: la usa "Nuova Partita".</summary>
    public void Azzera()
    {
        monete = 0;
        pozioni = 0;
        spadaComprata = false;
        bossUcciso = false;
        oggettoPreso = false;
        oggettoConsegnato = false;
        OnMoneteCambiate?.Invoke(monete);
        OnPozioniCambiate?.Invoke(pozioni);
        OnProgressoCambiato?.Invoke();
    }

    /// <summary>Rimette lo stato letto dal salvataggio, avvisando UI e resto del gioco.</summary>
    public void Applica(int moneteSalvate, int pozioniSalvate, bool spada, bool boss, bool oggetto, bool consegnato = false)
    {
        monete = Mathf.Max(0, moneteSalvate);
        pozioni = Mathf.Max(0, pozioniSalvate);
        spadaComprata = spada;
        bossUcciso = boss;
        oggettoPreso = oggetto;
        oggettoConsegnato = consegnato;
        if (debugLog)
            Debug.Log($"[ProgressoGioco] Caricato: {monete} monete, {pozioni} pozioni, spada={spada}, boss={boss}, oggetto={oggetto}");
        OnMoneteCambiate?.Invoke(monete);
        OnPozioniCambiate?.Invoke(pozioni);
        OnProgressoCambiato?.Invoke();
    }
}
