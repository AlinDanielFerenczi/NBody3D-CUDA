using DMSLibrary;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Threading;

namespace NBodyDistributed
{
    class Program
    {
        private static float timeStep = 0.01f;
        private static float gravitationalConstant = 1;
        private static string pipeName = "unity_";
        private static int option;

        static void Main(string[] args)
        {
            option = Convert.ToInt32(args[0]);

            //if(option == 1)
            Task.Run(() => Server(option.ToString()));

            Thread.Sleep(1000000);
        }

        private static void Server(string option)
        {
            Console.WriteLine("Instance created "+pipeName+option);

            var server = new Server(pipeName + option);

            server.newRequestEvent += (s, e) =>
            {
                //Console.WriteLine(e.Request);

                if(e.Request.Contains("velocities"))
                {
                    e.Response = JsonConvert.SerializeObject(
                        JsonConvert.DeserializeObject<Vector3D>(e.Request.Split(';')[1]) * (timeStep / 2)
                    );
                    return;
                }

                if(e.Request.Contains("positions"))
                {
                    e.Response = JsonConvert.SerializeObject(
                        JsonConvert.DeserializeObject<Vector3D>(e.Request.Split(';')[1]) * timeStep
                    );
                    return;
                }

                if (e.Request.Contains("accelerations"))
                {
                    var parameters = e.Request.Split(';');
                    var numberOfBodies = int.Parse(parameters[1]);
                    var i = int.Parse(parameters[2]);

                    Reader reader;

                    reader = new Reader("positions");
                    var positions = JsonConvert.DeserializeObject<Vector3D[]>(
                        reader.Read().ToString()
                    );

                    reader = new Reader("masses");
                    var masses = JsonConvert.DeserializeObject<float[]>(
                        reader.Read().ToString()
                    );

                    var result = new Vector3D(0, 0, 0);

                    for (int j = 0; j < numberOfBodies; j++)
                    {
                        var diff = positions[j] - positions[i];
                        var dist = (float)Math.Sqrt(diff.X * diff.X + diff.Y * diff.Y + diff.Z * diff.Z);
                        var F = (gravitationalConstant * masses[i] * masses[j]) / (dist * dist + 0.1f * 0.1f);

                        result += new Vector3D(
                            diff.X * F * dist,
                            diff.Y * F * dist,
                            diff.Z * F * dist
                        );
                    }

                    e.Response = JsonConvert.SerializeObject(result);
                    return;
                }

                //Console.WriteLine(e.Response);
                //var newReader = new Reader("masses");
            };
        }
    }
}
