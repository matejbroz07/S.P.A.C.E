using UnityEngine;

public class DirectionalStorageExpander : MonoBehaviour
{
    [Header("Nastavení Upgradu")]
    public int bonusCapacity = 5;      // Kolik slotů přidá
    public float gridSize = 1.0f;      // Velikost jednoho bloku (aby věděl, jak daleko je "soused")
    public LayerMask storageLayer;     // Vrstva, kde jsou truhly (např. "Building")

    // Zde si uložíme truhlu, kterou jsme úspěšně napojili
    private StorageUnit connectedStorage;

    void Start()
    {
        // Jakmile se blok postaví, zkusí se napojit
        ConnectToStorage();
    }

    void ConnectToStorage()
    {
        // 1. Vypočítáme pozici přesně JEDNO políčko PŘED tímto objektem.
        // transform.forward je směr, kam míří modrá šipka objektu (tvůj výstupek).
        Vector3 targetPosition = transform.position + (transform.forward * gridSize);
        
        // 2. Hodíme malou neviditelnou kouli PŘESNĚ na to jedno sousední políčko
        float checkRadius = gridSize * 0.4f; // 0.4 je ideální, aby to netrefilo rohy jiných bloků
        Collider[] hits = Physics.OverlapSphere(targetPosition, checkRadius, storageLayer);

        // 3. Zkontrolujeme, jestli na tom políčku něco je
        foreach (Collider hit in hits)
        {
            if (hit.TryGetComponent(out StorageUnit storage))
            {
                // Našli jsme truhlu!
                connectedStorage = storage;
                connectedStorage.AddCapacity(bonusCapacity);
                
                Debug.Log($"<color=cyan>[Upgrade]</color> Úspěšně napojeno na truhlu! Přidáno {bonusCapacity} slotů.");
                
                // Jakmile najdeme jednu truhlu, ukončíme hledání (v jednom políčku by měla být jen jedna)
                break; 
            }
        }

        if (connectedStorage == null)
        {
            Debug.Log($"<color=yellow>[Upgrade]</color> Výstupek míří do prázdna (nebo tam není truhla). Kapacita se nepřidala.");
        }
    }

    // Když hráč tento upgrade blok zničísss
    void OnDestroy()
    {
        // Pokud jsme byli napojení na truhlu, odebereme jí ty sloty zpět
        if (connectedStorage != null)
        {
            connectedStorage.RemoveCapacity(bonusCapacity);
            Debug.Log($"<color=orange>[Upgrade]</color> Upgrade zničen. Truhle byla odebrána kapacita.");
        }
    }

    // --- VIZUÁLNÍ POMŮCKA PRO TEBE DO EDITORU ---
    // Když v Unity klikneš na tento objekt, ukáže ti to modrou čáru a kuličku tam, kam to míří
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        
        // Kde je střed tohoto bloku
        Vector3 startPos = transform.position;
        // Kde je políčko před ním
        Vector3 targetPos = transform.position + (transform.forward * gridSize);

        // Vykreslí čáru (směr) a kouli (místo, kde hledá truhlu)
        Gizmos.DrawLine(startPos, targetPos);
        Gizmos.DrawWireSphere(targetPos, gridSize * 0.4f);
    }
}