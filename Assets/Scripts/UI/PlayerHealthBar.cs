using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class PlayerHealthBar : MonoBehaviour
{
    GameObject player;
    Slider hpSlider;
    float maxHP;
    float currentHP;
    void Start()
    {
        player = GameObject.Find("Main");
        hpSlider = GetComponent<Slider>();
        maxHP = player.GetComponent<PlayerController>().GetMaxHP();
        currentHP = player.GetComponent<PlayerController>().GetCurrentHP();
        SetMaxHP(maxHP);
        SetCurrentHP(currentHP);
    }

    // Update is called once per frame
    void Update()
    {
        GetPlayerCurrentHP();
    }
    void SetCurrentHP(float value)
    {
        hpSlider.value = value;
    }
    void SetMaxHP(float value)
    {
        hpSlider.maxValue = value;
    }
    public void GetPlayerCurrentHP()
    {
        currentHP = player.GetComponent<PlayerController>().GetCurrentHP();
        SetCurrentHP(currentHP);
    }
}
