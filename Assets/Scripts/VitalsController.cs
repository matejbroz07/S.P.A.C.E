using UnityEngine;

public class VitalsController : MonoBehaviour
{
    [Header("UI Rect Transforms")]
    public RectTransform energyBar;
    public RectTransform oxygenBar;
    public RectTransform hpBar;

    [Header("Current Vitals Values")]
    public float maxHealth = 100f;
    public float maxEnergy = 100f;
    public float maxOxygen = 100f;

    [SerializeField] private float currentHealth = 100f;
    [SerializeField] private float currentEnergy = 100f;
    [SerializeField] private float currentOxygen = 100f;

    [Header("Decay Rates (za sekundu)")]
    [Tooltip("Kyslík ubírá 1 za sekundu.")]
    public float oxygenDecayRate = 1f;
    
    [Tooltip("HP ubírá 10 za sekundu, když je kyslík 0.")]
    public float healthDecayRateWithoutOxygen = 10f;
    
    [Header("Energy/Stamina Rates (za sekundu)")]
    [Tooltip("Rychlost ubírání energie při sprintu.")]
    public float energyDecayRateSprint = 15f; 
    [Tooltip("Rychlost doplňování energie, když hráč nesprintuje.")]
    public float energyRegenRate = 10f;


    private void Start()
    {
        UpdateVitalsUI();
    }

    private void Update()
    {
        HandleVitalsDecay();
    }

    private void HandleVitalsDecay()
    {
        if (currentOxygen > 0)
        {
            SetOxygen(currentOxygen - oxygenDecayRate * Time.deltaTime);
        }

        if (currentOxygen <= 0f)
        {
            SetHealth(currentHealth - healthDecayRateWithoutOxygen * Time.deltaTime);
        }
        
    }


    public void SetHealth(float value)
    {
        currentHealth = Mathf.Clamp(value, 0, maxHealth);
        UpdateVitalsUI();
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
    
    // Přidáme novou veřejnou metodu pro kontrolu, zda je sprint povolen
    public bool CanSprint()
    {
        // Povolíme sprint, pokud máme alespoň minimální množství energie
        return currentEnergy > 0.1f; 
    }
    
    // Přidáme metodu pro zpracování úbytku/doplnění staminy
    public void HandleStamina(bool isSprinting)
    {
        if (isSprinting && CanSprint()) 
        {
            // Ubírání energie při sprintu
            SetEnergy(currentEnergy - energyDecayRateSprint * Time.deltaTime);
        }
        
        else if (!isSprinting) 
        {
            // Doplňování energie při stání/chůzi (regenerace)
            SetEnergy(currentEnergy + energyRegenRate * Time.deltaTime);
        }
    }

    private void UpdateVitalsUI()
    {
        float hpRatio = currentHealth / maxHealth;
        hpBar.localScale = new Vector3(hpRatio, 1f, 1f);

        float energyRatio = currentEnergy / maxEnergy;
        energyBar.localScale = new Vector3(energyRatio, 1f, 1f);

        float oxygenRatio = currentOxygen / maxOxygen;
        oxygenBar.localScale = new Vector3(oxygenRatio, 1f, 1f);
    }
}