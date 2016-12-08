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
            StreamReader inStream = null;
            BinaryReader binReader = null;
            while (!hasConnection) {
                // Verbindung zum Server aufbauen
                try {
                    client = new TcpClient("localhost", 4711);
                } catch {
                    Console.WriteLine("No server found...");
                }

                if (client != null) {
                    // Stream zum lesen holen
                    client.ReceiveBufferSize = 8;
                    //inStream = new StreamReader(client.GetStream());
                    binReader = new BinaryReader(client.GetStream());
                    hasConnection = true;
                }
                Thread.Sleep(1000);
                Console.WriteLine("Waiting for server...");
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
                    // Setze das Schleifen-Flag zurück
                    // wenn ein Fehler in der Kommunikation aufgetreten ist
                    loop = false;
                }
            }
            // Schließe die Verbindung zum Server
            client.Close();
        }
    }
}
