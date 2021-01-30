using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public struct ShipUpgrade
{
    public GameObject upgradeChoiceGameObject;
    public string upgradeText;
}

[Serializable]
public struct ShipUpgradeBundle
{
    public GameObject ship;
    public ShipUpgrade[] shipUpgrades;
}

[Serializable]
public struct ChoicePanel
{
    public GameObject choicePanelGameObject;
    public GameObject upgradeTextGameObject;
}

public class UpgradeMenu : MonoBehaviour
{
    private LevelManager _levelManager;
    
    [SerializeField] private List<ShipUpgradeBundle> shipUpgradeBundles;

    [SerializeField] private List<ChoicePanel> choicePanels;

    private ShipUpgradeBundle _chosenShipUpgradeBundle;
    
    private UnityEngine.Events.UnityAction _buttonCallback;
    
    private const int ShipScaleX = 166;
    private const int ShipScaleY = 50;
    private const int ShipOffsetY = 40;
    
    // Start is called before the first frame update
    void Start()
    {
        _levelManager = GameObject.FindWithTag("LevelManager").GetComponent<LevelManager>();

        foreach (var shipUpgradeBundle in shipUpgradeBundles.Where(shipUpgradeBundle => shipUpgradeBundle.ship == _levelManager.CurrentShip))
        {
            _chosenShipUpgradeBundle = shipUpgradeBundle;
        }
        
        // Wire the bundle for UI

        for (int i = 0; i < choicePanels.Count; i++)
        {
            if (i >= _chosenShipUpgradeBundle.shipUpgrades.Length) continue;
            if (_chosenShipUpgradeBundle.shipUpgrades[i].upgradeChoiceGameObject == null) continue;
            
            var choicePanel = choicePanels[i];
            var shipUpgrade = _chosenShipUpgradeBundle.shipUpgrades[i];
            
            var upgradedShip = Instantiate(shipUpgrade.upgradeChoiceGameObject, choicePanel.choicePanelGameObject.transform, true);
            upgradedShip.transform.localScale = new Vector3(ShipScaleX, ShipScaleY, 1); // Scale correctly on screen
            
            // Remove ability to fly or calculate rigity
            Destroy(upgradedShip.GetComponent<PlayerMovement>());
            Destroy(upgradedShip.GetComponent<Section>());
            
            // Wire the upgrade text
            choicePanel.upgradeTextGameObject.GetComponent<TextMeshProUGUI>().SetText(shipUpgrade.upgradeText);

            // Wire ui buttons with a callback
            _buttonCallback = () => SetPlayerShip(shipUpgrade.upgradeChoiceGameObject);
            choicePanel.choicePanelGameObject.GetComponent<Button>().onClick.AddListener(_buttonCallback);

        }
    }
    
    // Forward the request for debugging purposes
    public void NextLevel() => _levelManager.NextLevel();

    public void SetPlayerShip(GameObject chosenShip)
    {
        _levelManager.CurrentShip = chosenShip;
        NextLevel();
    }

}
