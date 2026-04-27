using UnityEngine;

public class MobHealth : MonoBehaviour
{
    [Header("Vita")]
    public float maxHealth = 100f;
    private float currentHealth;

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        Debug.Log($"{gameObject.name} ha ricevuto {damage} danni. Vita rimasta: {currentHealth}");

        if (currentHealth <= 0f)
            Die();
    }

    void Die()
    {
        Debug.Log($"{gameObject.name} è morto.");
        Destroy(gameObject);
    }
}