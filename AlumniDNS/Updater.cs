using System;
using System.Net;

namespace AlumniDNS
{
    public class Updater
    {
        public const string API_ENDPOINT = "https://my.miab.com/admin/dns/custom";
        public const string USERNAME = "";
        public const string PASSWORD = "";
        private const string TLD = ".domain.tld";
        public static WebClient WebClient = new WebClient();

        public Updater()
        {
            WebClient.UseDefaultCredentials = true;
            WebClient.Credentials = new NetworkCredential(USERNAME, PASSWORD);
        }

        public static void Update(string subdomain, string ip)
        {
            subdomain = subdomain + TLD;
            try
            {
                WebClient.UploadString(API_ENDPOINT + subdomain, "PUT", ip);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            Console.WriteLine(subdomain + " updated to point to " + ip);
        }

        public static void Delete(string subdomain)
        {
            subdomain = subdomain + TLD;
            try
            {
                WebClient.UploadString(API_ENDPOINT + subdomain, "DELETE", "");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            Console.WriteLine(subdomain + " removed.");
        }
    }
}
