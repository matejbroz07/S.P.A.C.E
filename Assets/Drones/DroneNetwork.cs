using UnityEngine;
using System.Collections.Generic;

public class DroneNetwork : MonoBehaviour
{
    public static DroneNetwork Instance;

    [Header("Sdílená data")]
    public List<DepositScript> knownDeposits = new List<DepositScript>();
    
    [Header("Kam nosit suroviny")]
    public StorageUnit mainStorage; 

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void ReportFoundDeposit(DepositScript deposit)
    {
        if (!knownDeposits.Contains(deposit) && deposit.GetOreLeft() > 0)
        {
            knownDeposits.Add(deposit);
            Debug.Log($"[Síť] Průzkumník našel nové ložisko: {deposit.itemName}");
        }
    }

    public DepositScript GetTargetForDrill()
    {
        knownDeposits.RemoveAll(d => d == null || d.GetOreLeft() <= 0);
        if (knownDeposits.Count > 0) return knownDeposits[0];
        return null;
    }
}