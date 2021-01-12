using Assets.Scripts;
using DMSLibrary;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class NBodyDistributed : MonoBehaviour
{
    public float gravitationalConstant = 1f;
    public Rigidbody prefab;
    public float softening = 0.1f;
    public int numberOfBodies = 3;
    public float timeStep = 0.01f;

    private static string pipeName = "unity_";
    private float[] masses;
    private Vector3D[] accelerations;
    private Rigidbody[] bodies;
    private Vector3D[] positions;
    private Vector3D[] velocities;
    private System.Random random = new System.Random();
    private bool run = false;
    private Client[] clients;
    private Process[] processes;

    // Start is called before the first frame update
    void Start()
    {
        bodies = new Rigidbody[numberOfBodies];
        masses = new float[numberOfBodies];
        positions = new Vector3D[numberOfBodies];
        velocities = new Vector3D[numberOfBodies];
        accelerations = new Vector3D[numberOfBodies];
        clients = new Client[numberOfBodies];
        processes = new Process[numberOfBodies];

        for (int i = 0; i < numberOfBodies; i++)
        {
            Process proc = new Process();
            //proc.StartInfo.UseShellExecute = false;
            //proc.StartInfo.CreateNoWindow = true;
            //proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            proc.StartInfo.FileName = @"D:\Faculty\PDPCourse\NBodyDistributed\bin\Debug\NBodyDistributed.exe";
            proc.StartInfo.Arguments = string.Format(i.ToString());
            proc.StartInfo.RedirectStandardError = false;
            proc.StartInfo.RedirectStandardOutput = false;
            proc.Start();
            processes[i] = proc;
        }

        Thread.Sleep(200 * numberOfBodies);

        for (int i = 0; i < numberOfBodies; i++)
        {
            masses[i] = GetRandomNumber(0, 0.2);
            positions[i] = new Vector3D(GetRandomNumber(-2, 2), GetRandomNumber(-2, 2), GetRandomNumber(-2, 2));
            velocities[i] = new Vector3D(GetRandomNumber(-2, 2), GetRandomNumber(-2, 2), GetRandomNumber(-2, 2));
            try
            {
                clients[i] = new Client(pipeName + i);
            }catch(Exception e)
            {
                print(e.Message + " for " + i);
            }
        }

        var averageMass = masses.Average();
        var multiplied = new Vector3D[numberOfBodies];

        for (int i = 0; i < numberOfBodies; i++)
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

        var newWriter = new Writer("masses");
        newWriter.Write(masses);
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
                bodies[i].position = positions[i].GetVector3();
            }
        }
    }

    private void FixedUpdate()
    {
        if (Input.GetKey(KeyCode.Space))
            run = !run;

        this.transform.position = new Vector3(
            positions.Select(position => position.X).Average(),
            positions.Select(position => position.Y).Average(),
            positions.Select(position => position.Z).Average()
        );
    }

    public void PrintResults()
    {
        for (int i = 0; i < numberOfBodies; ++i)
        {
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
            var newWriter = new Writer("positions");
            newWriter.Write(positions);
            newWriter = new Writer("masses");
            newWriter.Write(masses);

            var response = clients[i].SendRequest(
                    "accelerations;" +
                    numberOfBodies +
                    ";" +
                    i
                );

            //print(response);

            accelerations[i] = JsonConvert.DeserializeObject<Vector3D>(
                response
            );
        });
    }

    private void ComputeVelocities()
    {
        Parallel.For(0, numberOfBodies, (i) =>
        {
            velocities[i] += JsonConvert.DeserializeObject<Vector3D>(
                clients[i].SendRequest(
                    "velocities;" + 
                    JsonConvert.SerializeObject(accelerations[i])
                )
            );
        });
    }

    private void ComputePositions()
    {
        Parallel.For(0, numberOfBodies, (i) =>
        {
            positions[i] += JsonConvert.DeserializeObject<Vector3D>(
                clients[i].SendRequest(
                    "positions;" + 
                    JsonConvert.SerializeObject(velocities[i])
                )
            );
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

    private void OnApplicationQuit()
    {
        for(int i = 0; i< numberOfBodies; i++)
        {
            processes[i].Kill();
        }
    }
}