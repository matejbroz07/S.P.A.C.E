public interface IItemContainer
{
    InventoryItem GetItem(int index);
    void SetItem(int index, InventoryItem item);
    int GetMaxSlots();
    bool CanAddItem(InventoryItem item, int index);
}