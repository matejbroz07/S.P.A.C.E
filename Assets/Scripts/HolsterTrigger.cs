using UnityEngine;

public class HolsterTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Schováme zbraň plynule dolů
            if (WeaponHolsterHandler.Instance != null)
                WeaponHolsterHandler.Instance.SetHolsterState(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Vrátíme zbraň plynule nahoru
            if (WeaponHolsterHandler.Instance != null)
                WeaponHolsterHandler.Instance.SetHolsterState(false);
        }
    }
}
