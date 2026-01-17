using UnityEngine;

public class PickupItem : MonoBehaviour
{
    [Header("Item Settings")]
    public string itemName;
    public string itemInteractionText;
    public string itemInteractionKey;
    public Sprite icon;
    public int quantity;
    public bool stackable = true;
    public int maxStackSize;
}