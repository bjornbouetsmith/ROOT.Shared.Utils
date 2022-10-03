using System.IO;
using System.Runtime.InteropServices;

namespace ROOT.Shared.Utils.OS
{
    public static class OperatingSystem
    {
        public static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        public static bool IsMacOS => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

        public static bool IsLinux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

        public static string Description => RuntimeInformation.OSDescription;

        private static LinuxType? _linuxType;
        public static LinuxType LinuxType => _linuxType ??= GetLinuxType(Description);

        private static LinuxType GetLinuxType(string description)
        {
            var toLower = description.ToLowerInvariant();

            if (toLower.Contains("ubuntu"))
            {
                return LinuxType.Ubuntu;
            }

            if (toLower.Contains("debian"))
            {
                return LinuxType.Debian;
            }

            if (toLower.Contains("fedora"))
            {
                return LinuxType.Fedora;
            }

            if (toLower.Contains("centos"))
            {
                return LinuxType.CentOS;
            }

            if (toLower.Contains("rocky"))
            {
                return LinuxType.Rocky;
            }

            if (toLower.Contains("oracle"))
            {
                return LinuxType.Oracle;
            }

            if (toLower.Contains("opensuse"))
            {
                return LinuxType.OpenSuse;
            }

            if (toLower.Contains("sles"))
            {
                return LinuxType.SLES;
            }

            if (toLower.Contains("redhat") || toLower.Contains("rhel"))
            {
                return LinuxType.Redhat;
            }

            return LinuxType.Other;
        }

        private static ServiceInit? _serviceInit;
        public static ServiceInit ServiceInit => _serviceInit ??= GetServiceInit();

        private static ServiceInit GetServiceInit()
        {
            var which = "/bin/which";
            if (File.Exists(which))
            {
                var pc = new ProcessCall(which, "systemctl");

                var response = pc.LoadResponse(false);
                if (response.Success && response.StdOut.Trim()
                       .ToLowerInvariant().Contains("systemctl"))
                {
                    return ServiceInit.SystemD;
                }
                pc = new ProcessCall(which, "service");

                response = pc.LoadResponse(false);
                if (response.Success && response.StdOut.Trim()
                        .ToLowerInvariant().Contains("service"))
                {
                    return ServiceInit.SysVInit;
                }

                return ServiceInit.Unknown;
            }

            // TODO: Just use find / -name systemctl
            // TODO: Just use find / -name service
            return ServiceInit.Unknown;
        }
    }
}


