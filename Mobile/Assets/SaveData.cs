using UnityEngine;
using System.Collections.Generic;
using System.Collections;

[System.Serializable]
public class SaveData
{
    public Vector3 playerPosition;
    public string mapBoundary;
    public List<InventorySaveData> inventorySaveData;

    // Progressione della quest nascosta. Aggiunti dopo: JsonUtility ignora i campi
    // che nel file non ci sono, quindi un salvataggio vecchio si carica lo stesso
    // e questi restano a 0 / false.
    public int monete;
    public int pozioni;
    public bool spadaComprata;
    public bool bossUcciso;
    public bool oggettoPreso;
}
