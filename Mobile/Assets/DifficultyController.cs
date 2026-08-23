using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Collega lo slider della difficolta' all'impostazione globale <see cref="Difficulty"/>.
/// Sostituisce QualityController: in un 2D il livello di qualita' non cambiava nulla di
/// visibile, mentre la difficolta' scala vita e danni dei mob.
///
/// Il livello corrente non viene scritto in un'etichetta: lo si legge dalla posizione
/// del cursore sopra le tre tacche EASY / NORMAL / HARD, esattamente come fa la riga
/// del volume. Un testo in piu' non entrerebbe nello spazio disponibile.
/// </summary>
public class DifficultyController : MonoBehaviour
{
    [Tooltip("Lo slider della difficolta'. Viene configurato da solo su 0-2 interi.")]
    public Slider difficultySlider;

    void Start()
    {
        if (difficultySlider == null) return;

        difficultySlider.minValue     = 0;
        difficultySlider.maxValue     = Difficulty.LevelCount - 1;
        difficultySlider.wholeNumbers = true;
        // SetValueWithoutNotify: assegnare .value farebbe scattare OnValueChanged
        // e riscriverebbe la preferenza durante l'inizializzazione.
        difficultySlider.SetValueWithoutNotify(Difficulty.Level);
    }

    /// <summary>Da collegare all'OnValueChanged dello slider, sezione Dynamic float.</summary>
    public void ChangeDifficulty(float value)
    {
        Difficulty.Level = Mathf.RoundToInt(value);
    }
}
