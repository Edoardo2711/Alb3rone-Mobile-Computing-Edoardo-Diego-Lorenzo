using UnityEngine;
using UnityEngine.InputSystem; // Necessario per il nuovo sistema

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;  // Velocità di movimento
    public Rigidbody2D rb;        // Riferimento al corpo fisico
    
    // Questa variabile permette di configurare i tasti nell'Inspector
    public InputAction moveAction; 

    Vector2 movement;

    // Attiva l'input quando l'oggetto è abilitato
    void OnEnable()
    {
        moveAction.Enable();
    }

    // Disabilita l'input quando l'oggetto è disabilitato
    void OnDisable()
    {
        moveAction.Disable();
    }

    void Update()
    {
        // Legge il valore dei tasti (WASD o Frecce) come un vettore X, Y
        movement = moveAction.ReadValue<Vector2>();
    }

    void FixedUpdate()
    {
        // Muove il personaggio applicando la velocità alla posizione fisica
        // .normalized impedisce di andare più veloci in diagonale
        rb.MovePosition(rb.position + movement.normalized * moveSpeed * Time.fixedDeltaTime);
    }
}