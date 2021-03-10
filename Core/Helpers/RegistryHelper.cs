using Microsoft.Win32;

using System;

namespace Dofus.Retro.Supertools.Core.Helpers
{
    public static class RegistryHelper
    {
        public static bool IsNpcapInstalled()
        {
            try
            {
                var npcapKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\NpcapInst", false);
                npcapKey?.Close();
                return npcapKey != null;
            }
            catch (Exception)
            {
                return false;
            }

        }
    }
}
