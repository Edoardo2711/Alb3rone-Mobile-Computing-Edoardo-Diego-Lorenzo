using UnityEngine;
using UnityEngine.EventSystems;

public class ItemDragHandler : MonoBehaviour,IbeginDragHandler, IDragHandler, IEndDragHandler
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

        Slot dropslot=eventData.pointerEnter?.getComponent<Slot>();
        Slot originalSlot = originalParent.GetComponent<Slot>();
        if (dropslot != null)
        {
            if (dropslot.CanAcceptItem(originalSlot.Item))
            {
                dropslot.SetItem(originalSlot.Item);
                originalSlot.ClearItem();
            }
            else
            {
                transform.SetParent(originalParent);
                transform.localPosition = Vector3.zero;
            }
        }
        else
        {
            transform.SetParent(originalParent);
            transform.localPosition = Vector3.zero;
        }
    }
}
