using System;
using System.Collections.Generic;
using UnityEngine;
using Weapons;

// Logic uses different strategies dependent on the situation
// The logic asks for advice from strategies. This advice is taken into consideration when acting

// EnemyLogic is abstract because each enemy should really differ from each other by overriding or extending member functions

namespace Enemy
{
    public abstract class EnemyLogic : MonoBehaviour, IContext
    {
        protected GameObject player;
        protected Rigidbody2D _rigidbody2D;
        protected Rigidbody2D _playerRigidbody2D;
        protected Section _section;
        
        protected ObjectDimensions objectDimensions;
        
        protected MovementStrategy movementStrategy;
        protected ShootingStrategy shootingStrategy;

        [SerializeField] protected float criticalHealthTreshold = 0.5f; // Which HP divided by MaxHp is the lowest possible
        
        protected bool shootingEnabled = false;
        protected float shootingEnabledTimer = 2.5f;

        // Properties for fields to implement IContext. Used to give strategies the necessary info
        
        // Ship
        public Transform GetTransform() => transform;
        public Vector2 GetVelocity() => _rigidbody2D.velocity;
        public float GetAngularVelocity() => _rigidbody2D.angularVelocity;

        // Weapons
        public List<Weapon> GetWeapons() => _section.weapons;

        // Player
        public Vector3 GetPlayerPos() => player.transform.position;

        public Rigidbody2D GetPlayerRigidbody2D() => _playerRigidbody2D;

        // Events
        public event EventHandler EnemyDestroyed; 

        // Start is called before the first frame update
        // Start should only be used as is or extended to prefer another strategy
        protected virtual void Start()
        {
            player = GameObject.FindWithTag("Player");
            _playerRigidbody2D = player.GetComponent<Rigidbody2D>();
            
            _rigidbody2D = GetComponent<Rigidbody2D>();
            _section = GetComponent<Section>();

            objectDimensions = _section.CalculateShipDimensions(); // Get dimensions from section for strategies

            // Send the MonoBehaviour to strategy through the specific IContext
            // It would be catastrophic for strategies to access a MonoBehaviour directly
            // If there is a preferred strategy for enemy, override it in child
            //movementStrategy ??= new MovementStrategy(this); // C#8 only
            //shootingStrategy ??= new ShootingStrategy(this);
            
            if(movementStrategy == null)
                movementStrategy = new MovementStrategy(this);
            
            if(shootingStrategy == null)
                shootingStrategy = new ShootingStrategy(this);
            
            Invoke(nameof(EnableFighting), shootingEnabledTimer);
        }

        private void FixedUpdate()
        {
            // Call the current strategy for advice on movement
            var advisedAcceleration = movementStrategy.Move();
            var advisedAngularAcceleration = movementStrategy.Rotate();
            
            _rigidbody2D.velocity += advisedAcceleration;
            _rigidbody2D.angularVelocity += advisedAngularAcceleration;

            if (!shootingEnabled) return;
            // Align weapons and shoot if advised to do so
            // Only ask advice to shoot if not on coolcown
            foreach (var weapon in _section.weapons)
            {
                if (weapon.ONCooldown) continue; // Don't do anything on cooldown
                
                if(weapon.TurningSpeed != 0) // Gun can be stationary
                    weapon.ActualWeaponTransform.Rotate(Vector3.forward, shootingStrategy.AlignWeapon(weapon));
                //weapon.transform.rotation = Quaternion.AngleAxis(strategy.AlignWeapon(weapon), Vector3.forward);
                
                if(!weapon.ONCooldown && shootingStrategy.ShouldShoot(weapon))
                    weapon.Shoot();
            }
            
        }
        
        protected void EnableFighting() => shootingEnabled = true;

        protected void Die()
        {
            // TODO: Do some cool explosion animations or something and then end level
            
            EnemyDestroyed?.Invoke(this,EventArgs.Empty);
            Destroy(this);
        }
    }
}