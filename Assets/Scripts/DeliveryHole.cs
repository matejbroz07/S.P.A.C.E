using UnityEngine;

public class DeliveryHole : MonoBehaviour
{
    [Header("Nastavení Interakce")]
    public string hoverText = "Open"; // Text, co se ukáže na obrazovce
    
    [Header("UI Reference")]
    // DŮLEŽITÉ: Zde v Inspektoru přetáhni svůj Delivery Panel! (Bez GameObject.Find)
    public GameObject deliveryUI; 

    private bool isPanelOpen = false;

    void Start()
    {
        // Pojistka: Na začátku hry panel určitě schováme
        if (deliveryUI != null) deliveryUI.SetActive(false);
    }

    void Update()
    {
        // Hlídáme to JEN, když je panel zrovna otevřený
        if (isPanelOpen)
        {
            // 1. POJISTKA PRO TAB: 
            // Pokud hráč zavřel inventář (IsInventoryOpen je false), zavřeme i náš panel
            if (InventoryHandler.Instance != null && !InventoryHandler.Instance.IsInventoryOpen)
            {
                ClosePanel();
            }

            // 2. POJISTKA PRO ESCAPE: 
            // Pokud hráč zmáčkne Escape, vypneme oboje natvrdo
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (InventoryHandler.Instance != null) 
                    InventoryHandler.Instance.IsInventoryOpen = false;
                
                InventoryHandler.Instance.ToggleInventory();
                
                ClosePanel();
            }
        }
    }

    // Tuto metodu zavoláme paprskem z hráče
    public void Interact()
    {
        if (isPanelOpen) return; // Zabráníme dvojitému spuštění

        isPanelOpen = true;

        // 1. NEJDŘÍV otevřeme normální inventář hráče (Rodič)
        if (InventoryHandler.Instance != null && !InventoryHandler.Instance.IsInventoryOpen)
        {
            InventoryHandler.Instance.ToggleInventory();
        }

        // 2. AŽ POTÉ zapneme tvůj speciální panel (Potomek)
        if (deliveryUI != null) deliveryUI.SetActive(true);

        // 3. Odemkneme myš pro přetahování
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // Naše nová metoda, která to všechno čistě uklidí
    public void ClosePanel()
    {
        isPanelOpen = false;
        
        // Fyzicky vypneme UI
        if (deliveryUI != null) deliveryUI.SetActive(false);

        // Zamkneme myš zpět do hry (pojistka, kdyby to InventoryHandler nestihl udělat sám)
        if (InventoryHandler.Instance != null && !InventoryHandler.Instance.IsInventoryOpen)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}