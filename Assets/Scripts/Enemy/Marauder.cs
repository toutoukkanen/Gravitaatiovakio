using UnityEngine;

namespace Enemy
{
    
    // Marauder is an enemy type which inherits EnemyLogic
    // The Marauder isn't that intelligent but he sure can fly
    
    public sealed class Marauder : EnemyLogic
    {
        
        protected override void Start()
        {
            base.Start(); // Base start contains necessary steps
            
            movementStrategy = new AdvancedMovementStrategy(this, objectDimensions:objectDimensions, maxAngularAcceleration:5f);
            shootingStrategy = new AdvancedShootingStrategy(this, maxDistanceToHit:2f, predictTimeIncrement:0.1f);
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
            
            // If there are not much weapons left, change to CQC strategy
            //else if (_section.weapons.Count == 1)
            //{
            //    if (strategy is CQCStrategy) return; // Don't reassign to same strategy
            //    
            //    Debug.Log("Critical damage to weapons. Change to CQC strategy");
            //    strategy = new CQCStrategy(this);
            //}
            
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