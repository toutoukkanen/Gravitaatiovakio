using System.Collections.Generic;
using UnityEngine;
using Weapons;

namespace Enemy
{
    public interface IContext
    {
        // Ship info
        Transform Transform { get; }
        Vector2 Velocity { get; }
        float AngularVelocity { get; }

        // Weapons info
        List<Weapon> Weapons { get; }

        // Player info
        Vector3 PlayerPos { get; }
        Rigidbody2D PlayerRigidbody2D { get; }
    }
}

