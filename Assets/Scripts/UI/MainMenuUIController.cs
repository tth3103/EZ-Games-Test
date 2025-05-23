using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
public class MainMenuUIController : MonoBehaviour
{
    [SerializeField] GameObject selectModePanel;
    public int lastSelectedMode;
    private void Start()
    {
        lastSelectedMode = PlayerPrefs.GetInt("LastSelectedMode",0);
    }
    public void StartGame()
    {
        selectModePanel.SetActive(true);
    }
    public void ExitGame()
    {
        Application.Quit();
        //Debug.Log("Exit Game");
    }
    public void CloseSelectModePanel()
    {
        selectModePanel.SetActive(false);
    }
    public void SelectMode(int mode)
    {
        switch (mode)
        {
            case (int)GameMode.OneVsOne:
                PlayerPrefs.SetInt("GameMode",0);
                PlayerPrefs.SetInt("LastSelectedMode", 0);
                break;
            case (int)GameMode.OneVsMany:
                PlayerPrefs.SetInt("GameMode", 1);
                PlayerPrefs.SetInt("LastSelectedMode", 1);
                break;
            case (int)GameMode.ManyVsMany:
                PlayerPrefs.SetInt("GameMode", 2);
                PlayerPrefs.SetInt("LastSelectedMode", 2);
                break;
        }
        //Reset level if player picking different mode than last time
        if(lastSelectedMode != mode)
        {
            PlayerPrefs.SetInt("CurrentLevel",1);
        }
        SceneManager.LoadScene(1);
    }
}
