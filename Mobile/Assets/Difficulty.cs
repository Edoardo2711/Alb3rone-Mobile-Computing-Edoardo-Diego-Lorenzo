using UnityEngine;

/// <summary>
/// Livello di difficolta' scelto dal giocatore, condiviso fra tutte le scene.
/// I mob leggono <see cref="Multiplier"/> nel loro Awake per scalare vita e danni.
/// </summary>
public static class Difficulty
{
    public const string PrefsKey = "Difficolta";

    /// <summary>Etichette mostrate nella UI, nello stesso ordine dei livelli.</summary>
    public static readonly string[] Names = { "EASY", "NORMAL", "HARD" };

    /// <summary>Moltiplicatore di vita e danni dei mob, uno per livello.</summary>
    static readonly float[] multipliers = { 1f, 1.25f, 1.5f };

    // Copia in memoria del livello. Serve perche' MainMenuManager.NewGame() chiama
    // PlayerPrefs.DeleteAll(): senza questa cache la difficolta' appena scelta
    // verrebbe cancellata proprio nel momento in cui si avvia la partita.
    static int? cached;

    public static int LevelCount => multipliers.Length;

    public static int Level
    {
        get
        {
            if (!cached.HasValue)
                cached = Mathf.Clamp(PlayerPrefs.GetInt(PrefsKey, 0), 0, LevelCount - 1);
            return cached.Value;
        }
        set
        {
            int v = Mathf.Clamp(value, 0, LevelCount - 1);
            cached = v;
            PlayerPrefs.SetInt(PrefsKey, v);
            PlayerPrefs.Save();
        }
    }

    /// <summary>1 = base, 1.25 = +25%, 1.5 = +50%.</summary>
    public static float Multiplier => multipliers[Level];

    public static string CurrentName => Names[Level];
}
