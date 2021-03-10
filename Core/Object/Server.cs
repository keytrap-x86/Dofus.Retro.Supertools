using System.Linq;

namespace Dofus.Retro.Supertools.Core.Object
{
    public class Server
    {
        public string Id { get; set; }
        public string Ip { get; set; }
        public string Name { get; set; }

        public Server(string id, string ip, string name)
        {
            Id = id;
            Ip = ip;
            Name = name;
        }

        public Server this[string name] => Static.Datacenter.Servers.FirstOrDefault(s => s.Name == name);
    }
}
