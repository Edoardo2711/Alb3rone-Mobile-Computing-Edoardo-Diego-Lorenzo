using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Vita")]
    public float maxHealth = 100f;
    private float currentHealth;

    public event Action OnHit;
    public event Action OnDeath;
    public bool IsDead => currentHealth <= 0f;

    void Awake() { currentHealth = maxHealth; }

    public void TakeDamage(float damage)
    {
        if (IsDead) return;
        currentHealth -= damage;
        Debug.Log($"[PlayerHealth] Danno: {damage}. Vita: {currentHealth}/{maxHealth}");
        if (currentHealth <= 0f) Die();
        else OnHit?.Invoke();
    }

    public void Heal(float amount)
    {
        if (IsDead) return;
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
    }

    void Die()
    {
        currentHealth = 0f;
        OnDeath?.Invoke();
    }

    public float GetHealthPercent() => Mathf.Clamp01(currentHealth / maxHealth);
}