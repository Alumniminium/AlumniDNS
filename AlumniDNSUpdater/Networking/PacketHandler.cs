using System;
using AlumniDNSUpdater.Models;
using Packets;
using Packets.Enums;

namespace AlumniDNSUpdater.Networking
{
    public static class PacketHandler
    {
        public static void Handle(Client client, byte[] buffer)
        {
            var packetId = BitConverter.ToUInt16(buffer, 4);
            switch (packetId)
            {
                case 1000:
                    {
                        var msgLogin = (MsgLogin)buffer;
                        var uniqueId = msgLogin.UniqueId;

                        if (uniqueId != 0)
                            Console.WriteLine("Authentication successful. Your customer Id is: " + uniqueId);
                        else
                            Console.WriteLine("Authentication failed.");
                        break;
                    }
                case 1001:
                    {
                        var msgDomainList = (MsgDomainList)buffer;
                        var domains = msgDomainList.GetSubdomains();

                        Console.WriteLine("Here are your domains:");

                        foreach (var entry in domains)
                        {
                            if (string.IsNullOrEmpty(entry))
                                continue;

                            var parts = entry.Split(' ');
                            var domain = parts[0];
                            var ip = parts[1];

                            Console.WriteLine(domain + " - " + ip);
                            client.Subdomains.Add(new Subdomain(domain, ip));
                        }

                        if (client.Subdomains.Count == 0)
                            break;

                        Console.WriteLine("Updating:");
                        foreach (var subdomain in client.Subdomains)
                        {
                            if (!subdomain.Update)
                                continue;

                            Console.WriteLine(subdomain + ".alumni.re");
                            client.Send(MsgTransaction.Create(subdomain.Name, MsgTransactionType.Update));
                        }

                        break;
                    }
                case 1002:
                    {
                        var msgTransaction = (MsgTransaction)buffer;
                        var domain = msgTransaction.GetSubdomain();
                        switch (msgTransaction.Type)
                        {
                            case MsgTransactionType.Fail_Exists:
                                Console.WriteLine($"Couldn't add `{domain}` - It already exists.");
                                break;
                            case MsgTransactionType.Fail_OutOfSlots:
                                Console.WriteLine($"Couldn't add `{domain}` - You already have 5 out of 5 subdomains.");
                                break;
                            case MsgTransactionType.Fail_TooLong:
                                Console.WriteLine($"Couldn't add `{domain}` - Name can't be longer than 16 characters.");
                                break;
                            case MsgTransactionType.Fail_IllegalCharacters:
                                Console.WriteLine($"Couldn't add `{domain}` - Not allowed to contain special characters.");
                                break;
                            case MsgTransactionType.Fail_NotOwner:
                                Console.WriteLine($"Couldn't remove `{domain}` - It's not yours.");
                                break;
                            case MsgTransactionType.Success:
                                Console.WriteLine("Success: " + domain);
                                break;
                        }
                        break;
                    }
            }
        }
    }
}
