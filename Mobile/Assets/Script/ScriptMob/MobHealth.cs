using System;
using UnityEngine;

public class MobHealth : MonoBehaviour
{
    [Header("Vita")]
    public float maxHealth = 100f;
    private float currentHealth;

    [Header("Invulnerabilita'")]
    [Tooltip("Tempo (sec) di invulnerabilita' dopo aver subito danno.")]
    public float invulnerabilityTime = 0.15f;
    private float lastDamageTime = -999f;

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
        if (Time.time - lastDamageTime < invulnerabilityTime) return;
        lastDamageTime = Time.time;

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
        // Salva il riferimento ai subscriber prima di invocare:
        // se nessuno gestisce la distruzione, la facciamo noi.
        bool hadSubscribers = OnDeath != null;
        OnDeath?.Invoke();
        if (!hadSubscribers) Destroy(gameObject);
    }

    public float GetHealthPercent() => Mathf.Clamp01(currentHealth / maxHealth);
}
