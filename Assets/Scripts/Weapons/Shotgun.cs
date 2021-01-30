using UnityEngine;
using Weapons;
using Random = UnityEngine.Random;

public class Shotgun : Weapon
{
    [SerializeField] private int pellets = 8;
    [SerializeField] private int spreadDegrees = 45;
    [SerializeField] private int recoilDegrees = 50;
    [SerializeField] private float recoilMagnitude = 3;
    
    // Shoot is called by PlayerMovement or by EnemyLogic
    public override void Shoot()
    {
        if (ONCooldown) return;

        var weaponPosition = ActualWeaponTransform.position;
        var weaponUp = (Vector2) ActualWeaponTransform.up;

        Vector2 velocity = Vector2.zero;
        int angle = 0;
        
        for (int i = 0; i < pellets; i++)
        {
            // Get first projectile that is inactive and recycle it
            projectile = GetFirstFromPool();

            projectile.transform.position = weaponPosition;
            projectile.transform.rotation = ActualWeaponTransform.rotation;
            projectile.SetActive(true);
        
            projectile.layer = projectileLayer;

            // RigidBody lookup
            var rigidBody2D = projectileRigidBodies[projectile];
            
            // Movement is relative
            // Make the projectile match the speed of the ship shooting it
            rigidBody2D.velocity = _parentRigidBody2D.velocity;
            rigidBody2D.angularVelocity = _parentRigidBody2D.angularVelocity;
        
            // Now add a force to launch the projectile
            // To enable more precise enemy shooting, just add some velocity
            // Add velocity to the direction of the barrel and multiply it by speed
            velocity = weaponUp * ProjectileSpeed;
            
            // Get a random angle
            angle = Random.Range(-spreadDegrees, spreadDegrees);
            
            // Rotate the vector to point to the random angle
            velocity = Quaternion.Euler(0, 0, angle) * velocity;
        
            rigidBody2D.velocity += velocity;
            
        }
        
        // Also direct a force to the section shooting the weapon
        //_parentRigidBody2D.velocity -= weaponUp * projectileSpeed;
        //_parentRigidBody2D.AddForce(-force, ForceMode2D.Impulse);
        var projectileForce = velocity * _projectileMass; 
        _parentRigidBody2D.AddForceAtPosition(-projectileForce, weaponPosition, ForceMode2D.Impulse);
        
        ONCooldown = true;
        
        // Add a recoil effect to the camera
        
        var clampedRecoil = Vector3.ClampMagnitude(projectileForce, recoilMagnitude);

        // Get a random angle
        angle = Random.Range(-recoilDegrees, recoilDegrees);

        // Rotate the vector to point to a random angle
        clampedRecoil = Quaternion.Euler(0, 0, angle) * clampedRecoil;

        // Give some recoil to the camera
        _cameraFollow.recoil += (Vector3) (-clampedRecoil);

        // Play the shooting sound
        _audioSource.Play();
    }
}