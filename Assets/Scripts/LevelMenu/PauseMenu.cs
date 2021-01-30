using System;
using Enemy;
using UnityEngine;

public class PauseMenu : MonoBehaviour
{
    private LevelManager _levelManager;
    public GameObject pausePanel;

    private PlayerMovement _playerMovement;
    
    // Start is called before the first frame update
    void Start()
    {
        _levelManager = GameObject.FindWithTag("LevelManager").GetComponent<LevelManager>();
        _playerMovement = GameObject.FindWithTag("Player").GetComponent<PlayerMovement>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            ShowPauseMenu();
    }
    
    void ShowPauseMenu()
    {
        if (Time.timeScale < 1) return; // Don't pause when match over
        
        Time.timeScale = 0;
        _playerMovement.enabled = false;
        pausePanel.SetActive(true);
    }

    public void Continue()
    {
        Time.timeScale = 1;
        _playerMovement.enabled = true;
        pausePanel.SetActive(false);
    }
    
    public void BackToMainMenu()
    {
        _levelManager.GameOver();
    }
}
