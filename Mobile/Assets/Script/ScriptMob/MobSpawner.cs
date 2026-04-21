using UnityEngine;

// Definiamo la nostra "ricetta" per i mob
[System.Serializable]
public class MobType
{
    public string name; 
    public GameObject prefab; 
    public int amountToSpawn; 
}

public class MobSpawner : MonoBehaviour
{
    [Header("Impostazioni Gruppi Mob")]
    public Transform playerTransform; 
    public MobType[] mobsToSpawn; 

    [Header("Area di Spawn (Poligono)")]
    // Qui inseriremo il nostro PolygonCollider2D
    public PolygonCollider2D spawnArea; 

    [Header("Evitare le Collisioni")]
    public LayerMask obstacleLayer; 
    public float mobRadiusCheck = 0.5f; 
    public int maxAttemptsPerMob = 30; // Alzato a 30 perché la forma irregolare richiede più tentativi

    void Start()
    {
        // Controllo di sicurezza vitale per evitare errori NullReference
        if (spawnArea == null)
        {
            Debug.LogError("Attenzione: Non hai assegnato il PolygonCollider2D allo script MobSpawner!");
            return;
        }

        SpawnAllMobs();
    }

    void SpawnAllMobs()
    {
        // Otteniamo i "Bounds" (i limiti del rettangolo immaginario che racchiude tutto il poligono)
        Bounds bounds = spawnArea.bounds;

        foreach (MobType currentMobType in mobsToSpawn)
        {
            for (int i = 0; i < currentMobType.amountToSpawn; i++)
            {
                bool hasSpawned = false;
                int attempts = 0;

                while (!hasSpawned && attempts < maxAttemptsPerMob)
                {
                    // 1. Genera un punto a caso dentro i limiti (bounds)
                    float randomX = Random.Range(bounds.min.x, bounds.max.x);
                    float randomY = Random.Range(bounds.min.y, bounds.max.y);
                    Vector2 randomPosition = new Vector2(randomX, randomY);

                    // 2. MAGIA DEL POLIGONO: Controlla se il punto estratto è effettivamente DENTRO la forma irregolare
                    if (spawnArea.OverlapPoint(randomPosition))
                    {
                        // 3. Controlla se ci sono ostacoli (come i muri sulla Tilemap)
                        Collider2D hit = Physics2D.OverlapCircle(randomPosition, mobRadiusCheck, obstacleLayer);

                        // Se non ci sono ostacoli, fai nascere il mob!
                        if (hit == null)
                        {
                            GameObject newMob = Instantiate(currentMobType.prefab, randomPosition, Quaternion.identity);
                            
                            // Assegna il player al nuovo mob
                            if (playerTransform != null)
                            {
                                MobController controller = newMob.GetComponent<MobController>();
                                if (controller != null)
                                {
                                    controller.player = playerTransform;
                                }
                            }

                            hasSpawned = true; // Mob generato con successo, usciamo dal ciclo while
                        }
                    }
                    
                    attempts++;
                }

                if (!hasSpawned)
                {
                    Debug.LogWarning("Non ho trovato spazio per il mob: " + currentMobType.name + " dopo " + maxAttemptsPerMob + " tentativi.");
                }
            }
        }
    }
}