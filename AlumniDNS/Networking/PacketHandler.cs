using System;
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
        public static void Handle(ClientSocket client, byte[] packet)
        {
            var id = BitConverter.ToUInt16(packet, 4);
            var customer = (Customer)client.StateObject;

            switch (id)
            {
                case 1000:
                    ProcessLogin(packet, customer, client);
                    break;
                case 1002:
                    ProcessTransaction(packet, customer, client);
                    break;
                default:
                    Console.WriteLine("Invalid packet received from " + client.Socket.RemoteEndPoint);
                    client.Disconnect();
                    break;
            }
        }

        private static void ProcessTransaction(byte[] packet, Customer customer, ClientSocket client)
        {
            var msgTransaction = (MsgTransaction)packet;
            var domain = msgTransaction.GetSubdomain();
            switch (msgTransaction.Type)
            {
                case MsgTransactionType.Update:
                    {
                        var subdomain = customer.Subdomains.FirstOrDefault(s => s.Name == domain);
                        if (subdomain != null)
                        {
                            msgTransaction.Type = MsgTransactionType.Success;
                            Updater.Update(domain, customer.GetIp());
                            subdomain.LastUpdate = DateTime.Now;
                        }
                        else
                            msgTransaction.Type = MsgTransactionType.Fail_NotOwner;

                        customer.Send(msgTransaction);
                        break;
                    }
                case MsgTransactionType.Add:
                    {
                        var subdomain = new Subdomain
                        {
                            Name = domain,
                            CustomerId = customer.CustomerId,
                            UniqueId = Db.GetNextSubdomainUniqueId(),
                            IP = customer.GetIp()
                        };
                        if (!Regex.IsMatch(domain, "^[A-Za-z]+$", RegexOptions.Compiled))
                            msgTransaction.Type = MsgTransactionType.Fail_IllegalCharacters;
                        else if (customer.Subdomains.Count == 5)
                            msgTransaction.Type = MsgTransactionType.Fail_OutOfSlots;
                        else if (subdomain.Name.Length > 16)
                            msgTransaction.Type = MsgTransactionType.Fail_TooLong;
                        else if (Db.SubdomainExists(subdomain))
                            msgTransaction.Type = MsgTransactionType.Fail_Exists;
                        else
                        {
                            customer.Subdomains.Add(subdomain);
                            Db.AddSubdomain(subdomain);
                            msgTransaction.Type = MsgTransactionType.Success;
                        }

                        break;
                    }
                case MsgTransactionType.Remove:
                    {
                        var ownsDomain = customer.Subdomains.Any(subdomain => subdomain.Name == domain);

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

            client.Send(msgTransaction);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void ProcessLogin(byte[] packet, Customer customer, ClientSocket client)
        {
            fixed (byte* p = packet)
            {
                var msgLogin = (MsgLogin*)p;
                var (user, pass) = msgLogin->GetUserPass();
                var id = msgLogin->UniqueId;
                Console.WriteLine(user + " " + pass + " " + id);

                customer = new Customer
                {
                    Username = user,
                    Password = pass
                };

                client.StateObject = customer;
                customer.Socket = client;

                if (Db.Authenticate(ref customer))
                    msgLogin->UniqueId = (uint)customer.CustomerId;
                else if (Db.AddCustomer(customer))
                    msgLogin->UniqueId = (uint)customer.CustomerId;

                customer.Send(*msgLogin);

                var domains = customer.Subdomains.Aggregate("", (c, s) => c + (s.Name + " " + s.IP + "#"));
                customer.Send(MsgDomainList.Create(domains));
            }
        }
    }
}