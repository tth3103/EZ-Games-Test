using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
public class LevelManager : MonoBehaviour
{
    [SerializeField] EnemySpawner enemySpawner;
    [Header("Components")]
    [SerializeField] GameObject victoryPanel;
    [SerializeField] GameObject lossPanel;
    [SerializeField] GameObject outroScene;
    [SerializeField] TextMeshProUGUI levelLabel;
    public int currentLevel;
    public int enemyDefeatedCounter = 0;
    public int enemyToDefeat = 0;
    bool playerDefeated = false;
    bool levelCompleted = true;
    bool outroLoad = false;
    bool endLevelCalled = false;
    void Start()
    {
        currentLevel = PlayerPrefs.GetInt("CurrentLevel",1);
        levelLabel.text = $"Level {currentLevel}";

        enemyToDefeat = (int)enemySpawner.GetEnemyAmountThisLevel();
        enemyDefeatedCounter = 0;
    }

    public void DefeatEnemy()
    {
        enemyDefeatedCounter++;
        if (enemyDefeatedCounter >= enemyToDefeat)
        {
            StartCoroutine(EndLevel());
        }
    }
    public void PlayerDefeated()
    {
        playerDefeated = true;
        levelCompleted = false;
        StartCoroutine(EndLevel());
    }
    public IEnumerator EndLevel()
    {
        if (endLevelCalled)
        {
            yield break;
        }
        endLevelCalled = true;
        outroScene.GetComponent<Animator>().enabled = true;
        
        yield return new WaitForSeconds(1f);
    }
    public IEnumerator LoadOutro()
    {
        outroLoad = true;
        yield return new WaitForSeconds(1f);
        if (levelCompleted && !playerDefeated)
        {
            victoryPanel.SetActive(true);
        }
        else
        {
            lossPanel.SetActive(true);
        }
    }
    public void LoadNextLevel()
    {
        currentLevel++;
        PlayerPrefs.SetInt("CurrentLevel", currentLevel);
        SceneManager.LoadScene(1);
    }
    public void ReturnToMenu()
    {
        currentLevel++;
        PlayerPrefs.SetInt("CurrentLevel", currentLevel);
        SceneManager.LoadScene(0);
    }
    public void Retry()
    {
        SceneManager.LoadScene(1);
    }
}
