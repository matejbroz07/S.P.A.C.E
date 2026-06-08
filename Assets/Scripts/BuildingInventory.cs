using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class BuildingItem 
{
    public string name;
    public GameObject prefab; 
    public Sprite icon;       
    public int quantity; 
}

public class BuildingInventory : MonoBehaviour
{
    [Header("References")]
    public BuildingSystem buildingSystem; 
    public Transform slotsParent; 
    public GameObject slotPrefab;         

    [Header("Data")]
    public BuildingItem[] buildings = new BuildingItem[5]; 

    private List<BuildingSlotUI> uiSlots = new List<BuildingSlotUI>();
    private int selectedIndex = 0;

    void Start()
    {
        GenerateSlots();
        SelectSlot(0); 
    }

    void GenerateSlots()
    {
        if (slotPrefab == null) return;

        foreach (Transform child in slotsParent) Destroy(child.gameObject);
        uiSlots.Clear();

        for (int i = 0; i < buildings.Length; i++)
        {
            GameObject newSlotObj = Instantiate(slotPrefab, slotsParent);
            BuildingSlotUI slotUI = newSlotObj.GetComponent<BuildingSlotUI>();

            if (slotUI != null)
            {
                slotUI.Setup(buildings[i].icon, buildings[i].quantity, i + 1);
                uiSlots.Add(slotUI);
            }
        }
    }

    void Update()
    {
        if (buildingSystem != null && slotsParent != null)
        {
            bool isBuildingMode = buildingSystem.enabled;
            
            if (slotsParent.gameObject.activeSelf != isBuildingMode)
            {
                slotsParent.gameObject.SetActive(isBuildingMode);
            }

            if (!isBuildingMode) return;
        }

        if (Input.GetKeyDown(KeyCode.Alpha1)) SelectSlot(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SelectSlot(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SelectSlot(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SelectSlot(3);
        if (Input.GetKeyDown(KeyCode.Alpha5)) SelectSlot(4);
    }

    void SelectSlot(int index)
    {
        if (index < 0 || index >= buildings.Length) return;

        selectedIndex = index;

        for (int i = 0; i < uiSlots.Count; i++)
        {
            if (i == index) uiSlots[i].Select();
            else uiSlots[i].Deselect();
        }

        if (buildingSystem != null)
        {
            if (buildings[index].quantity > 0 && buildings[index].prefab != null)
            {
                buildingSystem.UpdateGhost(buildings[index].prefab);
            }
            else
            {
                buildingSystem.UpdateGhost(null); 
            }
        }
    }
    
    public void DecreaseCount()
    {
        if (uiSlots.Count == 0) return;

        if (buildings[selectedIndex].quantity > 0)
        {
            buildings[selectedIndex].quantity--;
            uiSlots[selectedIndex].UpdateCount(buildings[selectedIndex].quantity);

            // Pokud nám právě došel poslední kus...
            if (buildings[selectedIndex].quantity <= 0)
            {
                // ZCELA VYPRÁZDNÍME SLOT, aby do něj mohl jít jiný předmět
                buildings[selectedIndex].name = "";
                buildings[selectedIndex].prefab = null;
                buildings[selectedIndex].icon = null;
                
                // Aktualizujeme UI (schováme ikonu)
                uiSlots[selectedIndex].Setup(null, 0, selectedIndex + 1);

                if (buildingSystem != null)
                {
                    buildingSystem.UpdateGhost(null);
                }
            }
        }
    }
    
    public bool CanBuild()
    {
        return buildings[selectedIndex].quantity > 0 && buildings[selectedIndex].prefab != null;
    }

    // --- KOMPLETNÍ TVORBA NOVÉHO SLOTU ---
    public void AddBuildingItem(string itemName, int amountToAdd, GameObject prefab, Sprite icon)
    {
        string searchName = itemName.Trim().ToLower();

        // 1. Zkusíme zjistit, jestli už takový předmět nemáme (jen bychom přidali kusy)
        for (int i = 0; i < buildings.Length; i++)
        {
            if (buildings[i] != null && !string.IsNullOrEmpty(buildings[i].name))
            {
                if (buildings[i].name.Trim().ToLower() == searchName)
                {
                    buildings[i].quantity += amountToAdd;
                    if (uiSlots.Count > i) uiSlots[i].UpdateCount(buildings[i].quantity);

                    if (i == selectedIndex && buildingSystem != null && buildings[i].quantity > 0)
                    {
                        buildingSystem.UpdateGhost(buildings[i].prefab);
                    }
                    return;
                }
            }
        }

        // 2. Předmět nemáme. Najdeme první PRÁZDNÝ slot a vše do něj nahrajeme.
        for (int i = 0; i < buildings.Length; i++)
        {
            // Pokud je pole náhodou null, vyrobíme novou instanci
            if (buildings[i] == null) buildings[i] = new BuildingItem();

            // Prázdný slot je takový, co nemá jméno nebo má quantity 0
            if (string.IsNullOrEmpty(buildings[i].name) || buildings[i].quantity <= 0)
            {
                // Zapíšeme úplně všechny údaje
                buildings[i].name = itemName;
                buildings[i].prefab = prefab;
                buildings[i].icon = icon;
                buildings[i].quantity = amountToAdd;

                // Propíšeme vše do UI
                if (uiSlots.Count > i) 
                {
                    uiSlots[i].Setup(buildings[i].icon, buildings[i].quantity, i + 1);
                }

                // Pokud máme zrovna tento slot vybraný, ukážeme rovnou ducha
                if (i == selectedIndex && buildingSystem != null && buildings[i].quantity > 0)
                {
                    buildingSystem.UpdateGhost(buildings[i].prefab);
                }

                Debug.Log($"<color=cyan>[BuildingInventory] Vytvořen nový slot pro: {itemName}</color>");
                return;
            }
        }

        Debug.LogWarning($"<color=orange>[BuildingInventory] Tvůj stavební hotbar je plný! Nemám kam přidat {itemName}.</color>");
    }
}