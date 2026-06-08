using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "New Group", menuName = "Quests/Quest Group")]
public class QuestGroup : ScriptableObject
{
    public string groupName;
    public List<QuestData> questsInGroup;
    
    public float groupCreditReward = 50f;
    [HideInInspector] public bool rewardGiven = false;
    
    public bool IsGroupFinished()
    {
        return questsInGroup.All(q => q.isCompleted);
    }
}
