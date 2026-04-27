using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttacco : MonoBehaviour
{
    [Header("Componenti")]
    public Animator animator;
    public Transform attackPoint;

    [Header("Impostazioni Attacco")]
    public float attackRange = 0.5f;
    public float attackRate = 2f;
    public LayerMask Mob;
    public float attackOffset = 0.5f;

    private float nextAttackTime = 0f;
    private MovementPlayer movementPlayer;

    void Start()
    {
        // Prende il riferimento a MovementPlayer per leggere la direzione
        movementPlayer = GetComponent<MovementPlayer>();
    }

    // Aggiunge questo metodo in PlayerAttacco.cs

// Wrapper senza parametri — questo appare nel dropdown

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        if (Time.time < nextAttackTime) return;

        // Legge la direzione da MovementPlayer invece che da Input.GetAxisRaw
        Vector2 dir = movementPlayer.LastDirection;

        // Aggiorna LookX/Y e sposta l'attackPoint nella direzione corretta
        animator.SetFloat("LookX", dir.x);
        animator.SetFloat("LookY", dir.y);
        attackPoint.localPosition = new Vector3(dir.x * attackOffset, dir.y * attackOffset, 0);

        Attack();
        nextAttackTime = Time.time + 1f / attackRate;
    }

    public void OnAttackInput()
{
    // Invoke Unity Events chiama il metodo due volte (started + performed)
    // Il cooldown nextAttackTime filtra i duplicati
    if (Time.time < nextAttackTime) return;

    Vector2 dir = movementPlayer.LastDirection;
    animator.SetFloat("LookX", dir.x);
    animator.SetFloat("LookY", dir.y);
    attackPoint.localPosition = new Vector3(dir.x * attackOffset, dir.y * attackOffset, 0);

    Attack();
    nextAttackTime = Time.time + 1f / attackRate;
}

    void Attack()
    {
        animator.SetTrigger("Attacco");

        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, Mob);

        foreach (Collider2D enemy in hitEnemies)
{
        MobHealth mobHealth = enemy.GetComponent<MobHealth>();
        if (mobHealth != null)
            mobHealth.TakeDamage(25f); // danno per colpo, modificabile
}
    }

    void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}