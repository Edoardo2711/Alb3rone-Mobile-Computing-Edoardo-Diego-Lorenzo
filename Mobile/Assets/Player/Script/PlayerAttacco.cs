using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttacco : MonoBehaviour
{
    [Header("Componenti")]
    public Animator animator;
    public Transform attackPoint;

    [Header("Impostazioni Attacco")]
    public float attackRange = 1.5f;
    public float attackRate = 3f;
    [Tooltip("LayerMask opzionale. Se vuoto, vengono rilevati tutti i collider e filtrati per MobHealth.")]
    public LayerMask Mob;
    public float attackOffset = 0.6f;
    public float attackDamage = 25f;
    public bool debugLog = true;

    private float nextAttackTime = 0f;
    private MovementPlayer movementPlayer;

    void Start()
    {
        movementPlayer = GetComponent<MovementPlayer>();
        if (animator == null) animator = GetComponent<Animator>();
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (!context.started) return;
        TryAttack();
    }

    public void OnAttackInput() => TryAttack();

    private void TryAttack()
    {
        if (Time.time < nextAttackTime) return;
        if (attackPoint == null)
        {
            Debug.LogWarning("[PlayerAttacco] attackPoint non assegnato!");
            return;
        }

        Vector2 dir = (movementPlayer != null) ? movementPlayer.LastDirection : Vector2.down;

        if (animator != null)
        {
            animator.SetFloat("LookX", dir.x);
            animator.SetFloat("LookY", dir.y);
        }
        attackPoint.localPosition = new Vector3(dir.x * attackOffset, dir.y * attackOffset, 0);

        if (animator != null) animator.SetTrigger("Attacco");

        DealDamage();

        nextAttackTime = Time.time + 1f / attackRate;
    }

    private void DealDamage()
    {
        // Niente filtro per layer: cerchiamo tutti i collider e filtriamo per MobHealth.
        Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint.position, attackRange);

        int hitCount = 0;
        foreach (Collider2D col in hits)
        {
            if (col == null) continue;
            if (col.transform == transform || col.transform.IsChildOf(transform)) continue;

            MobHealth mh = col.GetComponent<MobHealth>()
                        ?? col.GetComponentInParent<MobHealth>()
                        ?? col.GetComponentInChildren<MobHealth>();

            if (mh != null)
            {
                mh.TakeDamage(attackDamage);
                hitCount++;
            }
        }

        if (debugLog)
            Debug.Log($"[PlayerAttacco] Attacco a {attackPoint.position} (raggio {attackRange}). Mob colpiti: {hitCount}");
    }

    void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}