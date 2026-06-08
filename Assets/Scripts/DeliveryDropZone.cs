using UnityEngine;
using UnityEngine.EventSystems; 

public class DeliveryDropZone : MonoBehaviour, IDropHandler
{
    [Header("Reference")]
    public DeliveryTerminal terminal; // Odkaz na hlavní mozek terminálu

    // Zavolá se automaticky, když hráč pustí předmět nad tímto panelem
    public void OnDrop(PointerEventData eventData)
    {
        // 1. Zjistíme, jestli hráč opravdu upustil ItemSlotUI
        ItemSlotUI draggedSlot = eventData.pointerDrag?.GetComponent<ItemSlotUI>();

        if (draggedSlot != null)
        {
            // 2. Vytáhneme si item pomocí tvé nové metody
            InventoryItem item = draggedSlot.GetItem();

            if (item != null && item.quantity > 0)
            {
                // 3. Pošleme ho do terminálu a zjistíme, kolik se nám toho vrátilo zpět
                int leftover = terminal.TryInsertItem(item.itemName, item.quantity);

                // 4. Aktualizujeme původní slot v inventáři hráče
                if (leftover <= 0)
                {
                    // Terminál sežral úplně všechno -> vyčistíme slot
                    draggedSlot.SetItem(null);
                }
                else if (leftover < item.quantity)
                {
                    // Terminál si vzal jen část -> upravíme množství a aktualizujeme slot
                    item.quantity = leftover;
                    draggedSlot.SetItem(item);
                }
                else
                {
                    // Terminál si nevzal nic (nepotřebuje to)
                    // Neděláme nic. Tvůj IEndDragHandler v ItemSlotUI se postará o to, 
                    // že se ikonka prostě "vrátí" zpět na své místo.
                    Debug.Log("Tuto surovinu Terminál momentálně nepotřebuje.");
                }
            }
        }
    }
}