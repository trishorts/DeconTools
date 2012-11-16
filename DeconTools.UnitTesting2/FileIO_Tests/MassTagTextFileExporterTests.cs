﻿using System.IO;
using DeconTools.Backend.Core;
using DeconTools.Backend.FileIO;
using NUnit.Framework;

namespace DeconTools.UnitTesting2.FileIO_Tests
{
    [TestFixture]
    public class MassTagTextFileExporterTests
    {

        string testOutput1 = "..\\..\\..\\TestFiles\\FileIOTests\\exportedMassTags.txt";


        [Test]
        public void exportMassTagsToTextFile_Test1()
        {
            //first, import some data
            TargetCollection mtc = new TargetCollection();
            string massTagTestFile1 = "..\\..\\..\\TestFiles\\FileIOTests\\top40MassTags.txt";
            MassTagFromTextFileImporter massTagImporter = new MassTagFromTextFileImporter(massTagTestFile1);
            mtc = massTagImporter.Import();

            //second, export it

            //but first delete any existing output file
            if (File.Exists(testOutput1))
            {
                File.Delete(testOutput1);
            }
            
            MassTagTextFileExporter exporter = new MassTagTextFileExporter(testOutput1);
            exporter.ExportResults(mtc.TargetList);

            FileInfo fi = new FileInfo(testOutput1);
            Assert.IsTrue(fi.Exists);

            //Assert.AreEqual(13716, fi.Length);

        }


    }
}
