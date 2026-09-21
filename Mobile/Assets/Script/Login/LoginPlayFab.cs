using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;

/// <summary>
/// Login facoltativo con PlayFab, dal menu principale: accesso con email e password o
/// registrazione di un account nuovo. Si puo' giocare anche senza.
///
/// Parla direttamente con l'API REST di PlayFab (Client/LoginWithEmailAddress e
/// Client/RegisterPlayFabUser) con UnityWebRequest: niente SDK da importare. Serve solo il
/// Title ID del titolo, che e' pubblico. La Secret Key NON va mai messa nel gioco.
///
/// La sessione resta in <see cref="SessionePlayFab"/> per tutta la partita. Si ricorda solo
/// l'email per la volta dopo, mai la password.
/// </summary>
public class LoginPlayFab : MonoBehaviour
{
    const string ChiaveEmail = "PlayFabEmail";

    [Header("PlayFab")]
    [Tooltip("Title ID del titolo su developer.playfab.com (Title settings > API Features). Pubblico, non e' la Secret Key.")]
    public string titleId = "";

    [Header("Riferimenti UI")]
    public GameObject pannello;
    public TMP_InputField campoEmail;
    public TMP_InputField campoPassword;
    [Tooltip("Serve solo per registrarsi: e' il nome mostrato nel gioco (3-25 caratteri).")]
    public TMP_InputField campoNome;
    public TMP_Text testoStato;
    [Tooltip("La scritta del pulsante che apre il pannello: diventa il nome del giocatore da loggato.")]
    public TMP_Text etichettaPulsante;
    [Tooltip("I pulsanti da disattivare mentre una richiesta e' in corso.")]
    public UnityEngine.UI.Selectable[] pulsanti;

    [Header("Testi")]
    public string testoPulsanteOspite = "LOGIN";
    public Color coloreErrore = new Color(0.75f, 0.15f, 0.1f, 1f);
    public Color coloreOk = new Color(0.2f, 0.45f, 0.15f, 1f);

    public bool debugLog = false;

    private bool inCorso;

    void Start()
    {
        if (pannello != null) pannello.SetActive(false);
        if (campoEmail != null) campoEmail.text = PlayerPrefs.GetString(ChiaveEmail, "");
        AggiornaPulsante();
    }

    // ---------------------------------------------------------------- pulsanti

    public void Apri()
    {
        if (pannello == null) return;
        pannello.SetActive(true);
        if (SessionePlayFab.Connesso) Stato("Signed in as " + SessionePlayFab.NomeGiocatore + ".", coloreOk);
        else Stato("", coloreOk);
    }

    public void Chiudi()
    {
        if (pannello != null) pannello.SetActive(false);
    }

    public void Accedi()
    {
        if (inCorso || !Configurato()) return;
        string email = Pulito(campoEmail), password = campoPassword != null ? campoPassword.text : "";
        if (email.Length == 0 || password.Length == 0) { Stato("Enter email and password.", coloreErrore); return; }

        var richiesta = new RichiestaLogin
        {
            TitleId = titleId, Email = email, Password = password,
            InfoRequestParameters = new ParametriInfo { GetPlayerProfile = true, GetUserAccountInfo = true }
        };
        StartCoroutine(Invia("LoginWithEmailAddress", JsonUtility.ToJson(richiesta), testo =>
        {
            var r = JsonUtility.FromJson<RispostaLogin>(testo);
            string nome = NomeDa(r.data, email);
            Entrato(r.data.PlayFabId, r.data.SessionTicket, nome, email);
        }));
    }

    public void Registrati()
    {
        if (inCorso || !Configurato()) return;
        string email = Pulito(campoEmail), password = campoPassword != null ? campoPassword.text : "", nome = Pulito(campoNome);
        if (email.Length == 0 || password.Length == 0) { Stato("Enter email and password.", coloreErrore); return; }
        if (password.Length < 6) { Stato("The password needs at least 6 characters.", coloreErrore); return; }
        if (nome.Length < 3 || nome.Length > 25) { Stato("Choose a name between 3 and 25 characters.", coloreErrore); return; }

        var richiesta = new RichiestaRegistrazione
        {
            TitleId = titleId, Email = email, Password = password, DisplayName = nome,
            RequireBothUsernameAndEmail = false
        };
        StartCoroutine(Invia("RegisterPlayFabUser", JsonUtility.ToJson(richiesta), testo =>
        {
            var r = JsonUtility.FromJson<RispostaRegistrazione>(testo);
            Entrato(r.data.PlayFabId, r.data.SessionTicket, nome, email);
        }));
    }

