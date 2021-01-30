using System;
using System.Collections.Generic;
using UnityEngine;

// A part is really just a single part.
// Parts don't have rigidbodies. They are placed in parent's section
public class Part : MonoBehaviour
{
    private Section _section;
    private Material _material;
    private static readonly int ColorTemperature = Shader.PropertyToID("_ColorTemperature");
    
    // Integrityvector defines the part's ability to resist momentum (damage)
    // Integrityvector's ability to resist comes directly from it's length and direction
    [SerializeField] private List<Vector2> integrityVectors = new List<Vector2>();
    
    [SerializeField] private bool resistOnlyInPositiveDirection = false; // By default allow parallel vectors with opposite signs

    [SerializeField] private float hp = 100;
    public float Hp => hp;
    public float MaxHP => maxHp;
    
    [SerializeField] private float maxHp;
    public float mass = 1; // Used to construct sum of mass in a Section

    private bool _sentPartToIntegrityCheck = false;

    public void Start()
    {
        // Early fail from missing section
        _section = GetComponentInParent<Section>();

        maxHp = hp;

        _material = GetComponent<SpriteRenderer>().material;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        //if (!transform.parent.CompareTag("Player")) return;
        
        //Debug.Log("This: " + gameObject.name + ", " + collision.relativeVelocity);
        
        // Momentum is velocity times mass. Mass is the whole mass of the system
        // This functionality requires that every object has a rigidbody
        var momentum = collision.relativeVelocity * collision.gameObject.GetComponent<Rigidbody2D>().mass;
        //Debug.Log("Momentum: " + momentum.normalized);

        // Integrityvector doesn't change according to global coordinates
        // So rotate it to match our orientation
        List<Vector2> globalIntegrityVectors = new List<Vector2>();
        foreach (var integrityVector in integrityVectors)
        {
            // Note that transform.rotation takes to account parent's rotation
            globalIntegrityVectors.Add(Quaternion.Euler(0, 0, transform.rotation.eulerAngles.z) * integrityVector);
        }
        
        // Now calculate damage reduction
        // Calculate the cross product that gives the "difference in angle" in some sense
        // The less the better resistance
        
        // Also now we want to know if the part is supposed to only resist in the specified positive direction
        // Two vectors can be parallel but different directions
        float damageMultiplier = 1f;
        Vector2 closestIntegrityVector = Vector2.zero;

        foreach (var globalIntegrityVector in globalIntegrityVectors)
        {
            var currentDamageMultiplier = 1f;
            if (resistOnlyInPositiveDirection)
            {
                // If momentum and integrityvector are on the same side
                if (Vector3.Dot(globalIntegrityVector, momentum.normalized) < 0)
                {
                    // Measure the difference in "angle"
                    currentDamageMultiplier = Math.Abs(Vector3.Cross(momentum.normalized, globalIntegrityVector.normalized).z);
                }
            }
            else // This way doesn't take to account the direction. Only if they are parallel
            {
                currentDamageMultiplier = Math.Abs(Vector3.Cross(momentum.normalized * momentum.normalized,
                    globalIntegrityVector.normalized * globalIntegrityVector.normalized).z);
            }
            
            if (currentDamageMultiplier < damageMultiplier)
            {
                damageMultiplier = currentDamageMultiplier;
                closestIntegrityVector = globalIntegrityVector;
            }
        }
        
        damageMultiplier += closestIntegrityVector.magnitude;
        
        //Debug.Log("Length of integrityVector: " + closestIntegrityVector.magnitude);
        if (damageMultiplier > 1f) // Prevent overage
            damageMultiplier = 1f;
        
        //Debug.Log("DamageMultiplier: " + damageMultiplier);
        
        // Remember, integrity means the resilience to damage in some angle
        //var resultMomentum = momentum / globalIntegrityVector1;
        var scalarResultMomentum = Math.Abs(momentum.magnitude * damageMultiplier);
        //Debug.Log("Result momentum:" + scalarResultMomentum);
        
        // Reduce hp by scalar result momentum
        hp -= scalarResultMomentum;

        if(hp <= 0)
            Die();
        else if (hp <= maxHp / 2)
            _material.SetFloat(ColorTemperature, 1f);

        
    }

    private void Die()
    {
        // Sometimes the collision is registered while the object is already disabled
        if (gameObject.activeSelf && !_sentPartToIntegrityCheck)
        {
            _section.destroyedParts.Add(transform);
            _sentPartToIntegrityCheck = true;
        }
    }
    
}
