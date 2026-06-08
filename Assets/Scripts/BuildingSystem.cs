using UnityEngine;

public class BuildingSystem : MonoBehaviour
{
    [Header("Grid Settings")]
    public float gridSize = 1.0f;
    public float yOffset = 0.5f; 
    public LayerMask buildableLayer;
    public Material ghostMaterial;

    [Header("Overlap Prevention (Ochrana proti překrývání)")]
    public LayerMask obstacleLayer;           // Vrstvy, které blokují stavbu (např. Default, Player, Buildings)
    public Material invalidGhostMaterial;     // Červený průhledný materiál
    public Vector3 checkBoxSize = new Vector3(0.45f, 0.45f, 0.45f); // Velikost kontroly (musí být menší než gridSize/2)
    private bool isPositionValid = true;
    private bool lastValidState = true;       // Pro optimalizaci (abychom neměnili materiál každý snímek)

    [Header("Building Zone (Hranice)")]
    public bool useBuildingLimits = true; 
    public Vector3 limitCenter = new Vector3(0, 5, 0); 
    public Vector3 limitSize = new Vector3(20, 10, 20); 

    [Header("Current Selection")]
    public BuildingInventory buildingInventory; 
    public GameObject objectToPlace;

    private GameObject currentGhost;
    private Camera cam;

    private void Start()
    {
        cam = Camera.main;
        if (objectToPlace != null) UpdateGhost(objectToPlace);
    }

    private void OnEnable()
    {
        if (objectToPlace != null && currentGhost == null) UpdateGhost(objectToPlace);
    }

    private void OnDisable()
    {
        if (currentGhost != null) Destroy(currentGhost);
    }

    public void UpdateGhost(GameObject newPrefab)
    {
        if (newPrefab == null)
        {
            if (currentGhost != null) Destroy(currentGhost);
            currentGhost = null;
            objectToPlace = null;
            return;
        }

        if (currentGhost != null) Destroy(currentGhost);

        objectToPlace = newPrefab;
        currentGhost = Instantiate(objectToPlace);

        // Zničení kolizí na duchovi (aby paprsek nebouchnul do něj)
        foreach (Collider c in currentGhost.GetComponentsInChildren<Collider>())
        {
            c.enabled = false; 
            Destroy(c);
        }

        // Vypnutí skriptů
        foreach (MonoBehaviour script in currentGhost.GetComponentsInChildren<MonoBehaviour>())
        {
            script.enabled = false;
        }

        // Výchozí barva (Zelená/Modrá = OK)
        lastValidState = true;
        ChangeGhostMaterial(ghostMaterial);
    }

    // Pomocná metoda pro rychlou změnu barvy ducha
    private void ChangeGhostMaterial(Material mat)
    {
        if (currentGhost == null || mat == null) return;
        
        foreach (Renderer r in currentGhost.GetComponentsInChildren<Renderer>())
        {
            Material[] newMaterials = new Material[r.sharedMaterials.Length];
            for (int i = 0; i < newMaterials.Length; i++) newMaterials[i] = mat;
            r.sharedMaterials = newMaterials;
        }
    }

    private void Update()
    {
        if (currentGhost == null) return;

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 50f, buildableLayer))
        {
            // 1. Výpočet pozice
            Vector3 rawPos = hit.point + (hit.normal * 0.01f);
            float x = Mathf.Round(rawPos.x / gridSize) * gridSize;
            float y = Mathf.Round(rawPos.y / gridSize) * gridSize;
            float z = Mathf.Round(rawPos.z / gridSize) * gridSize;

            Vector3 finalPosition = new Vector3(x, y + yOffset, z);

            // 2. Kontrola Hranic (Zóny)
            if (useBuildingLimits)
            {
                Bounds zone = new Bounds(limitCenter, limitSize);
                if (!zone.Contains(finalPosition))
                {
                    currentGhost.SetActive(false);
                    return; 
                }
            }

            currentGhost.SetActive(true);
            currentGhost.transform.position = finalPosition;

            if (Input.GetKeyDown(KeyCode.R)) currentGhost.transform.Rotate(0, 90, 0);

            // 3. KONTROLA PŘEKRÝVÁNÍ (Není tam už něco?)
            // Vytvoříme neviditelnou krabici a podíváme se, jestli uvnitř nejsou nějaké cizí collidery
            Collider[] colliders = Physics.OverlapBox(finalPosition, checkBoxSize, currentGhost.transform.rotation, obstacleLayer);
            isPositionValid = (colliders.Length == 0); // Platné je to jen tehdy, když je pole prázdné (nic jsme netrefili)

            // Přebarvení ducha (Zelený = Volno, Červený = Plno)
            if (isPositionValid != lastValidState)
            {
                ChangeGhostMaterial(isPositionValid ? ghostMaterial : invalidGhostMaterial);
                lastValidState = isPositionValid;
            }

            // 4. STAVĚNÍ (Kliknutí levým tlačítkem)
            if (Input.GetMouseButtonDown(0) && (InventoryHandler.Instance == null || !InventoryHandler.Instance.IsInventoryOpen))
            {
                if (isPositionValid) // <--- NOVÁ PODMÍNKA: Stavíme jen když je volno
                {
                    if (buildingInventory != null && buildingInventory.CanBuild())
                    {
                        Instantiate(objectToPlace, finalPosition, currentGhost.transform.rotation);
                        buildingInventory.DecreaseCount();
                    }
                    else if (buildingInventory == null) 
                    {
                        Instantiate(objectToPlace, finalPosition, currentGhost.transform.rotation);
                    }
                    else
                    {
                        Debug.Log("Nemáš dost surovin.");
                    }
                }
                else
                {
                    Debug.Log("Tady už něco je, nelze stavět!");
                }
            }
        }
        else
        {
            currentGhost.SetActive(false);
        }
    }

    private void OnDrawGizmos()
    {
        if (useBuildingLimits)
        {
            Gizmos.color = new Color(1, 1, 0, 0.2f); 
            Gizmos.DrawCube(limitCenter, limitSize);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(limitCenter, limitSize);
        }
    }
}