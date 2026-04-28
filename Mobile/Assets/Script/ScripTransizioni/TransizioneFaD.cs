using UnityEngine;
using Cinemachine;

public class TransizioneFaD : MonoBehaviour
{
  [SerializeField] PolygonCollider2D mapBoundry;
  CinemachineConfiner confiner;

  [SerializeField] Direction direction;
  enum Direction {Up, Down, Left, Right}
  private void Awake()
    {   
        confiner = FindObjectOfType<CinemachineConfiner >();
    }

    private void OnTriggerEnter2D(Collider2D collision)
{
    // Ignora il collider fisico, reagisce solo al trigger del Player
    if (!collision.gameObject.CompareTag("Player")) return;
    
    confiner.m_BoundingShape2D = mapBoundry;
    confiner.InvalidatePathCache();
    UpdatePlayerPosition(collision.gameObject);
    
}

    private void UpdatePlayerPosition(GameObject player)
    {
        Vector3 newPos = player.transform.position;

        newPos.x -= 135 ;
        newPos.y -= 3 ;

        player.transform.position = newPos;
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb != null) rb.velocity = Vector2.zero;
    }
}
