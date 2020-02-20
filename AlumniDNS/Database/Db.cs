using System.Linq;
using AlumniDNS.Database.Models;

namespace AlumniDNS.Database
{
    public static class Db
    {
        private static readonly SquigglyContext db = new SquigglyContext();

        internal static void EnsureDb() => db.Database.EnsureCreated();
        public static bool CustomerExists(Customer customer) => db.Customers.AsQueryable().FirstOrDefault(c => c.Username == customer.Username) != null;
        public static bool SubdomainExists(Subdomain subdomain) => db.Subdomains.AsQueryable().FirstOrDefault(c => c.Name == subdomain.Name) != null;
        public static ulong GetNextUniqueId() => (ulong)(db.Customers.Count() + 1);
        public static int GetNextSubdomainUniqueId() => db.Subdomains.Count() + 1;

        public static bool AddCustomer(Customer customer)
        {
            if (CustomerExists(customer))
                return false;

            customer.CustomerId = GetNextUniqueId();

            db.Customers.Add(customer);
            db.SaveChanges();

            return true;
        }
        public static bool AddSubdomain(Subdomain subdomain)
        {
            if (SubdomainExists(subdomain))
                return false;

            Updater.Update(subdomain.Name, subdomain.IP);

            db.Subdomains.Add(subdomain);
            db.SaveChanges();

            return false;
        }
        public static void RemoveSubdomain(string domain)
        {
            var subdomain = db.Subdomains.FirstOrDefault(s => s.Name == domain);

            if (subdomain != null)
            {
                Updater.Delete(domain);
                db.Subdomains.Remove(subdomain);
            }

            db.SaveChanges();
        }

        public static bool Authenticate(ref Customer customer)
        {
            var username = customer.Username;
            var dbCustomer = db.Customers.AsQueryable().FirstOrDefault(c => c.Username == username);

            if (dbCustomer?.Password == customer.Password)
            {
                dbCustomer.Subdomains = db.Subdomains.AsQueryable().Where(s => s.CustomerId == dbCustomer.CustomerId).ToList();
                dbCustomer.Socket = customer.Socket;
                customer = dbCustomer;
                customer.Socket.StateObject = dbCustomer;
                return true;
            }

            return false;
        }
    }
}
