using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class AllyAI : AI,IPoolable
{
    [SerializeField] GameObject player;
    protected override void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        base.Start();
    }
    protected override void OnDefeatedAnimationComplete()
    {
        //Debug.Log("Disable Ally");
        gameObject.SetActive(false);
    }
}
