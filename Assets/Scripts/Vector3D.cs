using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.Scripts
{
    [Serializable]
    public class Vector3D
    {
        public Vector3D(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public float X { get; }
        public float Y { get; }
        public float Z { get; }

        public double Mod()
        {
            return Math.Sqrt(X * X + Y * Y + Z * Z);
        }

        public static Vector3D operator +(Vector3D lhs, Vector3D rhs)
        {
            return new Vector3D(lhs.X + rhs.X, lhs.Y + rhs.Y, lhs.Z + rhs.Z);
        }

        public static Vector3D operator -(Vector3D lhs, Vector3D rhs)
        {
            return new Vector3D(lhs.X - rhs.X, lhs.Y - rhs.Y, lhs.Z - rhs.Z);
        }

        public static Vector3D operator *(Vector3D lhs, float rhs)
        {
            return new Vector3D(lhs.X * rhs, lhs.Y * rhs, lhs.Z * rhs);
        }

        public Vector3 GetVector3()
        {
            return new Vector3(X, Y, Z);
        }

        public static Vector3D make(Vector3 template)
        {
            return new Vector3D(template.x, template.y, template.z);
        }
    }
}
