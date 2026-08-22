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

    [Header("Feedback colpo subito")]
    [Tooltip("Lampeggio di colore quando il mob subisce danno. Utile soprattutto sui mob privi di animazione di 'colpo subito'.")]
    public bool hitFlash = true;
    public Color hitFlashColor = new Color(1f, 0.45f, 0.45f, 1f);
    [Tooltip("Durata di ogni mezzo lampeggio (sec).")]
    public float hitFlashDuration = 0.06f;
    [Tooltip("Numero di lampeggi.")]
    public int hitFlashCount = 2;

    [Tooltip("Ampiezza della vibrazione quando il mob subisce danno, in unita' di mondo. 0 per disabilitarla.")]
    public float hitShakeAmount = 0f;
    [Tooltip("Durata della vibrazione (sec).")]
    public float hitShakeDuration = 0.15f;

    private Animator    animator;
    private MobAI       mobAI;
    private MobHealth   mobHealth;
    private Rigidbody2D rb;
    private IMobAttack  mobAttack;   // MobAttack (corpo a corpo) o MobRangedAttack (a distanza)

    private bool    isDead    = false;
    private bool    isStunned = false;
    private Vector2 lastDirection;

    // MobAI muove il rigidbody con MovePosition, quindi rb.linearVelocity non
    // rispecchia lo spostamento reale: ce lo misuriamo dal delta di posizione.
    private Vector2 lastPosition;
    private Vector2 measuredVelocity;

    // Feedback di danno
    private SpriteRenderer[] sprites;
    private Color[]  spriteBaseColors;
    private Coroutine flashCo;
    private Coroutine shakeCo;
    // Offset attualmente applicato dalla vibrazione: e' RELATIVO (ogni frame si
    // annulla il precedente) e va sottratto dalla misura della velocita', altrimenti
    // un mob fermo che vibra sembrerebbe in movimento.
    private Vector3 shakeOffset = Vector3.zero;

    void Awake()
    {
        animator   = GetComponent<Animator>();
        mobAI      = GetComponent<MobAI>();
        mobHealth  = GetComponent<MobHealth>();
        rb         = GetComponent<Rigidbody2D>();
        mobAttack  = GetComponent<IMobAttack>();
        lastDirection = startingDirection;
        lastPosition  = rb.position;

        sprites = GetComponentsInChildren<SpriteRenderer>(true);
        spriteBaseColors = new Color[sprites.Length];
        for (int i = 0; i < sprites.Length; i++) spriteBaseColors[i] = sprites[i].color;
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

    void FixedUpdate()
    {
        if (isDead) return;

        // Sottrai la vibrazione: e' un effetto visivo, non movimento reale
        Vector2 now = rb.position - (Vector2)shakeOffset;
        float dt = Time.fixedDeltaTime;
        measuredVelocity = (dt > 0f) ? (now - lastPosition) / dt : Vector2.zero;
        lastPosition = now;
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

        Vector2 vel = measuredVelocity;
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

        if (hitFlash && sprites.Length > 0)
        {
            if (flashCo != null) StopCoroutine(flashCo);
            flashCo = StartCoroutine(HitFlash());
        }

        if (hitShakeAmount > 0f)
        {
            if (shakeCo != null) StopCoroutine(shakeCo);
            shakeCo = StartCoroutine(HitShake());
        }
    }

    IEnumerator HitFlash()
    {
        for (int n = 0; n < Mathf.Max(1, hitFlashCount); n++)
        {
            SetSpriteColor(hitFlashColor);
            yield return new WaitForSeconds(hitFlashDuration);
            RestoreSpriteColors();
            yield return new WaitForSeconds(hitFlashDuration);
        }
        flashCo = null;
    }

    // Vibrazione RELATIVA: ogni frame annulla l'offset precedente e ne applica uno
    // nuovo, cosi' segue il mob mentre si muove invece di riportarlo indietro.
    IEnumerator HitShake()
    {
        float elapsed = 0f;
        while (elapsed < hitShakeDuration)
        {
            transform.position -= shakeOffset;
            shakeOffset = new Vector3(
                Random.Range(-hitShakeAmount, hitShakeAmount),
                Random.Range(-hitShakeAmount, hitShakeAmount),
                0f);
            transform.position += shakeOffset;

            elapsed += Time.deltaTime;
            yield return null;
        }
        ClearShakeOffset();
        shakeCo = null;
    }

    void ClearShakeOffset()
    {
        if (shakeOffset == Vector3.zero) return;
        transform.position -= shakeOffset;
        shakeOffset = Vector3.zero;
    }

    void SetSpriteColor(Color c)
    {
        for (int i = 0; i < sprites.Length; i++)
            if (sprites[i] != null) sprites[i].color = c;
    }

    void RestoreSpriteColors()
    {
        for (int i = 0; i < sprites.Length; i++)
            if (sprites[i] != null) sprites[i].color = spriteBaseColors[i];
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

        // StopAllCoroutines interrompe flash e vibrazione a meta': ripulisci a mano,
        // altrimenti il mob resta tinto e spostato e il fade-out partirebbe dal
        // colore del lampeggio invece che da quello originale.
        flashCo = null;
        shakeCo = null;
        ClearShakeOffset();
        RestoreSpriteColors();

        if (!string.IsNullOrEmpty(deathTrigger))
            animator.SetTrigger(deathTrigger);
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
        if (TryGetComponent(out Collider2D col)) col.enabled = false;
        if (mobAI != null) mobAI.enabled = false;
        var attackBehaviour = mobAttack as MonoBehaviour;
        if (attackBehaviour != null) attackBehaviour.enabled = false;

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