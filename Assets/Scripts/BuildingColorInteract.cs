using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Collider))] // Pojistka, aby to mělo kolizní box pro Raycast
public class BuildingColorInteract : MonoBehaviour
{
    [Header("Nastavení Interakce")]
    public string hoverText = "OPEN"; // Text, který přečte tvůj Raycaster
    
    [Header("UI Reference")]
    public GameObject colorMenuUI; // Celý Canvas/Panel s paletami
    public Button confirmButton;   // Tlačítko pro potvrzení barev

    [Header("Color Pickery")]
    public ColorPicker picker1;
    public ColorPicker picker2;
    public ColorPicker picker3;

    [Header("Materiály k obarvení")]
    public Material material1;
    public Material material2;
    public Material material3;
    
    public float emissionIntensity = 5.5f;

    private bool isMenuOpen = false;

    void Start()
    {
        // Na začátku schováme menu s barvami
        if (colorMenuUI != null) colorMenuUI.SetActive(false);

        // Automaticky napojíme tlačítko
        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(ApplyColorsAndClose);
        }
    }

    void Update()
    {
        // Změněno pouze na Escape, aby se to nebilo s Ečkem!
        if (isMenuOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseMenu();
        }
    }

    // TUTO METODU ZAVOLÁ TVŮJ HRÁČ, KDYŽ SE PODÍVÁ NA BUDOVU A ZMÁČKNE 'E'
    public void Interact()
    {
        if (!isMenuOpen)
        {
            OpenMenu();
        }
    }

    private void OpenMenu()
    {
        isMenuOpen = true;
        if (colorMenuUI != null) colorMenuUI.SetActive(true);

        // Odemkneme a ukážeme myš
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void CloseMenu()
    {
        isMenuOpen = false;
        if (colorMenuUI != null) colorMenuUI.SetActive(false);

        // Zamkneme a schováme myš
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void ApplyColorsAndClose()
    {
        // Obarvíme materiály podle pickerů
        if (material1 != null && picker1 != null) material1.color = picker1.selectedColor;
        if (material2 != null && picker2 != null) material2.color = picker2.selectedColor;
        if (material3 != null && picker3 != null)
        {
            Color pickedColor = picker3.selectedColor;

            // Základní barva
            material3.color = pickedColor;

            // Zapneme emisi, kdyby náhodou byla na materiálu vypnutá
            material3.EnableKeyword("_EMISSION");

            // Nastavíme barvu emise a vynásobíme ji intenzitou, aby to bylo pravé svítící HDR
            material3.SetColor("_EmissionColor", pickedColor * emissionIntensity);
        }

        Debug.Log("<color=green>[BuildingColorizer] Barvy byly úspěšně aplikovány!</color>");
        
        // Zavřeme menu
        CloseMenu(); 
    }

    private void OnDestroy()
    {
        // Úklid, aby se neštosovaly eventy na tlačítku
        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveListener(ApplyColorsAndClose);
        }
    }
}