using UnityEngine;

namespace Enemy
{
    
    // WhiteStar is the one ship you shouldn't have messed with
    // No one ever lived to tell about it
    
    public sealed class WhiteStar : EnemyLogic
    {
        protected override void Start()
        {
            base.Start(); // Base start contains necessary steps

            movementStrategy = new AdvancedMovementStrategy(this, objectDimensions:objectDimensions);
            shootingStrategy = new AdvancedShootingStrategy(this, maxDistanceToHit: 2f, predictAngleMax: 45f);
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