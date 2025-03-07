using System.Collections;
using UnityEngine;

public abstract class Enemy : MonoBehaviour
{
    public Transform[] players = new Transform[4];
    public Transform followingPlayer;

    void Start()
    {
        StartCoroutine(FindClose(players));
    }
    protected abstract IEnumerator FindClose(Transform[] players);
}
