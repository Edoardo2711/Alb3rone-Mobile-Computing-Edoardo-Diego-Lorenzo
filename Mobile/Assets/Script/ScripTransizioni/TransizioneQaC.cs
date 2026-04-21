using UnityEngine;
using Cinemachine;

public class TransizioneQaC : MonoBehaviour
{
  [SerializeField] PolygonCollider2D mapBoundry;

  [SerializeField] float newCameraSize = 17f;
  CinemachineConfiner confiner;
  [SerializeField] CinemachineVirtualCamera  vcam;

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
            vcam.m_Lens.OrthographicSize = newCameraSize;
            UpdatePlayerPosition(collision.gameObject);
        }
    }

    private void UpdatePlayerPosition(GameObject player)
    {
        Vector3 newPos = player.transform.position;

        newPos.x -= 101;

        player.transform.position = newPos;
    }
}
