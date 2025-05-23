using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class HealthBarUI : MonoBehaviour
{
    public GameObject targetHP;
    [SerializeField] Slider hpSlider;
    [SerializeField] float maxHP;
    [SerializeField] float currentHP;
    void Start()
    {
        hpSlider = GetComponent<Slider>();
        maxHP = targetHP.GetComponent<AI>().GetMaxHP();
        currentHP = targetHP.GetComponent<AI>().GetCurrentHP();
        SetMaxHeath(maxHP);
        SetHealth(currentHP);
    }
    public void SetHealth(float value)
    {
        hpSlider.value = value;
    }
    public void SetMaxHeath(float value)
    {
        hpSlider.maxValue = value;
    }
}
