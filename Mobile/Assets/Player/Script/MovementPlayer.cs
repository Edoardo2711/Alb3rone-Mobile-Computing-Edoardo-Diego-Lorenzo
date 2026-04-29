using UnityEngine;
using UnityEngine.InputSystem;

public class MovementPlayer : MonoBehaviour
{
    public float Velocita;
    private Rigidbody2D rb;
    private Animator animator;
    private Vector2 moveInput;
    private Vector2 lastNonZeroDirection = Vector2.down;
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        animator.SetFloat("LastInputX", lastNonZeroDirection.x);
        animator.SetFloat("LastInputY", lastNonZeroDirection.y);
        animator.SetFloat("LookX", lastNonZeroDirection.x);
        animator.SetFloat("LookY", lastNonZeroDirection.y);
    }

    void Update()
    {
        // Blocca movimento durante attacco
        if (animator.GetCurrentAnimatorStateInfo(0).IsTag("Attack")) 
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        rb.linearVelocity = moveInput * Velocita;
    }

    public void Move(InputAction.CallbackContext context)
    {
        if (context.canceled)
        {
            animator.SetBool("Camminando", false);
            animator.SetFloat("LastInputX", lastNonZeroDirection.x);
            animator.SetFloat("LastInputY", lastNonZeroDirection.y);
            moveInput = Vector2.zero;
        }
        else
        {
            moveInput = context.ReadValue<Vector2>();

            if (moveInput.sqrMagnitude > 0.01f)
            {
                lastNonZeroDirection = moveInput.normalized;

                animator.SetBool("Camminando", true);
                animator.SetFloat("InputX", moveInput.x);
                animator.SetFloat("InputY", moveInput.y);
                animator.SetFloat("LookX", lastNonZeroDirection.x);
                animator.SetFloat("LookY", lastNonZeroDirection.y);
            }
        }
    }

    // Proprietà pubblica per condividere la direzione con PlayerAttacco
    public Vector2 LastDirection => lastNonZeroDirection;
        
}