using UnityEngine;

public class OxygenZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // Zkontrolujeme, jestli do zóny vešel hráč
        if (other.CompareTag("Player"))
        {
            VitalsController vitals = other.GetComponent<VitalsController>();
            if (vitals != null)
            {
                vitals.isInOxygenZone = true;
                Debug.Log("<color=cyan>[Základna] Hráč vstoupil. Doplňuji kyslík!</color>");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Zkontrolujeme, jestli hráč zónu opustil
        if (other.CompareTag("Player"))
        {
            VitalsController vitals = other.GetComponent<VitalsController>();
            if (vitals != null)
            {
                vitals.isInOxygenZone = false;
                Debug.Log("<color=cyan>[Základna] Hráč opustil bezpečí. Kyslík ubývá!</color>");
            }
        }
    }
}