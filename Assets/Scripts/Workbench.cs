using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; 
using TMPro;
using UnityEngine.Events;

[System.Serializable]
public class UpgradeBlueprint
{
    public string upgradeName;       
    public float creditCost;         
    public int currentLevel = 0;     
    public int maxLevel = 1;         
    public UnityEvent onUpgradePurchased; 
}

[System.Serializable]
public class ResourceCost
{
    public string resourceName;      
    public int amountRequired;       
}

[System.Serializable]
public class CraftingRecipe
{
    public string resultItemName;    
    public Sprite resultIcon;        
    
    [Header("Pro Budovy (Hotbar)")]
    [Tooltip("Zaškrtni, pokud je to budova (Solár, Stanice) a přiděl jí prefab.")]
    public bool isBuildingItem = false; 
    public GameObject resultPrefab; // <--- NOVÉ: 3D model, co se pak bude stavět

    [Header("Pro Běžné Předměty")]
    public int resultQuantity = 1;   
    public bool isStackable = true;
    public int maxStackSize = 99;
    
    [Header("Cena")]
    public List<ResourceCost> requiredResources; 
}

public class Workbench : MonoBehaviour
{
    [Header("Interakce")]
    public string hoverText = "Open Workbench"; // Křížek to teď pozná

    [Header("Hlavní UI")]
    public GameObject workbenchUI; 
    private bool isWorkbenchOpen = false;

    [Header("Hráč a Inventáře")]
    public VitalsController playerVitals; 
    public BuildingInventory buildingInventory; 

    [Header("Detailní Panel (Crafting)")]
    public TMP_Text detailNameText;      
    public Image detailIconImage;        
    public TMP_Text detailCostText;      
    public Button detailCraftButton;     
    
    private int selectedRecipeIndex = -1; 

    [Header("Nabídka")]
    public List<UpgradeBlueprint> upgrades;
    public List<CraftingRecipe> craftingRecipes;

    void Start()
    {
        if (workbenchUI != null) workbenchUI.SetActive(false);
        
        if (detailCraftButton != null) detailCraftButton.interactable = false;
        if (detailNameText != null) detailNameText.text = "Vyberte předmět";
        if (detailCostText != null) detailCostText.text = "";
    }

    void Update()
    {
        if (isWorkbenchOpen && (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.Escape)))
        {
            CloseWorkbench();
        }
    }

    public void Interact()
    {
        isWorkbenchOpen = true;
        if (workbenchUI != null) workbenchUI.SetActive(true);

        if (InventoryHandler.Instance != null && !InventoryHandler.Instance.IsInventoryOpen)
        {
            InventoryHandler.Instance.ToggleInventory();
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void CloseWorkbench()
    {
        isWorkbenchOpen = false;
        if (workbenchUI != null) workbenchUI.SetActive(false);

        if (InventoryHandler.Instance != null && InventoryHandler.Instance.IsInventoryOpen)
        {
            InventoryHandler.Instance.ToggleInventory();
        }
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void SelectRecipe(int recipeIndex)
    {
        if (recipeIndex < 0 || recipeIndex >= craftingRecipes.Count) return;

        selectedRecipeIndex = recipeIndex;
        CraftingRecipe recipe = craftingRecipes[recipeIndex];

        if (detailNameText != null) detailNameText.text = recipe.resultItemName;
        if (detailIconImage != null)
        {
            detailIconImage.sprite = recipe.resultIcon;
            detailIconImage.color = Color.white; 
        }

        string costText = "Potřebné suroviny:\n";
        bool hasAllResources = true;

        foreach (ResourceCost cost in recipe.requiredResources)
        {
            int amountOwned = InventoryHandler.Instance != null ? InventoryHandler.Instance.GetTotalItemCount(cost.resourceName) : 0;
            
            if (amountOwned >= cost.amountRequired)
            {
                costText += $"<color=green>- {cost.resourceName}: {amountOwned}/{cost.amountRequired}</color>\n";
            }
            else
            {
                costText += $"<color=red>- {cost.resourceName}: {amountOwned}/{cost.amountRequired}</color>\n";
                hasAllResources = false; 
            }
        }

        if (detailCostText != null) detailCostText.text = costText;
        if (detailCraftButton != null) detailCraftButton.interactable = hasAllResources;
    }

    public void CraftSelectedRecipe()
    {
        if (selectedRecipeIndex < 0) return;

        CraftingRecipe recipe = craftingRecipes[selectedRecipeIndex];

        // Smažeme suroviny
        foreach (ResourceCost cost in recipe.requiredResources)
        {
            InventoryHandler.Instance.RemoveItems(cost.resourceName, cost.amountRequired);
        }

        // PŘIDÁVÁNÍ DO SPRÁVNÉHO INVENTÁŘE
        if (recipe.isBuildingItem)
        {
            if (buildingInventory != null)
            {
                // Pošleme ÚPLNĚ VŠECHNO do stavebního hotbaru
                buildingInventory.AddBuildingItem(recipe.resultItemName, recipe.resultQuantity, recipe.resultPrefab, recipe.resultIcon);
            }
        }
        else
        {
            InventoryHandler.Instance.AddItem(recipe.resultItemName, recipe.resultIcon, recipe.resultQuantity, recipe.isStackable, recipe.maxStackSize);
        }
        
        Debug.Log($"<color=green>[Workbench] Vyrobeno: {recipe.resultItemName}!</color>");
        SelectRecipe(selectedRecipeIndex); 
    }

    public void BuyUpgrade(int upgradeIndex)
    {
        if (upgradeIndex < 0 || upgradeIndex >= upgrades.Count) return;
        UpgradeBlueprint upgrade = upgrades[upgradeIndex];

        if (upgrade.currentLevel >= upgrade.maxLevel) return;

        if (playerVitals != null && playerVitals.SpendCredits(upgrade.creditCost))
        {
            upgrade.currentLevel++;
            upgrade.onUpgradePurchased?.Invoke(); 
        }
    }
}