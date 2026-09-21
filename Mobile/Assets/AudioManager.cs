using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Volume generale del gioco (AudioListener.volume), salvato in PlayerPrefs.
/// Va sullo slider del volume, nel menu principale e in quello di gioco: si collega da solo
/// all'OnValueChanged, cosi' non dipende da un collegamento in Inspector che si puo' rompere
/// (e' successo: puntava a un AudioManager che non esisteva piu', e lo slider non faceva niente).
/// Il volume salvato si applica anche all'avvio, prima di aprire qualunque menu.
/// </summary>
public class AudioManager : MonoBehaviour
{
    const string Chiave = "MusicVolume";

    [Tooltip("Vuoto = lo Slider su questo stesso oggetto.")]
    public Slider volumeSlider;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void ApplicaAllAvvio()
    {
        AudioListener.volume = PlayerPrefs.GetFloat(Chiave, 1f);
    }

    void Awake()
    {
        if (volumeSlider == null) volumeSlider = GetComponent<Slider>();
        if (volumeSlider != null) volumeSlider.onValueChanged.AddListener(Imposta);
    }

    void OnEnable()
    {
        // Allinea lo slider al valore vero ogni volta che la pagina si riapre: l'altro menu
        // potrebbe averlo cambiato nel frattempo.
        if (volumeSlider != null) volumeSlider.SetValueWithoutNotify(AudioListener.volume);
    }

    void OnDestroy()
    {
        if (volumeSlider != null) volumeSlider.onValueChanged.RemoveListener(Imposta);
    }

    /// <summary>Tenuto per compatibilita' con i vecchi collegamenti in Inspector.</summary>
    public void ChangeVolume()
    {
        if (volumeSlider != null) Imposta(volumeSlider.value);
    }

    void Imposta(float valore)
    {
        AudioListener.volume = valore;
        PlayerPrefs.SetFloat(Chiave, valore);
    }
}
