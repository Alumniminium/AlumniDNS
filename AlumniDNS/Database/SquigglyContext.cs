using Microsoft.EntityFrameworkCore;
using AlumniDNS.Database.Models;

namespace AlumniDNS.Database
{
    public class SquigglyContext : DbContext
    {
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Subdomain> Subdomains { get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder oB) => oB.UseSqlite("Data Source=Squiggly.db");
    }
}
