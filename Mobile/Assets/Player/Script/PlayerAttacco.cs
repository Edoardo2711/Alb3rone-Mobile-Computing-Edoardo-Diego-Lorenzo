using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttacco : MonoBehaviour
{
    [Header("Componenti")]
    public Animator animator;
    public Transform attackPoint;

    [Header("Impostazioni Attacco")]
    public float attackRange = 0.5f;
    public float attackRate = 3f;
    public LayerMask Mob;
    public float attackOffset = 0.5f;
    public float attackDamage = 25f;
    private float nextAttackTime = 0f;
    private MovementPlayer movementPlayer;

    void Start()
    {
        // Prende il riferimento a MovementPlayer per leggere la direzione
        movementPlayer = GetComponent<MovementPlayer>();
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (!context.started) return;
        TryAttack();
    }

    public void OnAttackInput()
    {
        TryAttack();
    }

    private void TryAttack()
    {
        if (Time.time < nextAttackTime) return;

        Vector2 dir = movementPlayer.LastDirection;

        animator.SetFloat("LookX", dir.x);
        animator.SetFloat("LookY", dir.y);
        attackPoint.localPosition = new Vector3(dir.x * attackOffset, dir.y * attackOffset, 0);

        animator.SetTrigger("Attacco");
        DealDamage();

        nextAttackTime = Time.time + 1f / attackRate;
    }

    private void DealDamage()
    {
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, Mob);

        foreach (Collider2D enemy in hitEnemies)
        {
            MobHealth mobHealth = enemy.GetComponent<MobHealth>();
            if (mobHealth != null)
                mobHealth.TakeDamage(attackDamage);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}