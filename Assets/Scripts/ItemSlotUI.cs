using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class ItemSlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    [Header("UI References")]
    public Image iconImage;
    public TMP_Text countText;

    // ZMĚNA: Místo pevného InventoryHandleru používáme Interface
    private IItemContainer currentContainer; 
    private int slotIndex;
    
    private GameObject dragGhost; 
    private Canvas rootCanvas; 

    // Upravená Initialize, aby brala jakýkoliv kontejner (Inventory nebo Storage)
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

    public void UpdateSlot(InventoryItem item)
    {
        if (item != null)
        {
            iconImage.sprite = item.icon;
            iconImage.enabled = true;
            SetAlpha(1f);
            if (countText != null)
                countText.text = (item.stackable && item.quantity > 1) ? item.quantity.ToString() : "";
        }
        else
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
        // Kontrola přes interface
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

    // --- HLAVNÍ ZMĚNA ZDE ---
    public void OnDrop(PointerEventData eventData)
    {
        ItemSlotUI sourceSlot = eventData.pointerDrag?.GetComponent<ItemSlotUI>();
        
        if (sourceSlot != null)
        {
            // 1. Pokud jsme ve stejném okně (Inventář -> Inventář), použijeme starou metodu (SWAP)
            if (sourceSlot.currentContainer == this.currentContainer)
            {
                // Musíme přetypovat zpět na InventoryHandler pro vnitřní logiku, 
                // nebo StorageUnit, pokud to podporuje.
                if (currentContainer is InventoryHandler inv)
                {
                    inv.SwapItems(sourceSlot.slotIndex, this.slotIndex);
                }
                else if (currentContainer is StorageUnit storage)
                {
                    storage.SwapItems(sourceSlot.slotIndex, this.slotIndex);
                }
            }
            // 2. Pokud jsme v jiném okně (Inventář -> Storage nebo naopak)
            else
            {
                TransferItem(sourceSlot.currentContainer, sourceSlot.slotIndex, this.currentContainer, this.slotIndex);
            }
        }
    }

    // Logika pro přesun mezi kontejnery
    private void TransferItem(IItemContainer fromContainer, int fromIndex, IItemContainer toContainer, int toIndex)
    {
        InventoryItem itemFrom = fromContainer.GetItem(fromIndex);
        InventoryItem itemTo = toContainer.GetItem(toIndex);

        if (itemFrom == null) return;

        // A) Cílový slot je prázdný -> Prostý přesun
        if (itemTo == null)
        {
            toContainer.SetItem(toIndex, itemFrom);
            fromContainer.SetItem(fromIndex, null);
        }
        // B) Cílový slot má stejný item a je stackovatelný -> Sloučení
        else if (itemTo.itemName == itemFrom.itemName && itemTo.stackable)
        {
            int spaceLeft = itemTo.maxStackSize - itemTo.quantity;
            if (spaceLeft > 0)
            {
                int amountToMove = Mathf.Min(spaceLeft, itemFrom.quantity);
                itemTo.quantity += amountToMove;
                itemFrom.quantity -= amountToMove;

                toContainer.SetItem(toIndex, itemTo); // Aktualizace UI

                if (itemFrom.quantity <= 0)
                    fromContainer.SetItem(fromIndex, null);
                else
                    fromContainer.SetItem(fromIndex, itemFrom); // Aktualizace zbytku
            }
        }
        // C) Cílový slot má jiný item -> Prohození (Swap mezi kontejnery)
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
}