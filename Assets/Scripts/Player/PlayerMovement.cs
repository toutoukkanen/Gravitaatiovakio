using System;
using UnityEngine;
using Weapons;

public class PlayerMovement : MonoBehaviour
{
    // Start is called before the first frame update
    private Rigidbody2D _rigidbody2D;
    
    public float mainThrusterPower = 1;
    public float sideThrusterPower = 1;
    public float brakeThrusterPower = 1f;
    public float turnTime = 1f;
    public float maxAngularAcceleration = 100f;

    public float criticalHealthTreshold = 0.5f;

    private Section _section;

    private Camera _mainCamera;

    // Events
    public event EventHandler PlayerDestroyed; 
    
    void Start()
    {
        _rigidbody2D = GetComponent<Rigidbody2D>();
        _section = GetComponent<Section>();
        _mainCamera = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.Mouse0))
        {
            foreach (var weapon in _section.weapons)
            {
                weapon.Shoot();
            }
        }

        // If total health is lower than treshold or the ship lost all weapons
        if ((_section.ShipHp / _section.MaxShipHp) < criticalHealthTreshold || _section.weapons.Count == 0)
        {
            Debug.Log("Critical damage to ship! Player lost.");
            PlayerDestroyed?.Invoke(this,EventArgs.Empty);
            Destroy(this);
        }
        
    }

    float AlignWeapon(Weapon weapon)
    {
        var weaponUp = weapon.ActualWeaponTransform.up;

        var weaponToMouse = _mainCamera.ScreenToWorldPoint(Input.mousePosition) -
                            weapon.ActualWeaponTransform.position;

        var angleWeaponToMouse = Vector3.SignedAngle(weaponUp, weaponToMouse, Vector3.forward);
            
        var angleShipToWeapon = Vector3.SignedAngle(transform.up, weaponUp, Vector3.forward);
            
        // Clamp rotation. Prevent from going over
        if (angleShipToWeapon + angleWeaponToMouse > weapon.MAXTurnAngle)
            angleWeaponToMouse = weapon.MAXTurnAngle - angleShipToWeapon;
        else if(angleShipToWeapon + angleWeaponToMouse < -weapon.MAXTurnAngle)
            angleWeaponToMouse = -weapon.MAXTurnAngle - angleShipToWeapon;
            
        var lerpAngle = Mathf.LerpAngle(0, angleWeaponToMouse, Time.deltaTime * weapon.TurningSpeed);
        return lerpAngle;
    }
    
    private void FixedUpdate()
    {
        Move();
        Rotate();

        foreach (var weapon in _section.weapons)
        {
            if(weapon.TurningSpeed != 0) // Gun can be stationary
                weapon.ActualWeaponTransform.Rotate(Vector3.forward, AlignWeapon(weapon));
        }
    }

    private void Move()
    {
        // Movement is done with basic rigidbody stuff
        
        float horizontalForce = Input.GetAxisRaw("Horizontal") * sideThrusterPower;
        
        var verticalForce = Input.GetAxisRaw("Vertical") > 0 ? 
            Input.GetAxisRaw("Vertical") * mainThrusterPower : Input.GetAxisRaw("Vertical") * brakeThrusterPower;
        
        _rigidbody2D.AddForce(new Vector2(horizontalForce,verticalForce));
    }

    private void Rotate()
    {
        // Rotation with rigidbody and in harmony with physics
        // This for example makes collision induced torque more dangerous for the player
        
        var shipToMouse = _mainCamera.ScreenToWorldPoint(Input.mousePosition) - transform.position;
        shipToMouse += Vector3.forward * 10; // Negate camera distance in z-axis
        //Debug.DrawRay(transform.position, mousePos, Color.green, 1f);
        
        var shipLookDirection = transform.up;
        //Debug.DrawRay(transform.position, shipLookDirection * 5, Color.red, 1f);
        
        var angleFromShipToMouse = Vector3.SignedAngle(shipLookDirection, shipToMouse, Vector3.forward);
        //Debug.DrawRay(transform.position, shipToMouse, Color.white, 1f);
        
        var angleRad = angleFromShipToMouse * Mathf.Deg2Rad;
        
        var angularAcceleration = (2 * angleRad - 2 * _rigidbody2D.angularVelocity * Mathf.Deg2Rad * turnTime) / Mathf.Pow(turnTime,2);
        angularAcceleration = Mathf.Clamp(angularAcceleration, -maxAngularAcceleration, maxAngularAcceleration);
        
        //Debug.Log(angularAcceleration);
        
        _rigidbody2D.angularVelocity += angularAcceleration;
    }
}
