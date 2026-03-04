using UnityEngine;

public class PlayerAiming : MonoBehaviour
{
    public Rigidbody2D rb;
    private Vector2 mousePos;

    void Update()
    {
        // Trasforma la posizione del mouse da "pixel dello schermo" a "coordinate del mondo"
        mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
    }

    void FixedUpdate()
    {
        // Calcola la direzione: Destinazione (mouse) - Partenza (giocatore)
        Vector2 lookDir = mousePos - rb.position;

        // Calcola l'angolo in gradi. Sottraiamo 90 gradi se lo sprite è disegnato verso l'alto
        float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg - 90f;

        // Ruota il corpo fisico
        rb.rotation = angle;
    }
}