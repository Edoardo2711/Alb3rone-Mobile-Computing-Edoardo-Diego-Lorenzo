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
        
           //* for (int i = 0; i < slotCount; i++)
            //{
           // Slot slot = Instantiate(slotPrefab, inventoryPanel.transform).GetComponent<Slot>();
           // if (i < ItemPrefabs.Length)
           // {
           //         GameObject item = Instantiate(ItemPrefabs[i],   slot.transform);
           //         item.GetComponent<RectTransform>().anchoredPosition = Vector2.zero; // Center the item in the slot
           //         slot.currentItem = item; // Assign the item to the slot
           // }
            //}
        }
        void Update() {
    if (Input.GetKeyDown(KeyCode.I)) { // O il tasto che preferisci
        inventoryPanel.SetActive(!inventoryPanel.activeSelf);
    }

    public List<InventorySaveData> GetInventoryItems()
    {
        List<InventorySaveData> invData = new List<InventorySaveData>();
        foreach (Transform slotTransform in inventoryPanel.transform)
        {
            slot slot = slotTransform.GetComponent<slot>();
            if(slot.currentItem != null)
            {
                Item itemComponent = slot.currentItem.GetComponent<Item>();
                invData.Add(new InventorySaveData(item = item.ID, slotIndex = slotTransform.GetSiblingIndex()));
            }
        }
        return invData;
    }
    
}