using Assets.Scripts;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class NBody : MonoBehaviour
{
    public float gravitationalConstant = 1f;
    public Rigidbody prefab;
    public string fileName;
    public float softening = 0.1f;
    public int numberOfBodies = 3;
    public float timeStep = 0.01f;

    private float[] masses;
    private Vector3D[] accelerations;
    private Rigidbody[] bodies;
    private Vector3D[] positions;
    private Vector3D[] velocities;
    private System.Random random = new System.Random();
    private bool run = false;

    // Start is called before the first frame update
    void Start()
    {

        //var file = File.ReadAllText(fileName);
        //var parameters = JsonConvert.DeserializeObject<Dictionary<string, object>>(file);
        //var bodiesParameters = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(parameters["bodies"].ToString());

        //numberOfBodies = bodiesParameters.Count;

        bodies = new Rigidbody[numberOfBodies];
        masses = new float[numberOfBodies];
        positions = new Vector3D[numberOfBodies];
        velocities = new Vector3D[numberOfBodies];
        accelerations = new Vector3D[numberOfBodies];

        /*Parallel.ForEach(bodiesParameters, (body, _, index) =>
        {
            masses[index] = 0.2f;//float.Parse(body["mass"]);
            //positions[index] = Decompose(body["position"]);
            positions[index] = new Vector3D(GetRandomNumber(-2, 2), GetRandomNumber(-2, 2), GetRandomNumber(-2, 2));
            velocities[index] = new Vector3D(GetRandomNumber(-2, 2), GetRandomNumber(-2, 2), GetRandomNumber(-2, 2));
        });*/

        for(int i = 0; i < numberOfBodies; i++)
        {
            masses[i] = GetRandomNumber(0,0.2);
            positions[i] = new Vector3D(GetRandomNumber(-2, 2), GetRandomNumber(-2, 2), GetRandomNumber(-2, 2));
            velocities[i] = new Vector3D(GetRandomNumber(-2, 2), GetRandomNumber(-2, 2), GetRandomNumber(-2, 2));
        }

        var averageMass = masses.Average();
        var multiplied = new Vector3D[numberOfBodies];
        
        for(int i = 0; i < numberOfBodies; i++)
        {
            multiplied[i] = velocities[i] * masses[i];
        }

        var averageMultipliedX = multiplied.Select(x => x.X).Average();
        var averageMultipliedY = multiplied.Select(x => x.Y).Average();
        var averageMultipliedZ = multiplied.Select(x => x.Z).Average();

        for (int i = 0; i < numberOfBodies; i++)
        {
            velocities[i] = new Vector3D(
                velocities[i].X - (averageMultipliedX / averageMass),
                velocities[i].Y - (averageMultipliedY / averageMass),
                velocities[i].Z - (averageMultipliedZ / averageMass)
            );
        }

        velocities[0] = new Vector3D(0, 0, 0);
        masses[0] = 100;

        for (int i = 0; i < numberOfBodies; i++)
        {
            bodies[i] = Instantiate(prefab, positions[i].GetVector3(), Quaternion.identity);
        }

        ComputeAccelerations();
    }

    public float GetRandomNumber(double minimum, double maximum)
    {
        return (float)(random.NextDouble() * (maximum - minimum) + minimum);
    }

    private Vector3D Decompose(string line)
    {
        string[] xyz = line.Split();
        float x = float.Parse(xyz[0]);
        float y = float.Parse(xyz[1]);
        float z = float.Parse(xyz[2]);
        return new Vector3D(x, y, z);
    }

    // Update is called once per frame
    void Update()
    {
        if (run)
        {
            ComputeVelocities();
            ComputePositions();
            ComputeAccelerations();
            ComputeVelocities();
            ResolveCollisions();

            for (int i = 0; i < numberOfBodies; i++)
            {
                //bodies[i].velocity = velocities[i].GetVector3();
                //bodies[i].position = Vector3.MoveTowards(bodies[i].position, positions[i].GetVector3(), step);
                bodies[i].position = positions[i].GetVector3();
                //bodies[i].rigid
            }
        }
    }

    private void FixedUpdate()
    {
        if (Input.GetKey(KeyCode.Space))
            run = !run;
    }

    public void PrintResults()
    {
        for (int i = 0; i < numberOfBodies; ++i)
        {
            //bodies[i].position = positions[i].GetVector3();
            Console.WriteLine(
                "Body {0} : {1,9:F6}  {2,9:F6}  {3,9:F6} | {4,9:F6}  {5,9:F6}  {6,9:F6}",
                i + 1,
                positions[i].X, positions[i].Y, positions[i].Z,
                velocities[i].X, velocities[i].Y, velocities[i].Z
            );
        }
    }

    private void ComputeAccelerations()
    {
        Parallel.For(0, numberOfBodies, (i) =>
        {
            accelerations[i] = new Vector3D(0, 0, 0);

            Parallel.For(0, numberOfBodies, (j) =>
            {
                if (i == j)
                    return;


                // Initial implementation

                /*var diff = positions[j] - positions[i];

                var inv_r3 = Math.Pow(
                    Math.Pow(diff.X,2) + Math.Pow(diff.Y,2) + Math.Pow(diff.Z,1) + Math.Pow(softening, 2),
                    -1.5
                );

                if (double.IsNaN(inv_r3))
                    inv_r3 = 0;

                //var temp = gravitationalConstant * masses[j] / (float)Math.Pow(diff.Mod() + (float)Math.Pow(softening, 2), 3);

                lock (accelerations[i])
                {
                    //accelerations[i] += (diff) * temp;
                    accelerations[i] += (diff * (float)inv_r3 * masses[i] * gravitationalConstant);
                }*/




                // After a lot of searching

                var diff = positions[j] - positions[i];
                var dist = (float)Math.Sqrt(diff.X * diff.X + diff.Y * diff.Y + diff.Z * diff.Z);
                var F = (gravitationalConstant * masses[i] * masses[j]) / (dist * dist + softening * softening);

                lock (accelerations[i])
                {
                    accelerations[i] += new Vector3D(
                        diff.X * F * dist,
                        diff.Y * F * dist,
                        diff.Z * F * dist
                    );
                }
            });
        });
    }

    private void ComputeVelocities()
    {
        Parallel.For(0, numberOfBodies, (i) =>
        {
            velocities[i] += (accelerations[i] * (timeStep / 2));
        });
    }

    private void ComputePositions()
    {
        Parallel.For(0, numberOfBodies, (i) =>
        {
            positions[i] += (velocities[i] * timeStep);
        });
    }

    private void ResolveCollisions()
    {
        Parallel.For(0, numberOfBodies, (i) =>
        {
            Parallel.For(0, numberOfBodies, (j) =>
            {
                if (positions[i].X != positions[j].X
                 || positions[i].Y != positions[j].Y
                 || positions[i].Z != positions[j].Z)
                    return;

                velocities[j] = Interlocked.Exchange(ref velocities[i], velocities[j]);
            });
        });
    }
}