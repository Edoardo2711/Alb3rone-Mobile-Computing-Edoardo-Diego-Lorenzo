using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// L'icona della pozione nell'HUD: mostra quante ne hai e ne fa bere una.
/// Si beve col bottone (che funziona anche col dito, e' un progetto mobile) o col tasto.
/// </summary>
public class UsoPozione : MonoBehaviour
{
    [Header("Riferimenti")]
    public TMP_Text contatore;

    [Tooltip("Il bottone dell'icona. Ci si aggancia da solo.")]
    public Button bottone;

    [Tooltip("L'icona, per spegnerla quando non ne hai.")]
    public Image icona;

    [Tooltip("Vuoto = si cerca il Player per tag.")]
    public PlayerHealth vita;

    [Header("Effetto")]
    [Tooltip("Quanta vita massima rida' una pozione: 0,333 = un terzo.")]
    [Range(0.05f, 1f)] public float frazioneCurata = 1f / 3f;

    [Header("Tasto")]
    public Key tasto = Key.Q;

    [Header("Messaggi")]
    public string senzaPozioni = "Non ho pozioni.";
    public string vitaPiena = "Sto bene. Non la spreco adesso.";

    [Header("Aspetto")]
    [Tooltip("Colore dell'icona quando non hai pozioni.")]
    public Color coloreVuoto = new Color(1f, 1f, 1f, 0.35f);

    public bool debugLog = false;

    private ProgressoGioco agganciato;

    void OnEnable()
    {
        if (bottone != null) bottone.onClick.AddListener(Bevi);
        Aggancia();
    }

    void OnDisable()
    {
        if (bottone != null) bottone.onClick.RemoveListener(Bevi);
        if (agganciato != null)
        {
            agganciato.OnPozioniCambiate -= Mostra;
            agganciato = null;
        }
    }

    void Update()
    {
        if (agganciato == null) Aggancia();

        // Il tasto e' una scorciatoia del bottone. Keyboard.current e' null su telefono:
        // li' resta il tocco sull'icona.
        if (Keyboard.current != null && Keyboard.current[tasto].wasPressedThisFrame)
            Bevi();
    }

    void Aggancia()
    {
        ProgressoGioco p = ProgressoGioco.Instance;
        if (p == null) return;
        agganciato = p;
        agganciato.OnPozioniCambiate += Mostra;
        Mostra(agganciato.pozioni);
    }

    void Mostra(int quante)
    {
        if (contatore != null) contatore.text = quante.ToString();
        if (icona != null) icona.color = quante > 0 ? Color.white : coloreVuoto;
    }

    public void Bevi()
    {
        // Mentre si parla non si beve: il dialogo spegne movimento e attacco, questo
        // sarebbe l'unico comando ancora vivo.
        if (NpcInterazione.DialogoInCorso) return;

        if (vita == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) vita = p.GetComponent<PlayerHealth>();
            if (vita == null) return;
        }
        if (vita.IsDead) return;

        ProgressoGioco progresso = ProgressoGioco.Instance;
        if (progresso == null || progresso.pozioni <= 0)
        {
            MessaggioSchermo.Mostra(senzaPozioni);
            return;
        }

        // A vita piena la pozione non si consuma: sprecarla per errore con un tasto
        // premuto per sbaglio sarebbe la cosa piu' antipatica di tutte.
        if (vita.VitaPiena)
        {
            MessaggioSchermo.Mostra(vitaPiena);
            return;
        }

        if (!progresso.UsaPozione()) return;
        vita.Heal(vita.maxHealth * frazioneCurata);
        if (debugLog) Debug.Log($"[UsoPozione] Bevuta: +{vita.maxHealth * frazioneCurata:0.#} vita, ne restano {progresso.pozioni}");
    }
}
