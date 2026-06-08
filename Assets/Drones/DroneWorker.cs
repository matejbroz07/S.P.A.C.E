using UnityEngine;
using UnityEngine.AI;

public enum DroneRole { Discovery, Drill, Collector }
public enum DroneState { Charging, Working, Returning }

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(LineRenderer))]
public class WorkerDrone : MonoBehaviour
{
    [Header("Role a Dokování")]
    public DroneRole role;
    public DroneDockingStation myDock;
    
    [Header("Baterie a Spotřeba")]
    public float maxBattery = 100f;
    public float currentBattery = 0f;
    public float drainPerSecond = 2f; 
    public float chargeSpeed = 15f;   
    public float safetyMargin = 5f;   

    [Header("Vizuál Vrtulí a Laseru")]
    public Transform[] propellers; 
    public float propSpinSpeed = 1000f; 
    public Transform laserFirePoint;
    public float laserMaxRange = 5f;

    [Header("Vizuál Sběrače (Arkádový Hák)")]
    public Transform clawTransform;   // Tvůj model háku
    public Transform ropeTransform;   // Tvůj model válce (lano)
    public float clawSpeed = 5f;      // Jak rychle hák sjíždí a vyjíždí
    private bool isGrabbing = false;
    private bool isClawReturning = false;
    private Vector3 originalClawLocalPos;
    private Vector3 originalRopeScale;

    [Header("Nastavení Práce")]
    public float workRange = 3f;

    [Header("Discovery (Průzkumník)")]
    public float patrolRadius = 30f; // NOVÉ: Jak daleko od vlajky může létat
    public float scanRadius = 10f;   // PŮVODNÍ: Jak daleko dron "vidí" kolem SEBE
    public LayerMask depositLayer;

    [Header("Drill (Těžař)")]
    public float mineSpeed = 5f;
    [HideInInspector] public bool isMining = false;
    
    [Header("Collector (Sběrač)")]
    public LayerMask itemLayer;
    public float pickupRadius = 10f;

    public DroneState currentState = DroneState.Charging;
    
    public DepositScript targetDeposit;
    public PickupItem targetItem;
    public InventoryItem carriedItem;
    public StorageUnit targetStorage;

    private NavMeshAgent agent;
    private LineRenderer laserLine;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        laserLine = GetComponent<LineRenderer>();
        
        if (laserLine != null)
        {
            laserLine.positionCount = 2;
            laserLine.enabled = false;
        }

        if (myDock != null)
        {
            agent.Warp(myDock.restingPoint.position);
            transform.rotation = myDock.restingPoint.rotation;
        }

