using UnityEngine;
using System.Collections.Generic;

public class StorageUnit : MonoBehaviour, IItemContainer
{
    [Header("Storage Settings")]
    [SerializeField] private string storageName = "Chest";
    [SerializeField] private int slotCount = 10;
    
    [Header("Current Items")]
    [SerializeField] private List<InventoryItem> storageItems = new List<InventoryItem>();

    [Header("UI References")]
    [SerializeField] private GameObject storageUI;      // Celý Canvas nebo Panel Storage
    [SerializeField] private Transform slotsParent;     // Kam se generují sloty (Grid Layout Group)
    [SerializeField] private GameObject slotPrefab;     // Stejný prefab jako v inventáři

    private List<ItemSlotUI> uiSlots = new List<ItemSlotUI>();
    private bool isOpen = false;

    private void Start()
    {
        // Inicializace prázdného listu
        for (int i = 0; i < slotCount; i++) storageItems.Add(null);
        
        if (storageUI) storageUI.SetActive(false);
        GenerateSlots();
    }

    public void OpenStorage()
    {
        if (isOpen) return;
        
        isOpen = true;
        if (storageUI) storageUI.SetActive(true);
        
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
        if (storageUI) storageUI.SetActive(false);
    
        if (InventoryHandler.Instance != null)
        {
            if (InventoryHandler.Instance.IsInventoryOpen)
            {
                InventoryHandler.Instance.ToggleInventory();
            }
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void GenerateSlots()
    {
        if (slotsParent == null || slotPrefab == null) return;

        // Vyčistit staré
        foreach (Transform child in slotsParent) Destroy(child.gameObject);
        uiSlots.Clear();

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null && storageUI != null) canvas = storageUI.GetComponent<Canvas>();
        if (canvas == null) canvas = FindObjectOfType<Canvas>(); // Fallback

        for (int i = 0; i < slotCount; i++)
        {
            GameObject newSlot = Instantiate(slotPrefab, slotsParent);
            ItemSlotUI slotUI = newSlot.GetComponent<ItemSlotUI>();
            
            // Důležité: Initialize s 'this' (tato StorageUnit)
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

    // --- Implementace IItemContainer ---
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

    public int GetMaxSlots() => slotCount;

    public bool CanAddItem(InventoryItem item, int index) => true;

    // Metoda pro Update, kontrola vzdálenosti nebo zavření klávesou
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
