using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(MobHealth))]
[RequireComponent(typeof(Rigidbody2D))]
public class MobAnimatorController : MonoBehaviour
{
    [Header("Animator parameter names (case-sensitive)")]
    public string walkingBool   = "Camminando";
    public string lookXFloat    = "LookX";
    public string lookYFloat    = "LookY";
    public string hitTrigger    = "ColpoSubito";
    public string deathTrigger  = "Morte";

    [Header("Settings")]
    public Vector2 startingDirection = Vector2.down;
    public float minMovingSpeedSqr = 0.04f;
    public float hitStunTime = 0.25f;
    public float deathDuration = 1.6f;

    private Animator    animator;
    private MobAI       mobAI;
    private MobHealth   mobHealth;
    private Rigidbody2D rb;
    private MobAttack   mobAttack;

    private bool    isDead    = false;
    private bool    isStunned = false;
    private Vector2 lastDirection;

    void Awake()
    {
        animator   = GetComponent<Animator>();
        mobAI      = GetComponent<MobAI>();
        mobHealth  = GetComponent<MobHealth>();
        rb         = GetComponent<Rigidbody2D>();
        mobAttack  = GetComponent<MobAttack>();
        lastDirection = startingDirection;
    }

    void Start()
    {
        mobHealth.OnHit   += HandleHit;
        mobHealth.OnDeath += HandleDeath;
        SetLookDirection(startingDirection);
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
        if (isDead) return;
        UpdateMovementAnimation();
    }

    void UpdateMovementAnimation()
    {
        bool isAttacking = mobAttack != null && mobAttack.IsAttacking;

        if (isStunned || isAttacking)
        {
            if (!string.IsNullOrEmpty(walkingBool))
                animator.SetBool(walkingBool, false);
            return;
        }

        Vector2 vel = rb.velocity;
        bool moving = vel.sqrMagnitude > minMovingSpeedSqr;

        if (!string.IsNullOrEmpty(walkingBool))
            animator.SetBool(walkingBool, moving);

        if (moving)
        {
            lastDirection = vel.normalized;
            SetLookDirection(lastDirection);
        }
    }

    void SetLookDirection(Vector2 dir)
    {
        if (Mathf.Abs(dir.x) < 0.001f && Mathf.Abs(dir.y) < 0.001f) return;
        if (!string.IsNullOrEmpty(lookXFloat)) animator.SetFloat(lookXFloat, dir.x);
        if (!string.IsNullOrEmpty(lookYFloat)) animator.SetFloat(lookYFloat, dir.y);
    }

    void HandleHit()
    {
        if (isDead) return;
        if (!string.IsNullOrEmpty(hitTrigger))
            animator.SetTrigger(hitTrigger);
        StartCoroutine(HitStun());
    }

    IEnumerator HitStun()
    {
        isStunned = true;
        rb.velocity = Vector2.zero;
        if (mobAI != null) mobAI.enabled = false;
        yield return new WaitForSeconds(hitStunTime);
        if (!isDead && mobAI != null) mobAI.enabled = true;
        isStunned = false;
    }

    void HandleDeath()
    {
        if (isDead) return;
        isDead = true;
        StopAllCoroutines();
        if (!string.IsNullOrEmpty(deathTrigger))
            animator.SetTrigger(deathTrigger);
        rb.velocity    = Vector2.zero;
        rb.isKinematic = true;
        if (TryGetComponent(out Collider2D col)) col.enabled = false;
        if (mobAI != null) mobAI.enabled = false;
        Destroy(gameObject, deathDuration + 0.4f);
    }
}