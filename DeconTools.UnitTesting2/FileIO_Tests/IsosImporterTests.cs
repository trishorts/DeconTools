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
            string testMSFeatureFile = FileRefs.RawDataBasePath + @"\Output\QC_Shew_08_04-pt5-2_11Jan09_Sphinx_08-11-18_Scans6000_6050_isos.csv";
            IsosImporter importer = new IsosImporter(testMSFeatureFile, Backend.Globals.MSFileType.Finnigan);

            List<IsosResult> results = new List<IsosResult>();
            results = importer.Import();

            Assert.AreEqual(1340, results.Count);

            IsosResult testResult = results[0];

            Assert.AreEqual(6005, testResult.ScanSet.PrimaryScanNumber);
            Assert.AreEqual(2, testResult.IsotopicProfile.ChargeState);
            Assert.AreEqual(13084442, testResult.IsotopicProfile.IntensityAggregate);
            Assert.AreEqual(481.274108886719m, (decimal)testResult.IsotopicProfile.GetMZ());
            Assert.AreEqual(0.0101m, (decimal)testResult.IsotopicProfile.Score);
            Assert.AreEqual(0.10352m, (decimal)testResult.InterferenceScore);




        }


        [Test]
        public void importUIMFData_test1()
        {
            string testMSFeatureFile = FileRefs.RawDataBasePath + @"\Output\35min_QC_Shew_Formic_4T_1.8_500_20_30ms_fr1950_0000_Frames800_802_isos.csv";
            IsosImporter importer = new IsosImporter(testMSFeatureFile, Backend.Globals.MSFileType.PNNL_UIMF);

            List<IsosResult> results = new List<IsosResult>();
            results = importer.Import();

            Assert.AreEqual(4709, results.Count);

            UIMFIsosResult testResult = (UIMFIsosResult)results[0];

            Assert.AreEqual(800, testResult.FrameSet.PrimaryFrame);
            Assert.AreEqual(207, testResult.ScanSet.PrimaryScanNumber);
            Assert.AreEqual(2, testResult.IsotopicProfile.ChargeState);
            Assert.AreEqual(1318, testResult.IsotopicProfile.IntensityAggregate);
            Assert.AreEqual(402.731689453125m, (decimal)testResult.IsotopicProfile.GetMZ());
            Assert.AreEqual(0.0735m, (decimal)testResult.IsotopicProfile.Score);
            Assert.AreEqual(0.66701m, (decimal)testResult.InterferenceScore);


        }

        [Test]
        public void importPartialUIMFData_test1()
        {
            int startFrame = 801;
            int stopFrame = 801;


            string testMSFeatureFile = FileRefs.RawDataBasePath + @"\Output\35min_QC_Shew_Formic_4T_1.8_500_20_30ms_fr1950_0000_Frames800_802_isos.csv";
            IsosImporter importer = new IsosImporter(testMSFeatureFile, Backend.Globals.MSFileType.PNNL_UIMF, startFrame, stopFrame);

            List<IsosResult> results = new List<IsosResult>();
            results = importer.Import();

            Assert.AreEqual(1533, results.Count);

            UIMFIsosResult testResult = (UIMFIsosResult)results[0];

            Assert.AreEqual(801, testResult.FrameSet.PrimaryFrame);
            Assert.AreEqual(208, testResult.ScanSet.PrimaryScanNumber);
            Assert.AreEqual(2, testResult.IsotopicProfile.ChargeState);
            Assert.AreEqual(2135, testResult.IsotopicProfile.IntensityAggregate);
            Assert.AreEqual(402.220489501953m, (decimal)testResult.IsotopicProfile.GetMZ());
            Assert.AreEqual(0.0619m, (decimal)testResult.IsotopicProfile.Score);
            Assert.AreEqual(0.38938m, (decimal)testResult.InterferenceScore);


            TestUtilities.DisplayMSFeatures(results);
        }
    }
}