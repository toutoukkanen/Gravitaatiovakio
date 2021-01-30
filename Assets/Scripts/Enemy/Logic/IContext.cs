using System.Collections.Generic;
using UnityEngine;
using Weapons;

namespace Enemy
{
    public interface IContext
    {
        // Refactored to C# 7 member functions from C# 8 interface properties
        // TODO: Return to C# 8 interface properties for a more sensible aprroach
        
        // Ship info
        Transform GetTransform();
        Vector2 GetVelocity();
        float GetAngularVelocity();

        // Weapons info
        // Every weapon must implement IWeapon
        List<Weapon> GetWeapons();

        // Player info
        Vector3 GetPlayerPos();
        Rigidbody2D GetPlayerRigidbody2D();
    }
}

