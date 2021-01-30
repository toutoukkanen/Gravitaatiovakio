using UnityEngine;
using Random = UnityEngine.Random;

namespace Weapons
{
    public class Turret : Weapon
    {
        [SerializeField] private int recoilDegrees = 50;
        [SerializeField] private float recoilMagnitude = 3;
        
        //private void Start()
        //{
        //
        //}
    
        // Shoot is called by PlayerMovement or by EnemyLogic
        public override void Shoot()
        {
            if (ONCooldown) return;

            var weaponPosition = ActualWeaponTransform.position;
            var weaponUp = (Vector2) ActualWeaponTransform.up;
        
            // Get first projectile that is inactive and recycle it
            projectile = GetFirstFromPool();

            projectile.transform.position = weaponPosition;
            projectile.transform.rotation = ActualWeaponTransform.rotation;
            projectile.SetActive(true);
        
            projectile.layer = projectileLayer; // Assign to the same layer as parent

            // RigidBody lookup
            var rigidBody2D = projectileRigidBodies[projectile];

            // Movement is relative
            // Make the projectile match the speed of the ship shooting it
            rigidBody2D.velocity = _parentRigidBody2D.velocity;
            rigidBody2D.angularVelocity = _parentRigidBody2D.angularVelocity;
        
            // Now add a force to launch the projectile
            // To enable more precise enemy shooting, just add some velocity
            // Add velocity to the direction of the barrel and multiply it by speed
            var velocity = weaponUp * ProjectileSpeed;
        
            rigidBody2D.velocity += velocity;
        
            // Also direct a force to the section shooting the weapon
            //_parentRigidBody2D.velocity -= weaponUp * projectileSpeed;
            //_parentRigidBody2D.AddForce(-force, ForceMode2D.Impulse);
            var projectileForce = velocity * _projectileMass; 
            _parentRigidBody2D.AddForceAtPosition(-projectileForce, weaponPosition, ForceMode2D.Impulse);
        
            // Play the shooting sound
            _audioSource.Play();
        
            ONCooldown = true;
            
            // If an enemy has a heavy weapon, player can feel it too
            if (transform.root.CompareTag("Player") || transform.CompareTag("HeavyWeapon"))
            {
                // Add a recoil effect to the camera

                var clampedRecoil = Vector3.ClampMagnitude(projectileForce, recoilMagnitude);

                // Get a random angle
                var angle = Random.Range(-recoilDegrees, recoilDegrees);

                // Rotate the vector to point to a random angle
                clampedRecoil = Quaternion.Euler(0, 0, angle) * clampedRecoil;

                // Give some recoil to the camera
                _cameraFollow.recoil += (Vector3) (-clampedRecoil);
            }

        }
    }
}
