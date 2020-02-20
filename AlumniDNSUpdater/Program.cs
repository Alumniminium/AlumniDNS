using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Packets;

namespace AlumniDNSUpdater
{
    public static class Program
    {
        public static Client Client = new Client();
        public const string SERVER_IP = "192.168.0.3";
        public const ushort SERVER_PORT = 65534;

        public static async Task Main()
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, a) =>
            {
                Debugger.Break();
            };
            Console.Title = "CLIENT APP";
            LoadConfig();

            if (await Client.ConnectAsync(SERVER_IP, SERVER_PORT))
                Client.Send(MsgLogin.Create("user", "pass"));

            while (true)
                Console.ReadLine();
        }

        private static void LoadConfig()
        {
            if (File.Exists("config.json"))
                Client = JsonConvert.DeserializeObject<Client>(File.ReadAllText("config.json"));
            else
                Console.WriteLine("No config.json was found at " + Environment.CurrentDirectory);
        }
    }
}