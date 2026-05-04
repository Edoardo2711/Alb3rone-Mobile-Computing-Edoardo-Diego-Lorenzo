using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

public class InventoryController : MonoBehaviour
{
    public GameObject inventoryPanel; 
    public GameObject slotPrefab;
    public int slotCount; 
    public GameObject[] ItemPrefabs;
    void Start()
    {
        for (int i = 0; i < slotCount; i++)
        {
           Slot slot = Instantiate(slotPrefab, inventoryPanel.transform).GetComponent<Slot>();
           if (i < ItemPrefabs.Length)
           {
                GameObject item = Instantiate(ItemPrefabs[i],   slot.transform);
                item.GetComponent<RectTransform>().anchoredPosition = Vector2.zero; // Center the item in the slot
                slot.currentItem = item; // Assign the item to the slot
           }
        }
    }
}
