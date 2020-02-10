using AlumniSocketCore.Queues;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace AlumniSocketCore.Client
{
    public class ClientSocket
    {
        public Action OnConnected, OnDisconnect;

        public Socket Socket;
        public object StateObject;
        public bool IsConnected;

        internal readonly NeutralBuffer Buffer;
        internal readonly AutoResetEvent SendSync = new AutoResetEvent(true);
        internal readonly AutoResetEvent ReceiveSync = new AutoResetEvent(false);
        internal readonly SocketAsyncEventArgs ReceiveArgs;
        internal readonly SocketAsyncEventArgs SendArgs;

        public ClientSocket(object stateObject)
        {
            Buffer = new NeutralBuffer();
            StateObject = stateObject;

            SendArgs = new SocketAsyncEventArgs();
            SendArgs.Completed += Sent;

            ReceiveArgs = new SocketAsyncEventArgs();
            ReceiveArgs.SetBuffer(Buffer.ReceiveBuffer, 0, Buffer.ReceiveBuffer.Length);
            ReceiveArgs.Completed += Received;
            ReceiveArgs.UserToken = this;
        }


        public void ConnectAsync(string host, ushort port)
        {
            try
            {
                if (IsConnected)
                    Disconnect();

                Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                var connectArgs = new SocketAsyncEventArgs
                {
                    RemoteEndPoint = new IPEndPoint(IPAddress.Parse(host), port)
                };
                connectArgs.Completed += Connected;
                if (!Socket.ConnectAsync(connectArgs))
                    Connected(null, connectArgs);
            }
            catch
            {
                Disconnect();
            }
        }

        private void Connected(object o, SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                IsConnected = true;
                try
                {
                    OnConnected?.Invoke();
                    if (!Socket.ReceiveAsync(ReceiveArgs))
                        Received(null, ReceiveArgs);
                }
                catch
                {
                    Disconnect();
                }
            }
            else
                Disconnect();
        }

        private void Received(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError != SocketError.Success || e.BytesTransferred == 0)
            {
                Disconnect();
                return;
            }

            try
            {
                ReceiveQueue.Add(e);
                ReceiveSync.WaitOne();
                if (!Socket.ReceiveAsync(e))
                    Received(null, e);
            }
            catch
            {
                Disconnect();
            }
        }
        public void Send(byte[] packet)
        {
            SendSync.WaitOne();
            SendArgs.SetBuffer(packet, 0, packet.Length);

            try
            {
                if (!Socket.SendAsync(SendArgs))
                    Sent(null, SendArgs);
            }
            catch
            {
                Disconnect();
            }
        }

        private void Sent(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
                SendSync.Set();
            else
                Disconnect();
        }
        public void Disconnect()
        {
            try
            {
                IsConnected = false;
                Socket?.Dispose();
            }
            finally
            {
                OnDisconnect?.Invoke();
            }
        }
    }
}
