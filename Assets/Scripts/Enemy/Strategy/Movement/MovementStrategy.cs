using UnityEngine;

namespace Enemy
{
    public class MovementStrategy : Strategy
    {
        protected float optimalDistance;
        protected float accelerationTime;
        protected float maxAcceleration;
        protected float maxAngularAcceleration;
        protected float turningTime;
        
        public MovementStrategy(IContext context,
            float optimalDistance = 10f,
            float accelerationTime = 1.5f,
            float maxAcceleration = 3f,
            float maxAngularAcceleration = 3f,
            float turningTime = 1f) : base(context)
        {
            this.optimalDistance = optimalDistance;
            this.accelerationTime = accelerationTime;
            this.maxAcceleration = maxAcceleration;
            this.maxAngularAcceleration = maxAngularAcceleration;
            this.turningTime = turningTime;
        }
        
        // Try to hover around player within an optimal distance
        public virtual Vector2 Move()
        {
            var position = context.Transform.position;
            
            var playerToEnemy =  position - context.PlayerPos;
            var optimalPlayerToEnemy = (Vector2) (context.PlayerPos + playerToEnemy.normalized * optimalDistance);
            
            var acceleration = (2 * optimalPlayerToEnemy - 2 * (Vector2) position - context.Velocity * (2 * accelerationTime)) / Mathf.Pow(accelerationTime,2);
            acceleration = Vector2.ClampMagnitude(acceleration, maxAcceleration);

            return acceleration;
        }
        
        // Try to "look at" player. Same mechanic as with the player
        public float Rotate()
        {
            var shipLookDirection = context.Transform.up;
            //Debug.Log(shipLookDirection);
            
            var enemyToPlayer = context.PlayerPos - context.Transform.position;
            var angleFromEnemyToPlayer = Vector3.SignedAngle(shipLookDirection, enemyToPlayer, Vector3.forward);
            //Debug.DrawRay(transform.position, shipToMouse, Color.white, 1f);
            
            var angleRad = angleFromEnemyToPlayer * Mathf.Deg2Rad;
            //Debug.Log(angleRad);
            
            var angularAcceleration = (2 * angleRad - 2 * context.AngularVelocity * Mathf.Deg2Rad * turningTime) / Mathf.Pow(turningTime,2);
            angularAcceleration = Mathf.Clamp(angularAcceleration, -maxAngularAcceleration, maxAngularAcceleration);

            return angularAcceleration;
        }
    }
}