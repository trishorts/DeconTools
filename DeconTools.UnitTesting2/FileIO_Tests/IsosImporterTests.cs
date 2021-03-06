﻿using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using DeconTools.Backend.Data;
using DeconTools.Backend.Core;

namespace DeconTools.UnitTesting2.FileIO_Tests
{
    [TestFixture]
    public class IsosImporterTests
    {
        [Test]
        public void importOrbitrapData_test1()
        {
            var testMSFeatureFile = FileRefs.RawDataBasePath + @"\Output\QC_Shew_08_04-pt5-2_11Jan09_Sphinx_08-11-18_Scans6000_6050_isos.csv";
            var importer = new IsosImporter(testMSFeatureFile, Backend.Globals.MSFileType.Finnigan);

            var results = new List<IsosResult>();
            results = importer.Import();

            Assert.AreEqual(1340, results.Count);

            var testResult = results[0];

            Assert.AreEqual(6005, testResult.ScanSet.PrimaryScanNumber);
            Assert.AreEqual(2, testResult.IsotopicProfile.ChargeState);
            Assert.AreEqual(13084442, testResult.IntensityAggregate);
            Assert.AreEqual(481.274108886719m, (decimal)testResult.IsotopicProfile.GetMZ());
            Assert.AreEqual(0.0101m, (decimal)testResult.IsotopicProfile.Score);
            Assert.AreEqual(0.10352m, (decimal)testResult.InterferenceScore);




        }


        [Test]
        public void importUIMFData_test1()
        {
            var testMSFeatureFile = FileRefs.RawDataBasePath + @"\Output\35min_QC_Shew_Formic_4T_1.8_500_20_30ms_fr1950_0000_Frames800_802_isos.csv";
            var importer = new IsosImporter(testMSFeatureFile, Backend.Globals.MSFileType.PNNL_UIMF);

            var results = new List<IsosResult>();
            results = importer.Import();

            Assert.AreEqual(4709, results.Count);

            var testResult = (UIMFIsosResult)results[0];

            Assert.AreEqual(800, testResult.ScanSet.PrimaryScanNumber);
            Assert.AreEqual(207, testResult.IMSScanSet.PrimaryScanNumber);
            Assert.AreEqual(2, testResult.IsotopicProfile.ChargeState);
            Assert.AreEqual(1318, testResult.IntensityAggregate);
            Assert.AreEqual(402.731689453125m, (decimal)testResult.IsotopicProfile.GetMZ());
            Assert.AreEqual(0.0735m, (decimal)testResult.IsotopicProfile.Score);
            Assert.AreEqual(0.66701m, (decimal)testResult.InterferenceScore);


        }

        [Test]
        public void importPartialUIMFData_test1()
        {
            var startFrame = 801;
            var stopFrame = 801;


            var testMSFeatureFile = FileRefs.RawDataBasePath + @"\Output\35min_QC_Shew_Formic_4T_1.8_500_20_30ms_fr1950_0000_Frames800_802_isos.csv";
            var importer = new IsosImporter(testMSFeatureFile, Backend.Globals.MSFileType.PNNL_UIMF, startFrame, stopFrame);

            var results = new List<IsosResult>();
            results = importer.Import();

            Assert.AreEqual(1533, results.Count);

            var testResult = (UIMFIsosResult)results[0];

            Assert.AreEqual(801, testResult.ScanSet.PrimaryScanNumber);
            Assert.AreEqual(208, testResult.IMSScanSet.PrimaryScanNumber);
            Assert.AreEqual(2, testResult.IsotopicProfile.ChargeState);
            Assert.AreEqual(2135, testResult.IntensityAggregate);
            Assert.AreEqual(402.220489501953m, (decimal)testResult.IsotopicProfile.GetMZ());
            Assert.AreEqual(0.0619m, (decimal)testResult.IsotopicProfile.Score);
            Assert.AreEqual(0.38938m, (decimal)testResult.InterferenceScore);


            TestUtilities.DisplayMSFeatures(results);
        }
    }
}
