using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InteractionHandler : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float interactionDistance = 3f;
    [SerializeField] private LayerMask itemLayer; // Ujisti se, že Tlačítko, Díra, STAVBA, BARVY i WORKBENCH jsou na stejné vrstvě!
    [SerializeField] private float cleanupDelay = 0.1f;
    private float currentCleanupTimer;

    [Header("UI References")]
    [SerializeField] private GameObject interactionUI;
    [SerializeField] private TMP_Text interactionItemName;
    [SerializeField] private TMP_Text interactionTextUI;
    [SerializeField] private TMP_Text interactionKeyUI;
    [SerializeField] private Image crosshair;
    
    [Header("Visuals")]
    [SerializeField] private float normalCrosshairAlpha = 0.3f;
    [SerializeField] private float highlightCrosshairAlpha = 1f;

    private Camera mainCamera;
    
    // Na co zrovna koukáme
    private PickupItem currentTargetItem; 
    private StorageUnit currentTargetStorage;
    private DeliveryHole currentDeliveryHole;     
    private DeliveryButton currentDeliveryButton; 
    private BuildingColorInteract currentBuildingColorStation; 
    private Workbench currentWorkbench; // <--- NOVÉ PRO WORKBENCH
    
    private bool isHovering;

    private void Start()
    {
        mainCamera = Camera.main;
        if (interactionUI) interactionUI.SetActive(false);
        SetCrosshairAlpha(normalCrosshairAlpha);
    }

    private void Update()
    {
        // UNIVERZÁLNÍ POJISTKA: Myš je odemčená = jsme v menu
        if (Cursor.lockState == CursorLockMode.None)
        {
            if (isHovering) ClearInteraction(); // Vymaže texty z obrazovky
            return; // Zabrání střílení paprsků
        }

        // Původní pojistka inventáře
        if (InventoryHandler.Instance != null && InventoryHandler.Instance.IsInventoryOpen)
        {
            if (isHovering) ClearInteraction();
            return;
        }

        HandleRaycast();
        HandleInput();
    }

    private void HandleRaycast()
    {
        Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0)); 
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactionDistance, itemLayer))
        {
            PickupItem item = hit.collider.GetComponentInParent<PickupItem>();
            StorageUnit storage = hit.collider.GetComponentInParent<StorageUnit>();
            DeliveryHole hole = hit.collider.GetComponentInParent<DeliveryHole>();       
            DeliveryButton button = hit.collider.GetComponentInParent<DeliveryButton>(); 
            BuildingColorInteract colorStation = hit.collider.GetComponentInParent<BuildingColorInteract>(); 
            Workbench workbench = hit.collider.GetComponentInParent<Workbench>(); // <--- NOVÉ PRO WORKBENCH

            // 1. Trefili jsme ITEM
            if (item != null) 
            {
                currentCleanupTimer = 0f;
                if (currentTargetItem != item) 
                {
                    ClearInteractionTargets(); 
                    currentTargetItem = item;
                    isHovering = true;
                    UpdateUI(item.itemName, item.itemInteractionText, item.itemInteractionKey);
                }
                return;
            }
            // 2. Trefili jsme STORAGE (Truhlu)
            else if (storage != null) 
            {
                currentCleanupTimer = 0f;
                if (currentTargetStorage != storage) 
                {
                    ClearInteractionTargets();
                    currentTargetStorage = storage;
                    isHovering = true;
                    UpdateUI("Storage", "Open", "E");
                }
                return;
            }
            // 3. Trefili jsme DÍRU (Terminál)
            else if (currentDeliveryHole != hole)
            {
                currentCleanupTimer = 0f;
                if (currentDeliveryHole != hole)
                {
                    ClearInteractionTargets();
                    currentDeliveryHole = hole;
                    isHovering = true;
                    UpdateUI("Terminal", "Open", "E"); 
                }
                return;
            }
            // 4. Trefili jsme TLAČÍTKO
            else if (button != null)
            {
                currentCleanupTimer = 0f;
                if (currentDeliveryButton != button)
                {
                    ClearInteractionTargets();
                    currentDeliveryButton = button;
                    isHovering = true;
                    UpdateUI("Terminal", "Send", "E");
                }
                return;
            }
            // 5. Trefili jsme BARVY (Tablet nebo budovu)
            else if (colorStation != null)
            {
                currentCleanupTimer = 0f;
                if (currentBuildingColorStation != colorStation)
                {
                    ClearInteractionTargets();
                    currentBuildingColorStation = colorStation;
                    isHovering = true;
                    UpdateUI("Building", colorStation.hoverText, "E"); 
                }
                return;
            }
            // 6. Trefili jsme WORKBENCH <--- NOVÉ PRO WORKBENCH
            else if (workbench != null)
            {
                currentCleanupTimer = 0f;
                if (currentWorkbench != workbench)
                {
                    ClearInteractionTargets();
                    currentWorkbench = workbench;
                    isHovering = true;
                    // Vytáhneme si text "Open Workbench" přímo ze stolu
                    UpdateUI("Bench", workbench.hoverText, "E"); 
                }
                return;
            }
        }

        if (isHovering)
        {
            currentCleanupTimer += Time.deltaTime;
            if (currentCleanupTimer >= cleanupDelay)
            {
                ClearInteraction();
            }
        }
    }

    private void HandleInput()
    {
        if (!isHovering) return; 

        if (Input.GetKeyDown(KeyCode.E))
        {
            if (currentTargetItem != null) 
            {
                int leftOver = InventoryHandler.Instance.AddItem(
                    currentTargetItem.itemName, 
                    currentTargetItem.icon, 
                    currentTargetItem.quantity, 
                    currentTargetItem.stackable, 
                    currentTargetItem.maxStackSize
                );

                if (leftOver <= 0)
                {
                    Destroy(currentTargetItem.gameObject);
                    ClearInteraction();
                }
                else
                {
                    currentTargetItem.quantity = leftOver;
                }
            }
            else if (currentTargetStorage != null) 
            {
                currentTargetStorage.OpenStorage();
                ClearInteraction();
            }
            else if (currentDeliveryHole != null)
            {
                currentDeliveryHole.Interact();
                ClearInteraction();
            }
            else if (currentDeliveryButton != null)
            {
                currentDeliveryButton.Interact();
                ClearInteraction();
            }
            else if (currentBuildingColorStation != null)
            {
                currentBuildingColorStation.Interact();
                ClearInteraction();
            }
            // --- NOVÉ PRO WORKBENCH ---
            else if (currentWorkbench != null)
            {
                currentWorkbench.Interact(); // Otevře menu stolu
                ClearInteraction();
            }
        }
    }

    private void UpdateUI(string name, string action, string key)
    {
        if (interactionUI) interactionUI.SetActive(true);
        if (interactionItemName) interactionItemName.text = name;
        if (interactionTextUI) interactionTextUI.text = action;
        if (interactionKeyUI) interactionKeyUI.text = key;
        SetCrosshairAlpha(highlightCrosshairAlpha);
    }

    private void ClearInteractionTargets()
    {
        currentTargetItem = null;
        currentTargetStorage = null;
        currentDeliveryHole = null;
        currentDeliveryButton = null;
        currentBuildingColorStation = null;
        currentWorkbench = null; // <--- NOVÉ PRO WORKBENCH
    }

    private void ClearInteraction()
    {
        isHovering = false;
        ClearInteractionTargets();
        if (interactionUI) interactionUI.SetActive(false);
        SetCrosshairAlpha(normalCrosshairAlpha);
    }

    private void SetCrosshairAlpha(float alpha)
    {
        if (crosshair != null)
        {
            Color c = crosshair.color;
            c.a = alpha;
            crosshair.color = c;
        }
    }
}