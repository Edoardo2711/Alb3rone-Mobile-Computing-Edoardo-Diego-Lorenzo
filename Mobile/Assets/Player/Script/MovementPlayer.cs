using UnityEngine;
using UnityEngine.InputSystem;

public class MovementPlayer : MonoBehaviour
{
    public float Velocita;
    private Rigidbody2D rb;
    private Animator animator;
    private Vector2 moveInput;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        // Blocca movimento durante attacco
        if (animator.GetCurrentAnimatorStateInfo(0).IsName("Attacco")) 
        {
            rb.velocity = Vector2.zero;
            return;
        }

        rb.velocity = moveInput * Velocita;
    }

    public void Move(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();

        if (context.canceled)
        {
            animator.SetBool("Camminando", false);
            // Salva l'ultima direzione per l'idle direzionale
            animator.SetFloat("LastInputX", moveInput.x);
            animator.SetFloat("LastInputY", moveInput.y);
            moveInput = Vector2.zero;
        }
        else
        {
            animator.SetBool("Camminando", true);
            animator.SetFloat("InputX", moveInput.x);
            animator.SetFloat("InputY", moveInput.y);

            // Aggiorna LookX/Y qui, così PlayerAttacco ha sempre la direzione corretta
            animator.SetFloat("LookX", moveInput.x);
            animator.SetFloat("LookY", moveInput.y);
        }
    }

    // Proprietà pubblica per condividere la direzione con PlayerAttacco
    public Vector2 LastDirection => moveInput == Vector2.zero
        ? new Vector2(animator.GetFloat("LastInputX"), animator.GetFloat("LastInputY"))
        : moveInput;
}