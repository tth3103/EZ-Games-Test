using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class DealDamage : MonoBehaviour
{
    [SerializeField] AttackType type;
    public float damage = 10f;
    [SerializeField] string targetTag;
    GameObject owner;
    private void Start()
    {
        if (owner == null)
        {
            owner = transform.root.gameObject;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(targetTag))
        {
            AI ai = other.GetComponent<AI>();
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                player.TakeDamage(damage,type);
                //Debug.Log($"{owner.name} hit Player with {damage} damage!");
            }
            if(ai != null)
            {
                ai.TakeDamage(damage,type);
                //Debug.Log($"{owner.name} hit {other.name} with {damage} damage!");
            }
        }
    }
    public void SetOwner(GameObject newOwner)
    {
        owner = newOwner;
    }
    public void SetTargetTag(string newTargetTag)
    {
        targetTag = newTargetTag;
    }
}
