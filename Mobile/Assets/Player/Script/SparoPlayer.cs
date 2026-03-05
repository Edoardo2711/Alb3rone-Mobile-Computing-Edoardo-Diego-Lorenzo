using UnityEngine;

public class SparoPlayer : MonoBehaviour
{

    private Camera mainCam;
    private Vector3 mousePos;
    public GameObject Proiettile;
    public Transform TrasformazioneProiettile;
    public bool PuoSparare = true;
    private float timer; 
    public float TempoTraSpari;

    private Transform proiettileParent; 
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        mainCam = Camera.main;

        GameObject holder = GameObject.Find("ContenitoreProiettili");
    }

    // Update is called once per frame
    void Update()
    {
        mousePos = mainCam.ScreenToWorldPoint(Input.mousePosition);

        mousePos.z = 0;

        Vector3 rotation = mousePos - transform.position;
        
        float rotZ = Mathf.Atan2(rotation.y, rotation.x) * Mathf.Rad2Deg;
        
        transform.rotation = Quaternion.Euler(0, 0, rotZ);

        if(!PuoSparare)
        {
            timer += Time.deltaTime;
            if(timer > TempoTraSpari)
            {
                PuoSparare = true;      
                timer = 0;
            }
        }

        if(Input.GetMouseButtonDown(0) && PuoSparare)
        {
            PuoSparare = false; 
            Instantiate(Proiettile , TrasformazioneProiettile.position, transform.rotation, proiettileParent);
        }


    }
}
