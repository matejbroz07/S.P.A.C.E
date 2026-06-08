using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class StorageUnit : MonoBehaviour, IItemContainer
{
    [Header("Storage Settings")]
    [SerializeField] private string storageName = "Storage";
    [SerializeField] private int slotCount = 10;
    private int currentCapacity;
    
    [Header("Current Items")]
    [SerializeField] private List<InventoryItem> storageItems = new List<InventoryItem>();

    private List<ItemSlotUI> uiSlots = new List<ItemSlotUI>();
    public static int OpenStorageCount = 0;
        
    public bool isOpen { get; private set; } = false;

    private void Start()
    {
        // Inicializace dat (items), ale NE generování grafiky
        for (int i = 0; i < slotCount; i++) storageItems.Add(null);
        
        currentCapacity = slotCount;
    }

    public void OpenStorage()
    {
        if (isOpen) return;
            
        if (StorageUIManager.Instance == null)
        {
            Debug.LogError("Chybí StorageUIManager ve scéně!");
            return;
        }
        
        // 1. Zavřít ostatní
        StorageUnit[] allChests = FindObjectsOfType<StorageUnit>();
        foreach (var chest in allChests)
        {
            if (chest != this && chest.isOpen)
            {
                chest.CloseStorage();
            }
        }
        
        // 2. Teprve TEĎ vygenerujeme sloty pro tuto konkrétní truhlu
        // Protože sdílíme jedno UI, musíme ho přebudovat podle toho, co otevíráme
        GenerateSlots(); 

        isOpen = true;
        OpenStorageCount++;

        if (StorageUIManager.Instance.storageNameText != null) StorageUIManager.Instance.storageNameText.text = storageName;
        if (StorageUIManager.Instance.storageUI) StorageUIManager.Instance.storageUI.SetActive(true);
        
        if (InventoryHandler.Instance != null && !InventoryHandler.Instance.IsInventoryOpen)
        {
            InventoryHandler.Instance.ToggleInventory();
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        RefreshUI();
    }

    public void CloseStorage()
    {
        if (!isOpen) return;

        isOpen = false;
        OpenStorageCount--; // Snížíme globální počítadlo
        
        if (StorageUIManager.Instance != null)
        {
            StorageUIManager.Instance.storageNameText.text = ""; // <--- Tady vymažeme text
            StorageUIManager.Instance.storageUI.SetActive(false);
        }
        
        // ZMĚNA 4: Zavřeme inventář hráče JEN TEHDY, pokud už není otevřená žádná jiná truhla
        // (Díky té logice v OpenStorage by to mělo být vždy 0, ale je to dobrá pojistka)
        if (OpenStorageCount <= 0 && InventoryHandler.Instance != null)
        {
            // Resetujeme počítadlo pro jistotu, kdyby se něco pokazilo
            OpenStorageCount = 0; 

            if (InventoryHandler.Instance.IsInventoryOpen)
            {
                InventoryHandler.Instance.ToggleInventory();
            }
            
            // Kurzor zamkneme jen když se zavře všechno
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void GenerateSlots()
    {
        if (StorageUIManager.Instance.slotsParent == null || StorageUIManager.Instance.slotPrefab == null) return;

        // DŮLEŽITÉ: Nejdřív vyčistíme seznam v kódu, abychom ztratili reference na staré objekty
        uiSlots.Clear();

        // Pak zničíme fyzické objekty v UI (staré sloty z předchozí truhly)
        // Použijeme dočasný list, abychom ničili bezpečně
        foreach (Transform child in StorageUIManager.Instance.slotsParent) 
        {
            Destroy(child.gameObject);
        }

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null && StorageUIManager.Instance.storageUI != null) canvas = StorageUIManager.Instance.storageUI.GetComponent<Canvas>();
        if (canvas == null) canvas = FindObjectOfType<Canvas>();

        // Vytvoříme nové sloty přesně podle počtu TÉTO truhly
        for (int i = 0; i < currentCapacity; i++)
        {
            GameObject newSlot = Instantiate(StorageUIManager.Instance.slotPrefab, StorageUIManager.Instance.slotsParent);
            ItemSlotUI slotUI = newSlot.GetComponent<ItemSlotUI>();
            
            slotUI.Initialize(this, i, canvas); 
            uiSlots.Add(slotUI);
        }
    }

    public void RefreshUI()
    {
        for (int i = 0; i < uiSlots.Count; i++)
        {
            if (i < storageItems.Count)
            {
                uiSlots[i].UpdateSlot(storageItems[i]);
            }
        }
    }

    public void SwapItems(int index1, int index2)
    {
        if (index1 == index2) return;
        InventoryItem temp = storageItems[index1];
        storageItems[index1] = storageItems[index2];
        storageItems[index2] = temp;
        RefreshUI();
    }

    public InventoryItem GetItem(int index)
    {
        if (index >= 0 && index < storageItems.Count) return storageItems[index];
        return null;
    }

    public void SetItem(int index, InventoryItem item)
    {
        if (index >= 0 && index < storageItems.Count)
        {
            storageItems[index] = item;
            RefreshUI();
        }
    }
    
    public void AddCapacity(int amount)
    {
        currentCapacity += amount;

        // OPRAVA 1: Musíme fyzicky přidat prázdná místa i do seznamu itemů
        for (int i = 0; i < amount; i++)
        {
            storageItems.Add(null);
        }

        Debug.Log($"[Truhla] Kapacita ZVÝŠENA o {amount}. Nová kapacita: {currentCapacity}");
        
        // OPRAVA 2: Pokud do truhly zrovna koukáme, musíme vygenerovat nové čtverečky
        if (isOpen)
        {
            GenerateSlots();
            RefreshUI();
        }
    }
    
    public void RemoveCapacity(int amount)
    {
        // OPRAVA 3: Smažeme sloty z konce seznamu itemů
        for (int i = 0; i < amount; i++)
        {
            if (storageItems.Count > 0)
            {
                // TODO v budoucnu: Pokud v 'storageItems[storageItems.Count - 1]' něco je,
                // měl bys ten item vyhodit z truhly na zem.
                storageItems.RemoveAt(storageItems.Count - 1);
            }
        }

        currentCapacity -= amount; // Snížíme maximum
        Debug.Log($"[Truhla] Kapacita SNÍŽENA o {amount}. Nová kapacita: {currentCapacity}");

        // Pokud do truhly zrovna koukáme, musíme smazat čtverečky
        if (isOpen)
        {
            GenerateSlots();
            RefreshUI();
        }
    }

    public int GetMaxSlots() => currentCapacity;

    public bool CanAddItem(InventoryItem item, int index) => true;

    private void Update()
    {
        if (isOpen)
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Tab))
            {
                CloseStorage();
            }
        }
    }
}
