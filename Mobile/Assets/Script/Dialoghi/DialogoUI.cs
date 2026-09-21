using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Il pannello del dialogo. Si aggancia agli eventi statici di NpcInterazione, quindi
/// quello script non sa niente della UI e si puo' cambiare l'una senza toccare l'altro.
/// Mentre il dialogo e' aperto il player non si muove e non attacca.
/// </summary>
public class DialogoUI : MonoBehaviour
{
    public static DialogoUI Istanza { get; private set; }

    [Header("Scritta \"Premi E\"")]
    public GameObject prompt;
    public TMP_Text promptTesto;

    [Header("Pannello")]
    public GameObject pannello;
    public TMP_Text nomeTesto;
    public TMP_Text corpoTesto;

    [Tooltip("Dove vengono messi i bottoni delle scelte.")]
    public Transform contenitoreScelte;

    [Tooltip("Bottone spento usato come stampo: ne vengono clonati quanti servono.")]
    public GameObject bottoneStampo;

    [Header("Comportamento")]
    [Tooltip("Si possono scegliere le risposte anche coi tasti 1-9.")]
    public bool sceltaCoiNumeri = true;

    [Tooltip("Riga aggiunta sotto la battuta quando le monete non bastano.")]
    public string messaggioMoneteInsufficienti = "Non hai abbastanza monete.";
    public bool debugLog = false;

    private NpcInterazione interlocutore;
    private DialogoNodo nodoCorrente;
    private readonly List<SceltaDialogo> scelteMostrate = new List<SceltaDialogo>();

    private MovementPlayer movimento;
    private PlayerAttacco attacco;
    private Rigidbody2D corpoPlayer;

    public bool Aperto => nodoCorrente != null;

    void Awake()
    {
        if (Istanza != null && Istanza != this)
        {
            Debug.LogWarning("[DialogoUI] Ce n'e' gia' una in scena: questa si disattiva.");
            gameObject.SetActive(false);
            return;
        }
        Istanza = this;
        if (pannello != null) pannello.SetActive(false);
        if (prompt != null) prompt.SetActive(false);
        if (bottoneStampo != null) bottoneStampo.SetActive(false);
    }

    void OnEnable()
    {
        NpcInterazione.OnCandidatoCambiato += MostraPrompt;
        NpcInterazione.OnRichiestaDialogo += Apri;
    }

    void OnDisable()
    {
        NpcInterazione.OnCandidatoCambiato -= MostraPrompt;
        NpcInterazione.OnRichiestaDialogo -= Apri;

        // Rete di sicurezza: se questa UI sparisce mentre il dialogo e' aperto (play
        // fermato a meta' battuta, scena ricaricata, oggetto distrutto) il player
        // resterebbe con movimento e attacco spenti. Peggio: se la scena viene salvata
        // in quel momento, resta bloccato anche nel file.
        if (Aperto)
        {
            nodoCorrente = null;
            BloccaPlayer(false);
        }
        NpcInterazione.DialogoInCorso = false;

        if (Istanza == this) Istanza = null;
    }

    void Update()
    {
        if (!Aperto || !sceltaCoiNumeri || Keyboard.current == null) return;

        // Tasti 1-9 come scorciatoia per le risposte, nell'ordine in cui sono a schermo.
        for (int i = 0; i < scelteMostrate.Count && i < 9; i++)
        {
            Key tasto = Key.Digit1 + i;
            if (Keyboard.current[tasto].wasPressedThisFrame)
            {
                Scegli(i);
                return;
            }
        }
    }

    private void MostraPrompt(NpcInterazione npc)
    {
        if (prompt == null) return;
        bool mostra = npc != null && !Aperto;
        prompt.SetActive(mostra);
        if (mostra && promptTesto != null)
            promptTesto.text = "Premi E per parlare con " + npc.nome;
    }

    public void Apri(NpcInterazione npc)
    {
        if (npc == null || npc.NodoIniziale == null || Aperto) return;

        interlocutore = npc;
        NpcInterazione.DialogoInCorso = true;
        if (prompt != null) prompt.SetActive(false);
        BloccaPlayer(true);
        if (pannello != null) pannello.SetActive(true);
        MostraNodo(npc.NodoIniziale);
    }

    public void Chiudi()
    {
        nodoCorrente = null;
        interlocutore = null;
        PulisciBottoni();
        if (pannello != null) pannello.SetActive(false);
        BloccaPlayer(false);
        NpcInterazione.DialogoInCorso = false;
    }

    private void MostraNodo(DialogoNodo nodo) { MostraNodo(nodo, null); }

