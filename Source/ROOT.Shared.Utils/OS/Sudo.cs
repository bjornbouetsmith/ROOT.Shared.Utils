using System.Runtime.InteropServices;

namespace ROOT.Shared.Utils.OS
{
    public static class Sudo
    {
        const string WindowsSudo = "/usr/bin/sudo";
        private const string UnixSudo = "/usr/bin/sudo";
        public static string BinPath => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? WindowsSudo : UnixSudo;
    }
}