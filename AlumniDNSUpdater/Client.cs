using System.Threading.Tasks;
using System.Collections.Generic;
using AlumniDNSUpdater.Models;
using AlumniDNSUpdater.Networking;
using AlumniSocketCore.Client;
using AlumniSocketCore.Queues;
using Newtonsoft.Json;

namespace AlumniDNSUpdater
{
    public class Client
    {
        [JsonIgnore]
        public ClientSocket Socket;
        public string Ip = "192.168.0.3";
        public ushort Port = 65534;
        public List<Subdomain> Subdomains = new List<Subdomain>();
        public bool IsConnected;


        public Task<bool> ConnectAsync(string ip, ushort port)
        {
            Socket = new ClientSocket(this);
            ReceiveQueue.Start(OnPacket);

            var tcs = new TaskCompletionSource<bool>();
            Socket.OnConnected += () => { tcs?.SetResult(true); };
            Socket.OnDisconnect += () => { tcs?.SetResult(false); };

            Socket.OnDisconnect += Disconnected;
            Socket.OnConnected += Connected;
            Socket.ConnectAsync(ip, port);
            return tcs.Task;
        }

        private void Connected() => IsConnected = true;

        private void Disconnected() => ConnectAsync(Ip, Port);

        private void OnPacket(ClientSocket client, byte[] buffer) => PacketHandler.Handle((Client)client.StateObject, buffer);

        public void Send(byte[] packet) => Socket.Send(packet);
    }
}
