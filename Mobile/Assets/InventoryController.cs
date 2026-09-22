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

        [Tooltip("ID dell'oggetto Pozione (Pozione.prefab). Le pozioni si contano in ProgressoGioco: " +
                 "qui compare solo la loro icona, col numero, finche' ne hai almeno una.")]
        public int idPozione = 2;

        private ProgressoGioco progresso;

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

            progresso = ProgressoGioco.Instance;
            if (progresso != null)
            {
                progresso.OnPozioniCambiate += MostraPozioni;
                MostraPozioni(progresso.pozioni);
            }
        }

        void OnDestroy()
        {
            if (progresso != null) progresso.OnPozioniCambiate -= MostraPozioni;
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
                // La pozione no: il numero e' gia' salvato in ProgressoGioco, e al caricamento
                // MostraPozioni la rimette da se'. Salvandola comparirebbe doppia.
                if (item.ID == idPozione) continue;
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

    // Gli slot sono appena stati rifatti: la pozione, che non e' nel salvataggio, va rimessa.
    ProgressoGioco p = progresso != null ? progresso : ProgressoGioco.Instance;
    if (p != null) MostraPozioni(p.pozioni);
}
        
    /// <summary>
    /// Tiene l'icona della pozione allineata al contatore di ProgressoGioco: compare con la
    /// prima pozione, mostra quante ne hai e sparisce quando finiscono.
    /// </summary>
    void MostraPozioni(int quante)
    {
        if (inventoryPanel == null) return;

        GameObject pozione = TrovaOggetto(idPozione);
        if (quante <= 0)
        {
            if (pozione != null) RimuoviOggetto(idPozione);
            return;
        }

        if (pozione == null)
        {
            if (itemDictionary == null) itemDictionary = FindFirstObjectByType<ItemDictionary>();
            GameObject prefab = itemDictionary != null ? itemDictionary.GetItemPrefab(idPozione) : null;
            if (prefab == null || !AggiungiOggetto(prefab)) return;
            pozione = TrovaOggetto(idPozione);
            if (pozione == null) return;
        }

        TMPro.TMP_Text numero = pozione.GetComponentInChildren<TMPro.TMP_Text>(true);
        if (numero == null) numero = CreaNumero(pozione.transform);
        numero.text = quante.ToString();
    }

    GameObject TrovaOggetto(int itemID)
    {
        foreach (Transform slotTransform in inventoryPanel.transform)
        {
            Slot slot = slotTransform.GetComponent<Slot>();
            if (slot == null || slot.currentItem == null) continue;
            Item item = slot.currentItem.GetComponent<Item>();
            if (item != null && item.ID == itemID) return slot.currentItem;
        }
        return null;
    }

    /// <summary>Il numero in basso a destra dell'icona, con lo stesso font del contatore nell'HUD.</summary>
    TMPro.TMP_Text CreaNumero(Transform icona)
    {
        GameObject go = new GameObject("Quantita", typeof(RectTransform));
        go.transform.SetParent(icona, false);
        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        // Spostato un po' fuori dall'icona, verso l'angolo dello slot: sopra la pozione rossa
        // il numero sparirebbe.
        rt.offsetMin = new Vector2(0f, -22f);
        rt.offsetMax = new Vector2(22f, 0f);
        go.layer = icona.gameObject.layer;

        TMPro.TextMeshProUGUI testo = go.AddComponent<TMPro.TextMeshProUGUI>();
        testo.alignment = TMPro.TextAlignmentOptions.BottomRight;
        testo.fontSize = 56f;
        // Marrone scuro come le scritte delle linguette: bianco sparirebbe sul fondo chiaro del pannello.
        testo.color = new Color(0.24f, 0.16f, 0.09f);
        // Senza, il numero intercetterebbe il tocco e l'icona non si potrebbe piu' trascinare.
        testo.raycastTarget = false;

        UsoPozione hud = FindFirstObjectByType<UsoPozione>(FindObjectsInactive.Include);
        // Font e materiale presi entrambi dal contatore: il numero ha lo stesso aspetto. Niente
        // outlineWidth: il pannello di solito e' spento, il testo non ha ancora un materiale e va in errore.
        if (hud != null && hud.contatore != null)
        {
            testo.font = hud.contatore.font;
            testo.fontSharedMaterial = hud.contatore.fontSharedMaterial;
        }
        return testo;
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
