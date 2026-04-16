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

    private void OnTriggerEnter2D (Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            confiner.m_BoundingShape2D = mapBoundry;
            UpdatePlayerPosition(collision.gameObject);
        }
    }

    private void UpdatePlayerPosition(GameObject player)
    {
        Vector3 newPos = player.transform.position;

        newPos.x -= 135 ;
        newPos.y -= 3 ;

        player.transform.position = newPos;
    }
}
