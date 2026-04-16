using UnityEngine;

public class PlayerAttacco : MonoBehaviour
{
    [Header("Componenti")]
    public Animator animator;
    public Transform attackPoint; // Un GameObject vuoto figlio del Player
    
    [Header("Impostazioni Attacco")]
    public float attackRange = 0.5f;
    public float attackRate = 2f; // Attacchi al secondo
    public LayerMask enemyLayers; // Il layer assegnato ai nemici
    public float attackOffset = 0.5f; // Distanza dell'hitbox dal centro del player

    private float nextAttackTime = 0f;
    private Vector2 lastDirection = new Vector2(0, -1); // Direzione predefinita (giù)

    void Update()
    {
        // 1. Calcola la direzione basata sull'input di movimento
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");

        if (moveX != 0 || moveY != 0)
        {
            lastDirection = new Vector2(moveX, moveY).normalized;
            
            // Aggiorna i parametri dell'Animator per far sapere verso dove guardiamo
            animator.SetFloat("LookX", lastDirection.x);
            animator.SetFloat("LookY", lastDirection.y);

            // Sposta il punto di attacco nella direzione in cui stiamo guardando
            attackPoint.localPosition = new Vector3(lastDirection.x * attackOffset, lastDirection.y * attackOffset, 0);
        }

        // 2. Gestione del cooldown e dell'input di attacco
        if (Time.time >= nextAttackTime)
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
            {
                Attack();
                nextAttackTime = Time.time + 1f / attackRate;
            }
        }
    }

    void Attack()
    {
        // Avvia l'animazione tramite il Trigger
        animator.SetTrigger("Attacco");

        // Rileva tutti i collider dei nemici all'interno del raggio d'azione
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayers);

        // Applica i danni ai nemici colpiti
        foreach (Collider2D enemy in hitEnemies)
        {
            Debug.Log("Hai colpito: " + enemy.name);
            
            // Se hai uno script per la vita del nemico, lo richiami qui. Esempio:
            // enemy.GetComponent<EnemyHealth>().TakeDamage(10);
        }
    }

    // Disegna il raggio d'attacco nell'editor di Unity per aiutarti a bilanciarlo
    void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}