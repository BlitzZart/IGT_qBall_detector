using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OpenCV_Balls {
    class ServerThread {
        // TCP connetion to client
        private TcpClient _tcpClient = null;
        // Stores connection to a client
        public ServerThread(TcpClient connection) {
            _tcpClient = connection;
        }
        public void Close() {
            _tcpClient.Close();
        }

        private bool ClientConnected() {
            try {
                // Detect if client disconnected
                if (_tcpClient.Client.Poll(0, SelectMode.SelectRead)) {
                    byte[] buff = new byte[1];
                    if (_tcpClient.Client.Receive(buff, SocketFlags.Peek) == 0) {
                        // Client disconnected
                        return false;
                    }
                }
            }
            catch {
                return false;
            }

            return true;
        }

        // send position data via network
        public void SendPosition(float x, float y) {

            if (!ClientConnected()) {
                Close();
                return;
            }

            byte[] xBytes = FloatUnion.FloatToBytes(x);
            byte[] yBytes = FloatUnion.FloatToBytes(y);

            byte[] sendBytes = new byte[xBytes.Length + yBytes.Length];
            xBytes.CopyTo(sendBytes, 0);
            yBytes.CopyTo(sendBytes, 4);

            Stream outStream = _tcpClient.GetStream();
            
            outStream.Write(sendBytes, 0, sendBytes.Length);
        }    
    }
}