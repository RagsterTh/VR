using UnityEngine;

[CreateAssetMenu(fileName = "NPCData", menuName = "Data/NPC Data")]
public class NPCData : ScriptableObject
{
    public string npcName;
    public string[] dialogues;
    public float textSpeed;
    

}
