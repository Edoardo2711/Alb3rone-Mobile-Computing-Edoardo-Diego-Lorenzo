using UnityEngine;
using Cinemachine;
using System.IO;
using System.Collections.Generic;
using System.Collections;

[System.Serializable]
public class SaveController : MonoBehaviour
{
    public static string SavePath => Path.Combine(Application.persistentDataPath, "saveData.json");

    public static bool HasSave => File.Exists(SavePath);

    // Lo imposta il menu con "Continua" e lo consuma Start(): senza, anche
    // "Nuova Partita" ricaricherebbe il salvataggio. Statico perche' deve
    // sopravvivere al cambio di scena.
    public static bool LoadOnNextStart;

    private string saveLocation;
    private InventoryController inventoryController;
    private ProgressoGioco progresso;

    void Start()
    {
        saveLocation = SavePath;
        inventoryController = FindObjectOfType<InventoryController>();
        // Instance e' gia' pronto: lo assegna Awake, che gira prima di tutti gli Start.
        progresso = ProgressoGioco.Instance;

        if (LoadOnNextStart)
        {
            LoadOnNextStart = false;
            LoadGame();
        }
    }

    public void SaveGame()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

        // CONTROLLO DI SICUREZZA 1: Il Player � la cosa pi� importante
        if (playerObj == null)
        {
            Debug.LogError("ERRORE SALVATAGGIO: Il Player non � stato trovato! Controlla il tag 'Player'.");
            return;
        }

        // Cerchiamo il confiner, ma se non c'� non ci disperiamo
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
            mapBoundary = boundaryName,
            // Il controller puo' mancare: FindObjectOfType non trova i componenti
            // su GameObject disattivati, e l'inventario non e' in tutte le scene.
            inventorySaveData = inventoryController != null
                ? inventoryController.GetInventoryItems()
                : new List<InventorySaveData>(),

            // Il ProgressoGioco puo' mancare (scene senza GameManager): in quel caso
            // si salvano i valori di partenza invece di fermare tutto il salvataggio.
            monete         = progresso != null ? progresso.monete : 0,
            spadaComprata  = progresso != null && progresso.spadaComprata,
            bossUcciso     = progresso != null && progresso.bossUcciso,
            oggettoPreso   = progresso != null && progresso.oggettoPreso
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

            // Un salvataggio creato prima dell'inventario non ha la lista: saltiamo.
            if (inventoryController != null && saveData.inventorySaveData != null)
                inventoryController.SetInventoryItems(saveData.inventorySaveData);

            // Nei salvataggi fatti prima della quest questi campi non ci sono:
            // JsonUtility li lascia a 0 / false, che e' esattamente "quest mai iniziata".
            if (progresso != null)
                progresso.Applica(saveData.monete, saveData.spadaComprata,
                                  saveData.bossUcciso, saveData.oggettoPreso);
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