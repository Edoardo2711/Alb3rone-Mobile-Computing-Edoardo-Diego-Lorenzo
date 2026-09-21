    using UnityEngine;
    using UnityEngine.UI;
    using System.Collections.Generic;
    using System.Collections;

    public class InventoryController : MonoBehaviour
    {
        private ItemDictionary itemDictionary;
        public GameObject inventoryPanel; 
        public GameObject slotPrefab;
        public int slotCount; 
        public GameObject[] ItemPrefabs;
        void Start()
        {
            itemDictionary = FindObjectOfType<ItemDictionary>();
        
            for (int i = 0; i < slotCount; i++)
            {
                Slot slot = Instantiate(slotPrefab, inventoryPanel.transform).GetComponent<Slot>();
                if (i < ItemPrefabs.Length)
                {
                    GameObject item = Instantiate(ItemPrefabs[i], slot.transform);
                    item.GetComponent<RectTransform>().anchoredPosition = Vector2.zero; // Center the item in the slot
                    slot.currentItem = item; // Assign the item to the slot
                }
            }
        }

    public List<InventorySaveData> GetInventoryItems()
    {
        List<InventorySaveData> invData = new List<InventorySaveData>();
        foreach (Transform slotTransform in inventoryPanel.transform)
        {
            Slot slot = slotTransform.GetComponent<Slot>();
            // Un figlio senza Slot (es. un oggetto lasciato fuori dagli slot) non va salvato
            if(slot != null && slot.currentItem != null)
            {
                Item item = slot.currentItem.GetComponent<Item>();
                invData.Add(new InventorySaveData{itemID = item.ID, slotIndex = slotTransform.GetSiblingIndex()});
            }
        }
        return invData;
    }
    public void SetInventoryItems(List<InventorySaveData> inventorySaveData)
{
    //Clear inventory panel - avoid duplicates
    // Destroy() agisce solo a fine frame: senza staccarli, i vecchi slot restano figli
    // del pannello e GetChild() qui sotto ci metterebbe dentro gli oggetti caricati,
    // che sparirebbero insieme a loro. Si scorre al contrario perche' si stacca mentre si itera.
    for(int i = inventoryPanel.transform.childCount - 1; i >= 0; i--)
    {
        GameObject child = inventoryPanel.transform.GetChild(i).gameObject;
        child.SetActive(false);
        child.transform.SetParent(null, false);
        Destroy(child);
    }

    //Create new slots
    for(int i = 0; i < slotCount; i++)
    {
        Instantiate(slotPrefab, inventoryPanel.transform);
    }

    //Populate slots with saved items
    foreach(InventorySaveData data in inventorySaveData)
    {
        if(data.slotIndex < slotCount)
        {
            Slot slot = inventoryPanel.transform.GetChild(data.slotIndex).GetComponent<Slot>();
            GameObject itemPrefab = itemDictionary.GetItemPrefab(data.itemID);
            if(itemPrefab != null)
            {
                GameObject item = Instantiate(itemPrefab, slot.transform);
                item.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
                slot.currentItem = item;
            }
        }
    }
}
        
    /// <summary>Mette una copia del prefab nel primo slot libero. Torna false se l'inventario e' pieno.</summary>
    public bool AggiungiOggetto(GameObject itemPrefab)
    {
        if (itemPrefab == null || inventoryPanel == null) return false;
        foreach (Transform slotTransform in inventoryPanel.transform)
        {
            Slot slot = slotTransform.GetComponent<Slot>();
            if (slot == null || slot.currentItem != null) continue;
            GameObject item = Instantiate(itemPrefab, slot.transform);
            item.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            slot.currentItem = item;
            return true;
        }
        Debug.LogWarning("[InventoryController] Inventario pieno: " + itemPrefab.name + " non aggiunto.");
        return false;
    }

    /// <summary>Toglie il primo oggetto con quell'ID. Torna false se non c'era.</summary>
    public bool RimuoviOggetto(int itemID)
    {
        if (inventoryPanel == null) return false;
        foreach (Transform slotTransform in inventoryPanel.transform)
        {
            Slot slot = slotTransform.GetComponent<Slot>();
            if (slot == null || slot.currentItem == null) continue;
            Item item = slot.currentItem.GetComponent<Item>();
            if (item == null || item.ID != itemID) continue;
            Destroy(slot.currentItem);
            slot.currentItem = null;
            return true;
        }
        return false;
    }
}
