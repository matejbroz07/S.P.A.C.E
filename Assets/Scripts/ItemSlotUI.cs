using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class ItemSlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    [Header("UI References")]
    public Image iconImage;
    public TMP_Text countText;

    private IItemContainer currentContainer; 
    private int slotIndex;
    
    private GameObject dragGhost; 
    private Canvas rootCanvas; 

    // Do jakého inventáře patří (container), na jakém místě (index), a jeho root canvas (canvas)
    public void Initialize(IItemContainer container, int index, Canvas canvas)
    {
        this.currentContainer = container;
        this.slotIndex = index;
        this.rootCanvas = canvas;

        if (rootCanvas == null)
        {
            rootCanvas = GetComponentInParent<Canvas>();
            if (rootCanvas == null) rootCanvas = FindObjectOfType<Canvas>();
        }

        if (iconImage == null)
            iconImage = transform.Find("Icon")?.GetComponent<Image>();

        if (countText == null)
            countText = GetComponentInChildren<TMP_Text>();
    }
    
    // Tato funkce mění vzhled slotu podle dostupných informací
    public void UpdateSlot(InventoryItem item)
    {
        if (item != null) // Pokud v něm item je ukážeme informace o itemu
        {
            iconImage.sprite = item.icon;
            iconImage.enabled = true;
            SetAlpha(1f);
            if (countText != null)
                countText.text = (item.stackable && item.quantity > 1) ? item.quantity.ToString() : "";
        }
        else // Pokud v něm není item nic neukazujem 
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
            SetAlpha(0f);
            if (countText != null) countText.text = "";
        }
    }

    private void SetAlpha(float alpha)
    {
        if (iconImage != null)
        {
            Color c = iconImage.color;
            c.a = alpha;
            iconImage.color = c;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (currentContainer.GetItem(slotIndex) == null) return;
        
        if (rootCanvas == null) return; 

        SetAlpha(0.4f);
        CreateDragGhost();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragGhost != null)
        {
            dragGhost.transform.position = eventData.position;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (dragGhost != null) Destroy(dragGhost);
        SetAlpha(1f);
    }

    public void OnDrop(PointerEventData eventData)
    {
        ItemSlotUI sourceSlot = eventData.pointerDrag?.GetComponent<ItemSlotUI>();
        
        if (sourceSlot != null)
        {
            if (sourceSlot.currentContainer == this.currentContainer)
            {
                if (currentContainer is InventoryHandler inv)
                {
                    inv.SwapItems(sourceSlot.slotIndex, this.slotIndex);
                }
                else if (currentContainer is StorageUnit storage)
                {
                    storage.SwapItems(sourceSlot.slotIndex, this.slotIndex);
                }
            }
            else
            {
                TransferItem(sourceSlot.currentContainer, sourceSlot.slotIndex, this.currentContainer, this.slotIndex);
            }
        }
    }

    private void TransferItem(IItemContainer fromContainer, int fromIndex, IItemContainer toContainer, int toIndex)
    {
        InventoryItem itemFrom = fromContainer.GetItem(fromIndex);
        InventoryItem itemTo = toContainer.GetItem(toIndex);

        if (itemFrom == null) return;

        if (itemTo == null)
        {
            toContainer.SetItem(toIndex, itemFrom);
            fromContainer.SetItem(fromIndex, null);
        }
        else if (itemTo.itemName == itemFrom.itemName && itemTo.stackable)
        {
            int spaceLeft = itemTo.maxStackSize - itemTo.quantity;
            if (spaceLeft > 0)
            {
                int amountToMove = Mathf.Min(spaceLeft, itemFrom.quantity);
                itemTo.quantity += amountToMove;
                itemFrom.quantity -= amountToMove;

                toContainer.SetItem(toIndex, itemTo);

                if (itemFrom.quantity <= 0)
                    fromContainer.SetItem(fromIndex, null);
                else
                    fromContainer.SetItem(fromIndex, itemFrom);
            }
        }
        else
        {
            toContainer.SetItem(toIndex, itemFrom);
            fromContainer.SetItem(fromIndex, itemTo);
        }
    }

    private void CreateDragGhost()
    {
        dragGhost = new GameObject("DragIcon_Ghost");
    
        dragGhost.transform.SetParent(rootCanvas.transform, false);
        dragGhost.transform.SetAsLastSibling();

        Canvas ghostCanvas = dragGhost.AddComponent<Canvas>();
        ghostCanvas.overrideSorting = true;
        ghostCanvas.sortingOrder = 999; 

        dragGhost.AddComponent<GraphicRaycaster>();

        Image ghostImage = dragGhost.AddComponent<Image>();
        ghostImage.sprite = iconImage.sprite;
        ghostImage.raycastTarget = false;

        RectTransform ghostRect = dragGhost.GetComponent<RectTransform>();
        RectTransform sourceRect = GetComponent<RectTransform>();
        ghostRect.sizeDelta = sourceRect.sizeDelta;
    
        dragGhost.transform.position = Input.mousePosition;
    }
    
    public InventoryItem GetItem()
    {
        if (currentContainer == null) return null;
        return currentContainer.GetItem(slotIndex);
    }

    public void SetItem(InventoryItem item)
    {
        if (currentContainer == null) return;
        currentContainer.SetItem(slotIndex, item);
    }
}