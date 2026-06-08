using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class VitalsController : MonoBehaviour
{
    [Header("UI Rect Transforms")]
    public RectTransform energyBar;
    public RectTransform oxygenBar;
    public RectTransform hpBar;
    public Canvas UICanvas; // Zde máš hlavní UI hry (životy, atd.)

    [Header("Warnings UI")]
    public TMP_Text warningText;            
    public float lowOxygenThreshold = 25f;  
    public float lowHealthThreshold = 25f;  
    public float blinkSpeed = 3f;           

    [Header("Death & Respawn System")]
    public GameObject youDiedCanvas;        
    public Transform respawnPoint;          
    private bool isDead = false;

    [Header("Max Vitals Values")]
    public float maxHealth = 100f;
    public float maxEnergy = 100f;
    public float maxOxygen = 100f;

    [SerializeField] private float currentHealth = 100f;
    [SerializeField] private float currentEnergy = 100f;
    [SerializeField] private float currentOxygen = 100f;

    [Header("Decay Rates (za sekundu)")]
    public float oxygenDecayRate = 1f;
    public float healthDecayRateWithoutOxygen = 10f;
    
    [Header("Energy/Stamina Rates (za sekundu)")]
    public float energyDecayRateSprint = 15f; 
    public float energyRegenRate = 10f;
    
    [SerializeField] private float staminaRegenDelay = 1.5f;
    private float lastStaminaUseTime;
    
    [Header("Currency")]
    [SerializeField] private float credits = 0f;
    public TMP_Text creditsText;

    [Header("Regeneration (Základna)")]
    public float oxygenRegenRate = 15f; 
    [HideInInspector] public bool isInOxygenZone = false; 


    private void Start()
    {
        if (warningText != null) warningText.gameObject.SetActive(false);
        if (youDiedCanvas != null) youDiedCanvas.SetActive(false);

        UpdateVitalsUI();
        UpdateCreditsUI();
    }

    private void Update()
    {
        if (isDead) return;

        HandleVitalsDecay();
        
        // OPRAVA 1: Zkontrolujeme, jestli nás HandleVitalsDecay právě nezabilo.
        // Pokud ano, ukončíme Update a už nespouštíme HandleWarnings!
        if (isDead) return; 

        HandleWarnings(); 
    }

    private void HandleVitalsDecay()
    {
        if (isInOxygenZone)
        {
            if (currentOxygen < maxOxygen)
            {
                SetOxygen(currentOxygen + oxygenRegenRate * Time.deltaTime);
            }
        }
        else
        {
            if (currentOxygen > 0)
            {
                SetOxygen(currentOxygen - oxygenDecayRate * Time.deltaTime);
            }
        }

        if (currentOxygen <= 0f)
        {
            SetHealth(currentHealth - healthDecayRateWithoutOxygen * Time.deltaTime);
        }
    }

    private void HandleWarnings()
    {
        if (warningText == null) return;

        bool isOxygenEmpty = currentOxygen <= 0f;
        bool isHealthLow = currentHealth <= lowHealthThreshold;
        bool isOxygenLow = currentOxygen <= lowOxygenThreshold;

        if (isOxygenEmpty || isHealthLow || isOxygenLow)
        {
            if (!warningText.gameObject.activeSelf) 
                warningText.gameObject.SetActive(true);

            if (isOxygenEmpty)
            {
                warningText.text = "WARNING:\nNO OXYGEN";
                warningText.color = new Color(1f, 0f, 0f, warningText.color.a); 
            }
            else if (isHealthLow)
            {
                warningText.text = "WARNING:\nHEALTH LOW";
                warningText.color = new Color(1f, 0f, 0f, warningText.color.a); 
            }
            else
            {
                warningText.text = "WARNING:\nOXYGEN LOW";
                warningText.color = new Color(1f, 0.5f, 0f, warningText.color.a); 
            }

            Color c = warningText.color;
            float currentBlinkSpeed = isOxygenEmpty ? blinkSpeed * 1.5f : blinkSpeed;
            c.a = Mathf.Clamp(Mathf.PingPong(Time.time * currentBlinkSpeed, 1f), 0.2f, 1f);
            warningText.color = c;
        }
        else
        {
            if (warningText.gameObject.activeSelf)
            {
                warningText.gameObject.SetActive(false);
            }
        }
    }

    public void SetHealth(float value)
    {
        currentHealth = Mathf.Clamp(value, 0, maxHealth);
        UpdateVitalsUI();

        if (currentHealth <= 0f && !isDead)
        {
            Die();
        }
    }

    private void Die()
    {
        isDead = true;

        // OPRAVA 2: Vypneme celé hlavní UI, ať po smrti na obrazovce nestraší
        if (UICanvas != null) UICanvas.gameObject.SetActive(false);

        // Zobrazíme "You Died" obrazovku a pro jistotu jí dáme obří prioritu vykreslování
        if (youDiedCanvas != null) 
        {
            youDiedCanvas.SetActive(true);
            Canvas deathCanvas = youDiedCanvas.GetComponent<Canvas>();
            if (deathCanvas == null) deathCanvas = youDiedCanvas.GetComponentInParent<Canvas>();
            
            if (deathCanvas != null) deathCanvas.sortingOrder = 1000;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (warningText != null) warningText.gameObject.SetActive(false);

        Debug.Log("<color=red>[PLAYER] Hráč zemřel!</color>");
    }

    public void RespawnPlayer()
    {
        if (!isDead) return;

        if (InventoryHandler.Instance != null)
        {
            for (int i = 0; i < InventoryHandler.Instance.GetMaxSlots(); i++)
            {
                InventoryHandler.Instance.SetItem(i, null);
            }

            if (InventoryHandler.Instance.IsInventoryOpen)
            {
                InventoryHandler.Instance.ToggleInventory();
            }
        }

        currentHealth = maxHealth;
        currentEnergy = maxEnergy;
        currentOxygen = maxOxygen;
        UpdateVitalsUI();

        if (respawnPoint != null)
        {
            CharacterController cc = GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            transform.position = respawnPoint.position;
            transform.rotation = respawnPoint.rotation;

            if (cc != null) cc.enabled = true;
        }
        else
        {
            Debug.LogError("[RESPAWN] V Inspektoru chybí přiřazený Respawn Point!");
        }

        // UKLIZENÍ UI A NÁVRAT DO HRY
        if (youDiedCanvas != null) youDiedCanvas.SetActive(false);
        
        // OPRAVA 2 (Návrat): Znovu zapneme hlavní UI
        if (UICanvas != null) UICanvas.gameObject.SetActive(true);
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        isDead = false; 
        Debug.Log("<color=green>[RESPAWN] Hráč úspěšně oživen na základně.</color>");
    }

    public void SetEnergy(float value)
    {
        currentEnergy = Mathf.Clamp(value, 0, maxEnergy);
        UpdateVitalsUI();
    }

    public void SetOxygen(float value)
    {
        currentOxygen = Mathf.Clamp(value, 0, maxOxygen);
        UpdateVitalsUI();
    }
    
    public bool CanSprint()
    {
        return currentEnergy > 0.1f; 
    }
    
    public void HandleStamina(bool isSprinting)
    {
        if (isSprinting && CanSprint()) 
        {
            SetEnergy(currentEnergy - energyDecayRateSprint * Time.deltaTime);
            lastStaminaUseTime = Time.time;
        }
        else if (!isSprinting) 
        {
            if (Time.time >= lastStaminaUseTime + staminaRegenDelay)
            {
                SetEnergy(currentEnergy + energyRegenRate * Time.deltaTime);
            }
        }
    }
    
    public void AddCredits(float amount)
    {
        credits += amount;
        NotificationManager.Instance.ShowNotification("Credits", (int)amount, Color.white);
        UpdateCreditsUI();
    }

    private void UpdateCreditsUI()
    {
        if (creditsText != null)
        {
            creditsText.text = $"credits: {credits}";
        }
    }
    
    public float GetCredits()
    {
        return credits;
    }

    public bool SpendCredits(float amount)
    {
        if (credits >= amount)
        {
            credits -= amount;
            UpdateCreditsUI();
            return true; // Transakce proběhla úspěšně
        }
        return false; // Hráč nemá dost peněz
    }

    private void UpdateVitalsUI()
    {
        if (UICanvas != null) UICanvas.sortingOrder = 9;
        
        float hpRatio = currentHealth / maxHealth;
        if (hpBar != null) hpBar.localScale = new Vector3(hpRatio, 1f, 1f);

        float energyRatio = currentEnergy / maxEnergy;
        if (energyBar != null) energyBar.localScale = new Vector3(energyRatio, 1f, 1f);

        float oxygenRatio = currentOxygen / maxOxygen;
        if (oxygenBar != null) oxygenBar.localScale = new Vector3(oxygenRatio, 1f, 1f);
    }
}