using UnityEngine;
using TMPro;

/// <summary>
/// Pulsante dei settings che alterna schermo intero e finestra. La scelta resta in
/// PlayerPrefs e viene riapplicata a ogni avvio, prima della prima scena.
/// Nell'editor non cambia niente di visibile: Screen.SetResolution agisce solo nella build.
/// </summary>
public class ModalitaSchermo : MonoBehaviour
{
    const string Chiave = "SchermoIntero";

    [Tooltip("Il testo del pulsante, aggiornato a ogni cambio.")]
    public TMP_Text etichetta;
    public string testoIntero = "SCREEN: FULLSCREEN";
    public string testoFinestra = "SCREEN: WINDOWED";

    [Tooltip("Dimensione della finestra quando si esce dallo schermo intero.")]
    public Vector2Int risoluzioneFinestra = new Vector2Int(1280, 720);

    /// <summary>La scelta salvata; senza, quella con cui il gioco e' partito.</summary>
    public static bool SchermoIntero =>
        PlayerPrefs.HasKey(Chiave) ? PlayerPrefs.GetInt(Chiave) == 1 : Screen.fullScreenMode != FullScreenMode.Windowed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void ApplicaAllAvvio()
    {
        if (PlayerPrefs.HasKey(Chiave)) Applica(SchermoIntero, new Vector2Int(1280, 720));
    }

    void OnEnable()
    {
        Aggiorna();
    }

    /// <summary>Da collegare all'onClick del pulsante.</summary>
    public void Alterna()
    {
        bool intero = !SchermoIntero;
        PlayerPrefs.SetInt(Chiave, intero ? 1 : 0);
        PlayerPrefs.Save();
        Applica(intero, risoluzioneFinestra);
        Aggiorna();
    }

    static void Applica(bool intero, Vector2Int finestra)
    {
        if (intero)
            Screen.SetResolution(Display.main.systemWidth, Display.main.systemHeight, FullScreenMode.FullScreenWindow);
        else
            Screen.SetResolution(finestra.x, finestra.y, FullScreenMode.Windowed);
    }

    // L'etichetta legge la scelta salvata e non Screen.fullScreenMode, che si aggiorna solo al frame dopo.
    void Aggiorna()
    {
        if (etichetta != null) etichetta.text = SchermoIntero ? testoIntero : testoFinestra;
    }
}
