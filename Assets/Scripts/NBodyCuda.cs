using Assets.Scripts;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class NBodyCuda : MonoBehaviour
{
    [DllImport(@"D:\Faculty\PDPCourse\NBody\x64\Debug\NBody.dll", 
        EntryPoint = "calculatePosition")]
    private static extern Vector3[] calculatePosition(
        Vector3[] position, 
        Vector3[] velocity, 
        Vector3[] accelerations,
        float[] masses,
        float timeStep, 
        int size
    );

    public float gravitationalConstant = 1f;
    public Rigidbody prefab;
    public float softening = 0.1f;
    public int numberOfBodies = 3;
    public float timeStep = 0.01f;

    private float[] masses;
    private Vector3[] accelerations;
    private Rigidbody[] bodies;
    private Vector3[] positions;
    private Vector3[] velocities;
    private System.Random random = new System.Random();
    private bool run = false;

    // Start is called before the first frame update
    void Start()
    {
        bodies = new Rigidbody[numberOfBodies];
        masses = new float[numberOfBodies];
        positions = new Vector3[numberOfBodies];
        velocities = new Vector3[numberOfBodies];
        accelerations = new Vector3[numberOfBodies];

        for (int i = 0; i < numberOfBodies; i++)
        {
            masses[i] = GetRandomNumber(0, 0.2);
            positions[i] = new Vector3(
                GetRandomNumber(-2, 2), 
                GetRandomNumber(-2, 2), 
                GetRandomNumber(-2, 2)
            );
            velocities[i] = new Vector3(
                GetRandomNumber(-2, 2), 
                GetRandomNumber(-2, 2), 
                GetRandomNumber(-2, 2)
            );
        }

        var averageMass = masses.Average();
        var multiplied = new Vector3[numberOfBodies];

        for (int i = 0; i < numberOfBodies; i++)
        {
            multiplied[i].x = velocities[i].x * masses[i];
            multiplied[i].y = velocities[i].y * masses[i];
            multiplied[i].z = velocities[i].z * masses[i];
        }

        var averageMultipliedX = multiplied.Select(x => x.x).Average();
        var averageMultipliedY = multiplied.Select(x => x.y).Average();
        var averageMultipliedZ = multiplied.Select(x => x.z).Average();

        for (int i = 0; i < numberOfBodies; i++)
        {
            velocities[i] = new Vector3(
                velocities[i].x - (averageMultipliedX / averageMass),
                velocities[i].y - (averageMultipliedY / averageMass),
                velocities[i].z - (averageMultipliedZ / averageMass)
            );
        }

        velocities[0] = new Vector3(0, 0, 0);
        masses[0] = 100;

        for (int i = 0; i < numberOfBodies; i++)
        {
            bodies[i] = Instantiate(prefab, positions[i], Quaternion.identity);
        }

        ComputeAccelerations();
    }

    public float GetRandomNumber(double minimum, double maximum)
    {
        return (float)(random.NextDouble() * (maximum - minimum) + minimum);
    }

    // Update is called once per frame
    void Update()
    {

        if (run)
        {
            ComputePositions();

            for (int i = 0; i < numberOfBodies; i++)
            {
                bodies[i].position = positions[i];
            }
        }
    }

    private void FixedUpdate()
    {
        if (Input.GetKey(KeyCode.Space))
            run = !run;

        this.transform.position = new Vector3(
            positions.Select(position => position.x).Average(),
            positions.Select(position => position.y).Average(),
            positions.Select(position => position.z).Average()
        );

        //this.transform.position = positions[0];

    }

    private void ComputeAccelerations()
    {
        for (int i = 0; i < numberOfBodies; i++)
        {
            accelerations[i] = new Vector3(0, 0, 0);

            for (int j = 0; j < numberOfBodies; j++)
            {
                if (i == j)
                    return;

                var diff = positions[j] - positions[i];
                var dist = (float)Math.Sqrt(
                    diff.x * diff.x +
                    diff.y * diff.y + 
                    diff.z * diff.z
                );
                var F = (gravitationalConstant * masses[i] * masses[j]) /
                    (dist * dist + softening * softening);

                accelerations[i] += new Vector3(
                    diff.x * F * dist,
                    diff.y * F * dist,
                    diff.z * F * dist
                );
            }
        }
    }

    private void ComputePositions()
    {
        calculatePosition(
            positions,
            velocities,
            accelerations,
            masses,
            timeStep,
            numberOfBodies
        );
    }

    private void ResolveCollisions()
    {
        for (int i = 0; i < numberOfBodies; i++)
        {
            for (int j = 0; j < numberOfBodies; j++)
            {
                if (positions[i].x != positions[j].x
                 || positions[i].y != positions[j].y
                 || positions[i].z != positions[j].z)
                    return;

                (velocities[j], velocities[i]) = (velocities[i], velocities[j]);
            }
        }
    }
}