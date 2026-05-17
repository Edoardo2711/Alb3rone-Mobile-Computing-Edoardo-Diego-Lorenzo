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
    [Tooltip("Durata stun dopo un colpo. Tienilo basso (0.08-0.15) per non bloccare il mob.")]
    public float hitStunTime = 0.1f;
    [Tooltip("Se true, durante lo stun viene disabilitato MobAI. Tienilo OFF per non interrompere l'attacco del mob.")]
    public bool disableAIDuringStun = false;
    [Tooltip("Tempo (sec) prima che il mob venga distrutto dopo la morte. Tienilo coerente con la durata dell'animazione di morte.")]
    public float deathDuration = 0.6f;
    [Tooltip("Buffer extra dopo deathDuration prima della distruzione del GameObject.")]
    public float destroyExtraDelay = 0.05f;
    [Tooltip("Se true, lo sprite scompare gradualmente in fade-out durante deathDuration.")]
    public bool fadeOutOnDeath = true;

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

        Vector2 vel = rb.linearVelocity;
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
        // Non azzeriamo piu' la velocity ne' disabilitiamo l'AI:
        // lasciamo che il mob continui a muoversi/attaccare.
        // Lo stun serve solo a far giocare l'animazione del colpo nell'animator.
        if (disableAIDuringStun && mobAI != null) mobAI.enabled = false;
        yield return new WaitForSeconds(hitStunTime);
        if (disableAIDuringStun && !isDead && mobAI != null) mobAI.enabled = true;
        isStunned = false;
    }

    void HandleDeath()
    {
        if (isDead) return;
        isDead = true;
        StopAllCoroutines();
        if (!string.IsNullOrEmpty(deathTrigger))
            animator.SetTrigger(deathTrigger);
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
        if (TryGetComponent(out Collider2D col)) col.enabled = false;
        if (mobAI != null) mobAI.enabled = false;
        if (mobAttack != null) mobAttack.enabled = false;

        StartCoroutine(DeathRoutine());
    }

    IEnumerator DeathRoutine()
    {
        if (fadeOutOnDeath)
        {
            var srs = GetComponentsInChildren<SpriteRenderer>(true);
            // Memorizza i colori iniziali
            Color[] startColors = new Color[srs.Length];
            for (int i = 0; i < srs.Length; i++) startColors[i] = srs[i].color;

            float t = 0f;
            // Fade-out negli ultimi 50% di deathDuration
            float fadeStart = deathDuration * 0.5f;
            while (t < deathDuration)
            {
                t += Time.deltaTime;
                if (t > fadeStart)
                {
                    float a = 1f - Mathf.Clamp01((t - fadeStart) / (deathDuration - fadeStart));
                    for (int i = 0; i < srs.Length; i++)
                    {
                        if (srs[i] == null) continue;
                        var c = startColors[i];
                        c.a = a;
                        srs[i].color = c;
                    }
                }
                yield return null;
            }
        }
        else
        {
            yield return new WaitForSeconds(deathDuration);
        }

        if (destroyExtraDelay > 0f) yield return new WaitForSeconds(destroyExtraDelay);

        Destroy(gameObject);
    }
}