﻿using System;
using System.Collections.Generic;
using System.Text;
using DeconTools.Backend.Data;
using NUnit.Framework;
using DeconTools.Backend.Core;
using DeconTools.Backend.Runs;
using DeconTools.Backend.Utilities;
using DeconTools.Backend.ProcessingTasks.MSGenerators;
using DeconTools.Backend.ProcessingTasks;
using DeconTools.Backend.ProcessingTasks.ResultValidators;
using System.Diagnostics;
using System.IO;
using DeconTools.Backend.FileIO;


namespace DeconTools.UnitTesting2.FileIO_Tests
{
    [TestFixture]
    public class MSFeatureExporterTests
    {
        readonly string exportedMSFeaturesToTextFileFromOrbitrapFile1 = FileRefs.TestFileBasePath + @"\FileIOTests\exportedMSFeaturesToTextFileFromOrbitrapFile1.csv";
        readonly string exportedMSFeaturesToSQLiteFromOrbitrapFile1 = FileRefs.TestFileBasePath + @"\FileIOTests\exportedMSFeaturesToSQLiteFromOrbitrapFile1.db3";
        readonly string exportedMSFeaturesToSQLiteFromUIMFFile1 = FileRefs.TestFileBasePath + @"\FileIOTests\exportedMSFeaturesToSQLiteFromUIMFFile1.db3";
        readonly string exportedMSFeaturesToTextFileFromUIMFFile1 = FileRefs.TestFileBasePath + @"\FileIOTests\exportedMSFeaturesToTextFileFromUIMFFile1.csv";

        [Test]
        public void outputToText_xcaliburData_Test1()
        {
            var exportedFile = exportedMSFeaturesToTextFileFromOrbitrapFile1;


            if (File.Exists(exportedFile))
            {
                File.Delete(exportedFile);
            }

            //create run and get some results
            var run = TestDataCreationUtilities.CreateResultsFromThreeScansOfStandardOrbitrapData();

            var isosExporter = new DeconTools.Backend.FileIO.MSFeatureToTextFileExporterBasic(exportedFile);
            isosExporter.ExportResults(run.ResultCollection.ResultList);

            Assert.AreEqual(true, File.Exists(exportedFile));

            var importer = new IsosImporter(exportedFile, run.MSFileType);
            var results=   importer.Import();

            Assert.IsTrue(results != null);
            Assert.IsTrue(results.Count>300);

            //TODO: need to check if '388' is good or not
            //Assert.AreEqual(388, results.Count);

           
        }

        
        /// <summary>
        /// Note:  this test fails if Configuration is set to 'x86'
        /// </summary>
        [Test]
        public void ouputToSQLite_xcaliburData_Test1()
        {
            var exportedFile = exportedMSFeaturesToSQLiteFromOrbitrapFile1;

            if (File.Exists(exportedFile))
            {
                File.Delete(exportedFile);
            }

            ExporterBase<IsosResult> exporter = new MSFeatureToSQLiteExporterBasic(exportedFile);

            var run = TestDataCreationUtilities.CreateResultsFromThreeScansOfStandardOrbitrapData();

            exporter.ExportResults(run.ResultCollection.ResultList);

            Assert.AreEqual(true, File.Exists(exportedFile));

            var fi = new FileInfo(exportedFile);
            //Assert.AreEqual(28672, fi.Length);
            Console.Write(fi.Length);


        }


        [Test]
        public void outputToText_UIMFData_Test1()
        {
            var exportedFile = exportedMSFeaturesToTextFileFromUIMFFile1;


            if (File.Exists(exportedFile))
            {
                File.Delete(exportedFile);
            }

            //create run and get some results
            var run = TestDataCreationUtilities.CreateResultsFromTwoFramesOfStandardUIMFData();

            ExporterBase<IsosResult> isosExporter = new DeconTools.Backend.FileIO.MSFeatureToTextFileExporterUIMF(exportedFile);
            isosExporter.ExportResults(run.ResultCollection.ResultList);

            Assert.AreEqual(true, File.Exists(exportedFile));

            var fi = new FileInfo(exportedFile);
            Assert.AreEqual(135329, fi.Length);    //TODO: verify this
            Console.Write(fi.Length);
        }

        [Test]
        public void ouputToSQLite_UIMFData_Test1()
        {
            var exportedFile = exportedMSFeaturesToSQLiteFromUIMFFile1;

            if (File.Exists(exportedFile))
            {
                File.Delete(exportedFile);
            }

            ExporterBase<IsosResult> exporter = new MSFeatureToSQLiteExporterUIMF(exportedFile);

            var run = TestDataCreationUtilities.CreateResultsFromTwoFramesOfStandardUIMFData();

            exporter.ExportResults(run.ResultCollection.ResultList);

            Assert.AreEqual(true, File.Exists(exportedFile));

            var fi = new FileInfo(exportedFile);
            Assert.AreEqual(141312, fi.Length);
            Console.Write(fi.Length);


        }



    }
}
