using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class DepositDropChance
{
    public GameObject depositPrefab;
    [Range(1, 100)] public int spawnWeight = 10;
    public bool alignWithGround = false; // NOVÉ: Pokud zaškrtneš, deposit se "přilepí" ke sklonu země
}

[System.Serializable]
public class BiomeZone
{
    public string biomeName;
    public Transform centerPoint;
    public float radius = 50f;
    public int maxDepositsInBiome = 20; // NOVÉ: Každý biom má svůj vlastní limit
    public List<DepositDropChance> possibleDeposits;
    
    // Skrytý seznam pro sledování kamenů v tomto konkrétním biomu
    [HideInInspector] public List<GameObject> activeDeposits = new List<GameObject>();
}

public class DepositSpawner : MonoBehaviour
{
    [Header("Základní Nastavení")]
    public float respawnInterval = 5f; 
    public LayerMask groundLayer;

    [Header("Ochrana pohledu")]
    public Camera playerCamera;
    public float minDistanceFromPlayer = 15f;

    [Header("Seznam Biomů")]
    public List<BiomeZone> biomes;

    private float timer = 0f;

    void Start()
    {
        if (playerCamera == null) playerCamera = Camera.main;

        // Na začátku naplníme všechny biomy na jejich maximum
        GenerateInitialWorld();
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= respawnInterval)
        {
            // Každých pár vteřin zkontrolujeme všechny biomy, jestli nepotřebují doplnit
            foreach (var biome in biomes)
            {
                // Vyčistíme seznam od zničených kamenů
                biome.activeDeposits.RemoveAll(item => item == null);

                if (biome.activeDeposits.Count < biome.maxDepositsInBiome)
                {
                    TrySpawnInBiome(biome);
                }
            }
            timer = 0f;
        }
    }

    private void GenerateInitialWorld()
    {
        foreach (var biome in biomes)
        {
            int attempts = 0;
            int maxAttempts = biome.maxDepositsInBiome * 5;

            while (biome.activeDeposits.Count < biome.maxDepositsInBiome && attempts < maxAttempts)
            {
                TrySpawnInBiome(biome);
                attempts++;
            }
        }
    }

    private void TrySpawnInBiome(BiomeZone biome)
    {
        Vector2 randomCircle = Random.insideUnitCircle * biome.radius;
        Vector3 testPosition = biome.centerPoint.position + new Vector3(randomCircle.x, 0, randomCircle.y);

        Vector3 rayStart = new Vector3(testPosition.x, testPosition.y + 100f, testPosition.z);
        
        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 200f, groundLayer))
        {
            if (IsPositionHiddenFromPlayer(hit.point))
            {
                // 1. Vybereme, co budeme spawnovat
                DepositDropChance selection = GetRandomSelection(biome.possibleDeposits);
                
                // 2. VYPOČTEME ROTACI (To je ta díra v zemi / sklon)
                Quaternion finalRotation;
                if (selection.alignWithGround)
                {
                    // Tato funkce otočí objekt tak, aby jeho osa "UP" mířila tam, kam míří normála země
                    finalRotation = Quaternion.FromToRotation(Vector3.up, hit.normal) * selection.depositPrefab.transform.rotation;
                }
                else
                {
                    // Klasická rotace prefabu
                    finalRotation = selection.depositPrefab.transform.rotation;
                }

                // 3. SPAWN
                GameObject newDep = Instantiate(selection.depositPrefab, hit.point, finalRotation);
                biome.activeDeposits.Add(newDep);
            }
        }
    }

    private bool IsPositionHiddenFromPlayer(Vector3 targetPos)
    {
        if (Vector3.Distance(playerCamera.transform.position, targetPos) < minDistanceFromPlayer) return false;

        Vector3 viewportPoint = playerCamera.WorldToViewportPoint(targetPos);
        bool isOnScreen = viewportPoint.z > 0 && 
                          viewportPoint.x > -0.1f && viewportPoint.x < 1.1f && 
                          viewportPoint.y > -0.1f && viewportPoint.y < 1.1f;

        return !isOnScreen;
    }

    private DepositDropChance GetRandomSelection(List<DepositDropChance> deposits)
    {
        int totalWeight = 0;
        foreach (var dep in deposits) totalWeight += dep.spawnWeight;
        int randomValue = Random.Range(0, totalWeight);

        foreach (var dep in deposits)
        {
            if (randomValue < dep.spawnWeight) return dep;
            randomValue -= dep.spawnWeight;
        }
        return deposits[0];
    }

    private void OnDrawGizmos()
    {
        if (biomes == null) return;
        foreach (var biome in biomes)
        {
            if (biome.centerPoint != null)
            {
                Gizmos.color = new Color(0f, 1f, 0f, 0.2f);
                Gizmos.DrawWireSphere(biome.centerPoint.position, biome.radius);
            }
        }
    }
}