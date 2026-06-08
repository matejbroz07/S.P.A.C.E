using UnityEngine;
using TMPro;

public class DroneDockingStation : MonoBehaviour
{
    [Header("Nastavení Spawnu")]
    public WorkerDrone dronePrefab;   // Vložíš prefab drona (z tvých složek v Projektu, ne ze Scény!)
    public Transform restingPoint;    // Kde přesně dron sedí

    [Header("Propojení s Dronem")]
    [HideInInspector] public WorkerDrone assignedDrone; // Skryto! Stanice si to propojí sama při spawnu.

    [Header("Připojení k Síti (Zásuvka)")]
    public Transform powerCheckPoint; 
    public float checkRadius = 0.5f;  

    [Header("UI Displeje")]
    public TMP_Text typeText;         
    public TMP_Text batteryText;      

    void Start()
    {
        // Když se stanice objeví ve světě (hráč ji postaví), hned si vyrobí svého drona
        if (dronePrefab != null && assignedDrone == null)
        {
            SpawnDrone();
        }
    }

    private void SpawnDrone()
    {
        // 1. Vytvoříme drona přesně na místě RestingPointu s jeho rotací
        assignedDrone = Instantiate(dronePrefab, restingPoint.position, restingPoint.rotation);
        
        // 2. DŮLEŽITÉ: Řekneme dronovi "Já jsem tvoje domovská stanice!"
        assignedDrone.myDock = this;

        // 3. Pro pořádek ho přejmenujeme v Hierarchy, ať víme, co je zač
        assignedDrone.gameObject.name = "Drone_" + assignedDrone.role.ToString();
    }

    void Update()
    {
        // Aktualizace displejů běží dál, jak jsme ji nastavili
        if (assignedDrone != null)
        {
            if (typeText != null) typeText.text = assignedDrone.role.ToString();
            if (batteryText != null) 
            {
                float percent = (assignedDrone.currentBattery / assignedDrone.maxBattery) * 100f;
                batteryText.text = Mathf.RoundToInt(percent) + " %";
            }
        }
        else
        {
            if (typeText != null) typeText.text = "ŽÁDNÝ";
            if (batteryText != null) batteryText.text = "0 %";
        }
    }

    // Tuto metodu volá dron, když na stanici sedí a chce šťávu
    public float ChargeDrone(float requestedAmount)
    {
        EnergyUnit powerSource = FindPowerSource();

        if (powerSource != null)
        {
            if (powerSource.currentEnergy >= requestedAmount)
            {
                powerSource.currentEnergy -= requestedAmount;
                return requestedAmount;
            }
            else
            {
                float available = powerSource.currentEnergy;
                powerSource.currentEnergy = 0;
                return available;
            }
        }
        return 0f;
    }

    private EnergyUnit FindPowerSource()
    {
        if (powerCheckPoint == null) return null;

        Collider[] hits = Physics.OverlapSphere(powerCheckPoint.position, checkRadius);
        foreach (Collider hit in hits)
        {
            EnergyUnit eu = hit.GetComponentInParent<EnergyUnit>();
            if (eu != null)
            {
                return eu; 
            }
        }
        return null;
    }
    
    public void ReceiveDirectEnergy(float amount)
    {
        // 1. Zkontrolujeme, jestli stanice vůbec ví o svém dronovi
        if (assignedDrone == null)
        {
            Debug.Log("<color=red>[Stanice] Dostala jsem energii ze Soláru, ale NEMÁM přiřazeného žádného drona!</color>");
            return;
        }

        // 2. Zkontrolujeme, jestli dron sedí v doku a chce se nabíjet
        if (assignedDrone.currentState != DroneState.Charging)
        {
            Debug.Log($"<color=orange>[Stanice] Dostávám energii, ale dron ji nechce! Zrovna dělá tohle: {assignedDrone.currentState}</color>");
            return;
        }

        // 3. Vše je v pořádku, posíláme energii dronovi!
        assignedDrone.currentBattery += amount;
        Debug.Log($"<color=green>[Stanice] Vše funguje! Cpu do drona energii. Aktuální stav: {assignedDrone.currentBattery} / {assignedDrone.maxBattery}</color>");
            
        // Pojistka proti přebití
        if (assignedDrone.currentBattery > assignedDrone.maxBattery)
        {
            assignedDrone.currentBattery = assignedDrone.maxBattery;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (powerCheckPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(powerCheckPoint.position, checkRadius);
        }
    }
}