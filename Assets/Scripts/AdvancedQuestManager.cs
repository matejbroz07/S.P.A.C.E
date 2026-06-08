using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class AdvancedQuestManager : MonoBehaviour
{
    // SINGLETON - Abychom na manažera mohli volat z jakéhokoliv skriptu ve hře
    public static AdvancedQuestManager Instance;

    [Header("Quest Line Configuration")] 
    public List<QuestGroup> questLine;
    private int currentGroupIndex = 0;

    [Header("UI References")] 
    public GameObject questUI;
    public GameObject questGroupUI;
    public Transform questListParent;  // Objekt s Vertical Layout Group
    public GameObject questTextPrefab; // Prefab s TMP_Text komponentou

    private void Awake()
    {
        // Nastavení Singletonu
        if (Instance == null) 
        {
            Instance = this;
        }
        else 
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Resetujeme data při startu - DŮLEŽITÉ: musíme vyčistit seznamy a akce!
        foreach (var group in questLine)
        {
            foreach (var q in group.questsInGroup)
            {
                group.rewardGiven = false;
                q.isCompleted = false;
                q.currentActionCount = 0; // Resetujeme akční questy
                
                if (q.pressedKeys != null) 
                    q.pressedKeys.Clear(); // Vyčistí stará data z minulé hry
                else 
                    q.pressedKeys = new List<KeyCode>();
            }
        }

        // Připojení na inventář (pokud se inventář změní, zkontrolujeme questy)
        if (InventoryHandler.Instance != null)
        {
            InventoryHandler.Instance.InventoryChanged += CheckProgress;
        }
        
        UpdateUI();
    }

    private void Update()
    {
        if (InventoryHandler.Instance != null)
        {
            questUI.SetActive(!InventoryHandler.Instance.IsInventoryOpen); 
        }
        HandleInputQuests();
    }
    
    // --- 1. LOGIKA PRO INPUT QUESTY (Mačkání kláves) ---
    private void HandleInputQuests()
    {
        if (currentGroupIndex >= questLine.Count) return;

        QuestGroup currentGroup = questLine[currentGroupIndex];
        bool changed = false;

        foreach (var quest in currentGroup.questsInGroup)
        {
            if (!quest.isCompleted && quest.requiredKeys != null && quest.requiredKeys.Count > 0)
            {
                foreach (KeyCode key in quest.requiredKeys)
                {
                    if (Input.GetKeyDown(key))
                    {
                        if (!quest.pressedKeys.Contains(key))
                        {
                            quest.pressedKeys.Add(key);
                            changed = true;
                        }
                    }
                }

                if (quest.pressedKeys.Count >= quest.requiredKeys.Count)
                {
                    quest.isCompleted = true;
                    changed = true;
                }
            }
        }

        if (changed)
        {
            CheckProgress(); 
            UpdateUI();
        }
    }

    // --- 2. LOGIKA PRO AKČNÍ QUESTY (Voláno z jiných skriptů) ---
    public void ReportAction(string actionName, int amount = 1)
    {
        if (currentGroupIndex >= questLine.Count) return;

        QuestGroup currentGroup = questLine[currentGroupIndex];
        bool changed = false;

        foreach (var quest in currentGroup.questsInGroup)
        {
            // Hledáme nedokončený quest, který čeká přesně na tuhle akci
            if (!quest.isCompleted && quest.targetActionName == actionName)
            {
                quest.currentActionCount += amount;
                
                if (quest.currentActionCount >= quest.actionGoalAmount)
                {
                    quest.currentActionCount = quest.actionGoalAmount;
                    quest.isCompleted = true;
                }
                changed = true;
            }
        }

        if (changed)
        {
            CheckProgress();
            UpdateUI();
        }
    }

    // --- KONTROLA POSTUPU A ROZDÁVÁNÍ ODMĚN ---
    private void CheckProgress()
    {
        if (currentGroupIndex >= questLine.Count) return;

        QuestGroup currentGroup = questLine[currentGroupIndex];
        bool allDone = true;

        foreach (var quest in currentGroup.questsInGroup)
        {
            // Kontrola ITEM questů (jestli máme věci v inventáři)
            if (!string.IsNullOrEmpty(quest.targetItemName))
            {
                int amount = GetItemCount(quest.targetItemName);
                if (amount >= quest.goalAmount) quest.isCompleted = true;
            }

            // Pokud alespoň jeden quest není hotový, skupina není hotová
            if (!quest.isCompleted) allDone = false;
        }

        // ODMĚNA ZA DOKONČENÍ CELÉ SKUPINY
        if (allDone && !currentGroup.rewardGiven)
        {
            VitalsController vitals = FindObjectOfType<VitalsController>();
            if (vitals != null)
            {
                vitals.AddCredits(currentGroup.groupCreditReward);
                currentGroup.rewardGiven = true;
            }
            
            if (NotificationManager.Instance != null)
            {
                NotificationManager.Instance.ShowNotification($"{currentGroup.groupName} Completed", 0, new Color(0f, 0.972f, 1f));
            }
            
            Invoke("NextGroup", 1.0f);
        }

        UpdateUI();
    }

    private void NextGroup()
    {
        currentGroupIndex++;
        UpdateUI();
    }

    private int GetItemCount(string itemName)
    {
        if (InventoryHandler.Instance == null) return 0;

        int count = 0;
        for (int i = 0; i < InventoryHandler.Instance.GetMaxSlots(); i++)
        {
            var item = InventoryHandler.Instance.GetItem(i);
            if (item != null && item.itemName == itemName) count += item.quantity;
        }
        return count;
    }

    // --- VYKRESLOVÁNÍ DO UI ---
    private void UpdateUI()
    {
        // Vymazání starých textů
        foreach (Transform child in questListParent) 
        {
            Destroy(child.gameObject);
        }

        if (currentGroupIndex >= questLine.Count)
        {
            GameObject obj = Instantiate(questTextPrefab, questListParent);
            obj.GetComponent<TMP_Text>().text = "<color=#00f7f6>All quests completed.</color>";
            return;
        }

        QuestGroup currentGroup = questLine[currentGroupIndex];
        if (questGroupUI != null) questGroupUI.GetComponent<TMP_Text>().text = currentGroup.groupName;

        // Vykreslení aktuálních úkolů
        foreach (var q in currentGroup.questsInGroup)
        {
            GameObject obj = Instantiate(questTextPrefab, questListParent);
            TMP_Text txt = obj.GetComponent<TMP_Text>();

            string questText = $"- {q.questDescription}";
        
            // Zobrazení čísel pro Input Questy
            if (q.requiredKeys != null && q.requiredKeys.Count > 0)
            {
                if (q.requiredKeys.Count > 1) questText += $" {q.pressedKeys.Count}/{q.requiredKeys.Count}";
            }
            // Zobrazení čísel pro Akční Questy
            else if (!string.IsNullOrEmpty(q.targetActionName))
            {
                if (q.actionGoalAmount > 1) questText += $" {q.currentActionCount}/{q.actionGoalAmount}";
            }
            // Zobrazení čísel pro Item Questy
            else if (!string.IsNullOrEmpty(q.targetItemName))
            {
                int displayAmount = q.isCompleted ? q.goalAmount : Mathf.Min(GetItemCount(q.targetItemName), q.goalAmount);
                questText += $" {displayAmount}/{q.goalAmount}";
            }

            // Přeškrtnutí hotových úkolů
            if (q.isCompleted)
            {
                txt.text = $"<color=#00f7f6><s>{questText}</s></color>";
            }
            else
            {
                txt.text = questText;
            }
        }
    }
}