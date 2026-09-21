using UnityEngine;
using Cinemachine;

/// <summary>
/// Musica di sottofondo che cambia con la zona. La zona si legge dal confiner della camera,
/// che ZoneTransition, PlayerRespawn e il caricamento del salvataggio tengono gia' aggiornato:
/// cosi' non serve un trigger in piu' per ogni zona.
///
/// Nel Dungeon suona la musica del boss finche' il demone e' vivo; ucciso il boss, li' e nella
/// zona Oggetto suona quella tranquilla del "dopo". Fra un brano e l'altro c'e' una dissolvenza
/// incrociata fra due AudioSource. Il volume generale resta quello dello slider (AudioListener).
/// </summary>
public class MusicaZone : MonoBehaviour
{
    [Header("Brani")]
    [Tooltip("Zona iniziale e casa della quest.")]
    public AudioClip villaggio;
    [Tooltip("Zona dei mob.")]
    public AudioClip avventura;
    [Tooltip("Dungeon, finche' il boss e' vivo.")]
    public AudioClip boss;
    [Tooltip("Dungeon e zona Oggetto, dopo aver ucciso il boss.")]
    public AudioClip dopoBoss;

    [Header("Nomi dei confini (figli di LimitiMappa)")]
    public string zonaIniziale = "ZonaIniziale";
    public string casaQuest = "CasaQuest";
    public string zonaMob = "ZonaSpawnMob";
    public string dungeon = "Dungeon";
    public string zonaOggetto = "Oggetto";

    [Header("Mix")]
    [Range(0f, 1f)] public float volume = 0.6f;
    [Tooltip("Secondi della dissolvenza incrociata.")]
    public float dissolvenza = 1.5f;

    public bool debugLog = false;

    private CinemachineConfiner confiner;
    private AudioSource[] sorgenti;
    private int attiva;
    private AudioClip corrente;

    void Awake()
    {
        sorgenti = new AudioSource[2];
        for (int i = 0; i < 2; i++)
        {
            var s = gameObject.AddComponent<AudioSource>();
            s.loop = true;
            s.playOnAwake = false;
            s.volume = 0f;
            s.spatialBlend = 0f;
            sorgenti[i] = s;
        }
    }

    void Start()
    {
        confiner = FindFirstObjectByType<CinemachineConfiner>();
    }

    void Update()
    {
        AudioClip voluto = BranoPerLaZona();
        if (voluto != corrente) Passa(voluto);

        // Dissolvenza in tempo reale: col game over il timeScale e' 0, ma la musica non si ferma.
        float passo = dissolvenza > 0f ? Time.unscaledDeltaTime / dissolvenza * volume : volume;
        for (int i = 0; i < 2; i++)
        {
            float obiettivo = i == attiva && sorgenti[i].clip != null ? volume : 0f;
            sorgenti[i].volume = Mathf.MoveTowards(sorgenti[i].volume, obiettivo, passo);
            if (i != attiva && sorgenti[i].isPlaying && sorgenti[i].volume <= 0f) sorgenti[i].Stop();
        }
    }

    AudioClip BranoPerLaZona()
    {
        if (confiner == null || confiner.m_BoundingShape2D == null) return corrente;
        string zona = confiner.m_BoundingShape2D.name;

        bool bossMorto = ProgressoGioco.Instance != null && ProgressoGioco.Instance.bossUcciso;
        if (zona == dungeon) return bossMorto ? dopoBoss : boss;
        if (zona == zonaOggetto) return bossMorto ? dopoBoss : villaggio;
        if (zona == zonaMob) return avventura;
        if (zona == zonaIniziale || zona == casaQuest) return villaggio;
        return corrente;
    }

    void Passa(AudioClip clip)
    {
        if (debugLog) Debug.Log($"[MusicaZone] {(corrente ? corrente.name : "-")} -> {(clip ? clip.name : "-")}");
        corrente = clip;
        attiva = 1 - attiva;
        var s = sorgenti[attiva];
        s.clip = clip;
        s.volume = 0f;
        if (clip != null) s.Play();
    }
}
