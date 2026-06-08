using UnityEngine;

[RequireComponent(typeof(Collider))]
public class LocationQuestTrigger : MonoBehaviour
{
    [Header("Nastavení Questu")] [Tooltip("Přesně toto jméno musíš mít vyplněné v QuestData jako 'Target Action Name'")]
    public string actionNameToReport; 
    
    [Tooltip("Zničí tento objekt po vstupu, aby se akce nehlásila pořád dokola")]
    public bool destroyAfterTrigger = true; 

    private void OnTriggerEnter(Collider other)
    {
        // Zkontrolujeme, jestli do zóny vstoupil HRÁČ (musí mít na sobě Tag "Player")
        if (other.CompareTag("Player"))
        {
            if (AdvancedQuestManager.Instance != null)
            {
                // Nahlásíme to do našeho Quest Manageru!
                AdvancedQuestManager.Instance.ReportAction(actionNameToReport);
                Debug.Log($"<color=green>[Exploration] Hráč objevil oblast: {actionNameToReport}</color>");
                
                // Zničíme neviditelnou zónu, úkol je hotový
                if (destroyAfterTrigger)
                {
                    Destroy(gameObject);
                }
            }
        }
    }
}