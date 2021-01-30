using System;
using System.Collections.Generic;
using System.Linq;
using Enemy;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelManager : MonoBehaviour
{
    [SerializeField] private GameObject currentEnemy;
    [SerializeField] private GameObject currentShip;
    private GameObject _instantiatedShip;

    private GameObject _youHaveWonPanel;
    private GameObject _youHaveLostPanel;
    
    [SerializeField] private List<string> levels; // Still no SceneAsset at runtime

    [SerializeField] private string currentScene;
    [SerializeField] private string lastScene;

    private const string MainMenuScene = "MainMenu";
    private const string UpgradeMenuScene = "UpgradeMenu";

    [SerializeField] private float winTimeSlowFactor = 0.5f;
    [SerializeField] private float timeUntilLevelChange = 5f;

    // Properties for upgrade menu
    public GameObject CurrentShip
    {
        get => currentShip;
        set => currentShip = value;
    }

    public string CurrentScene => currentScene;
    public string LastScene => lastScene;
    
    private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;

    // Start is called before the first frame update
    private void Start() => DontDestroyOnLoad(gameObject);

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log("OnSceneLoaded: " + scene.name);
        
        lastScene = currentScene;
        Debug.Log("Last scene: " + LastScene);

        currentScene = scene.name;
        
        if (currentScene != MainMenuScene && currentScene != UpgradeMenuScene)
        {
            // Instantiate player and listen to it's events
            _instantiatedShip = Instantiate(currentShip);
            _instantiatedShip.GetComponent<PlayerMovement>().PlayerDestroyed += PlayerDestroyedHandler;
        
            // Find enemy and listen to it's events
            currentEnemy = GameObject.FindWithTag("Enemy");
            if(currentEnemy != null)
                currentEnemy.GetComponent<EnemyLogic>().EnemyDestroyed += EnemyDestroyedHandler;
            
            _youHaveWonPanel =  GameObject.FindWithTag("WonPanel");
            _youHaveLostPanel = GameObject.FindWithTag("LostPanel");
        }
    }
    
    // Only done at the first time
    // GameObject received through UnityEvents
    public void StartGame(GameObject playerShip)
    {
        currentShip = playerShip;

        Debug.Log("Starting game with: " + CurrentShip.name);

        SceneManager.LoadScene(levels[0]);
    }

    // Go to upgrade menu
    private void EnemyDestroyedHandler(object sender, EventArgs e)
    {
        // Player can't win and lose at the same time
        _instantiatedShip.GetComponent<PlayerMovement>().PlayerDestroyed -= PlayerDestroyedHandler;
        
        _youHaveWonPanel.transform.GetChild(0).gameObject.SetActive(true);
        
        if(CurrentScene != levels.Last())
            Invoke(nameof(LevelComplete), timeUntilLevelChange);
        else
            Invoke(nameof(LevelComplete), timeUntilLevelChange * 1.5f);
        
        Time.timeScale = winTimeSlowFactor;
    }
    
    private void PlayerDestroyedHandler(object sender, EventArgs e)
    {
        // Player can't win and lose at the same time
        currentEnemy.GetComponent<EnemyLogic>().EnemyDestroyed -= EnemyDestroyedHandler;

        _youHaveLostPanel.transform.GetChild(0).gameObject.SetActive(true);
        
        Invoke(nameof(GameOver), timeUntilLevelChange);
        
        Time.timeScale = winTimeSlowFactor;
    }

    private void LevelComplete()
    {
        Time.timeScale = 1f;
        
        if(CurrentScene != levels.Last())
            SceneManager.LoadScene(UpgradeMenuScene);
        else
        {
            Debug.Log("Game end reached!");
            GameOver();
        }
    } 
    
    public void GameOver()
    {
        Time.timeScale = 1f;
        currentScene = "";
        lastScene = "";
        currentShip = null;
        currentEnemy = null;
        Destroy(gameObject);
        SceneManager.LoadScene(MainMenuScene);
    } 

    // Go to next level or end game
    public void NextLevel()
    {
        var lastLevelIndex = levels.FindIndex(x => x == LastScene);
        var nextLevelIndex = lastLevelIndex + 1;
            
        if(nextLevelIndex != levels.Count)
            SceneManager.LoadScene(levels[nextLevelIndex]);
    }
    
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;
    
}
