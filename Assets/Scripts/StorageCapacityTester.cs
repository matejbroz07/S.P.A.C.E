using System.Collections;
using UnityEngine;

public class StorageCapacityTester : MonoBehaviour
{
    [Header("Nastavení testu")]
    public StorageUnit storageToTest;  // Kterou truhlu testujeme
    public int amountToChange = 5;     // Kolik slotů se má přidat/odebrat
    public float delayInSeconds = 3f;  // Za jak dlouho se to přepne

    void Start()
    {
        // Pokud jsme truhlu nepřetáhli v Inspektoru, zkusí ji najít na stejném objektu
        if (storageToTest == null)
        {
            storageToTest = GetComponent<StorageUnit>();
        }

        // Pokud ji máme, spustíme nekonečnou smyčku
        if (storageToTest != null)
        {
            StartCoroutine(TestRoutine());
        }
        else
        {
            Debug.LogError("Tester nenašel StorageUnit! Přetáhni ji do Inspektoru.");
        }
    }

    // Coroutine - speciální metoda, která umí čekat v čase
    IEnumerator TestRoutine()
    {
        while (true) // Nekonečná smyčka (furt dokola)
        {
            // 1. Počkáme 3 sekundy
            yield return new WaitForSeconds(delayInSeconds);

            // 2. Přidáme kapacitu
            Debug.Log("<color=green>TESTER: Přidávám kapacitu!</color>");
            storageToTest.AddCapacity(amountToChange);

            // 3. Zase počkáme 3 sekundy
            yield return new WaitForSeconds(delayInSeconds);

            // 4. Odebereme kapacitu
            Debug.Log("<color=red>TESTER: Odebírám kapacitu!</color>");
            storageToTest.RemoveCapacity(amountToChange);
        }
    }
}