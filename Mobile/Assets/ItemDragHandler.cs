using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.EventSystems;

public class ItemDragHandler : MonoBehaviour,IBeginDragHandler, IDragHandler, IEndDragHandler
{
    Transform originalParent;
    CanvasGroup canvasGroup;
    void Start()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }
    public void OnBeginDrag(PointerEventData eventData)
    {
        originalParent = transform.parent;
        transform.SetParent(transform.root);
        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.6f;
    }
    public void OnDrag(PointerEventData eventData)
    {
        transform.position = eventData.position;
    }
    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;

        Slot dropslot=eventData.pointerEnter?.GetComponent<Slot>();
        Slot originalSlot = originalParent.GetComponent<Slot>();
        if (dropslot != null)
        {
            if (dropslot.currentItem != null)
            {
                dropslot.currentItem.transform.SetParent(originalSlot.transform);
                originalSlot.currentItem = dropslot.currentItem;
                dropslot.currentItem.GetComponent<RectTransform>().anchoredPosition = Vector2.zero; // Center the item in the original slot
            }
            else
            {
                originalSlot.currentItem = null;
            }
            transform.SetParent(dropslot.transform);
            dropslot.currentItem = gameObject;
        }
        else
        {
            transform.SetParent(originalParent);
        }
        GetComponent<RectTransform>().anchoredPosition = Vector2.zero; // Center the item in its new slot
    }
}