    /// <summary>
    /// Password dimenticata: PlayFab manda all'email del campo un link per sceglierne una nuova.
    /// Funziona senza server di posta nostro (verificato: risponde 200 anche senza SMTP).
    /// </summary>
    public void RecuperaPassword()
    {
        if (inCorso || !Configurato()) return;
        string email = Pulito(campoEmail);
        if (email.Length == 0) { Stato("Write your email first, then press FORGOT PASSWORD.", coloreErrore); return; }

        var richiesta = new RichiestaRecupero { TitleId = titleId, Email = email };
        StartCoroutine(Invia("SendAccountRecoveryEmail", JsonUtility.ToJson(richiesta), testo =>
        {
            Stato("Check your inbox: we sent you a link to choose a new password.", coloreOk);
        }));
    }

    public void Esci()
    {
        SessionePlayFab.Esci();
        if (campoPassword != null) campoPassword.text = "";
        Stato("Signed out.", coloreOk);
        AggiornaPulsante();
    }

    // ---------------------------------------------------------------- rete

    IEnumerator Invia(string api, string json, Action<string> seOk)
    {
        inCorso = true;
        Attiva(false);
        Stato("Connecting...", coloreOk);

        string url = "https://" + titleId + ".playfabapi.com/Client/" + api;
        using (var req = new UnityWebRequest(url, "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.timeout = 15;
            yield return req.SendWebRequest();

            string testo = req.downloadHandler != null ? req.downloadHandler.text : "";
            if (debugLog) Debug.Log($"[LoginPlayFab] {api} -> {req.responseCode} {testo}");

            if (req.result == UnityWebRequest.Result.Success)
            {
                try { seOk(testo); }
                catch (Exception e) { Debug.LogWarning("[LoginPlayFab] Risposta inattesa: " + e.Message); Stato("Unexpected answer from PlayFab.", coloreErrore); }
            }
            else if (!string.IsNullOrEmpty(testo) && testo.TrimStart().StartsWith("{"))
            {
                var err = JsonUtility.FromJson<RispostaErrore>(testo);
                Stato(Spiega(err), coloreErrore);
            }
            else
            {
                Stato("No connection to PlayFab. Check your internet.", coloreErrore);
            }
        }

        inCorso = false;
        Attiva(true);
    }

    void Entrato(string playFabId, string ticket, string nome, string email)
    {
        SessionePlayFab.Entra(playFabId, ticket, nome);
        PlayerPrefs.SetString(ChiaveEmail, email);
        PlayerPrefs.Save();
        if (campoPassword != null) campoPassword.text = "";
        Stato("Welcome, " + nome + "!", coloreOk);
        AggiornaPulsante();
    }

    /// <summary>Il nome da mostrare: quello del titolo, poi l'username, poi la parte prima della @.</summary>
    static string NomeDa(DatiLogin d, string email)
    {
        if (d != null && d.InfoResultPayload != null)
        {
            var p = d.InfoResultPayload;
            if (p.PlayerProfile != null && !string.IsNullOrEmpty(p.PlayerProfile.DisplayName)) return p.PlayerProfile.DisplayName;
            if (p.AccountInfo != null)
            {
                if (p.AccountInfo.TitleInfo != null && !string.IsNullOrEmpty(p.AccountInfo.TitleInfo.DisplayName)) return p.AccountInfo.TitleInfo.DisplayName;
                if (!string.IsNullOrEmpty(p.AccountInfo.Username)) return p.AccountInfo.Username;
            }
        }
        int at = email.IndexOf('@');
        return at > 0 ? email.Substring(0, at) : email;
    }

    static string Spiega(RispostaErrore e)
    {
        if (e == null) return "Something went wrong.";
        switch (e.error)
        {
            case "InvalidEmailOrPassword":
            case "AccountNotFound":
            case "InvalidUsernameOrPassword": return "Wrong email or password.";
            case "EmailAddressNotAvailable": return "This email is already registered: sign in instead.";
            case "InvalidEmailAddress": return "That email address is not valid.";
            case "NoContactEmailAddressFound": return "No account is registered with that email.";
            case "InvalidPassword": return "The password needs 6 to 100 characters.";
            case "NameNotAvailable": return "That name is already taken.";
            case "ProfaneDisplayName": return "Please choose another name.";
            case "InvalidTitleId":
            case "TitleNotActivated": return "PlayFab is not configured correctly (Title ID).";
            default: return string.IsNullOrEmpty(e.errorMessage) ? "Something went wrong." : e.errorMessage;
        }
    }

    // ---------------------------------------------------------------- utilita'

    bool Configurato()
    {
        if (!string.IsNullOrWhiteSpace(titleId)) return true;
        Stato("Login is not configured yet (missing PlayFab Title ID).", coloreErrore);
        return false;
    }

    static string Pulito(TMP_InputField f) => f != null ? f.text.Trim() : "";

    void Stato(string testo, Color colore)
    {
        if (testoStato == null) return;
        testoStato.text = testo;
        testoStato.color = colore;
    }

    void Attiva(bool attivo)
    {
        if (pulsanti == null) return;
        foreach (var p in pulsanti) if (p != null) p.interactable = attivo;
    }

    void AggiornaPulsante()
    {
        if (etichettaPulsante != null)
            etichettaPulsante.text = SessionePlayFab.Connesso ? SessionePlayFab.NomeGiocatore.ToUpper() : testoPulsanteOspite;
    }

    // ---------------------------------------------------------------- JSON di PlayFab (campi con i nomi dell'API)

    [Serializable] class ParametriInfo { public bool GetPlayerProfile; public bool GetUserAccountInfo; }
    [Serializable] class RichiestaLogin { public string TitleId; public string Email; public string Password; public ParametriInfo InfoRequestParameters; }
    [Serializable] class RichiestaRecupero { public string TitleId; public string Email; }
    [Serializable] class RichiestaRegistrazione { public string TitleId; public string Email; public string Password; public string DisplayName; public bool RequireBothUsernameAndEmail; }

    [Serializable] class Profilo { public string DisplayName; }
    [Serializable] class InfoTitolo { public string DisplayName; }
    [Serializable] class InfoAccount { public string Username; public InfoTitolo TitleInfo; }
    [Serializable] class InfoRisultato { public Profilo PlayerProfile; public InfoAccount AccountInfo; }
    [Serializable] class DatiLogin { public string PlayFabId; public string SessionTicket; public InfoRisultato InfoResultPayload; }
    [Serializable] class RispostaLogin { public int code; public DatiLogin data; }
    [Serializable] class DatiRegistrazione { public string PlayFabId; public string SessionTicket; public string Username; }
    [Serializable] class RispostaRegistrazione { public int code; public DatiRegistrazione data; }
    [Serializable] class RispostaErrore { public int code; public string error; public int errorCode; public string errorMessage; }
}

/// <summary>La sessione PlayFab corrente: statica, sopravvive al cambio di scena.</summary>
public static class SessionePlayFab
{
    public static bool Connesso => !string.IsNullOrEmpty(SessionTicket);
    public static string PlayFabId { get; private set; }
    public static string SessionTicket { get; private set; }
    public static string NomeGiocatore { get; private set; }

    /// <summary>Scatta a ogni accesso o uscita.</summary>
    public static event Action OnCambiata;

    public static void Entra(string playFabId, string ticket, string nome)
    {
        PlayFabId = playFabId;
        SessionTicket = ticket;
        NomeGiocatore = nome;
        OnCambiata?.Invoke();
    }

    public static void Esci()
    {
        PlayFabId = null;
        SessionTicket = null;
        NomeGiocatore = null;
        OnCambiata?.Invoke();
    }
}
