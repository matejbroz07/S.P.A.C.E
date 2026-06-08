using System;
using UnityEngine;

public class DepositScript : MonoBehaviour
{
    public string itemName;
    public string itemInventoryName;
    public Sprite icon;
    public int maxStackSize;
    
    [Header("Co z toho padá na zem (Pro drona)")]
    public GameObject oreDropPrefab;

    public float maxHp = 100f;
    public float currentHp;
    public float hpPerItem = 5f;

    private InventoryHandler inventory;
    private int totalOre;
    private int droppedOre;

    void Start()
    {
        currentHp = maxHp;
        inventory = FindObjectOfType<InventoryHandler>();

        totalOre = Mathf.CeilToInt(maxHp / hpPerItem);
        droppedOre = 0;
    }

    public int GetOreLeft()
    {
        return totalOre - droppedOre;
    }

    // --- ZMĚNA: Přidán parametr 'bool isDrone = false' ---
    public void MineDamage(float amount, bool isDrone = false)
    {
        float previousHp = currentHp;
        currentHp -= amount;

        int oreBefore = Mathf.FloorToInt(previousHp / hpPerItem);
        int oreAfter  = Mathf.FloorToInt(Mathf.Max(currentHp, 0) / hpPerItem);

        int oreToDrop = oreBefore - oreAfter;

        for (int i = 0; i < oreToDrop; i++)
        {
            if (isDrone)
            {
                // 1. TĚŽÍ DRON -> Ruda padá fyzicky na zem
                if (oreDropPrefab != null)
                {
                    // Vypočítáme náhodnou pozici, ať itemy neskáčou úplně do sebe
                    Vector3 randomOffset = new Vector3(UnityEngine.Random.Range(-1f, 1f), 1f, UnityEngine.Random.Range(-1f, 1f));
                    Instantiate(oreDropPrefab, transform.position + randomOffset, Quaternion.identity);
                }
                else
                {
                    Debug.LogWarning("V DepositScript chybí Ore Drop Prefab!");
                }
            }
            else
            {
                // 2. TĚŽÍ HRÁČ -> Ruda jde rovnou do inventáře
                inventory.AddItem(itemInventoryName, icon, 1, true, maxStackSize);
            }

            // Ať už to vzal hráč nebo dron, kámen o rudu přišel
            droppedOre++;
        }

        if (currentHp <= 0)
        {
            Destroy(gameObject);
        }
    }
}