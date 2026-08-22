using UnityEngine;

/// <summary>
/// Proiettile sparato dai mob a distanza (vedi MobRangedAttack).
/// Vola in linea retta finche' non colpisce il bersaglio o un ostacolo,
/// poi riproduce l'animazione di impatto e si autodistrugge.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class Fireball : MonoBehaviour
{
    [Header("Volo")]
    public float speed = 7f;
    [Tooltip("Secondi dopo i quali il proiettile esplode da solo se non colpisce nulla.")]
    public float maxLifetime = 4f;

    [Header("Danno")]
    public float damage = 8f;
    [Tooltip("Se true, il proiettile ruota per puntare nella direzione di volo.")]
    public bool rotateTowardsDirection = false;

    [Header("Impatto")]
    public string impactTrigger = "Impatto";
    [Tooltip("Durata dell'animazione di impatto prima della distruzione.")]
    public float impactDuration = 0.35f;

    private Vector2   direction = Vector2.right;
    private Animator  animator;
    private Collider2D col;
    private bool      hasImpacted = false;
    private float     spawnTime;

    void Awake()
    {
        animator  = GetComponent<Animator>();
        col       = GetComponent<Collider2D>();
        spawnTime = Time.time;
    }

    /// <summary>Chiamata da chi spara subito dopo l'Instantiate.</summary>
    public void Launch(Vector2 dir, float projectileSpeed, float projectileDamage)
    {
        direction = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.right;
        speed     = projectileSpeed;
        damage    = projectileDamage;

        if (rotateTowardsDirection)
        {
            float ang = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, ang);
        }
    }

    void Update()
    {
        if (hasImpacted) return;

        transform.position += (Vector3)(direction * speed * Time.deltaTime);

        if (Time.time - spawnTime >= maxLifetime)
            Impact();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (hasImpacted) return;

        // Ignora gli altri trigger (zone di spawn, waypoint di transizione, ...):
        // il proiettile deve esplodere solo su bersagli e ostacoli solidi.
        if (other.isTrigger) return;

        // Non colpisce i mob (compreso chi lo ha sparato) ne' gli altri proiettili
        if (other.GetComponentInParent<MobHealth>() != null) return;
        if (other.GetComponentInParent<Fireball>() != null) return;

        PlayerHealth ph = other.GetComponent<PlayerHealth>()
                       ?? other.GetComponentInParent<PlayerHealth>();
        if (ph != null) ph.TakeDamage(damage);

        Impact();
    }

    void Impact()
    {
        if (hasImpacted) return;
        hasImpacted = true;

        speed = 0f;
        if (col != null) col.enabled = false;
        if (animator != null && !string.IsNullOrEmpty(impactTrigger))
            animator.SetTrigger(impactTrigger);

        Destroy(gameObject, Mathf.Max(impactDuration, 0.01f));
    }
}
