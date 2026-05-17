using UnityEngine;
using UnityEngine.InputSystem;

public class MovementPlayer : MonoBehaviour
{
    public float Velocita = 5f;

    [Header("Input")]
    [Tooltip("Soglia minima di input per essere considerato valido. Filtra stick drift dei gamepad.")]
    [Range(0f, 0.5f)]
    public float inputDeadzone = 0.2f;

    private Rigidbody2D rb;
    private Animator animator;
    private Vector2 moveInput;
    private Vector2 lastNonZeroDirection = Vector2.down;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
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

        // Blocca movimento durante attacco
        if (animator != null && animator.GetCurrentAnimatorStateInfo(0).IsTag("Attack"))
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // Clamp magnitudine per evitare diagonale piu' veloce (max 1)
        Vector2 input = Vector2.ClampMagnitude(moveInput, 1f);
        rb.linearVelocity = input * Velocita;
    }

    public void Move(InputAction.CallbackContext context)
    {
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
