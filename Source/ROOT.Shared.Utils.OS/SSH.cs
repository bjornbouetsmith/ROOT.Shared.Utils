using System.Runtime.InteropServices;

namespace ROOT.Shared.Utils.OS
{
    public static class SSH
    {
        const string WindowsSSh = "C:\\Windows\\System32\\OpenSSH\\ssh.exe";
        private const string UnixSsh = "/usr/bin/ssh";
        public static string BinPath => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? WindowsSSh : UnixSsh;
    }
}