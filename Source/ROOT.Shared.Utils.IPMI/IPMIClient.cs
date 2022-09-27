using System;
using System.Collections.Generic;
using ROOT.Shared.Utils.OS;

namespace ROOT.Shared.Utils.IPMI
{
    /// <summary>
    /// IPMI_PASSWORD environment variable can be set to not use password.
    /// </summary>
    public class IPMIClient
    {
        private readonly string _hostName;
        private readonly string _userNam;
        private readonly string _password;
        private readonly string _ipmiInterface;
        private readonly bool _usePasswordFromEnv;
        private readonly int _timeOutSeconds;
        private readonly IPMIParser _parser = new IPMIParser();
        public IPMIClient(string hostName, string userNam, string password, string ipmiInterface = "lanplus", bool usePasswordFromEnv = false, int timeOutSeconds=10)
        {
            _hostName = hostName;
            _userNam = userNam;
            _password = password;
            _ipmiInterface = ipmiInterface;
            _usePasswordFromEnv = usePasswordFromEnv;
            _timeOutSeconds = timeOutSeconds;
            if (usePasswordFromEnv && string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException($"Please specify password or set {nameof(usePasswordFromEnv)} to true");
            }
        }

        public IEnumerable<IPMISensorRecord> LoadSensorReadings(SSHProcessCall sshCall = null)
        {
            var pc = GetIPMISensorRecordsProcessCall();
            if (sshCall != null)
            {
                pc = sshCall | pc;
            }

            pc.Timeout = TimeSpan.FromSeconds(_timeOutSeconds);
            var resp = pc.LoadResponse();
            if (!resp.Success)
            {
                throw resp.ToException();
            }

            var data = resp.StdOut;

            return _parser.ParseSensorReadings(data);
        }

        public IEnumerable<Sensor> LoadSensors(SSHProcessCall sshCall = null)
        {
            var pc = GetIPMISensorListProcessCall();
            if (sshCall != null)
            {
                pc = sshCall | pc;
            }

            var resp = pc.LoadResponse();
            if (!resp.Success)
            {
                throw resp.ToException();
            }

            var data = resp.StdOut;

            return _parser.ParseSensorIds(data);
        }

        private ProcessCall GetIPMISensorListProcessCall()
        {
            var args = $"-I {_ipmiInterface} -H {_hostName} -U {_userNam}";
            if (_usePasswordFromEnv)
            {
                args += $" -E sensor -v";
            }
            else
            {
                args += $" -P {_password} sensor -v";
            }

            var pc = new ProcessCall("/usr/bin/ipmitool", args) | new ProcessCall("/usr/bin/grep", "'Sensor ID'");

            return pc;
        }
        private ProcessCall GetIPMISensorRecordsProcessCall()
        {
            var args = $"-I {_ipmiInterface} -H {_hostName} -U {_userNam}";
            if (_usePasswordFromEnv)
            {
                args += $" -E sensor";
            }
            else
            {
                args += $" -P {_password} sensor";
            }

            return new ProcessCall("/usr/bin/ipmitool", args);
        }

        private ProcessCall GetIPMISensorValuesProcessCall()
        {
            var args = $"-I {_ipmiInterface} -H {_hostName} -U {_userNam}";
            if (_usePasswordFromEnv)
            {
                args += $" -E sensor";
            }
            else
            {
                args += $" -P {_password} sdr";
            }

            return new ProcessCall("/usr/bin/ipmitool", args);
        }
    }
}
