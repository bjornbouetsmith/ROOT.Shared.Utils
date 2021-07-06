using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace ROOT.Shared.Utils.IPMI
{
    public class IPMIParser
    {
        /// <summary>
        /// Parse lines like:
        /// Sensor ID              : CPU1 Temp (0x1)
        /// Sensor ID              : CPU2 Temp(0x2)
        /// Sensor ID              : PCH Temp(0xa)
        /// Sensor ID              : System Temp(0xb)
        /// Sensor ID              : Peripheral Temp(0xc)
        /// Sensor ID              : MB_10G Temp(0xd)
        /// Sensor ID              : Vcpu1VRM Temp(0x10)
        /// Sensor ID              : Vcpu2VRM Temp(0x11)
        /// Sensor ID              : VmemABVRM Temp(0x12)
        /// into Sensor objects with Name and id from parantesis
        /// </summary>
        /// <param name="rawLines">Raw string like example</param>
        public IEnumerable<Sensor> ParseSensorIds(string rawLines)
        {
            var lines = rawLines.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var records = line.Split(':');
                var data = records[1];
                var dataValues = data.Split('(');
                var name = dataValues[0].Trim();
                var idValue = dataValues[1].Substring(0, dataValues[1].IndexOf(')'));
                var id = Convert.ToInt32(idValue, 16);
                yield return Sensor.LookupOrAdd(id, name);
            }
        }
        /// <summary>
        /// CPU1 Temp        | 41.000     | degrees C  | ok    | 0.000     | 0.000     | 0.000     | 83.000    | 88.000    | 88.000
        /// CPU2 Temp        | 49.000     | degrees C  | ok    | 0.000     | 0.000     | 0.000     | 83.000    | 88.000    | 88.000
        /// PCH Temp         | 50.000     | degrees C  | ok    | 0.000     | 5.000     | 16.000    | 90.000    | 95.000    | 100.000
        /// System Temp      | 39.000     | degrees C  | ok    | -10.000   | -5.000    | 0.000     | 80.000    | 85.000    | 90.000
        /// Peripheral Temp  | 51.000     | degrees C  | ok    | -10.000   | -5.000    | 0.000     | 80.000    | 85.000    | 90.000
        /// MB_10G Temp      | 66.000     | degrees C  | ok    | -5.000    | 0.000     | 5.000     | 95.000    | 100.000   | 105.000
        /// Vcpu1VRM Temp    | 42.000     | degrees C  | ok    | -5.000    | 0.000     | 5.000     | 95.000    | 100.000   | 105.000
        /// Vcpu2VRM Temp    | 47.000     | degrees C  | ok    | -5.000    | 0.000     | 5.000     | 95.000    | 100.000   | 105.000
        /// VmemABVRM Temp   | 45.000     | degrees C  | ok    | -5.000    | 0.000     | 5.000     | 95.000    | 100.000   | 105.000
        /// </summary>
        public IEnumerable<IPMISensorRecord> ParseSensorReadings(string rawLines)
        {
            var lines = rawLines.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var records = line.Split('|');
                var name = records[0].Trim();
                if (!Sensor.TryLookup(name, out var sensor))
                {
                    throw new InvalidOperationException($"Please load sensor ids first, by calling IPMIClient.{nameof(IPMIClient.LoadSensors)}");
                }

                var rec = new IPMISensorRecord();
                rec.Sensor = sensor;
                yield return rec;

            }
        }
    }

    public class IPMISensorRecord
    {
        public Sensor Sensor { get; set; }
        public string SensorReading { get; set; }
        public SensorType SensorType { get; set; }
        public string Status { get; set; }
        public string LowerNonRecoverable { get; set; }
        public string LowerCritical { get; set; }
        public string LowerNonCritical { get; set; }
        public string UpperNonCritical { get; set; }
        public string UpperCritical { get; set; }
        public string UpperNonRecoverable { get; set; }
    }

    public class Sensor
    {
        private static ConcurrentDictionary<int, Sensor> _values = new();
        private static ConcurrentDictionary<string, Sensor> _byName = new();
        private static Func<int, string, Sensor> _sensorFactory = (id, name) => new Sensor(id, name);
        public int Id { get; set; }
        public string Name { get; set; }

        public Sensor(int id, string name)
        {
            Id = id;
            Name = name;
        }

        public static Sensor LookupOrAdd(int id, string name)
        {
            var sensor = _values.GetOrAdd(id, _sensorFactory, name);
            _byName.TryAdd(name, sensor);
            return sensor;
        }

        public static bool TryLookup(string name, out Sensor sensor)
        {
            return _byName.TryGetValue(name, out sensor);
        }


    }

    public class SensorType
    {
        public static SensorType Volts = new SensorType("Volts");
        public static SensorType RPM = new SensorType("RPM");
        public static SensorType DegreesC = new SensorType("degrees C");
        public static SensorType Discrete = new SensorType("discrete");
        public static SensorType N_A = new SensorType("");

        public string Name { get; set; }

        public SensorType(string name)
        {
            Name = name;
        }
    }

    public class SensorStatus : IEquatable<SensorStatus>
    {
        public static SensorStatus OK = new SensorStatus("ok");
        public static SensorStatus N_A = new SensorStatus("na");

        public string Name { get; }

        public SensorStatus(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        public bool Equals(SensorStatus other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Name == other.Name;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((SensorStatus)obj);
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }
    }
}
