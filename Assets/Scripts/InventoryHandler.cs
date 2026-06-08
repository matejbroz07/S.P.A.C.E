using System.Collections.Generic;
using UnityEngine;

public class InventoryHandler : MonoBehaviour, IItemContainer
{
    public static InventoryHandler Instance { get; private set; } 

    [Header("Inventory Settings")]
    [SerializeField] private int maxSlots = 20;

    [Header("Current Inventory")]
    [SerializeField] private List<InventoryItem> items = new List<InventoryItem>(); 

    [Header("UI Settings")]
    [SerializeField] private GameObject inventoryCanvas; 
    [SerializeField] private GameObject inventoryPanel; 
    [SerializeField] private GameObject itemSlotPrefab; 
    
    [SerializeField] private GameObject panelTerminal;
    
    private List<ItemSlotUI> slotUIs = new List<ItemSlotUI>(); 
    public bool IsInventoryOpen { get; set; } 

    public delegate void OnInventoryChanged();      
    public event OnInventoryChanged InventoryChanged;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (inventoryCanvas != null) inventoryCanvas.SetActive(false); 

        items.Clear();
        for (int i = 0; i < maxSlots; i++) items.Add(null); 

        GenerateSlots();
        InventoryChanged += UpdateInventoryUI; 
    }

    private void Update()
    {
        if (StorageUnit.OpenStorageCount > 0) return;
        
        if (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.Escape)) 
        {
            ToggleInventory();
        }
    }

    public void ToggleInventory()
    {
        IsInventoryOpen = !IsInventoryOpen; 
        
        if (inventoryCanvas != null)
            inventoryCanvas.SetActive(IsInventoryOpen); 

        if (IsInventoryOpen)
        {
            Cursor.lockState = CursorLockMode.None; 
            Cursor.visible = true; 
            InventoryChanged?.Invoke();
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void GenerateSlots()
    {
        if (inventoryPanel == null || itemSlotPrefab == null) return; 
        
        Canvas canvasComp = inventoryCanvas != null ? inventoryCanvas.GetComponent<Canvas>() : inventoryPanel.GetComponentInParent<Canvas>();

        foreach (Transform child in inventoryPanel.transform)
            Destroy(child.gameObject); 
        
        slotUIs.Clear(); 

        for (int i = 0; i < maxSlots; i++)
        {
            GameObject slotObj = Instantiate(itemSlotPrefab, inventoryPanel.transform); 
            ItemSlotUI slotUI = slotObj.GetComponent<ItemSlotUI>(); 
            if (slotUI == null) slotUI = slotObj.AddComponent<ItemSlotUI>(); 
            
            slotUI.Initialize(this, i, canvasComp); 
            slotUIs.Add(slotUI); 
        }
    }
    
    private void UpdateInventoryUI()
    {
        // OPRAVA: Zrušena blokace přes if (!IsInventoryOpen). 
        // Vždy, když se změní data, bezpečně překreslíme sloty.
        for (int i = 0; i < slotUIs.Count; i++)
        {
            if (i < items.Count) slotUIs[i].UpdateSlot(items[i]); 
        }
    }

    public InventoryItem GetItem(int index)
    {
        if (index >= 0 && index < items.Count) return items[index];
        return null;
    }

    public void SetItem(int index, InventoryItem item)
    {
        if (index >= 0 && index < items.Count)
        {
            items[index] = item;
            InventoryChanged?.Invoke(); 
        }
    }

    public int GetMaxSlots() => maxSlots;

    public bool CanAddItem(InventoryItem item, int index) => true;

    public int AddItem(string itemName, Sprite icon, int quantity, bool stackable = true, int maxStackSize = 99)
    {
        if (quantity <= 0) return 0; 
        int remainingQuantity = quantity;

        // 1. FÁZE: Zkusíme to nacpat do existujících stacků
        if (stackable)
        {
            foreach (InventoryItem existing in items)
            {
                if (existing != null && existing.itemName == itemName && existing.stackable)
                {
                    int availableSpace = existing.maxStackSize - existing.quantity;
                    if (availableSpace > 0)
                    {
                        int toAdd = Mathf.Min(remainingQuantity, availableSpace);
                        existing.quantity += toAdd;
                        remainingQuantity -= toAdd;
                        if (remainingQuantity <= 0) break;
                    }
                }
            }
        }
        
        // 2. FÁZE: Hledáme prázdný slot
        if (remainingQuantity > 0)
        {
            for (int i = 0; i < maxSlots && remainingQuantity > 0; i++)
            {
                // OPRAVA: Mnohem chytřejší detekce "prázdného" slotu!
                if (items[i] == null || string.IsNullOrEmpty(items[i].itemName) || items[i].quantity <= 0)
                {
                    int newStackAmount = Mathf.Min(remainingQuantity, maxStackSize);
                    items[i] = new InventoryItem(itemName, icon, newStackAmount, stackable, maxStackSize);
                    remainingQuantity -= newStackAmount;
                }
            }
        }
        
        // 3. FÁZE: Férová notifikace
        // Spočítáme, kolik předmětů jsme REÁLNĚ uložili do batohu
        int actuallyAdded = quantity - remainingQuantity;
        
        // Zobrazíme notifikaci pouze tehdy, pokud jsme opravdu něco sebrali
        if (actuallyAdded > 0)
        {
            NotificationManager.Instance.ShowNotification(itemName, actuallyAdded, Color.white);
            InventoryChanged?.Invoke();
        }

        return remainingQuantity;
    }

    public bool HasItemAtIndex(int index) => index >= 0 && index < items.Count && items[index] != null;
    
    public void SwapItems(int index1, int index2)
    {
        if (index1 < 0 || index1 >= maxSlots || index2 < 0 || index2 >= maxSlots || index1 == index2) return;

        InventoryItem item1 = items[index1]; 
        InventoryItem item2 = items[index2];
        
        if (item1 != null && item2 != null && item1.itemName == item2.itemName && item1.stackable && item2.stackable)
        {
            int total = item1.quantity + item2.quantity;
            if (total <= item1.maxStackSize)
            {
                item2.quantity = total;
                items[index1] = null;
            }
            else
            {
                item2.quantity = item1.maxStackSize;
                item1.quantity = total - item1.maxStackSize;
            }
        }
        else
        {
            items[index1] = item2;
            items[index2] = item1;
        }
        
        InventoryChanged?.Invoke();
    }
    
    // Zjistí, kolik dané suroviny hráč celkem má
    public int GetTotalItemCount(string itemName)
    {
        int total = 0;
        foreach (InventoryItem item in items)
        {
            if (item != null && item.itemName == itemName)
            {
                total += item.quantity;
            }
        }
        return total;
    }

    // Odebere přesný počet surovin z inventáře
    public void RemoveItems(string itemName, int amountToRemove)
    {
        int remainingToRemove = amountToRemove;

        for (int i = 0; i < maxSlots; i++)
        {
            if (items[i] != null && items[i].itemName == itemName)
            {
                if (items[i].quantity > remainingToRemove)
                {
                    items[i].quantity -= remainingToRemove;
                    remainingToRemove = 0;
                    break;
                }
                else
                {
                    remainingToRemove -= items[i].quantity;
                    items[i] = null; // Slot se vyprázdní
                }
            }
        }
        InventoryChanged?.Invoke();
    }
}