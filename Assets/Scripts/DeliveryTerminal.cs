using UnityEngine;
using UnityEngine.Events;
using TMPro;
using System.Collections.Generic;

// Třída pro jeden požadavek v questu
[System.Serializable]
public class DeliveryRequirement
{
    public string itemName;
    public Sprite itemIcon;
    public int requiredAmount;
    [HideInInspector] public int insertedAmount;
}

// Třída pro celý quest
[System.Serializable]
public class DeliveryQuest
{
    public string questName;
    public List<DeliveryRequirement> requirements;
}

public class DeliveryTerminal : MonoBehaviour
{
    [Header("Quests Data")]
    public List<DeliveryQuest> quests;
    private int currentQuestIndex = 0;

    [Header("World Space UI (Obrazovka)")]
    public TMP_Text questNameText;
    public List<DeliveryWorldSlot> worldSlots;

    [Header("UI Panel (Inventářové menu)")]
    public GameObject deliveryPanelUI; // Přetáhni sem ten UI panel, co se otevírá
    public bool isPanelOpen = false;

    [Header("Button (Tlačítko)")]
    public MeshRenderer buttonRenderer;
    public Material lockedMaterial;
    public Material readyMaterial;
    
    private bool isReadyToSubmit = false;

    void Start()
    {
        UpdateTerminalScreen();
        if (deliveryPanelUI != null) deliveryPanelUI.SetActive(false);
    }

    void Update()
    {
        // --- KLÍČOVÁ OPRAVA: Synchronizace s inventářem ---
        if (isPanelOpen)
        {
            // Pokud je panel otevřený, ale inventář už ne (hráč zmáčkl Tab), zavřeme i panel
            if (InventoryHandler.Instance != null && !InventoryHandler.Instance.IsInventoryOpen)
            {
                ClosePanel();
            }

            // Volitelně: Zavření pomocí Escape
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (InventoryHandler.Instance != null) InventoryHandler.Instance.IsInventoryOpen = false;
                ClosePanel();
            }
        }
    }

    // --- METODY PRO OTEVŘENÍ / ZAVŘENÍ (Volá InteractionHandler) ---
    public void OpenPanel()
    {
        isPanelOpen = true;

        // 1. NEJDŘÍV musíme zapnout rodiče (Inventář)
        if (InventoryHandler.Instance != null)
        {
            // Tímto se zapne hlavní UI inventáře (a odemkne myš)
            InventoryHandler.Instance.IsInventoryOpen = true; 
            // POZOR: Tady možná musíš zavolat i metodu, která ten tvůj inventář fyzicky ukáže, 
            // např. InventoryHandler.Instance.inventoryUI.SetActive(true); pokud to nedělá sám.
        }

        // 2. AŽ POTÉ zapneme našeho potomka (Delivery Panel)
        if (deliveryPanelUI != null) 
        {
            deliveryPanelUI.SetActive(true);
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ClosePanel()
    {
        isPanelOpen = false;
        
        // Vypneme našeho potomka
        if (deliveryPanelUI != null) 
        {
            deliveryPanelUI.SetActive(false);
        }
        
        // Zámek myši a vypnutí inventáře řeší pravděpodobně už sám InventoryHandler, 
        // ale necháme si tu pojistku, pokud by se to zavíralo Escape klávesou přímo z terminálu.
        if (InventoryHandler.Instance != null && !InventoryHandler.Instance.IsInventoryOpen)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public void UpdateTerminalScreen()
    {
        if (currentQuestIndex >= quests.Count)
        {
            questNameText.text = "ALL QUESTS COMPLETED";
            foreach (var slot in worldSlots) slot.SetupSlot(null, 0);
            SetButtonState(false);
            return;
        }

        DeliveryQuest currentQuest = quests[currentQuestIndex];
        questNameText.text = currentQuest.questName;

        bool allRequirementsMet = true;

        for (int i = 0; i < worldSlots.Count; i++)
        {
            if (i < currentQuest.requirements.Count)
            {
                var req = currentQuest.requirements[i];
                int amountLeft = req.requiredAmount - req.insertedAmount;
                worldSlots[i].SetupSlot(req.itemIcon, amountLeft);
                if (amountLeft > 0) allRequirementsMet = false;
            }
            else
            {
                worldSlots[i].SetupSlot(null, 0); 
            }
        }
        SetButtonState(allRequirementsMet);
    }

    public int TryInsertItem(string itemName, int amountToInsert)
    {
        if (currentQuestIndex >= quests.Count) return amountToInsert;

        DeliveryQuest currentQuest = quests[currentQuestIndex];
        foreach (var req in currentQuest.requirements)
        {
            if (req.itemName == itemName)
            {
                int amountNeeded = req.requiredAmount - req.insertedAmount;
                if (amountNeeded > 0)
                {
                    int amountTaken = Mathf.Min(amountToInsert, amountNeeded);
                    req.insertedAmount += amountTaken;
                    UpdateTerminalScreen();
                    return amountToInsert - amountTaken;
                }
            }
        }
        return amountToInsert;
    }

    private void SetButtonState(bool isReady)
    {
        isReadyToSubmit = isReady;
        if (buttonRenderer != null)
        {
            buttonRenderer.material = isReady ? readyMaterial : lockedMaterial;
        }
    }

    public void OnSubmitButtonPressed()
    {
        if (isReadyToSubmit)
        {
            currentQuestIndex++;
            
            // --- NOVÝ KÓD PRO CUTSCÉNU ---
            if (currentQuestIndex >= quests.Count)
            {
                // Pokud už žádné questy nejsou, zavřeme terminál a spustíme kino!
                ClosePanel(); 
                if (VictoryManager.Instance != null)
                {
                    VictoryManager.Instance.PlayVictorySequence();
                }
            }
            else
            {
                // Pokud questy ještě jsou, pokračujeme dál
                UpdateTerminalScreen();
            }
        }
    }
}