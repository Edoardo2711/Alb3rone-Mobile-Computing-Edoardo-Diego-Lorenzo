using UnityEngine;
using UnityEngine.InputSystem;

public class MovementPlayer : MonoBehaviour
{
    public float Velocita = 5f;

    [Header("Input")]
    [Tooltip("Soglia minima di input per essere considerato valido. Filtra stick drift dei gamepad.")]
    [Range(0f, 0.5f)]
    public float inputDeadzone = 0.2f;

    [Header("Attacco")]
    [Tooltip("Frazione di velocita' mantenuta mentre il player attacca. 0 = fermo come prima, 1 = velocita' piena.")]
    [Range(0f, 1f)]
    public float attackMoveMultiplier = 0.45f;

    [Header("Fluidita'")]
    [Tooltip("Quanto in fretta la velocita' raggiunge quella desiderata (unita'/s^2). Valori alti = piu' reattivo, 0 = istantaneo come prima.")]
    public float accelerazione = 80f;

    private Rigidbody2D rb;
    private Animator animator;
    private PlayerHealth health;
    private Vector2 moveInput;
    private Vector2 lastNonZeroDirection = Vector2.down;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        health = GetComponent<PlayerHealth>();
        if (animator == null)
            Debug.LogWarning("[MovementPlayer] Animator mancante su " + name);

        // Difensivo: assicura che il player parta fermo
        moveInput = Vector2.zero;
        if (rb != null) rb.linearVelocity = Vector2.zero;

        if (animator != null)
        {
            animator.SetBool("Camminando", false);
            animator.SetFloat("LastInputX", lastNonZeroDirection.x);
            animator.SetFloat("LastInputY", lastNonZeroDirection.y);
            animator.SetFloat("LookX", lastNonZeroDirection.x);
            animator.SetFloat("LookY", lastNonZeroDirection.y);
        }
    }

    void Update()
    {
        if (rb == null) return;

        // Clamp magnitudine per evitare diagonale piu' veloce (max 1)
        Vector2 input = Vector2.ClampMagnitude(moveInput, 1f);

        // Durante l'attacco il player rallenta ma NON si inchioda: bloccarlo del tutto
        // per l'intera durata della clip lo faceva sembrare fermo a mezz'aria.
        float velocita = Velocita;
        if (animator != null && animator.GetCurrentAnimatorStateInfo(0).IsTag("Attack"))
            velocita *= attackMoveMultiplier;

        Vector2 desiderata = input * velocita;

        // Raggiungi la velocita' desiderata gradualmente invece che di scatto:
        // toglie lo strappo sia all'inizio che alla fine dell'attacco.
        rb.linearVelocity = (accelerazione > 0f)
            ? Vector2.MoveTowards(rb.linearVelocity, desiderata, accelerazione * Time.deltaTime)
            : desiderata;
    }

    public void Move(InputAction.CallbackContext context)
    {
        // Da morto l'input non deve piu' arrivare all'Animator: Update() e' gia' fermo
        // perche' il componente viene disabilitato, ma PlayerInput continua a invocare
        // questo metodo e senza la guardia il cadavere resterebbe in animazione di corsa.
        if (health != null && health.IsDead)
        {
            moveInput = Vector2.zero;
            return;
        }

        Vector2 raw = context.ReadValue<Vector2>();

        // Deadzone difensiva: ignora input sotto soglia (stick drift dei gamepad)
        if (raw.magnitude < inputDeadzone) raw = Vector2.zero;

        if (context.canceled || raw == Vector2.zero)
        {
            moveInput = Vector2.zero;
            if (animator != null)
            {
                animator.SetBool("Camminando", false);
                animator.SetFloat("LastInputX", lastNonZeroDirection.x);
                animator.SetFloat("LastInputY", lastNonZeroDirection.y);
            }
        }
        else
        {
            moveInput = raw;
            lastNonZeroDirection = raw.normalized;

            if (animator != null)
            {
                animator.SetBool("Camminando", true);
                animator.SetFloat("InputX", moveInput.x);
                animator.SetFloat("InputY", moveInput.y);
                animator.SetFloat("LookX", lastNonZeroDirection.x);
                animator.SetFloat("LookY", lastNonZeroDirection.y);
            }
        }
    }

    // Proprieta' pubblica per condividere la direzione con PlayerAttacco
    public Vector2 LastDirection => lastNonZeroDirection;
}
