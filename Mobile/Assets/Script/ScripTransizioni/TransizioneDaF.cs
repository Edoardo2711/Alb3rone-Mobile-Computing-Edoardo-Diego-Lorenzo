using UnityEngine;
using UnityEngine.Serialization;
using Cinemachine;

public class TransizioneDaF : MonoBehaviour
{
    [FormerlySerializedAs("mapBoundry")]
    [SerializeField] private PolygonCollider2D mapBoundary;

    private CinemachineConfiner confiner;
    private static bool isTeleporting = false;

    private void Awake()
    {
        confiner = FindFirstObjectByType<CinemachineConfiner>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.gameObject.CompareTag("Player")) return;
        if (isTeleporting) return;
        isTeleporting = true;

        if (confiner != null && mapBoundary != null)
        {
            confiner.m_BoundingShape2D = mapBoundary;
            confiner.InvalidatePathCache();
        }
        else
        {
            Debug.LogWarning($"[{nameof(TransizioneDaF)}] Confiner o mapBoundary non impostato!");
        }

        UpdatePlayerPosition(collision.gameObject);
        StartCoroutine(ResetTeleportFlag());
    }

    private void UpdatePlayerPosition(GameObject player)
    {
        Vector3 newPos = player.transform.position;
        newPos.x += 135;
        newPos.y += 1;

        // Aggiorna PRIMA il transform (immediato), POI sincronizza il rigidbody.
        player.transform.position = newPos;
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.position = newPos;
            rb.linearVelocity = Vector2.zero;
        }
    }

    private System.Collections.IEnumerator ResetTeleportFlag()
    {
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        isTeleporting = false;
    }
}
