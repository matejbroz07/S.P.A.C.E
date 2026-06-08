using UnityEngine;
using UnityEngine.AI;

public class DroneHoverVisual : MonoBehaviour
{
    [Header("Nastavení Vznášení")]
    public float hoverSpeed = 2f;    
    public float hoverAmount = 0.5f; 
    public float tiltAmount = 15f;   
    public float tiltSpeed = 5f;     

    private Vector3 startPos;
    private Quaternion startRot; 
    private NavMeshAgent parentAgent;
    private WorkerDrone parentDrone; // <--- Přidána reference na hlavní mozek

    void Start()
    {
        startPos = transform.localPosition;
        startRot = transform.localRotation; 
        
        parentAgent = GetComponentInParent<NavMeshAgent>();
        parentDrone = GetComponentInParent<WorkerDrone>();
    }

    void Update()
    {
        // 1. VZNÁŠENÍ (Bobbing) - Vypne se, pokud dron těží
        if (parentDrone != null && parentDrone.isMining)
        {
            // Pokud těží, plynule (Lerp) se srovnáme do přesně stabilní výšky bez houpání
            float stableY = Mathf.Lerp(transform.localPosition.y, startPos.y, Time.deltaTime * 5f);
            transform.localPosition = new Vector3(startPos.x, stableY, startPos.z);
        }
        else
        {
            // Pokud netěží (letí nebo stojí), klasicky se houpe
            float newY = startPos.y + Mathf.Sin(Time.time * hoverSpeed) * hoverAmount;
            transform.localPosition = new Vector3(startPos.x, newY, startPos.z);
        }

        // 2. NAKLÁNĚNÍ PŘI LETU
        if (parentAgent != null)
        {
            float targetTilt = 0f;
            if (parentAgent.velocity.sqrMagnitude > 0.1f)
            {
                targetTilt = tiltAmount; 
            }

            Quaternion tiltRotation = Quaternion.Euler(0f, 0f, targetTilt);
            Quaternion finalRotation = startRot * tiltRotation;
            transform.localRotation = Quaternion.Slerp(transform.localRotation, finalRotation, Time.deltaTime * tiltSpeed);
        }
    }
}