    private void MostraNodo(DialogoNodo nodo, string avviso)
    {
        if (nodo == null) { Chiudi(); return; }

        nodoCorrente = nodo;
        if (nomeTesto != null)
            nomeTesto.text = !string.IsNullOrEmpty(nodo.chiParla)
                ? nodo.chiParla
                : (interlocutore != null ? interlocutore.nome : "");
        if (corpoTesto != null)
            corpoTesto.text = string.IsNullOrEmpty(avviso)
                ? nodo.testo
                : nodo.testo + "\n\n<color=#ff8080>" + avviso + "</color>";

        PulisciBottoni();
        scelteMostrate.Clear();
        scelteMostrate.AddRange(nodo.SceltePossibili());

        if (scelteMostrate.Count == 0)
        {
            // Nodo finale: resta solo il modo di chiudere.
            CreaBottone("Chiudi", -1);
            return;
        }

        for (int i = 0; i < scelteMostrate.Count; i++)
        {
            string etichetta = sceltaCoiNumeri
                ? (i + 1) + ". " + scelteMostrate[i].testo
                : scelteMostrate[i].testo;
            CreaBottone(etichetta, i);
        }
    }

    private void CreaBottone(string etichetta, int indice)
    {
        if (bottoneStampo == null || contenitoreScelte == null) return;

        GameObject b = Instantiate(bottoneStampo, contenitoreScelte);
        b.SetActive(true);
        b.name = "Scelta" + (indice + 1);

        TMP_Text txt = b.GetComponentInChildren<TMP_Text>(true);
        if (txt != null) txt.text = etichetta;

        Button bottone = b.GetComponent<Button>();
        if (bottone != null)
        {
            int catturato = indice;   // senza copia, tutti i bottoni userebbero l'ultimo valore
            bottone.onClick.AddListener(() => Scegli(catturato));
        }
    }

    private void Scegli(int indice)
    {
        if (indice < 0 || scelteMostrate.Count == 0) { Chiudi(); return; }
        if (indice >= scelteMostrate.Count) return;

        SceltaDialogo s = scelteMostrate[indice];

        // Se l'effetto non e' andato a buon fine (monete insufficienti) il dialogo
        // NON avanza: si resta sul nodo, altrimenti si regalerebbe la merce.
        if (!s.Applica())
        {
            if (debugLog) Debug.Log("[DialogoUI] Effetto non riuscito: resto sul nodo.");
            MostraNodo(nodoCorrente, messaggioMoneteInsufficienti);
            return;
        }

        if (s.nodoSuccessivo != null) MostraNodo(s.nodoSuccessivo);
        else Chiudi();
    }

    private void PulisciBottoni()
    {
        if (contenitoreScelte == null) return;

        // Si guarda chi c'e' davvero nel contenitore invece di fidarsi di una lista.
        // Con la lista bastava perderne lo stato una volta - una ricompilazione mentre il
        // gioco gira la svuota, e i bottoni gia' in scena restano vivi - perche' da quel
        // momento non li ripulisse piu' nessuno: le risposte del dialogo precedente
        // restavano a schermo sotto quelle nuove (preso parlando col fabbro, 19/09/2026).
        for (int i = contenitoreScelte.childCount - 1; i >= 0; i--)
        {
            GameObject figlio = contenitoreScelte.GetChild(i).gameObject;
            if (bottoneStampo != null && figlio == bottoneStampo) continue;   // lo stampo resta

            // Destroy agisce solo a fine frame: senza staccarlo e spegnerlo subito, il
            // bottone vecchio resta figlio del contenitore mentre nascono i nuovi e il
            // VerticalLayoutGroup li dispone tutti insieme. Col ContentSizeFitter la
            // colonna delle risposte si allunga e finisce sopra la battuta.
            figlio.SetActive(false);
            figlio.transform.SetParent(null, false);
            Destroy(figlio);
        }
    }

    private void BloccaPlayer(bool blocca)
    {
        if (movimento == null || attacco == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p == null) return;
            movimento = p.GetComponent<MovementPlayer>();
            attacco = p.GetComponent<PlayerAttacco>();
            corpoPlayer = p.GetComponent<Rigidbody2D>();
        }

        // Stesso sistema di PlayerHealth.Die(): si spengono i componenti.
        if (movimento != null) movimento.enabled = !blocca;
        if (attacco != null)
        {
            attacco.enabled = !blocca;
            // Spegnerlo non basta: PlayerInput gli manda gli UnityEvent comunque, quindi il
            // click con cui si scegle la risposta faceva partire anche un colpo di spada.
            attacco.Bloccato = blocca;
        }

        // Senza questo resta la velocita' dell'ultimo frame e il player scivola via parlando.
        if (blocca && corpoPlayer != null) corpoPlayer.linearVelocity = Vector2.zero;
    }
}
