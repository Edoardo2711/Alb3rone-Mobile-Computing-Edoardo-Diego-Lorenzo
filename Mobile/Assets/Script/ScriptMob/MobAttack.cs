using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class MobAttack : MonoBehaviour
{
    [Header("Target")]
    public string targetTag = "Player";
    public Transform target;

    [Header("Combattimento")]
    public float attackRange = 2f;
    public float attackDamage = 10f;
    public float attackCooldown = 1.5f;

    [Header("Timing animazione")]
    public float attackHitDelay = 0.4f;
    public float attackRecovery = 0.4f;
    public float hitTolerance = 0.5f;

    [Header("Animator")]
    public string attackTrigger = "Attacco";
    public string lookXParam = "LookX";
    public string lookYParam = "LookY";

    [Header("Movimento")]
    public bool stopMobAIDuringAttack = true;

    [Header("Debug")]
    public bool debugLog = false;

    public bool IsAttacking { get; private set; }
    public bool IsAlive { get; private set; } = true;

    private Animator    animator;
    private Rigidbody2D rb;
    private MobAI       mobAI;
    private MobHealth   mobHealth;

    private float nextAttackTime = 0f;
    private Coroutine attackCo;

    void Awake()
    {
        animator  = GetComponent<Animator>();
        rb        = GetComponent<Rigidbody2D>();
        mobAI     = GetComponent<MobAI>();
        mobHealth = GetComponent<MobHealth>();
    }

    void Start()
    {
        ResolveTarget();
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
        if (Time.time < nextAttackTime) return;
        Transform t = GetTarget();
        if (t == null) return;
        if (Vector2.Distance(transform.position, t.position) > attackRange) return;
        nextAttackTime = Time.time + attackCooldown;
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

    IEnumerator DoAttack(Transform t)
    {
        IsAttacking = true;
        Vector2 dir = ((Vector2)t.position - (Vector2)transform.position).normalized;
        SetLookDirection(dir);

        rb.velocity = Vector2.zero;
        if (stopMobAIDuringAttack && mobAI != null) mobAI.enabled = false;

        if (animator != null && !string.IsNullOrEmpty(attackTrigger))
            animator.SetTrigger(attackTrigger);

        if (debugLog) Debug.Log($"[MobAttack] {gameObject.name} -> attacca {t.name}");

        yield return new WaitForSeconds(attackHitDelay);

        if (IsAlive && t != null)
        {
            float dist = Vector2.Distance(transform.position, t.position);
            if (dist <= attackRange + hitTolerance) ApplyDamage(t);
        }

        yield return new WaitForSeconds(attackRecovery);

        if (stopMobAIDuringAttack && mobAI != null && IsAlive) mobAI.enabled = true;
        IsAttacking = false;
        attackCo = null;
    }

    void ApplyDamage(Transform t)
    {
        PlayerHealth ph = t.GetComponent<PlayerHealth>()
                       ?? t.GetComponentInParent<PlayerHealth>()
                       ?? t.GetComponentInChildren<PlayerHealth>();
        if (ph != null)
        {
            ph.TakeDamage(attackDamage);
            if (debugLog) Debug.Log($"[MobAttack] {gameObject.name} infligge {attackDamage} a {t.name}");
        }
    }

    void SetLookDirection(Vector2 dir)
    {
        if (animator == null) return;
        if (Mathf.Abs(dir.x) < 0.001f && Mathf.Abs(dir.y) < 0.001f) return;
        if (!string.IsNullOrEmpty(lookXParam)) animator.SetFloat(lookXParam, dir.x);
        if (!string.IsNullOrEmpty(lookYParam)) animator.SetFloat(lookYParam, dir.y);
    }

    void HandleHit() { CancelAttack(); }

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
}