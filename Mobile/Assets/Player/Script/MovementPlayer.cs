using UnityEngine;
using UnityEngine.InputSystem;

public class MovementPlayer : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public float Velocita;
    private Rigidbody2D rb;

    private Animator animator;
    private Vector2 moveInput; 

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        rb.linearVelocity = moveInput * Velocita;
    }

    public void Move(InputAction.CallbackContext context)
    {

        animator.SetBool("Camminando", true);

        if (context.canceled)
        {
            animator.SetBool("Camminando", false);
            animator.SetFloat("LastInputX", moveInput.x);
            animator.SetFloat("LastInputY", moveInput.y);
        }

        moveInput = context.ReadValue<Vector2>();

        animator.SetFloat("InputX", moveInput.x);
        animator.SetFloat("InputY", moveInput.y);

        }
}
