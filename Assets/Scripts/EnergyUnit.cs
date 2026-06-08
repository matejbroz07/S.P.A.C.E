using UnityEngine;
using TMPro;

public class EnergyUnit : MonoBehaviour
{
    [Header("Nastavení Energie")]
    public float maxEnergy = 300f;   // 3x víc než má dron (aby 1 dron sežral přesně 33 %)
    public float currentEnergy = 0f; 

    [Header("UI a Vizualizace")]
    public TMP_Text percentageText; 
    public Transform visualFill;    
    
    private float initialFillHeight; 

    void Start()
    {
        if (visualFill != null)
        {
            initialFillHeight = visualFill.localScale.y;
        }
        UpdateUI();
    }

    void Update()
    {
        // Přidali jsme UpdateUI i sem, aby se baterie vizuálně vybila, když z ní stanice saje energii!
        UpdateUI();
    }

    // Tuto metodu volá Solární panel
    public void ReceiveEnergy(float amount)
    {
        if (currentEnergy >= maxEnergy) return;

        currentEnergy += amount;
        
        if (currentEnergy > maxEnergy) 
            currentEnergy = maxEnergy;
    }

    private void UpdateUI()
    {
        float percentage = (currentEnergy / maxEnergy) * 100f;

        if (percentageText != null)
        {
            percentageText.text = $"{Mathf.RoundToInt(percentage)} %";
            
            if (percentage < 20f) percentageText.color = Color.red;
            else if (percentage < 50f) percentageText.color = Color.orange;
            else if (percentage > 99f) percentageText.color = Color.white;
            else percentageText.color = Color.yellow;
        }

        if (visualFill != null)
        {
            Vector3 newScale = visualFill.localScale;
            newScale.y = initialFillHeight * (currentEnergy / maxEnergy);
            visualFill.localScale = newScale;
        }
    }
}