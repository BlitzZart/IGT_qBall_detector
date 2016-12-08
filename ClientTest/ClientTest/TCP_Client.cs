using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OpenCV_Balls {
    class TCP_Client {
        public static void Main() {
            bool hasConnection = false;
            TcpClient client = null;
            BinaryReader binReader = null;
            while (!hasConnection) {
                // connect to server
                try {
                    client = new TcpClient("localhost", 4711);
                } catch {
                    Console.WriteLine("No server found...");
                }

                if (client != null) {
                    // read stream
                    client.ReceiveBufferSize = 8;
                    binReader = new BinaryReader(client.GetStream());
                    hasConnection = true;
                }
                Console.WriteLine("Waiting for server...");
                Thread.Sleep(1000);
            }
            Console.WriteLine("Connection established.");
            bool loop = true;
            while (loop) {
                try {
                    float x = binReader.ReadSingle();
                    float y = binReader.ReadSingle();

                    Console.WriteLine("x " + x + "y " + y);

                }
                catch (Exception) {
                    // stop loop when error occures
                    loop = false;
                }
            }
            // close connection
            client.Close();
        }
    }
}
