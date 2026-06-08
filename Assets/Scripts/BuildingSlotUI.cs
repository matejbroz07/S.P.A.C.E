using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BuildingSlotUI : MonoBehaviour
{
    [Header("UI References")]
    public Image iconImage;       
    public TMP_Text countText;    
    public Image borderImage;     
    public TMP_Text keyText;      

    [Header("Colors")]
    public Color selectedColor = Color.yellow;
    public Color normalColor = new Color(1f, 1f, 1f, 0.5f); 

    public void Setup(Sprite icon, int count, int keyNumber)
    {
        if (keyText != null) keyText.text = keyNumber.ToString();
        
        iconImage.sprite = icon;
        UpdateVisuals(icon, count);
        Deselect();
    }

    public void UpdateCount(int newCount)
    {
        UpdateVisuals(iconImage.sprite, newCount);
    }

    // NOVÁ METODA: Stará se o to, jak slot vypadá (průhlednost a text)
    private void UpdateVisuals(Sprite icon, int count)
    {
        if (icon == null || count <= 0)
        {
            // Slot je PRÁZDNÝ nebo došly suroviny
            iconImage.enabled = false;                   // Vypne renderování
            iconImage.color = new Color(1f, 1f, 1f, 0f); // Průhlednost na maximum (0)
            countText.text = "";                         // Vymaže nulu
        }
        else
        {
            // Slot je PLNÝ
            iconImage.enabled = true;                    // Zapne renderování
            iconImage.color = new Color(1f, 1f, 1f, 1f); // Průhlednost na nulu (plně viditelné)
            countText.text = count.ToString();           // Ukáže číslo
        }
    }

    public void Select()
    {
        if (borderImage) borderImage.color = selectedColor;
    }

    public void Deselect()
    {
        if (borderImage) borderImage.color = normalColor;
    }
}