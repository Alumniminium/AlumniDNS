﻿using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading;
using AlumniSocketCore.Client;

namespace AlumniSocketCore.Queues
{
    public static class ReceiveQueue
    {
        private static readonly ConcurrentQueue<SocketAsyncEventArgs> Queue = new ConcurrentQueue<SocketAsyncEventArgs>();
        private static readonly AutoResetEvent Sync = new AutoResetEvent(false);
        private static Thread _workerThread;
        private static Action<ClientSocket, byte[]> _onPacket;
        private const int HEADER_SIZE = 4;
        private static int _count;
        private static int _destOffset;
        private static int _recOffset;

        public static void Start(Action<ClientSocket, byte[]> onPacket)
        {
            if (_onPacket == null)
                _onPacket = onPacket;
            if (_workerThread == null)
                _workerThread = new Thread(WorkLoop) { IsBackground = true };
            if (!_workerThread.IsAlive)
                _workerThread.Start();
        }
        
        public static void Add(SocketAsyncEventArgs e)
        {
            Queue.Enqueue(e);
            Sync.Set();
        }
        
        private static void WorkLoop()
        {
            while (true)
            {
                Sync.WaitOne();
                while (Queue.TryDequeue(out var e))
                {
                    var connection = (ClientSocket)e.UserToken;

                    AssemblePacket(e);

                    connection.ReceiveSync.Set();
                }
            }
        }
        private static void AssemblePacket(SocketAsyncEventArgs e)
        {
            while (true)
            {
                var connection = (ClientSocket)e.UserToken;
                _destOffset = connection.Buffer.BytesInBuffer;
                _recOffset = connection.Buffer.BytesProcessed;

                if (connection.Buffer.BytesInBuffer == 0)
                {
                    var receivedBytes = e.BytesTransferred - connection.Buffer.BytesProcessed;
                    if (receivedBytes >= HEADER_SIZE)
                        connection.Buffer.BytesRequired = BitConverter.ToInt32(e.Buffer, connection.Buffer.BytesProcessed);
                }
                else if (connection.Buffer.BytesInBuffer > 0)
                {
                    if (connection.Buffer.BytesInBuffer < HEADER_SIZE)
                    {
                        _count = HEADER_SIZE - connection.Buffer.BytesInBuffer;
                        Copy(e, connection, true);
                    }
                    else//?????
                        connection.Buffer.BytesRequired = BitConverter.ToInt32(connection.Buffer.MergeBuffer, 0);
                }
                else
                {
                    connection.Buffer.BytesRequired = e.BytesTransferred - connection.Buffer.BytesProcessed;
                }

                _destOffset = connection.Buffer.BytesInBuffer;
                _recOffset = connection.Buffer.BytesProcessed;
                _count = e.BytesTransferred - connection.Buffer.BytesProcessed;

                Copy(e, connection);

                if (connection.Buffer.BytesInBuffer == connection.Buffer.BytesRequired && connection.Buffer.BytesRequired > 6)
                {
                    _onPacket(connection, connection.Buffer.MergeBuffer);
                    connection.Buffer.BytesInBuffer = 0;
                }

                if (connection.Buffer.BytesProcessed != e.BytesTransferred)
                    continue;

                connection.Buffer.BytesProcessed = 0;
                break;
            }
        }
        private static unsafe void Copy(SocketAsyncEventArgs e, ClientSocket connection, bool header = false)
        {
            fixed (byte* dest = connection.Buffer.MergeBuffer)
            fixed (byte* rec = e.Buffer)
            {
                for (var i = 0; i < _count; i++)
                {
                    dest[i + _destOffset] = rec[i + _recOffset];
                    connection.Buffer.BytesInBuffer++;
                    connection.Buffer.BytesProcessed++;

                    if (connection.Buffer.BytesInBuffer == (header ? HEADER_SIZE : connection.Buffer.BytesRequired))
                        break;
                }
            }
        }
    }
}