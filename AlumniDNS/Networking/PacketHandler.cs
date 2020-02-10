using System;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using Packets;
using AlumniDNS.Database;
using AlumniDNS.Database.Models;
using Packets.Enums;
using AlumniSocketCore.Client;
using System.Runtime.CompilerServices;

namespace AlumniDNS.Networking
{
    public static class PacketHandler
    {
        public static ClientSocket Client;
        public static Customer Customer;

        public static void Handle(ClientSocket client, byte[] packet)
        {
            var id = BitConverter.ToUInt16(packet, 4);
            Client = client;
            Customer = (Customer)client.StateObject;

            switch (id)
            {
                case 1000:
                    ProcessLogin(packet);
                    break;
                case 1002:
                    ProcessTransaction(packet);
                    break;
                default:
                    Console.WriteLine("Invalid packet received from " + client.Socket.RemoteEndPoint);
                    client.Disconnect();
                    break;
            }
        }

        private static void ProcessTransaction(byte[] packet)
        {
            var msgTransaction = (MsgTransaction)packet;
            var domain = msgTransaction.GetSubdomain();
            switch (msgTransaction.Type)
            {
                case MsgTransactionType.Update:
                    {
                        var subdomain = Customer.Subdomains.FirstOrDefault(s => s.Name == domain);
                        if (subdomain != null)
                        {
                            msgTransaction.Type = MsgTransactionType.Success;
                            Updater.Update(domain, Customer.GetIp());
                            subdomain.LastUpdate = DateTime.Now;
                        }
                        else
                            msgTransaction.Type = MsgTransactionType.Fail_NotOwner;

                        Customer.Send(msgTransaction);
                        break;
                    }
                case MsgTransactionType.Add:
                    {
                        var subdomain = new Subdomain
                        {
                            Name = domain,
                            CustomerId = Customer.CustomerId,
                            UniqueId = Db.GetNextSubdomainUniqueId(),
                            IP = Customer.GetIp()
                        };
                        if (!Regex.IsMatch(domain, "^[A-Za-z]+$", RegexOptions.Compiled))
                            msgTransaction.Type = MsgTransactionType.Fail_IllegalCharacters;
                        else if (Customer.Subdomains.Count == 5)
                            msgTransaction.Type = MsgTransactionType.Fail_OutOfSlots;
                        else if (subdomain.Name.Length > 16)
                            msgTransaction.Type = MsgTransactionType.Fail_TooLong;
                        else if (Db.SubdomainExists(subdomain))
                            msgTransaction.Type = MsgTransactionType.Fail_Exists;
                        else
                        {
                            Customer.Subdomains.Add(subdomain);
                            Db.AddSubdomain(subdomain);
                            msgTransaction.Type = MsgTransactionType.Success;
                        }

                        break;
                    }
                case MsgTransactionType.Remove:
                    {
                        var ownsDomain = Customer.Subdomains.Any(subdomain => subdomain.Name == domain);

                        if (ownsDomain)
                        {
                            Db.RemoveSubdomain(domain);
                            msgTransaction.Type = MsgTransactionType.Success;
                        }
                        else
                            msgTransaction.Type = MsgTransactionType.Fail_NotOwner;

                        break;
                    }
            }

            Client.Send(msgTransaction);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void ProcessLogin(byte[] packet)
        {
            fixed (byte* p = packet)
            {
                var msgLogin = (MsgLogin*)p;
                var (user, pass) = msgLogin->GetUserPass();
                var id = msgLogin->UniqueId;
                Console.WriteLine(user + " " + pass + " " + id);

                Customer = new Customer
                {
                    Username = user,
                    Password = pass
                };

                Client.StateObject = Customer;
                Customer.Socket = Client;

                if (Db.Authenticate(ref Customer))
                    msgLogin->UniqueId = (uint)Customer.CustomerId;
                else if (Db.AddCustomer(Customer))
                    msgLogin->UniqueId = (uint)Customer.CustomerId;

                Customer.Send(*msgLogin);

                var domains = Customer.Subdomains.Aggregate("", (c, s) => c + (s.Name + " " + s.IP + "#"));
                Customer.Send(MsgDomainList.Create(domains));
            }
        }
    }
}