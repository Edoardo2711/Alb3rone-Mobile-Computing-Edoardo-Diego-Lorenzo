using UnityEngine;
using Cinemachine;

/// <summary>
/// Script unificato per tutte le transizioni di zona.
/// Sostituisce: TransizioneCaF, TransizioneCaQ, TransizioneDaF,
///              TransizioneDaO, TransizioneFaC, TransizioneFaD,
///              TransizioneOaD, TransizioneQaC
///
/// Come usarlo:
/// 1. Aggiungi questo script al GameObject trigger di ogni waypoint.
/// 2. Compila i campi nel Inspector:
///    - Map Boundary   → il PolygonCollider2D della zona di destinazione
///    - Offset         → lo spostamento XY da applicare al player
///    - Change Camera Size → spunta solo se la transizione deve cambiare la ortho size
///    - New Camera Size    → valore della nuova ortho size (usato solo se Change Camera Size = true)
///    - Vcam           → trascina la CinemachineVirtualCamera (usato solo se Change Camera Size = true)
/// </summary>
public class ZoneTransition : MonoBehaviour
{
    [Header("Zona di destinazione")]
    [SerializeField] private PolygonCollider2D mapBoundary;

    [Header("Spostamento del player")]
    [SerializeField] private Vector2 offset = Vector2.zero;

    [Header("Camera (opzionale)")]
    [SerializeField] private bool changeCameraSize = false;
    [SerializeField] private float newCameraSize = 5f;
    [SerializeField] private CinemachineVirtualCamera vcam;

    // Flag statico condiviso tra tutte le istanze per evitare il double-trigger:
    // quando un waypoint teletrasporta il player, gli altri trigger ignorano
    // l'evento OnTriggerEnter2D per un frame.
    private static bool isTeleporting = false;

    private CinemachineConfiner confiner;

    private void Awake()
    {
        confiner = FindFirstObjectByType<CinemachineConfiner>();

        if (changeCameraSize && vcam == null)
            vcam = FindFirstObjectByType<CinemachineVirtualCamera>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        // Protezione double-trigger: se stiamo già teletrasportando, esci subito
        if (isTeleporting) return;

        isTeleporting = true;

        // 1. Aggiorna il confiner della camera
        if (confiner != null && mapBoundary != null)
        {
            confiner.m_BoundingShape2D = mapBoundary;
            confiner.InvalidatePathCache();
        }

        // 2. Cambia ortho size se richiesto
        if (changeCameraSize && vcam != null)
            vcam.m_Lens.OrthographicSize = newCameraSize;

        // 3. Teletrasporta il player: PRIMA transform (immediato), POI rigidbody (sync fisica)
        Vector3 newPos = collision.transform.position;
        newPos.x += offset.x;
        newPos.y += offset.y;

        collision.transform.position = newPos;
        Rigidbody2D rb = collision.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.position = newPos;
            rb.linearVelocity = Vector2.zero;
        }

        // 5. Resetta il flag al frame successivo
        // (garantisce che i trigger della zona di destinazione non sparino subito)
        StartCoroutine(ResetTeleportFlag());
    }

    private System.Collections.IEnumerator ResetTeleportFlag()
    {
        // Aspetta la fine del frame corrente + 1 frame fisico
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        isTeleporting = false;
    }
}
