namespace AlumniDNSUpdater.Models
{
    public class Subdomain
    {
        public string Name = "";
        public string IP = "";
        public bool Update = false;

        public Subdomain(string name, string ip)
        {
            Name = name;
            IP = ip;
        }
    }
}
