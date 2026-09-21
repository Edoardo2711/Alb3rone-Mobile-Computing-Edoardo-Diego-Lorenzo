using System.Collections.Generic;
using UnityEngine;

/// <summary>Cosa deve essere vero perche' una scelta compaia nel dialogo.</summary>
public enum CondizioneScelta
{
    Nessuna,
    HaMonete,          // parametro = quante ne servono
    NonHaMonete,       // parametro = quante ne servirebbero
    SpadaComprata,
    SpadaNonComprata,
    BossUcciso,
    OggettoPreso,
    // Aggiunti dopo: sempre in fondo, i valori sono salvati come numeri negli .asset.
    OggettoDaConsegnare,   // raccolto ma non ancora dato a Ilde
    OggettoConsegnato,
    OggettoNonPreso
}

/// <summary>Cosa succede quando il player sceglie quella riga.</summary>
public enum EffettoScelta
{
    Nessuno,
    TogliMonete,       // parametro = quante
    DaiMonete,         // parametro = quante
    DaiSpada,
    SegnaBossUcciso,
    SegnaOggettoPreso,
    DaiPozioni,        // parametro = quante. ⚠ In fondo: i valori sono salvati come numeri
                       // negli .asset, e infilarlo in mezzo cambierebbe gli effetti gia' scritti
    ConsegnaOggetto    // parametro = ID dell'oggetto: lo toglie dall'inventario e segna la consegna
}

/// <summary>Un effetto con il suo numero. Una scelta puo' averne piu' d'uno: il negozio
/// deve togliere le monete E consegnare la spada nello stesso momento.</summary>
[System.Serializable]
public class EffettoDialogo
{
    public EffettoScelta tipo = EffettoScelta.Nessuno;
    [Tooltip("Il numero che serve all'effetto, per esempio quante monete.")]
    public int parametro;
}

[System.Serializable]
public class SceltaDialogo
{
    [TextArea(1, 3)]
    public string testo;

    [Tooltip("Nodo a cui si passa dopo questa scelta. Vuoto = il dialogo finisce.")]
    public DialogoNodo nodoSuccessivo;

    [Header("Condizione per mostrarla")]
    public CondizioneScelta condizione = CondizioneScelta.Nessuna;
    [Tooltip("Il numero che serve alla condizione, per esempio quante monete.")]
    public int parametroCondizione;

    [Header("Effetti quando viene scelta")]
    [Tooltip("Applicati in ordine, ma o tutti o nessuno: se le monete non bastano non succede niente.")]
    public List<EffettoDialogo> effetti = new List<EffettoDialogo>();

    /// <summary>La scelta va mostrata? Senza ProgressoGioco in scena passano solo quelle senza condizione.</summary>
    public bool Soddisfatta()
    {
        if (condizione == CondizioneScelta.Nessuna) return true;

        ProgressoGioco p = ProgressoGioco.Instance;
        if (p == null)
        {
            Debug.LogWarning("[SceltaDialogo] ProgressoGioco non in scena: la scelta \"" + testo + "\" resta nascosta.");
            return false;
        }
        return Verifica(condizione, parametroCondizione, p);
    }

    /// <summary>La condizione e' vera adesso? La usa anche NpcInterazione per scegliere la prima battuta.</summary>
    public static bool Verifica(CondizioneScelta condizione, int parametroCondizione, ProgressoGioco p)
    {
        if (condizione == CondizioneScelta.Nessuna) return true;
        if (p == null) return false;

        switch (condizione)
        {
            case CondizioneScelta.HaMonete:         return p.HaMonete(parametroCondizione);
            case CondizioneScelta.NonHaMonete:      return !p.HaMonete(parametroCondizione);
            case CondizioneScelta.SpadaComprata:    return p.spadaComprata;
            case CondizioneScelta.SpadaNonComprata: return !p.spadaComprata;
            case CondizioneScelta.BossUcciso:       return p.bossUcciso;
            case CondizioneScelta.OggettoPreso:     return p.oggettoPreso;
            case CondizioneScelta.OggettoDaConsegnare: return p.oggettoPreso && !p.oggettoConsegnato;
            case CondizioneScelta.OggettoConsegnato:   return p.oggettoConsegnato;
            case CondizioneScelta.OggettoNonPreso:     return !p.oggettoPreso;
            default:                                return true;
        }
    }

    /// <summary>
    /// Applica gli effetti. Torna false se non si poteva (monete insufficienti):
    /// in quel caso il dialogo non avanza e non viene toccato niente, altrimenti
    /// si finirebbe per pagare senza ricevere o per regalare la spada.
    /// </summary>
    public bool Applica()
    {
        if (effetti == null || effetti.Count == 0) return true;

        ProgressoGioco p = ProgressoGioco.Instance;
        if (p == null)
        {
            Debug.LogWarning("[SceltaDialogo] ProgressoGioco non in scena: effetti ignorati.");
            return true;
        }

        // Prima si somma quanto costa tutto, poi si paga: cosi' o passa tutto o niente.
        int costo = 0;
        foreach (EffettoDialogo e in effetti)
            if (e != null && e.tipo == EffettoScelta.TogliMonete) costo += e.parametro;

        if (costo > 0 && !p.HaMonete(costo)) return false;

        foreach (EffettoDialogo e in effetti)
        {
            if (e == null) continue;
            switch (e.tipo)
            {
                case EffettoScelta.TogliMonete:        p.PagaMonete(e.parametro); break;
                case EffettoScelta.DaiMonete:          p.AggiungiMonete(e.parametro); break;
                case EffettoScelta.DaiSpada:           p.SegnaSpadaComprata(); break;
                case EffettoScelta.SegnaBossUcciso:    p.SegnaBossUcciso(); break;
                case EffettoScelta.SegnaOggettoPreso:  p.SegnaOggettoPreso(); break;
                case EffettoScelta.DaiPozioni:         p.AggiungiPozioni(e.parametro); break;
                case EffettoScelta.ConsegnaOggetto:
                    InventoryController inv = Object.FindFirstObjectByType<InventoryController>();
                    if (inv == null || !inv.RimuoviOggetto(e.parametro))
                        Debug.LogWarning("[SceltaDialogo] Oggetto " + e.parametro + " non trovato nell'inventario: consegna segnata lo stesso.");
                    p.SegnaOggettoConsegnato();
                    break;
            }
        }
        return true;
    }
}

/// <summary>
/// Una battuta di dialogo con le sue risposte. I nodi si concatenano fra loro:
/// ogni scelta punta al nodo successivo, e un nodo senza scelte chiude il dialogo.
/// </summary>
[CreateAssetMenu(fileName = "NuovoDialogo", menuName = "Gioco/Nodo di dialogo")]
public class DialogoNodo : ScriptableObject
{
    [Tooltip("Nome mostrato sopra la battuta. Vuoto = si usa quello dell'NPC.")]
    public string chiParla;

    [TextArea(3, 8)]
    public string testo;

    [Tooltip("Vuoto = il dialogo si chiude dopo questa battuta.")]
    public List<SceltaDialogo> scelte = new List<SceltaDialogo>();

    /// <summary>Le scelte effettivamente mostrabili adesso, nell'ordine della lista.</summary>
    public List<SceltaDialogo> SceltePossibili()
    {
        List<SceltaDialogo> fuori = new List<SceltaDialogo>();
        foreach (SceltaDialogo s in scelte)
            if (s != null && s.Soddisfatta()) fuori.Add(s);
        return fuori;
    }
}
