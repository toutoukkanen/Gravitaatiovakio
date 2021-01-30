using UnityEngine;

namespace Enemy
{
    
    public sealed class Dart : EnemyLogic
    {
        protected override void Start()
        {
            base.Start(); // Base start contains necessary steps

            movementStrategy = new AdvancedMovementStrategy(this, maxAcceleration:3f, maxAngularAcceleration:30f, 
                accelerationTime:1.2f, optimalDistance: 20f, moveAroundFactor: 0.03f, objectDimensions:objectDimensions);
            shootingStrategy = new AdvancedShootingStrategy(this, maxDistanceToHit: 1.5f, predictAngleMax: 45f, predictTimeIncrement:0.1f);
        }
        
        // Here we decide which strategy to use based on several conditions
        private void Update() 
        {
            // Decide which strategy to use based on distance, health, etc.
            var distanceFromEnemyToPlayer = Vector3.Magnitude(transform.position - player.transform.position);

            // If total health is lower than treshold or the ship lost all weapons
            if ((_section.ShipHp / _section.MaxShipHp) < criticalHealthTreshold || _section.weapons.Count == 0)
            {
                Debug.Log("Critical damage to ship! Cannot operate anymore.");
                
                Die();
            }
            
            // If distance is long, switch to default strategy
            //else if (distanceFromEnemyToPlayer > 7)
            //{
            //    if (strategy is DefaultStrategy) return;
            //    
            //    Debug.Log("Switch to default strategy");
            //    strategy = new DefaultStrategy(this);
            //}
            
        }
        
    }
}