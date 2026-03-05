using UnityEngine;

public class Proiettile : MonoBehaviour
{

    private Vector3 mousePos; 
    private Camera mainCam;

    private Rigidbody2D rb;
    
    public float force;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        mainCam = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
        rb = GetComponent<Rigidbody2D>();
        mousePos = mainCam.ScreenToWorldPoint(Input.mousePosition);
        Vector3 direction = mousePos - transform.position;
        rb.linearVelocity = new Vector2(direction.x , direction.y).normalized * force;

        Destroy(gameObject, 5f);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
