using System;
using UnityEngine;

public class DepositScript : MonoBehaviour
{
    public string itemName;
    public string itemInventoryName;
    public Sprite icon;
    public int maxStackSize;

    public float maxHp = 100f;
    public float currentHp;
    public float hpPerItem = 5f;

    private InventoryHandler inventory;
    private int totalOre;
    private int droppedOre;

    [Obsolete("Obsolete")]
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

    public void MineDamage(float amount)
    {
        float previousHP = currentHp;
        currentHp -= amount;

        int oreBefore = Mathf.FloorToInt(previousHP / hpPerItem);
        int oreAfter  = Mathf.FloorToInt(Mathf.Max(currentHp, 0) / hpPerItem);

        int oreToDrop = oreBefore - oreAfter;

        for (int i = 0; i < oreToDrop; i++)
        {
            inventory.AddItem(itemInventoryName, icon, 1, true, maxStackSize);
            droppedOre++;
        }

        if (currentHp <= 0)
        {
            Destroy(gameObject);
        }
    }
}