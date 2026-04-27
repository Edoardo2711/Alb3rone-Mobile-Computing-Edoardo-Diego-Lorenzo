using UnityEngine;
using Cinemachine;

public class TransizioneZona : MonoBehaviour
{
    [Header("Destinazione")]
    public Transform destination;

    [Header("Camera")]
    [SerializeField] PolygonCollider2D mapBoundary;
    public float cameraSize = 5f;

    private CinemachineConfiner confiner;
    private CinemachineVirtualCamera virtualCamera;

    void Awake()
    {
        confiner = FindObjectOfType<CinemachineConfiner>();
        virtualCamera = FindObjectOfType<CinemachineVirtualCamera>();
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        collision.gameObject.transform.position = destination.position;

        if (mapBoundary != null)
            confiner.m_BoundingShape2D = mapBoundary;

        // Modifica l'orthographic size della Cinemachine Virtual Camera
        if (virtualCamera != null)
            virtualCamera.m_Lens.OrthographicSize = cameraSize;
    }
}