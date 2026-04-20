using UnityEngine;
using UnityEngine.UI;
using TMPro; // Rimuovi se non usi TextMeshPro

public class QualityController : MonoBehaviour
{
    public TextMeshProUGUI qualityText; // Trascina qui il testo del titolo

    public void ChangeQuality(float value)
    {
        int index = Mathf.RoundToInt(value);
        QualitySettings.SetQualityLevel(index);

        // Aggiorna il testo sopra lo slider
        // Unity prende i nomi direttamente dalle tue impostazioni (Low, Medium, ecc.)
        qualityText.text = "GRAFICA: " + QualitySettings.names[index].ToUpper();
    }
}