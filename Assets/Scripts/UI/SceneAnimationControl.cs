using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneAnimationControl : MonoBehaviour
{
    [SerializeField] LevelManager levelManager;
    public void OnIntroAnimationComplete()
    {
        this.gameObject.SetActive(false);
    }
    private void OnOutroAnimationComplete()
    {
        StartCoroutine(levelManager.LoadOutro());
    }
}
