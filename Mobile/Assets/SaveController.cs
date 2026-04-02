using UnityEngine;
using Unity.Cinemachine; // <-- NOTA: In Cinemachine 3 si scrive così
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
        SaveData saveData = new SaveData
        {
            playerPosition = GameObject.FindGameObjectWithTag("Player").transform.position,
            // NOTA: m_BoundingShape2D è diventato BoundingShape2D
            mapBoundary = FindFirstObjectOfType<CinemachineConfiner2D>().BoundingShape2D.gameObject.name
        };

        Debug.Log("Save path: " + saveLocation);
        File.WriteAllText(saveLocation, JsonUtility.ToJson(saveData));
    }

    public void LoadGame()
    {
        if (File.Exists(saveLocation))
        {
            SaveData saveData = JsonUtility.FromJson<SaveData>(File.ReadAllText(saveLocation));

            GameObject.FindGameObjectWithTag("Player").transform.position = saveData.playerPosition;

            FindFirstObjectOfType<CinemachineConfiner2D>().BoundingShape2D = GameObject.Find(saveData.mapBoundary).GetComponent<PolygonCollider2D>();
        }
        else
        {
            SaveGame();
        }
    }
}