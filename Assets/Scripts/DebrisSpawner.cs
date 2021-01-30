using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

[Serializable]
public struct Debris
{
    [SerializeField] public GameObject gameObject;
    [SerializeField] public int probability;
}

public class DebrisSpawner : MonoBehaviour
{
    [SerializeField] private List<Debris> debrisList;

    [SerializeField] private List<GameObject> spawnedDebris = new List<GameObject>();
    
    [SerializeField] private int maxDebris = 0; 
    [SerializeField] private float minDistance = 0;
    [SerializeField] private float maxDistance = 0;

    [SerializeField] private float spawnCooldown = 1f;
    private float currentCooldoown = 0f;
    private bool isOnCooldown = false;

    private GameObject player;
    private Camera _camera;
    
    // Start is called before the first frame update
    void Start()
    {
        _camera = Camera.main;
        
        player = GameObject.FindWithTag("Player");
        
        UpdateDistances();
    }

    // Set distances so debris won't spawn light years away or worse, under the players eyes
    void UpdateDistances(float minModifier = 1.2f)
    {
        if(minDistance <= 0)
            minDistance = _camera.sensorSize.x * minModifier;

        if (maxDistance < minDistance)
            maxDistance = minDistance * 2;
    }

    // Update is called once per frame
    void Update()
    {
        if (maxDebris <= 0) return;
        
        if (isOnCooldown)
        {
            if (currentCooldoown > spawnCooldown)
            {
                currentCooldoown = 0;
                isOnCooldown = false;
            }
            else
            {
                currentCooldoown += Time.deltaTime;
                return;
            }
        }
        
        SpawnDebris();

        // Don't pollute the whole game with debris
        while (spawnedDebris.Count > maxDebris)
        {
            Destroy(spawnedDebris.First());
            spawnedDebris.Remove(spawnedDebris.First());
        }
        
        isOnCooldown = true;
    }

    private async Task<int> RollDebrisIndex(int index)
    {

        if (debrisList[index].probability >= 100)
            return index; // Maximum probability
        else if (debrisList[index].probability <= 0)
        {
            return await RollDebrisIndex(Random.Range(0, debrisList.Count)); // Roll a new one
        }
        
        // Classic probability
        var randomNumber = Random.Range(0, 101);
        if (randomNumber <= debrisList[index].probability)
            return index;
        else
            return await RollDebrisIndex(Random.Range(0, debrisList.Count)); // Roll a new one
        
    }
    
    private async void SpawnDebris()
    {
        // Choose a random debris to (try to) spawn
        var randomIndex = Random.Range(0, debrisList.Count);

        var getDebrisIndexTask = RollDebrisIndex(randomIndex);
        
        Vector2 spawnPos = player.transform.position;
        
        // Get a random angle
        var angle = Random.Range(0, 360);
        
        // Construct a vector
        Vector2 spawnDirection = Vector2.up;
        
        // Rotate the vector to point to the random angle
        spawnDirection = Quaternion.Euler(0, 0, angle) * spawnDirection;

        // Update distances cause camera can resize
        UpdateDistances();
        
        var distance = Random.Range(minDistance, maxDistance);
        // Debris spawn position is the random unit vector multiplied by distance
        spawnPos += spawnDirection * distance;
        
        // Get a random angle for debris rotation
        angle = Random.Range(0, 360);
        
        var debrisIndex = await RollDebrisIndex(randomIndex);
        
        GameObject debris = Instantiate(debrisList[debrisIndex].gameObject, spawnPos, Quaternion.Euler(0, 0, angle));
        spawnedDebris.Add(debris);
    }
    
}
