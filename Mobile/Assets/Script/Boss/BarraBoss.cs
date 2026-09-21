using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// La barra della vita del boss, in alto al centro. La comanda BossDemone: si vede solo
/// durante lo scontro. Il riempimento scende morbido come quello del player.
/// </summary>
public class BarraBoss : MonoBehaviour
{
    [Header("Riferimenti")]
    [Tooltip("L'oggetto da accendere e spegnere. Vuoto = questo stesso.")]
    public GameObject pannello;
    public Image riempimento;
    public TMP_Text nome;

    [Tooltip("Secondi per raggiungere il valore nuovo. 0 = di colpo.")]
    public float morbidezza = 0.25f;

    private float obiettivo = 1f;
    private float velocita;

    void Awake()
    {
        if (pannello == null) pannello = gameObject;
    }

    public void Mostra(string titolo, float percentuale)
    {
        if (nome != null && !string.IsNullOrEmpty(titolo)) nome.text = titolo;
        obiettivo = Mathf.Clamp01(percentuale);
        if (riempimento != null) riempimento.fillAmount = obiettivo;
        pannello.SetActive(true);
    }

    public void Imposta(float percentuale)
    {
        obiettivo = Mathf.Clamp01(percentuale);
    }

    public void Nascondi()
    {
        pannello.SetActive(false);
    }

    void Update()
    {
        if (riempimento == null) return;
        riempimento.fillAmount = morbidezza <= 0f
            ? obiettivo
            : Mathf.SmoothDamp(riempimento.fillAmount, obiettivo, ref velocita, morbidezza);
    }
}
