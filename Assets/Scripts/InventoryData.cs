using UnityEngine;

[System.Serializable]
public class InventoryItem
{
    public string itemName;
    public Sprite icon;
    public int quantity;
    public bool stackable;
    public int maxStackSize;

    public InventoryItem(string name, Sprite icon = null, int quantity = 1, bool stackable = true, int maxStackSize = 99)
    {
        this.itemName = name;
        this.icon = icon;
        this.quantity = quantity;
        this.stackable = stackable;
        this.maxStackSize = maxStackSize;
    }
}