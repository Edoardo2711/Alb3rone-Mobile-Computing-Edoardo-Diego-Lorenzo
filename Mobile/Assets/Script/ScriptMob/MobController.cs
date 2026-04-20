using UnityEngine;

public class MobController : MonoBehaviour
{
    [Header("Impostazioni Movimento")]
    public float speed = 3f;
    public float aggroRange = 8f; // Distanza entro la quale il mob "vede" il player

    [Header("Riferimenti")]
    public Transform player; 

    // Variabile per tenere traccia della direzione verso cui guarda il mob (di default a destra)
    private bool isFacingRight = true;

    void Update()
    {
        // Evitiamo errori se il player non è assegnato o è stato distrutto
        if (player == null) return;

        // Calcola la distanza tra il mob e il player
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        // Se il player è nel raggio d'azione, inseguilo
        if (distanceToPlayer <= aggroRange)
        {
            ChasePlayer();
        }
    }

    void ChasePlayer()
    {
        // 1. Logica di Movimento verso il player
        transform.position = Vector2.MoveTowards(transform.position, player.position, speed * Time.deltaTime);

        // 2. Logica di Ribaltamento (Flip)
        // Se il player è a destra del mob e il mob guarda a sinistra...
        if (player.position.x > transform.position.x && !isFacingRight)
        {
            Flip();
        }
        // ...altrimenti, se il player è a sinistra del mob e il mob guarda a destra...
        else if (player.position.x < transform.position.x && isFacingRight)
        {
            Flip();
        }
    }

    void Flip()
    {
        // Inverti lo stato della direzione
        isFacingRight = !isFacingRight;

        // Prendi la scala attuale, moltiplica l'asse X per -1 e riassegnala
        Vector3 localScale = transform.localScale;
        localScale.x *= -1f;
        transform.localScale = localScale;
    }
}