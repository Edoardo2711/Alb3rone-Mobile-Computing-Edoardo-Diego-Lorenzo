using UnityEngine;
using Cinemachine;

/// <summary>
/// Riporta il player al punto di partenza dopo la morte.
///
/// Va messo sullo stesso GameObject di PlayerHealth. Non si aggancia da solo a
/// OnDeath: e' GameOverUI a chiamare Respawn() quando il giocatore preme "Riprova",
/// cosi' la schermata di game over resta visibile finche' lui non decide.
///
/// Perche' rimette a posto anche la camera: attraversando i waypoint, ZoneTransition
/// cambia il bounding shape del CinemachineConfiner e la ortho size della vcam. Se il
/// player muore nel Dungeon (size 17) e rinasce nella zona iniziale, senza ripristino
/// resterebbe inquadrato con lo zoom del Dungeon e confinato nei limiti sbagliati.
/// </summary>
public class PlayerRespawn : MonoBehaviour
{
    [Header("Punto di respawn")]
    [Tooltip("Dove riportare il player. Se vuoto, si usa la posizione che il player ha all'avvio della scena.")]
    [SerializeField] private Transform respawnPoint;

    [Header("Camera")]
    [Tooltip("Ripristina confiner e ortho size che la camera aveva all'avvio della scena.")]
    [SerializeField] private bool resetCamera = true;
    [Tooltip("Lasciare vuoto per cercarla automaticamente in scena.")]
    [SerializeField] private CinemachineVirtualCamera vcam;

    // Stato iniziale catturato in Awake: e' il riferimento a cui tornare.
    private Vector3 startPosition;
    private CinemachineConfiner confiner;
    private Collider2D startBoundary;
    private float startOrthoSize;

    private PlayerHealth health;
    private Rigidbody2D rb;

    private void Awake()
    {
        health = GetComponent<PlayerHealth>();
        rb     = GetComponent<Rigidbody2D>();

        startPosition = transform.position;

        confiner = FindFirstObjectByType<CinemachineConfiner>();
        if (vcam == null) vcam = FindFirstObjectByType<CinemachineVirtualCamera>();

        if (confiner != null) startBoundary = confiner.m_BoundingShape2D;
        if (vcam != null)     startOrthoSize = vcam.m_Lens.OrthographicSize;
    }

    /// <summary>
    /// Riporta il player al punto di respawn con la vita piena e la camera come all'avvio.
    /// </summary>
    public void Respawn()
    {
        Vector3 oldPos = transform.position;
        Vector3 newPos = (respawnPoint != null) ? respawnPoint.position : startPosition;
        newPos.z = oldPos.z;

        // Stesso ordine usato da ZoneTransition: prima il transform (immediato),
        // poi il rigidbody, perche' m_AutoSyncTransforms e' disattivato e scrivere
        // solo sul transform verrebbe annullato al FixedUpdate successivo.
        transform.position = newPos;
        if (rb != null)
        {
            rb.position = newPos;
            rb.linearVelocity = Vector2.zero;
        }

        if (resetCamera) ResetCameraState(newPos - oldPos);

        if (health != null) health.Revive();
    }

    private void ResetCameraState(Vector3 positionDelta)
    {
        if (confiner != null && startBoundary != null)
        {
            confiner.m_BoundingShape2D = startBoundary;
            confiner.InvalidatePathCache();
        }

        if (vcam != null)
        {
            vcam.m_Lens.OrthographicSize = startOrthoSize;
            // Senza questo la camera interpolerebbe dalla posizione della morte a
            // quella di respawn, attraversando tutta la mappa in una panoramica.
            vcam.OnTargetObjectWarped(transform, positionDelta);
        }
    }
}
