using UnityEngine;

namespace Enemy
{
    public class Shuttle : EnemyLogic
    {
        private int _strategyNumber = 0;
        
        protected override void Start()
        {
            base.Start(); // Base start contains necessary steps

            movementStrategy = new AdvancedMovementStrategy(this, optimalDistance: 2f, maxAcceleration:0.3f, 
                maxAngularAcceleration: 0.4f, objectDimensions:objectDimensions);
            shootingStrategy = new AdvancedShootingStrategy(this, maxDistanceToHit:1f);
        }
        
        // Here we decide which strategy to use based on several conditions
        private void Update() 
        {
            // Decide which strategy to use based on distance, health, etc.
            var distanceFromEnemyToPlayer = Vector3.Magnitude(transform.position - player.transform.position);

            // If total health is lower than treshold or the ship lost all weapons
            if ((_section.ShipHp / _section.MaxShipHp) < criticalHealthTreshold || _section.weapons.Count == 0) // 0
            {
                Debug.Log("Critical damage to ship! Cannot operate anymore.");
                Die();
            }
            
            // If distance is long, ease up on accuracy with a shotgun
            else if (distanceFromEnemyToPlayer > 20 && _strategyNumber != 1) 
            {
                shootingStrategy = new AdvancedShootingStrategy(this, maxDistanceToHit:7f);
                _strategyNumber = 1;
            }
            // Change back to stricter shooting
            else if (distanceFromEnemyToPlayer < 20 && _strategyNumber != 2) 
            {
                shootingStrategy = new AdvancedShootingStrategy(this, maxDistanceToHit:2f);
                _strategyNumber = 2;
            }
            
        }
    }
}

