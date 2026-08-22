using System.Collections;
using UnityEngine;

/// <summary>
/// Attacco a distanza: il mob continua a inseguire il bersaglio tramite MobAI e,
/// quando lo ha entro il raggio di tiro, spara un proiettile (vedi Fireball).
/// Gemello di MobAttack, che invece infligge danno a contatto.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class MobRangedAttack : MonoBehaviour, IMobAttack
{
    [Header("Target")]
    public string targetTag = "Player";
    public Transform target;

    [Header("Combattimento")]
    [Tooltip("Raggio di tiro. Tienilo >= chaseRange di MobAI, altrimenti il mob spara senza inseguire.")]
    public float attackRange = 7f;
    [Min(0.05f)] public float attackCooldown = 2f;

    [Header("Proiettile")]
    public GameObject projectilePrefab;
    public float projectileSpeed  = 7f;
    public float projectileDamage = 8f;
    [Tooltip("Distanza dal centro del mob a cui nasce il proiettile, nella direzione di tiro.")]
    public float spawnOffset = 1.2f;

    [Header("Timing animazione")]
    [Tooltip("Ritardo tra l'inizio dell'animazione e la nascita del proiettile.")]
    public float attackWindup = 0.2f;
    [Tooltip("Coda dell'animazione dopo il tiro, prima di poter riattaccare.")]
    public float attackRecovery = 0.25f;

    [Header("Linea di tiro")]
    [Tooltip("Se true, non spara quando c'e' un ostacolo tra il mob e il bersaglio.")]
    public bool requireLineOfSight = true;
    public string obstacleLayerName = "Ostacoli";

    [Header("Animator")]
    public string attackTrigger = "Attacco";
    public string lookXParam = "LookX";
    public string lookYParam = "LookY";

    [Header("Movimento")]
    [Tooltip("Se true, il mob si ferma mentre spara. Lasciato OFF: insegue e spara.")]
    public bool stopMobAIDuringAttack = false;

    [Header("Debug")]
    public bool debugLog = false;

    public bool IsAttacking { get; private set; }
    public bool IsAlive { get; private set; } = true;

    private Animator  animator;
    private MobAI     mobAI;
    private MobHealth mobHealth;
    private LayerMask obstacleMask;

    private float nextAttackTime = 0f;
    private Coroutine attackCo;

    void Awake()
    {
        animator  = GetComponent<Animator>();
        mobAI     = GetComponent<MobAI>();
        mobHealth = GetComponent<MobHealth>();
    }

    void Start()
    {
        int idx = LayerMask.NameToLayer(obstacleLayerName);
        if (idx < 0) idx = LayerMask.NameToLayer("ostacoli");
        obstacleMask = (idx >= 0) ? (1 << idx) : 0;

        ResolveTarget();

        if (projectilePrefab == null)
            Debug.LogWarning("[MobRangedAttack] projectilePrefab non assegnato su " + name + ": il mob non sparera'.");

        if (mobHealth != null)
        {
            mobHealth.OnHit   += HandleHit;
            mobHealth.OnDeath += HandleDeath;
        }
    }

    void OnDestroy()
    {
        if (mobHealth != null)
        {
            mobHealth.OnHit   -= HandleHit;
            mobHealth.OnDeath -= HandleDeath;
        }
    }

    void Update()
    {
        if (!IsAlive || IsAttacking) return;
        if (projectilePrefab == null) return;
        if (Time.time < nextAttackTime) return;

        Transform t = GetTarget();
        if (t == null) return;
        if (Vector2.Distance(transform.position, t.position) > attackRange) return;
        if (!HasLineOfSight(t)) return;

        nextAttackTime = Time.time + Mathf.Max(attackCooldown, 0.05f);
        attackCo = StartCoroutine(DoAttack(t));
    }

    Transform GetTarget()
    {
        if (target != null) return target;
        if (mobAI != null && mobAI.player != null) return mobAI.player;
        ResolveTarget();
        return target;
    }

    void ResolveTarget()
    {
        if (target != null) return;
        if (mobAI != null && mobAI.player != null) { target = mobAI.player; return; }
        if (string.IsNullOrEmpty(targetTag)) return;
        try
        {
            GameObject go = GameObject.FindGameObjectWithTag(targetTag);
            if (go != null) target = go.transform;
        }
        catch (UnityException) { }
    }

    bool HasLineOfSight(Transform t)
    {
        if (!requireLineOfSight || obstacleMask.value == 0) return true;
        Vector2 from = transform.position;
        Vector2 to   = t.position;
        return Physics2D.Linecast(from, to, obstacleMask).collider == null;
    }

    IEnumerator DoAttack(Transform t)
    {
        IsAttacking = true;

        Vector2 dir = ((Vector2)t.position - (Vector2)transform.position).normalized;
        SetLookDirection(dir);

        if (stopMobAIDuringAttack && mobAI != null) mobAI.enabled = false;

        if (animator != null && !string.IsNullOrEmpty(attackTrigger))
            animator.SetTrigger(attackTrigger);

        if (attackWindup > 0f) yield return new WaitForSeconds(attackWindup);

        // Rimira al momento del tiro: il bersaglio si e' mosso durante il windup
        if (IsAlive && t != null)
        {
            dir = ((Vector2)t.position - (Vector2)transform.position).normalized;
            SetLookDirection(dir);
            Fire(dir);
        }

        if (attackRecovery > 0f) yield return new WaitForSeconds(attackRecovery);

        if (stopMobAIDuringAttack && mobAI != null && IsAlive) mobAI.enabled = true;
        IsAttacking = false;
        attackCo = null;
    }

    void Fire(Vector2 dir)
    {
        Vector3 spawnPos = transform.position + (Vector3)(dir * spawnOffset);
        GameObject go = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);

        Fireball fb = go.GetComponent<Fireball>();
        if (fb != null) fb.Launch(dir, projectileSpeed, projectileDamage);
        else Debug.LogWarning("[MobRangedAttack] Il prefab '" + projectilePrefab.name + "' non ha il componente Fireball.");

        if (debugLog) Debug.Log("[MobRangedAttack] " + name + " spara verso " + dir);
    }

    void SetLookDirection(Vector2 dir)
    {
        if (animator == null) return;
        if (Mathf.Abs(dir.x) < 0.001f && Mathf.Abs(dir.y) < 0.001f) return;
        if (!string.IsNullOrEmpty(lookXParam)) animator.SetFloat(lookXParam, dir.x);
        if (!string.IsNullOrEmpty(lookYParam)) animator.SetFloat(lookYParam, dir.y);
    }

    // Come per MobAttack: il mob "committa" al colpo, subire danno non annulla il tiro.
    void HandleHit()
    {
        // no-op
    }

    void HandleDeath()
    {
        IsAlive = false;
        CancelAttack();
        enabled = false;
    }

    public void CancelAttack()
    {
        if (attackCo != null) { StopCoroutine(attackCo); attackCo = null; }
        if (stopMobAIDuringAttack && mobAI != null && IsAlive) mobAI.enabled = true;
        IsAttacking = false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
