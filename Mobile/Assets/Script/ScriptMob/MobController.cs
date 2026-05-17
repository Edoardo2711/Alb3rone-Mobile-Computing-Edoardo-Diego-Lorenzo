using UnityEngine;
using Pathfinding;

public class MobAI : MonoBehaviour
{
    [Header("Riferimenti")]
    public Transform player;

    [Header("Movimento")]
    public float moveSpeed = 3f;
    public float chaseRange = 5f;      // distanza entro cui insegue il Player
    public float waypointRange = 3f;   // raggio entro cui sceglie un nuovo waypoint casuale
    public float waypointReachedDist = 0.3f; // distanza per considerare il waypoint raggiunto

    [Header("Pathfinding")]
    public float repathRate = 0.5f;    // secondi tra un ricalcolo del path e l'altro

    [Header("Pattuglia")]
    [Tooltip("Layer degli ostacoli su cui validare i waypoint casuali.")]
    public string obstacleLayerName = "Ostacoli";
    [Tooltip("Tentativi di scegliere un waypoint libero da ostacoli.")]
    public int patrolPickAttempts = 10;
    public float patrolObstacleCheckRadius = 0.2f;

    private bool playerWarningLogged = false;
    private Seeker seeker;
    private Rigidbody2D rb;
    private Path currentPath;
    private int currentWaypoint = 0;
    private float repathTimer = 0f;
    private Vector2 patrolTarget;
    private bool isChasing = false;
    private LayerMask obstacleMask;

    void Start()
    {
        seeker = GetComponent<Seeker>();
        rb = GetComponent<Rigidbody2D>();

        if (seeker == null)
        {
            Debug.LogError("[MobAI] Manca il componente Seeker su " + name + ". Componente disabilitato.");
            enabled = false;
            return;
        }
        if (rb == null)
        {
            Debug.LogError("[MobAI] Manca il Rigidbody2D su " + name + ". Componente disabilitato.");
            enabled = false;
            return;
        }

        // Risolvi la layer mask degli ostacoli
        int idx = LayerMask.NameToLayer(obstacleLayerName);
        if (idx < 0) idx = LayerMask.NameToLayer("ostacoli");
        obstacleMask = (idx >= 0) ? (1 << idx) : 0;

        // Trova il Player automaticamente se non assegnato
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;

        PickNewPatrolTarget();
        RequestPath(patrolTarget);
    }

    void Update()
    {
        if (player == null)
        {
            if (!playerWarningLogged)
            {
                Debug.LogWarning("[MobAI] Player non trovato!");
                playerWarningLogged = true;
            }
            // tentativo di ri-trovare il player
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
            {
                player = p.transform;
                playerWarningLogged = false;
            }
            return;
        }

        float distToPlayer = Vector2.Distance(transform.position, player.position);
        isChasing = distToPlayer <= chaseRange;

        repathTimer += Time.deltaTime;
        if (repathTimer >= repathRate)
        {
            repathTimer = 0f;

            if (isChasing)
            {
                // Ricalcola path verso il Player
                RequestPath(player.position);
            }
            else
            {
                // Se ha raggiunto il waypoint di pattuglia, ne sceglie uno nuovo
                if (Vector2.Distance(transform.position, patrolTarget) <= waypointRange)
                    PickNewPatrolTarget();

                RequestPath(patrolTarget);
            }
        }
    }

    void FixedUpdate()
    {
        if (currentPath == null) return;
        if (currentWaypoint >= currentPath.vectorPath.Count) return;

        Vector2 direction = ((Vector2)currentPath.vectorPath[currentWaypoint] - rb.position).normalized;
        rb.MovePosition(rb.position + direction * moveSpeed * Time.fixedDeltaTime);

        // Avanza al prossimo waypoint se sufficientemente vicino
        if (Vector2.Distance(rb.position, currentPath.vectorPath[currentWaypoint]) <= waypointReachedDist)
            currentWaypoint++;
    }

    void RequestPath(Vector2 target)
    {
        if (seeker != null && seeker.IsDone())
            seeker.StartPath(transform.position, target, OnPathComplete);
    }

    void OnPathComplete(Path p)
    {
        if (p.error)
        {
            Debug.LogWarning($"[MobAI] Errore nel path: {p.errorLog}");
            return;
        }
        currentPath = p;
        currentWaypoint = 0;
    }

    // Sceglie un punto casuale nelle vicinanze come destinazione pattuglia,
    // evitando di piazzarlo dentro ostacoli (se la layer e' definita).
    void PickNewPatrolTarget()
    {
        for (int i = 0; i < Mathf.Max(1, patrolPickAttempts); i++)
        {
            Vector2 candidate = (Vector2)transform.position + Random.insideUnitCircle * waypointRange;
            if (obstacleMask.value == 0 ||
                Physics2D.OverlapCircle(candidate, patrolObstacleCheckRadius, obstacleMask) == null)
            {
                patrolTarget = candidate;
                return;
            }
        }
        // Fallback: resta sul posto se non trova nulla di libero
        patrolTarget = transform.position;
    }

    // Visualizza il range di inseguimento nell'editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, chaseRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, waypointRange);
    }
}
