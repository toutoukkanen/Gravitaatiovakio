using System.Collections.Generic;
using UnityEngine;
using Weapons;

namespace Enemy
{
    public sealed class AdvancedShootingStrategy : ShootingStrategy
    {
        // These 5 floats must be positive when initializing
        private readonly float predictTimeIncrement; // Less = better accuracy = less performance
        private readonly float predictTimeMax; // More = better accuracy = less performance
        private float predictAngleIncrement; // Less = better accuracy = less performance
        private readonly float predictAngleMax; // More = better accuracy = less performance
        private readonly float maxDistanceToHit; // Less = better accuracy = less performance

        
        // Strategies themselves should have quirky fields that shouldn't be stored in weapons themselves
        // A dictionary to enable quick decisions to shoot precisely
        private readonly Dictionary<Weapon, bool> executingPreciseInstruction = new Dictionary<Weapon, bool>();
        
        // A strategy has to go trough the base strategy to get info about context
        // Constructor gives opportunity to fine tune the strategy's variables. So enemy and strategy "think" alike
        public AdvancedShootingStrategy(IContext context,
            float predictTimeIncrement = 0.2f,
            float predictTimeMax = 3f,
            float predictAngleIncrement = 0.5f,
            float predictAngleMax = 25f,
            float maxDistanceToHit = 3f) : base(context)
        {
            this.predictTimeIncrement = predictTimeIncrement;
            this.predictTimeMax = predictTimeMax;
            this.predictAngleIncrement = predictAngleIncrement;
            this.predictAngleMax = predictAngleMax;
            this.maxDistanceToHit = maxDistanceToHit;
        }
        
        // Gives advice on alignment based on projectile speed, player's speed etc.
        public override float AlignWeapon(Weapon weapon)
        {
            // if (weapon.name != "Omega_II") return 0f; // Just for debug
            if (executingPreciseInstruction.ContainsKey(weapon))
                return 0;
            
            var weaponTransform = weapon.ActualWeaponTransform;
            
            var playerVelocity = context.PlayerRigidbody2D.velocity;
            
            var predictedPlayerPosition = context.PlayerPos;

            var weaponToPredictedPlayer = predictedPlayerPosition - weaponTransform.position;
            
            var angle = Vector3.SignedAngle(weaponTransform.up, weaponToPredictedPlayer, Vector3.forward);
            
            var predictedWeaponAlignment = Vector2.zero;
            
            //var predictAngleIncrement = 0.5f;
            //var predictAngleMax = 25f;
            
            if (angle > 0) // Which way we want to predict to
            {
                predictAngleIncrement = -predictAngleIncrement;
            }
            
            // Try out every angle at every time point starting from exactly the gun pointing to the player
            for (var t = 0f; t < predictTimeMax; t += predictTimeIncrement) // Increase at reasonable steps
            {
                // Condition check must be done for the angle to remain between negative and positive angleCondition
                for (var angleModifier = 0f; Mathf.Abs(angleModifier) < predictAngleMax; angleModifier += predictAngleIncrement) // Increase at reasonable steps
                {
                    // Rotate weapon barrel by test modifier
                    predictedWeaponAlignment = Quaternion.Euler(0f,0f, angleModifier) * weaponTransform.up;

                    //Debug.Log(predictedWeaponAlignment);
                        
                    if (DoesHit(weaponTransform.position, context.PlayerPos, predictedWeaponAlignment * weapon.ProjectileSpeed + context.Velocity, playerVelocity, t))
                    {
                        //Debug.Log("Precise instruction found! Angle:  " + angleModifier + ", Time: " + t);
                            
                        var angleShipToWeapon = Vector3.SignedAngle(context.Transform.up, weaponTransform.up, Vector3.forward);

                        var lerpAngle = 0f;
                        
                        // Clamp rotation. Prevent from going over
                        // Don't add precise instruction if it's off the weapon's rotation
                        if (angleShipToWeapon + angleModifier > weapon.MAXTurnAngle)
                        {
                            // Interpolate angle for smooth rotation
                            lerpAngle = Mathf.LerpAngle(0, weapon.MAXTurnAngle - angleShipToWeapon, Time.deltaTime * weapon.TurningSpeed);
                            return lerpAngle;
                        }

                        if (angleShipToWeapon + angleModifier < -weapon.MAXTurnAngle)
                        {
                            // Interpolate angle for smooth rotation
                            lerpAngle = Mathf.LerpAngle(0, -weapon.MAXTurnAngle - angleShipToWeapon, Time.deltaTime * weapon.TurningSpeed);
                            return lerpAngle;
                        }

                        // Interpolate angle for smooth rotation
                        lerpAngle = Mathf.LerpAngle(0, angleModifier, Time.deltaTime * weapon.TurningSpeed);
                            
                        if(!executingPreciseInstruction.ContainsKey(weapon))
                            executingPreciseInstruction.Add(weapon, true);
                            
                        return lerpAngle;
                    }
                }
            }
            
            // If precise prediction failed, just aim the barrel in some way to the player
            
            var basicAngleShipToWeapon = Vector3.SignedAngle(context.Transform.up, weaponTransform.up, Vector3.forward);

            // Clamp rotation. Prevent from going over
            if (basicAngleShipToWeapon + angle > weapon.MAXTurnAngle || basicAngleShipToWeapon + angle < -weapon.MAXTurnAngle)
                return 0;
            
            // Interpolate angle for smooth rotation
            var basicLerpAngle = Mathf.LerpAngle(0, angle, Time.deltaTime * weapon.TurningSpeed);
            
            return basicLerpAngle;
        }

        // Returns true if projectile and player collide at specific time
        private bool DoesHit(Vector2 projectilePos, Vector2 playerPos, Vector2 projectileVelocity, Vector2 playerVelocity, float time)
        {
            var projectileEndPos = projectilePos + projectileVelocity * time;
            var playerEndPos = playerPos + playerVelocity * time;

            var distanceBetweenPositions = Mathf.Abs(Vector2.Distance(projectileEndPos, playerEndPos));

            //Debug.Log(projectileVelocity);
            
            //if(distanceBetweenPositions < 15)
            //    Debug.Log(distanceBetweenPositions);
            
            return distanceBetweenPositions < maxDistanceToHit;
        }
        
        public override bool ShouldShoot(Weapon weapon)
        {
            // Only allow precise shots
            if (!executingPreciseInstruction.ContainsKey(weapon)) return false;
            
            executingPreciseInstruction.Remove(weapon);
            return true;

        }
        
    }
}