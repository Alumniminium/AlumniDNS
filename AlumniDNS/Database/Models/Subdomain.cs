using System;
using System.ComponentModel.DataAnnotations;

namespace AlumniDNS.Database.Models
{
    public class Subdomain
    {
        [Key]
        public int UniqueId { get; set; }
        public ulong CustomerId { get; set; }
        public string Name { get; set; }
        public string IP { get; set; }
        public DateTime LastUpdate { get; set; }

        public Subdomain() => LastUpdate = DateTime.Now;
    }
}
