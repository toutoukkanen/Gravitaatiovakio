using UnityEngine;
using Weapons;

namespace Enemy
{
    public class ShootingStrategy : Strategy
    {
        protected float predictModifier; // Used in simple shooting prediction. Otherwise just ignored
        
        // Used to shoot even more accurately than with the weapon's specs
        // This can be a pretty high number because the error can't go higher than the weapon's specs
        // Used in simple shooting prediction. Otherwise just ignored
        protected float maxWeaponAngleError; 
        
        public ShootingStrategy(IContext context, float predictModifier = 0.5f, float maxWeaponAngleError = 50f) : base(context)
        {
            this.maxWeaponAngleError = maxWeaponAngleError;
            this.predictModifier = predictModifier;
        }
        
        // Simple method that tries to predict where we should align the weapons to
        // Other strategies may offer more precise strategies in aligning weapons
        public virtual float AlignWeapon(Weapon weapon)
        {
            var weaponTransform = weapon.ActualWeaponTransform;
            var weaponTransformUp = weaponTransform.up;
            
            var predictedPlayerPosition = context.PlayerPos + (Vector3) context.PlayerRigidbody2D.velocity * predictModifier;
            var weaponToPredictedPlayer = predictedPlayerPosition - weaponTransform.position;

            var predictedAngle = Vector3.SignedAngle(weaponTransformUp, weaponToPredictedPlayer, Vector3.forward);
            
            var angleShipToWeapon = Vector3.SignedAngle(context.Transform.up, weaponTransformUp, Vector3.forward);

            // Clamp rotation. Prevent from going over
            if (angleShipToWeapon + predictedAngle > weapon.MAXTurnAngle || angleShipToWeapon + predictedAngle < -weapon.MAXTurnAngle)
                return 0;
            
            // Interpolate angle for smooth rotation
            var lerpAngle = Mathf.LerpAngle(0, predictedAngle, Time.deltaTime * weapon.TurningSpeed);
            
            return lerpAngle;
        }

        // Give advice on shooting if it's possible
        // This is usually overridden when AlignWeapon is also overridden
        public virtual bool ShouldShoot(Weapon weapon)
        {
            // var weaponTransform = weapon.ActualWeaponTransform;
            // 
            // var weaponToPlayer = context.GetPlayerPos() - weaponTransform.position;
            // var absAngle = Mathf.Abs(Vector3.SignedAngle(weaponTransform.up, weaponToPlayer, Vector3.forward));
            // 
            // // Use weapon's own error and strategy's preference to determine the result
            // // Strategy preference is used to advise on even greater precision than the weapon's own error
            // return absAngle < weapon.maxAngleError && absAngle < maxWeaponAngleError;
            return true;
        }
    }
}