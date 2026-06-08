using UnityEngine;

public class DeliveryButton : MonoBehaviour
{
    [Header("Nastavení Interakce")]
    public string hoverText = "Send"; // Text při najetí
    
    [Header("Reference")]
    public DeliveryTerminal terminal; // Odkaz na hlavní mozek terminálu

    public void Interact()
    {
        if (terminal != null)
        {
            terminal.OnSubmitButtonPressed();
        }
    }
}