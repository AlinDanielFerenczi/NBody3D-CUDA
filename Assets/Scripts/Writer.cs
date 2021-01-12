using Microsoft.Win32.SafeHandles;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;

namespace DMSLibrary
{
    public class Writer
    {
        const int MMF_MAX_SIZE = 1024;  // allocated memory for this memory mapped file (bytes)
        const int MMF_VIEW_SIZE = 1024; // how many bytes of the allocated memory can this process access
        MemoryMappedFile mmf;
        private string _location;

        public Writer(string location)
        {
            _location = location;
        }

        public void Write(object message)
        {
            // creates the memory mapped file which allows 'Reading' and 'Writing'
            //mmf = MemoryMappedFile.CreateFromFile(filePath, FileMode.OpenOrCreate, "memory_buffer", MMF_MAX_SIZE);
            mmf = MemoryMappedFile.CreateOrOpen(_location, MMF_MAX_SIZE, MemoryMappedFileAccess.ReadWrite);
            
            // creates a stream for this process, which allows it to write data from offset 0 to 1024 (whole memory)
            MemoryMappedViewStream mmvStream = mmf.CreateViewStream(0, MMF_VIEW_SIZE);

            message = JsonConvert.SerializeObject(message);

            // serialize the variable 'message' and write it to the memory mapped file
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(mmvStream, message);
            mmvStream.Seek(0, SeekOrigin.Begin); // sets the current position back to the beginning of the stream
        }
    }
}
