using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Text;

namespace DMSLibrary
{
    public class Client : IDisposable
    {
        private NamedPipeClientStream client;
        private StreamReader reader;
        private StreamWriter writer;

        public Client(string pipeName) : this(pipeName, 1000000) { }

        public Client(string pipeName, int timeOut)
        {
            client = new NamedPipeClientStream(pipeName);
            client.Connect(timeOut);
            reader = new StreamReader(client);
            writer = new StreamWriter(client);
        }

        public void Dispose()
        {
            writer.Dispose();
            reader.Dispose();
            client.Dispose();
        }

        public string SendRequest(string request)
        {
            if (request != null)
            {
                try
                {
                    writer.WriteLine(request);
                    writer.Flush();
                    return reader.ReadLine();
                }
                catch (Exception ex)
                {
                    return string.Format("{0}\r\nDetails:\r\n{1}", "Error on server communication.", ex.Message);
                }
            }
            else
            {
                return "Error. Null request.";
            }
        }
    }
}
