using UnityEngine;
using Cinemachine;
using System.IO;

public class SaveController : MonoBehaviour
{
    private string saveLocation;

    void Start()
    {
        saveLocation = Path.Combine(Application.persistentDataPath, "saveData.json");
    }

    public void SaveGame()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

        // CONTROLLO DI SICUREZZA 1: Il Player è la cosa più importante
        if (playerObj == null)
        {
            Debug.LogError("ERRORE SALVATAGGIO: Il Player non è stato trovato! Controlla il tag 'Player'.");
            return;
        }

        // Cerchiamo il confiner, ma se non c'è non ci disperiamo
        CinemachineConfiner2D confiner = FindFirstObjectByType<CinemachineConfiner2D>();
        string boundaryName = "";

        if (confiner != null && confiner.m_BoundingShape2D != null)
        {
            boundaryName = confiner.m_BoundingShape2D.gameObject.name;
        }
        else
        {
            Debug.LogWarning("Nessun Confiner2D trovato o assegnato. Salvo solo la posizione del player.");
        }

        // Creiamo i dati
        SaveData saveData = new SaveData
        {
            playerPosition = playerObj.transform.position,
            mapBoundary = boundaryName
        };

        File.WriteAllText(saveLocation, JsonUtility.ToJson(saveData));
        Debug.Log("Gioco Salvato Correttamente in: " + saveLocation);
    }

    public void LoadGame()
    {
        if (File.Exists(saveLocation))
        {
            SaveData saveData = JsonUtility.FromJson<SaveData>(File.ReadAllText(saveLocation));

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                player.transform.position = saveData.playerPosition;
            }

            // Ripristiniamo i bordi SOLO se nel salvataggio c'era scritto un nome
            if (!string.IsNullOrEmpty(saveData.mapBoundary))
            {
                CinemachineConfiner2D confiner = FindFirstObjectByType<CinemachineConfiner2D>();
                GameObject boundaryObj = GameObject.Find(saveData.mapBoundary);

                if (confiner != null && boundaryObj != null)
                {
                    confiner.m_BoundingShape2D = boundaryObj.GetComponent<PolygonCollider2D>();
                }
            }
        }
        else
        {
            Debug.Log("Nessun file di salvataggio trovato.");
        }
    }
}