using System;
using System.DirectoryServices;
using System.Linq;
using System.Security.Principal;

namespace Dofus.Retro.Supertools.Core.Helpers
{
    public static class UidHelper
    {
        public static SecurityIdentifier GetComputerSid()
        {
            return new SecurityIdentifier((byte[])new DirectoryEntry($"WinNT://{Environment.MachineName},Computer").Children.Cast<DirectoryEntry>().First().InvokeGet("objectSID"), 0).AccountDomainSid;
        }
    }
}
