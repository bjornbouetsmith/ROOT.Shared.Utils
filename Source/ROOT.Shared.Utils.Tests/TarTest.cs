﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ROOT.Shared.Utils.Archiving.Tar;

namespace ROOT.Shared.Utils.Tests
{
    [TestClass]
    public class TarTest
    {
        [TestMethod]
        public void CreateSimpleTar()
        {
            var tempPath = Path.GetTempPath();

            var tarPath = Path.Combine(tempPath, "tartest");

            Console.WriteLine(tarPath);

            Directory.CreateDirectory(tarPath);

            var fileName = Path.Combine(tarPath, "test.txt");
            var tarName = Path.Combine(tempPath, "test.tar");
            File.WriteAllText(fileName, string.Join("", Enumerable.Repeat("A", 10000)));

            try
            {
                using (var tar = new TarWriter(tarName, tempPath))
                {
                    tar.WriteDirectoryAsync(tarPath, true).Wait();
                }
            }
            finally
            {
                Directory.Delete(tarPath, true);
                File.Delete(tarName);
            }
        }
    }
}
