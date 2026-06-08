using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DeliveryWorldSlot : MonoBehaviour
{
    public Image itemIcon;
    public TMP_Text amountLeftText;

    // Tuto metodu zavolá hlavní terminál
    public void SetupSlot(Sprite icon, int amountLeft)
    {
        if (icon == null || amountLeft <= 0)
        {
            // Pokud slot není potřeba, nebo už je splněno, zprůhledníme ho
            itemIcon.color = new Color(1, 1, 1, 0); 
            amountLeftText.text = "";
        }
        else
        {
            // Slot je aktivní a čeká na suroviny
            itemIcon.color = new Color(1, 1, 1, 1);
            itemIcon.sprite = icon;
            amountLeftText.text = amountLeft.ToString();
        }
    }
}