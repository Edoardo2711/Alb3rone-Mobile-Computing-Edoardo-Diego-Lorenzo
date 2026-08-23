using UnityEngine;
using UnityEngine.UI;

public class AudioManager : MonoBehaviour
{
    // Collega qui lo Slider trascinandolo dall'Hierarchy
    public Slider volumeSlider;

    void Start()
    {
        // Se avevi già salvato un volume, caricalo, altrimenti usa 1 (massimo)
        float savedVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
        volumeSlider.value = savedVolume;
        AudioListener.volume = savedVolume;
    }

    public void ChangeVolume()
    {
        // Applica il valore dello slider al volume globale del gioco
        AudioListener.volume = volumeSlider.value;

        // Opzionale: Salva la preferenza così al riavvio il volume resta lo stesso
        PlayerPrefs.SetFloat("MusicVolume", volumeSlider.value);
    }
}