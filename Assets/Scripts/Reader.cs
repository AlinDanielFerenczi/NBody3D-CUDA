using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;

namespace DMSLibrary
{
    public delegate void callbackWrite(string message);

    public class Reader
    {
        EventWaitHandle wh = new EventWaitHandle(false, EventResetMode.ManualReset,
                                          "Lab7");
        const int MMF_MAX_SIZE = 1024;  // allocated memory for this memory mapped file (bytes)
        const int MMF_VIEW_SIZE = 1024; // how many bytes of the allocated memory can this process access
        private string _location;

        public Reader(string location)
        {
            _location = location;
        }

        public object Read()
        {
            using (var mmf = MemoryMappedFile.OpenExisting(_location))
            {
                using (var mmvStream = mmf.CreateViewStream(0, MMF_VIEW_SIZE))
                {
                    BinaryFormatter formatter = new BinaryFormatter();

                    while (!mmvStream.CanRead)
                        Thread.Sleep(100);

                    // needed for deserialization
                    byte[] buffer = new byte[MMF_VIEW_SIZE];

                    object message;

                    // stores everything into this buffer
                    mmvStream.Read(buffer, 0, MMF_VIEW_SIZE);

                    // deserializes the buffer & prints the message
                    message = formatter.Deserialize(new MemoryStream(buffer));

                    return message;
                }
            }
        }
    }
}
