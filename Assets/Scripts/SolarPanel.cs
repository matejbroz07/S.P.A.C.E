using UnityEngine;

public class SolarPanel : MonoBehaviour
{
    [Header("Výroba Energie")]
    public float energyPerSecond = 1f; // 1 bod energie = 1% drona za sekundu!
    
    [Header("Směrové připojení (Kabel)")]
    public float gridSize = 1.0f; 
    public LayerMask targetLayer; // Vrstva, kde jsou Baterie i Stanice (přejmenováno)

    private float timer = 0f;

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= 1f)
        {
            SendEnergyForward();
            timer = 0f; 
        }
    }

    void SendEnergyForward()
    {
        Vector3 targetPosition = transform.position + (transform.forward * gridSize);
        float checkRadius = gridSize * 0.4f; 
        Collider[] hits = Physics.OverlapSphere(targetPosition, checkRadius, targetLayer);

        foreach (Collider hit in hits)
        {
            // OPRAVA: Použijeme GetComponentInParent!
            // Díky tomu Solár najde skript i v případě, že trefil jen nějaký pod-objekt (model střechy, dveře...)
            EnergyUnit battery = hit.GetComponentInParent<EnergyUnit>();
            DroneDockingStation station = hit.GetComponentInParent<DroneDockingStation>();

            if (battery != null)
            {
                battery.ReceiveEnergy(energyPerSecond);
                Debug.Log($"<color=yellow>[Solár] Úspěšně posílám energii do BATERIE ({battery.gameObject.name})</color>");
                break; 
            }
            else if (station != null)
            {
                station.ReceiveDirectEnergy(energyPerSecond);
                Debug.Log($"<color=yellow>[Solár] Úspěšně posílám energii přímo do STANICE ({station.gameObject.name})</color>");
                break;
            }
            else
            {
                // Pokud narazí do něčeho, co má správnou vrstvu, ale nemá na sobě ani jeden skript
                Debug.Log($"<color=red>[Solár] Trefil jsem {hit.gameObject.name}, ale nemá to na sobě ani Baterii ani Stanici!</color>");
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 startPos = transform.position;
        Vector3 targetPos = transform.position + (transform.forward * gridSize);

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(startPos, targetPos);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(targetPos, gridSize * 0.4f);
    }
}