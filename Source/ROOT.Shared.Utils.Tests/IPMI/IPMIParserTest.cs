using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ROOT.Shared.Utils.IPMI;
using ROOT.Shared.Utils.Serialization;

namespace ROOT.Shared.Utils.Tests.IPMI
{
    [TestClass]
    public class IPMIParserTest
    {
        [TestMethod]
        public void ParseSensorRecords()
        {
            const string raw = @"Sensor ID              : CPU1 Temp (0x1)
Sensor ID              : CPU2 Temp (0x2)
Sensor ID              : PCH Temp (0xa)
Sensor ID              : System Temp (0xb)
Sensor ID              : Peripheral Temp (0xc)
Sensor ID              : MB_10G Temp (0xd)
Sensor ID              : Vcpu1VRM Temp (0x10)
Sensor ID              : Vcpu2VRM Temp (0x11)
Sensor ID              : VmemABVRM Temp (0x12)
Sensor ID              : VmemCDVRM Temp (0x13)
Sensor ID              : VmemEFVRM Temp (0x14)
Sensor ID              : VmemGHVRM Temp (0x15)
Sensor ID              : P1-DIMMA1 Temp (0xb0)
Sensor ID              : P1-DIMMA2 Temp (0xb1)
Sensor ID              : P1-DIMMB1 Temp (0xb4)
Sensor ID              : P1-DIMMB2 Temp (0xb5)
Sensor ID              : P1-DIMMC1 Temp (0xb8)
Sensor ID              : P1-DIMMC2 Temp (0xb9)
Sensor ID              : P1-DIMMD1 Temp (0xbc)
Sensor ID              : P1-DIMMD2 Temp (0xbd)
Sensor ID              : P2-DIMME1 Temp (0xd0)
Sensor ID              : P2-DIMME2 Temp (0xd1)
Sensor ID              : P2-DIMMF1 Temp (0xd4)
Sensor ID              : P2-DIMMF2 Temp (0xd5)
Sensor ID              : P2-DIMMG1 Temp (0xd8)
Sensor ID              : P2-DIMMG2 Temp (0xd9)
Sensor ID              : P2-DIMMH1 Temp (0xdc)
Sensor ID              : P2-DIMMH2 Temp (0xdd)
Sensor ID              : FAN1 (0x41)
Sensor ID              : FAN2 (0x42)
Sensor ID              : FAN3 (0x43)
Sensor ID              : FAN4 (0x44)
Sensor ID              : FAN5 (0x45)
Sensor ID              : FAN6 (0x46)
Sensor ID              : FANA (0x47)
Sensor ID              : FANB (0x48)
Sensor ID              : 12V (0x30)
Sensor ID              : 5VCC (0x31)
Sensor ID              : 3.3VCC (0x32)
Sensor ID              : VBAT (0x33)
Sensor ID              : Vcpu1 (0x34)
Sensor ID              : Vcpu2 (0x36)
Sensor ID              : VDIMMAB (0x35)
Sensor ID              : VDIMMCD (0x37)
Sensor ID              : VDIMMEF (0x3a)
Sensor ID              : VDIMMGH (0x3b)
Sensor ID              : 5VSB (0x38)
Sensor ID              : 3.3VSB (0x39)
Sensor ID              : 1.5V PCH (0x3c)
Sensor ID              : 1.2V BMC (0x3d)
Sensor ID              : 1.05V PCH (0x3e)
Sensor ID              : Chassis Intru (0xaa)
Sensor ID              : PS2 Status (0xc9)";

            var parser = new IPMIParser();
            var sensors = parser.ParseSensorIds(raw);
            Console.WriteLine(sensors.Dump());
            
        }

    }
}
