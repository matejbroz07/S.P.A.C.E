using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Quest", menuName = "Quests/Quest")]
public class QuestData : ScriptableObject
{
    public string questDescription;
    
    [Header("Item Requirement (Leave empty if Input Quest)")]
    public string targetItemName;
    public int goalAmount;

    [Header("Input Requirement (Optional)")]
    public List<KeyCode> requiredKeys; 
    [HideInInspector] public List<KeyCode> pressedKeys = new List<KeyCode>();

    // --- NOVÉ: AKČNÍ QUESTY (Stavění, Pokládání atd.) ---
    [Header("Action Requirement (Optional)")]
    public string targetActionName;     // Jak se akce jmenuje (např. "PlaceFlag", "BuildSolar")
    public int actionGoalAmount;        // Kolikrát to musí udělat (např. postavit 3 soláry)
    [HideInInspector] public int currentActionCount; // Kolik už toho udělal

    [HideInInspector] public bool isCompleted;
}