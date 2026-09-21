using UnityEngine;
using TMPro;

/// <summary>
/// Scrive il nome dell'account PlayFab in un testo (la scheda Player del menu di gioco).
/// Senza login mostra il testo per l'ospite.
/// </summary>
public class NomeGiocatoreUI : MonoBehaviour
{
    public TMP_Text testo;
    public string testoOspite = "GUEST";

    void OnEnable()
    {
        SessionePlayFab.OnCambiata += Aggiorna;
        Aggiorna();
    }

    void OnDisable()
    {
        SessionePlayFab.OnCambiata -= Aggiorna;
    }

    void Aggiorna()
    {
        if (testo == null) testo = GetComponent<TMP_Text>();
        if (testo != null) testo.text = SessionePlayFab.Connesso ? SessionePlayFab.NomeGiocatore.ToUpper() : testoOspite;
    }
}
