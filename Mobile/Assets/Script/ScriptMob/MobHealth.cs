using System;
using UnityEngine;

public class MobHealth : MonoBehaviour
{
    [Header("Vita")]
    public float maxHealth = 100f;
    private float currentHealth;

    /// <summary>Invocato quando il mob riceve un colpo (ma non muore).</summary>
    public event Action OnHit;

    /// <summary>Invocato alla morte.</summary>
    public event Action OnDeath;

    public bool IsDead => currentHealth <= 0f;

    void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float damage)
    {
        if (IsDead) return;

        currentHealth -= damage;
        Debug.Log($"{gameObject.name} ricevuto {damage} danni. Vita: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0f)
            Die();
        else
            OnHit?.Invoke();
    }

    public void Heal(float amount)
    {
        if (IsDead) return;
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
    }

    void Die()
    {
        Debug.Log($"{gameObject.name} morto.");
        OnDeath?.Invoke();
        // Se nessun subscriber gestisce la distruzione, distruggi qui
        if (OnDeath == null) Destroy(gameObject);
    }

    public float GetHealthPercent() => Mathf.Clamp01(currentHealth / maxHealth);
}