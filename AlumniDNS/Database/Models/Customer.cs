using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Net;
using AlumniSocketCore.Client;
using AlumniSocketCore.Server;

namespace AlumniDNS.Database.Models
{
    public class Customer
    {
        [Key]
        public ulong CustomerId { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public ICollection<Subdomain> Subdomains { get; set; }
        [NotMapped]
        public ClientSocket Socket;

        public Customer()
        {
            Subdomains = new List<Subdomain>();
        }

        public void Send(byte[] packet) => Socket?.Send(packet);

        public string GetIp() => ((IPEndPoint) Socket.Socket.RemoteEndPoint).Address.ToString();
    }
}
