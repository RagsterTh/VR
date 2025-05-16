using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct Resources
{
    public ResourceTypes type;
    public GameObject resource;
}
public enum ResourceTypes
{
    Player, PlayerVR
}
[CreateAssetMenu(menuName = "SceneResources", fileName = "newResourcesList")]
public class SceneResources : ScriptableObject
{
    public Resources[] resources;
}
