using UnityEngine;

[System.Serializable]
public struct Dialogues 
{
    public string dialogueLine;
    public AudioClip dialogueVoice;
}

[CreateAssetMenu(fileName = "NPCData", menuName = "Data/NPC Data")]
public class NPCData : ScriptableObject
{
    public string npcName;
    public float textSpeed;
    public Dialogues[] dialogues;
    

}
