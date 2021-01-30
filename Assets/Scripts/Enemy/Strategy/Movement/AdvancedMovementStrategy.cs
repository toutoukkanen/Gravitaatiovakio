using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Enemy
{
    public sealed class AdvancedMovementStrategy : MovementStrategy
    {
        private readonly ObjectDimensions _objectDimensions;
        private float _moveAroundFactor;
        private float _initialMoveAroundFactor;
        //private readonly LayerMask _defaultLayerMask = LayerMask.NameToLayer("Default");
        
        public AdvancedMovementStrategy(IContext context, float optimalDistance = 10f, float moveAroundFactor = 0f, float accelerationTime = 1.5f,
            float maxAcceleration = 0.1f, float maxAngularAcceleration = 3f, float turningTime = 1f,
            ObjectDimensions objectDimensions = default)
            : base(context, optimalDistance, accelerationTime, maxAcceleration, maxAngularAcceleration, turningTime)
        {
            this._objectDimensions = objectDimensions;
            this._moveAroundFactor = moveAroundFactor;
            this._initialMoveAroundFactor = moveAroundFactor;
        }
        
        // Try to hover around player within an optimal distance
        // Also try to find obstacles and avoid them
        public override Vector2 Move()
        {
            var position = context.Transform.position;
            
            var playerToEnemy =  position - context.PlayerPos;
            
            // Some enemies can go around the player in a circular motion
            if (_moveAroundFactor != 0)
            {
                playerToEnemy = Quaternion.Euler(0, 0, _moveAroundFactor) * playerToEnemy;
                if (_moveAroundFactor >= 360)
                    _moveAroundFactor = 0f;
                _moveAroundFactor += _initialMoveAroundFactor;
            }
            
            var optimalPlayerToEnemy = (Vector2) (context.PlayerPos + playerToEnemy.normalized * optimalDistance);
            
            var maxEvadeAngle = 90;
            var evadeStep = 15;

            // If there is debris or other obstacles in the way, shift course
            if (ShipPathObstructed(position, optimalPlayerToEnemy, LayerMask.GetMask("Default", "Player")))
            {
                
                // Try positive angles
                for (var angle = 0; angle < maxEvadeAngle; angle += evadeStep)
                {
                    // Try another path
                    var newPath = Quaternion.Euler(0, 0, angle) * optimalPlayerToEnemy;
                    
                    if (ShipPathObstructed(position, newPath, LayerMask.GetMask("Default", "Player")))
                        continue;
                    
                    optimalPlayerToEnemy = newPath;
                    break;
                }
                
                // Try negative angles
                for (var angle = 0; angle > -maxEvadeAngle; angle -= evadeStep)
                {
                    // Try another path
                    var newPath = Quaternion.Euler(0, 0, angle) * optimalPlayerToEnemy;
                    
                    if (ShipPathObstructed(position, newPath, LayerMask.GetMask("Default", "Player")))
                        continue;
                    
                    optimalPlayerToEnemy = newPath;
                    break;
                }
                
                // If no valid path found, try go back
                //if (ShipPathObstructed(position, optimalPlayerToEnemy, LayerMask.NameToLayer("Default"), 3f))
                //{
                //    Debug.Log("Going back");
                //    optimalPlayerToEnemy = Quaternion.Euler(0, 0, 180) * optimalPlayerToEnemy;
                //}
            }
            
            // TODO: Check directly ahead if we are on "törmäyskurssi"
            
            var acceleration = (2 * optimalPlayerToEnemy - 2 * (Vector2) position - context.Velocity * (2 * accelerationTime)) / Mathf.Pow(accelerationTime,2);
            acceleration = Vector2.ClampMagnitude(acceleration, maxAcceleration);

            return acceleration;
        }
        
        private bool ShipPathObstructed(Vector2 startPosition, Vector2 endPosition, LayerMask layerMask)
        {
            // Check if any debris is on the way
            // Check for all 4 primary directions

            var startPositionsOnRadius = new List<Vector2>();
            startPositionsOnRadius.Add(startPosition + Vector2.up * _objectDimensions.up);
            startPositionsOnRadius.Add(startPosition + Vector2.up * _objectDimensions.down);
            startPositionsOnRadius.Add(startPosition + Vector2.right * _objectDimensions.right);
            startPositionsOnRadius.Add(startPosition + Vector2.right * _objectDimensions.left);
            
            var hits = new List<RaycastHit2D>();
            foreach (var startPositionOnShipRadius in startPositionsOnRadius)
            {
                hits.Add(Physics2D.Linecast(startPositionOnShipRadius, endPosition, layerMask));
                Debug.DrawLine(startPositionOnShipRadius, endPosition, Color.red, 0.2f);
            }
            
            // If any of the rays are obstructed
            return hits.Any(x => x.collider != null);

        }
    }
}