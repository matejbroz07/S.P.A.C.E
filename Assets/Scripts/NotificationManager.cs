using UnityEngine;

public class NotificationManager : MonoBehaviour
{
    public static NotificationManager Instance;

    [Header("UI Settings")]
    public Transform notificationParent; 
    public GameObject notificationPrefab; 

    // Pamatujeme si poslední notifikaci
    private NotificationItem lastNotification;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // Metoda se změnila - teď přijímá jméno a množství zvlášť!
    public void ShowNotification(string itemName, int quantity, Color color)
    {
        if (notificationPrefab == null || notificationParent == null) return;

        // KONTROLA STACKOVÁNÍ
        // 1. Máme nějakou poslední notifikaci?
        // 2. Je to ten samý předmět?
        // 3. Existuje ještě ten objekt (nebyl zničen)?
        if (lastNotification != null && 
            lastNotification.itemName == itemName && 
            lastNotification != null) // Unity check if object exists
        {
            // Pokud ano, jen přičteme množství a restartujeme časovač
            lastNotification.AddAmount(quantity);
            return; // Končíme, nevytváříme nový
        }

        // Pokud ne (nebo je to jiný item), vytvoříme novou notifikaci
        GameObject newItem = Instantiate(notificationPrefab, notificationParent);
        NotificationItem itemScript = newItem.GetComponent<NotificationItem>();
        
        if (itemScript != null)
        {
            itemScript.Initialize(itemName, quantity, color);
            lastNotification = itemScript; // Uložíme si ji jako novou "poslední"
        }
        
        newItem.transform.SetAsLastSibling();
    }
}