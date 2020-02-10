using System;
using System.Diagnostics;
using AlumniDNS.Database;
using AlumniDNS.Networking;
using AlumniSocketCore.Queues;
using AlumniSocketCore.Server;

namespace AlumniDNS
{
    public static class Program
    {
        public static void Main()
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                Debugger.Break();
            };
            Console.Title = "SERVER APP";
            Db.EnsureDb();

            ReceiveQueue.Start(PacketHandler.Handle);
            ServerSocket.Start(65534);
            Console.WriteLine("Online");
            while(true){
                Console.ReadLine();
            }
        }
    }
}
