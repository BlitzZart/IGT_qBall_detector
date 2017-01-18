using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace OpenCV_Balls {
    class TCP_Server {
        private static TcpListener listener = null;
        // list of open connection threads
        private static ArrayList threads = new ArrayList();
        // main thread
        private static Thread mainThread;

        // server initialisation
        public static void InitServer() {
            listener =  new TcpListener(4711); // new TcpListener(GetOwnIP(), 4711);//
            listener.Start();
            // init main thread and input thread
            mainThread = new Thread(new ThreadStart(Run));
            mainThread.Start();
        }

        // main thread
        // listens for conection requests
        public static void Run() {
            while (true) {
                // waiting for requesting client
                TcpClient c = listener.AcceptTcpClient();
                // initialize and store a new server thread
                threads.Add(new ServerThread(c));
            }
        }

        public static void SendPosition(float x, float y) {
            foreach (ServerThread item in threads) {
                item.SendPosition(x, y);
            }
        }

        public static void CloseAll() {
            // stop main and input thread
            mainThread.Abort();
            // stop all server threads
            foreach (ServerThread item in threads) {
                item.Close();
            }

            listener.Stop();
        }

        private static IPAddress GetOwnIP() {
            IPHostEntry host;
            host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList) {
                if (ip.AddressFamily == AddressFamily.InterNetwork) {
                    return ip;
                }
            }
            return null;
        }
    }
}