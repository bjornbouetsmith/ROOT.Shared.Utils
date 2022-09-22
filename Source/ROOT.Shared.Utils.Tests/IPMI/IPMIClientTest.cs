using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ROOT.Shared.Utils.IPMI;
using ROOT.Shared.Utils.OS;
using ROOT.Shared.Utils.Serialization;

namespace ROOT.Shared.Utils.Tests.IPMI
{
    [TestClass]
    public class IPMIClientTest
    {
        [TestMethod,Timeout(10000),Ignore]
        public void LoadSensorIDs()
        {

            var client = new IPMIClient("192.168.10.253", "ADMIN", "ADMIN");

            var remotePc = new RemoteProcessCall("bbs", "192.168.0.150");

            var data = client.LoadSensors(remotePc).ToList();


            var resp = data.Dump();

            Console.WriteLine(resp);

        }

        [TestMethod, Timeout(10000),Ignore]
        public void LoadSensorRecords()
        {
            var client = new IPMIClient("192.168.10.253", "ADMIN", "ADMIN");

            var remotePc = new RemoteProcessCall("bbs", "192.168.0.150");

            var sensors = client.LoadSensors(remotePc).ToList();
            var data = client.LoadSensorReadings(remotePc).ToList();


            var resp = data.Dump();

            Console.WriteLine(resp);

        }

    }
}
