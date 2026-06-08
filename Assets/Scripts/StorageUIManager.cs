using UnityEngine;
using TMPro;

public class StorageUIManager : MonoBehaviour
{
    public static StorageUIManager Instance;

    [Header("UI References")]
    public GameObject storageUI;       // Celý panel (aktivuješ/deaktivuješ)
    public Transform slotsParent;      // Kam se generují sloty
    public TMP_Text storageNameText;   // Nadpis truhly
    public GameObject slotPrefab;      // Prefab slotu (aby ho nemusely mít truhly)

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
        
        if (storageUI) storageUI.SetActive(false);
    }
}