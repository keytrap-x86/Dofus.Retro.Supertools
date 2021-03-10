using System.Net;
using System.Net.Sockets;

namespace Dofus.Retro.Supertools.Core.Helpers
{
    public static class NetworkHelper
    {
        public static string GetIpFromDnsName(string dnsName) => Dns.GetHostAddresses(dnsName)[0].ToString();

        public static bool LocalIpFound()
        {
            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                //Récupération de l'IP locale du PC
                socket.Connect("8.8.8.8", 65530);
                if (!(socket.LocalEndPoint is IPEndPoint endPoint) || endPoint.Address == null)
                {

                    return false;
                }

                Static.Datacenter.LocalIp = endPoint.Address.ToString();
            }

            return Static.Datacenter.LocalIp != null;
        }
    }
}
