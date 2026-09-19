using UnityEngine;

/// <summary>
/// Fa cadere monete quando il mob muore. Va sul prefab del mob, accanto a MobHealth.
///
/// ⚠ Le monete vengono istanziate SENZA padre: MobAnimatorController distrugge il mob
/// alla fine dell'animazione di morte, e delle figlie sparirebbero con lui.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(MobHealth))]
public class DropMonete : MonoBehaviour
{
    [Header("Cosa cade")]
    public GameObject prefabMoneta;

    [Header("Quante")]
    [Min(0)] public int minMonete = 4;
    [Min(0)] public int maxMonete = 8;

    [Header("Dove")]
    [Tooltip("Di quanto si sparpagliano attorno al punto di morte, in unita'.")]
    public float raggioSparpagliamento = 1.2f;

    [Tooltip("Spostamento del punto di caduta rispetto al centro del mob.")]
    public Vector2 scostamento = Vector2.zero;

    public bool debugLog = false;

    private MobHealth vita;

    void Awake() => vita = GetComponent<MobHealth>();

    void OnEnable()
    {
        if (vita != null) vita.OnDeath += LasciaMonete;
    }

    void OnDisable()
    {
        if (vita != null) vita.OnDeath -= LasciaMonete;
    }

    void LasciaMonete()
    {
        if (prefabMoneta == null)
        {
            Debug.LogWarning($"[DropMonete] {name}: prefabMoneta non assegnato, niente drop.");
            return;
        }

        int quante = Random.Range(Mathf.Min(minMonete, maxMonete), Mathf.Max(minMonete, maxMonete) + 1);
        Vector3 centro = transform.position + (Vector3)scostamento;

        for (int i = 0; i < quante; i++)
        {
            Vector3 posizione = centro + (Vector3)(Random.insideUnitCircle * raggioSparpagliamento);
            GameObject moneta = Instantiate(prefabMoneta, posizione, Quaternion.identity);

            // Instantiate sistema il transform prima di Awake, quindi il punto del saltello
            // e' gia' giusto. La chiamata resta per chi spostasse la moneta dopo averla creata:
            // senza, il saltello tornerebbe a oscillare attorno al punto vecchio.
            if (moneta.TryGetComponent(out MonetaRaccolta m)) m.Posiziona(posizione);
        }

        if (debugLog) Debug.Log($"[DropMonete] {name} ha lasciato {quante} monete.");
    }
}