        // Uložíme si původní (zataženou) pozici háku a tloušťku lana
        if (clawTransform != null)
        {
            originalClawLocalPos = clawTransform.localPosition;
            if (ropeTransform != null) 
            {
                originalRopeScale = ropeTransform.localScale; // Uložíme tloušťku
                ropeTransform.gameObject.SetActive(false); 
            }
        }
    }

    void Update()
    {
        isMining = false; 

        if (currentBattery <= 0 && currentState != DroneState.Charging)
        {
            currentBattery = 0;
            agent.isStopped = true;
            AnimatePropellers();
            UpdateLaserVisual();
            return;
        }

        switch (currentState)
        {
            case DroneState.Charging: 
                HandleCharging(); 
                break;
                
            case DroneState.Working:
                DrainBattery();
                CheckReturnTrip();
                
                if (currentState == DroneState.Working) 
                {
                    if (role == DroneRole.Discovery) HandleDiscovery();
                    else if (role == DroneRole.Drill) HandleDrill(); 
                    else if (role == DroneRole.Collector) HandleCollector();
                }
                break;
                
            case DroneState.Returning:
                DrainBattery();
                HandleReturn();
                break;
        }

        AnimatePropellers(); 
        UpdateLaserVisual();
    }

    // --- VIZUÁLY ---
    private void AnimatePropellers()
    {
        if (propellers == null || propellers.Length == 0) return;

        // Vrtule se točí, pokud dron NENÍ v doku (Charging) A ZÁROVEŇ má šťávu.
        // (Kdybychom nekontrolovali baterku, točil by se i mrtvý dron padající k zemi!)
        bool shouldSpin = currentState != DroneState.Charging && currentBattery > 0f;

        if (shouldSpin)
        {
            float rotationThisFrame = propSpinSpeed * Time.deltaTime;
            foreach (Transform prop in propellers)
            {
                if (prop != null) prop.Rotate(Vector3.forward * rotationThisFrame, Space.Self);
            }
        }
    }

    private void UpdateLaserVisual()
    {
        if (laserLine == null) return;

        if (isMining && role == DroneRole.Drill && laserFirePoint != null)
        {
            laserLine.enabled = true;
            laserLine.SetPosition(0, laserFirePoint.position);
            laserLine.SetPosition(1, laserFirePoint.position + Vector3.down * laserMaxRange);
        }
        else
        {
            laserLine.enabled = false;
        }
    }

    // --- BATERIE A POHYB ---
    void DrainBattery() => currentBattery -= drainPerSecond * Time.deltaTime;

    void CheckReturnTrip()
    {
        if (myDock == null) return;
        float energyNeeded = (Vector3.Distance(transform.position, myDock.restingPoint.position) / agent.speed) * drainPerSecond;

        // Dron by se neměl vracet, pokud zrovna spouští dráp dolů, aby se neutrhlo lano!
        if (currentBattery <= (energyNeeded + safetyMargin) && !isGrabbing)
        {
            currentState = DroneState.Returning;
        }
    }

    void HandleReturn()
    {
        agent.isStopped = false;
        agent.SetDestination(myDock.restingPoint.position);

        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            currentState = DroneState.Charging;
            transform.rotation = myDock.restingPoint.rotation;
        }
    }

    void HandleCharging()
    {
        agent.isStopped = true;
        currentBattery += myDock.ChargeDrone(chargeSpeed * Time.deltaTime);

        if (currentBattery >= maxBattery)
        {
            currentBattery = maxBattery;
            currentState = DroneState.Working;
            
            // OPRAVA TADY: Předáme mu aktivní vlajku (pokud nějaká je)
            if (role == DroneRole.Discovery) 
            {
                MoveToRandomLocation(GetActiveFlag()); 
            }
        }
    }

    // --- LOGIKA ROLÍ ---
    void HandleDiscovery()
    {
        Transform flag = GetActiveFlag();
        
        // ZMĚNA: Skenujeme VŽDY KOLEM DRONA! (Oči drona)
        Collider[] hits = Physics.OverlapSphere(transform.position, scanRadius, depositLayer);
        
        foreach (Collider hit in hits) 
        {
            DepositScript deposit = hit.GetComponentInParent<DepositScript>();
            if (deposit != null) 
            {
                DroneNetwork.Instance.ReportFoundDeposit(deposit);
            }
        }

        if (!agent.pathPending && agent.remainingDistance < 0.5f) 
        {
            MoveToRandomLocation(flag);
        }
    }

    void MoveToRandomLocation(Transform flag)
    {
        // ZMĚNA: Používáme patrolRadius pro velikost oblasti létání!
        Vector3 center = flag != null ? flag.position : transform.position;
        Vector2 randomDir = Random.insideUnitCircle * patrolRadius; 
        
        agent.isStopped = false;
        agent.SetDestination(new Vector3(center.x + randomDir.x, transform.position.y, center.z + randomDir.y));
    }

    void HandleDrill()
    {
        if (targetDeposit != null && targetDeposit.GetOreLeft() <= 0)
        {
            Destroy(targetDeposit.gameObject); 
            targetDeposit = null;              
        }
        
        if (targetDeposit == null || targetDeposit.GetOreLeft() <= 0)
        {
            targetDeposit = DroneNetwork.Instance.GetTargetForDrill();
            agent.isStopped = true;
            return; 
        }

        Collider depositCollider = targetDeposit.GetComponent<Collider>();
        Vector3 exactCenter = depositCollider != null ? depositCollider.bounds.center : targetDeposit.transform.position;

        Vector3 pos2D = new Vector3(transform.position.x, 0, transform.position.z);
        Vector3 target2D = new Vector3(exactCenter.x, 0, exactCenter.z);

        if (Vector3.Distance(pos2D, target2D) > workRange)
        {
            agent.isStopped = false;
            agent.SetDestination(exactCenter);
        }
        else
        {
            agent.isStopped = true;
            isMining = true; 
            Vector3 lookPos = exactCenter;
            lookPos.y = transform.position.y;
            transform.LookAt(lookPos);
            targetDeposit.MineDamage(mineSpeed * Time.deltaTime, true); 
        }
    }

    // --- SBĚRAČ A HÁK ---
    // --- HLAVNÍ METODA SBĚRAČE ---
    void HandleCollector()
    {
        if (isGrabbing)
        {
            agent.isStopped = true; 
            HandleClawAnimation();
            return; 
        }

        bool hasRealItem = carriedItem != null && !string.IsNullOrEmpty(carriedItem.itemName);
        bool isFull = hasRealItem && carriedItem.quantity >= 20;
        
        Transform flag = GetActiveFlag();

        // --- 1. SKENOVÁNÍ KÓLEM VLAJKY (nebo kolem sebe) ---
        if (targetItem == null && !isFull)
        {
            Vector3 scanCenter = flag != null ? flag.position : transform.position;
            Collider[] hits = Physics.OverlapSphere(scanCenter, pickupRadius, itemLayer);
            
            foreach (Collider nalezenyObjekt in hits)
            {
                PickupItem potentialItem = nalezenyObjekt.GetComponentInParent<PickupItem>();
                
                if (potentialItem != null)
                {
                    if (hasRealItem)
                    {
                        if (potentialItem.itemName == carriedItem.itemName)
                        {
                            targetItem = potentialItem;
                            break; 
                        }
                    }
                    else
                    {
                        targetItem = potentialItem;
                        break;
                    }
                }
            }
        }

        // --- 2. NÁVRAT K TRUHLE ---
        bool wantsToDeposit = isFull || (hasRealItem && targetItem == null);

        if (wantsToDeposit)
        {
            if (targetStorage == null)
            {
                targetStorage = FindNearestStorage();
                if (targetStorage == null) 
                {
                    agent.isStopped = true;
                    return; 
                }
            }

            Vector3 myPos2D = new Vector3(transform.position.x, 0, transform.position.z);
            Vector3 storage2D = new Vector3(targetStorage.transform.position.x, 0, targetStorage.transform.position.z);
            
            if (Vector3.Distance(myPos2D, storage2D) > workRange)
            {
                agent.isStopped = false;
                agent.SetDestination(targetStorage.transform.position);
            }
            else
            {
                agent.isStopped = true;
                bool deposited = false;

                if (carriedItem.stackable)
                {
                    for (int i = 0; i < targetStorage.GetMaxSlots(); i++)
                    {
                        InventoryItem slotItem = targetStorage.GetItem(i);
                        if (slotItem != null && !string.IsNullOrEmpty(slotItem.itemName) && slotItem.itemName == carriedItem.itemName)
                        {
                            if (slotItem.quantity + carriedItem.quantity <= slotItem.maxStackSize)
                            {
                                slotItem.quantity += carriedItem.quantity;
                                carriedItem = null;
                                deposited = true;
                                break; 
                            }
                        }
                    }
                }

                if (!deposited)
                {
                    for (int i = 0; i < targetStorage.GetMaxSlots(); i++)
                    {
                        InventoryItem slotItem = targetStorage.GetItem(i);
                        if (slotItem == null || string.IsNullOrEmpty(slotItem.itemName))
                        {
                            targetStorage.SetItem(i, carriedItem);
                            carriedItem = null;
                            deposited = true;
                            break;
                        }
                    }
                }

                targetStorage = null; 
            }
            return; 
        }

        // --- 3. LETÍME PRO RUDU, NEBO LETÍME K VLAJCE ČEKAT ---
        if (targetItem != null)
        {
            Vector3 pos2D = new Vector3(transform.position.x, 0, transform.position.z);
            Vector3 item2D = new Vector3(targetItem.transform.position.x, 0, targetItem.transform.position.z);

            if (Vector3.Distance(pos2D, item2D) > 0.1f)
            {
                agent.isStopped = false;
                agent.SetDestination(targetItem.transform.position);
            }
            else
            {
                agent.isStopped = true;
                Vector3 lookPos = targetItem.transform.position;
                lookPos.y = transform.position.y;
                transform.LookAt(lookPos);

                if (clawTransform != null)
                {
                    isGrabbing = true;
                    isClawReturning = false;
                    if (ropeTransform != null) ropeTransform.gameObject.SetActive(true);
                }
                else FinishGrab();
            }
        }
        else
        {
            // Pokud ruda došla, ale vlajka je postavená, leť k vlajce a čekej tam na další práci!
            if (flag != null)
            {
                Vector3 pos2D = new Vector3(transform.position.x, 0, transform.position.z);
                Vector3 flag2D = new Vector3(flag.position.x, 0, flag.position.z);
                
                // Zastaví se cca 3 metry od vlajky, ať do ní nenarazí
                if (Vector3.Distance(pos2D, flag2D) > 3f)
                {
                    agent.isStopped = false;
                    agent.SetDestination(flag.position);
                }
                else agent.isStopped = true;
            }
            else
            {
                agent.isStopped = true; // Žádný cíl, žádná vlajka
            }
        }
    }

    // --- OPRAVENÁ ANIMACE HÁKU ---
    // --- OPRAVENÁ ANIMACE HÁKU (Natahování přes osu Z) ---
    void HandleClawAnimation()
    {
        Vector3 targetUpWorld = clawTransform.parent.TransformPoint(originalClawLocalPos);

        if (!isClawReturning)
        {
            // FÁZE A: Hák jede DOLŮ k rudě
            if (targetItem != null)
            {
                clawTransform.position = Vector3.MoveTowards(clawTransform.position, targetItem.transform.position, clawSpeed * Time.deltaTime);
                
                // Jsme dole u rudy!
                if (Vector3.Distance(clawTransform.position, targetItem.transform.position) < 0.2f)
                {
                    isClawReturning = true; 
                }
            }
            else
            {
                isClawReturning = true; 
            }
        }
        else
        {
            // FÁZE B: Hák jede ZPÁTKY NAHORU
            clawTransform.position = Vector3.MoveTowards(clawTransform.position, targetUpWorld, clawSpeed * Time.deltaTime);

            // "Falešné přichycení" rudy - drží se háku bez deformace
            if (targetItem != null)
            {
                targetItem.transform.position = clawTransform.position;
            }

            if (Vector3.Distance(clawTransform.position, targetUpWorld) < 0.01f)
            {
                FinishGrab(); 
            }
        }

        // --- NATAHOVÁNÍ LANA PŘES OSU Z ---
        if (ropeTransform != null)
        {
            Vector3 startPoint = targetUpWorld; // Břicho drona
            Vector3 endPoint = clawTransform.position; // Hák
            
            Vector3 direction = endPoint - startPoint;
            if (direction != Vector3.zero)
            {
                // Natočíme lano (osu Z) přesně na hák
                ropeTransform.forward = direction.normalized;
                
                float distance = direction.magnitude;
                
                // ZMĚNA 1: SCALE (Délka lana)
                // Vyhodil jsem dělení dvěma. Teď to bere čistou vzdálenost.
                // Pokud by lano bylo pořád moc krátké/dlouhé, můžeš to tu vynásobit, např: (distance * 2f) nebo (distance * 0.5f)
                ropeTransform.localScale = new Vector3(originalRopeScale.x, originalRopeScale.y, distance*100f);
                
                // ZMĚNA 2: POZICE (Pivot point)
                // Pokud má tvůj válec z Blenderu střed přesně UPROSTŘED:
                ropeTransform.position = (startPoint + endPoint) / 2f;
                
                // Pokud má tvůj válec z Blenderu střed NA KRAJI (nahoře):
                // Smaž řádek nad tímto textem a odkomentuj tento řádek dole:
                // ropeTransform.position = startPoint;
            }
        }
    }

    void FinishGrab()
    {
        isGrabbing = false;
        if (ropeTransform != null) ropeTransform.gameObject.SetActive(false); 

        if (targetItem != null)
        {
            // Pokud je břicho prázdné, vytvoříme nový záznam
            if (carriedItem == null || string.IsNullOrEmpty(carriedItem.itemName))
            {
                carriedItem = new InventoryItem(targetItem.itemName, targetItem.icon, targetItem.quantity, targetItem.stackable, targetItem.maxStackSize);
            }
            else
            {
                // Pokud už stejnou rudu vezeme, PŘIČTEME MNOŽSTVÍ!
                carriedItem.quantity += targetItem.quantity;
            }

            Destroy(targetItem.gameObject);
            targetItem = null;
            
            // Log do konzole, abychom viděli, kolik už toho nabral!
            Debug.Log($"<color=yellow>[Sběrač] CHŇAP! V břiše mám teď {carriedItem.quantity}/20 kousků.</color>");
        }
    }

    StorageUnit FindNearestStorage()
    {
        StorageUnit[] allStorages = FindObjectsOfType<StorageUnit>();
        StorageUnit nearest = null;
        float minDistance = Mathf.Infinity;

        foreach (StorageUnit storage in allStorages)
        {
            bool hasSpace = false;
            for (int i = 0; i < storage.GetMaxSlots(); i++)
            {
                if (storage.GetItem(i) == null || string.IsNullOrEmpty(storage.GetItem(i).itemName)) 
                {
                    hasSpace = true; break;
                }
            }
            if (hasSpace)
            {
                float dist = Vector3.Distance(transform.position, storage.transform.position);
                if (dist < minDistance) { minDistance = dist; nearest = storage; }
            }
        }
        return nearest;
    }
    
    private Transform GetActiveFlag()
    {
        // Najde ve scéně objekt se štítkem "DroneFlag"
        GameObject flag = GameObject.FindGameObjectWithTag("DroneFlag");
        return flag != null ? flag.transform : null;
    }
}