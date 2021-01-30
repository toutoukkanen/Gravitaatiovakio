using System.Collections.Generic;
using UnityEngine;

namespace Weapons
{
    public abstract class Weapon : MonoBehaviour //, IWeapon
    {
        protected Rigidbody2D _parentRigidBody2D; // Rigidbody at the core
        protected AudioSource _audioSource;
        protected Transform _transform;
        protected CameraFollow _cameraFollow;

        protected float _currentCooldown = 0f;
        [SerializeField] protected int projectilePoolSize = 10; // Expands when limit reached
        protected GameObject projectile;
        protected float _projectileMass;
        [SerializeField] protected List<GameObject> projectilePool;
        protected int projectileLayer = 0;

        // A dictionary for faster access to rigidbodies
        protected Dictionary<GameObject, Rigidbody2D> projectileRigidBodies;

        [SerializeField] protected GameObject projectilePrefab;
        [SerializeField] protected float projectileSpeed = 1f;
        [SerializeField] protected float shootingCooldown = 1f;
        [SerializeField] protected float turningSpeed = 1f; // Turning speed scales with Time.deltaTime
        [SerializeField] protected float maxTurnAngle = 45f;
    
        // Implement IWeapon properties
        public bool ONCooldown { get; set; }
    
        // Weapon might have base and a barrel which is the child
        public Transform ActualWeaponTransform { get; protected set; }

        public GameObject ProjectilePrefab
        {
            get => projectilePrefab;
            set => projectilePrefab = value;
        }

        public float ProjectileSpeed
        {
            get => projectileSpeed;
            set => projectileSpeed = value;
        }

        public float ShootingCooldown
        {
            get => shootingCooldown;
            set => shootingCooldown = value;
        }

        public float TurningSpeed
        {
            get => turningSpeed;
            set => turningSpeed = value;
        }

        public float MAXTurnAngle
        {
            get => maxTurnAngle;
            set => maxTurnAngle = value;
        }

        protected void Start()
        {
            _transform = transform;
        
            _parentRigidBody2D = gameObject.GetComponentInParent<Rigidbody2D>();
            _audioSource = GetComponent<AudioSource>();
            _projectileMass = ProjectilePrefab.GetComponent<Rigidbody2D>().mass;
            _cameraFollow = Camera.main.GetComponent<CameraFollow>();
            
            // Choose which layer the bullets will be added to
            if (transform.root.CompareTag("Player"))
                projectileLayer = LayerMask.NameToLayer("ProjectilePlayer");
            else
                projectileLayer = LayerMask.NameToLayer("ProjectileEnemy");
            
            ONCooldown = false; // Initialize with no cooldown
        
            // Detect if the weapon has a child component.
            // In example the weapon is the base and the child is the actual barrel of the weapon
            if (transform.childCount == 0)
            {
                ActualWeaponTransform = _transform;
            }
            else
            {
                // If a weapon has a base, it's on top of the ship. Spawn all projectiles on top too
                ActualWeaponTransform = transform.GetChild(0).transform;
                projectilePrefab.GetComponent<SpriteRenderer>().sortingLayerID = SortingLayer.NameToID("TopShip");
            }
            
            // Initialize object pool with specified size
            projectilePool = new List<GameObject>();
            projectileRigidBodies = new Dictionary<GameObject, Rigidbody2D>();

            ExpandPool(projectilePoolSize);
        }

        // Update is called once per frame
        void Update()
        {
            if (!ONCooldown) return;
        
            _currentCooldown += Time.deltaTime;
            if (_currentCooldown >= ShootingCooldown)
            {
                ONCooldown = false;
                _currentCooldown = 0f;
            }
        
        }

        protected void ExpandPool(int size)
        {
            GameObject temp;
            for (var i = 0; i < size; i++)
            {
                temp = Instantiate(ProjectilePrefab);
                temp.SetActive(false);
                projectilePool.Add(temp);
                projectileRigidBodies.Add(temp, temp.GetComponent<Rigidbody2D>());
            }
        }
    
        protected GameObject GetFirstFromPool()
        {
            //var temp = projectilePool.Find(x => !x.activeSelf);
            var index = projectilePool.FindIndex(x => !x.activeSelf);
        
            if (index != -1)
                return projectilePool[index];
            else
            {
                // Increase pool size with a reasonable amount
                ExpandPool(5);
                return projectilePool.Find(x => !x.activeSelf);
            }
        
        }
    
        // Shoot is called by PlayerMovement or by EnemyLogic
        public abstract void Shoot();
    }
}
