using System.Runtime.InteropServices;

namespace ROOT.Shared.Utils.OS
{
    public static class Sudo
    {
        //TODO: This is wrong, but I need to figure out how and if there is a windows equivelent to sudo
        const string WindowsSudo = "/usr/bin/sudo";
        private const string UnixSudo = "/usr/bin/sudo";
        public static string BinPath => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? WindowsSudo : UnixSudo;
    }
}