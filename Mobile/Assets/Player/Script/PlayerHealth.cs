using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Vita")]
    public float maxHealth = 100f;
    private float currentHealth;

    [Header("Invulnerabilita'")]
    [Tooltip("Tempo (sec) di invulnerabilita' dopo aver subito danno.")]
    public float invulnerabilityTime = 0.3f;
    private float lastDamageTime = -999f;

    [Header("Animator")]
    [Tooltip("Trigger dell'animazione di morte (lasciare vuoto per disabilitare).")]
    public string deathTrigger = "Morte";

    [Header("Feedback visivo Hit")]
    [Tooltip("Colore del flash quando si subisce danno.")]
    public Color hitFlashColor = new Color(1f, 0.25f, 0.25f, 1f);
    [Tooltip("Durata di ogni lampeggio (sec).")]
    public float hitFlashDuration = 0.08f;
    [Tooltip("Numero di lampeggi.")]
    public int hitFlashCount = 3;
    [Tooltip("Forza dello shake della camera/player (0 per disabilitare).")]
    public float hitShakeAmount = 0.08f;
    [Tooltip("Durata dello shake (sec).")]
    public float hitShakeDuration = 0.15f;

    public event Action OnHit;
    public event Action OnDeath;
    public bool IsDead => currentHealth <= 0f;

    // Cache di tutti gli SpriteRenderer figli per il flash
    private SpriteRenderer[] spriteRenderers;
    private Color[] originalColors;
    private Coroutine flashCo;
    private Coroutine shakeCo;

    // Offset attualmente applicato dallo shake. Lo shake e' RELATIVO: ogni frame
    // annulla l'offset precedente e ne applica uno nuovo, cosi' vibra attorno alla
    // posizione corrente del player e non attorno a quella di partenza.
    private Vector3 shakeOffset = Vector3.zero;

    void Awake()
    {
        currentHealth = maxHealth;
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        originalColors = new Color[spriteRenderers.Length];
        for (int i = 0; i < spriteRenderers.Length; i++)
            originalColors[i] = spriteRenderers[i].color;
    }

    public void TakeDamage(float damage)
    {
        if (IsDead) return;
        if (Time.time - lastDamageTime < invulnerabilityTime) return;
        lastDamageTime = Time.time;

        currentHealth -= damage;
        Debug.Log($"[PlayerHealth] Danno subito: {damage}. Vita: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0f)
        {
            Die();
        }
        else
        {
            OnHit?.Invoke();
            // Feedback visivo
            if (flashCo != null) StopCoroutine(flashCo);
            flashCo = StartCoroutine(FlashCoroutine());
            if (hitShakeAmount > 0f)
            {
                if (shakeCo != null) StopCoroutine(shakeCo);
                shakeCo = StartCoroutine(ShakeCoroutine());
            }
        }
    }

    public void Heal(float amount)
    {
        if (IsDead) return;
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
    }

    void Die()
    {
        currentHealth = 0f;
        Debug.Log("[PlayerHealth] Player morto!");
        OnDeath?.Invoke();

        // Disattiva movimento e attacco
        var mv  = GetComponent<MovementPlayer>();
        var atk = GetComponent<PlayerAttacco>();
        if (mv  != null) mv.enabled  = false;
        if (atk != null) atk.enabled = false;

        // Azzera fisica
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;

        // Trigger animazione di morte se presente
        var anim = GetComponent<Animator>();
        if (anim != null && !string.IsNullOrEmpty(deathTrigger))
        {
            try { anim.SetTrigger(deathTrigger); }
            catch { /* parametro mancante: ignora */ }
        }
    }

    IEnumerator FlashCoroutine()
    {
        for (int n = 0; n < hitFlashCount; n++)
        {
            // ON: colora di rosso
            for (int i = 0; i < spriteRenderers.Length; i++)
                if (spriteRenderers[i] != null) spriteRenderers[i].color = hitFlashColor;
            yield return new WaitForSeconds(hitFlashDuration);

            // OFF: torna al colore originale
            for (int i = 0; i < spriteRenderers.Length; i++)
                if (spriteRenderers[i] != null) spriteRenderers[i].color = originalColors[i];
            yield return new WaitForSeconds(hitFlashDuration);
        }
        flashCo = null;
    }

    IEnumerator ShakeCoroutine()
    {
        float elapsed = 0f;
        while (elapsed < hitShakeDuration)
        {
            // Annulla l'offset del frame precedente PRIMA di applicare il nuovo:
            // il risultato e' una vibrazione attorno a dove il player si trova ora.
            transform.position -= shakeOffset;
            shakeOffset = new Vector3(
                UnityEngine.Random.Range(-hitShakeAmount, hitShakeAmount),
                UnityEngine.Random.Range(-hitShakeAmount, hitShakeAmount),
                0f);
            transform.position += shakeOffset;

            elapsed += Time.deltaTime;
            yield return null;
        }
        ClearShakeOffset();
        shakeCo = null;
    }

    // Rimuove l'offset residuo dello shake senza toccare la posizione "vera".
    void ClearShakeOffset()
    {
        if (shakeOffset == Vector3.zero) return;
        transform.position -= shakeOffset;
        shakeOffset = Vector3.zero;
    }

    void OnDisable()
    {
        // Ripristina sprite e annulla l'eventuale offset se la coroutine viene interrotta
        if (spriteRenderers != null && originalColors != null)
            for (int i = 0; i < spriteRenderers.Length; i++)
                if (spriteRenderers[i] != null) spriteRenderers[i].color = originalColors[i];
        ClearShakeOffset();
    }

    public float GetHealthPercent() => Mathf.Clamp01(currentHealth / maxHealth);
}
