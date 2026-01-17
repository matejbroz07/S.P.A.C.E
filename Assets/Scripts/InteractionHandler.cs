using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InteractionHandler : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float interactionDistance = 3f;
    [SerializeField] private LayerMask itemLayer;
    [SerializeField] private float cleanupDelay = 0.1f;
    private float currentCleanupTimer = 0f;

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
    private PickupItem currentTargetItem; 
    private StorageUnit currentTargetStorage; // Přidáno pro bedny
    private bool isHovering = false;

    private void Start()
    {
        mainCamera = Camera.main;
        if (interactionUI) interactionUI.SetActive(false);
        SetCrosshairAlpha(normalCrosshairAlpha);
    }

    private void Update()
    {
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
            // Zkusíme najít komponentu na tom, co jsme trefili (nebo na jeho rodiči)
            PickupItem item = hit.collider.GetComponentInParent<PickupItem>();
            StorageUnit storage = hit.collider.GetComponentInParent<StorageUnit>();

            if (item != null)
            {
                currentCleanupTimer = 0f;
                if (currentTargetItem != item)
                {
                    currentTargetItem = item;
                    currentTargetStorage = null;
                    isHovering = true;
                    UpdateUI(item.itemName, item.itemInteractionText, item.itemInteractionKey);
                }
                return;
            }
            else if (storage != null)
            {
                currentCleanupTimer = 0f;
                if (currentTargetStorage != storage)
                {
                    currentTargetStorage = storage;
                    currentTargetItem = null;
                    isHovering = true;
                    UpdateUI("Storage", "Open", "E"); // Tady můžeš dát storage.storageName
                }
                return;
            }
        }

        // Pokud nic netrefíme
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
            // SEBRÁNÍ ITEMU
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
            // OTEVŘENÍ STORAGE
            else if (currentTargetStorage != null)
            {
                currentTargetStorage.OpenStorage();
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

    private void ClearInteraction()
    {
        isHovering = false;
        currentTargetItem = null;
        currentTargetStorage = null;
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